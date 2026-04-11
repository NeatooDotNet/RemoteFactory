using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Events;

// =============================================================================
// TRANSACTION ROLLBACK TEST TARGETS
// =============================================================================
//
// Proves end-to-end that a [FactoryEventHandler<T>] handler exception rolls back
// a factory method's EF Core transaction. The invariant under test:
//
//   1. Factory method opens a transaction on a shared DbContext
//   2. Factory method writes an entity and SaveChangesAsync'es it
//   3. Factory method raises an event
//   4. Handler Alpha (shares DbContext via [Service] because shared scope)
//      writes another entity and SaveChangesAsync'es it
//   5. Handler Beta throws
//   6. The exception propagates out of Raise, back through the factory method,
//      without CommitAsync being reached — the `using` disposes the transaction,
//      which rolls back every uncommitted write
//   7. After the test, a fresh connection sees an empty table
//
// If the shared-scope / sequential / awaited / exception-propagates invariants
// are all working, step 7 produces an empty table. If any of them is broken, the
// test fails visibly.
//
// Uses Sqlite :memory: with a shared connection string (file::memory:?cache=shared)
// so multiple EF Core DbContexts in the same test observe the same in-memory DB.

public sealed class TxTestEntity
{
    public int Id { get; set; }
    public string Source { get; set; } = "";
}

public sealed class TxTestDbContext(DbContextOptions<TxTestDbContext> options) : DbContext(options)
{
    public DbSet<TxTestEntity> Entities => Set<TxTestEntity>();
}

/// <summary>
/// Shared SQLite in-memory connection holder. One per test scope; holds a single
/// <see cref="SqliteConnection"/> open for the lifetime of the scope so every
/// DbContext resolved from that scope observes the same in-memory database.
/// </summary>
public sealed class TxTestConnection : IDisposable
{
    public SqliteConnection Connection { get; }

    public TxTestConnection()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();
        using var ctx = new TxTestDbContext(
            new DbContextOptionsBuilder<TxTestDbContext>()
                .UseSqlite(Connection)
                .Options);
        ctx.Database.EnsureCreated();
    }

    public TxTestDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TxTestDbContext>()
            .UseSqlite(Connection)
            .Options);

    public void Dispose() => Connection.Dispose();
}

// -----------------------------------------------------------------------------
// EVENTS
// -----------------------------------------------------------------------------

/// <summary>Event whose handlers both succeed — the happy-path rollback test.</summary>
public record TxHappyEvent(string Tag) : FactoryEventBase;

/// <summary>Event whose second handler throws — the rollback test.</summary>
public record TxRollbackEvent(string Tag) : FactoryEventBase;

// -----------------------------------------------------------------------------
// HANDLERS
// -----------------------------------------------------------------------------

/// <summary>
/// Handler for the happy-path event: writes an entity and saves. Shares the
/// factory's scope → shares the factory's DbContext → writes go into the same
/// transaction.
/// </summary>
[FactoryEventHandler<TxHappyEvent>]
public partial class TxHappyHandler
{
    internal static async Task Handle(
        TxHappyEvent evt,
        [Service] TxTestDbContext db,
        CancellationToken ct)
    {
        db.Entities.Add(new TxTestEntity { Source = $"happy-handler:{evt.Tag}" });
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// First handler for the rollback event. Writes and saves. After the second
/// handler throws, this SaveChangesAsync must be rolled back — i.e. on the
/// next connection after the test, this entity is NOT present.
/// </summary>
[FactoryEventHandler<TxRollbackEvent>]
public partial class TxRollbackHandlerWriter
{
    internal static async Task Handle(
        TxRollbackEvent evt,
        [Service] TxTestDbContext db,
        CancellationToken ct)
    {
        db.Entities.Add(new TxTestEntity { Source = $"writer-handler:{evt.Tag}" });
        await db.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Second handler for the rollback event. Throws. The exception propagates back
/// through IFactoryEvents.Raise, out of the factory method, and bypasses the
/// factory method's CommitAsync — the using-disposed transaction rolls every
/// uncommitted write back.
/// </summary>
[FactoryEventHandler<TxRollbackEvent>]
public partial class TxRollbackHandlerThrower
{
    internal static Task Handle(
        TxRollbackEvent evt,
        [Service] TxTestDbContext db,
        CancellationToken ct)
    {
        throw new InvalidOperationException($"rollback-handler throws for tag {evt.Tag}");
    }
}

// -----------------------------------------------------------------------------
// FACTORY — writes an entity inside an explicit transaction, then raises the
// event. If the event chain succeeds, the transaction commits. If any handler
// throws, the exception propagates and the transaction is rolled back by the
// using-dispose.
// -----------------------------------------------------------------------------

[Factory]
public partial class TxTestFactory
{
    public int CreatedId { get; set; }
    public string Tag { get; set; } = "";

    public TxTestFactory() { }

    [Execute]
    public static async Task<TxTestFactory> RunHappyPath(
        string tag,
        [Service] TxTestDbContext db,
        [Service] IFactoryEvents events,
        CancellationToken ct)
    {
        using var tx = await db.Database.BeginTransactionAsync(ct);

        var entity = new TxTestEntity { Source = $"factory:{tag}" };
        db.Entities.Add(entity);
        await db.SaveChangesAsync(ct);

        await events.Raise(new TxHappyEvent(tag), RaiseOptions.None, ct);

        await tx.CommitAsync(ct);
        return new TxTestFactory { CreatedId = entity.Id, Tag = tag };
    }

    [Execute]
    public static async Task<TxTestFactory> RunRollbackPath(
        string tag,
        [Service] TxTestDbContext db,
        [Service] IFactoryEvents events,
        CancellationToken ct)
    {
        using var tx = await db.Database.BeginTransactionAsync(ct);

        var entity = new TxTestEntity { Source = $"factory:{tag}" };
        db.Entities.Add(entity);
        await db.SaveChangesAsync(ct);

        // TxRollbackHandlerThrower throws here — the exception propagates out
        // of Raise, out of this method, and bypasses tx.CommitAsync below.
        await events.Raise(new TxRollbackEvent(tag), RaiseOptions.None, ct);

        await tx.CommitAsync(ct); // unreachable in the rollback scenario
        return new TxTestFactory { CreatedId = entity.Id, Tag = tag };
    }
}
