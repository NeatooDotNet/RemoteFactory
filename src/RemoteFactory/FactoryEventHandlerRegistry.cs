using System.Collections.Concurrent;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Static registry for [FactoryEventHandler] handler factories.
/// Each assembly's generated FactoryServiceRegistrar adds its handlers here during DI setup.
/// The FactoryEventsDispatcher reads from this registry to dispatch events.
/// </summary>
public static class FactoryEventHandlerRegistry
{
    private static readonly ConcurrentDictionary<Type, List<Func<IServiceProvider, object, RaiseOptions, Task>>> _handlers = new();

    /// <summary>
    /// Registers a handler factory for the given event type.
    /// Called by generated FactoryServiceRegistrar methods during DI setup.
    /// </summary>
    public static void RegisterHandler<TEvent>(Func<IServiceProvider, object, RaiseOptions, Task> handlerFactory)
        where TEvent : FactoryEventBase
    {
        var list = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Func<IServiceProvider, object, RaiseOptions, Task>>());
        lock (list)
        {
            list.Add(handlerFactory);
        }
    }

    /// <summary>
    /// Gets all registered handler factories for the given event type.
    /// </summary>
    internal static IReadOnlyList<Func<IServiceProvider, object, RaiseOptions, Task>>? GetHandlers(Type eventType)
    {
        if (!_handlers.TryGetValue(eventType, out var handlers))
            return null;
        lock (handlers)
        {
            return handlers.ToArray();
        }
    }

    /// <summary>
    /// Clears all registrations. Used for testing only.
    /// </summary>
    internal static void Clear()
    {
        _handlers.Clear();
    }
}
