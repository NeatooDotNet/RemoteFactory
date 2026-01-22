using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0202 warning: Authorization method has wrong return type.
/// </summary>
public class NF0202Tests
{
    [Fact]
    public void NF0202_AuthMethod_WrongReturnType_ReportsWarning()
    {
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

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0202", DiagnosticSeverity.Warning);
        Assert.Contains("CanRead", diagnostic.GetMessage());
        Assert.Contains("int", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0202_AuthMethod_ReturnsBool_NoWarning()
    {
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

        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0202");
        Assert.Empty(diagnostics);
    }
}
