using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.DtoDiscovery;

/// <summary>
/// Verifies the per-assembly event-preservation registrar (TRIM-007): the generator
/// discovers every concrete, accessible FactoryEventBase descendant declared in the
/// compilation and emits PreserveType/Register for the event and its nested DTO
/// graph — no handler attribute, no factory reference, no consumer action required.
/// </summary>
public class EventPreservationDiscoveryTests
{
    private static Microsoft.CodeAnalysis.GeneratorDriverRunResult Run(string source)
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);
        return runResult;
    }

    private static string AllTrees(Microsoft.CodeAnalysis.GeneratorDriverRunResult runResult)
        => string.Join("\n", runResult.GeneratedTrees.Select(t => t.GetText()?.ToString() ?? ""));

    /// <summary>
    /// Text of the event-preservation registrar tree (empty string when not emitted).
    /// The helper compilation is named TestAssembly, so the hint is
    /// "TestAssembly.NeatooEventPreservation.g.cs".
    /// </summary>
    private static string EventRegistrarTree(Microsoft.CodeAnalysis.GeneratorDriverRunResult runResult)
        => string.Join("\n", runResult.GeneratedTrees
            .Where(t => t.FilePath.EndsWith(".NeatooEventPreservation.g.cs"))
            .Select(t => t.GetText()?.ToString() ?? ""));

    [Fact]
    public void SubscribeOnlyEvent_PreserveTypeEmittedInEventRegistrar()
    {
        // No factory, no handler, no reference to the event anywhere — declaration
        // alone must produce preservation.
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public record StatusChangedEvent(int Id, string Status) : FactoryEventBase;
}
";
        var tree = EventRegistrarTree(Run(source));

        Assert.Contains("DtoConstructorRegistry.PreserveType<global::TestNamespace.StatusChangedEvent>()", tree);
        Assert.Contains("NeatooFactoryRegistrar(typeof(global::TestAssembly.NeatooEventPreservationRegistrar))", tree);
    }

    [Fact]
    public void EventNestedTypes_BothBucketsEmitted()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public record ShippingDetail(string Street, string City);

    public class PlainInfo
    {
        public string Note { get; set; }
    }

    public record OrderShippedEvent(int OrderId, ShippingDetail Detail) : FactoryEventBase
    {
        public PlainInfo Info { get; set; }
    }
}
";
        var tree = EventRegistrarTree(Run(source));

        Assert.Contains("DtoConstructorRegistry.PreserveType<global::TestNamespace.OrderShippedEvent>()", tree);
        Assert.Contains("DtoConstructorRegistry.PreserveType<global::TestNamespace.ShippingDetail>()", tree);
        Assert.Contains("DtoConstructorRegistry.Register<global::TestNamespace.PlainInfo>", tree);
    }

    [Fact]
    public void AbstractIntermediate_SkippedButConcreteDescendantWalked()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class PanelInfo
    {
        public string Name { get; set; }
    }

    public abstract record PanelEventBase : FactoryEventBase
    {
        public PanelInfo Panel { get; set; }
    }

    public record PanelOpenedEvent(int PanelId) : PanelEventBase;
}
";
        var tree = EventRegistrarTree(Run(source));

        Assert.Contains("DtoConstructorRegistry.PreserveType<global::TestNamespace.PanelOpenedEvent>()", tree);
        // Inherited property from the abstract intermediate is walked...
        Assert.Contains("DtoConstructorRegistry.Register<global::TestNamespace.PanelInfo>", tree);
        // ...but the abstract intermediate itself is never preserved.
        Assert.DoesNotContain("PanelEventBase>", tree);
    }

    [Fact]
    public void PrivateNestedEventRecord_SkippedByAccessibilityGate()
    {
        // The generated registrar is a separate file — it cannot legally reference a
        // private nested record (in-repo shape: FactoryEventCollectorTests' events).
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class Holder
    {
        private record HiddenEvent(int Id) : FactoryEventBase;
    }
}
";
        var all = AllTrees(Run(source));

        Assert.DoesNotContain("HiddenEvent", all);
        Assert.DoesNotContain("NeatooEventPreservationRegistrar", all);
    }

    [Fact]
    public void GenericEventRecord_Skipped()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public record PayloadEvent<T>(T Payload) : FactoryEventBase;
}
";
        var all = AllTrees(Run(source));

        Assert.DoesNotContain("PayloadEvent", all);
        Assert.DoesNotContain("NeatooEventPreservationRegistrar", all);
    }

    [Fact]
    public void NoEventsDeclared_NoRegistrarEmitted()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public record PlainRecord(int Id);

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal void Create() { }
    }
}
";
        var all = AllTrees(Run(source));

        Assert.DoesNotContain("NeatooEventPreservationRegistrar", all);
    }

    [Fact]
    public void SharedNestedTypeAcrossEvents_SingleEmission()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public record SharedDetail(string Value);

    public record FirstEvent(int Id, SharedDetail Detail) : FactoryEventBase;
    public record SecondEvent(int Id, SharedDetail Detail) : FactoryEventBase;
}
";
        var tree = EventRegistrarTree(Run(source));

        var emissions = System.Text.RegularExpressions.Regex.Matches(
            tree, @"DtoConstructorRegistry\.PreserveType<global::TestNamespace\.SharedDetail>\(\)");
        Assert.Single(emissions);
        Assert.Contains("PreserveType<global::TestNamespace.FirstEvent>()", tree);
        Assert.Contains("PreserveType<global::TestNamespace.SecondEvent>()", tree);
    }

    [Fact]
    public void EventWithParameterlessCtor_LandsInRegisterBucket()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public record MutableEvent : FactoryEventBase
    {
        public int Id { get; set; }
    }
}
";
        var tree = EventRegistrarTree(Run(source));

        Assert.Contains("DtoConstructorRegistry.Register<global::TestNamespace.MutableEvent>", tree);
        Assert.DoesNotContain("PreserveType<global::TestNamespace.MutableEvent>", tree);
    }
}
