using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Events;

// =============================================================================
// SCOPE PROBE — scoped service used to prove handlers share the caller's scope
// =============================================================================

/// <summary>
/// Scoped probe whose <see cref="InstanceId"/> differs per DI scope.
/// If two places resolve <c>IScopeProbe</c> and see the same <see cref="InstanceId"/>,
/// they are running in the same scope.
/// </summary>
public interface IScopeProbe
{
    Guid InstanceId { get; }
    List<string> Touches { get; }
}

public sealed class ScopeProbe : IScopeProbe
{
    public Guid InstanceId { get; } = Guid.NewGuid();
    public List<string> Touches { get; } = new();
}

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
/// Second handler for TestFailingEvent. Always succeeds — used to verify that
/// both handlers observe the event when no handler throws.
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
/// Event used to exercise non-canonical handler parameter declaration order.
/// </summary>
public record TestParamOrderEvent(Guid Id) : FactoryEventBase;

/// <summary>
/// Handler whose parameters are declared in the order (event, CancellationToken,
/// [Service]) — i.e. NOT the canonical (event, [Service], CancellationToken) order.
/// The generator must emit invocation arguments in declaration order so this binds
/// correctly. Before the fix, arguments were reshuffled to (event, service, ct)
/// and the call would either fail to compile or silently bind the wrong values.
/// </summary>
[FactoryEventHandler<TestParamOrderEvent>]
public partial class TestParamOrderHandler
{
    internal static Task Handle(
        TestParamOrderEvent paramEvent,
        CancellationToken ct,
        [Service] IEventTestService eventService)
    {
        eventService.RecordEventFired($"ParamOrderHandler:ctCancellable={ct.CanBeCanceled}", paramEvent.Id);
        return Task.CompletedTask;
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

// =============================================================================
// SHARED-SCOPE TEST TARGETS
// =============================================================================

/// <summary>
/// Event for shared-scope tests. Carries the scope InstanceId the caller observed,
/// so handlers can compare and assert they resolved the same scoped instance.
/// </summary>
public record TestScopeProbeEvent(Guid CallerScopeInstanceId, string Tag) : FactoryEventBase;

/// <summary>
/// First handler for TestScopeProbeEvent. Resolves IScopeProbe from its injected
/// scope and appends its name to the probe's Touches list. A shared-scope dispatch
/// means the probe instance is the same as the caller's.
/// </summary>
[FactoryEventHandler<TestScopeProbeEvent>]
public partial class TestScopeProbeHandlerAlpha
{
    internal static Task HandleProbe(
        TestScopeProbeEvent probeEvent,
        [Service] IScopeProbe probe,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        probe.Touches.Add("Alpha");
        eventService.RecordEventFired(
            $"ProbeHandlerAlpha:{probe.InstanceId}:match={(probe.InstanceId == probeEvent.CallerScopeInstanceId)}",
            probeEvent.CallerScopeInstanceId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Second handler for TestScopeProbeEvent. Used together with Alpha to verify
/// sequential dispatch: after Raise returns, the probe's Touches list contains
/// both names.
/// </summary>
[FactoryEventHandler<TestScopeProbeEvent>]
public partial class TestScopeProbeHandlerBeta
{
    internal static Task HandleProbe(
        TestScopeProbeEvent probeEvent,
        [Service] IScopeProbe probe,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        probe.Touches.Add("Beta");
        eventService.RecordEventFired(
            $"ProbeHandlerBeta:{probe.InstanceId}:touchCount={probe.Touches.Count}",
            probeEvent.CallerScopeInstanceId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Event whose handler blocks on its CancellationToken so tests can assert
/// the caller's CT flows through to handlers.
/// </summary>
public record TestCancellableEvent(Guid Id) : FactoryEventBase;

/// <summary>
/// Handler that awaits a long Task.Delay bound to the caller's CT. Cancelling
/// the CT must make this handler observe an OperationCanceledException.
/// </summary>
[FactoryEventHandler<TestCancellableEvent>]
public partial class TestCancellableHandler
{
    internal static async Task HandleCancellable(
        TestCancellableEvent cancellableEvent,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            eventService.RecordEventFired("CancellableHandler:completed", cancellableEvent.Id);
        }
        catch (OperationCanceledException)
        {
            eventService.RecordEventFired("CancellableHandler:cancelled", cancellableEvent.Id);
            throw;
        }
    }
}
