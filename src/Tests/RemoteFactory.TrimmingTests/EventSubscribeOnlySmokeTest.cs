using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// The event record under test (TRIM-003). Its ONLY client-side static reference is
/// the generic <c>Subscribe&lt;TrimSubscribeOnlyEvent&gt;(...)</c> call site below —
/// never constructed, never referenced via <c>typeof</c>, no
/// <c>[FactoryEventHandler&lt;T&gt;]</c> anywhere. Trimming survival must come solely
/// from the inherited <c>[FactoryEvent]</c> +
/// <c>[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]</c>
/// annotations on <see cref="FactoryEventBase"/> (shipped v1.4.0).
/// </summary>
public record TrimSubscribeOnlyEvent(int Id, string Note) : FactoryEventBase;

/// <summary>
/// Consumer-style relay aggregator — the zTreatment client shape: typed
/// subscriptions registered through a generic method, deserialized relay batches
/// dispatched by runtime type.
/// </summary>
public sealed class SubscribingRelay : IFactoryEventRelay
{
    private readonly Dictionary<Type, List<Action<FactoryEventBase>>> _subscriptions = new();

    // KNOWN GAP (TRIM-003 finding, fix = TRIM-007): without [DynamicallyAccessedMembers]
    // on TEvent, the trimmed client strips the event record's ctor — the inherited
    // annotation on FactoryEventBase does NOT flow to derived types under ILLink.
    // Adding DAM(PublicConstructors | PublicProperties) here makes the check pass
    // (verified 2026-07-07); this is the unannotated consumer shape on purpose.
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : FactoryEventBase
    {
        if (!_subscriptions.TryGetValue(typeof(TEvent), out var list))
        {
            list = new List<Action<FactoryEventBase>>();
            _subscriptions[typeof(TEvent)] = list;
        }

        list.Add(evt => handler((TEvent)evt));
    }

    public Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        foreach (var evt in events)
        {
            if (_subscriptions.TryGetValue(evt.GetType(), out var list))
            {
                foreach (var handler in list)
                {
                    handler(evt);
                }
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// End-to-end trimming verification for the subscribe-only consumer shape (TRIM-003).
/// The existing EventRelaySmokeTest cannot settle this — it constructs its event and
/// uses typeof(), statically rooting exactly the metadata under test. Here the wire
/// entry's TypeFullName is a string literal, so resolution goes through the runtime
/// FactoryEventTypeRegistry attribute scan, and member preservation depends entirely
/// on the FactoryEventBase inherited annotations.
/// </summary>
public static class EventSubscribeOnlySmokeTest
{
    public static bool Run()
    {
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(EventSubscribeOnlySmokeTest).Assembly);

        using var sp = services.BuildServiceProvider();
        var serializer = sp.GetRequiredService<INeatooJsonSerializer>();

        var relay = new SubscribingRelay();

        // THE shape under test: this generic subscription is the event type's only
        // static reference anywhere in the client.
        TrimSubscribeOnlyEvent? received = null;
        relay.Subscribe<TrimSubscribeOnlyEvent>(evt => received = evt);

        var wire = new[]
        {
            new RelayedFactoryEvent
            {
                // String literal on purpose — typeof(...).FullName would root the
                // type outside the consumer shape.
                TypeFullName = "RemoteFactory.TrimmingTests.TrimSubscribeOnlyEvent",
                Json = "{\"Id\":42,\"Note\":\"subscribe-only\"}",
            },
        };

        IReadOnlyList<FactoryEventBase> deserialized;
        try
        {
            deserialized = FactoryEventDeserializer.Deserialize(wire, serializer);
        }
        catch (UnknownFactoryEventTypeException ex)
        {
            Console.WriteLine($"Subscribe-only event smoke FAILED: registry could not resolve the event type post-trim (type stripped?). {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Subscribe-only event smoke FAILED: deserialization threw {ex.GetType().Name}: {ex.Message}");
            return false;
        }

        try
        {
            relay.Relay(deserialized).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Subscribe-only event smoke FAILED: dispatch threw {ex.GetType().Name}: {ex.Message}");
            return false;
        }

        if (received is null)
        {
            Console.WriteLine("Subscribe-only event smoke FAILED: typed subscriber did not receive the event.");
            return false;
        }

        if (received.Id != 42 || received.Note != "subscribe-only")
        {
            Console.WriteLine($"Subscribe-only event smoke FAILED: round-trip values lost. Got Id={received.Id}, Note=\"{received.Note}\".");
            return false;
        }

        Console.WriteLine("Subscribe-only event smoke PASSED: FactoryEventBase inherited annotations preserved a subscribe-only event record through trimming.");
        return true;
    }
}
