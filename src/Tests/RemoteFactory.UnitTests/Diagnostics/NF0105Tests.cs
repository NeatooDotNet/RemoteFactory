using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0105 diagnostic: [Remote] cannot be used with public methods.
/// [Remote] methods must be internal to enable IL trimming on client assemblies.
/// The generated factory interface method is always public regardless of source method visibility.
/// </summary>
public class NF0105Tests
{
    [Fact]
    public void NF0105_RemoteInternal_NoDiagnostic()
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

        // [Remote] internal is now the correct pattern -- no diagnostic
        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0105").ToList();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NF0105_RemoteInternalFetch_NoDiagnostic()
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

        // [Remote] internal is now the correct pattern -- no diagnostic
        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0105").ToList();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NF0105_RemotePublic_ReportsDiagnostic()
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

        // [Remote] public is now an error -- public defeats IL trimming
        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0105", DiagnosticSeverity.Error);
        Assert.Contains("Create", diagnostic.GetMessage());
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
    public void NF0105_RemoteInternalInsert_NoDiagnostic()
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

        // [Remote] internal is now the correct pattern -- no diagnostic
        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0105").ToList();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NF0105_RemotePublicFetch_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class RemotePublicFetchTarget
    {
        [Remote, Fetch]
        public void Fetch(int id) { }
    }
}
";

        // [Remote] public is now an error
        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0105", DiagnosticSeverity.Error);
        Assert.Contains("Fetch", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0105_RemotePublicStaticExecute_NoDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class StaticFactoryTarget
    {
        [Remote, Execute]
        public static Task<StaticFactoryTarget> ExecuteRemote() { return Task.FromResult(new StaticFactoryTarget()); }
    }
}
";

        // Static factory methods are exempt from NF0105 -- [Remote] public static is allowed
        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0105").ToList();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NF0105_RemotePublicInsert_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class RemotePublicInsertTarget : IFactorySaveMeta
    {
        public bool IsNew { get; set; } = true;
        public bool IsDeleted { get; set; }

        [Remote, Insert]
        public Task Insert() { return Task.CompletedTask; }

        [Update]
        internal Task Update() { return Task.CompletedTask; }

        [Delete]
        internal Task Delete() { return Task.CompletedTask; }
    }
}
";

        // [Remote] public is an error even for write operations
        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0105", DiagnosticSeverity.Error);
        Assert.Contains("Insert", diagnostic.GetMessage());
    }
}
