using Neatoo.RemoteFactory;

namespace Design.Domain.FactoryPatterns;

// =============================================================================
// FACTORY EVENT HANDLER PATTERN (Mediator-style Events)
// =============================================================================
//
// The [FactoryEventHandler<T>] class attribute provides MediatR-style pub/sub events
// using source generation instead of reflection.
//
// Key points:
// - Publisher injects IFactoryEvents, not a specific delegate
// - Multiple handlers can subscribe to the same event type
// - Events are first-class objects (inherit from FactoryEventBase)
// - Caller chooses semantics: fire-and-forget vs await
//
// Static handler methods → server-side dispatch (isolated scope, fire-and-forget)
// Instance handler methods → client-side relay dispatch
//
// No [Factory] attribute needed — [FactoryEventHandler<T>] is a standalone pipeline.
//
// =============================================================================

/// <summary>
/// Event object for when an order is placed.
/// Must inherit from FactoryEventBase.
/// </summary>
public record OrderPlacedEvent(int OrderId, string CustomerEmail) : FactoryEventBase;

/// <summary>
/// Demonstrates: [FactoryEventHandler&lt;T&gt;] with a static server-side handler.
///
/// Key points:
/// - Handler method is static → registers into FactoryEventHandlerRegistry (server-side)
/// - First non-[Service]/non-CT parameter is the event type (routing key)
/// - [Service] parameters are injected from an isolated DI scope
/// - CancellationToken bound to IHostApplicationLifetime.ApplicationStopping
/// </summary>
[FactoryEventHandler<OrderPlacedEvent>]
public partial class OrderNotifyHandlers
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
/// Demonstrates: Multiple handlers for the same event type in a static class.
/// Both handlers run in parallel when the event is raised.
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
