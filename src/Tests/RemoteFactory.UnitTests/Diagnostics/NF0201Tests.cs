using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0201 warning: Factory method should be static.
/// </summary>
public class NF0201Tests
{
    [Fact]
    public void NF0201_FactoryMethod_NotStatic_ReportsWarning()
    {
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

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0201", DiagnosticSeverity.Warning);
        Assert.Contains("CreateInstance", diagnostic.GetMessage());
        Assert.Contains("MyClass", diagnostic.GetMessage());
        Assert.Contains("static", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0201_StaticFactoryMethod_NoWarning()
    {
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

        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0201");
        Assert.Empty(diagnostics);
    }
}
