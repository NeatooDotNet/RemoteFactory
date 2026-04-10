using System.Collections.Concurrent;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Static registry for relay event dispatch.
/// Maps handler class types to their event type names and dispatch/deserialization delegates.
/// Populated by generated FactoryServiceRegistrar methods during DI setup.
/// </summary>
public static class FactoryEventRelayRegistry
{
    private static readonly ConcurrentDictionary<Type, List<(string eventTypeName, Func<object, object, Task> dispatch, Func<string, INeatooJsonSerializer, object> deserialize)>> _handlerTypes = new();

    /// <summary>
    /// Registers a handler class type with its event type name, dispatch delegate, and deserializer.
    /// Called by generated code for each [FactoryEventHandler&lt;T&gt;] with an instance handler method.
    /// </summary>
    public static void RegisterHandlerType(
        Type handlerType,
        string eventTypeName,
        Func<object, object, Task> dispatch,
        Func<string, INeatooJsonSerializer, object> deserializer)
    {
        var entries = _handlerTypes.GetOrAdd(handlerType, _ => new List<(string, Func<object, object, Task>, Func<string, INeatooJsonSerializer, object>)>());
        lock (entries)
        {
            // Avoid duplicate registration from multiple DI container setups in tests
            if (!entries.Any(e => e.eventTypeName == eventTypeName))
            {
                entries.Add((eventTypeName, dispatch, deserializer));
            }
        }
    }

    /// <summary>
    /// Gets the relay entries for a handler type.
    /// </summary>
    internal static IReadOnlyList<(string eventTypeName, Func<object, object, Task> dispatch, Func<string, INeatooJsonSerializer, object> deserialize)>? GetHandlerEntries(Type handlerType)
    {
        if (_handlerTypes.TryGetValue(handlerType, out var entries))
        {
            lock (entries)
            {
                return entries.ToArray();
            }
        }
        return null;
    }

    /// <summary>
    /// Gets a deserializer for a given event type name (from any registered handler).
    /// </summary>
    internal static Func<string, INeatooJsonSerializer, object>? GetDeserializer(string eventTypeName)
    {
        foreach (var entries in _handlerTypes.Values)
        {
            lock (entries)
            {
                var match = entries.FirstOrDefault(e => e.eventTypeName == eventTypeName);
                if (match.deserialize != null)
                    return match.deserialize;
            }
        }
        return null;
    }

    /// <summary>
    /// Clears all registrations. Used for testing only.
    /// </summary>
    internal static void Clear()
    {
        _handlerTypes.Clear();
    }
}
