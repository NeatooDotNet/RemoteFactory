using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Singleton implementation of <see cref="IFactoryEventRelay"/> that dispatches
/// relayed factory events to registered handler instances using source-generated
/// dispatch delegates from <see cref="FactoryEventRelayRegistry"/>.
/// Uses weak references to prevent memory leaks from unregistered handlers.
/// </summary>
internal sealed class FactoryEventRelayDispatcher : IFactoryEventRelay
{
    private readonly List<(WeakReference<object> handler, string eventTypeName, Func<object, object, Task> dispatch)> _handlers = new();
    private readonly Lock _lock = new();

    public void Register(object handler)
    {
        var entries = FactoryEventRelayRegistry.GetHandlerEntries(handler.GetType());
        if (entries == null)
            return;

        lock (_lock)
        {
            foreach (var entry in entries)
            {
                _handlers.Add((new WeakReference<object>(handler), entry.eventTypeName, entry.dispatch));
            }
        }
    }

    public void Unregister(object handler)
    {
        lock (_lock)
        {
            _handlers.RemoveAll(entry => !entry.handler.TryGetTarget(out var target) || ReferenceEquals(target, handler));
        }
    }

    /// <summary>
    /// Dispatches relayed events to registered handlers. Each event is deserialized
    /// using the deserializer from <see cref="FactoryEventRelayRegistry"/>
    /// and dispatched via the handler's source-generated typed delegate (no reflection).
    /// Handler exceptions are swallowed.
    /// </summary>
#pragma warning disable CA1031 // Intentional: handler exceptions must not propagate to factory caller
    internal async Task DispatchRelayedEvents(IReadOnlyList<RelayedFactoryEvent> events, INeatooJsonSerializer serializer)
    {
        foreach (var relayedEvent in events)
        {
            var deserializer = FactoryEventRelayRegistry.GetDeserializer(relayedEvent.TypeFullName);
            if (deserializer == null)
                continue;

            object eventObj;
            try
            {
                eventObj = deserializer(relayedEvent.Json, serializer);
            }
            catch
            {
                continue;
            }

            List<(WeakReference<object> handler, Func<object, object, Task> dispatch)> snapshot;
            lock (_lock)
            {
                // Clean up dead references opportunistically
                _handlers.RemoveAll(entry => !entry.handler.TryGetTarget(out _));

                // Get handlers matching this event type
                snapshot = _handlers
                    .Where(entry => entry.eventTypeName == relayedEvent.TypeFullName)
                    .Select(entry => (entry.handler, entry.dispatch))
                    .ToList();
            }

            foreach (var (wr, dispatch) in snapshot)
            {
                if (!wr.TryGetTarget(out var handler))
                    continue;

                try
                {
                    await dispatch(handler, eventObj).ConfigureAwait(false);
                }
                catch
                {
                    // Handler exceptions are swallowed — they must not propagate to the factory caller
                }
            }
        }
    }
#pragma warning restore CA1031
}
