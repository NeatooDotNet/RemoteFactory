using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Reflection;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.FactoryMode;

/// <summary>
/// Tests for the FactoryMode assembly-level attribute that controls
/// client-server code separation in generated factories.
/// </summary>
public class FactoryModeTests
{
    private static readonly Lazy<IIncrementalGenerator> GeneratorInstance = new(() =>
    {
        var testAssemblyPath = typeof(FactoryModeTests).Assembly.Location;
        var testDir = Path.GetDirectoryName(testAssemblyPath)!;

        var generatorPath = FindGeneratorAssembly(testDir);
        if (generatorPath == null)
        {
            throw new FileNotFoundException("Could not find Neatoo.Generator.dll");
        }

        var generatorAssembly = Assembly.LoadFrom(generatorPath);
        var generatorType = generatorAssembly.GetType("Neatoo.Factory")
            ?? throw new InvalidOperationException("FactoryGenerator type not found");

        return (IIncrementalGenerator)Activator.CreateInstance(generatorType)!;
    });

    private static string? FindGeneratorAssembly(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        var configurations = new[] { "Debug", "Release" };

        while (dir != null)
        {
            foreach (var config in configurations)
            {
                var srcDir = Path.Combine(dir.FullName, "src", "Generator", "bin", config, "netstandard2.0");
                var candidatePath = Path.Combine(srcDir, "Neatoo.Generator.dll");
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }

                candidatePath = Path.Combine(dir.FullName, "Generator", "bin", config, "netstandard2.0", "Neatoo.Generator.dll");
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }

            dir = dir.Parent;
        }
        return null;
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, string GeneratedSource) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var remoteFactoryAssembly = typeof(FactoryAttribute).Assembly;

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(remoteFactoryAssembly.Location)
        };

        var runtimeAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
        var systemRuntimePath = Path.Combine(runtimeAssemblyPath!, "System.Runtime.dll");
        if (File.Exists(systemRuntimePath))
        {
            references.Add(MetadataReference.CreateFromFile(systemRuntimePath));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = GeneratorInstance.Value;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        var runResult = driver.GetRunResult();

        var generatedSource = "";
        if (runResult.GeneratedTrees.Length > 0)
        {
            generatedSource = string.Join("\n\n---\n\n", runResult.GeneratedTrees.Select(t => t.GetText().ToString()));
        }

        return (diagnostics, generatedSource);
    }

    [Fact]
    public void FactoryMode_DefaultsToFull_WhenNoAttribute()
    {
        var source = """
            using Neatoo.RemoteFactory;

            namespace TestNamespace;

            [Factory]
            public class TestEntity
            {
                [Create]
                [Remote]
                public void Create() { }
            }
            """;

        var (diagnostics, generatedSource) = RunGenerator(source);

        // Should have no errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Full mode generates both local and remote constructors
        Assert.Contains("public TestEntityFactory(IServiceProvider serviceProvider, IFactoryCore", generatedSource);
        Assert.Contains("public TestEntityFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore", generatedSource);

        // Full mode generates LocalCreate method
        Assert.Contains("LocalCreate", generatedSource);

        // MakeRemoteDelegateRequest is nullable in Full mode
        Assert.Contains("IMakeRemoteDelegateRequest?", generatedSource);
    }

    [Fact]
    public void FactoryMode_RemoteOnly_GeneratesRemoteConstructorOnly()
    {
        var source = """
            using Neatoo.RemoteFactory;

            [assembly: FactoryMode(FactoryMode.RemoteOnly)]

            namespace TestNamespace;

            [Factory]
            public class TestEntity
            {
                [Create]
                [Remote]
                public void Create() { }
            }
            """;

        var (diagnostics, generatedSource) = RunGenerator(source);

        // Should have no errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // RemoteOnly mode generates only remote constructor
        Assert.Contains("public TestEntityFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore", generatedSource);

        // Count constructors - should only have one
        var constructorCount = CountOccurrences(generatedSource, "public TestEntityFactory(IServiceProvider");
        Assert.Equal(1, constructorCount);

        // MakeRemoteDelegateRequest is non-nullable in RemoteOnly mode
        Assert.Contains("private readonly IMakeRemoteDelegateRequest MakeRemoteDelegateRequest;", generatedSource);
        Assert.DoesNotContain("IMakeRemoteDelegateRequest?", generatedSource);
    }

    [Fact]
    public void FactoryMode_RemoteOnly_OmitsLocalMethods()
    {
        var source = """
            using Neatoo.RemoteFactory;

            [assembly: FactoryMode(FactoryMode.RemoteOnly)]

            namespace TestNamespace;

            [Factory]
            public class TestEntity
            {
                [Create]
                [Remote]
                public void Create() { }

                [Fetch]
                [Remote]
                public System.Threading.Tasks.Task Fetch(int id) { return System.Threading.Tasks.Task.CompletedTask; }
            }
            """;

        var (diagnostics, generatedSource) = RunGenerator(source);

        // Should have no errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // RemoteOnly mode should NOT generate LocalCreate or LocalFetch methods
        Assert.DoesNotContain("LocalCreate", generatedSource);
        Assert.DoesNotContain("LocalFetch", generatedSource);

        // But should have RemoteCreate and RemoteFetch
        Assert.Contains("RemoteCreate", generatedSource);
        Assert.Contains("RemoteFetch", generatedSource);
    }

    [Fact]
    public void FactoryMode_RemoteOnly_OmitsDelegateServiceRegistrations()
    {
        var source = """
            using Neatoo.RemoteFactory;

            [assembly: FactoryMode(FactoryMode.RemoteOnly)]

            namespace TestNamespace;

            [Factory]
            public class TestEntity
            {
                [Create]
                [Remote]
                public void Create() { }
            }
            """;

        var (diagnostics, generatedSource) = RunGenerator(source);

        // Should have no errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // RemoteOnly mode should NOT register delegate (that's for server-side handling)
        Assert.DoesNotContain("services.AddScoped<CreateDelegate>", generatedSource);

        // But should still register the factory itself
        Assert.Contains("services.AddScoped<TestEntityFactory>()", generatedSource);
        Assert.Contains("services.AddScoped<ITestEntityFactory, TestEntityFactory>()", generatedSource);
    }

    [Fact]
    public void FactoryMode_Full_GeneratesBothConstructors()
    {
        var source = """
            using Neatoo.RemoteFactory;

            [assembly: FactoryMode(FactoryMode.Full)]

            namespace TestNamespace;

            [Factory]
            public class TestEntity
            {
                [Create]
                [Remote]
                public void Create() { }
            }
            """;

        var (diagnostics, generatedSource) = RunGenerator(source);

        // Should have no errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Full mode generates both constructors
        var constructorCount = CountOccurrences(generatedSource, "public TestEntityFactory(IServiceProvider");
        Assert.Equal(2, constructorCount);

        // Full mode generates LocalCreate method
        Assert.Contains("LocalCreate", generatedSource);

        // MakeRemoteDelegateRequest is nullable in Full mode
        Assert.Contains("IMakeRemoteDelegateRequest?", generatedSource);
    }

    [Fact]
    public void InterfaceSignature_IdenticalInBothModes()
    {
        var sourceRemoteOnly = """
            using Neatoo.RemoteFactory;

            [assembly: FactoryMode(FactoryMode.RemoteOnly)]

            namespace TestNamespace;

            [Factory]
            public class TestEntity
            {
                [Create]
                [Remote]
                public void Create() { }

                [Fetch]
                [Remote]
                public System.Threading.Tasks.Task Fetch(int id) { return System.Threading.Tasks.Task.CompletedTask; }
            }
            """;

        var sourceFull = """
            using Neatoo.RemoteFactory;

            [assembly: FactoryMode(FactoryMode.Full)]

            namespace TestNamespace;

            [Factory]
            public class TestEntity
            {
                [Create]
                [Remote]
                public void Create() { }

                [Fetch]
                [Remote]
                public System.Threading.Tasks.Task Fetch(int id) { return System.Threading.Tasks.Task.CompletedTask; }
            }
            """;

        var (_, generatedRemoteOnly) = RunGenerator(sourceRemoteOnly);
        var (_, generatedFull) = RunGenerator(sourceFull);

        // Extract interface sections
        var interfaceRemoteOnly = ExtractInterfaceSection(generatedRemoteOnly, "ITestEntityFactory");
        var interfaceFull = ExtractInterfaceSection(generatedFull, "ITestEntityFactory");

        // Interface signatures should be identical
        Assert.Equal(interfaceFull, interfaceRemoteOnly);
    }

    [Fact]
    public void ServiceParameters_StrippedFromInterface_InBothModes()
    {
        var sourceRemoteOnly = """
            using Neatoo.RemoteFactory;

            [assembly: FactoryMode(FactoryMode.RemoteOnly)]

            namespace TestNamespace;

            public interface IService { }

            [Factory]
            public class TestEntity
            {
                [Fetch]
                [Remote]
                public System.Threading.Tasks.Task Fetch(int id, [Service] IService service) { return System.Threading.Tasks.Task.CompletedTask; }
            }
            """;

        var (_, generatedSource) = RunGenerator(sourceRemoteOnly);

        // Interface method should NOT include [Service] parameter
        var interfaceSection = ExtractInterfaceSection(generatedSource, "ITestEntityFactory");
        Assert.DoesNotContain("IService", interfaceSection);
        Assert.Contains("int id", interfaceSection);
    }

    [Fact]
    public void NonRemoteMethods_WorkInRemoteOnlyMode()
    {
        var source = """
            using Neatoo.RemoteFactory;

            [assembly: FactoryMode(FactoryMode.RemoteOnly)]

            namespace TestNamespace;

            [Factory]
            public class TestEntity
            {
                // Local Create - no [Remote] attribute
                [Create]
                public void Create() { }
            }
            """;

        var (diagnostics, generatedSource) = RunGenerator(source);

        // Should have no errors
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Local [Create] should still have LocalCreate method even in RemoteOnly mode
        // because it's not marked [Remote] - it runs locally on client
        Assert.Contains("LocalCreate", generatedSource);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    private static string ExtractInterfaceSection(string source, string interfaceName)
    {
        var interfaceStart = source.IndexOf($"public interface {interfaceName}", StringComparison.Ordinal);
        if (interfaceStart == -1) return "";

        var braceStart = source.IndexOf('{', interfaceStart);
        if (braceStart == -1) return "";

        // Find matching closing brace
        int braceCount = 1;
        int index = braceStart + 1;
        while (index < source.Length && braceCount > 0)
        {
            if (source[index] == '{') braceCount++;
            else if (source[index] == '}') braceCount--;
            index++;
        }

        return source.Substring(braceStart + 1, index - braceStart - 2).Trim();
    }

    /// <summary>
    /// Tests that RemoteOnly mode generates the explicit IFactorySave interface implementation.
    /// This is a regression test for a bug where the generated factory class implemented
    /// IFactorySave&lt;T&gt; but didn't generate the explicit Save method implementation.
    /// </summary>
    [Fact]
    public void FactoryMode_RemoteOnly_GeneratesIFactorySaveImplementation()
    {
        var source = """
            using Neatoo.RemoteFactory;
            using System.Threading.Tasks;

            [assembly: FactoryMode(FactoryMode.RemoteOnly)]

            namespace TestNamespace;

            public interface ITestEntity : IFactorySaveMeta
            {
                string Name { get; set; }
            }

            [Factory]
            public class TestEntity : ITestEntity
            {
                public string Name { get; set; }
                public bool IsDeleted { get; set; }
                public bool IsNew { get; set; } = true;

                [Create]
                [Remote]
                public void Create() { }

                [Insert]
                [Remote]
                public Task Insert() { return Task.CompletedTask; }

                [Update]
                [Remote]
                public Task Update() { return Task.CompletedTask; }

                [Delete]
                [Remote]
                public Task Delete() { return Task.CompletedTask; }
            }
            """;

        var (diagnostics, generatedSource) = RunGenerator(source);

        // Should have no errors - this is the bug: CS0535 'OrderFactory' does not implement interface member 'IFactorySave<Order>.Save(Order, CancellationToken)'
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // In RemoteOnly mode, the factory should still inherit from IFactorySave<T>
        Assert.Contains("IFactorySave<TestEntity>", generatedSource);

        // The explicit interface implementation should be generated
        Assert.Contains("IFactorySave<TestEntity>.Save", generatedSource);

        // Save method (public) should be generated
        Assert.Contains("public virtual Task<ITestEntity?> Save(ITestEntity target", generatedSource);

        // RemoteSave method should be generated
        Assert.Contains("RemoteSave", generatedSource);
    }
}
