using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Reflection;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Diagnostics;

/// <summary>
/// Tests for record-related diagnostics in the RemoteFactory source generator.
/// These tests verify that NF0205 and NF0206 errors are correctly reported.
///
/// NF0205: [Create] on type requires record with primary constructor
/// NF0206: record struct not supported
/// </summary>
public class RecordDiagnosticTests
{
    private static readonly Lazy<IIncrementalGenerator> GeneratorInstance = new(() =>
    {
        // Load the generator assembly dynamically
        var testAssemblyPath = typeof(RecordDiagnosticTests).Assembly.Location;
        var testDir = Path.GetDirectoryName(testAssemblyPath)!;

        // Search for generator in src folder structure
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
                // Check src folder structure
                var srcDir = Path.Combine(dir.FullName, "src", "Generator", "bin", config, "netstandard2.0");
                var candidatePath = Path.Combine(srcDir, "Neatoo.Generator.dll");
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }

                // Also check direct child
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

    private static (ImmutableArray<Diagnostic> Diagnostics, Compilation OutputCompilation, GeneratorDriverRunResult RunResult) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Get the RemoteFactory assembly for references
        var remoteFactoryAssembly = typeof(FactoryAttribute).Assembly;

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(remoteFactoryAssembly.Location)
        };

        // Add System.Runtime reference
        var runtimeAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
        var systemRuntimePath = Path.Combine(runtimeAssemblyPath!, "System.Runtime.dll");
        if (File.Exists(systemRuntimePath))
        {
            references.Add(MetadataReference.CreateFromFile(systemRuntimePath));
        }

        // Add System.Collections for List<T>
        var collectionsPath = Path.Combine(runtimeAssemblyPath!, "System.Collections.dll");
        if (File.Exists(collectionsPath))
        {
            references.Add(MetadataReference.CreateFromFile(collectionsPath));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = GeneratorInstance.Value;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        var runResult = driver.GetRunResult();

        // Get diagnostics from the generator run result as well
        var allDiagnostics = diagnostics.AddRange(runResult.Diagnostics);

        return (allDiagnostics, outputCompilation, runResult);
    }

    // ============================================================================
    // NF0205 Tests - [Create] on type requires record with primary constructor
    // ============================================================================

    [Fact]
    public void NF0205_CreateOnNonRecord_Class_ReportsDiagnostic()
    {
        // Arrange - [Create] on a class (not a record)
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public class NotARecord
    {
        public string Name { get; set; }
    }
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0205Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0205");
        Assert.NotNull(nf0205Diagnostic);
        Assert.Equal(DiagnosticSeverity.Error, nf0205Diagnostic.Severity);
        Assert.Contains("NotARecord", nf0205Diagnostic.GetMessage());
        Assert.Contains("primary constructor", nf0205Diagnostic.GetMessage());
    }

    [Fact]
    public void NF0205_CreateOnRecordWithoutPrimaryConstructor_ReportsDiagnostic()
    {
        // Arrange - [Create] on a record without primary constructor
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record RecordWithoutPrimaryConstructor
    {
        public string Name { get; init; }
    }
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0205Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0205");
        Assert.NotNull(nf0205Diagnostic);
        Assert.Equal(DiagnosticSeverity.Error, nf0205Diagnostic.Severity);
        Assert.Contains("RecordWithoutPrimaryConstructor", nf0205Diagnostic.GetMessage());
    }

    [Fact]
    public void NF0205_CreateOnRecordWithPrimaryConstructor_NoDiagnostic()
    {
        // Arrange - [Create] on a record with primary constructor (valid)
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record ValidRecord(string Name, int Value);
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0205Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0205");
        Assert.Null(nf0205Diagnostic);
    }

    [Fact]
    public void NF0205_CreateOnConstructor_Class_NoDiagnostic()
    {
        // Arrange - [Create] on an explicit constructor (valid for classes)
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public class ClassWithExplicitCreate
    {
        public string Name { get; set; }

        [Create]
        public ClassWithExplicitCreate(string name)
        {
            Name = name;
        }
    }
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0205Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0205");
        Assert.Null(nf0205Diagnostic);
    }

    [Fact]
    public void NF0205_CreateOnConstructor_RecordWithoutPrimaryConstructor_NoDiagnostic()
    {
        // Arrange - [Create] on explicit constructor of a record (valid)
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public record RecordWithExplicitCreate
    {
        public string Name { get; init; }

        [Create]
        public RecordWithExplicitCreate(string name)
        {
            Name = name;
        }
    }
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0205Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0205");
        Assert.Null(nf0205Diagnostic);
    }

    // ============================================================================
    // NF0206 Tests - record struct not supported
    // ============================================================================

    [Fact]
    public void NF0206_RecordStruct_ReportsDiagnostic()
    {
        // Arrange - record struct (not supported)
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record struct ValueRecord(string Name);
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0206Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0206");
        Assert.NotNull(nf0206Diagnostic);
        Assert.Equal(DiagnosticSeverity.Error, nf0206Diagnostic.Severity);
        Assert.Contains("ValueRecord", nf0206Diagnostic.GetMessage());
        Assert.Contains("record struct", nf0206Diagnostic.GetMessage());
    }

    [Fact]
    public void NF0206_RecordClass_NoDiagnostic()
    {
        // Arrange - explicit record class (valid)
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record class ReferenceRecord(string Name);
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0206Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0206");
        Assert.Null(nf0206Diagnostic);
    }

    [Fact]
    public void NF0206_Record_ImplicitClass_NoDiagnostic()
    {
        // Arrange - implicit record class (valid)
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record ImplicitClassRecord(string Name);
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0206Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0206");
        Assert.Null(nf0206Diagnostic);
    }

    // ============================================================================
    // Positive Tests - Valid record configurations
    // ============================================================================

    [Fact]
    public void ValidRecord_PositionalWithCreate_GeneratesFactory()
    {
        // Arrange
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record SimpleRecord(string Name, int Value);
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert - No NF errors
        var nfErrors = diagnostics.Where(d => d.Id.StartsWith("NF") && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(nfErrors);

        // Verify factory was generated
        var generatedSource = runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("SimpleRecord"));
        Assert.NotNull(generatedSource);
    }

    [Fact]
    public void ValidRecord_WithFetch_GeneratesFactory()
    {
        // Arrange
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record FetchableRecord(string Id, string Data)
    {
        [Fetch]
        public static FetchableRecord FetchById(string id)
            => new FetchableRecord(id, $""Fetched-{id}"");

        [Fetch]
        public static Task<FetchableRecord> FetchByIdAsync(string id)
            => Task.FromResult(new FetchableRecord(id, $""AsyncFetched-{id}""));
    }
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert - No NF errors
        var nfErrors = diagnostics.Where(d => d.Id.StartsWith("NF") && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(nfErrors);
    }

    [Fact]
    public void ValidRecord_SealedWithCreate_GeneratesFactory()
    {
        // Arrange
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public sealed record SealedRecord(string Value);
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert - No NF errors
        var nfErrors = diagnostics.Where(d => d.Id.StartsWith("NF") && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(nfErrors);
    }

    [Fact]
    public void ValidRecord_WithService_GeneratesFactory()
    {
        // Arrange
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface ITestService { }

    [Factory]
    [Create]
    public record RecordWithService(string Name, [Service] ITestService Service);
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert - No NF errors
        var nfErrors = diagnostics.Where(d => d.Id.StartsWith("NF") && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(nfErrors);
    }

    [Fact]
    public void ValidRecord_WithDefaults_GeneratesFactory()
    {
        // Arrange
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record RecordWithDefaults(string Name = ""default"", int Value = 42);
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert - No NF errors
        var nfErrors = diagnostics.Where(d => d.Id.StartsWith("NF") && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(nfErrors);
    }

    // ============================================================================
    // Edge Cases
    // ============================================================================

    [Fact]
    public void Record_WithSuppressFactory_NoGeneration()
    {
        // Arrange - SuppressFactory should prevent any generation
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [SuppressFactory]
    [Create]
    public record SuppressedRecord(string Name);
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert - No factory should be generated (SuppressFactory takes effect in predicate)
        // No NF diagnostics should be reported for suppressed types
        var nfDiagnostics = diagnostics.Where(d => d.Id.StartsWith("NF"));
        Assert.Empty(nfDiagnostics);
    }

    [Fact]
    public void GenericRecord_Rejected()
    {
        // Arrange - Generic types are rejected by the generator predicate
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record GenericRecord<T>(T Value);
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert - No factory generated (filtered out by predicate)
        // Generic types are filtered in the predicate, so no diagnostics are expected
        var generatedSource = runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("GenericRecord"));
        Assert.Null(generatedSource);
    }

    [Fact]
    public void AbstractRecord_Rejected()
    {
        // Arrange - Abstract types are rejected by the generator predicate
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public abstract record AbstractRecord(string Name);
}";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert - No factory generated (filtered out by predicate)
        var generatedSource = runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("AbstractRecord"));
        Assert.Null(generatedSource);
    }
}
