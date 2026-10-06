using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

// =============================================================================
// DICT-001 — two DTO-walk blind spots, each with a red-first trimmed check.
//
// Both shapes put a DTO on the wire that the generator's DTO walk could not see,
// so nothing told the trimmer to keep its constructor, and the reflection
// serializer failed on the client with a no-constructor error:
//
//   1. A dictionary VALUE type. The walk unwrapped a collection through its single
//      IEnumerable<T> argument, so Dictionary<K,V> became KeyValuePair<K,V> — a
//      System type the walk rejects — and neither K nor V was ever visited.
//   2. A type reachable only through a PUBLIC FIELD. The serializer runs with
//      IncludeFields = true, so fields are on the wire, but the walk visited
//      properties only.
//
// DictionaryAndFieldDtoSmokeTest deserializes each from a JSON literal. Neither DTO
// below may be constructed anywhere a client can reach: a client-reachable `new`
// roots the constructor on its own and turns the check green for the wrong reason.
// =============================================================================

/// <summary>
/// Dictionary value carried only by <see cref="TrimDictCarrier.Locations"/>. A plain
/// sealed class with an implicit parameterless constructor and primitive properties —
/// the zTreatment <c>LocationAssessment</c> shape that failed on its 1.5.0 → 1.9.0
/// upgrade. Its ONLY construction site is the async <c>[Remote]</c> body in
/// <see cref="TrimDictCarrier"/>, which is trimmed from a client publish (TRIM-009),
/// so on the client nothing but the generator's DTO walk can root its constructor.
/// </summary>
public sealed class TrimDictValue
{
    public string? Region { get; set; }
    public int Severity { get; set; }
}

/// <summary>
/// Plain DTO reachable as a property of <see cref="TrimDictCarrier"/>, so the entity
/// walk registers it. It carries <see cref="TrimFieldCarried"/> in a public field.
/// </summary>
public sealed class TrimFieldHost
{
#pragma warning disable CA1051 // Visible instance field: the public field IS the shape under test.
    public TrimFieldCarried? Carried;
#pragma warning restore CA1051
}

/// <summary>
/// DTO reachable ONLY through the public field <see cref="TrimFieldHost.Carried"/>.
/// Never constructed anywhere in this assembly.
/// </summary>
public sealed class TrimFieldCarried
{
    public string? Text { get; set; }
}

/// <summary>
/// <c>[Factory]</c> carrier for the DICT-001 shapes. Its registrar holder,
/// <c>NeatooClassFactoryRegistrar_TrimDictCarrier</c>, is a positive control in
/// <c>verify-trimmed.sh</c>: the entity walk emits into this registrar, so a check
/// that goes green while the holder is missing would be green for the wrong reason.
/// </summary>
/// <remarks>
/// The fetch body is the consumer shape on purpose. Before v1.7.0 an async
/// <c>[Remote]</c> body leaked to trimmed clients, and the <c>new TrimDictValue()</c>
/// inside it would have rooted the constructor by accident — which is exactly how the
/// dictionary gap stayed hidden in zTreatment until TRIM-009 removed the leak. If that
/// leak ever returned, the dictionary check would go green without the walk doing its
/// job. It takes no server-only service, so it adds nothing to the absence gate.
/// </remarks>
[Factory]
public class TrimDictCarrier
{
#pragma warning disable CA2227 // Settable collection property: the consumer shape under test, and the entity's serializer sets it.
    public Dictionary<string, TrimDictValue>? Locations { get; set; }
#pragma warning restore CA2227

    public TrimFieldHost? Host { get; set; }

    [Remote]
    [Fetch]
    internal async Task FetchAsync(string key)
    {
        await Task.CompletedTask;
        Locations = new Dictionary<string, TrimDictValue>
        {
            [key] = new TrimDictValue { Region = key, Severity = 1 },
        };
    }
}
