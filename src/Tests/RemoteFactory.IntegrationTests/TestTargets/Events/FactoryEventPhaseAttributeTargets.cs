using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Events;

// =============================================================================
// PHASE-002 ATTRIBUTE-DECLARED PHASE TARGETS
// =============================================================================
//
// The end-to-end closure of the phase feature: handlers here declare their phase
// through [FactoryEventHandler<T>(DispatchPhase.X)] and register through the
// GENERATED registrar. Nothing in this file or its tests calls
// FactoryEventHandlerRegistry.RegisterHandler.
//
// That distinction is the whole point. PHASE-003's targets register through the
// registry's 3-arg overload by hand (see PhaseHandlerRegistrations), which proved
// the runtime drains a phased handler but said nothing about whether a consumer
// writing the attribute gets one — and until PHASE-002 they did not: the generator
// read the attribute's type argument and never its constructor arguments, so every
// registration landed at Immediate.
//
// Same one-event-type-per-scenario discipline as the PHASE-003 targets: the
// registry is process-global with (eventType, handlerClass) first-registration-wins
// dedupe and no test-isolation hook (PHASE-007), so a shared event type silently
// drops registrations. These two types appear nowhere else.

// -----------------------------------------------------------------------------
// EVENTS
// -----------------------------------------------------------------------------

public record AttributePhasedProjectionEvent(Guid Id) : FactoryEventBase;
public record AttributePhasedAtomicEvent(Guid Id) : FactoryEventBase;

// -----------------------------------------------------------------------------
// HANDLERS — phase declared on the attribute, registered by generated code
// -----------------------------------------------------------------------------

/// <summary>
/// Read-only projection handler: declared <c>AfterCommit</c>, so it must run at the
/// entry-call drain rather than inline at raise time.
/// </summary>
[FactoryEventHandler<AttributePhasedProjectionEvent>(DispatchPhase.AfterCommit)]
public static partial class AttributePhasedProjectionHandler
{
    internal static Task Project(
        AttributePhasedProjectionEvent projectionEvent,
        [Service] IEventTestService testService)
    {
        testService.RecordEventFired("attr-after-commit", projectionEvent.Id);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Atomic handler with no phase argument: the pre-existing contract, dispatched
/// inline at raise time.
/// </summary>
[FactoryEventHandler<AttributePhasedAtomicEvent>]
public static partial class AttributePhasedAtomicHandler
{
    internal static Task Apply(
        AttributePhasedAtomicEvent atomicEvent,
        [Service] IEventTestService testService)
    {
        testService.RecordEventFired("attr-immediate", atomicEvent.Id);
        return Task.CompletedTask;
    }
}

// -----------------------------------------------------------------------------
// FACTORY TARGET
// -----------------------------------------------------------------------------

/// <summary>
/// Raises both events, then records its own completion marker last.
/// </summary>
/// <remarks>
/// The marker is the ordering discriminator, same as PHASE-003's targets: a handler
/// recorded BEFORE "attr-method-done" ran inline at raise time; one recorded AFTER it
/// provably ran at the entry drain. Raising the AfterCommit event FIRST and the
/// Immediate event second means a generator that ignored the phase — registering both
/// at Immediate — would produce the raise order, which is the reverse of what the test
/// asserts. The sequence cannot pass by accident.
/// </remarks>
[Factory]
public partial class PhaseAttributeTarget
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
        await events.Raise(new AttributePhasedProjectionEvent(id), RaiseOptions.None, ct);
        await events.Raise(new AttributePhasedAtomicEvent(id), RaiseOptions.None, ct);
        testService.RecordEventFired("attr-method-done", id);
    }
}
