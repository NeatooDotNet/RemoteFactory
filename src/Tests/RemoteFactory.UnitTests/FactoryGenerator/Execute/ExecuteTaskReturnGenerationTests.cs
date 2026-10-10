using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.Execute;

/// <summary>
/// A static <c>[Execute]</c> returning non-generic <c>Task</c> generates code that compiles (#113).
/// </summary>
/// <remarks>
/// NF0102 has always accepted <c>Task</c> ("must return Task or Task&lt;T&gt;"), and
/// <c>NF0102Tests</c> pins that no NF diagnostic fires for it. But that test checks only the NF
/// diagnostics, never the generated code — and the generated delegate was declared
/// <c>Task&lt;global::System.Threading.Tasks.Task&gt;</c>, failing CS0266 inside the consumer's
/// <c>obj/</c> with no rule to look up. These tests compile the generator's output, which is the
/// only place the defect shows.
/// <para>
/// Each source carries <c>using System.Threading;</c> because generated files copy the source
/// file's usings and name <c>CancellationToken</c> unqualified; without it every
/// <c>[Execute]</c>, <c>Task&lt;T&gt;</c> included, fails CS0246 here, which is not this defect
/// (it is #97: generated code relies on the consumer's implicit usings).
/// </para>
/// </remarks>
public class ExecuteTaskReturnGenerationTests
{
    [Fact]
    public void RemoteExecute_ReturningTask_GeneratedCodeCompiles()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public interface INotifier { Task SendAsync(string message); }

    [Factory]
    public static partial class Commands
    {
        [Remote, Execute]
        private static Task _Notify(string message, [Service] INotifier notifier)
            => notifier.SendAsync(message);
    }
}
";

        AssertGeneratedCodeCompiles(source);
    }

    [Fact]
    public void LocalExecute_ReturningTask_GeneratedCodeCompiles()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public static partial class Commands
    {
        [Execute]
        private static Task _Pause(int milliseconds, CancellationToken cancellationToken)
            => Task.Delay(milliseconds, cancellationToken);
    }
}
";

        AssertGeneratedCodeCompiles(source);
    }

    [Fact]
    public void RemoteExecute_ReturningTask_DelegateReturnsTask()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public static partial class Commands
    {
        [Remote, Execute]
        private static Task _Notify(string message) => Task.CompletedTask;
    }
}
";

        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);
        var generated = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));

        Assert.Contains("public delegate Task Notify(string message, CancellationToken cancellationToken = default);", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Task<global::System.Threading.Tasks.Task>", generated, StringComparison.Ordinal);
    }

    private static void AssertGeneratedCodeCompiles(string source)
    {
        var (diagnostics, outputCompilation, _) = DiagnosticTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Id.StartsWith("NF", StringComparison.Ordinal)));

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => $"{d.Id}: {d.GetMessage()} @ {d.Location.SourceTree?.FilePath}")
            .ToList();

        Assert.True(errors.Count == 0, "Generated code does not compile:\n" + string.Join("\n", errors));
    }
}
