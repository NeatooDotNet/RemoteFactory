using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0204 warning: Write method (Insert/Update/Delete) should not return target type.
/// </summary>
public class NF0204Tests
{
    [Fact]
    public void NF0204_WriteMethod_ReturnsTargetType_ReportsWarning()
    {
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

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0204", DiagnosticSeverity.Warning);
        Assert.Contains("Insert", diagnostic.GetMessage());
        Assert.Contains("MyClass", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0204_WriteMethod_ReturnsVoid_NoWarning()
    {
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

        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0204");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NF0204_FetchMethod_ReturnsTargetType_NoWarning()
    {
        // Fetch methods ARE allowed to return target type
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

        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0204");
        Assert.Empty(diagnostics);
    }
}
