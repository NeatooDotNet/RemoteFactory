using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.Ordinal;

/// <summary>
/// A <c>partial</c> <c>[Factory]</c> collection gets no object-shaped ordinal converter (#119).
/// </summary>
/// <remarks>
/// <c>List&lt;T&gt;</c> exposes one public settable property, <c>Capacity</c>, so the generator
/// used to treat the list as an object with one ordinal property and write <c>[capacity]</c>
/// instead of the items. The round trip itself is pinned by
/// <c>PartialListSerializationTests</c> in the integration project, which also compiles and runs
/// the generated code (this project's compilation helper has no System.Text.Json reference).
/// </remarks>
public class OrdinalCollectionGenerationTests
{
    private const string Source = @"
using Neatoo.RemoteFactory;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class Item
    {
        public string Name { get; set; } = string.Empty;

        [Create]
        public void Create(string name) { Name = name; }
    }

    [Factory]
    public partial class ItemList : List<Item>
    {
        [Create]
        public ItemList() { }
    }
}
";

    [Fact]
    public void PartialListClass_GetsNoOrdinalConverter()
    {
        var generated = GeneratedSource();

        Assert.DoesNotContain("class ItemListOrdinalConverter", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("partial class ItemList : global::Neatoo.RemoteFactory.IOrdinalSerializable", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void PartialListClass_ItsItemTypeStillGetsOrdinalConverter()
    {
        var generated = GeneratedSource();

        Assert.Contains("class ItemOrdinalConverter", generated, StringComparison.Ordinal);
    }

    private static string GeneratedSource()
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(Source);
        return string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));
    }
}
