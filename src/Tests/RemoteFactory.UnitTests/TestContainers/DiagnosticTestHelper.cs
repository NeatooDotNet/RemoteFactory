using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Neatoo.RemoteFactory;
using System.Collections.Immutable;
using System.Reflection;

namespace RemoteFactory.UnitTests.TestContainers;

/// <summary>
/// Helper class for running the Roslyn source generator in tests and capturing diagnostics.
/// </summary>
/// <remarks>
/// This helper uses the Roslyn CSharpGeneratorDriver to run the generator against
/// test source code and capture any diagnostics emitted. This is used for testing
/// that the generator correctly reports errors, warnings, and info diagnostics.
/// </remarks>
public static class DiagnosticTestHelper
{
    private static readonly Lazy<IIncrementalGenerator> GeneratorInstance = new(() =>
    {
        // Load the generator assembly dynamically
        var testAssemblyPath = typeof(DiagnosticTestHelper).Assembly.Location;
        var testDir = Path.GetDirectoryName(testAssemblyPath)!;

        // Search for generator in src folder structure
        var generatorPath = FindGeneratorAssembly(testDir)
            ?? throw new FileNotFoundException("Could not find Neatoo.Generator.dll");

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

    /// <summary>
    /// Runs the source generator on the provided source code and returns the diagnostics.
    /// </summary>
    /// <param name="source">C# source code to analyze.</param>
    /// <returns>Tuple containing all diagnostics, the output compilation, and the generator run result.</returns>
    public static (ImmutableArray<Diagnostic> Diagnostics, Compilation OutputCompilation, GeneratorDriverRunResult RunResult) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Get the RemoteFactory assembly for references
        var remoteFactoryAssembly = typeof(FactoryAttribute).Assembly;

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(remoteFactoryAssembly.Location)
        };

        // Add System.Runtime reference
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

        // Get diagnostics from the generator run result as well
        var allDiagnostics = diagnostics.AddRange(runResult.Diagnostics);

        return (allDiagnostics, outputCompilation, runResult);
    }

    /// <summary>
    /// Runs the generator and returns only diagnostics with the specified ID.
    /// </summary>
    public static IEnumerable<Diagnostic> GetDiagnosticsById(string source, string diagnosticId)
    {
        var (diagnostics, _, _) = RunGenerator(source);
        return diagnostics.Where(d => d.Id == diagnosticId);
    }

    /// <summary>
    /// Runs the generator and returns all NF-prefixed diagnostics (RemoteFactory diagnostics).
    /// </summary>
    public static IEnumerable<Diagnostic> GetRemoteFactoryDiagnostics(string source)
    {
        var (diagnostics, _, _) = RunGenerator(source);
        return diagnostics.Where(d => d.Id.StartsWith("NF"));
    }

    /// <summary>
    /// Asserts that the generator produces no RemoteFactory diagnostics.
    /// </summary>
    public static void AssertNoRemoteFactoryDiagnostics(string source)
    {
        var nfDiagnostics = GetRemoteFactoryDiagnostics(source).ToList();
        if (nfDiagnostics.Count > 0)
        {
            var messages = string.Join(Environment.NewLine, nfDiagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));
            throw new Xunit.Sdk.XunitException($"Expected no RemoteFactory diagnostics, but found:{Environment.NewLine}{messages}");
        }
    }

    /// <summary>
    /// Asserts that the generator produces a diagnostic with the specified ID.
    /// </summary>
    public static Diagnostic AssertHasDiagnostic(string source, string diagnosticId, DiagnosticSeverity? expectedSeverity = null)
    {
        var diagnostics = GetDiagnosticsById(source, diagnosticId).ToList();
        if (diagnostics.Count == 0)
        {
            throw new Xunit.Sdk.XunitException($"Expected diagnostic {diagnosticId} but none was found.");
        }

        var diagnostic = diagnostics.First();
        if (expectedSeverity.HasValue && diagnostic.Severity != expectedSeverity.Value)
        {
            throw new Xunit.Sdk.XunitException(
                $"Diagnostic {diagnosticId} has severity {diagnostic.Severity}, expected {expectedSeverity.Value}.");
        }

        return diagnostic;
    }
}
