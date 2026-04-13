using Neatoo.RemoteFactory;

namespace Design.Domain.FactoryPatterns;

// =============================================================================
// FACTORY EVENT HANDLER PATTERN (Transactional Mediator Events)
// =============================================================================
//
// [FactoryEventHandler<T>] + IFactoryEvents.Raise<T>() is Neatoo's path for
// domain events that must participate in the caller's transaction.
//
// EXECUTION MODEL (the three invariants that define FactoryEvent):
//
//   1. SHARED SCOPE. Handlers resolve their [Service] dependencies from the
//      caller's IServiceProvider. A DbContext injected into the factory method
//      and a DbContext injected into the handler are the same instance, so
//      both participate in the same transaction.
//
//   2. SEQUENTIAL. Handlers run one after another in unspecified order. A
//      DbContext is not thread-safe, so handlers cannot run in parallel.
//      Callers must not depend on a specific ordering.
//
//   3. AWAITED. Raise<T>() does not return until every handler has completed.
//      A handler exception aborts the remaining handlers and propagates to the
//      caller, so the caller can let the transaction roll back. Across the
//      client/server boundary the HTTP call stays open until all server-side
//      handlers have finished, and a server exception surfaces on the client.
//
// When to use [FactoryEventHandler<T>] with IFactoryEvents.Raise<T>:
//   - Domain events that must participate in the aggregate's transaction
//   - Events where a handler failure should abort the aggregate operation
//   - Events where you need the caller to observe the post-handler state
//
// When to use [Event] delegates instead:
//   - Fire-and-forget notifications (email, webhooks, queue publishes)
//   - Audit sinks to external systems
//   - Anything that should survive (or shouldn't block) the caller's work
//
// Static handler methods → server-side dispatch (runs in caller's scope).
// Instance handler methods → client-side relay dispatch (see FactoryEventRelayPattern.cs).
//
// No [Factory] attribute needed — [FactoryEventHandler<T>] is a standalone pipeline.
//
// =============================================================================

/// <summary>
/// Event object raised when an order is placed. Inherits from FactoryEventBase.
/// </summary>
public record OrderPlacedEvent(int OrderId, string CustomerEmail) : FactoryEventBase;

/// <summary>
/// Demonstrates: [FactoryEventHandler&lt;T&gt;] with a static server-side handler.
///
/// Key points:
/// - Handler method is static → registers into FactoryEventHandlerRegistry.
/// - First non-[Service]/non-CT parameter is the event type (routing key).
/// - [Service] parameters resolve from the CALLER'S scope. A DbContext injected
///   here is the same DbContext the factory method is using — the handler's
///   work participates in the caller's transaction automatically.
/// - CancellationToken is the token the caller passed to
///   <see cref="IFactoryEvents.Raise{T}"/>.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Share the caller's DI scope with handlers.
///
/// Reasons:
/// 1. Domain events often need to read or write through the same DbContext
///    (and therefore the same transaction) as the aggregate that raised them.
///    A new scope would give the handler a new DbContext and a separate
///    transaction, which defeats the point.
/// 2. Scoped services other than DbContext (tenant context, correlation
///    context, unit-of-work markers) are also shared automatically — no
///    explicit propagation needed.
/// 3. A handler that throws can roll the caller's transaction back, which
///    makes handler code a natural place to enforce cross-aggregate invariants.
///
/// DID NOT DO THIS: Detached scope + fire-and-forget dispatch (the "older"
/// mediator default).
///
/// Reasons:
/// 1. That path exists already as <c>[Event]</c> delegates — use those when
///    detached semantics are what you want.
/// 2. Two different execution models sharing one API would be confusing.
///    Naming them separately (FactoryEvent vs Event) makes the choice loud.
/// </remarks>
[FactoryEventHandler<OrderPlacedEvent>]
public static partial class OrderNotifyHandlers
{
    internal static async Task SendOrderConfirmation(
        OrderPlacedEvent orderEvent,
        [Service] INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        await notificationService.SendAsync(
            orderEvent.CustomerEmail,
            $"Order {orderEvent.OrderId} confirmed!");
    }
}

/// <summary>
/// Demonstrates: multiple handlers for the same event type.
/// Both handlers run in the caller's scope, sequentially, in unspecified order.
/// A failure in either one aborts the remaining handler and propagates to the caller.
/// </summary>
[FactoryEventHandler<OrderPlacedEvent>]
public static partial class OrderAuditHdlrs
{
    static async Task AuditOrderPlaced(
        OrderPlacedEvent orderEvent,
        [Service] INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        await notificationService.SendAsync(
            "audit@example.com",
            $"Audit: Order {orderEvent.OrderId} placed by {orderEvent.CustomerEmail}");
    }
}

// =============================================================================
// NESTED-RECORD EVENT: automatic IL-trimming preservation
// =============================================================================
//
// When an event record carries a nested record property (like ShippingAddress
// below), the generator emits preservation calls for BOTH the event and the
// nested record in the handler's FactoryServiceRegistrar:
//
//   DtoConstructorRegistry.PreserveType<OrderShippedEvent>();
//   DtoConstructorRegistry.PreserveType<ShippingAddress>();
//
// The PreserveType<T> primitive applies [DynamicallyAccessedMembers(All)] to T,
// instructing the IL trimmer to keep the record's primary constructor and
// public properties intact. Without this, a Blazor WASM Release build with
// PublishTrimmed=true would strip the metadata that NeatooJsonTypeInfoResolver
// and RecordBypassConverterFactory need to round-trip the event at runtime.
//
// Nested records with parameterless ctors (plain DTOs) instead get
// DtoConstructorRegistry.Register<N>(() => new N()) — same trimming effect.
//
// Known gap: Dictionary<K,V> value types are not walked. If your event exposes
// Dictionary<string, Payload>, declare another [FactoryEventHandler<Payload>]
// or an additional preservation hint to keep Payload intact after trimming.
// =============================================================================

/// <summary>
/// Nested value object shipped with <see cref="OrderShippedEvent"/>.
/// A record with a parameterized primary ctor — deserialized via
/// RecordBypassConverterFactory on the receiving side.
/// </summary>
public record ShippingAddress(string Street, string City, string PostalCode);

/// <summary>
/// Event object raised when an order ships. Demonstrates an event record with
/// a nested record property — the generator automatically emits preservation
/// for both <c>OrderShippedEvent</c> and <c>ShippingAddress</c> so the event
/// round-trips correctly in IL-trimmed builds.
/// </summary>
public record OrderShippedEvent(Guid OrderId, ShippingAddress Address) : FactoryEventBase;

/// <summary>
/// Demonstrates: handler for an event with a nested parameterized-record property.
/// Verifies that the nested record is preserved from IL trimming without any
/// manual hints on the user side — the generator handles it automatically.
/// </summary>
[FactoryEventHandler<OrderShippedEvent>]
public static partial class OrderShippedHandlers
{
    internal static Task NotifyShipped(
        OrderShippedEvent shipEvent,
        [Service] INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        return notificationService.SendAsync(
            "ops@example.com",
            $"Order {shipEvent.OrderId} shipped to {shipEvent.Address.City}");
    }
}
