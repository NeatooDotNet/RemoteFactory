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

    /// <summary>
    /// The offending return type is named in readable form, without the <c>global::</c>
    /// prefix the model carries for emission.
    /// </summary>
    /// <remarks>
    /// PHASE-011 made model return types <c>global::</c>-qualified so generated code binds
    /// correctly inside a consumer's namespace. That string is also NF0102's second message
    /// argument, so the change leaked into consumer-facing build output as
    /// <c>not 'global::TestNamespace.Payload'</c> — caught by that plan's code review (C2).
    /// <para>
    /// Uses a CONSUMER type deliberately. The sibling test above returns <c>string</c>, which
    /// renders identically under both formats and therefore could not have caught this; only
    /// a type in the consumer's own namespace distinguishes them.
    /// </para>
    /// </remarks>
    [Fact]
    public void NF0102_NamesTheReturnTypeWithoutTheGlobalPrefix()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public sealed class Payload { }

    [Factory]
    public static partial class ExecuteWithConsumerTypeReturn
    {
        [Execute]
        public static Payload RunOnServer(Payload input)
        {
            return input;
        }
    }
}
";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0102", DiagnosticSeverity.Error);
        var message = diagnostic.GetMessage();

        Assert.Contains("TestNamespace.Payload", message, StringComparison.Ordinal);
        Assert.DoesNotContain("global::", message, StringComparison.Ordinal);
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
