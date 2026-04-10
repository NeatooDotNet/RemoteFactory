using Neatoo.RemoteFactory;

namespace Design.Domain.FactoryPatterns;

// =============================================================================
// FACTORY EVENT HANDLER PATTERN (Mediator-style Events)
// =============================================================================
//
// The [FactoryEventHandler] pattern provides MediatR-style pub/sub events
// using source generation instead of reflection.
//
// Key differences from [Event]:
// - Publisher injects IFactoryEvents, not a specific delegate
// - Multiple handlers can subscribe to the same event type
// - Events are first-class objects (inherit from FactoryEventBase)
// - Caller chooses semantics: fire-and-forget vs await
//
// Handler methods must be static but the class does NOT need to be static.
// No underscore prefix convention — name methods naturally.
//
// =============================================================================

/// <summary>
/// Event object for when an order is placed.
/// Must inherit from FactoryEventBase.
/// </summary>
public record OrderPlacedEvent(int OrderId, string CustomerEmail) : FactoryEventBase;

/// <summary>
/// Demonstrates: [FactoryEventHandler] in a non-static [Factory] class.
///
/// Key points:
/// - Handler method is static (required) but class is non-static
/// - No underscore prefix needed (no delegate generated)
/// - First parameter is the event type (routing key)
/// - [Service] parameters are injected from an isolated DI scope
/// - CancellationToken is required as final parameter
/// </summary>
[Factory]
public partial class OrderNotifyHandlers
{
    [FactoryEventHandler]
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
[Factory]
public static partial class OrderAuditHdlrs
{
    [FactoryEventHandler]
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
