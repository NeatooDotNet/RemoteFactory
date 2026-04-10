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
// HANDLERS — [FactoryEventHandler<T>] class attribute with matching static methods
// =============================================================================

/// <summary>
/// First handler for TestOrderEvent. Records "HandlerA".
/// </summary>
[FactoryEventHandler<TestOrderEvent>]
public partial class TestOrderHandlerA
{
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
[FactoryEventHandler<TestOrderEvent>]
public static partial class TestOrderHandlerB
{
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
/// Tests strict type matching (no polymorphic dispatch).
/// </summary>
[FactoryEventHandler<TestAuditEvent>]
public partial class TestAuditHandler
{
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
[FactoryEventHandler<TestFailingEvent>]
public static partial class TestFailingHandler
{
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
/// Verifies ContinueOnFail with mixed class types.
/// </summary>
[FactoryEventHandler<TestFailingEvent>]
public partial class TestFailingSurvivor
{
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
[FactoryEventHandler<TestComplexEvent>]
public partial class TestComplexHandler
{
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
[FactoryEventHandler<TestInventoryEvent>]
public partial class TestInventoryHandler
{
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
[FactoryEventHandler<TestOrderEvent>]
public static partial class TestSlowHandler
{
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
[FactoryEventHandler<TestOrderEvent>]
public static partial class TestCorrelationHandler
{
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
