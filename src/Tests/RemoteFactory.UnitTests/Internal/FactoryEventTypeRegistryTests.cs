using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Verifies the runtime scan that maps TypeFullName to FactoryEventBase descendants.
/// </summary>
public class FactoryEventTypeRegistryTests
{
    // Concrete test-local event types used by Resolve tests.
    public record RegistryProbeEventA(int X) : FactoryEventBase;
    public record RegistryProbeEventB(string Y) : FactoryEventBase;

    public abstract record AbstractRegistryProbeEvent : FactoryEventBase;

    [Fact]
    public void Resolve_ConcreteDescendant_ReturnsType()
    {
        var type = FactoryEventTypeRegistry.Resolve(typeof(RegistryProbeEventA).FullName!);
        Assert.Equal(typeof(RegistryProbeEventA), type);
    }

    [Fact]
    public void Resolve_DifferentDescendants_AreDiscoveredIndependently()
    {
        var a = FactoryEventTypeRegistry.Resolve(typeof(RegistryProbeEventA).FullName!);
        var b = FactoryEventTypeRegistry.Resolve(typeof(RegistryProbeEventB).FullName!);
        Assert.Equal(typeof(RegistryProbeEventA), a);
        Assert.Equal(typeof(RegistryProbeEventB), b);
    }

    [Fact]
    public void Resolve_AbstractDescendant_IsSkipped()
    {
        // Abstract types must be filtered — they cannot be instantiated on deserialization.
        var type = FactoryEventTypeRegistry.Resolve(typeof(AbstractRegistryProbeEvent).FullName!);
        Assert.Null(type);
    }

    [Fact]
    public void Resolve_UnknownName_ReturnsNull()
    {
        var type = FactoryEventTypeRegistry.Resolve("TestNs.DoesNotExist.EvenAfterRescan");
        Assert.Null(type);
    }

    [Fact]
    public void Snapshot_CachesResolvedTypes()
    {
        var first = FactoryEventTypeRegistry.Snapshot();
        var second = FactoryEventTypeRegistry.Snapshot();
        // EnsureCache returns the same underlying dictionary reference until Reset().
        Assert.Same(first, second);
    }
}
