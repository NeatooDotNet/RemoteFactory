using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0205 error: [Create] on type requires record with primary constructor.
/// </summary>
public class NF0205Tests
{
    [Fact]
    public void NF0205_CreateOnNonRecord_Class_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public class NotARecord
    {
        public string Name { get; set; }
    }
}";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0205", DiagnosticSeverity.Error);
        Assert.Contains("NotARecord", diagnostic.GetMessage());
        Assert.Contains("primary constructor", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0205_CreateOnRecordWithoutPrimaryConstructor_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record RecordWithoutPrimaryConstructor
    {
        public string Name { get; init; }
    }
}";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0205", DiagnosticSeverity.Error);
        Assert.Contains("RecordWithoutPrimaryConstructor", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0205_CreateOnRecordWithPrimaryConstructor_NoDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record ValidRecord(string Name, int Value);
}";

        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0205");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NF0205_CreateOnConstructor_Class_NoDiagnostic()
    {
        // [Create] on an explicit constructor is valid for classes
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public class ClassWithExplicitCreate
    {
        public string Name { get; set; }

        [Create]
        public ClassWithExplicitCreate(string name)
        {
            Name = name;
        }
    }
}";

        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0205");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NF0205_CreateOnConstructor_RecordWithoutPrimaryConstructor_NoDiagnostic()
    {
        // [Create] on explicit constructor of a record is valid
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public record RecordWithExplicitCreate
    {
        public string Name { get; init; }

        [Create]
        public RecordWithExplicitCreate(string name)
        {
            Name = name;
        }
    }
}";

        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0205");
        Assert.Empty(diagnostics);
    }
}
