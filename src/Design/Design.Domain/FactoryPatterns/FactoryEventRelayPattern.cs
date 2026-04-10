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
// 2. The SERVER-SIDE RAISER is a factory method that injects IFactoryEvents
//    and calls Raise(new MyEvent(...)) during its execution.
// 3. The CLIENT-SIDE HANDLER is a class decorated with [FactoryEventHandler<T>]
//    that has a matching instance method. It registers itself with
//    IFactoryEventRelay at runtime (typically from a layout component or
//    viewmodel constructor) and receives events after factory operations
//    complete on the client.
//
// The source generator discovers [FactoryEventHandler<T>] classes via attribute
// scanning (cheap Roslyn operation) and generates a FactoryServiceRegistrar that
// registers the dispatch delegate with FactoryEventRelayRegistry. No reflection
// is used at runtime — dispatch is a direct cast + method call.
//
// =============================================================================

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
/// 1. Records have structural equality (useful for deduplication)
/// 2. Records are immutable by default — events should not mutate
/// 3. FactoryEventBase is the marker type the generator looks for
///
/// DID NOT DO THIS: Use classes or interfaces for events
///
/// Reasons:
/// 1. Classes would need manual equality members
/// 2. Interfaces can't be used as the generic argument of [FactoryEventHandler<T>]
///    because the generator needs a concrete type for deserialization
///
/// The rule: Events are records that inherit FactoryEventBase.
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
///    the response
/// 2. Injection is method-level ([Service] parameter), so the relay is
///    server-only by default (client has no IFactoryEvents implementation
///    that captures)
/// 3. The same IFactoryEvents handles both server-side [FactoryEventHandler]
///    dispatch and client-relay capture — one API, two effects
///
/// COMMON MISTAKE: Trying to raise events after the factory method returns
///
/// WRONG:
///     var result = await factory.Create(...);
///     factoryEvents.Raise(new MyEvent(...));  // Too late — no scope
///
/// RIGHT: Raise inside the factory method itself, while still on the server:
///     [Remote, Create]
///     internal async Task Create(..., [Service] IFactoryEvents events)
///     {
///         // ... do work ...
///         await events.Raise(new MyEvent(...));
///     }
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
        //   1. Dispatched to server-side [FactoryEventHandler<T>] static-method handlers
        //   2. Captured for relay back to the client in RemoteResponseDto
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
// CLIENT-SIDE RELAY HANDLER
// -----------------------------------------------------------------------------

/// <summary>
/// Demonstrates: [FactoryEventHandler&lt;T&gt;] with an instance method → client-side relay handler.
/// </summary>
/// <remarks>
/// DESIGN DECISION: [FactoryEventHandler&lt;T&gt;] is a class-level attribute, not a method attribute
///
/// Reasons:
/// 1. Attribute-on-class is efficient to find in Roslyn (attribute metadata lookup)
///    vs. scanning every method in every class
/// 2. The class attribute carries the event type explicitly — no inference needed
/// 3. The generator validates exactly one matching method exists (NF0501 / NF0502
///    compile-time errors if missing or ambiguous)
/// 4. Clean separation from [Factory] — handler classes are NOT factories
///
/// DID NOT DO THIS: Use IFactoryEventHandler&lt;T&gt; interface
///
/// Reasons:
/// 1. Interface scanning in Roslyn is expensive (every class must be checked)
/// 2. Interfaces force a specific method name (HandleFactoryEvent) — less natural
/// 3. The handler class would need to cast in the dispatch delegate anyway
///
/// The rule: Use [FactoryEventHandler&lt;T&gt;] class attribute. Let the generator
/// find the matching method by signature.
///
/// Method matching rules:
/// - Return type: Task (not void, not Task&lt;T&gt;)
/// - First non-[Service]/non-CancellationToken parameter: type T
/// - Accessibility: any (public, internal, private)
/// - Exactly one match required — NF0502 if ambiguous
///
/// STATIC vs INSTANCE:
/// - Static method → server-side handler (runs in isolated scope via
///   FactoryEventHandlerRegistry with [Service] injection and CancellationToken)
/// - Instance method → client-side relay handler (called on the registered
///   handler instance via FactoryEventRelayRegistry, no DI scope)
///
/// A single class can declare multiple [FactoryEventHandler&lt;T&gt;] attributes
/// to handle multiple event types.
/// </remarks>
[FactoryEventHandler<OrderCheckoutCompleted>]
public sealed partial class OrderCheckoutViewModel : IDisposable
{
    private readonly IFactoryEventRelay _relay;
    private readonly List<OrderCheckoutCompleted> _receivedEvents = new();

    public IReadOnlyList<OrderCheckoutCompleted> ReceivedEvents => _receivedEvents;

    public OrderCheckoutViewModel(IFactoryEventRelay relay)
    {
        ArgumentNullException.ThrowIfNull(relay);
        _relay = relay;
        _relay.Register(this);
    }

    /// <summary>
    /// The handler method the generator discovers. Must return Task and take T
    /// as its first non-[Service]/non-CancellationToken parameter.
    /// </summary>
    /// <remarks>
    /// COMMON MISTAKE: Making the handler async void or returning Task&lt;T&gt;
    ///
    /// WRONG:
    ///     public async void HandleCheckout(OrderCheckoutCompleted evt) { ... }  // void — NF0501
    ///     public Task&lt;string&gt; HandleCheckout(OrderCheckoutCompleted evt) { ... }  // Task&lt;T&gt; — NF0501
    ///
    /// RIGHT:
    ///     public Task HandleCheckout(OrderCheckoutCompleted evt) { ... }
    /// </remarks>
    public Task HandleCheckout(OrderCheckoutCompleted factoryEvent)
    {
        ArgumentNullException.ThrowIfNull(factoryEvent);
        _receivedEvents.Add(factoryEvent);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _relay.Unregister(this);
    }
}
