using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.DtoDiscovery;

/// <summary>
/// Verifies that a class factory with no generated ordinal converter gets
/// DtoConstructorRegistry.PreserveProperties&lt;T&gt;() in its registrar (EXSET-016).
/// The ordinal converter is what references a factory class's accessors on a trimmed
/// client; without it IsNew/IsDeleted and every other property serialized as {}.
/// The trimmed end-to-end check is SaveMetaRoundTripSmokeTest in RemoteFactory.TrimmingTests.
/// </summary>
public class PreservePropertiesEmissionTests
{
    private static string RunAndGetGeneratedSource(string source)
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);
        return string.Join("\n", runResult.GeneratedTrees.Select(t => t.GetText()?.ToString() ?? ""));
    }

    private static bool PreservesProperties(string generated, string typeName) =>
        generated.Contains($"DtoConstructorRegistry.PreserveProperties<{typeName}>()", StringComparison.Ordinal);

    private static bool RegistersOrdinalConverter(string generated, string typeName) =>
        generated.Contains($"{typeName}.CreateOrdinalConverter()", StringComparison.Ordinal);

    [Fact]
    public void ConstructorTakesAService_PreservePropertiesEmitted()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public interface IClock { }

    [Factory]
    public partial class Visit : IFactorySaveMeta
    {
        private readonly IClock _clock;
        public Visit(IClock clock) { _clock = clock; }

        public bool IsNew { get; set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public void Create() { }

        [Remote, Insert]
        internal Task Insert() => Task.CompletedTask;
    }
}
";
        var generated = RunAndGetGeneratedSource(source);

        Assert.True(PreservesProperties(generated, "Visit"), "a class whose constructor takes a service has no ordinal converter, so its properties must be preserved");
        Assert.False(RegistersOrdinalConverter(generated, "Visit"));
    }

    [Fact]
    public void NotPartial_PreservePropertiesEmitted()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public class Visit : IFactorySaveMeta
    {
        public bool IsNew { get; set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public void Create() { }

        [Remote, Insert]
        internal Task Insert() => Task.CompletedTask;
    }
}
";
        var generated = RunAndGetGeneratedSource(source);

        Assert.True(PreservesProperties(generated, "Visit"), "a class that is not partial has no ordinal converter, so its properties must be preserved");
        Assert.False(RegistersOrdinalConverter(generated, "Visit"));
    }

    [Fact]
    public void PartialWithParameterlessConstructor_OrdinalConverterInstead()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class Visit : IFactorySaveMeta
    {
        public bool IsNew { get; set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public void Create() { }

        [Remote, Insert]
        internal Task Insert() => Task.CompletedTask;
    }
}
";
        var generated = RunAndGetGeneratedSource(source);

        Assert.True(RegistersOrdinalConverter(generated, "Visit"));
        Assert.False(PreservesProperties(generated, "Visit"), "the ordinal converter already references the accessors");
    }
}
