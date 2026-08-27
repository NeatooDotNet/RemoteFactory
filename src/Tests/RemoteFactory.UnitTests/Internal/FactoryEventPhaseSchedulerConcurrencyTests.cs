using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// The scheduler's shared-scope contract, exercised rather than asserted in a comment.
/// </summary>
/// <remarks>
/// <para>
/// The class documents behavior under concurrent flows, mid-drain enqueues, and a
/// re-entrant <c>Equals</c>, and until PHASE-008 none of it was executed. Both PHASE-006
/// reviewers recommended this file and both warned against the obvious shape: a
/// <c>Task.WhenAll</c> race proves nothing, because a green run may simply mean the
/// interleaving under test never happened, and a red one is not reproducible.
/// </para>
/// <para>
/// So every interleaving here is <b>driven</b>, via <see cref="Rendezvous"/>: a handler
/// parks at a known point, the test does the other flow's work while it is parked, and
/// then releases it. No sleeps, no polling, no thread-count tuning — the schedule is a
/// property of the test, so a failure reproduces and a pass means the window was actually
/// entered.
/// </para>
/// </remarks>
public class FactoryEventPhaseSchedulerConcurrencyTests
{
    private sealed record PhaseTestEvent(string Value) : FactoryEventBase;

    private static IFactoryEventPhaseScheduler NewScheduler() => NewScheduler(out _);

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
    /// A two-party handoff: one side announces it has reached a point and blocks there
    /// until the other side releases it.
    /// </summary>
    /// <remarks>
    /// <c>RunContinuationsAsynchronously</c> on both sides is load-bearing. Without it the
    /// releasing thread runs the parked continuation inline, so the "concurrent" flow's
    /// remaining statements execute on the wrong side of the handoff and the test asserts
    /// against a schedule it did not create.
    /// </remarks>
    private sealed class Rendezvous
    {
        private readonly TaskCompletionSource<bool> _arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Completes once the parked side has reached the rendezvous point.</summary>
        public Task Arrived => _arrived.Task;

        /// <summary>Called from the parked side: announce arrival, then wait to be let go.</summary>
        public Task ArriveAndWaitAsync()
        {
            _arrived.TrySetResult(true);
            return _release.Task;
        }

        /// <summary>Called from the driving side: let the parked side continue.</summary>
        public void Release() => _release.TrySetResult(true);
    }

    private static Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> Recording(List<string> log, string name)
        => (_, _, _, _) =>
        {
            lock (log)
            {
                log.Add(name);
            }

            return Task.CompletedTask;
        };

    // ---------------------------------------------------------------------------------
    // The mid-drain enqueue window (PHASE-003 gate, round 2, finding N1)
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Work a second flow enqueues while the surviving flow's outermost drain is running
    /// joins that drain rather than being discarded.
    /// </summary>
    /// <remarks>
    /// PHASE-003's round-2 gate recorded this window and left it unpinned, describing the
    /// outcome as timing-dependent: the work "either joins that drain or is discarded by
    /// the post-drain clear." Driven rather than raced, the reachable branch is decidable —
    /// <c>DrainAsync</c> loops until <c>TryDequeueThrough</c> comes back empty, so anything
    /// enqueued while a handler is still running is picked up by the same loop.
    /// <para>
    /// The entry call stays active for the whole drain (depth is released only in
    /// <c>EndEntryCallAsync</c>'s finally), which is what makes the second flow's enqueue a
    /// normal queue operation rather than an out-of-band one.
    /// </para>
    /// <para>
    /// <b>The other branch is deliberately not pinned, and is not an omission.</b> Discard
    /// requires enqueueing after the drain loop has observed an empty queue but before
    /// <c>ClearAtExit</c> takes the lock — a window with no seam a test can reach from
    /// outside the class, since both steps happen inside one <c>EndEntryCallAsync</c> call.
    /// Recorded here as unreachable-from-outside rather than left looking untested.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task MidDrainEnqueueFromAnotherFlow_JoinsTheRunningDrain()
    {
        var scheduler = NewScheduler();
        var log = new List<string>();
        var rendezvous = new Rendezvous();

        scheduler.BeginEntryCall();

        // The parked handler: the drain stops here and stays stopped until this test says so.
        scheduler.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None,
            async (_, _, _, _) =>
            {
                lock (log)
                {
                    log.Add("first");
                }

                await rendezvous.ArriveAndWaitAsync();
            });

        // Not awaited yet: this is the flow whose drain we want to catch mid-flight.
        var exit = scheduler.EndEntryCallAsync(true);

        await rendezvous.Arrived;

        // The drain is now provably in flight, with a handler parked inside it. This is the
        // second flow's raise — the one PHASE-003 could only describe.
        scheduler.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("b"), RaiseOptions.None, Recording(log, "second-flow"));

        rendezvous.Release();
        await exit;

        Assert.Equal(["first", "second-flow"], log);

        // And the drain left nothing behind for the exit clear to discard.
        Assert.False(scheduler.HasPending);
    }

    /// <summary>
    /// The same window, one phase earlier: work enqueued mid-drain into a phase whose drain
    /// point has already passed still joins the running drain.
    /// </summary>
    /// <remarks>
    /// Distinct from the test above because it exercises the sweep rather than the
    /// same-phase loop — <c>TryDequeueThrough</c> reaches back to earlier phases precisely
    /// so work created after their drain point is not stranded in a scope nobody drains
    /// again. Under a concurrent second flow that is the difference between a projection
    /// running late and never running at all.
    /// </remarks>
    [Fact]
    public async Task MidDrainEnqueueIntoAnAlreadyPassedPhase_StillJoinsTheRunningDrain()
    {
        var scheduler = NewScheduler();
        var log = new List<string>();
        var rendezvous = new Rendezvous();

        scheduler.BeginEntryCall();

        scheduler.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("a"), RaiseOptions.None,
            async (_, _, _, _) =>
            {
                lock (log)
                {
                    log.Add("commit-handler");
                }

                await rendezvous.ArriveAndWaitAsync();
            });

        var exit = scheduler.EndEntryCallAsync(true);
        await rendezvous.Arrived;

        // AfterFlush sorts BEFORE AfterCommit, so this lands in a phase the sweep has
        // already walked past.
        scheduler.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("b"), RaiseOptions.None, Recording(log, "late-flush"));

        rendezvous.Release();
        await exit;

        Assert.Equal(["commit-handler", "late-flush"], log);
        Assert.False(scheduler.HasPending);
    }

    /// <summary>
    /// A consumer drain overlapping the entry drain does not release the mid-drain mark
    /// early, so work created inside the overlap keeps its 9007 carve-out.
    /// </summary>
    /// <remarks>
    /// <c>_activeDrains</c> is a counter rather than a bool for exactly this shape, and the
    /// reason is stated in the field's comment. Driven here instead of asserted: the inner
    /// drain completes while the outer one is still parked, and the raise that follows must
    /// still be treated as created mid-drain — silent, not warned — because every drain
    /// point it could have used had already passed.
    /// </remarks>
    [Fact]
    public async Task ConsumerDrainNestedInsideTheEntryDrain_DoesNotClearTheMidDrainMarkEarly()
    {
        var scheduler = NewScheduler(out var logs);
        var log = new List<string>();
        var rendezvous = new Rendezvous();

        scheduler.BeginEntryCall();

        scheduler.Enqueue(DispatchPhase.AfterCommit, new PhaseTestEvent("outer"), RaiseOptions.None,
            async (_, _, _, _) =>
            {
                // A nested drain that finds nothing and completes, decrementing the counter
                // by one. If the mark were a bool it would now read "no drain in flight."
                await scheduler.DrainAsync(DispatchPhase.AfterFlush, inTransaction: true);
                await rendezvous.ArriveAndWaitAsync();
            });

        var exit = scheduler.EndEntryCallAsync(true);
        await rendezvous.Arrived;

        // Created while the OUTER drain is still running, after the inner one finished.
        scheduler.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent("mid"), RaiseOptions.None, Recording(log, "created-mid-drain"));

        rendezvous.Release();
        await exit;

        Assert.Equal(["created-mid-drain"], log);

        // The carve-out held: mid-drain creation is silent, never a fail-open warning.
        Assert.DoesNotContain(logs.Entries, e => e.EventId == 9007);
    }

    // ---------------------------------------------------------------------------------
    // Re-entrant consumer Equals under _gate (PHASE-006 code review C4, PHASE-007 gate)
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// An event whose <c>Equals</c> re-enters the scheduler — the hazard the <c>_gate</c>
    /// comment names and does not guard.
    /// </summary>
    /// <remarks>
    /// The coalescing identity scan invokes consumer <c>Equals</c> while holding the lock,
    /// and <c>lock</c> is re-entrant on the same thread, so the lock does not stop this.
    /// The callback fires on the PENDING event, which is the receiver the scan uses.
    /// </remarks>
    private sealed record ReentrantEqualsEvent : FactoryEventBase
    {
        private readonly string _key;

        public ReentrantEqualsEvent(string key) => _key = key;

        /// <summary>Runs inside <c>Equals</c>, i.e. inside the scheduler's lock and mid-scan.</summary>
        public Action? OnEquals { get; set; }

        /// <summary>
        /// The strongly-typed half, which is the customization point a record actually
        /// offers: <c>Equals(object?)</c> and <c>Equals(FactoryEventBase?)</c> are both
        /// compiler-synthesized and cannot be declared, and the synthesized chain lands
        /// here. The scheduler's scan calls <c>existing.Event.Equals(factoryEvent)</c>
        /// against two <c>FactoryEventBase</c> references, so it enters that chain and
        /// arrives at this method.
        /// </summary>
        public bool Equals(ReentrantEqualsEvent? other)
        {
            OnEquals?.Invoke();
            return other is not null && other._key == _key;
        }

        public override int GetHashCode() => _key.GetHashCode(StringComparison.Ordinal);
    }

    /// <summary>
    /// A re-entrant <c>Equals</c> that appends during the identity scan does not corrupt the
    /// queue: the appended entry is simply not a collapse candidate for the raise in flight.
    /// </summary>
    /// <remarks>
    /// The scan captures its span and its length before iterating, so an entry appended
    /// underneath it is outside the loop bound — stated in the <c>_gate</c> comment as
    /// unguarded, pinned here as the actual behavior. The consequence is a missed collapse,
    /// never a wrong dispatch or a lost one, and that distinction is the whole point of
    /// pinning it: it bounds how bad a pathological consumer <c>Equals</c> can be.
    /// <para>
    /// The append is sized to force the backing list to GROW mid-scan, so the span the scan
    /// holds is left over a stale array. PHASE-007's code review traced that as safe — the
    /// old array stays alive and GC-tracked, the values in it are still the ones that were
    /// copied, and <c>Replace</c> resolves its index against the live list rather than the
    /// span — and this is that trace made executable rather than believed.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ReentrantEqualsThatAppendsMidScan_MissesTheCollapseButKeepsTheQueueIntact()
    {
        var scheduler = NewScheduler();
        var log = new List<string>();

        Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handler = Recording(log, "handler");

        var pending = new ReentrantEqualsEvent("same");
        scheduler.Enqueue(DispatchPhase.AfterFlush, pending, RaiseOptions.None, handler, coalesce: true);

        // Grow past List<T>'s default capacity of 4 from inside the scan, so the span the
        // scan is holding is over the pre-growth array.
        var appended = 0;
        pending.OnEquals = () =>
        {
            if (appended > 0)
            {
                return;
            }

            for (var i = 0; i < 8; i++)
            {
                appended++;
                scheduler.Enqueue(DispatchPhase.AfterFlush, new PhaseTestEvent($"appended-{i}"), RaiseOptions.None, Recording(log, $"appended-{i}"));
            }
        };

        // Triggers the scan, which calls pending.Equals(incoming) and appends underneath it.
        scheduler.Enqueue(DispatchPhase.AfterFlush, new ReentrantEqualsEvent("same"), RaiseOptions.None, handler, coalesce: true);

        await scheduler.DrainAsync(DispatchPhase.AfterFlush, inTransaction: true);

        // The collapse still happened for the raise in flight — the survivor ran once, not
        // twice — and every appended dispatch survived in order behind it.
        Assert.Equal(
            ["handler", "appended-0", "appended-1", "appended-2", "appended-3", "appended-4", "appended-5", "appended-6", "appended-7"],
            log);

        Assert.False(scheduler.HasPending);
    }

    /// <summary>
    /// A re-entrant <c>Equals</c> that enqueues an entry <i>identical</i> to the one being
    /// raised still ends with one pending dispatch: the coalescing contract survives
    /// re-entrancy.
    /// </summary>
    /// <remarks>
    /// <b>This test was written to assert the opposite and the run disproved it</b>
    /// (PHASE-008 RP-5). The reasoning that failed: "the scan captured its bound before the
    /// appended entry existed, so the appended copy escapes the collapse and one identity
    /// holds two pending dispatches." The missing step is that the re-entrant
    /// <c>Enqueue</c> is a complete <c>Enqueue</c> — it runs its OWN identity scan against
    /// the live queue, finds the same pending entry the outer scan is standing on, and
    /// collapses into it. The stale bound only ever hides entries from the scan that is
    /// already running, never from the next one.
    /// <para>
    /// So the attribute's "at most one pending dispatch per identity" promise holds even
    /// under a pathological consumer <c>Equals</c>, and what escapes the captured bound is
    /// limited to <i>non-identical</i> appends — a missed collapse opportunity, which the
    /// test above pins. That is a materially better answer than the one this test was
    /// drafted to record, and it is the reason the re-entrancy hazard stayed a documented
    /// caution rather than becoming a guard.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task ReentrantEqualsThatEnqueuesAnIdenticalEntry_StillCollapsesToOnePendingDispatch()
    {
        var scheduler = NewScheduler();
        var log = new List<string>();

        Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handler = Recording(log, "handler");

        var pending = new ReentrantEqualsEvent("same");
        scheduler.Enqueue(DispatchPhase.AfterFlush, pending, RaiseOptions.None, handler, coalesce: true);

        var appended = false;
        pending.OnEquals = () =>
        {
            if (appended)
            {
                return;
            }

            appended = true;

            // Same identity, same handler, same options. This re-entrant call runs its own
            // full identity scan against the live queue — which is the step the original
            // prediction missed — finds `pending`, and collapses into it.
            scheduler.Enqueue(DispatchPhase.AfterFlush, new ReentrantEqualsEvent("same"), RaiseOptions.None, handler, coalesce: true);
        };

        scheduler.Enqueue(DispatchPhase.AfterFlush, new ReentrantEqualsEvent("same"), RaiseOptions.None, handler, coalesce: true);

        await scheduler.DrainAsync(DispatchPhase.AfterFlush, inTransaction: true);

        // One dispatch for one coalescing identity: three raises, two of them re-entrant,
        // and the contract holds.
        Assert.Equal(["handler"], log);
        Assert.False(scheduler.HasPending);
    }

    // ---------------------------------------------------------------------------------
    // Registry isolation (PHASE-001 routing; PHASE-006 dependents; PHASE-007 gate)
    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Two test classes registering the same handler class for DIFFERENT event types do not
    /// interfere — the property the suite's isolation discipline actually rests on.
    /// </summary>
    /// <remarks>
    /// <b>This replaced a test that called <c>FactoryEventHandlerRegistry.Clear()</c>, and
    /// the replacement is the finding</b> (PHASE-008 RP-6). The routed item asked for the
    /// isolation discipline to become "enforceable from the test infrastructure rather than
    /// resident only in prose," and the obvious reading was to pin the <c>Clear()</c> escape
    /// hatch the discipline notes point at. Written, it turned
    /// <c>FactoryEntryCallTests.DrainedHandlerInvokingAFactory_NestsWithoutDrainingOrClearingTheDrainInProgress</c>
    /// red — that test passes alone and fails beside this one, because the registry is
    /// process-wide static and xUnit runs test classes in parallel.
    /// <para>
    /// So <c>Clear()</c> is not an escape hatch any test in this suite may use: calling it
    /// strips registrations out from under whatever else is mid-run. The discipline that
    /// works is the one the suite already follows — every test invents event types nobody
    /// else uses — and the dedupe key <c>(event type, handler class)</c> is what makes that
    /// sufficient. That is what this pins, and the correction is written onto
    /// <c>Clear()</c>'s own XML doc so the next author meets it at the method rather than in
    /// a todo file.
    /// </para>
    /// </remarks>
    [Fact]
    public void RegistryEntriesAreKeyedByEventType_SoPerTestEventTypesAreSufficientIsolation()
    {
        var log = new List<string>();

        FactoryEventHandlerRegistry.RegisterHandler<RegistryIsolationProbeEventA>(
            typeof(FactoryEventPhaseSchedulerConcurrencyTests),
            DispatchPhase.AfterCommit,
            Recording(log, "a"));

        FactoryEventHandlerRegistry.RegisterHandler<RegistryIsolationProbeEventB>(
            typeof(FactoryEventPhaseSchedulerConcurrencyTests),
            DispatchPhase.AfterCommit,
            Recording(log, "b"));

        // Same handler class, two event types: each keyed separately, neither displacing
        // the other. A shared-key registry would have let the second registration win.
        var forA = FactoryEventHandlerRegistry.GetHandlers(typeof(RegistryIsolationProbeEventA));
        var forB = FactoryEventHandlerRegistry.GetHandlers(typeof(RegistryIsolationProbeEventB));

        Assert.NotNull(forA);
        Assert.NotNull(forB);
        Assert.Single(forA);
        Assert.Single(forB);

        // And an event type no test registered resolves to nothing rather than to a
        // neighbour's handlers — the property that makes "invent your own event type"
        // sufficient isolation without any teardown at all.
        Assert.Null(FactoryEventHandlerRegistry.GetHandlers(typeof(RegistryIsolationProbeEventNeverRegistered)));
    }

    private sealed record RegistryIsolationProbeEventA : FactoryEventBase;

    private sealed record RegistryIsolationProbeEventB : FactoryEventBase;

    private sealed record RegistryIsolationProbeEventNeverRegistered : FactoryEventBase;
}
