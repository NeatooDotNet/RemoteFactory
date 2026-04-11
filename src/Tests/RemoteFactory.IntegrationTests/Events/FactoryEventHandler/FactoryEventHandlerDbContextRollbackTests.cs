using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.FactoryEventHandler;

/// <summary>
/// End-to-end proof that a throwing [FactoryEventHandler&lt;T&gt;] handler rolls back
/// the factory method's EF Core transaction. Together with the IScopeProbe tests
/// this closes the loop on the v1.1.0 value proposition:
///
///   shared scope → shared DbContext → shared transaction → handler exception
///   → transaction rolled back on the way out.
///
/// Each test gets its own isolated SQLite :memory: database held open by a single
/// connection for the duration of the scope.
/// </summary>
public class FactoryEventHandlerDbContextRollbackTests
{
    /// <summary>
    /// Builds a scope with a single-connection SQLite :memory: DbContext registered
    /// as scoped. The TxTestConnection holds the shared connection alive; the
    /// TxTestDbContext is resolved scoped and closes when the scope disposes.
    /// </summary>
    private static (IServiceScope client, IServiceScope server, IServiceScope local) CreateScopes(TxTestConnection connection)
    {
        return ClientServerContainers.Scopes(
            configureClient: null,
            configureServer: services =>
            {
                services.AddSingleton(connection);
                services.AddScoped(sp =>
                {
                    var conn = sp.GetRequiredService<TxTestConnection>();
                    return new TxTestDbContext(
                        new DbContextOptionsBuilder<TxTestDbContext>()
                            .UseSqlite(conn.Connection)
                            .Options);
                });
            });
    }

    [Fact]
    public async Task HappyPath_FactoryAndHandlerBothPersist()
    {
        using var connection = new TxTestConnection();
        var (client, server, local) = CreateScopes(connection);

        var factory = server.GetRequiredService<ITxTestFactoryFactory>();
        var result = await factory.RunHappyPath("happy");
        Assert.NotNull(result);
        Assert.NotEqual(0, result.CreatedId);

        // Open a fresh context on the same shared connection; both rows must be present.
        using var verify = connection.NewContext();
        var rows = await verify.Entities.AsNoTracking().OrderBy(e => e.Id).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.Source == "factory:happy");
        Assert.Contains(rows, r => r.Source == "happy-handler:happy");
    }

    [Fact]
    public async Task HandlerThrows_FactoryMethodTransactionRollsBack()
    {
        using var connection = new TxTestConnection();
        var (client, server, local) = CreateScopes(connection);

        var factory = server.GetRequiredService<ITxTestFactoryFactory>();

        // The factory method writes, raises the event, handler Writer saves another
        // entity, handler Thrower throws. The exception aborts the chain, propagates
        // out of Raise, out of RunRollbackPath, bypassing tx.CommitAsync. The
        // using-dispose rolls every uncommitted write back.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.RunRollbackPath("rollback"));
        Assert.Contains("rollback-handler throws", ex.Message);

        // Fresh context: table must be empty. The factory's write AND the handler
        // Writer's write both participated in the same transaction and both were
        // rolled back when the transaction disposed without committing.
        using var verify = connection.NewContext();
        var rowCount = await verify.Entities.CountAsync();
        Assert.Equal(0, rowCount);
    }
}
