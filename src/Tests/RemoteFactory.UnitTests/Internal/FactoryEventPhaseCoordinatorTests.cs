using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Covers the consumer-facing drain trigger (PHASE-004): what it drains, when it
/// refuses, and how failures at its in-transaction drain point behave.
/// </summary>
public class FactoryEventPhaseCoordinatorTests
{
    // One event type per test: FactoryEventHandlerRegistry is process-global with
    // (eventType, handlerClass) first-registration-wins dedupe, so sharing an event
    // type across tests silently drops the later registration (PHASE-007 tech debt).
    private sealed record CoordinatorDrainEvent(string Value) : FactoryEventBase;
    private sealed record CoordinatorFailureEvent(string Value) : FactoryEventBase;

    private sealed class DeferredHandler { }
    private sealed class ThrowingHandler { }
    private sealed class SurvivorHandler { }

    private static Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> NoOp
        => (_, _, _, _) => Task.CompletedTask;

    private static (ServiceProvider Provider, IServiceScope Scope) ServerScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(FactoryEventPhaseCoordinatorTests).Assembly);
        var provider = services.BuildServiceProvider();
        return (provider, provider.CreateScope());
    }

    private static IFactoryEventPhaseScheduler NewScheduler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return new FactoryEventPhaseScheduler(services.BuildServiceProvider());
    }

    /// <summary>
    /// The consumer pattern end to end at the unit tier: inside an entry call, the
    /// coordinator resolved from the scope drains the AfterFlush work queued so far — at
    /// the consumer's chosen point, before the entry call completes. Resolving both the
    /// dispatcher's queue and the coordinator from DI also pins that the registration
    /// wires the coordinator to the scope's one real scheduler; a coordinator draining a
    /// twin scheduler would leave this dispatch for the entry sweep and fail the
    /// in-body assertion.
    /// </summary>
    [Fact]
    public async Task DrainAsync_MidEntryCall_DrainsQueuedAfterFlushAtTheCallPoint()
    {
        var dispatched = new List<string>();
        FactoryEventHandlerRegistry.RegisterHandler<CoordinatorDrainEvent>(typeof(DeferredHandler), DispatchPhase.AfterFlush,
            (_, _, _, _) => { dispatched.Add("flush"); return Task.CompletedTask; });

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var sp = scope.ServiceProvider;
            var events = sp.GetRequiredService<IFactoryEvents>();
            var coordinator = sp.GetRequiredService<IFactoryEventPhaseCoordinator>();

            await FactoryEntryCall.RunAsync(sp, async () =>
            {
                await events.Raise(new CoordinatorDrainEvent("x"));
                Assert.Empty(dispatched);

                await coordinator.DrainAsync(DispatchPhase.AfterFlush);

                Assert.Equal(["flush"], dispatched);
                return 0;
            });

            Assert.Equal(["flush"], dispatched);
        }
    }

    /// <summary>
    /// The coordinator's drain point is in-transaction: a handler exception propagates
    /// to the drain caller and aborts the rest of the drain, and when the entry call
    /// then fails, the un-run dispatch is discarded rather than riding into the next
    /// entry call's drain.
    /// </summary>
    [Fact]
    public async Task DrainAsync_HandlerException_PropagatesAndTheFailedEntryDiscardsTheRest()
    {
        var dispatched = new List<string>();
        FactoryEventHandlerRegistry.RegisterHandler<CoordinatorFailureEvent>(typeof(ThrowingHandler), DispatchPhase.AfterFlush,
            (_, _, _, _) => throw new InvalidOperationException("flush handler blew up"));
        FactoryEventHandlerRegistry.RegisterHandler<CoordinatorFailureEvent>(typeof(SurvivorHandler), DispatchPhase.AfterFlush,
            (_, _, _, _) => { dispatched.Add("never runs"); return Task.CompletedTask; });

        var (provider, scope) = ServerScope();
        using (provider)
        using (scope)
        {
            var sp = scope.ServiceProvider;
            var events = sp.GetRequiredService<IFactoryEvents>();
            var coordinator = sp.GetRequiredService<IFactoryEventPhaseCoordinator>();
            var scheduler = sp.GetRequiredService<IFactoryEventPhaseScheduler>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                FactoryEntryCall.RunAsync(sp, async () =>
                {
                    await events.Raise(new CoordinatorFailureEvent("x"));
                    await coordinator.DrainAsync(DispatchPhase.AfterFlush);
                    return 0;
                }));

            Assert.Empty(dispatched);
            Assert.False(scheduler.HasPending);
            Assert.False(scheduler.IsEntryCallActive);
        }
    }

    /// <summary>
    /// Whitelist, not blacklist: the scheduler's drain sweeps every phase at or before
    /// the requested one, so waving through any value but AfterFlush — including
    /// undefined casts — would let a consumer drain the framework-owned AfterCommit
    /// queue inside their transaction.
    /// </summary>
    [Theory]
    [InlineData(DispatchPhase.Immediate)]
    [InlineData(DispatchPhase.AfterCommit)]
    [InlineData((DispatchPhase)99)]
    [InlineData((DispatchPhase)(-1))]
    public async Task DrainAsync_EveryPhaseButAfterFlush_IsRejected(DispatchPhase phase)
    {
        var scheduler = NewScheduler();
        var coordinator = new FactoryEventPhaseCoordinator(scheduler);

        // Even mid-entry-call — the rejection is about the phase, not the timing.
        scheduler.BeginEntryCall();
        try
        {
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => coordinator.DrainAsync(phase));
        }
        finally
        {
            await scheduler.EndEntryCallAsync(success: false);
        }
    }

    /// <summary>
    /// Outside an entry call the coordinator short-circuits rather than delegating:
    /// work sitting in the scope's queues (another flow's, under the documented
    /// per-scope granularity) is left untouched instead of being drained in-transaction
    /// on the wrong caller's token. An unconditional delegate to the scheduler fails
    /// this — the dispatch would run.
    /// </summary>
    [Fact]
    public async Task DrainAsync_OutsideAnyEntryCall_LeavesQueuedWorkUntouched()
    {
        var scheduler = NewScheduler();
        var coordinator = new FactoryEventPhaseCoordinator(scheduler);
        var log = new List<string>();

        scheduler.Enqueue(DispatchPhase.AfterFlush, new CoordinatorDrainEvent("x"), RaiseOptions.None,
            (_, _, _, _) => { log.Add("drained"); return Task.CompletedTask; });

        await coordinator.DrainAsync(DispatchPhase.AfterFlush);

        Assert.Empty(log);
        Assert.True(scheduler.HasPending);
    }

    /// <summary>
    /// The consumer's token reaches the drained handlers — the coordinator's drain is
    /// the one drain point that takes a caller token at all.
    /// </summary>
    [Fact]
    public async Task DrainAsync_PassesTheConsumersTokenToDrainedHandlers()
    {
        var scheduler = NewScheduler();
        var coordinator = new FactoryEventPhaseCoordinator(scheduler);
        using var cts = new CancellationTokenSource();
        CancellationToken observed = default;

        scheduler.BeginEntryCall();
        try
        {
            scheduler.Enqueue(DispatchPhase.AfterFlush, new CoordinatorDrainEvent("x"), RaiseOptions.None,
                (_, _, _, ct) => { observed = ct; return Task.CompletedTask; });

            await coordinator.DrainAsync(DispatchPhase.AfterFlush, cts.Token);
        }
        finally
        {
            await scheduler.EndEntryCallAsync(success: true);
        }

        Assert.Equal(cts.Token, observed);
    }
}
