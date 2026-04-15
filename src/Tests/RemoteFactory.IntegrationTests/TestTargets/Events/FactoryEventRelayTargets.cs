using System.Collections.Concurrent;
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
/// Events are captured for relay back to the client's IFactoryEventRelay.
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
// TEST RELAY — IFactoryEventRelay implementation used by integration tests
// =============================================================================

/// <summary>
/// Integration test relay. Captures every relayed batch for assertion.
/// Tests can optionally inject a throwing delegate to exercise exception isolation.
/// </summary>
public sealed class RecordingFactoryEventRelay : IFactoryEventRelay
{
    private readonly ConcurrentQueue<FactoryEventBase> _received = new();
    private int _invocationCount;

    /// <summary>Total number of times Relay() has been invoked (one per [Remote] call).</summary>
    public int InvocationCount => Volatile.Read(ref _invocationCount);

    public IReadOnlyList<FactoryEventBase> Received => _received.ToArray();

    public IReadOnlyList<T> ReceivedOfType<T>() where T : FactoryEventBase =>
        _received.OfType<T>().ToList();

    /// <summary>Optional hook invoked before events are appended. Use to throw from Relay.</summary>
    public Func<IReadOnlyList<FactoryEventBase>, Task>? OnRelay { get; set; }

    public async Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        Interlocked.Increment(ref _invocationCount);
        if (OnRelay != null)
        {
            await OnRelay(events).ConfigureAwait(false);
        }
        foreach (var evt in events)
        {
            _received.Enqueue(evt);
        }
    }
}
