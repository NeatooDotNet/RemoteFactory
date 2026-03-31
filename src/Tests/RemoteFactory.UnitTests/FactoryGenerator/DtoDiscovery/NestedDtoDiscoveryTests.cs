using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.DtoDiscovery;

/// <summary>
/// Verifies that the generator recursively discovers nested DTO types through
/// property walking and emits DtoConstructorRegistry.Register calls for each.
/// </summary>
public class NestedDtoDiscoveryTests
{
    /// <summary>
    /// Helper to extract all DtoConstructorRegistry.Register type arguments from generated source.
    /// Returns fully-qualified type names (e.g., "global::TestNamespace.ChildDto").
    /// </summary>
    private static HashSet<string> GetRegisteredDtoTypes(string source)
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var allGeneratedSource = string.Join("\n",
            runResult.GeneratedTrees.Select(t => t.GetText()?.ToString() ?? ""));

        var registered = new HashSet<string>();
        // Match: DtoConstructorRegistry.Register<TYPE>(() => new TYPE());
        var matches = System.Text.RegularExpressions.Regex.Matches(
            allGeneratedSource,
            @"DtoConstructorRegistry\.Register<(.+?)>\(\(\)");

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            registered.Add(match.Groups[1].Value);
        }

        return registered;
    }

    #region TS-001: Single-level nested DTO (BR-NEST-001)

    [Fact]
    public void SingleLevelNestedDto_BothDiscovered()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class ChildDto
    {
        public int Id { get; set; }
    }

    public class ParentDto
    {
        public int Id { get; set; }
        public ChildDto Child { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal ParentDto Create() => new ParentDto();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.ParentDto", registered);
        Assert.Contains("global::TestNamespace.ChildDto", registered);
    }

    #endregion

    #region TS-002: Collection property unwrapping - List (BR-NEST-002)

    [Fact]
    public void CollectionProperty_ListChildDto_BothDiscovered()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Collections.Generic;

namespace TestNamespace
{
    public class ChildDto
    {
        public int Id { get; set; }
    }

    public class ParentDto
    {
        public int Id { get; set; }
        public List<ChildDto> Items { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal ParentDto Create() => new ParentDto();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.ParentDto", registered);
        Assert.Contains("global::TestNamespace.ChildDto", registered);
    }

    #endregion

    #region TS-003: Array property unwrapping (BR-NEST-002)

    [Fact]
    public void CollectionProperty_ArrayChildDto_BothDiscovered()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class ChildDto
    {
        public int Id { get; set; }
    }

    public class ParentDto
    {
        public int Id { get; set; }
        public ChildDto[] Items { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal ParentDto Create() => new ParentDto();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.ParentDto", registered);
        Assert.Contains("global::TestNamespace.ChildDto", registered);
    }

    #endregion

    #region TS-004: IReadOnlyList property unwrapping (BR-NEST-002)

    [Fact]
    public void CollectionProperty_IReadOnlyListChildDto_BothDiscovered()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Collections.Generic;

namespace TestNamespace
{
    public class ChildDto
    {
        public int Id { get; set; }
    }

    public class ParentDto
    {
        public int Id { get; set; }
        public IReadOnlyList<ChildDto> Items { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal ParentDto Create() => new ParentDto();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.ParentDto", registered);
        Assert.Contains("global::TestNamespace.ChildDto", registered);
    }

    #endregion

    #region TS-005: Nullable property unwrapping (BR-NEST-003)

    [Fact]
    public void NullableProperty_ChildDto_BothDiscovered()
    {
        var source = @"
#nullable enable
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class ChildDto
    {
        public int Id { get; set; }
    }

    public class ParentDto
    {
        public int Id { get; set; }
        public ChildDto? OptionalChild { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal ParentDto Create() => new ParentDto();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.ParentDto", registered);
        Assert.Contains("global::TestNamespace.ChildDto", registered);
    }

    #endregion

    #region TS-006: Deep nesting - three levels (BR-NEST-004)

    [Fact]
    public void DeepNesting_ThreeLevels_AllDiscovered()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class GrandchildDto
    {
        public int Id { get; set; }
    }

    public class ChildDto
    {
        public int Id { get; set; }
        public GrandchildDto Grandchild { get; set; }
    }

    public class ParentDto
    {
        public int Id { get; set; }
        public ChildDto Child { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal ParentDto Create() => new ParentDto();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.ParentDto", registered);
        Assert.Contains("global::TestNamespace.ChildDto", registered);
        Assert.Contains("global::TestNamespace.GrandchildDto", registered);
    }

    #endregion

    #region TS-007: Circular reference (BR-NEST-005)

    [Fact]
    public void CircularReference_AB_BothDiscovered()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class DtoA
    {
        public int Id { get; set; }
        public DtoB Other { get; set; }
    }

    public class DtoB
    {
        public int Id { get; set; }
        public DtoA Back { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal DtoA Create() => new DtoA();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.DtoA", registered);
        Assert.Contains("global::TestNamespace.DtoB", registered);
    }

    #endregion

    #region TS-008: Self-reference (BR-NEST-005)

    [Fact]
    public void SelfReference_TreeNode_Discovered()
    {
        var source = @"
#nullable enable
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class TreeNode
    {
        public int Id { get; set; }
        public TreeNode? Parent { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal TreeNode Create() => new TreeNode();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.TreeNode", registered);
    }

    #endregion

    #region TS-009: [Factory] property excluded (BR-NEST-006)

    [Fact]
    public void FactoryProperty_Excluded()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class FactoryEntity
    {
        public int Id { get; set; }

        [Create]
        internal void Create() { }
    }

    public class ParentDto
    {
        public int Id { get; set; }
        public FactoryEntity Child { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal ParentDto Create() => new ParentDto();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.ParentDto", registered);
        Assert.DoesNotContain("global::TestNamespace.FactoryEntity", registered);
    }

    #endregion

    #region TS-010: Mixed property types (BR-NEST-007)

    [Fact]
    public void MixedProperties_OnlyEligibleDiscovered()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class ChildDto
    {
        public int Id { get; set; }
    }

    public interface IService { }

    public class ParentDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ChildDto Child { get; set; }
        public IService Svc { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal ParentDto Create() => new ParentDto();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.ParentDto", registered);
        Assert.Contains("global::TestNamespace.ChildDto", registered);
        Assert.Equal(2, registered.Count);
    }

    #endregion

    #region TS-011: Abstract property type excluded (BR-NEST-007)

    [Fact]
    public void AbstractProperty_Excluded()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public abstract class AbstractBase
    {
        public int Id { get; set; }
    }

    public class ParentDto
    {
        public int Id { get; set; }
        public AbstractBase Item { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal ParentDto Create() => new ParentDto();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.ParentDto", registered);
        Assert.DoesNotContain("global::TestNamespace.AbstractBase", registered);
    }

    #endregion

    #region TS-013: Inherited property discovery (BR-NEST-009)

    [Fact]
    public void InheritedProperty_ChildDiscovered()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class ChildDto
    {
        public int Id { get; set; }
    }

    public class BaseDto
    {
        public ChildDto Child { get; set; }
    }

    public class DerivedDto : BaseDto
    {
        public int Id { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal DerivedDto Create() => new DerivedDto();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.DerivedDto", registered);
        Assert.Contains("global::TestNamespace.ChildDto", registered);
    }

    #endregion

    #region TS-014: No-regression for simple DTOs (BR-NEST-010)

    [Fact]
    public void SimpleDto_NoNestedTypes_OnlyParentDiscovered()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class SimpleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal SimpleDto Create() => new SimpleDto();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.SimpleDto", registered);
        Assert.Single(registered);
    }

    #endregion

    #region TS-015: Collection of collections (edge case)

    [Fact]
    public void CollectionOfCollections_DocumentBehavior()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Collections.Generic;

namespace TestNamespace
{
    public class ChildDto
    {
        public int Id { get; set; }
    }

    public class ParentDto
    {
        public int Id { get; set; }
        public List<List<ChildDto>> Nested { get; set; }
    }

    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal ParentDto Create() => new ParentDto();
    }
}
";
        var registered = GetRegisteredDtoTypes(source);

        Assert.Contains("global::TestNamespace.ParentDto", registered);
        // The outer List<List<ChildDto>> implements IEnumerable<List<ChildDto>>.
        // The inner List<ChildDto> is a System type (skipped by IsDtoCandidate).
        // ChildDto is NOT discovered through nested collections — this is acceptable.
        // Users with this pattern can return ChildDto from a factory method to trigger discovery.
    }

    #endregion
}
