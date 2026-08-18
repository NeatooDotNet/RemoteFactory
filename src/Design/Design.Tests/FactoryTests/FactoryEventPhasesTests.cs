using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
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

        var invoice = await factory.Finalize(id, 100m);

        var audit = server.GetRequiredService<IPhaseAuditService>();
        Assert.Equal(
            ["invoice-immediate", "invoice-flush", "invoice-method-done", "invoice-commit"],
            audit.EntriesFor(id));

        // The remote leg is real: the entity serialized back across the boundary.
        Assert.Equal(id, invoice.Id);
        Assert.Equal(100m, invoice.Total);

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
    /// runs — after the method body, at the AfterCommit point. (The framework also
    /// logs Warning 9007 naming the event type; that emission is pinned by the
    /// unit and integration suites — this harness runs a NullLogger, so only the
    /// marker order is observed here.) Compare the order with Finalize_Remote
    /// above: the drain call is what moves "archive-flush" ahead of the
    /// method-done marker.
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

        await Assert.ThrowsAnyAsync<Exception>(() => record(id, reject: true));

        var audit = server.GetRequiredService<IPhaseAuditService>();
        Assert.Equal(["pay-started", "pay-immediate"], audit.EntriesFor(id));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// The positive control for the discard demonstration, and the proof the
    /// failed call's queue was discarded rather than left dangling: a failed call
    /// followed by a successful call in the SAME server scope. The success runs
    /// all four of its own markers; the failed call's trail never grows — its
    /// queued work does not ride the survivor's drain.
    /// </summary>
    [Fact]
    public async Task PaymentIntake_FailedThenSuccessfulCall_SameScope_DiscardsRatherThanLeaks()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var record = client.GetRequiredService<PaymentIntake.Record>();
        var rejectedId = Guid.NewGuid();
        var acceptedId = Guid.NewGuid();

        await Assert.ThrowsAnyAsync<Exception>(() => record(rejectedId, reject: true));
        await record(acceptedId, reject: false);

        var audit = server.GetRequiredService<IPhaseAuditService>();

        // The success path proves every Payment handler is wired end to end.
        Assert.Equal(
            ["pay-started", "pay-immediate", "pay-flush", "pay-done", "pay-commit"],
            audit.EntriesFor(acceptedId));

        // Discarded, not leaked: the rejected call's flush/commit work did not
        // run during the accepted call's drains.
        Assert.Equal(["pay-started", "pay-immediate"], audit.EntriesFor(rejectedId));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// The DI boundary: the coordinator is registered where handlers dispatch
    /// (Server, Logical) and absent in Remote mode — a client container has no
    /// handlers to drain, so resolving it there yields null (and `[Service]`
    /// injection on a non-[Remote] method would fail with the standard
    /// not-registered exception).
    /// </summary>
    [Fact]
    public void Coordinator_NotRegisteredInTheRemoteClientContainer()
    {
        var (server, client, local) = DesignClientServerContainers.Scopes();

        Assert.Null(client.ServiceProvider.GetService<IFactoryEventPhaseCoordinator>());
        Assert.NotNull(server.ServiceProvider.GetService<IFactoryEventPhaseCoordinator>());
        Assert.NotNull(local.ServiceProvider.GetService<IFactoryEventPhaseCoordinator>());

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }
}
