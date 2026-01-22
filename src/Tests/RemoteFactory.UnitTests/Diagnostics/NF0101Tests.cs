using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0101 diagnostic: Static class with [Execute] attribute must be partial.
/// </summary>
public class NF0101Tests
{
    [Fact]
    public void NF0101_StaticClass_NotPartial_ReportsDiagnostic()
    {
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

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0101", DiagnosticSeverity.Error);
        Assert.Contains("NotPartialExecuteClass", diagnostic.GetMessage());
        Assert.Contains("partial", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0101_StaticPartialClass_NoDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public static partial class ValidPartialExecuteClass
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
