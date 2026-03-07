using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0105 diagnostic: [Remote] cannot be used with internal methods.
/// [Remote] marks a method as a client-to-server entry point. Internal methods
/// are not visible to clients. These modifiers are contradictory.
/// </summary>
public class NF0105Tests
{
    [Fact]
    public void NF0105_RemoteInternal_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class RemoteInternalTarget
    {
        [Remote, Create]
        internal void Create() { }
    }
}
";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0105", DiagnosticSeverity.Error);
        Assert.Contains("Create", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0105_RemoteInternalFetch_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class RemoteInternalFetchTarget
    {
        [Remote, Fetch]
        internal void Fetch(int id) { }
    }
}
";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0105", DiagnosticSeverity.Error);
        Assert.Contains("Fetch", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0105_RemotePublic_NoDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class RemotePublicTarget
    {
        [Remote, Create]
        public void Create() { }
    }
}
";

        // No NF0105 should be emitted for public [Remote] methods
        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0105").ToList();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NF0105_InternalWithoutRemote_NoDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class InternalNoRemoteTarget
    {
        [Create]
        internal void Create() { }
    }
}
";

        // No NF0105 for internal methods without [Remote]
        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0105").ToList();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NF0105_RemoteInternalInsert_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class RemoteInternalInsertTarget : IFactorySaveMeta
    {
        public bool IsNew { get; set; } = true;
        public bool IsDeleted { get; set; }

        [Remote, Insert]
        internal Task Insert() { return Task.CompletedTask; }

        [Update]
        public Task Update() { return Task.CompletedTask; }

        [Delete]
        public Task Delete() { return Task.CompletedTask; }
    }
}
";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0105", DiagnosticSeverity.Error);
        Assert.Contains("Insert", diagnostic.GetMessage());
    }
}
