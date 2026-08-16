using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Covers the phase queue and its drain primitive: what gets deferred, what order it
/// drains in, and which drain points propagate handler exceptions versus swallow them.
/// </summary>
public class FactoryEventPhaseSchedulerTests
{
    private sealed record PhaseTestEvent(string Value) : FactoryEventBase;

    private static IFactoryEventPhaseScheduler NewDispatcher()
        => NewDispatcher(out _);

    private static IFactoryEventPhaseScheduler NewDispatcher(out CapturingLoggerProvider logs)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var captured = new CapturingLoggerProvider();
        // Not disposed: the dispatcher holds a logger created from this factory for the
        // lifetime of the test.
        var loggerFactory = LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(LogLevel.Debug);
            b.AddProvider(captured);
        });
        logs = captured;
        return new FactoryEventPhaseScheduler(services.BuildServiceProvider(), loggerFactory);
    }

    // LogEntry / CapturingLoggerProvider extracted to TestContainers (PHASE-004) so
    // entry-call-scoped tests can wire the capture into a real DI container.

    private static Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> Recording(List<string> log, string name)
        => (_, _, _, _) =>
        {
            log.Add(name);
            return Task.CompletedTask;
        };

    private static Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> Throwing(Exception ex)
        => (_, _, _, _) => throw ex;

    [Fact]
    public void Enqueue_DoesNotInvokeHandler()
    {
        var dispatcher = NewDispatcher();
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, Recording(log, "handler"));

        Assert.Empty(log);
        Assert.True(dispatcher.HasPending);
    }

    [Fact]
    public async Task DrainAsync_RunsDeferredDispatchesInFifoOrder()
    {
        var dispatcher = NewDispatcher();
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, Recording(log, "first"));
        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("b"), RaiseOptions.None, Recording(log, "second"));
        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("c"), RaiseOptions.None, Recording(log, "third"));

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["first", "second", "third"], log);
        Assert.False(dispatcher.HasPending);
    }

    [Fact]
    public async Task DrainAsync_OnlyDrainsTheRequestedPhase()
    {
        var dispatcher = NewDispatcher();
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("a"), RaiseOptions.None, Recording(log, "flush"));
        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("b"), RaiseOptions.None, Recording(log, "commit"));

        await dispatcher.DrainAsync(DispatchPhase.AfterFlush, inTransaction: true);

        Assert.Equal(["flush"], log);
        Assert.True(dispatcher.HasPending);

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["flush", "commit"], log);
        Assert.False(dispatcher.HasPending);
    }

    [Fact]
    public async Task DrainAsync_PostCompletion_SwallowsHandlerExceptionAndRunsTheRest()
    {
        var dispatcher = NewDispatcher();
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, Recording(log, "before"));
        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("b"), RaiseOptions.None, Throwing(new InvalidOperationException("read-side blew up")));
        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("c"), RaiseOptions.None, Recording(log, "after"));

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["before", "after"], log);
    }

    /// <summary>
    /// PHASE-004 pre-declared pin amendment: this test previously pinned
    /// "OCE still propagates at a post-completion drain" and now pins the restated
    /// contract — a handler-internal OCE with no live cancelled token is a
    /// post-completion failure like any other: swallowed, logged as 9003, and the
    /// dispatches behind it still run. The old behavior failed a call that had already
    /// succeeded AND silently discarded the rest of the queue — the exact loss AC-3
    /// exists to prevent.
    /// </summary>
    [Fact]
    public async Task DrainAsync_PostCompletion_SwallowsHandlerInternalCancellationAndRunsTheRest()
    {
        var dispatcher = NewDispatcher(out var logs);
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, Throwing(new OperationCanceledException()));
        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("b"), RaiseOptions.None, Recording(log, "behind-the-oce"));

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["behind-the-oce"], log);
        Assert.False(dispatcher.HasPending);
        var failure = Assert.Single(logs.Entries, e => e.EventId == 9003);
        Assert.IsAssignableFrom<OperationCanceledException>(failure.Exception);
    }

    /// <summary>
    /// The half of the restated contract that keeps "OCE still propagates" true where it
    /// means something: the drain's own token going cancelled is genuine cooperative
    /// cancellation, and it aborts the drain — the un-run dispatch stays queued (for the
    /// entry exit's clear), it is not swallowed-and-continued past.
    /// </summary>
    [Fact]
    public async Task DrainAsync_PostCompletion_CooperativeCancellationStillPropagatesAndAbandonsTheRest()
    {
        var dispatcher = NewDispatcher();
        var log = new List<string>();
        using var cts = new CancellationTokenSource();

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, (_, _, _, ct) =>
        {
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });
        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("b"), RaiseOptions.None, Recording(log, "never runs"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false, cts.Token));

        Assert.Empty(log);
        Assert.True(dispatcher.HasPending);
    }

    [Fact]
    public async Task DrainAsync_InTransaction_PropagatesHandlerExceptionAndAbortsRemaining()
    {
        var dispatcher = NewDispatcher();
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("a"), RaiseOptions.None, Throwing(new InvalidOperationException("write-side blew up")));
        dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("b"), RaiseOptions.None, Recording(log, "never runs"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DrainAsync(DispatchPhase.AfterFlush, inTransaction: true));

        Assert.Empty(log);
    }

    [Fact]
    public async Task DrainAsync_SamePhaseHandlersDrainPoint_KeysSemanticsNotThePhase()
    {
        // The same AfterFlush handlers get swallow semantics when drained at a
        // post-completion point (the fail-open path a consumer who never drains hits).
        var dispatcher = NewDispatcher();
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("a"), RaiseOptions.None, Throwing(new InvalidOperationException("boom")));
        dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("b"), RaiseOptions.None, Recording(log, "still runs"));

        await dispatcher.DrainAsync(DispatchPhase.AfterFlush, inTransaction: false);

        Assert.Equal(["still runs"], log);
    }

    [Fact]
    public async Task DrainAsync_ReentrantEnqueueDuringDrain_RunsInTheSameDrain()
    {
        var dispatcher = NewDispatcher();
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, (_, _, _, _) =>
        {
            log.Add("outer");
            // A handler raising an event whose handlers land in the phase being drained.
            dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("nested"), RaiseOptions.None, Recording(log, "nested"));
            return Task.CompletedTask;
        });

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["outer", "nested"], log);
        Assert.False(dispatcher.HasPending);
    }

    [Fact]
    public async Task DrainAsync_NothingDeferred_IsANoOp()
    {
        var dispatcher = NewDispatcher();

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.False(dispatcher.HasPending);
    }

    [Fact]
    public async Task DrainAsync_DeferredDispatchesRunOnce_NotOnASecondDrain()
    {
        var dispatcher = NewDispatcher();
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, Recording(log, "handler"));

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);
        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["handler"], log);
    }

    [Fact]
    public async Task DrainAsync_ReentrantEnqueueIntoAnAlreadyPassedPhase_StillRunsInThisDrain()
    {
        // The AfterCommit drain is the last one a scope gets. A handler running there that
        // raises an event with an AfterFlush handler would otherwise leave that dispatch in
        // a scope nobody drains again.
        var dispatcher = NewDispatcher();
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, (_, _, _, _) =>
        {
            log.Add("commit");
            dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("late"), RaiseOptions.None, Recording(log, "late-flush"));
            return Task.CompletedTask;
        });

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["commit", "late-flush"], log);
        Assert.False(dispatcher.HasPending);
    }

    [Fact]
    public async Task DrainAsync_SweepsAnEarlierPhaseTheConsumerNeverDrained()
    {
        // Fail-open: a consumer who never calls the AfterFlush drain point must not lose
        // those handlers. The AfterCommit drain sweeps them up, earliest phase first, and
        // they take the post-completion drain point's swallow semantics.
        var dispatcher = NewDispatcher(out var logs);
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("a"), RaiseOptions.None, Throwing(new InvalidOperationException("flush-side blew up")));
        dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("b"), RaiseOptions.None, Recording(log, "flush"));
        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("c"), RaiseOptions.None, Recording(log, "commit"));

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["flush", "commit"], log);
        Assert.False(dispatcher.HasPending);

        // The failure is attributed to the phase the dispatch was queued at, not the phase
        // that was requested.
        var failure = Assert.Single(logs.Entries, e => e.EventId == 9003);
        Assert.Equal(DispatchPhase.AfterFlush, failure.Phase);
    }

    /// <summary>
    /// AC-5's warning (PHASE-004): every AfterFlush dispatch the post-completion sweep
    /// picks up warns, per dispatch, naming the event type — including one whose handler
    /// then throws, because the warning is about when the work ran, not whether it
    /// succeeded.
    /// </summary>
    [Fact]
    public async Task DrainAsync_PostCompletionSweepOfUndrainedAfterFlush_WarnsPerDispatchNamingTheEventType()
    {
        var dispatcher = NewDispatcher(out var logs);
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("a"), RaiseOptions.None, Throwing(new InvalidOperationException("boom")));
        dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("b"), RaiseOptions.None, Recording(log, "flush"));

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["flush"], log);
        var warnings = logs.Entries.Where(e => e.EventId == 9007).ToList();
        Assert.Equal(2, warnings.Count);
        Assert.All(warnings, w =>
        {
            Assert.Equal(LogLevel.Warning, w.Level);
            Assert.Equal(nameof(PhaseTestEvent), w.EventType);
        });
    }

    /// <summary>
    /// The documented carve-out stays silent: work a handler creates mid-sweep had no
    /// drain point left to miss, so it runs without the fail-open warning.
    /// </summary>
    [Fact]
    public async Task DrainAsync_AfterFlushWorkCreatedMidSweep_RunsWithoutTheFailOpenWarning()
    {
        var dispatcher = NewDispatcher(out var logs);
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, (_, _, _, _) =>
        {
            log.Add("commit");
            dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("late"), RaiseOptions.None, Recording(log, "late-flush"));
            return Task.CompletedTask;
        });

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["commit", "late-flush"], log);
        Assert.DoesNotContain(logs.Entries, e => e.EventId == 9007);
    }

    /// <summary>
    /// AC-5's letter, third case: the consumer drained, then raised MORE AfterFlush work
    /// from their own code — not from inside any drain. That work was never drained by
    /// the consumer, runs at the sweep, and warns. (A per-entry-call "consumer drained"
    /// flag would silence this case; the per-dispatch discriminator does not — plan
    /// review A-V2.)
    /// </summary>
    [Fact]
    public async Task DrainAsync_AfterFlushRaisedAfterTheConsumersOwnDrain_StillWarnsAtTheSweep()
    {
        var dispatcher = NewDispatcher(out var logs);
        var log = new List<string>();

        // The consumer's drain: covers everything queued so far.
        dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("a"), RaiseOptions.None, Recording(log, "drained-by-consumer"));
        await dispatcher.DrainAsync(DispatchPhase.AfterFlush, inTransaction: true);
        Assert.DoesNotContain(logs.Entries, e => e.EventId == 9007);

        // Raised after the drain, outside any drain loop.
        dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("b"), RaiseOptions.None, Recording(log, "raised-after-drain"));

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["drained-by-consumer", "raised-after-drain"], log);
        var warning = Assert.Single(logs.Entries, e => e.EventId == 9007);
        Assert.Equal(nameof(PhaseTestEvent), warning.EventType);
    }

    /// <summary>
    /// The happy path is silent end to end: work the consumer actually drains produces
    /// no fail-open warning at either drain.
    /// </summary>
    [Fact]
    public async Task DrainAsync_ConsumerDrainedAfterFlush_NeverWarns()
    {
        var dispatcher = NewDispatcher(out var logs);
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("a"), RaiseOptions.None, Recording(log, "flush"));
        await dispatcher.DrainAsync(DispatchPhase.AfterFlush, inTransaction: true);

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["flush"], log);
        Assert.DoesNotContain(logs.Entries, e => e.EventId == 9007);
    }

    [Fact]
    public async Task DrainAsync_MidDrainEarlierPhaseWork_PreemptsRemainingLaterPhaseWork()
    {
        // Pins the ordering choice rather than leaving it accidental: earlier-phase work
        // created mid-drain runs before later-phase dispatches that were already queued.
        var dispatcher = NewDispatcher();
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, (_, _, _, _) =>
        {
            log.Add("commit1");
            dispatcher.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("late"), RaiseOptions.None, Recording(log, "late-flush"));
            return Task.CompletedTask;
        });
        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("b"), RaiseOptions.None, Recording(log, "commit2"));

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["commit1", "late-flush", "commit2"], log);
    }

    [Fact]
    public async Task DrainAsync_DoesNotRunLaterPhasesThanRequested()
    {
        var dispatcher = NewDispatcher();
        var log = new List<string>();

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, Recording(log, "commit"));

        await dispatcher.DrainAsync(DispatchPhase.AfterFlush, inTransaction: true);

        Assert.Empty(log);
        Assert.True(dispatcher.HasPending);
    }

    [Fact]
    public async Task DrainAsync_PostCompletionSwallow_LogsTheDedicatedEventIdWithTheException()
    {
        var dispatcher = NewDispatcher(out var logs);
        var boom = new InvalidOperationException("read-side blew up");

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, Throwing(boom));

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        var failure = Assert.Single(logs.Entries, e => e.EventId == 9003);
        Assert.Equal(LogLevel.Error, failure.Level);
        Assert.Same(boom, failure.Exception);
    }

    [Fact]
    public async Task DrainAsync_HandlerReceivesTheEventAndOptionsItWasQueuedWith()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var scopeProvider = services.BuildServiceProvider();
        var dispatcher = new FactoryEventPhaseScheduler(scopeProvider);

        var seen = new List<(string Value, RaiseOptions Options)>();
        var providers = new List<IServiceProvider>();

        Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> capture =
            (sp, evt, options, _) =>
            {
                seen.Add((((PhaseTestEvent)evt).Value, options));
                providers.Add(sp);
                return Task.CompletedTask;
            };

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("first"), RaiseOptions.None, capture);
        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("second"), RaiseOptions.ServerOnly, capture);

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal([("first", RaiseOptions.None), ("second", RaiseOptions.ServerOnly)], seen);
        // Handlers resolve their [Service] dependencies from the originating scope.
        Assert.All(providers, sp => Assert.Same(scopeProvider, sp));
        Assert.Equal(2, providers.Count);
    }

    [Fact]
    public async Task DrainAsync_HandlerReceivesTheDrainTimeCancellationToken()
    {
        // The token is supplied at drain time, not captured at raise time. PHASE-003 owns
        // the policy question of which token a post-completion drain should pass; this
        // pins the plumbing it will reason from.
        var dispatcher = NewDispatcher();
        using var drainCts = new CancellationTokenSource();
        CancellationToken received = default;

        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, (_, _, _, ct) =>
        {
            received = ct;
            return Task.CompletedTask;
        });

        await dispatcher.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false, drainCts.Token);

        Assert.Equal(drainCts.Token, received);
    }

    [Fact]
    public void Enqueue_NullEvent_Throws()
    {
        var dispatcher = NewDispatcher();

        Assert.Throws<ArgumentNullException>(
            () => dispatcher.Enqueue(DispatchPhase.AfterCommit, null!, RaiseOptions.None, (_, _, _, _) => Task.CompletedTask));
    }

    [Fact]
    public void ScopeDisposedWithoutDraining_RunsNothing()
    {
        // Rollback-discard: a scope whose factory operation failed is never drained. The
        // dispatcher must not run deferred work on the way out.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(FactoryEventPhaseSchedulerTests).Assembly);

        using var provider = services.BuildServiceProvider();
        var log = new List<string>();

        var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IFactoryEventPhaseScheduler>();
        dispatcher.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None, Recording(log, "handler"));
        Assert.True(dispatcher.HasPending);

        scope.Dispose();

        Assert.Empty(log);
    }
}
