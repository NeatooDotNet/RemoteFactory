using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;
using Neatoo.RemoteFactory;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Demonstrates the dispatch-phase contract from FactoryEventPhasesPattern.cs:
/// where each phase's handlers run relative to the factory method body, the
/// consumer's AfterFlush drain, the fail-open sweep, and discard on failure.
/// </summary>
/// <remarks>
/// Every sequence assertion leans on a "*-method-done" marker the factory method
/// records after its drain call: a handler marker BEFORE it proves the consumer's
/// drain ran the handler inside the body, because the only other path that runs
/// AfterFlush work — the fail-open sweep — fires after the body returns.
/// Registration is entirely attribute-declared through the generated registrar;
/// nothing here registers handlers by hand.
/// </remarks>
public class FactoryEventPhasesTests
{
    /// <summary>
    /// One event, three phases: Immediate at Raise, AfterFlush at the method's
    /// coordinator call (inside the body), AfterCommit after the entry call
    /// completes — the consumer drain pattern end to end over the remote boundary.
    /// </summary>
    [Fact]
    public async Task Finalize_Remote_RunsEachPhaseAtItsDrainPoint()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IInvoiceFactory>();
        var id = Guid.NewGuid();

        await factory.Finalize(id, 100m);

        var audit = server.GetRequiredService<IPhaseAuditService>();
        Assert.Equal(
            ["invoice-immediate", "invoice-flush", "invoice-method-done", "invoice-commit"],
            audit.EntriesFor(id));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>Same contract through the Logical (single-tier) container.</summary>
    [Fact]
    public async Task Finalize_Logical_RunsEachPhaseAtItsDrainPoint()
    {
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IInvoiceFactory>();
        var id = Guid.NewGuid();

        await factory.Finalize(id, 100m);

        var audit = local.GetRequiredService<IPhaseAuditService>();
        Assert.Equal(
            ["invoice-immediate", "invoice-flush", "invoice-method-done", "invoice-commit"],
            audit.EntriesFor(id));

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }

    /// <summary>
    /// Phase order, not raise order: QuarterClose._Run raises commit, flush,
    /// immediate — in that order — yet the handlers run in phase order. The second
    /// "q-immediate" (raised after the drain) lands between the AfterFlush and
    /// AfterCommit drain points: ordering is anchored per drain point, and there
    /// is no global barrier over the operation.
    /// </summary>
    [Fact]
    public async Task QuarterClose_Remote_RunsInPhaseOrderNotRaiseOrder()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var run = client.GetRequiredService<QuarterClose.Run>();
        var id = Guid.NewGuid();

        await run(id);

        var audit = server.GetRequiredService<IPhaseAuditService>();
        Assert.Equal(
            ["q-immediate", "q-flush", "q-immediate", "q-method-done", "q-commit"],
            audit.EntriesFor(id));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Fail-open: an AfterFlush handler whose factory method never drains still
    /// runs — after the method body, at the AfterCommit point (with Warning 9007
    /// in the server's logs naming the event type). Compare the marker order with
    /// Finalize_Remote above: the drain call is what moves "archive-flush" ahead
    /// of the method-done marker.
    /// </summary>
    [Fact]
    public async Task Archive_NeverDrained_AfterFlushHandlerRunsAtTheSweep()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IInvoiceArchiverFactory>();
        var id = Guid.NewGuid();

        await factory.Archive(id);

        var audit = server.GetRequiredService<IPhaseAuditService>();
        Assert.Equal(["archive-method-done", "archive-flush"], audit.EntriesFor(id));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// A failed entry call discards queued phased work. The Immediate handler ran
    /// at Raise — atomic with the operation, unwound by the consumer's rollback —
    /// but the AfterFlush and AfterCommit handlers never run at all.
    /// </summary>
    [Fact]
    public async Task PaymentIntake_EntryCallThrows_QueuedPhasedWorkIsDiscarded()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var record = client.GetRequiredService<PaymentIntake.Record>();
        var id = Guid.NewGuid();

        await Assert.ThrowsAnyAsync<Exception>(() => record(id));

        var audit = server.GetRequiredService<IPhaseAuditService>();
        Assert.Equal(["pay-started", "pay-immediate"], audit.EntriesFor(id));

        server.Dispose();
        client.Dispose();
    }
}
