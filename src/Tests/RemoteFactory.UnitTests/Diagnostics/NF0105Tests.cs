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
using System;
using System.Threading;
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

        // Static methods are exempt from NF0105 -- [Remote] public static is allowed. Kept at
        // EXRM-004 (AC-6): a class-level [Execute] is public static by documented convention and
        // [Remote] alone drives its guard, so there is no visibility contradiction to report.
        // Reason recorded at the check in FactoryModelBuilder.
        var (diagnostics, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "NF0105");

        // Silence must come from a factory that generated and compiles -- not from a generator
        // that crashed (CS8785) or emitted a /* Error: */ comment: FactoryRenderer swallows render
        // exceptions, so a mangled emission surfaces as output-compilation errors, not a throw.
        // The fixture's System / System.Threading usings are load-bearing for this check:
        // generated factories name IServiceProvider, InvalidOperationException, Task and
        // CancellationToken unqualified and rely on the consumer's ImplicitUsings (see
        // AssemblyAttributeEmissionTests, "The usings are required").
        Assert.NotEmpty(runResult.GeneratedTrees);
        Assert.Empty(outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// The exemption keys on the method being static, not on [Execute]: a static [Create] on a
    /// class factory is exempt too. Pinned at EXRM-004 (AC-6) so that narrowing the exemption
    /// is a deliberate change with a red test, never a side effect.
    /// </summary>
    [Fact]
    public void NF0105_RemotePublicStaticCreate_NoDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public partial class StaticCreateTarget
    {
        [Remote, Create]
        public static StaticCreateTarget CreateOnServer(int id) { return new StaticCreateTarget(); }
    }
}
";

        var (diagnostics, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "NF0105");

        // Same crash-proofing as the Execute case: the shape must actually generate and compile.
        Assert.NotEmpty(runResult.GeneratedTrees);
        Assert.Empty(outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
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
