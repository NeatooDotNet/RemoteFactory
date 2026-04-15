using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Verifies FactoryEventBase carries [FactoryEvent] and [DynamicallyAccessedMembers]
/// with Inherited = true, so every descendant is discoverable by
/// <c>FactoryEventTypeRegistry</c> and preserved through IL trimming.
/// </summary>
public class FactoryEventBaseAttributeTests
{
    private record ExampleEvent(int Id) : FactoryEventBase;
    private record NestedDerivedEvent(int Id, string Name) : ExampleEvent(Id);

    [Fact]
    public void FactoryEventBase_CarriesFactoryEventAttribute()
    {
        var attr = typeof(FactoryEventBase).GetCustomAttribute<FactoryEventAttribute>(inherit: false);
        Assert.NotNull(attr);
    }

    [Fact]
    public void FactoryEventBase_CarriesDynamicallyAccessedMembers_PublicCtorsAndProperties()
    {
        var attr = typeof(FactoryEventBase).GetCustomAttribute<DynamicallyAccessedMembersAttribute>(inherit: false);
        Assert.NotNull(attr);
        Assert.Equal(
            DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties,
            attr!.MemberTypes);
    }

    [Fact]
    public void FactoryEventAttribute_IsInheritedByDescendants()
    {
        var attr = typeof(ExampleEvent).GetCustomAttribute<FactoryEventAttribute>(inherit: true);
        Assert.NotNull(attr);
    }

    [Fact]
    public void FactoryEventAttribute_IsInheritedTransitivelyThroughMultipleDerivations()
    {
        var attr = typeof(NestedDerivedEvent).GetCustomAttribute<FactoryEventAttribute>(inherit: true);
        Assert.NotNull(attr);
    }

    [Fact]
    public void FactoryEventAttribute_UsageIsClassOnly_Inherited_SingleInstance()
    {
        var usage = typeof(FactoryEventAttribute).GetCustomAttribute<AttributeUsageAttribute>(inherit: false);
        Assert.NotNull(usage);
        Assert.Equal(AttributeTargets.Class, usage!.ValidOn);
        Assert.True(usage.Inherited);
        Assert.False(usage.AllowMultiple);
    }
}
