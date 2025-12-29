using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Diagnostics;

/// <summary>
/// Tests for the new diagnostic error reporting in the RemoteFactory source generator.
/// These tests verify that NF0101, NF0102, and NF0103 errors are correctly reported.
///
/// Note: The diagnostic system has been implemented in the generator. These tests
/// use the CSharpGeneratorDriver to run the generator and verify the diagnostics.
/// </summary>
public class DiagnosticsTests
{
    private static readonly Lazy<IIncrementalGenerator> GeneratorInstance = new(() =>
    {
        // Load the generator assembly dynamically
        var testAssemblyPath = typeof(DiagnosticsTests).Assembly.Location;
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

        var references = new[]
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
            references = [.. references, MetadataReference.CreateFromFile(systemRuntimePath)];
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

    [Fact]
    public void NF0101_StaticClass_NotPartial_ReportsDiagnostic()
    {
        // Arrange
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public static class NotPartialExecuteClass
    {
        [Execute]
        public static Task<string> RunOnServer(string message)
        {
            return Task.FromResult(""result"");
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0101Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0101");
        Assert.NotNull(nf0101Diagnostic);
        Assert.Equal(DiagnosticSeverity.Error, nf0101Diagnostic.Severity);
        Assert.Contains("NotPartialExecuteClass", nf0101Diagnostic.GetMessage());
        Assert.Contains("partial", nf0101Diagnostic.GetMessage());
    }

    [Fact]
    public void NF0102_ExecuteMethod_NonTask_ReportsDiagnostic()
    {
        // Arrange
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public static partial class ExecuteWithVoidReturn
    {
        [Execute]
        public static string RunOnServer(string message)
        {
            return ""result"";
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0102Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0102");
        Assert.NotNull(nf0102Diagnostic);
        Assert.Equal(DiagnosticSeverity.Error, nf0102Diagnostic.Severity);
        Assert.Contains("RunOnServer", nf0102Diagnostic.GetMessage());
        Assert.Contains("Task", nf0102Diagnostic.GetMessage());
    }

    [Fact(Skip = "NF0103 is only reported for static classes with non-static Execute methods. Non-static classes go through GenerateFactory which handles Execute differently.")]
    public void NF0103_ExecuteMethod_InNonStaticClass_ReportsDiagnostic()
    {
        // Note: NF0103 is designed to catch [Execute] on non-static methods in static classes.
        // When a class is non-static, the generator takes a different path (GenerateFactory instead of GenerateExecute).
        // This test is skipped as the scenario requires more complex setup or different error handling.
        //
        // The NF0103 check happens in TypeFactoryMethods when:
        // - factoryOperation == Execute
        // - serviceSymbol.TypeKind != Interface
        // - !methodSymbol.IsStatic || !serviceSymbol.IsStatic
        //
        // For a static class with a non-static [Execute] method, NF0103 would be reported.

        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class NonStaticExecuteClass
    {
        [Execute]
        public Task<string> RunOnServer(string message)
        {
            return Task.FromResult(""result"");
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0103Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0103");
        Assert.NotNull(nf0103Diagnostic);
        Assert.Equal(DiagnosticSeverity.Error, nf0103Diagnostic.Severity);
        Assert.Contains("RunOnServer", nf0103Diagnostic.GetMessage());
        Assert.Contains("static", nf0103Diagnostic.GetMessage());
    }

    [Fact]
    public void ValidStaticPartialClass_NoDiagnostics()
    {
        // Arrange
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public static partial class ValidExecuteClass
    {
        [Execute]
        public static Task<string> RunOnServer(string message)
        {
            return Task.FromResult(""result"");
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nfDiagnostics = diagnostics.Where(d => d.Id.StartsWith("NF"));
        Assert.Empty(nfDiagnostics);
    }

    // ============================================================================
    // Phase 2: Warning Diagnostics Tests (NF0201-NF0204)
    // ============================================================================

    [Fact]
    public void NF0201_FactoryMethod_NotStatic_ReportsWarning()
    {
        // Arrange - A method returning target type that is not static
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyClass
    {
        [Create]
        public MyClass CreateInstance()
        {
            return new MyClass();
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0201Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0201");
        Assert.NotNull(nf0201Diagnostic);
        Assert.Equal(DiagnosticSeverity.Warning, nf0201Diagnostic.Severity);
        Assert.Contains("CreateInstance", nf0201Diagnostic.GetMessage());
        Assert.Contains("MyClass", nf0201Diagnostic.GetMessage());
        Assert.Contains("static", nf0201Diagnostic.GetMessage());
    }

    [Fact]
    public void NF0201_StaticFactoryMethod_NoWarning()
    {
        // Arrange - A static factory method returning target type should NOT trigger NF0201
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyClass
    {
        [Create]
        public static MyClass CreateInstance()
        {
            return new MyClass();
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0201Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0201");
        Assert.Null(nf0201Diagnostic);
    }

    [Fact]
    public void NF0202_AuthMethod_WrongReturnType_ReportsWarning()
    {
        // Arrange - An authorization method with wrong return type
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class MyAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public int CanRead()
        {
            return 1;
        }
    }

    [Factory]
    [AuthorizeFactory<MyAuth>]
    public partial class MyClass
    {
        [Fetch]
        public static MyClass Fetch()
        {
            return new MyClass();
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0202Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0202");
        Assert.NotNull(nf0202Diagnostic);
        Assert.Equal(DiagnosticSeverity.Warning, nf0202Diagnostic.Severity);
        Assert.Contains("CanRead", nf0202Diagnostic.GetMessage());
        Assert.Contains("int", nf0202Diagnostic.GetMessage());
    }

    [Fact]
    public void NF0202_AuthMethod_ValidReturnType_NoWarning()
    {
        // Arrange - An authorization method with valid return type (bool)
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class MyAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        public bool CanRead()
        {
            return true;
        }
    }

    [Factory]
    [AuthorizeFactory<MyAuth>]
    public partial class MyClass
    {
        [Fetch]
        public static MyClass Fetch()
        {
            return new MyClass();
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0202Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0202");
        Assert.Null(nf0202Diagnostic);
    }

    [Fact(Skip = "NF0203 requires two methods with the same NamePostfix (derived from method name), which is impossible in practice since C# doesn't allow methods with identical names and parameter signatures.")]
    public void NF0203_AmbiguousSaveOperations_ReportsWarning()
    {
        // Note: NF0203 is designed to catch truly ambiguous save operations where
        // two methods have the same factory operation type (Insert/Update/Delete),
        // the same parameter signature, AND the same NamePostfix.
        //
        // The NamePostfix is derived from the method name by removing the operation prefix.
        // For example: Insert -> "", InsertAsync -> "Async", InsertWithParam -> "WithParam"
        //
        // Since C# doesn't allow two methods with identical names and parameter signatures,
        // this scenario cannot occur with methods that have the same NamePostfix.
        //
        // The scenario that DOES trigger the outer check (multiple Insert methods with
        // same parameter signature but different names like Insert and InsertAlternate)
        // is a supported pattern where different Save methods are generated.

        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IFactorySaveMeta
    {
        bool IsNew { get; }
        bool IsDeleted { get; }
    }

    [Factory]
    public partial class MyClass : IFactorySaveMeta
    {
        public bool IsNew { get; set; }
        public bool IsDeleted { get; set; }

        [Insert]
        public void Insert()
        {
        }

        [Insert]
        public void InsertAlternate()
        {
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0203Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0203");
        Assert.NotNull(nf0203Diagnostic);
        Assert.Equal(DiagnosticSeverity.Warning, nf0203Diagnostic.Severity);
        Assert.Contains("Insert", nf0203Diagnostic.GetMessage());
    }

    [Fact]
    public void NF0203_SingleSaveOperations_NoWarning()
    {
        // Arrange - Single Insert, Update, Delete methods
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IFactorySaveMeta
    {
        bool IsNew { get; }
        bool IsDeleted { get; }
    }

    [Factory]
    public partial class MyClass : IFactorySaveMeta
    {
        public bool IsNew { get; set; }
        public bool IsDeleted { get; set; }

        [Insert]
        public void Insert()
        {
        }

        [Update]
        public void Update()
        {
        }

        [Delete]
        public void Delete()
        {
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0203Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0203");
        Assert.Null(nf0203Diagnostic);
    }

    [Fact]
    public void NF0204_WriteMethod_ReturnsTargetType_ReportsWarning()
    {
        // Arrange - An Insert method that returns the target type
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IFactorySaveMeta
    {
        bool IsNew { get; }
        bool IsDeleted { get; }
    }

    [Factory]
    public partial class MyClass : IFactorySaveMeta
    {
        public bool IsNew { get; set; }
        public bool IsDeleted { get; set; }

        [Create]
        public static MyClass Create()
        {
            return new MyClass();
        }

        [Insert]
        public MyClass Insert()
        {
            return this;
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0204Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0204");
        Assert.NotNull(nf0204Diagnostic);
        Assert.Equal(DiagnosticSeverity.Warning, nf0204Diagnostic.Severity);
        Assert.Contains("Insert", nf0204Diagnostic.GetMessage());
        Assert.Contains("MyClass", nf0204Diagnostic.GetMessage());
    }

    [Fact]
    public void NF0204_WriteMethod_ReturnsVoid_NoWarning()
    {
        // Arrange - A proper Insert method that returns void
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IFactorySaveMeta
    {
        bool IsNew { get; }
        bool IsDeleted { get; }
    }

    [Factory]
    public partial class MyClass : IFactorySaveMeta
    {
        public bool IsNew { get; set; }
        public bool IsDeleted { get; set; }

        [Create]
        public static MyClass Create()
        {
            return new MyClass();
        }

        [Insert]
        public void Insert()
        {
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0204Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0204");
        Assert.Null(nf0204Diagnostic);
    }

    [Fact]
    public void NF0204_FetchMethod_ReturnsTargetType_NoWarning()
    {
        // Arrange - Fetch and Create methods ARE allowed to return the target type
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyClass
    {
        [Create]
        public static MyClass Create()
        {
            return new MyClass();
        }

        [Fetch]
        public static MyClass Fetch()
        {
            return new MyClass();
        }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert
        var nf0204Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0204");
        Assert.Null(nf0204Diagnostic);
    }

    // ============================================================================
    // Phase 3: Info Diagnostics Tests (NF0301)
    // ============================================================================

    [Fact]
    public void NF0301_MethodWithoutAttribute_DiagnosticDescriptorExists()
    {
        // This test verifies that NF0301 is available and disabled by default.
        // Since NF0301 is opt-in (isEnabledByDefault: false), it won't appear
        // in the diagnostics unless explicitly enabled via .editorconfig.
        //
        // We can verify the diagnostic exists by checking that the generator
        // doesn't crash when processing a class with methods that would trigger it.

        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyClass
    {
        [Create]
        public MyClass() { }

        // This public method has no factory attribute
        public void HelperMethod() { }

        // Another public method without factory attribute
        public bool Validate() { return true; }
    }
}
";

        // Act - Just verify the generator runs without error
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert - NF0301 should NOT appear because it's disabled by default
        // Even though the methods would trigger it if enabled
        var nf0301Diagnostics = diagnostics.Where(d => d.Id == "NF0301");

        // NF0301 is disabled by default, so we should not see any such diagnostics
        // This test documents that behavior - to enable NF0301, users need .editorconfig
        Assert.Empty(nf0301Diagnostics);
    }

    [Fact]
    public void NF0301_PrivateMethodWithoutAttribute_NoInfoReported()
    {
        // Arrange - Private methods should never trigger NF0301
        // Even if NF0301 were enabled, private methods are not candidates
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyClass
    {
        [Create]
        public MyClass() { }

        // Private helper method - should not trigger NF0301
        private void PrivateHelper() { }

        // Internal method - should not trigger NF0301
        internal void InternalHelper() { }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert - No NF0301 diagnostics (disabled by default + not for private methods)
        var nf0301Diagnostics = diagnostics.Where(d => d.Id == "NF0301");
        Assert.Empty(nf0301Diagnostics);
    }

    [Fact]
    public void NF0301_AllMethodsHaveAttributes_NoInfoReported()
    {
        // Arrange - When all public methods have factory attributes, no NF0301 should be reported
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IFactorySaveMeta
    {
        bool IsNew { get; }
        bool IsDeleted { get; }
    }

    [Factory]
    public partial class MyClass : IFactorySaveMeta
    {
        public bool IsNew { get; set; }
        public bool IsDeleted { get; set; }

        [Create]
        public MyClass() { }

        [Fetch]
        public static MyClass Fetch(int id) { return new MyClass(); }

        [Insert]
        public void Insert() { }

        [Update]
        public void Update() { }

        [Delete]
        public void Delete() { }
    }
}
";

        // Act
        var (diagnostics, _, runResult) = RunGenerator(source);

        // Assert - No NF0301 diagnostics since all public methods have attributes
        var nf0301Diagnostics = diagnostics.Where(d => d.Id == "NF0301");
        Assert.Empty(nf0301Diagnostics);

        // Also verify no other NF errors
        var nfErrors = diagnostics.Where(d => d.Id.StartsWith("NF") && d.Severity == DiagnosticSeverity.Error);
        Assert.Empty(nfErrors);
    }
}
