using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator;

/// <summary>
/// Verifies that the generator emits [assembly: NeatooFactoryRegistrar(typeof(...))]
/// for all three factory patterns (class, static, interface) and that the removed
/// [DynamicDependency] and using System.Diagnostics.CodeAnalysis are absent.
/// </summary>
public class AssemblyAttributeEmissionTests
{
    #region Class Factory

    /// <summary>
    /// Class factory generated source contains the assembly-level NeatooFactoryRegistrar
    /// attribute with the fully-qualified factory type name.
    /// </summary>
    [Fact]
    public void ClassFactory_EmitsAssemblyAttribute()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyEntityFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.Contains("[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(TestNamespace.MyEntityFactory))]", generatedSource);
    }

    /// <summary>
    /// Class factory generated source does NOT contain [DynamicDependency] (removed in favor
    /// of the assembly-level attribute).
    /// </summary>
    [Fact]
    public void ClassFactory_DoesNotEmitDynamicDependency()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyEntityFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.DoesNotContain("[DynamicDependency", generatedSource);
    }

    /// <summary>
    /// Class factory generated source does NOT contain the using directive for
    /// System.Diagnostics.CodeAnalysis (was only needed for [DynamicDependency]).
    /// </summary>
    [Fact]
    public void ClassFactory_DoesNotEmitDiagnosticsCodeAnalysisUsing()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyEntityFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.DoesNotContain("using System.Diagnostics.CodeAnalysis;", generatedSource);
    }

    #endregion

    #region Static Factory

    /// <summary>
    /// Static factory generated source contains the assembly-level NeatooFactoryRegistrar
    /// attribute with the fully-qualified static class type name.
    /// </summary>
    [Fact]
    public void StaticFactory_EmitsAssemblyAttribute()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public static partial class MyCommands
    {
        [Execute]
        private static Task<string> _DoWork(string input)
        {
            return Task.FromResult(input);
        }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyCommands"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.Contains("[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(TestNamespace.MyCommands))]", generatedSource);
    }

    #endregion

    #region Interface Factory

    /// <summary>
    /// Interface factory generated source contains the assembly-level NeatooFactoryRegistrar
    /// attribute with the fully-qualified implementation factory type name.
    /// </summary>
    [Fact]
    public void InterfaceFactory_EmitsAssemblyAttribute()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public interface IMyService
    {
        Task<string> DoWork(string input);
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyServiceFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.Contains("[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(TestNamespace.MyServiceFactory))]", generatedSource);
    }

    #endregion
}
