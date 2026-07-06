using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.DtoDiscovery;

/// <summary>
/// Verifies entity property-graph DTO discovery (TRIM-002): [Factory] class types
/// walk their own public property graph and emit preservation for reachable DTOs
/// in their own registrar — the entity itself is never bucketed, and factory-typed
/// properties are skipped (each [Factory] class's own registrar owns its graph).
/// </summary>
public class EntityPropertyDtoDiscoveryTests
{
    private static Microsoft.CodeAnalysis.GeneratorDriverRunResult Run(string source)
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);
        return runResult;
    }

    private static string AllTrees(Microsoft.CodeAnalysis.GeneratorDriverRunResult runResult)
        => string.Join("\n", runResult.GeneratedTrees.Select(t => t.GetText()?.ToString() ?? ""));

    /// <summary>
    /// Text of the generated tree(s) for exactly the given factory hint — anchored to
    /// the ".{hint}.g.cs" file-name shape so a hint can never substring-match another
    /// factory's tree (e.g. "EntityFactory" vs "ParentEntityFactory"). Lets assertions
    /// distinguish WHICH factory's registrar carries an emission.
    /// </summary>
    private static string FactoryTree(Microsoft.CodeAnalysis.GeneratorDriverRunResult runResult, string factoryFileHint)
        => string.Join("\n", runResult.GeneratedTrees
            .Where(t => t.FilePath.EndsWith($".{factoryFileHint}.g.cs"))
            .Select(t => t.GetText()?.ToString() ?? ""));

    [Fact]
    public void EntityWithDtoProperty_RegisterEmittedInEntityRegistrar()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class TreatmentInfo
    {
        public string Text { get; set; }
    }

    [Factory]
    public class TreatmentContext
    {
        public TreatmentInfo Info { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "TreatmentContextFactory");

        Assert.Contains("DtoConstructorRegistry.Register<global::TestNamespace.TreatmentInfo>", tree);
    }

    [Fact]
    public void EntityWithRecordProperty_PreserveTypeEmitted()
    {
        // The zTreatment TreatmentBanner shape: a positional record carried only
        // as a property of an [Execute]-opened aggregate.
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public record TreatmentBanner(string Text, string Severity);

    [Factory]
    public class TreatmentContext
    {
        public TreatmentBanner Banner { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "TreatmentContextFactory");

        Assert.Contains("DtoConstructorRegistry.PreserveType<global::TestNamespace.TreatmentBanner>()", tree);
    }

    [Fact]
    public void EntityWithDtoCollectionProperty_ElementDiscovered()
    {
        // The zTreatment DashboardContactResult shape: List<T> property on a
        // factory entity.
        var source = @"
using Neatoo.RemoteFactory;
using System.Collections.Generic;

namespace TestNamespace
{
    public record DashboardContactResult(int Id, string Name);

    [Factory]
    public class PatientSearchQuery
    {
        public List<DashboardContactResult> Results { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "PatientSearchQueryFactory");

        Assert.Contains("DtoConstructorRegistry.PreserveType<global::TestNamespace.DashboardContactResult>()", tree);
    }

    [Fact]
    public void DtoNestedUnderEntityProperty_BothLevelsDiscovered()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public record NestedBanner(string Text);

    public class CarriedInfo
    {
        public NestedBanner Banner { get; set; }
    }

    [Factory]
    public class Aggregate
    {
        public CarriedInfo Info { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "AggregateFactory");

        Assert.Contains("DtoConstructorRegistry.Register<global::TestNamespace.CarriedInfo>", tree);
        Assert.Contains("DtoConstructorRegistry.PreserveType<global::TestNamespace.NestedBanner>()", tree);
    }

    [Fact]
    public void ChildEntityProperty_CoveredByChildRegistrarNotParent()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class ChildInfo
    {
        public string Note { get; set; }
    }

    [Factory]
    public class ChildEntity
    {
        public ChildInfo Info { get; set; }

        [Create]
        internal void Create() { }
    }

    [Factory]
    public class ParentEntity
    {
        public ChildEntity Child { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var runResult = Run(source);
        var parentTree = FactoryTree(runResult, "ParentEntityFactory");
        var childTree = FactoryTree(runResult, "ChildEntityFactory");

        // The parent neither buckets the child entity nor descends into it.
        Assert.DoesNotContain("DtoConstructorRegistry.Register<global::TestNamespace.ChildEntity>", parentTree);
        Assert.DoesNotContain("global::TestNamespace.ChildInfo", parentTree);

        // The child's own registrar owns its graph.
        Assert.Contains("DtoConstructorRegistry.Register<global::TestNamespace.ChildInfo>", childTree);
    }

    [Fact]
    public void InterfaceFactory_NoEntityPropertyWalk()
    {
        // Interface factories are service contracts; the implementation class is a
        // stateless service and gets no registrar/walk — its properties are out of
        // reach by design (TRIM-002 plan review B1).
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class ServiceStateDto
    {
        public int Id { get; set; }
    }

    [Factory]
    public interface ILookupService
    {
        [Remote]
        Task<int> CountAsync();
    }

    public class LookupService : ILookupService
    {
        public ServiceStateDto State { get; set; }
        public Task<int> CountAsync() => Task.FromResult(0);
    }
}
";
        var all = AllTrees(Run(source));

        Assert.DoesNotContain("global::TestNamespace.ServiceStateDto", all);
    }

    [Fact]
    public void SystemsPrefixedConsumerNamespace_NotExcluded()
    {
        // Segment-match hardening: "Systems.Domain" is a consumer namespace, not a
        // framework one, and must be discovered (TRIM-001 code-review callout).
        var source = @"
using Neatoo.RemoteFactory;

namespace Systems.Domain
{
    public class SystemsDto
    {
        public int Id { get; set; }
    }
}

namespace TestNamespace
{
    using Systems.Domain;

    [Factory]
    public class Aggregate
    {
        public SystemsDto Info { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "AggregateFactory");

        Assert.Contains("DtoConstructorRegistry.Register<global::Systems.Domain.SystemsDto>", tree);
    }

    [Fact]
    public void EntityBaseClassDtoProperty_Discovered()
    {
        // Base-aggregate pattern: the DTO-carrying property lives on the entity's
        // base class; the walk includes the inherited chain.
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class AuditInfo
    {
        public string ChangedBy { get; set; }
    }

    public abstract class AggregateBase
    {
        public AuditInfo Audit { get; set; }
    }

    [Factory]
    public class ConcreteAggregate : AggregateBase
    {
        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "ConcreteAggregateFactory");

        Assert.Contains("DtoConstructorRegistry.Register<global::TestNamespace.AuditInfo>", tree);
    }

    [Fact]
    public void RecordEntitySelfWalk_CarriedDtoDiscovered()
    {
        // [Factory] record aggregates pass the same non-interface/non-static gate as
        // class aggregates — the entity walk must run for them too.
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class CarriedNote
    {
        public string Text { get; set; }
    }

    [Factory]
    public record ReportAggregate
    {
        public CarriedNote Note { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "ReportAggregateFactory");

        Assert.Contains("DtoConstructorRegistry.Register<global::TestNamespace.CarriedNote>", tree);
    }

    [Fact]
    public void SystemFrameworkTypeProperty_StillExcluded()
    {
        // The other half of the segment-match hardening: real System.* framework
        // types (which have public parameterless ctors and would otherwise register)
        // stay excluded.
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public class Aggregate
    {
        public System.Text.StringBuilder Buffer { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "AggregateFactory");

        Assert.DoesNotContain("StringBuilder", tree);
    }

    [Fact]
    public void DtoInBothSignatureAndEntityProperty_SingleEmission()
    {
        // Cross-walk dedupe: the same DTO reachable via a factory method signature
        // AND via the entity's property graph emits exactly once in the registrar.
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class SharedDto
    {
        public int Id { get; set; }
    }

    [Factory]
    public class Aggregate
    {
        public SharedDto Info { get; set; }

        [Create]
        internal void Create() { }

        [Remote]
        [Execute]
        internal Task<SharedDto> _Load() => Task.FromResult(Info);
    }
}
";
        var tree = FactoryTree(Run(source), "AggregateFactory");

        var emissions = System.Text.RegularExpressions.Regex.Matches(
            tree, @"DtoConstructorRegistry\.Register<global::TestNamespace\.SharedDto>");
        Assert.Single(emissions);
    }

    [Fact]
    public void DtoCycleUnderEntity_TerminatesAndRegistersOnce()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class DtoA
    {
        public DtoB B { get; set; }
    }

    public class DtoB
    {
        public DtoA A { get; set; }
    }

    [Factory]
    public class Aggregate
    {
        public DtoA Root { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "AggregateFactory");

        var aEmissions = System.Text.RegularExpressions.Regex.Matches(
            tree, @"DtoConstructorRegistry\.Register<global::TestNamespace\.DtoA>");
        var bEmissions = System.Text.RegularExpressions.Regex.Matches(
            tree, @"DtoConstructorRegistry\.Register<global::TestNamespace\.DtoB>");

        Assert.Single(aEmissions);
        Assert.Single(bEmissions);
    }

    [Fact]
    public void LazyLoadDtoProperty_InnerDtoDiscoveredThroughValue()
    {
        // Accepted behavior (TRIM-002 plan review B2): descent reaches T through
        // LazyLoad<T>.Value, so the inner DTO is preserved; LazyLoad<T> itself also
        // lands in the Register bucket — redundant (LazyLoadJsonConverterFactory
        // constructs it in compiled generic code) but harmless and idempotent.
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class DeferredInfo
    {
        public string Text { get; set; }
    }

    [Factory]
    public class Aggregate
    {
        public LazyLoad<DeferredInfo> Info { get; set; }

        [Create]
        internal void Create() { }
    }
}
";
        var tree = FactoryTree(Run(source), "AggregateFactory");

        Assert.Contains("DtoConstructorRegistry.Register<global::TestNamespace.DeferredInfo>", tree);
    }
}
