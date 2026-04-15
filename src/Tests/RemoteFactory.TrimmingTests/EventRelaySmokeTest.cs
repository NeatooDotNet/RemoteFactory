using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// End-to-end trimming smoke test for the factory event relay path.
///
/// Exercises everything that must survive <c>PublishTrimmed=true</c>:
///   1. FactoryEventBase descendant remains constructible after trimming
///      (preserved by [DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]
///      with Inherited = true on FactoryEventBase).
///   2. FactoryEventTypeRegistry can resolve the descendant by FullName via the
///      runtime [FactoryEvent](Inherited=true) attribute scan.
///   3. FactoryEventDeserializer can construct the typed event from JSON.
///   4. A consumer-supplied IFactoryEventRelay receives the typed instance.
///
/// Failure modes this catches that attribute-presence unit tests cannot:
///   - Trimmer removes the record's parameterized constructor.
///   - Trimmer removes properties needed by RecordBypassConverterFactory.
///   - The runtime attribute scan misses descendants when their assembly metadata is stripped.
/// </summary>
public record TrimTestRelayEvent(int Id, string Message) : FactoryEventBase;

public sealed class CapturingRelay : IFactoryEventRelay
{
    public List<FactoryEventBase> Captured { get; } = new();

    public Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        Captured.AddRange(events);
        return Task.CompletedTask;
    }
}

public static class EventRelaySmokeTest
{
    public static void Run()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(EventRelaySmokeTest).Assembly);

        var capturing = new CapturingRelay();
        services.AddSingleton<IFactoryEventRelay>(capturing);

        using var sp = services.BuildServiceProvider();
        var serializer = sp.GetRequiredService<INeatooJsonSerializer>();

        // Round-trip: serialize a known event, then drive it through the deserializer
        // exactly as the dispatch site does on the client.
        var source = new TrimTestRelayEvent(42, "trim-smoke");
        var json = serializer.Serialize(source);
        if (string.IsNullOrEmpty(json))
        {
            System.Console.WriteLine("Event relay smoke FAILED: serializer produced null/empty JSON.");
            return;
        }

        var wire = new[]
        {
            new RelayedFactoryEvent
            {
                TypeFullName = typeof(TrimTestRelayEvent).FullName!,
                Json = json!,
            },
        };

        IReadOnlyList<FactoryEventBase> deserialized;
        try
        {
            deserialized = FactoryEventDeserializer.Deserialize(wire, serializer);
        }
        catch (UnknownFactoryEventTypeException ex)
        {
            System.Console.WriteLine($"Event relay smoke FAILED: registry could not resolve TrimTestRelayEvent post-trim. {ex.Message}");
            return;
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"Event relay smoke FAILED: deserialization threw {ex.GetType().Name}: {ex.Message}");
            return;
        }

        capturing.Relay(deserialized).GetAwaiter().GetResult();

        if (capturing.Captured.Count != 1)
        {
            System.Console.WriteLine($"Event relay smoke FAILED: expected 1 captured event, got {capturing.Captured.Count}.");
            return;
        }

        if (capturing.Captured[0] is not TrimTestRelayEvent rt)
        {
            System.Console.WriteLine($"Event relay smoke FAILED: captured event is {capturing.Captured[0].GetType().FullName}, not TrimTestRelayEvent.");
            return;
        }

        if (rt.Id != 42 || rt.Message != "trim-smoke")
        {
            System.Console.WriteLine($"Event relay smoke FAILED: round-trip values lost. Got Id={rt.Id}, Message=\"{rt.Message}\".");
            return;
        }

        System.Console.WriteLine("Event relay smoke PASSED: FactoryEventBase descendant survived trimming and round-tripped through registry+deserializer+relay.");
    }
}
