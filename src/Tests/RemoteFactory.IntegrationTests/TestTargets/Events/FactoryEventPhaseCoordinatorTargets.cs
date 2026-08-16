using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Events;

// =============================================================================
// PHASE-004 CONSUMER-DRAIN (COORDINATOR) TARGETS
// =============================================================================
//
// The consumer pattern for the AfterFlush drain point, end to end: handlers declare
// AfterFlush through [FactoryEventHandler<T>(DispatchPhase.AfterFlush)] and register
// through the GENERATED registrar; factory methods inject the public
// IFactoryEventPhaseCoordinator via [Service] — the server-only method-injection
// pattern — and drain mid-body, standing in for a consumer's transaction abstraction
// draining between its flush and its commit. Nothing in this file or its tests calls
// FactoryEventHandlerRegistry.RegisterHandler.
//
// Marker discipline (same as the PHASE-002/003 targets): "*-method-done" is recorded
// by the factory method AFTER its drain call, so a handler marker recorded BEFORE it
// provably ran at the consumer's drain point — the one ordering the PHASE-002
// fail-open sweep (which runs after method completion) cannot produce. That ordering
// is what separates a working coordinator from a no-op one.
//
// One event type per scenario: the registry is process-global with
// (eventType, handlerClass) first-registration-wins dedupe (PHASE-007), so a shared
// event type silently drops registrations. These types appear nowhere else.

// -----------------------------------------------------------------------------
// EVENTS
// -----------------------------------------------------------------------------

public record CoordinatorDrainedFlushEvent(Guid Id) : FactoryEventBase;
public record CoordinatorOrderedImmediateEvent(Guid Id) : FactoryEventBase;
public record CoordinatorOrderedFlushEvent(Guid Id) : FactoryEventBase;
public record CoordinatorOrderedCommitEvent(Guid Id) : FactoryEventBase;
public record CoordinatorFailOpenEvent(Guid Id) : FactoryEventBase;

// -----------------------------------------------------------------------------
// HANDLERS — phases declared on the attribute, registered by generated code
// -----------------------------------------------------------------------------

/// <summary>AfterFlush handler drained by the consumer's coordinator call.</summary>
[FactoryEventHandler<CoordinatorDrainedFlushEvent>(DispatchPhase.AfterFlush)]
public static partial class CoordinatorDrainedFlushHandler
{
    internal static Task Project(
        CoordinatorDrainedFlushEvent flushEvent,
        [Service] IEventTestService testService)
    {
        testService.RecordEventFired("coord-flush", flushEvent.Id);
        return Task.CompletedTask;
    }
}

/// <summary>Immediate leg of the three-phase ordering scenario.</summary>
[FactoryEventHandler<CoordinatorOrderedImmediateEvent>]
public static partial class CoordinatorOrderedImmediateHandler
{
    internal static Task Apply(
        CoordinatorOrderedImmediateEvent immediateEvent,
        [Service] IEventTestService testService)
    {
        testService.RecordEventFired("ord-immediate", immediateEvent.Id);
        return Task.CompletedTask;
    }
}

/// <summary>AfterFlush leg of the three-phase ordering scenario.</summary>
[FactoryEventHandler<CoordinatorOrderedFlushEvent>(DispatchPhase.AfterFlush)]
public static partial class CoordinatorOrderedFlushHandler
{
    internal static Task Project(
        CoordinatorOrderedFlushEvent flushEvent,
        [Service] IEventTestService testService)
    {
        testService.RecordEventFired("ord-flush", flushEvent.Id);
        return Task.CompletedTask;
    }
}

/// <summary>AfterCommit leg of the three-phase ordering scenario.</summary>
[FactoryEventHandler<CoordinatorOrderedCommitEvent>(DispatchPhase.AfterCommit)]
public static partial class CoordinatorOrderedCommitHandler
{
    internal static Task Project(
        CoordinatorOrderedCommitEvent commitEvent,
        [Service] IEventTestService testService)
    {
        testService.RecordEventFired("ord-commit", commitEvent.Id);
        return Task.CompletedTask;
    }
}

/// <summary>
/// AfterFlush handler its consumer never drains — the fail-open path. Runs at the
/// entry sweep with the 9007 warning.
/// </summary>
[FactoryEventHandler<CoordinatorFailOpenEvent>(DispatchPhase.AfterFlush)]
public static partial class CoordinatorFailOpenHandler
{
    internal static Task Project(
        CoordinatorFailOpenEvent failOpenEvent,
        [Service] IEventTestService testService)
    {
        testService.RecordEventFired("failopen-flush", failOpenEvent.Id);
        return Task.CompletedTask;
    }
}

// -----------------------------------------------------------------------------
// FACTORY TARGETS
// -----------------------------------------------------------------------------

/// <summary>
/// The consumer pattern: raise, drain via the coordinator, then record the completion
/// marker. Expected sequence ["coord-flush", "coord-method-done"]; a no-op coordinator
/// produces the reverse.
/// </summary>
[Factory]
public partial class CoordinatorDrainTarget
{
    public Guid Id { get; set; }

    [Create]
    [Remote]
    internal async Task Create(
        Guid id,
        [Service] IFactoryEvents events,
        [Service] IFactoryEventPhaseCoordinator coordinator,
        [Service] IEventTestService testService,
        CancellationToken ct)
    {
        Id = id;
        await events.Raise(new CoordinatorDrainedFlushEvent(id), RaiseOptions.None, ct);
        await coordinator.DrainAsync(DispatchPhase.AfterFlush, ct);
        testService.RecordEventFired("coord-method-done", id);
    }
}

/// <summary>
/// Three-phase ordering commands.
/// </summary>
[Factory]
public static partial class CoordinatorOrderingCommands
{
    /// <summary>
    /// Raises the three phases in REVERSE phase order (commit, flush, immediate) so the
    /// expected sequence cannot be produced by raise order: a generator or registration
    /// that ignored phases would replay the raise order, and a no-op coordinator would
    /// push "ord-flush" after "ord-method-done".
    /// </summary>
    /// <remarks>
    /// The second Immediate raise sits AFTER the drain call, pinning the scoped
    /// ordering sentence <c>DispatchPhase</c>'s docs now carry (plan review A-C3):
    /// ordering is anchored per drain point, not a global barrier — code that raises
    /// after its own drain interleaves that later Immediate work between the AfterFlush
    /// and AfterCommit drain points.
    /// </remarks>
    [Execute]
    [Remote]
    internal static async Task<Guid> _RunOrdered(
        Guid id,
        [Service] IFactoryEvents events,
        [Service] IFactoryEventPhaseCoordinator coordinator,
        [Service] IEventTestService testService,
        CancellationToken ct)
    {
        await events.Raise(new CoordinatorOrderedCommitEvent(id), RaiseOptions.None, ct);
        await events.Raise(new CoordinatorOrderedFlushEvent(id), RaiseOptions.None, ct);
        await events.Raise(new CoordinatorOrderedImmediateEvent(id), RaiseOptions.None, ct);
        await coordinator.DrainAsync(DispatchPhase.AfterFlush, ct);
        await events.Raise(new CoordinatorOrderedImmediateEvent(id), RaiseOptions.None, ct);
        testService.RecordEventFired("ord-method-done", id);
        return id;
    }
}

/// <summary>
/// Declares AfterFlush and never drains — the consumer mistake the fail-open warning
/// exists for.
/// </summary>
[Factory]
public partial class CoordinatorFailOpenTarget
{
    public Guid Id { get; set; }

    [Create]
    [Remote]
    internal async Task Create(
        Guid id,
        [Service] IFactoryEvents events,
        [Service] IEventTestService testService,
        CancellationToken ct)
    {
        Id = id;
        await events.Raise(new CoordinatorFailOpenEvent(id), RaiseOptions.None, ct);
        testService.RecordEventFired("failopen-method-done", id);
    }
}
