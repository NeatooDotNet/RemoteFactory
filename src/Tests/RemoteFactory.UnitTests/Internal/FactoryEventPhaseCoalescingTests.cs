using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Covers PHASE-006's opt-in coalescing: the scheduler's pending-queue collapse (identity
/// key, warn-preserving merge, counts), and the registry's per-entry flag with its
/// first-wins survivor rule.
/// </summary>
public class FactoryEventPhaseCoalescingTests
{
    private sealed record CoalesceTestEvent(string Value) : FactoryEventBase;

    private static FactoryEventPhaseScheduler NewScheduler(out CapturingLoggerProvider logs)
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

    private static Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> Recording(List<string> log, string name)
        => (_, _, _, _) =>
        {
            log.Add(name);
            return Task.CompletedTask;
        };

    // ------------------------------------------------------------------
    // Collapse and the identity key
    // ------------------------------------------------------------------

    [Fact]
    public async Task Coalesce_IdenticalPendingRaises_RunOnceAtTheDrain()
    {
        var scheduler = NewScheduler(out var logs);
        var log = new List<string>();
        var handler = Recording(log, "projection");
        var evt = new CoalesceTestEvent("same");

        scheduler.Enqueue(DispatchPhase.AfterCommit, evt, RaiseOptions.None, handler, coalesce: true);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, handler, coalesce: true);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, handler, coalesce: true);

        await scheduler.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["projection"], log);

        // The first raise queues (9001); the two collapses announce themselves (9008)
        // instead of double-logging 9001.
        Assert.Equal(1, logs.Entries.Count(e => e.EventId == 9001));
        Assert.Equal(2, logs.Entries.Count(e => e.EventId == 9008));
    }

    [Fact]
    public async Task Coalesce_ValueDistinctEvents_DoNotCollapse()
    {
        var scheduler = NewScheduler(out _);
        var log = new List<string>();
        var handler = Recording(log, "run");

        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("a"), RaiseOptions.None, handler, coalesce: true);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("b"), RaiseOptions.None, handler, coalesce: true);

        await scheduler.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["run", "run"], log);
    }

    [Fact]
    public async Task Coalesce_DistinctHandlerDelegates_DoNotCollapse()
    {
        var scheduler = NewScheduler(out _);
        var log = new List<string>();

        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, Recording(log, "first"), coalesce: true);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, Recording(log, "second"), coalesce: true);

        await scheduler.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["first", "second"], log);
    }

    [Fact]
    public async Task Coalesce_DistinctRaiseOptions_DoNotCollapse()
    {
        var scheduler = NewScheduler(out _);
        var log = new List<string>();
        var handler = Recording(log, "run");

        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, handler, coalesce: true);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.ServerOnly, handler, coalesce: true);

        await scheduler.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["run", "run"], log);
    }

    [Fact]
    public async Task Coalesce_SameIdentityAtDistinctPhases_DoesNotCollapseAcrossQueues()
    {
        var scheduler = NewScheduler(out _);
        var log = new List<string>();
        var handler = Recording(log, "run");
        var evt = new CoalesceTestEvent("same");

        scheduler.Enqueue(DispatchPhase.AfterFlush, evt, RaiseOptions.None, handler, coalesce: true);
        scheduler.Enqueue(DispatchPhase.AfterCommit, evt, RaiseOptions.None, handler, coalesce: true);

        await scheduler.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["run", "run"], log);
    }

    [Fact]
    public async Task NoCoalesce_IdenticalRaises_StillRunOncePerRaise()
    {
        // Backcompat at this tier: the flagless path (both overload shapes) is untouched.
        var scheduler = NewScheduler(out _);
        var log = new List<string>();
        var handler = Recording(log, "run");

        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, handler);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, handler, coalesce: false);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, handler, coalesce: false);

        await scheduler.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["run", "run", "run"], log);
    }

    [Fact]
    public async Task Coalesce_SurvivorKeepsTheEarliestQueuePosition()
    {
        var scheduler = NewScheduler(out _);
        var log = new List<string>();
        var coalescing = Recording(log, "first-raised");

        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, coalescing, coalesce: true);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("other"), RaiseOptions.None, Recording(log, "second-raised"), coalesce: false);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, coalescing, coalesce: true);

        await scheduler.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        // The duplicate collapsed into the FIRST pending dispatch — it did not re-queue
        // behind "second-raised".
        Assert.Equal(["first-raised", "second-raised"], log);
    }

    /// <summary>
    /// An event whose custom <c>Equals</c> compares only the Id — the documented
    /// over-collapse hazard, used here to make the survivor observable.
    /// </summary>
    private sealed record IdOnlyEvent(int Id, string Payload) : FactoryEventBase
    {
        public bool Equals(IdOnlyEvent? other) => other is not null && other.Id == this.Id;
        public override int GetHashCode() => this.Id;
    }

    /// <summary>
    /// Which collapsed instance the handler receives: the FIRST-raised one. Under the
    /// recommended value-only shape this is unobservable; under a custom-Equals
    /// over-collapse it is exactly the payload the consumer sees, so the choice is
    /// contract — a silent switch to latest-wins must turn this red.
    /// </summary>
    [Fact]
    public async Task Coalesce_CustomEqualsCollapse_TheHandlerReceivesTheFirstRaisedInstance()
    {
        var scheduler = NewScheduler(out _);
        var payloads = new List<string>();
        Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handler =
            (_, evt, _, _) =>
            {
                payloads.Add(((IdOnlyEvent)evt).Payload);
                return Task.CompletedTask;
            };

        scheduler.Enqueue(DispatchPhase.AfterCommit, new IdOnlyEvent(1, "first"), RaiseOptions.None, handler, coalesce: true);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new IdOnlyEvent(1, "second"), RaiseOptions.None, handler, coalesce: true);

        await scheduler.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["first"], payloads);
    }

    /// <summary>
    /// The documented no-op hazard, executable (plan review B-V3): a reference-typed
    /// event member defeats the synthesized structural equality, so value-equal-looking
    /// raises stay distinct and coalescing silently does nothing. Four doc surfaces
    /// state this; a later "improvement" to a structural/deep comparer flips it and
    /// must turn this red.
    /// </summary>
    private sealed record ListPayloadEvent(List<int> Items) : FactoryEventBase;

    [Fact]
    public async Task Coalesce_ReferenceTypedMember_DefeatsEqualityAndDoesNotCollapse()
    {
        var scheduler = NewScheduler(out _);
        var log = new List<string>();
        var handler = Recording(log, "run");

        scheduler.Enqueue(DispatchPhase.AfterCommit, new ListPayloadEvent([1, 2]), RaiseOptions.None, handler, coalesce: true);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new ListPayloadEvent([1, 2]), RaiseOptions.None, handler, coalesce: true);

        await scheduler.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["run", "run"], log);
    }

    // ------------------------------------------------------------------
    // Pending-only identity
    // ------------------------------------------------------------------

    [Fact]
    public async Task Coalesce_RaiseAfterTheDispatchWasTakenByADrain_StartsAFreshDispatch()
    {
        // Identity looks only at PENDING work. A dispatch a running drain has already
        // taken is history — an identical raise from inside the drain starts a fresh
        // pending dispatch, which drain-until-empty then runs in the same drain.
        var scheduler = NewScheduler(out _);
        var log = new List<string>();
        var evt = new CoalesceTestEvent("same");

        Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task>? projection = null;
        var reRaised = false;
        projection = (_, _, _, _) =>
        {
            log.Add("projection");
            if (!reRaised)
            {
                reRaised = true;
                scheduler.Enqueue(DispatchPhase.AfterCommit, evt, RaiseOptions.None, projection!, coalesce: true);
            }
            return Task.CompletedTask;
        };

        scheduler.Enqueue(DispatchPhase.AfterCommit, evt, RaiseOptions.None, projection, coalesce: true);

        await scheduler.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["projection", "projection"], log);
    }

    // ------------------------------------------------------------------
    // The warn-preserving merge (plan review B-V1)
    // ------------------------------------------------------------------

    /// <summary>
    /// A pre-drain AfterFlush raise (never drained by anyone — owed a 9007) collapses
    /// with an identical mid-drain re-raise (the carve-out — silent on its own). The
    /// survivor must keep the warning obligation: exactly one 9007 fires at the
    /// post-completion sweep. A latest-bit-wins merge erases it — that sabotage turns
    /// this red (red-proofed at the gate).
    /// </summary>
    [Fact]
    public async Task Coalesce_PreDrainAndMidDrainRaisesCollapse_TheSurvivorStillWarns9007()
    {
        var scheduler = NewScheduler(out var logs);
        var log = new List<string>();
        var evt = new CoalesceTestEvent("same");
        var flushProjection = Recording(log, "flush-projection");

        // An earlier-phase dispatch that re-raises the identical AfterFlush work while
        // the sweep is in flight (mid-drain, EnqueuedMidDrain = true).
        scheduler.Enqueue(
            DispatchPhase.Immediate,
            new CoalesceTestEvent("trigger"),
            RaiseOptions.None,
            (_, _, _, _) =>
            {
                scheduler.Enqueue(DispatchPhase.AfterFlush, evt, RaiseOptions.None, flushProjection, coalesce: true);
                return Task.CompletedTask;
            });

        // The pre-drain raise: EnqueuedMidDrain = false — this one is owed the warning.
        scheduler.Enqueue(DispatchPhase.AfterFlush, evt, RaiseOptions.None, flushProjection, coalesce: true);

        await scheduler.DrainAsync(DispatchPhase.AfterCommit, inTransaction: false);

        Assert.Equal(["flush-projection"], log);
        Assert.Equal(1, logs.Entries.Count(e => e.EventId == 9007));
    }

    // ------------------------------------------------------------------
    // Discard on failure — the collapsed count is the discriminator
    // ------------------------------------------------------------------

    [Fact]
    public async Task EntryCallFails_CoalescedRaisesWereOnePendingDispatch_9006ReportsTheCollapsedCount()
    {
        var scheduler = NewScheduler(out var logs);
        var log = new List<string>();
        var handler = Recording(log, "never");

        scheduler.BeginEntryCall();
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, handler, coalesce: true);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, handler, coalesce: true);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, handler, coalesce: true);
        await scheduler.EndEntryCallAsync(success: false);

        Assert.Empty(log);
        Assert.False(scheduler.HasPending);

        // Three raises were ONE pending dispatch — a coalescing implementation that does
        // nothing discards 3 here, which is what makes this leg falsifiable.
        var discarded = Assert.Single(logs.Entries, e => e.EventId == 9006);
        Assert.Equal("Discarded 1 deferred handler dispatch(es) at entry-call exit without running them.", discarded.Message);
    }

    [Fact]
    public async Task EntryCallFails_NonCoalescingSibling_9006ReportsOnePerRaise()
    {
        // The paired control for the collapsed-count pin above.
        var scheduler = NewScheduler(out var logs);
        var log = new List<string>();
        var handler = Recording(log, "never");

        scheduler.BeginEntryCall();
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, handler, coalesce: false);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, handler, coalesce: false);
        scheduler.Enqueue(DispatchPhase.AfterCommit, new CoalesceTestEvent("same"), RaiseOptions.None, handler, coalesce: false);
        await scheduler.EndEntryCallAsync(success: false);

        Assert.Empty(log);
        var discarded = Assert.Single(logs.Entries, e => e.EventId == 9006);
        Assert.Equal("Discarded 3 deferred handler dispatch(es) at entry-call exit without running them.", discarded.Message);
    }

    // ------------------------------------------------------------------
    // Registry: the per-entry flag and the first-wins survivor
    // ------------------------------------------------------------------

    private sealed record RegistryCoalesceEventA(string Value) : FactoryEventBase;
    private sealed record RegistryCoalesceEventB(string Value) : FactoryEventBase;
    private sealed record RegistryCoalesceEventC(string Value) : FactoryEventBase;

    private sealed class RegistryHandlerOne { }
    private sealed class RegistryHandlerTwo { }

    private static Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> NoOp
        => (_, _, _, _) => Task.CompletedTask;

    [Fact]
    public void RegisterHandler_CoalesceFlag_RoundTripsThroughGetHandlers()
    {
        FactoryEventHandlerRegistry.RegisterHandler<RegistryCoalesceEventA>(typeof(RegistryHandlerOne), DispatchPhase.AfterFlush, coalesce: true, NoOp);
        FactoryEventHandlerRegistry.RegisterHandler<RegistryCoalesceEventA>(typeof(RegistryHandlerTwo), DispatchPhase.AfterFlush, coalesce: false, NoOp);

        var handlers = FactoryEventHandlerRegistry.GetHandlers(typeof(RegistryCoalesceEventA));

        Assert.NotNull(handlers);
        Assert.Equal(2, handlers.Count);
        Assert.Contains(handlers, h => h.Coalesce);
        Assert.Contains(handlers, h => !h.Coalesce);
    }

    [Fact]
    public void RegisterHandler_ExistingOverloads_DefaultTheFlagOff()
    {
        FactoryEventHandlerRegistry.RegisterHandler<RegistryCoalesceEventB>(typeof(RegistryHandlerOne), NoOp);
        FactoryEventHandlerRegistry.RegisterHandler<RegistryCoalesceEventB>(typeof(RegistryHandlerTwo), DispatchPhase.AfterCommit, NoOp);

        var handlers = FactoryEventHandlerRegistry.GetHandlers(typeof(RegistryCoalesceEventB));

        Assert.NotNull(handlers);
        Assert.Equal(2, handlers.Count);
        Assert.All(handlers, h => Assert.False(h.Coalesce));
    }

    [Fact]
    public void RegisterHandler_SameHandlerClassTwice_KeepsTheFirstFlag()
    {
        // The (event type, handler class) first-wins dedupe extends to the flag: the
        // surviving declaration's registration — phase AND coalesce — is the one that
        // stands, for the life of the process.
        FactoryEventHandlerRegistry.RegisterHandler<RegistryCoalesceEventC>(typeof(RegistryHandlerOne), DispatchPhase.AfterFlush, coalesce: true, NoOp);
        FactoryEventHandlerRegistry.RegisterHandler<RegistryCoalesceEventC>(typeof(RegistryHandlerOne), DispatchPhase.AfterFlush, coalesce: false, NoOp);

        var handlers = FactoryEventHandlerRegistry.GetHandlers(typeof(RegistryCoalesceEventC));

        Assert.NotNull(handlers);
        var entry = Assert.Single(handlers);
        Assert.True(entry.Coalesce);
    }
}
