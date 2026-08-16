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
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var references = BuildReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = GeneratorInstance.Value;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        var runResult = driver.GetRunResult();

        // The driver's out-param already holds every generator diagnostic from this run, and
        // GetRunResult().Diagnostics holds the same ones — so the AddRange this used to do
        // returned every diagnostic TWICE. Nothing consumed the count, so nothing was red, but
        // it silently falsified any count assertion written against this array. Found while
        // writing NF0504's tests, which asserted Single() and got two.
        //
        // Returning one source rather than Distinct()-ing the union, for a narrower reason than
        // it first appears. Distinct() also works — measured, not assumed: the two collections
        // hold the SAME Diagnostic instances, so identity dedupe removes the doubling, while
        // two genuinely repeated diagnostics (a class stacking attributes that each fail the
        // same way reports NF0502 once per attribute, same location, byte-identical message)
        // are separate instances and survive. But that correctness rests entirely on the two
        // collections sharing object identity, which is a Roslyn implementation detail no test
        // here controls. Not concatenating needs no such assumption.
        //
        // Whichever way this is written, multiplicity must be preserved — a genuine repeat is
        // signal, and DiagnosticTestHelperTests pins it.
        //
        // This is the compilation-filtered set — what a consumer's build actually surfaces,
        // with severity settings and suppressions applied. Callers needing the raw, unfiltered
        // generator output can still reach RunResult.Diagnostics on the third tuple element.
        return (diagnostics, outputCompilation, runResult);
    }

    private static List<MetadataReference> BuildReferences()
    {
        // Get the RemoteFactory assembly for references
        var remoteFactoryAssembly = typeof(FactoryAttribute).Assembly;

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(remoteFactoryAssembly.Location),

            // Generated factory code declares FactoryServiceRegistrar(IServiceCollection, ...)
            // and calls AddScoped / TryAddTransient / GetRequiredService; class factories also
            // reach for ILogger<T>. Without these, EVERY generated tree fails to compile in this
            // harness for reasons that have nothing to do with the code under test.
            //
            // Added by TRIM-008. Their absence was not costing correctness — nothing asserted on
            // OutputCompilation — but it silently capped what this helper could ever detect: a
            // generator change that emits uncompilable source was undetectable here, and the
            // relay-handler renderer in particular has no other error signal (it bypasses
            // NormalizeWhitespace, and FactoryRenderer swallows parse failures into a
            // /* Error: */ comment rather than throwing).
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.DependencyInjection.ServiceCollection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Microsoft.Extensions.Logging.ILogger<>).Assembly.Location)
        };

        // Add System.Runtime, plus System.ComponentModel — which is where IServiceProvider
        // is surfaced for reference purposes. Generated registrars and factories take an
        // IServiceProvider, so without it the output compilation reports CS0012 on every
        // generated tree.
        var runtimeAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
        foreach (var name in new[] { "System.Runtime.dll", "System.ComponentModel.dll" })
        {
            var path = Path.Combine(runtimeAssemblyPath!, name);
            if (File.Exists(path))
            {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        return references;
    }

    /// <summary>
    /// Runs the generator twice with incremental step tracking enabled, applying an
    /// unrelated edit between the runs, and returns the second run's result.
    /// </summary>
    /// <param name="source">The fixture source, present in both runs.</param>
    /// <param name="appendedSource">An unrelated declaration appended for the second run.</param>
    /// <remarks>
    /// Two details make this a non-vacuous caching probe:
    ///
    /// 1. The edit REPLACES the existing syntax tree rather than adding a second one.
    ///    Adding a separate tree would leave the fixture's nodes untouched, so Roslyn
    ///    would serve every transform from its own upstream cache and the assertion
    ///    would pass no matter how broken the transform-output equality is. Replacing
    ///    the tree forces every transform in it to re-run, which is what makes the
    ///    equality of the transform-output records the thing under test.
    ///
    /// 2. The edit is APPENDED, so the spans and line positions of the existing
    ///    declarations do not move. TypeInfo and DiagnosticInfo capture file paths and
    ///    line/column positions, so an edit that shifted them would change transform
    ///    output for an entirely benign reason and the guard would fail without a defect.
    ///
    /// Local iteration gotcha: the generator is loaded once per process via
    /// Assembly.LoadFrom and held in a static Lazy. If you rebuild ONLY the generator
    /// and re-run with --no-build, a surviving testhost can serve the previously loaded
    /// generator assembly, and the guard will report on stale code — it will appear to
    /// pass against a generator you just broke. Rebuild the test project too (or run
    /// without --no-build) when verifying a red/green cycle on this guard. CI is
    /// unaffected: every run starts from a cold build.
    /// </remarks>
    public static (GeneratorDriverRunResult First, GeneratorDriverRunResult Second) RunGeneratorTracked(
        string source,
        string appendedSource)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var firstTree = CSharpSyntaxTree.ParseText(source, parseOptions, path: "Fixture.cs");

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [firstTree],
            references: BuildReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [GeneratorInstance.Value.AsSourceGenerator()],
            additionalTexts: null,
            parseOptions: parseOptions,
            optionsProvider: null,
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        var first = driver.GetRunResult();

        var secondTree = CSharpSyntaxTree.ParseText(source + appendedSource, parseOptions, path: "Fixture.cs");
        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(firstTree, secondTree));
        var second = driver.GetRunResult();

        return (first, second);
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
