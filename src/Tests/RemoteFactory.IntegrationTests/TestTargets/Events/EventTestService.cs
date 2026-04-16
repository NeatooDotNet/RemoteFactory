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
