using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.UnitTests.TestContainers;

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
    private sealed record CoordinatorLateRaiseEvent(string Value) : FactoryEventBase;

    private sealed class DeferredHandler { }
    private sealed class ThrowingHandler { }
    private sealed class SurvivorHandler { }

    private static (ServiceProvider Provider, IServiceScope Scope) ServerScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(FactoryEventPhaseCoordinatorTests).Assembly);
        var provider = services.BuildServiceProvider();
        return (provider, provider.CreateScope());
    }

    private static (ServiceProvider Provider, IServiceScope Scope, CapturingLoggerProvider Logs) ServerScopeWithLogs()
    {
        var services = new ServiceCollection();
        var captured = new CapturingLoggerProvider();
        services.AddLogging(b =>
        {
            b.SetMinimumLevel(LogLevel.Debug);
            b.AddProvider(captured);
        });
        services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(FactoryEventPhaseCoordinatorTests).Assembly);
        var provider = services.BuildServiceProvider();
        return (provider, provider.CreateScope(), captured);
    }

    private static IFactoryEventPhaseScheduler NewScheduler()
        => NewScheduler(out _);

    private static IFactoryEventPhaseScheduler NewScheduler(out CapturingLoggerProvider logs)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var captured = new CapturingLoggerProvider();
        var loggerFactory = LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(LogLevel.Debug);
            b.AddProvider(captured);
        });
        logs = captured;
        return new FactoryEventPhaseScheduler(services.BuildServiceProvider(), loggerFactory);
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

    /// <summary>
    /// Cancelling the consumer's token mid-drain propagates out of the COORDINATOR's
    /// call and leaves the sibling dispatch queued — pinned at this drain point, not
    /// only at the scheduler's post-completion one. Propagation here is structural
    /// today (the in-transaction branch has no catch), but the OCE filter lives
    /// fifteen lines below it; a refactor hoisting that filter would swallow a
    /// consumer's cancellation mid-transaction, and the sibling exception test would
    /// stay green because it throws InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task DrainAsync_ConsumersTokenCancelledMidDrain_PropagatesAndLeavesTheRestQueued()
    {
        var scheduler = NewScheduler();
        var coordinator = new FactoryEventPhaseCoordinator(scheduler);
        var log = new List<string>();
        using var cts = new CancellationTokenSource();

        scheduler.BeginEntryCall();
        try
        {
            scheduler.Enqueue(DispatchPhase.AfterFlush, new CoordinatorDrainEvent("a"), RaiseOptions.None,
                (_, _, _, ct) =>
                {
                    cts.Cancel();
                    ct.ThrowIfCancellationRequested();
                    return Task.CompletedTask;
                });
            scheduler.Enqueue(DispatchPhase.AfterFlush, new CoordinatorDrainEvent("b"), RaiseOptions.None,
                (_, _, _, _) => { log.Add("never runs"); return Task.CompletedTask; });

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => coordinator.DrainAsync(DispatchPhase.AfterFlush, cts.Token));

            Assert.Empty(log);
            Assert.True(scheduler.HasPending);
        }
        finally
        {
            // The consumer's transaction rolls back and the entry fails; the abandoned
            // dispatch is discarded by the exit clear.
            await scheduler.EndEntryCallAsync(success: false);
        }

        Assert.False(scheduler.HasPending);
    }

    /// <summary>
    /// Gate-closure test (test review must-cover): plan review A-V2's case 3, pinned in
    /// the state production actually reaches — a REAL entry call, real dispatcher, real
    /// generated-shape flow. Raise → drain → raise again: the second dispatch was never
    /// drained by the consumer, runs at the entry sweep, and warns exactly once. The
    /// rejected per-entry-call "consumer drained" flag — set while the entry is active,
    /// reset at exit — latches at the drain and silences the second dispatch's warning:
    /// zero 9007s, red here. The bare-scheduler variant of this pin
    /// (FactoryEventPhaseSchedulerTests.DrainAsync_AfterFlushRaisedAfterTheConsumersOwnDrain_StillWarnsAtTheSweep)
    /// cannot catch that design because it never opens an entry call, so the flag never
    /// latches there. Measured by red-proof RP-7.
    /// </summary>
    [Fact]
    public async Task DrainAsync_RaiseAfterTheDrain_InARealEntryCall_SweepsAndWarnsExactlyOnce()
    {
        var dispatched = new List<string>();
        FactoryEventHandlerRegistry.RegisterHandler<CoordinatorLateRaiseEvent>(typeof(DeferredHandler), DispatchPhase.AfterFlush,
            (_, evt, _, _) => { dispatched.Add(((CoordinatorLateRaiseEvent)evt).Value); return Task.CompletedTask; });

        var (provider, scope, logs) = ServerScopeWithLogs();
        using (provider)
        using (scope)
        {
            var sp = scope.ServiceProvider;
            var events = sp.GetRequiredService<IFactoryEvents>();
            var coordinator = sp.GetRequiredService<IFactoryEventPhaseCoordinator>();

            await FactoryEntryCall.RunAsync(sp, async () =>
            {
                await events.Raise(new CoordinatorLateRaiseEvent("drained-by-consumer"));
                await coordinator.DrainAsync(DispatchPhase.AfterFlush);
                Assert.Equal(["drained-by-consumer"], dispatched);

                await events.Raise(new CoordinatorLateRaiseEvent("raised-after-drain"));
                return 0;
            });

            Assert.Equal(["drained-by-consumer", "raised-after-drain"], dispatched);

            // Exactly one warning — for the post-drain raise, not the drained one.
            var warning = Assert.Single(logs.Entries, e => e.EventId == 9007);
            Assert.Equal(nameof(CoordinatorLateRaiseEvent), warning.EventType);
        }
    }

    /// <summary>
    /// Gate-closure test (test review should-cover): overlapping drains. A swept
    /// handler calling the coordinator opens an inner drain while the entry sweep is
    /// still running; work a LATER swept handler then creates must still count as
    /// mid-drain (carve-out, no warning). This is what makes the scheduler's
    /// drain-in-progress state a counter and not a bool — a bool goes false when the
    /// inner drain exits, stamps the later work as not-mid-drain, and warns spuriously.
    /// </summary>
    [Fact]
    public async Task DrainAsync_CoordinatorCalledInsideTheEntrySweep_LaterMidSweepWorkStaysWarningFree()
    {
        var scheduler = NewScheduler(out var logs);
        var coordinator = new FactoryEventPhaseCoordinator(scheduler);
        var log = new List<string>();

        scheduler.BeginEntryCall();

        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoordinatorDrainEvent("a"), RaiseOptions.None,
            async (_, _, _, _) =>
            {
                log.Add("inner-drain");
                // Inner drain overlapping the entry sweep; AfterFlush queue is empty,
                // so this exercises only the drain-in-progress accounting.
                await coordinator.DrainAsync(DispatchPhase.AfterFlush);
            });
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoordinatorDrainEvent("b"), RaiseOptions.None,
            (_, _, _, _) =>
            {
                log.Add("late-enqueue");
                scheduler.Enqueue(DispatchPhase.AfterFlush, new CoordinatorDrainEvent("late"), RaiseOptions.None,
                    (_, _, _, _) => { log.Add("late-flush"); return Task.CompletedTask; });
                return Task.CompletedTask;
            });

        await scheduler.EndEntryCallAsync(success: true);

        Assert.Equal(["inner-drain", "late-enqueue", "late-flush"], log);
        Assert.DoesNotContain(logs.Entries, e => e.EventId == 9007);
    }

    /// <summary>
    /// Validation runs before the outside-entry-call short-circuit: a rejected phase
    /// throws even when no entry call is active. An implementation that short-circuited
    /// first would silently no-op where the XML promises ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public async Task DrainAsync_RejectedPhaseOutsideAnyEntryCall_StillThrows()
    {
        var coordinator = new FactoryEventPhaseCoordinator(NewScheduler());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => coordinator.DrainAsync(DispatchPhase.AfterCommit));
    }
}
