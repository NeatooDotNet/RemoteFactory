using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Events;

// =============================================================================
// PHASE-006 COALESCING TARGETS
// =============================================================================
//
// End-to-end coverage for the attribute's Coalesce named argument: the flag flows
// attribute → generated registrar → registry → dispatcher → scheduler, with nothing
// here calling FactoryEventHandlerRegistry.RegisterHandler by hand.
//
// Isolation discipline (plan review B-C8): the coalescing and non-coalescing cases
// use DISTINCT handler classes and DISTINCT event types — the registry is
// process-global with (eventType, handlerClass) first-registration-wins dedupe, so a
// shared type would let one case silently measure the other's flag. And the N
// identical raises share ONE per-test Guid: a Guid that varied per raise would make
// the events value-distinct and defeat the collapse being measured.

public record CoalescedRecomputeEvent(Guid Id) : FactoryEventBase;
public record UncoalescedRecomputeEvent(Guid Id) : FactoryEventBase;

/// <summary>Opted in: identical queued dispatches collapse — one run per drain.</summary>
[FactoryEventHandler<CoalescedRecomputeEvent>(DispatchPhase.AfterCommit, Coalesce = true)]
public static partial class CoalescedRecomputeHandler
{
    internal static Task Recompute(
        CoalescedRecomputeEvent recompute,
        [Service] IEventTestService testService)
    {
        testService.RecordEventFired("coalesced-run", recompute.Id);
        return Task.CompletedTask;
    }
}

/// <summary>The backcompat control: no flag — one run per raise, exactly as today.</summary>
[FactoryEventHandler<UncoalescedRecomputeEvent>(DispatchPhase.AfterCommit)]
public static partial class UncoalescedRecomputeHandler
{
    internal static Task Recompute(
        UncoalescedRecomputeEvent recompute,
        [Service] IEventTestService testService)
    {
        testService.RecordEventFired("uncoalesced-run", recompute.Id);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Raises the same value-identical event N times in one entry call — the
/// multiple-recomputes-per-save shape the coalescing flag exists for.
/// </summary>
[Factory]
public static partial class CoalescingCommands
{
    [Execute]
    [Remote]
    internal static async Task<Guid> _RunCoalesced(
        Guid id,
        [Service] IFactoryEvents events,
        CancellationToken ct)
    {
        for (var i = 0; i < 3; i++)
        {
            await events.Raise(new CoalescedRecomputeEvent(id), RaiseOptions.None, ct);
        }

        return id;
    }

    [Execute]
    [Remote]
    internal static async Task<Guid> _RunUncoalesced(
        Guid id,
        [Service] IFactoryEvents events,
        CancellationToken ct)
    {
        for (var i = 0; i < 3; i++)
        {
            await events.Raise(new UncoalescedRecomputeEvent(id), RaiseOptions.None, ct);
        }

        return id;
    }
}
