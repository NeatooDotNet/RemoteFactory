using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.DtoDiscovery;

/// <summary>
/// Demonstrates the nested DTO discovery gap: before the fix, nested DTOs
/// are NOT registered, causing deserialization failures under IL trimming.
/// </summary>
public class NestedDtoFailureTest
{
    [Fact]
    public void NestedDto_ShouldBeRegistered_ButIsNot()
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
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var allGeneratedSource = string.Join("\n",
            runResult.GeneratedTrees.Select(t => t.GetText()?.ToString() ?? ""));

        // ParentDto should be registered (it's the direct return type)
        Assert.Contains("DtoConstructorRegistry.Register<global::TestNamespace.ParentDto>", allGeneratedSource);

        // ChildDto SHOULD be registered (it's a property of ParentDto) — this is the gap
        Assert.Contains("DtoConstructorRegistry.Register<global::TestNamespace.ChildDto>", allGeneratedSource);
    }
}
