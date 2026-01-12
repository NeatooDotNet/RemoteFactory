using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Diagnostics;

/// <summary>
/// Generator unit test to reproduce the duplicate Save method bug.
///
/// Bug scenario: When Insert, Update, and Delete all have CancellationToken,
/// the generator creates two SaveFactoryMethods with the same Name but different
/// UniqueName, causing duplicate Save/TrySave methods on the interface (CS0111).
///
/// See: docs/todos/duplicate-save-cancellation-token-bug.md
/// </summary>
public class DuplicateSaveGeneratorTest
{
    private static readonly Lazy<IIncrementalGenerator> GeneratorInstance = new(() =>
    {
        var testAssemblyPath = typeof(DuplicateSaveGeneratorTest).Assembly.Location;
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

    private static (ImmutableArray<Diagnostic> Diagnostics, Compilation OutputCompilation, GeneratorDriverRunResult RunResult) RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var remoteFactoryAssembly = typeof(FactoryAttribute).Assembly;

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(remoteFactoryAssembly.Location)
        };

        var runtimeAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
        var systemRuntimePath = Path.Combine(runtimeAssemblyPath!, "System.Runtime.dll");
        if (File.Exists(systemRuntimePath))
        {
            references.Add(MetadataReference.CreateFromFile(systemRuntimePath));
        }

        // Add System.Threading to ensure CancellationToken is properly resolved
        var systemThreadingPath = Path.Combine(runtimeAssemblyPath!, "System.Threading.dll");
        if (File.Exists(systemThreadingPath))
        {
            references.Add(MetadataReference.CreateFromFile(systemThreadingPath));
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

        var allDiagnostics = diagnostics.AddRange(runResult.Diagnostics);

        return (allDiagnostics, outputCompilation, runResult);
    }

    /// <summary>
    /// Test that matches the EXACT Neatoo Person scenario:
    /// - Insert returns Task<Entity?> with 2 services + CancellationToken
    /// - Update returns Task<Entity?> with 2 services + CancellationToken
    /// - Delete returns Task (void) with 1 service + CancellationToken
    /// - Authorization via [AuthorizeFactory<T>]
    ///
    /// This should reproduce the CS0111 duplicate Save method bug.
    /// </summary>
    [Fact]
    public void Person_Pattern_Should_Not_Generate_Duplicate_Save_Methods()
    {
        // Arrange - Exact pattern from Neatoo Person.cs
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    // Mock services
    public interface IPersonDbContext { }
    public interface IPersonPhoneListFactory { }

    // Mock entity returned by Insert/Update
    public class PersonEntity { }

    // Authorization interface
    public interface IPersonAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
        bool HasAccess();

        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        bool HasCreate();

        [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
        bool HasFetch();

        [AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
        bool HasInsert();

        [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
        bool HasUpdate();

        [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
        bool HasDelete();
    }

    public interface IPerson : IFactorySaveMeta
    {
        bool IsNew { get; set; }
        bool IsDeleted { get; set; }
    }

    // Mock EntityBase to match Neatoo pattern
    public abstract class EntityBase<T>
    {
        protected EntityBase() { }
    }

    [Factory]
    [AuthorizeFactory<IPersonAuth>]
    internal partial class Person : EntityBase<Person>, IPerson
    {
        [Create]
        public Person([Service] IPersonPhoneListFactory personPhoneListFactory)
        {
        }

        public bool IsDeleted { get; set; } = false;
        public bool IsNew { get; set; } = true;

        // Fetch with TWO services + CancellationToken
        [Remote]
        [Fetch]
        public async Task<bool> Fetch(
            [Service] IPersonDbContext personContext,
            [Service] IPersonPhoneListFactory personPhoneModelListFactory,
            CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            this.IsNew = false;
            return true;
        }

        // Insert returns Task<PersonEntity?> with TWO services + CancellationToken
        // NOTE: Multi-line parameter formatting with specific indentation (matches Neatoo)
        [Remote]
        [Insert]
        public async Task<PersonEntity?> Insert([Service] IPersonDbContext personContext,
                                    [Service] IPersonPhoneListFactory personPhoneModelListFactory,
                                    CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            this.IsNew = false;
            return new PersonEntity();
        }

        // Update returns Task<PersonEntity?> with TWO services + CancellationToken
        [Remote]
        [Update]
        public async Task<PersonEntity?> Update([Service] IPersonDbContext personContext,
                                    [Service] IPersonPhoneListFactory personPhoneModelListFactory,
                                    CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            return new PersonEntity();
        }

        // Delete returns Task (void) with ONE service + CancellationToken
        // NOTE: Different indentation pattern than Insert/Update!
        [Remote]
        [Delete]
        public async Task Delete([Service] IPersonDbContext personContext,
                             CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
";

        // Act
        var (diagnostics, outputCompilation, runResult) = RunGenerator(source);

        // Get the generated source
        var generatedSources = runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .Where(s => s.HintName.Contains("PersonFactory"))
            .ToList();

        Assert.NotEmpty(generatedSources);

        var generatedCode = generatedSources.First().SourceText.ToString();

        // Debug output
        Console.WriteLine("=== Generated Factory Code ===");
        Console.WriteLine(generatedCode);
        Console.WriteLine("==============================");

        // Assert - Check for duplicate Save methods
        // Count occurrences of "Task<IPerson?> Save(" in the interface section
        var interfaceStart = generatedCode.IndexOf("public interface IPersonFactory");
        var interfaceEnd = generatedCode.IndexOf("}", interfaceStart);
        var interfaceCode = generatedCode.Substring(interfaceStart, interfaceEnd - interfaceStart);

        // Use patterns that don't overlap - look for the method declaration
        var saveMethodCount = CountOccurrences(interfaceCode, "> Save(IPerson target");  // matches "Task<...> Save("
        var trySaveMethodCount = CountOccurrences(interfaceCode, "> TrySave(IPerson target");

        Console.WriteLine($"Save method count in interface: {saveMethodCount}");
        Console.WriteLine($"TrySave method count in interface: {trySaveMethodCount}");
        Console.WriteLine($"Interface code:\n{interfaceCode}");

        // Also check for duplicate delegates
        var save1DelegateExists = generatedCode.Contains("Save1Delegate");
        var saveDelegateExists = generatedCode.Contains("SaveDelegate");

        Console.WriteLine($"Save1Delegate exists: {save1DelegateExists}");
        Console.WriteLine($"SaveDelegate exists: {saveDelegateExists}");

        // If there are TWO SaveDelegates (Save1Delegate AND SaveDelegate), that's the bug
        if (save1DelegateExists && saveDelegateExists)
        {
            Console.WriteLine("BUG REPRODUCED: Two Save delegates created!");
        }

        // Check for CS0111 compilation errors
        var cs0111Errors = diagnostics.Where(d => d.Id == "CS0111").ToList();

        // ASSERTION: There should be exactly ONE Save method in the interface
        Assert.Equal(1, saveMethodCount);
        Assert.Equal(1, trySaveMethodCount);
        Assert.Empty(cs0111Errors);
        // Should NOT have both Save1Delegate and SaveDelegate - that would indicate duplicate groupings
        Assert.False(save1DelegateExists && saveDelegateExists, "Should not have both Save1Delegate and SaveDelegate");
    }

    /// <summary>
    /// Simpler test case to isolate the issue.
    /// </summary>
    [Fact]
    public void Simple_Insert_Update_Delete_With_CancellationToken_No_Duplicates()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public interface ISimple : IFactorySaveMeta
    {
        bool IsNew { get; set; }
        bool IsDeleted { get; set; }
    }

    [Factory]
    public partial class Simple : ISimple
    {
        [Create]
        public Simple()
        {
        }

        public bool IsDeleted { get; set; } = false;
        public bool IsNew { get; set; } = true;

        [Insert]
        public Task Insert(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        [Update]
        public Task Update(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        [Delete]
        public Task Delete(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
";

        // Act
        var (diagnostics, outputCompilation, runResult) = RunGenerator(source);

        var generatedSources = runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .Where(s => s.HintName.Contains("SimpleFactory"))
            .ToList();

        Assert.NotEmpty(generatedSources);

        var generatedCode = generatedSources.First().SourceText.ToString();

        var interfaceStart = generatedCode.IndexOf("public interface ISimpleFactory");
        var interfaceEnd = generatedCode.IndexOf("}", interfaceStart);
        var interfaceCode = generatedCode.Substring(interfaceStart, interfaceEnd - interfaceStart);

        var saveMethodCount = CountOccurrences(interfaceCode, "Save(ISimple target");

        Console.WriteLine($"Simple test - Save method count: {saveMethodCount}");
        Console.WriteLine($"Interface:\n{interfaceCode}");

        Assert.Equal(1, saveMethodCount);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
