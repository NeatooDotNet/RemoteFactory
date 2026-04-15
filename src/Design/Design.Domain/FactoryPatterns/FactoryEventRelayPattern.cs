// =============================================================================
// DESIGN SOURCE OF TRUTH: Factory Event Relay Pattern
// =============================================================================
//
// Events raised on the server during factory operations (Create, Fetch, Save)
// can be relayed back to the client. The relay uses the existing HTTP response
// channel — events piggyback on RemoteResponseDto — so no SignalR or separate
// push infrastructure is needed.
//
// Three roles:
//
// 1. The EVENT TYPE inherits from FactoryEventBase (shared between client/server).
//    FactoryEventBase carries [FactoryEvent] and [DynamicallyAccessedMembers] with
//    Inherited = true — descendants are automatically discoverable by the runtime
//    FactoryEventTypeRegistry and preserved through IL trimming, with no generator
//    emission or client codegen.
// 2. The SERVER-SIDE RAISER is a factory method that injects IFactoryEvents
//    and calls Raise(new MyEvent(...)) during its execution.
// 3. The CLIENT-SIDE RELAY is the consumer's implementation of IFactoryEventRelay.
//    RemoteFactory invokes relay.Relay(events) fire-and-forget, strictly after the
//    factory method returns to its caller. The consumer's implementation bridges
//    the batch to their event aggregator (MediatR, plain aggregator, UI message
//    bus, etc.) and owns any threading / SyncContext marshaling.
//
// DESIGN DECISION: Single-method IFactoryEventRelay, consumer-owned bridge
//
// Reasons:
// 1. Every consumer already has an event aggregator — the old Register/Unregister
//    surface duplicated what the aggregator does (weak refs, type dispatch,
//    lifecycle) inside RemoteFactory.
// 2. A single Relay(IReadOnlyList<FactoryEventBase>) method lets the consumer
//    apply their own fan-out rules — per-session, per-viewmodel, etc.
// 3. The one-call-per-factory-call contract (even for zero events) gives
//    consumers a clean "a remote call just returned" hook for batch-end
//    bookkeeping.
//
// DID NOT DO THIS: Ship a MediatR bridge
//
// Reasons:
// 1. Not every consumer uses MediatR.
// 2. RemoteFactory has no dependency on any aggregator — the consumer picks.
//
// =============================================================================

using System.Collections.Concurrent;
using Neatoo.RemoteFactory;

namespace Design.Domain.FactoryPatterns;

// -----------------------------------------------------------------------------
// EVENT TYPES (shared assembly — visible to both client and server)
// -----------------------------------------------------------------------------

/// <summary>
/// Event raised when an order completes checkout on the server.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Events are records inheriting FactoryEventBase
///
/// Reasons:
/// 1. Records have structural equality (useful for deduplication).
/// 2. Records are immutable by default — events should not mutate.
/// 3. FactoryEventBase carries [FactoryEvent] and [DynamicallyAccessedMembers]
///    with Inherited = true, so every descendant is automatically discoverable
///    and trim-safe without any per-event annotation.
///
/// The rule: Events are records that inherit FactoryEventBase. Nothing else required.
/// </remarks>
public record OrderCheckoutCompleted(int OrderId, decimal Total) : FactoryEventBase;

// -----------------------------------------------------------------------------
// SERVER-SIDE RAISER — a factory method raises the event during its execution
// -----------------------------------------------------------------------------

/// <summary>
/// Demonstrates: raising a factory event from a server-side [Remote] method.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Raise events via IFactoryEvents injected as [Service]
///
/// Reasons:
/// 1. IFactoryEvents is scoped to the request — captured events travel with
///    the response.
/// 2. The same IFactoryEvents handles both server-side [FactoryEventHandler]
///    dispatch and client-relay capture — one API, two effects.
///
/// COMMON MISTAKE: Trying to raise events after the factory method returns
///
/// WRONG:
///     var result = await factory.Create(...);
///     factoryEvents.Raise(new MyEvent(...));  // Too late — no scope
///
/// RIGHT: Raise inside the factory method itself, while still on the server.
/// </remarks>
public interface ICheckoutOrder
{
    int Id { get; set; }
    decimal Total { get; set; }
}

[Factory]
internal partial class CheckoutOrder : ICheckoutOrder
{
    public int Id { get; set; }
    public decimal Total { get; set; }

    public CheckoutOrder() { }

    [Remote, Create]
    internal async Task Create(int id, decimal total, [Service] IFactoryEvents factoryEvents)
    {
        this.Id = id;
        this.Total = total;

        // Raise the event. With default RaiseOptions.None, the event is:
        //   1. Dispatched to every server-side [FactoryEventHandler<T>] static-method
        //      handler in THIS method's DI scope, sequentially, awaited. A handler
        //      that touches a DbContext sees the same DbContext this factory uses;
        //      a handler exception aborts the chain and propagates to this method
        //      (letting the caller's transaction roll back).
        //   2. Captured for relay back to the client in RemoteResponseDto. On the
        //      client, IFactoryEventRelay.Relay(...) is invoked fire-and-forget
        //      strictly AFTER this factory method's return value has been handed
        //      back to the caller and their continuation has resumed.
        await factoryEvents.Raise(new OrderCheckoutCompleted(id, total));
    }

    /// <summary>
    /// Demonstrates RaiseOptions.ServerOnly — dispatches to server handlers but
    /// does NOT relay back to the client.
    /// </summary>
    /// <remarks>
    /// Use ServerOnly when the event is a server-internal concern (e.g.,
    /// triggering a downstream process) that the UI doesn't need to know about.
    /// </remarks>
    [Remote, Create]
    internal async Task CreateWithServerOnlyEvent(int id, decimal total, [Service] IFactoryEvents factoryEvents)
    {
        this.Id = id;
        this.Total = total;

        // ServerOnly: server handlers run, client does NOT receive the event
        await factoryEvents.Raise(new OrderCheckoutCompleted(id, total), RaiseOptions.ServerOnly);
    }
}

// -----------------------------------------------------------------------------
// CLIENT-SIDE RELAY — consumer implements IFactoryEventRelay
// -----------------------------------------------------------------------------

/// <summary>
/// Demonstrates: an IFactoryEventRelay implementation that bridges relayed events
/// to an in-memory aggregator. Production consumers typically forward to MediatR,
/// a reactive subject, or a UI message bus.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Consumer implements IFactoryEventRelay; RemoteFactory ships a NoOp default
///
/// Reasons:
/// 1. In Remote mode, if the consumer registers nothing, RemoteFactory registers
///    NoOpFactoryEventRelay via TryAdd — zero surprise, no relayed events fire.
/// 2. To receive events, the consumer registers their own IFactoryEventRelay
///    implementation BEFORE calling AddNeatooRemoteFactory (TryAdd keeps it) OR
///    AFTER (standard DI override replaces the NoOp).
/// 3. Relay.Relay is called fire-and-forget on a separate continuation, so the
///    caller's `_entity = await factory.Create(...)` assignment has already
///    completed when Relay fires — handlers that read caller state see the new
///    value.
/// 4. Exceptions thrown by Relay are caught and logged; they never propagate to
///    the factory caller. The consumer owns SyncContext marshaling for UI work.
///
/// DID NOT DO THIS: Provide a bridge (MediatR, ReactiveUI, etc.)
///
/// Reasons:
/// 1. The aggregator choice is the consumer's — RemoteFactory takes no
///    opinion.
/// 2. Shipping a MediatR bridge would pull MediatR into RemoteFactory's
///    transitive dependencies. Staying aggregator-agnostic keeps the
///    surface minimal.
///
/// The rule: Implement IFactoryEventRelay, register it, bridge to your aggregator.
/// </remarks>
public sealed class InMemoryAggregatorRelay : IFactoryEventRelay
{
    private readonly ConcurrentQueue<FactoryEventBase> _received = new();

    public IReadOnlyCollection<FactoryEventBase> Received => _received.ToArray();

    public IEnumerable<T> ReceivedOfType<T>() where T : FactoryEventBase =>
        _received.OfType<T>();

    public Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        // Per the contract: one [Remote] call = one Relay call, even when the batch is empty.
        // Consumers can use the empty-batch invocation as a "a factory call just returned"
        // signal (e.g. for end-of-batch UI refresh).
        foreach (var evt in events)
        {
            _received.Enqueue(evt);
        }
        return Task.CompletedTask;
    }
}
