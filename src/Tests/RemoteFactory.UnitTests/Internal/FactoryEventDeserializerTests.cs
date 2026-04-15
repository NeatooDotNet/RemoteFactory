using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Unit tests for FactoryEventDeserializer — the bridge from wire-format
/// RelayedFactoryEvent entries to concrete FactoryEventBase instances.
/// </summary>
public class FactoryEventDeserializerTests
{
    public record DeserializerProbeEvent(int Id, string Name) : FactoryEventBase;
    public record OtherDeserializerProbeEvent(decimal Amount) : FactoryEventBase;

    private static INeatooJsonSerializer BuildSerializer()
    {
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Server, typeof(FactoryEventDeserializerTests).Assembly);
        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<INeatooJsonSerializer>();
    }

    [Fact]
    public void Deserialize_SingleEvent_RoundTripsThroughRegistry()
    {
        var serializer = BuildSerializer();
        var source = new DeserializerProbeEvent(42, "hello");

        var json = serializer.Serialize(source)!;
        var wire = new[] { new RelayedFactoryEvent { TypeFullName = typeof(DeserializerProbeEvent).FullName!, Json = json } };

        var result = FactoryEventDeserializer.Deserialize(wire, serializer);

        var evt = Assert.IsType<DeserializerProbeEvent>(Assert.Single(result));
        Assert.Equal(42, evt.Id);
        Assert.Equal("hello", evt.Name);
    }

    [Fact]
    public void Deserialize_MultipleEvents_PreservesOrder()
    {
        var serializer = BuildSerializer();
        var wire = new[]
        {
            new RelayedFactoryEvent { TypeFullName = typeof(DeserializerProbeEvent).FullName!, Json = serializer.Serialize(new DeserializerProbeEvent(1, "a"))! },
            new RelayedFactoryEvent { TypeFullName = typeof(OtherDeserializerProbeEvent).FullName!, Json = serializer.Serialize(new OtherDeserializerProbeEvent(9.99m))! },
            new RelayedFactoryEvent { TypeFullName = typeof(DeserializerProbeEvent).FullName!, Json = serializer.Serialize(new DeserializerProbeEvent(2, "b"))! },
        };

        var result = FactoryEventDeserializer.Deserialize(wire, serializer);

        Assert.Collection(result,
            e => Assert.Equal(1, Assert.IsType<DeserializerProbeEvent>(e).Id),
            e => Assert.Equal(9.99m, Assert.IsType<OtherDeserializerProbeEvent>(e).Amount),
            e => Assert.Equal(2, Assert.IsType<DeserializerProbeEvent>(e).Id));
    }

    [Fact]
    public void Deserialize_EmptyBatch_ReturnsEmptyArray()
    {
        var serializer = BuildSerializer();
        var result = FactoryEventDeserializer.Deserialize(Array.Empty<RelayedFactoryEvent>(), serializer);
        Assert.Empty(result);
    }

    [Fact]
    public void Deserialize_UnknownTypeFullName_ThrowsUnknownFactoryEventTypeException()
    {
        var serializer = BuildSerializer();
        var wire = new[]
        {
            new RelayedFactoryEvent { TypeFullName = "TotallyMissing.NotARealType", Json = "{}" },
        };

        var ex = Assert.Throws<UnknownFactoryEventTypeException>(() => FactoryEventDeserializer.Deserialize(wire, serializer));
        Assert.Equal("TotallyMissing.NotARealType", ex.UnresolvedTypeFullName);
        Assert.Contains("TotallyMissing.NotARealType", ex.BatchTypeFullNames);
    }

    [Fact]
    public void Deserialize_UnknownTypeInMiddleOfBatch_PreservesAllBatchNamesForDiagnostics()
    {
        var serializer = BuildSerializer();
        var wire = new[]
        {
            new RelayedFactoryEvent { TypeFullName = typeof(DeserializerProbeEvent).FullName!, Json = serializer.Serialize(new DeserializerProbeEvent(1, "a"))! },
            new RelayedFactoryEvent { TypeFullName = "TotallyMissing.Middle", Json = "{}" },
            new RelayedFactoryEvent { TypeFullName = typeof(OtherDeserializerProbeEvent).FullName!, Json = serializer.Serialize(new OtherDeserializerProbeEvent(5m))! },
        };

        var ex = Assert.Throws<UnknownFactoryEventTypeException>(() => FactoryEventDeserializer.Deserialize(wire, serializer));
        Assert.Equal("TotallyMissing.Middle", ex.UnresolvedTypeFullName);
        Assert.Equal(3, ex.BatchTypeFullNames.Count);
    }
}
