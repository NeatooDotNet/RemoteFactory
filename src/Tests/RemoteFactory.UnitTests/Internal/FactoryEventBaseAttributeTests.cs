using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// Verifies FactoryEventBase carries [FactoryEvent] (inherited at runtime, making
/// every descendant discoverable by <c>FactoryEventTypeRegistry</c>) and
/// [DynamicallyAccessedMembers]. Note: the DAM annotation does NOT preserve
/// descendants' members under IL trimming (DAM does not flow to derived types in
/// ILLink) — trimming preservation comes from the generator-emitted per-assembly
/// event-preservation registrar; see FactoryEventBase's doc comment.
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
