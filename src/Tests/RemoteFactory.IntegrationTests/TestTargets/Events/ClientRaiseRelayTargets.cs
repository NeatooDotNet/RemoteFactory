using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Events;

// =============================================================================
// CLIENT-INITIATED RAISE → RELAY TARGETS
// =============================================================================
//
// Targets for the client-raise relay path: the client calls IFactoryEvents.Raise,
// RemoteFactoryEvents ships it to the server as a RaiseFactoryEventRemote request,
// and the batch the server collects comes back to the client's IFactoryEventRelay.
//
// The distinction from FactoryEventRelayTargets is the TRIGGER, not the plumbing.
// There, a [Remote] factory method raises server-side and the relay rides its
// response. Here there is no factory method at all — the raise itself is the
// round-trip. Both end up in HandleRemoteDelegateRequest's collector read, which
// does not branch on which delegate ran.
//
// The client's own event is in the batch it gets back. That is not an accident of
// the transport: RemoteFactoryEvents.Raise does no local dispatch, so the client's
// relay has never seen the event, and relaying it is the only way client-side
// handlers observe it. RaiseOptions.ServerOnly is the opt-out.
//
// One event type per scenario, as everywhere else in this suite:
// FactoryEventHandlerRegistry is process-global with (eventType, handlerClass)
// first-registration-wins dedupe and no reset hook, so a shared event type
// silently drops registrations. None of these types appear anywhere else.

// -----------------------------------------------------------------------------
// EVENTS
// -----------------------------------------------------------------------------

/// <summary>Raised by the client with no handler registered at all.</summary>
public record ClientRaiseSoloEvent(Guid Id) : FactoryEventBase;

/// <summary>Raised by the client; its handler raises <see cref="ClientRaiseChainedEvent"/>.</summary>
public record ClientRaiseChainEvent(Guid Id) : FactoryEventBase;

/// <summary>Raised server-side by <see cref="ClientRaiseChainHandler"/>, never by a test.</summary>
public record ClientRaiseChainedEvent(Guid Id) : FactoryEventBase;

/// <summary>Raised by the client with <see cref="RaiseOptions.ServerOnly"/>.</summary>
public record ClientRaiseSuppressedEvent(Guid Id) : FactoryEventBase;

/// <summary>Raised by the client; its handler throws.</summary>
public record ClientRaiseThrowingEvent(Guid Id) : FactoryEventBase;

/// <summary>Raised by the client; its <c>AfterCommit</c> handler raises <see cref="ClientRaiseDeferredChainedEvent"/>.</summary>
public record ClientRaiseDeferredEvent(Guid Id) : FactoryEventBase;

/// <summary>Raised during the framework's AfterCommit drain, never by a test.</summary>
public record ClientRaiseDeferredChainedEvent(Guid Id) : FactoryEventBase;

// -----------------------------------------------------------------------------
// HANDLERS
// -----------------------------------------------------------------------------

/// <summary>
/// Raises a second event while handling the client's. Both belong in the response's
/// relay batch — this handler's event is the one the gap used to swallow.
/// </summary>
[FactoryEventHandler<ClientRaiseChainEvent>]
public static partial class ClientRaiseChainHandler
{
    internal static Task Handle(
        ClientRaiseChainEvent chainEvent,
        [Service] IFactoryEvents factoryEvents,
        CancellationToken ct)
        => factoryEvents.Raise(new ClientRaiseChainedEvent(chainEvent.Id), RaiseOptions.None, ct);
}

/// <summary>
/// Throws, so the client's <c>Raise</c> faults. The response never reaches the
/// client, so nothing relays.
/// </summary>
[FactoryEventHandler<ClientRaiseThrowingEvent>]
public static partial class ClientRaiseThrowingHandler
{
    internal static Task Handle(ClientRaiseThrowingEvent throwingEvent)
        => throw new InvalidOperationException($"client-raise handler throws for {throwingEvent.Id}");
}

/// <summary>
/// Deferred to <c>AfterCommit</c>, so it runs at the framework drain in
/// <c>HandleRemoteDelegateRequest</c> — which sits before the collector read. The
/// event it raises there still joins this response's batch.
/// </summary>
[FactoryEventHandler<ClientRaiseDeferredEvent>(DispatchPhase.AfterCommit)]
public static partial class ClientRaiseDeferredHandler
{
    internal static Task Handle(
        ClientRaiseDeferredEvent deferredEvent,
        [Service] IFactoryEvents factoryEvents,
        CancellationToken ct)
        => factoryEvents.Raise(new ClientRaiseDeferredChainedEvent(deferredEvent.Id), RaiseOptions.None, ct);
}
