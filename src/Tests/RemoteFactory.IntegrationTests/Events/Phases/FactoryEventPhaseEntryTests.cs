using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;
using System.Collections.Concurrent;

namespace RemoteFactory.IntegrationTests.Events.Phases;

/// <summary>
/// End-to-end coverage for PHASE-003: the entry-call drain of AfterCommit handlers,
/// uniformly for HTTP-dispatched [Remote] calls (through HandleRemoteDelegateRequest)
/// and direct Logical invocation (through the generated wrapper seam), with
/// rollback-discard, swallow semantics, sweep ordering, and same-response relay.
/// </summary>
/// <remarks>
/// The ordering discriminator used throughout: the factory method records
/// "*-method-done" as its last statement, so an AfterCommit handler recorded AFTER
/// that marker provably ran at the entry drain, not inline at raise time.
/// </remarks>
public class FactoryEventPhaseEntryTests
{
    public FactoryEventPhaseEntryTests()
    {
        PhaseHandlerRegistrations.EnsureRegistered();
    }

    private static List<string> RecordedFor(IServiceScope server, Guid id)
    {
        return server.GetRequiredService<IEventTestService>()
            .GetRecordedEvents()
            .Where(e => e.EntityId == id)
            .Select(e => e.EventName)
            .ToList();
    }

    [Fact]
    public async Task RemoteCreate_AfterCommitHandlerRunsAfterTheEntryCallCompletes()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IPhaseEntryTargetFactory>();
        var id = Guid.NewGuid();

        await factory.Create(id);

        Assert.Equal(["immediate", "create-method-done", "after-commit"], RecordedFor(server, id));
    }

    [Fact]
    public async Task LogicalCreate_AfterCommitHandlerRunsAfterTheWrapperCompletes()
    {
        var (_, _, local) = ClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IPhaseEntryTargetFactory>();
        var id = Guid.NewGuid();

        await factory.Create(id);

        Assert.Equal(["immediate", "create-method-done", "after-commit"], RecordedFor(local, id));
    }

    [Fact]
    public async Task LogicalSave_NestedInsert_DrainsExactlyOnceAfterTheOutermostCompletion()
    {
        var (_, _, local) = ClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IPhaseEntryTargetFactory>();
        var id = Guid.NewGuid();
        var target = new PhaseEntryTarget { Id = id };

        await factory.Save(target);

        var recorded = RecordedFor(local, id);
        Assert.Equal(["insert-method-done", "save-after-commit"], recorded);
    }

    [Fact]
    public async Task RemoteSave_NestedUnderTheChokePoint_StillDrainsExactlyOnce()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IPhaseEntryTargetFactory>();
        var id = Guid.NewGuid();
        var target = new PhaseEntryTarget { Id = id };

        await factory.Save(target);

        var recorded = RecordedFor(server, id);
        Assert.Equal(["insert-method-done", "save-after-commit"], recorded);
    }

    [Fact]
    public async Task EntryDrain_SweepsAfterFlushBeforeAfterCommit()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var run = client.ServiceProvider.GetRequiredService<PhaseStaticCommands.RunSweep>();
        var id = Guid.NewGuid();

        await run(id);

        Assert.Equal(["sweep-method-done", "sweep-flush", "sweep-commit"], RecordedFor(server, id));
    }

    [Fact]
    public async Task StaticCommand_DrainsAtTheDelegateSeam_LocalAndRemote()
    {
        var (server, client, local) = ClientServerContainers.Scopes();

        var remoteId = Guid.NewGuid();
        await client.ServiceProvider.GetRequiredService<PhaseStaticCommands.RunPhased>()(remoteId);
        Assert.Equal(["static-method-done", "static-after-commit"], RecordedFor(server, remoteId));

        var localId = Guid.NewGuid();
        await local.ServiceProvider.GetRequiredService<PhaseStaticCommands.RunPhased>()(localId);
        Assert.Equal(["static-method-done", "static-after-commit"], RecordedFor(local, localId));
    }

    [Fact]
    public async Task RemoteEntryFails_QueuedHandlersNeverRun()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IPhaseEntryTargetFactory>();
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.FetchAndFail(id));

        Assert.Empty(RecordedFor(server, id));
    }

    [Fact]
    public async Task LogicalEntryFails_QueuedHandlersNeverRun()
    {
        var (_, _, local) = ClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IPhaseEntryTargetFactory>();
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.FetchAndFail(id));

        Assert.Empty(RecordedFor(local, id));
    }

    [Fact]
    public async Task FailedCall_ThenSuccessfulCall_InTheSameServerScope_RunsOnlyTheSecond()
    {
        // The harness reuses ONE server scope for every remote call in a test — the
        // long-lived-scope fixture (plan review A-V2). The failed call's deferred
        // work must be cleared, not left to ride the next call's drain.
        var (server, client, _) = ClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IPhaseEntryTargetFactory>();
        var failedId = Guid.NewGuid();
        var successId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.FetchAndFail(failedId));
        await factory.Create(successId);

        Assert.Empty(RecordedFor(server, failedId));
        Assert.Equal(["immediate", "create-method-done", "after-commit"], RecordedFor(server, successId));
    }

    [Fact]
    public async Task ForbiddenInnerCall_AfterEnqueueingPhasedWork_NothingRuns()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var run = client.ServiceProvider.GetRequiredService<PhaseStaticCommands.RunForbiddenInner>();
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<NotAuthorizedException>(() => run(id));

        Assert.Empty(RecordedFor(server, id));
    }

    [Fact]
    public async Task ThrowingAfterCommitHandler_IsSwallowed_TheCallSucceeds_AndTheSurvivorStillRuns()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var run = client.ServiceProvider.GetRequiredService<PhaseStaticCommands.RunWithThrowingHandler>();
        var id = Guid.NewGuid();

        // The handler throw must NOT fail the entry call's response.
        var returned = await run(id);

        Assert.Equal(id, returned);
        Assert.Equal(["thrower", "survivor"], RecordedFor(server, id));
    }

    [Fact]
    public async Task EventsRaisedByAfterCommitHandlers_JoinTheSameResponsesRelayBatch()
    {
        var relayed = new ConcurrentBag<FactoryEventBase>();
        var (client, server, _) = ClientServerContainers.Scopes(
            configureClient: services => services.AddSingleton<IFactoryEventRelay>(new CapturingRelay(relayed)));

        PhaseHandlerRegistrations.EnsureRegistered();
        var run = client.ServiceProvider.GetRequiredService<PhaseStaticCommands.RunRelayChain>();
        var id = Guid.NewGuid();

        await run(id);

        // The relay fires on a background task (production parity) — poll briefly.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline && !relayed.OfType<PhasedRelayOutEvent>().Any(e => e.Id == id))
        {
            await Task.Delay(25);
        }

        Assert.Contains(relayed.OfType<PhasedRelayOutEvent>(), e => e.Id == id);
        // The chain event itself was also raised (by the factory method) and relays too.
        Assert.Contains(relayed.OfType<PhasedRelayChainEvent>(), e => e.Id == id);
    }

    [Fact]
    public async Task ClientRaisedEvent_PhasedHandlerGetsEntrySemantics_NotTheOutsideEntryFallback()
    {
        var (server, client, _) = ClientServerContainers.ScopesWithLogging(out var logger);

        PhaseHandlerRegistrations.EnsureRegistered();
        var events = client.GetRequiredService<IFactoryEvents>();
        var id = Guid.NewGuid();

        await events.Raise(new PhasedUntypedRemoteEvent(id));

        Assert.Equal(["untyped-after-commit"], RecordedFor(server, id));

        // Entry semantics, provable in the logs: the dispatch was QUEUED (9001) during
        // the raise delegate and drained afterward — never the outside-entry immediate
        // fallback (9005) and never the no-scheduler fallback (9004).
        Assert.Contains(logger.Messages, m =>
            m.EventId.Id == 9001 && m.Message.Contains(nameof(PhasedUntypedRemoteEvent)));
        Assert.DoesNotContain(logger.Messages, m =>
            (m.EventId.Id == 9005 || m.EventId.Id == 9004) && m.Message.Contains(nameof(PhasedUntypedRemoteEvent)));
    }

    [Fact]
    public async Task TokenCancelledAfterTheEntryCallSucceeds_DrainStillRuns()
    {
        var trigger = new PhaseCancellationTrigger();
        var (client, server, _) = ClientServerContainers.Scopes(
            configureServer: services => services.AddSingleton(trigger));

        PhaseHandlerRegistrations.EnsureRegistered();
        using var cts = new CancellationTokenSource();
        trigger.OnCancel = cts.Cancel;

        var run = client.ServiceProvider.GetRequiredService<PhaseStaticCommands.RunAndCancel>();
        var id = Guid.NewGuid();

        // The factory method succeeds, then the token goes cancelled. The choke point's
        // post-invoke cancellation check still throws to the caller (pre-existing
        // behavior), but the drain sits BEFORE that check and passes no token — the
        // AfterCommit handler must have run.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run(id, cts.Token));

        Assert.Equal(["cancel-after-commit"], RecordedFor(server, id));
    }

    [Fact]
    public async Task AspForbidException_AfterEnqueueingPhasedWork_ClearsWithoutDraining()
    {
        // The one failure path with a success-shaped return: the choke point converts
        // AspForbidException to an empty RemoteResponseDto. The drain must not ride
        // that shape — the entry failed, the queued work clears.
        var (server, client, _) = ClientServerContainers.Scopes();
        var run = client.ServiceProvider.GetRequiredService<PhaseStaticCommands.RunAspForbid>();
        var id = Guid.NewGuid();

        // The empty-shape response deserializes to default client-side — the
        // pre-existing forbid semantics for value returns. Pinned here so a change to
        // that shape is a conscious one; the load-bearing assertion is the no-drain.
        var returned = await run(id);
        Assert.Equal(Guid.Empty, returned);

        Assert.Empty(RecordedFor(server, id));

        // The clear half (round-2 S1): the forbid exit must release entry state, not
        // just skip the drain. A forbid route that skipped EndEntryCallAsync(false)
        // would leave this long-lived server scope at depth >= 1 and silently kill
        // every subsequent drain — so a follow-on success in the same scope must drain.
        var successId = Guid.NewGuid();
        await client.GetRequiredService<IPhaseEntryTargetFactory>().Create(successId);
        Assert.Equal(["immediate", "create-method-done", "after-commit"], RecordedFor(server, successId));
    }

    [Fact]
    public async Task NestedChildSave_DoesNotDrainAtTheChildsCompletion()
    {
        // The inner-vs-outer discriminator: the parent's Insert raises phased work,
        // saves a child through the child's factory (a nested entry), then records
        // "parent-after-child". A drain firing at ANY inner completion would land
        // "nested-after-commit" between the two markers.
        var (_, _, local) = ClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IPhaseParentTargetFactory>();
        var id = Guid.NewGuid();

        await factory.Save(new PhaseParentTarget { Id = id });

        Assert.Equal(
            ["child-insert-done", "parent-after-child", "nested-after-commit"],
            RecordedFor(local, id));
    }

    [Fact]
    public async Task InterfaceFactory_AsTheOutermostEntry_DrainsOnSuccess()
    {
        // Success-path drain on the interface leg, resolved directly server-side so the
        // interface factory itself is the depth-1 entry (behind the choke point it is
        // always nested).
        var (server, _, _) = ClientServerContainers.Scopes();
        var factory = server.GetRequiredService<IPhaseAuditServiceFactory>();
        var id = Guid.NewGuid();

        await factory.AuditPhased(id);

        Assert.Equal(["interface-method-done", "interface-after-commit"], RecordedFor(server, id));
    }

    [Fact]
    public void SyncFactoryMethod_WithPendingPhasedWork_BlockDrainsAtCompletion()
    {
        // The generated Run route (sync non-Task factory shape) with deferred work
        // actually pending — no-silent-loss at the generated level, not just the
        // FactoryEntryCall.Run unit level.
        var (server, _, _) = ClientServerContainers.Scopes();
        var factory = server.GetRequiredService<IPhaseSyncTargetFactory>();
        var id = Guid.NewGuid();

        factory.Create(id);

        Assert.Equal(["sync-method-done", "sync-after-commit"], RecordedFor(server, id));
    }

    private sealed class CapturingRelay(ConcurrentBag<FactoryEventBase> sink) : IFactoryEventRelay
    {
        public Task Relay(IReadOnlyList<FactoryEventBase> factoryEvents)
        {
            foreach (var evt in factoryEvents)
            {
                sink.Add(evt);
            }

            return Task.CompletedTask;
        }
    }
}
