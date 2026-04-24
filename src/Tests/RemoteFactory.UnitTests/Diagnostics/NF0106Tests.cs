using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for NF0106: Factory-operation attribute on an interface factory method.
/// Enforces Anti-Pattern 2 — interface factory methods must have no operation attributes.
/// Applies to every [Factory] interface, with or without [AuthorizeFactory&lt;T&gt;].
/// </summary>
public class NF0106Tests
{
    [Fact]
    public void NF0106_FetchOnInterfaceMethod_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public interface IRepoFetch
    {
        [Fetch]
        Task<string> GetValue(int id);
    }

    public class RepoFetch : IRepoFetch
    {
        public Task<string> GetValue(int id) => Task.FromResult(""v"");
    }
}
";
        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0106", DiagnosticSeverity.Error);
        Assert.Contains("GetValue", diagnostic.GetMessage());
        Assert.Contains("Fetch", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0106_CreateOnInterfaceMethod_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public interface IRepoCreate
    {
        [Create]
        Task<string> MakeValue();
    }

    public class RepoCreate : IRepoCreate
    {
        public Task<string> MakeValue() => Task.FromResult(""v"");
    }
}
";
        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0106", DiagnosticSeverity.Error);
        Assert.Contains("MakeValue", diagnostic.GetMessage());
        Assert.Contains("Create", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0106_ExecuteOnInterfaceMethod_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public interface IRepoExecute
    {
        [Execute]
        Task<bool> DoCommand(int input);
    }

    public class RepoExecute : IRepoExecute
    {
        public Task<bool> DoCommand(int input) => Task.FromResult(true);
    }
}
";
        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0106", DiagnosticSeverity.Error);
        Assert.Contains("DoCommand", diagnostic.GetMessage());
        Assert.Contains("Execute", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0106_DeleteOnInterfaceMethod_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public interface IRepoDelete
    {
        [Delete]
        Task<bool> Remove(int id);
    }

    public class RepoDelete : IRepoDelete
    {
        public Task<bool> Remove(int id) => Task.FromResult(true);
    }
}
";
        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0106", DiagnosticSeverity.Error);
        Assert.Contains("Remove", diagnostic.GetMessage());
        Assert.Contains("Delete", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0106_OpAttrOnAuthInterfaceMethod_ReportsDiagnostic()
    {
        // Applies regardless of whether [AuthorizeFactory<T>] is present on the interface.
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public interface IRepoAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
        bool HasAccess();
    }

    public class RepoAuth : IRepoAuth
    {
        public bool HasAccess() => true;
    }

    [Factory]
    [AuthorizeFactory<IRepoAuth>]
    public interface IAuthorizedRepo
    {
        [Fetch]
        Task<string> GetValue(int id);
    }

    public class AuthorizedRepo : IAuthorizedRepo
    {
        public Task<string> GetValue(int id) => Task.FromResult(""v"");
    }
}
";
        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0106", DiagnosticSeverity.Error);
        Assert.Contains("GetValue", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0106_CleanInterface_NoDiagnostic()
    {
        // Interface factory without any op attributes — current correct pattern, no NF0106.
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public interface IRepoClean
    {
        Task<string> GetValue(int id);
        Task<int> CountAll();
    }

    public class RepoClean : IRepoClean
    {
        public Task<string> GetValue(int id) => Task.FromResult(""v"");
        public Task<int> CountAll() => Task.FromResult(0);
    }
}
";
        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0106").ToList();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NF0106_ClassFactoryOpAttrs_NoDiagnostic()
    {
        // NF0106 is scoped to interface-declared methods only. Class factory methods
        // with op attributes are the correct pattern — must not emit NF0106.
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public partial interface IClassTarget
    {
        int Id { get; set; }
    }

    [Factory]
    internal partial class ClassTarget : IClassTarget
    {
        public int Id { get; set; }

        [Remote, Create]
        internal void Create(string name) { }

        [Remote, Fetch]
        internal void Fetch(int id) { }
    }
}
";
        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0106").ToList();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NF0106_EmitsForEachOpAttr_NoCascadeFromDuplicateCodegen()
    {
        // Multiple interface methods each with an op attribute → one NF0106 per method,
        // and no CS0111/CS0738 cascade from duplicate codegen (downstream compilation clean
        // apart from NF0106 itself).
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public interface IMultiOp
    {
        [Fetch]
        Task<string> GetA(int id);

        [Create]
        Task<string> MakeB();
    }

    public class MultiOp : IMultiOp
    {
        public Task<string> GetA(int id) => Task.FromResult(""a"");
        public Task<string> MakeB() => Task.FromResult(""b"");
    }
}
";
        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0106").ToList();
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("GetA"));
        Assert.Contains(diagnostics, d => d.GetMessage().Contains("MakeB"));
    }
}
