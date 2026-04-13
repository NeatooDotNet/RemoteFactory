using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Static registry for <c>[FactoryEventHandler&lt;T&gt;]</c> handler factories.
/// Each assembly's generated <c>FactoryServiceRegistrar</c> adds its handlers here during DI setup.
/// The <see cref="FactoryEventsDispatcher"/> reads from this registry to dispatch events.
/// </summary>
public static class FactoryEventHandlerRegistry
{
    private static readonly ConcurrentDictionary<Type, List<HandlerEntry>> _handlers = new();

    private readonly struct HandlerEntry
    {
        public HandlerEntry(Type handlerClassType, Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> invoke)
        {
            HandlerClassType = handlerClassType;
            Invoke = invoke;
        }

        public Type HandlerClassType { get; }
        public Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> Invoke { get; }
    }

    /// <summary>
    /// Registers a handler factory for the given event type.
    /// Called by generated <c>FactoryServiceRegistrar</c> methods during DI setup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The handler factory is invoked with the caller's <see cref="IServiceProvider"/> — handlers
    /// resolve their <c>[Service]</c> dependencies from the caller's scope. The
    /// <see cref="CancellationToken"/> parameter is threaded from
    /// <see cref="IFactoryEvents.Raise{T}"/> to any handler parameter of that type.
    /// </para>
    /// <para>
    /// Registrations are deduplicated by the <c>(event type, handler class type)</c> pair
    /// so multiple DI container builds in a test run do not multiply registrations.
    /// </para>
    /// </remarks>
    public static void RegisterHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        Type handlerClassType,
        Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handlerFactory)
        where TEvent : FactoryEventBase
    {
        var list = _handlers.GetOrAdd(typeof(TEvent), _ => new List<HandlerEntry>());
        lock (list)
        {
            // Avoid duplicate registration from multiple DI container setups in tests.
            if (!list.Any(e => e.HandlerClassType == handlerClassType))
            {
                list.Add(new HandlerEntry(handlerClassType, handlerFactory));
            }
        }
    }

    /// <summary>
    /// Gets all registered handler factories for the given event type.
    /// </summary>
    internal static IReadOnlyList<Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task>>? GetHandlers(Type eventType)
    {
        if (!_handlers.TryGetValue(eventType, out var handlers))
            return null;
        lock (handlers)
        {
            return handlers.Select(h => h.Invoke).ToArray();
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
