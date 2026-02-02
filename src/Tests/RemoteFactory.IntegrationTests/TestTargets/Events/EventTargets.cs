using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Events;

/// <summary>
/// Service for tracking event handler invocations in tests.
/// </summary>
public interface IEventTestService
{
    void RecordEventFired(string eventName, Guid entityId);
    void RecordEventFiredWithCorrelation(string eventName, Guid entityId, string? correlationId);
    List<(string EventName, Guid EntityId)> GetRecordedEvents();
    List<(string EventName, Guid EntityId, string? CorrelationId)> GetRecordedEventsWithCorrelation();
    void Clear();
}

/// <summary>
/// Implementation of event test service for tracking events in tests.
/// Named to match IEventTestService for RegisterMatchingName pattern.
/// </summary>
public class EventTestService : IEventTestService
{
    private readonly List<(string EventName, Guid EntityId)> _events = new();
    private readonly List<(string EventName, Guid EntityId, string? CorrelationId)> _eventsWithCorrelation = new();
    private readonly object _lock = new();

    public void RecordEventFired(string eventName, Guid entityId)
    {
        lock (_lock)
        {
            _events.Add((eventName, entityId));
        }
    }

    public void RecordEventFiredWithCorrelation(string eventName, Guid entityId, string? correlationId)
    {
        lock (_lock)
        {
            _eventsWithCorrelation.Add((eventName, entityId, correlationId));
        }
    }

    public List<(string EventName, Guid EntityId)> GetRecordedEvents()
    {
        lock (_lock)
        {
            return new List<(string EventName, Guid EntityId)>(_events);
        }
    }

    public List<(string EventName, Guid EntityId, string? CorrelationId)> GetRecordedEventsWithCorrelation()
    {
        lock (_lock)
        {
            return new List<(string EventName, Guid EntityId, string? CorrelationId)>(_eventsWithCorrelation);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
            _eventsWithCorrelation.Clear();
        }
    }
}

/// <summary>
/// Test entity with [Event] method for testing event generation.
/// </summary>
[Factory]
public partial class OrderEventTarget
{
    public Guid OrderId { get; set; }

    /// <summary>
    /// Event handler that runs in an isolated scope.
    /// </summary>
    [Event]
    public Task SendOrderConfirmation(Guid orderId, [Service] IEventTestService eventService, CancellationToken ct)
    {
        eventService.RecordEventFired("SendConfirmation", orderId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Event handler that returns void (should still generate Task delegate).
    /// </summary>
    [Event]
    public void NotifyOrderShipped(Guid orderId, string message, [Service] IEventTestService eventService, CancellationToken ct)
    {
        eventService.RecordEventFired($"NotifyShipped:{message}", orderId);
    }

    [Create]
    public static OrderEventTarget Create(Guid orderId)
    {
        return new OrderEventTarget { OrderId = orderId };
    }
}

/// <summary>
/// Static class with [Event] method for testing static event generation.
/// </summary>
[Factory]
public static partial class OrderEventHandler
{
    /// <summary>
    /// Static event handler.
    /// </summary>
    [Event]
    public static Task NotifyWarehouse(Guid orderId, [Service] IEventTestService eventService, CancellationToken ct)
    {
        eventService.RecordEventFired("NotifyWarehouse", orderId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test target for verifying correlation ID propagation to event handlers.
/// </summary>
[Factory]
public partial class CorrelationEventTarget
{
    public Guid Id { get; set; }

    /// <summary>
    /// Event handler that captures and records the correlation ID it receives.
    /// </summary>
    [Event]
    public Task ProcessWithCorrelation(
        Guid entityId,
        [Service] ICorrelationContext correlationContext,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        eventService.RecordEventFiredWithCorrelation(
            "ProcessWithCorrelation",
            entityId,
            correlationContext.CorrelationId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Event handler that captures correlation ID with additional message parameter.
    /// </summary>
    [Event]
    public Task ProcessWithCorrelationAndMessage(
        Guid entityId,
        string message,
        [Service] ICorrelationContext correlationContext,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        eventService.RecordEventFiredWithCorrelation(
            $"ProcessWithMessage:{message}",
            entityId,
            correlationContext.CorrelationId);
        return Task.CompletedTask;
    }

    [Create]
    public static CorrelationEventTarget Create()
    {
        return new CorrelationEventTarget { Id = Guid.NewGuid() };
    }
}

/// <summary>
/// Static class with correlation-aware event handlers for testing.
/// </summary>
[Factory]
public static partial class CorrelationEventHandler
{
    /// <summary>
    /// Static event handler that captures correlation ID.
    /// </summary>
    [Event]
    public static Task HandleWithCorrelation(
        Guid entityId,
        [Service] ICorrelationContext correlationContext,
        [Service] IEventTestService eventService,
        CancellationToken ct)
    {
        eventService.RecordEventFiredWithCorrelation(
            "StaticHandleWithCorrelation",
            entityId,
            correlationContext.CorrelationId);
        return Task.CompletedTask;
    }
}
