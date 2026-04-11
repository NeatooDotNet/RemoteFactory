using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Events;

// =============================================================================
// RELAY EVENT TYPES
// =============================================================================

/// <summary>
/// Event raised during server-side operations for relay testing.
/// </summary>
public record TestRelayEvent(Guid Id, string Message) : FactoryEventBase;

/// <summary>
/// Second relay event type for multi-event and ordering tests.
/// </summary>
public record TestRelayEventB(Guid Id, int Sequence) : FactoryEventBase;

/// <summary>
/// Event for testing ServerOnly exclusion from relay.
/// </summary>
public record TestServerOnlyRelayEvent(Guid Id) : FactoryEventBase;

// =============================================================================
// FACTORY CLASS THAT RAISES EVENTS (SERVER-SIDE)
// Uses static factory pattern with [Execute] methods.
// =============================================================================

/// <summary>
/// Result type returned from relay test factory methods.
/// </summary>
public class RelayTestResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

/// <summary>
/// Static factory class that raises events during server-side operations.
/// Events should be captured for relay back to the client.
/// </summary>
[Factory]
public static partial class RelayTestCommands
{
    [Execute]
    [Remote]
    internal static async Task<RelayTestResult> _Create(
        string name,
        [Service] IFactoryEvents factoryEvents)
    {
        var result = new RelayTestResult { Id = Guid.NewGuid(), Name = name };
        await factoryEvents.Raise(new TestRelayEvent(result.Id, $"Created: {name}"));
        return result;
    }

    [Execute]
    [Remote]
    internal static async Task<RelayTestResult> _CreateWithMultipleEvents(
        string name,
        [Service] IFactoryEvents factoryEvents)
    {
        var result = new RelayTestResult { Id = Guid.NewGuid(), Name = name };
        await factoryEvents.Raise(new TestRelayEvent(result.Id, "First"));
        await factoryEvents.Raise(new TestRelayEventB(result.Id, 2));
        return result;
    }

    [Execute]
    [Remote]
    internal static async Task<RelayTestResult> _CreateWithServerOnlyEvent(
        string name,
        [Service] IFactoryEvents factoryEvents)
    {
        var result = new RelayTestResult { Id = Guid.NewGuid(), Name = name };
        await factoryEvents.Raise(new TestServerOnlyRelayEvent(result.Id), RaiseOptions.ServerOnly);
        return result;
    }

    [Execute]
    [Remote]
    internal static Task<RelayTestResult> _CreateNoEvents(string name)
    {
        return Task.FromResult(new RelayTestResult { Id = Guid.NewGuid(), Name = name });
    }

    [Execute]
    [Remote]
    internal static async Task<RelayTestResult> _CreateWithServerOnlyCombinedFlags(
        string name,
        [Service] IFactoryEvents factoryEvents)
    {
        var result = new RelayTestResult { Id = Guid.NewGuid(), Name = name };
        await factoryEvents.Raise(new TestServerOnlyRelayEvent(result.Id), RaiseOptions.ServerOnly);
        return result;
    }
}

// =============================================================================
// CLIENT-SIDE RELAY HANDLERS
// [FactoryEventHandler<T>] with instance methods — generates relay dispatch entries.
// =============================================================================

/// <summary>
/// Client-side handler that records relayed events for test assertions.
/// </summary>
[FactoryEventHandler<TestRelayEvent>]
public partial class TestRelayHandler
{
    private readonly List<TestRelayEvent> _received = new();
    private readonly object _lock = new();

    public IReadOnlyList<TestRelayEvent> ReceivedEvents
    {
        get { lock (_lock) { return _received.ToList(); } }
    }

    public Task HandleFactoryEvent(TestRelayEvent factoryEvent)
    {
        lock (_lock)
        {
            _received.Add(factoryEvent);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Second handler for TestRelayEvent — tests multiple handlers for same event.
/// </summary>
[FactoryEventHandler<TestRelayEvent>]
public partial class TestRelayHandlerB
{
    private readonly List<TestRelayEvent> _received = new();
    private readonly object _lock = new();

    public IReadOnlyList<TestRelayEvent> ReceivedEvents
    {
        get { lock (_lock) { return _received.ToList(); } }
    }

    public Task HandleFactoryEvent(TestRelayEvent factoryEvent)
    {
        lock (_lock)
        {
            _received.Add(factoryEvent);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler for TestRelayEventB — tests multi-event-type relay.
/// </summary>
[FactoryEventHandler<TestRelayEventB>]
public partial class TestRelayEventBHandler
{
    private readonly List<TestRelayEventB> _received = new();
    private readonly object _lock = new();

    public IReadOnlyList<TestRelayEventB> ReceivedEvents
    {
        get { lock (_lock) { return _received.ToList(); } }
    }

    public Task HandleFactoryEvent(TestRelayEventB factoryEvent)
    {
        lock (_lock)
        {
            _received.Add(factoryEvent);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// Handler that throws — tests exception swallowing.
/// </summary>
[FactoryEventHandler<TestRelayEvent>]
public partial class TestRelayThrowingHandler
{
    public Task HandleFactoryEvent(TestRelayEvent factoryEvent)
    {
        throw new InvalidOperationException("Handler failure should be swallowed");
    }
}
