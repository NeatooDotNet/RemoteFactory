using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.Ordinal;

/// <summary>
/// The generated ordinal converter writes and reads <c>$id</c>/<c>$ref</c> for a class, and
/// builds the object before reading its properties when every property has a plain setter (#133).
/// </summary>
/// <remarks>
/// The round trips are pinned by <c>FactoryReferenceIdentityTests</c> in the integration project,
/// which compiles and runs the generated code.
/// </remarks>
public class OrdinalReferenceGenerationTests
{
    private const string Source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class Settable
    {
        public string Name { get; set; } = string.Empty;
        public Settable? Parent { get; set; }

        [Create]
        public void Create() { }
    }

    [Factory]
    public partial class InitOnly
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        [Create]
        public void Create() { }
    }

    [Factory]
    public partial class Required
    {
        public required string Code { get; set; }

        [Create]
        public void Create() { }
    }

    [Factory]
    [Create]
    public partial record Value(string Code);
}
";

    [Fact]
    public void Class_WritesReferenceOrIdWrapper()
    {
        var converter = Converter("Settable");

        Assert.Contains("NeatooOrdinalReferences.WriteReferenceOrStart(writer, value, out var wrapped)", converter, StringComparison.Ordinal);
        Assert.Contains("NeatooOrdinalReferences.WriteEnd(writer, wrapped)", converter, StringComparison.Ordinal);
    }

    [Fact]
    public void SettableClass_IsBuiltAndRegisteredBeforeItsPropertiesAreRead()
    {
        var converter = Converter("Settable");

        Assert.Contains("ReadReferenceOrStart<global::TestNamespace.Settable>(ref reader, buildsFirst: true", converter, StringComparison.Ordinal);
        var built = converter.IndexOf("var result = new global::TestNamespace.Settable();", StringComparison.Ordinal);
        var registered = converter.IndexOf("NeatooOrdinalReferences.AddReference(referenceId, result);", StringComparison.Ordinal);
        var firstPropertyRead = converter.IndexOf("var prop0", StringComparison.Ordinal);
        Assert.True(built >= 0 && built < registered && registered < firstPropertyRead,
            "Settable must be built and registered before its first property is read.");
        Assert.Contains("result.Parent = prop1;", converter, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("InitOnly")]
    [InlineData("Required")]
    public void ClassWithInitOrRequiredProperty_IsBuiltFromItsProperties(string typeName)
    {
        var converter = Converter(typeName);

        Assert.Contains($"ReadReferenceOrStart<global::TestNamespace.{typeName}>(ref reader, buildsFirst: false", converter, StringComparison.Ordinal);
        Assert.Contains($"var result = new global::TestNamespace.{typeName}", converter, StringComparison.Ordinal);
        Assert.DoesNotContain($"new global::TestNamespace.{typeName}();", converter, StringComparison.Ordinal);
    }

    [Fact]
    public void Record_WritesNoReferenceMetadata()
    {
        var converter = Converter("Value");

        Assert.DoesNotContain("NeatooOrdinalReferences", converter, StringComparison.Ordinal);
    }

    private static string Converter(string typeName)
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(Source);
        var tree = runResult.GeneratedTrees
            .Select(t => t.ToString())
            .Single(t => t.Contains($"class {typeName}OrdinalConverter", StringComparison.Ordinal));
        var start = tree.IndexOf($"class {typeName}OrdinalConverter", StringComparison.Ordinal);
        var end = tree.IndexOf($"partial ", start, StringComparison.Ordinal);
        return tree[start..end];
    }
}
