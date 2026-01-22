using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0206 error: record struct not supported.
/// </summary>
public class NF0206Tests
{
    [Fact]
    public void NF0206_RecordStruct_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record struct ValueRecord(string Name);
}";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0206", DiagnosticSeverity.Error);
        Assert.Contains("ValueRecord", diagnostic.GetMessage());
        Assert.Contains("record struct", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0206_RecordClass_NoDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record class ReferenceRecord(string Name);
}";

        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0206");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NF0206_Record_ImplicitClass_NoDiagnostic()
    {
        // Implicit record class (no 'class' keyword) is valid
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record ImplicitClassRecord(string Name);
}";

        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0206");
        Assert.Empty(diagnostics);
    }
}
