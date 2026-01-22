using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0102 diagnostic: Execute method must return Task or Task&lt;T&gt;.
/// </summary>
public class NF0102Tests
{
    [Fact]
    public void NF0102_ExecuteMethod_NonTask_ReportsDiagnostic()
    {
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

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0102", DiagnosticSeverity.Error);
        Assert.Contains("RunOnServer", diagnostic.GetMessage());
        Assert.Contains("Task", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0102_ExecuteMethod_ReturnsTask_NoDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public static partial class ValidExecuteClass
    {
        [Execute]
        public static Task RunOnServer(string message)
        {
            return Task.CompletedTask;
        }
    }
}
";

        DiagnosticTestHelper.AssertNoRemoteFactoryDiagnostics(source);
    }

    [Fact]
    public void NF0102_ExecuteMethod_ReturnsTaskT_NoDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public static partial class ValidExecuteWithResult
    {
        [Execute]
        public static Task<string> RunOnServer(string message)
        {
            return Task.FromResult(""result"");
        }
    }
}
";

        DiagnosticTestHelper.AssertNoRemoteFactoryDiagnostics(source);
    }
}
