using System;
using System.Collections.Generic;
using System.Linq;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Deserializes a batch of wire-format <see cref="RelayedFactoryEvent"/> entries into
/// concrete <see cref="FactoryEventBase"/> instances using <see cref="FactoryEventTypeRegistry"/>
/// and an <see cref="INeatooJsonSerializer"/>.
///
/// Fails loud: throws <see cref="UnknownFactoryEventTypeException"/> when any event's
/// <c>TypeFullName</c> cannot be resolved. The caller (relay dispatch site) catches this
/// and logs it — it does not propagate to the factory caller.
/// </summary>
internal static class FactoryEventDeserializer
{
    public static IReadOnlyList<FactoryEventBase> Deserialize(
        IReadOnlyList<RelayedFactoryEvent> events,
        INeatooJsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(serializer);

        if (events.Count == 0)
        {
            return Array.Empty<FactoryEventBase>();
        }

        var batchTypeNames = events.Select(e => e.TypeFullName).ToArray();
        var result = new FactoryEventBase[events.Count];

        for (var i = 0; i < events.Count; i++)
        {
            var relayed = events[i];
            var type = FactoryEventTypeRegistry.Resolve(relayed.TypeFullName)
                ?? throw new UnknownFactoryEventTypeException(relayed.TypeFullName, batchTypeNames);

            var deserialized = serializer.Deserialize(relayed.Json, type);
            if (deserialized is not FactoryEventBase evt)
            {
                throw new InvalidOperationException(
                    $"Type '{relayed.TypeFullName}' resolved but is not a FactoryEventBase descendant (got {deserialized?.GetType().FullName ?? "null"}).");
            }
            result[i] = evt;
        }

        return result;
    }
}
