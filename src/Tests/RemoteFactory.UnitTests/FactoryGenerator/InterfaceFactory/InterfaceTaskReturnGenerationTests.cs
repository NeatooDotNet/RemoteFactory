using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.InterfaceFactory;

/// <summary>
/// An interface-factory method returning non-generic <c>Task</c> generates code that compiles (#122).
/// </summary>
/// <remarks>
/// The interface renderer wrapped the method's return type in <c>Task&lt;&gt;</c> whenever it did
/// not start with "Task". A non-generic <c>Task</c> is recorded fully qualified
/// (<c>global::System.Threading.Tasks.Task</c>), so it was wrapped again into
/// <c>Task&lt;Task&gt;</c> and failed CS0738/CS0266 in the generated file, with no NF diagnostic.
/// This is #113's defect on the interface-factory leg. Each source carries
/// <c>using System;</c> and <c>using System.Threading;</c> for #97.
/// </remarks>
public class InterfaceTaskReturnGenerationTests
{
    [Fact]
    public void InterfaceMethod_ReturningTask_GeneratedCodeCompiles()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public interface IThingService
    {
        Task Close(Guid id);
        Task<int> Count();
    }
}
";

        AssertGeneratedCodeCompiles(source);
    }

    [Fact]
    public void AuthorizedInterfaceMethod_ReturningTask_WithAsyncAuth_GeneratedCodeCompiles()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public interface IThingAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
        Task<bool> CanClose(Guid id);
    }

    [Factory]
    [AuthorizeFactory<IThingAuth>]
    public interface IThingService
    {
        Task Close(Guid id);
    }
}
";

        AssertGeneratedCodeCompiles(source);
    }

    [Fact]
    public void InterfaceMethod_ReturningTask_GeneratesTaskDelegateAndMethod()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public interface IThingService
    {
        Task Close(Guid id);
    }
}
";

        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);
        var generated = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));

        Assert.Contains("public delegate Task CloseDelegate(Guid id);", generated, StringComparison.Ordinal);
        Assert.Contains("public virtual Task Close(Guid id)", generated, StringComparison.Ordinal);
        Assert.Contains("ForDelegateNoResult(typeof(CloseDelegate)", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Task<global::System.Threading.Tasks.Task>", generated, StringComparison.Ordinal);
    }

    private static void AssertGeneratedCodeCompiles(string source)
    {
        var (diagnostics, outputCompilation, _) = DiagnosticTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Id.StartsWith("NF", StringComparison.Ordinal)));

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"{d.Id}: {d.GetMessage()}")
            .ToList();

        Assert.True(errors.Count == 0, "Generated code does not compile:\n" + string.Join("\n", errors));
    }
}
