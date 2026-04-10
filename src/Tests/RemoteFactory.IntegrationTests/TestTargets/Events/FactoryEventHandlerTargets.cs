using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Events;

// =============================================================================
// EVENT TYPES
// =============================================================================

/// <summary>
/// Multi-property event for testing serialization and data integrity.
/// </summary>
public record TestOrderEvent(Guid OrderId, string Email) : FactoryEventBase;

/// <summary>
/// Simple single-property event for testing strict type matching.
/// </summary>
public record TestAuditEvent(Guid EntityId) : FactoryEventBase;

/// <summary>
/// Event for testing error handling — handler throws when ShouldThrow is true.
/// </summary>
public record TestFailingEvent(Guid Id, bool ShouldThrow) : FactoryEventBase;

/// <summary>
/// Event with no registered handlers — tests no-op dispatch.
/// </summary>
public record TestUnhandledEvent() : FactoryEventBase;

/// <summary>
/// Nested record for complex serialization testing.
/// </summary>
public record TestAddress(string Street, string City, string Zip);

/// <summary>
/// Event with nested record property — tests complex serialization across client/server boundary.
/// </summary>
public record TestComplexEvent(Guid Id, string Name, TestAddress Address, List<string> Tags) : FactoryEventBase;

/// <summary>
/// Second event type for sequence testing — client raises multiple different event types.
/// </summary>
public record TestInventoryEvent(Guid ProductId, int Quantity) : FactoryEventBase;

// =============================================================================
// HANDLERS
// =============================================================================

/// <summary>
/// First handler for TestOrderEvent. Records "HandlerA".
/// Non-static class with internal static handler method.
/// </summary>
[Factory]
public partial class TestOrderHandlerA
{
    [FactoryEventHandler]
    internal static Task HandleOrder(
        TestOrderEvent orderEvent,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        eventService.RecordEventFired("HandlerA", orderEvent.OrderId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Second handler for TestOrderEvent. Records "HandlerB".
/// Static class with private static handler — tests both class types work.
/// </summary>
[Factory]
public static partial class TestOrderHandlerB
{
    [FactoryEventHandler]
    private static Task HandleOrder(
        TestOrderEvent orderEvent,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        eventService.RecordEventFired("HandlerB", orderEvent.OrderId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for TestAuditEvent only. Must NOT fire for TestOrderEvent.
/// Non-static class — tests strict type matching (no polymorphic dispatch).
/// </summary>
[Factory]
public partial class TestAuditHandler
{
    [FactoryEventHandler]
    internal static Task HandleAudit(
        TestAuditEvent auditEvent,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        eventService.RecordEventFired("AuditHandler", auditEvent.EntityId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler that throws when the event says to. For error handling tests.
/// </summary>
[Factory]
public static partial class TestFailingHandler
{
    [FactoryEventHandler]
    static Task HandleFailing(
        TestFailingEvent failEvent,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        if (failEvent.ShouldThrow)
            throw new InvalidOperationException($"Handler failed for {failEvent.Id}");

        eventService.RecordEventFired("FailHandler", failEvent.Id);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Second handler for TestFailingEvent. Always succeeds.
/// Non-static class — verifies ContinueOnFail with mixed class types.
/// </summary>
[Factory]
public partial class TestFailingSurvivor
{
    [FactoryEventHandler]
    internal static Task HandleFailing(
        TestFailingEvent failEvent,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        eventService.RecordEventFired("SurvivorHandler", failEvent.Id);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for TestComplexEvent — records all properties to verify serialization.
/// </summary>
[Factory]
public partial class TestComplexHandler
{
    [FactoryEventHandler]
    internal static Task HandleComplex(
        TestComplexEvent complexEvent,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        eventService.RecordEventFired(
            $"ComplexHandler:{complexEvent.Name}:{complexEvent.Address.City}:{string.Join(",", complexEvent.Tags)}",
            complexEvent.Id);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for TestInventoryEvent — non-static class with public static handler.
/// </summary>
[Factory]
public partial class TestInventoryHandler
{
    [FactoryEventHandler]
    public static Task HandleInventory(
        TestInventoryEvent inventoryEvent,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        eventService.RecordEventFired($"InventoryHandler:{inventoryEvent.Quantity}", inventoryEvent.ProductId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Slow handler for TestOrderEvent — used to test IEventTracker and await vs fire-and-forget.
/// </summary>
[Factory]
public static partial class TestSlowHandler
{
    [FactoryEventHandler]
    internal static async Task HandleSlow(
        TestOrderEvent orderEvent,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        await Task.Delay(300, ct);
        eventService.RecordEventFired("SlowHandler", orderEvent.OrderId);
    }
}

/// <summary>
/// Handler that captures correlation ID for TestOrderEvent.
/// </summary>
[Factory]
public static partial class TestCorrelationHandler
{
    [FactoryEventHandler]
    internal static Task HandleWithCorrelation(
        TestOrderEvent orderEvent,
        [Service] ICorrelationContext correlationContext,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        eventService.RecordEventFiredWithCorrelation(
            "CorrelationHandler",
            orderEvent.OrderId,
            correlationContext.CorrelationId);
        return Task.CompletedTask;
    }
}
