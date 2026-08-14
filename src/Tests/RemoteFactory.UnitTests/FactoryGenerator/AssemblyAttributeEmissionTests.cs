using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator;

/// <summary>
/// Verifies that the generator emits [assembly: NeatooFactoryRegistrar(typeof(...))]
/// for all three factory patterns (class, static, interface) and that the removed
/// [DynamicDependency] and using System.Diagnostics.CodeAnalysis are absent.
/// </summary>
public class AssemblyAttributeEmissionTests
{
    #region Class Factory

    /// <summary>
    /// Class factory generated source contains the assembly-level NeatooFactoryRegistrar
    /// attribute with the fully-qualified factory type name.
    /// </summary>
    [Fact]
    public void ClassFactory_EmitsAssemblyAttribute()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyEntityFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // The attribute names the generated single-method HOLDER, never the factory.
        // Inverted by TRIM-009: this assertion previously expected
        // `typeof(global::TestNamespace.MyEntityFactory)`. Original intent is preserved —
        // the attribute is still emitted and still names the correct type — but the
        // correct type changed, because [DynamicallyAccessedMembers(PublicMethods |
        // NonPublicMethods)] on the attribute roots EVERY method on whatever it names,
        // bodies included. {X}Factory hosts every Local*, so naming it kept [Remote]
        // bodies on publish-trimmed clients. Measured, TRIM-009.
        Assert.Contains(
            "[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(global::TestNamespace.NeatooClassFactoryRegistrar_MyEntity))]",
            generatedSource);

        // The regression assertion whose absence let the static leg ship broken for a year:
        // assert the attribute does NOT name the factory type.
        Assert.DoesNotContain(
            "NeatooFactoryRegistrar(typeof(global::TestNamespace.MyEntityFactory))",
            generatedSource);
    }

    /// <summary>
    /// The class-factory registrar holder is emitted as a top-level type with exactly one
    /// method, forwarding to the factory's own registrar.
    /// </summary>
    /// <remarks>
    /// Anchored with <see cref="Assert.Matches(string, string)"/> binding the signature to the
    /// holder's class declaration. A bare Contains on the signature line is satisfied by
    /// <c>{X}Factory.FactoryServiceRegistrar</c>, which emits a byte-identical line — that is
    /// exactly how TRIM-008's first version of this test passed while pinning nothing.
    /// </remarks>
    [Fact]
    public void ClassFactory_EmitsRegistrarHolder_ForwardingToFactory()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyEntityFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        Assert.Matches(
            @"internal static class NeatooClassFactoryRegistrar_MyEntity\s*\{\s*internal static void FactoryServiceRegistrar\(IServiceCollection services, NeatooFactory remoteLocal\)",
            generatedSource);

        Assert.Contains(
            "global::TestNamespace.MyEntityFactory.FactoryServiceRegistrar(services, remoteLocal);",
            generatedSource);
    }

    /// <summary>
    /// A guarded <c>async</c> <c>Local*</c> method is emitted as a NON-async wrapper carrying
    /// the feature-switch guard, forwarding to a private async core.
    /// </summary>
    /// <remarks>
    /// The guard must not sit inside the async state machine. When it does, the compiler
    /// lowers it into <c>MoveNext</c> inside the builder's protected region, ILLink folds the
    /// switch but does not eliminate the unreachable remainder, and the <c>[Remote]</c> body
    /// ships to trimmed clients. Measured, TRIM-009 — stripping the async lifecycle probes
    /// and the OperationCanceledException arm did not fix it, and adding them to a sync
    /// method did not break it.
    /// </remarks>
    [Fact]
    public void ClassFactory_GuardedAsyncLocalMethod_SplitsIntoSyncWrapperAndAsyncCore()
    {
        var source = @"
using System.Threading.Tasks;
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyEntity
    {
        [Remote]
        [Fetch]
        internal async Task FetchIt(string name) { await Task.CompletedTask; }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyEntityFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Wrapper: NOT async, carries the guard, forwards to the core.
        Assert.Matches(
            @"public Task<MyEntity> LocalFetchIt\(string name, CancellationToken cancellationToken = default\)\s*\{\s*if \(!NeatooRuntime\.IsServerRuntime\)",
            generatedSource);
        Assert.Contains("return LocalFetchItCore(name, cancellationToken);", generatedSource);

        // Core: async, private, and carries NO guard — the guard already ran in the wrapper.
        Assert.Contains("private async Task<MyEntity> LocalFetchItCore(", generatedSource);
        Assert.Matches(
            @"private async Task<MyEntity> LocalFetchItCore\([^)]*\)\s*\{\s*(?!\s*if \(!NeatooRuntime\.IsServerRuntime\))",
            generatedSource);
    }

    /// <summary>
    /// A SYNCHRONOUS guarded <c>Local*</c> method keeps the guard inline and is not split.
    /// </summary>
    /// <remarks>
    /// The sync shape already trims correctly — unreachability begins before any protected
    /// region, so the whole remainder goes. Splitting it would be churn. This test is the
    /// control that keeps the wrapper narrowly scoped to the shape that needed it.
    /// </remarks>
    [Fact]
    public void ClassFactory_GuardedSyncLocalMethod_IsNotSplit()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyEntity
    {
        [Remote]
        [Create]
        internal void Create(string name) { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyEntityFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.DoesNotContain("LocalCreateCore", generatedSource);
        Assert.Matches(
            @"public Task<MyEntity> LocalCreate\(string name, CancellationToken cancellationToken = default\)\s*\{\s*if \(!NeatooRuntime\.IsServerRuntime\)",
            generatedSource);
    }

    /// <summary>
    /// Class factory generated source does NOT contain [DynamicDependency] (removed in favor
    /// of the assembly-level attribute).
    /// </summary>
    [Fact]
    public void ClassFactory_DoesNotEmitDynamicDependency()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyEntityFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.DoesNotContain("[DynamicDependency", generatedSource);
    }

    /// <summary>
    /// Class factory generated source does NOT contain the using directive for
    /// System.Diagnostics.CodeAnalysis (was only needed for [DynamicDependency]).
    /// </summary>
    [Fact]
    public void ClassFactory_DoesNotEmitDiagnosticsCodeAnalysisUsing()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MyEntity
    {
        [Create]
        internal void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyEntityFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.DoesNotContain("using System.Diagnostics.CodeAnalysis;", generatedSource);
    }

    #endregion

    #region Static Factory

    // The usings are required, not decorative: without System.Threading.Tasks the fixture's
    // Task<string> is an error type, and StaticFactory_RegistrarHolder_ForwardsToUserClass —
    // which asserts the generated output actually compiles — fails on the fixture rather than
    // on the emission. String-containment assertions never noticed.
    private const string StaticFactorySource = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    [Factory]
    public static partial class MyCommands
    {
        [Execute]
        private static Task<string> _DoWork(string input)
        {
            return Task.FromResult(input);
        }
    }
}
";

    /// <summary>
    /// Static factory generated source contains the assembly-level NeatooFactoryRegistrar
    /// attribute naming the correct type.
    /// </summary>
    /// <remarks>
    /// Retargeted by TRIM-008. The original pinned <c>typeof(global::TestNamespace.MyCommands)</c>
    /// — the user's own class — which is the defect: the attribute's
    /// [DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)] retains every method on
    /// whatever type it names, bodies included, so that shipped <c>_DoWork</c>'s server-only
    /// body to trimmed clients. The test's intent is unchanged and deliberately preserved:
    /// "the registrar attribute is emitted, and it names the correct type". Only what counts
    /// as correct has changed, from the consumer's class to the generated forwarding holder.
    /// </remarks>
    [Fact]
    public void StaticFactory_EmitsAssemblyAttribute()
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(StaticFactorySource);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyCommands"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.Contains("[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(global::TestNamespace.NeatooFactoryRegistrar_MyCommands))]", generatedSource);
    }

    /// <summary>
    /// The static factory's registrar attribute must NOT name the consumer's own class.
    /// This is the assertion whose absence let the defect ship.
    /// </summary>
    /// <remarks>
    /// The closing paren in the expected substring is load-bearing. Without it,
    /// <c>typeof(global::TestNamespace.MyCommands</c> is a prefix of any name that merely
    /// starts with the user's type, and the assertion would pass or fail for the wrong
    /// reason. The holder is prefixed rather than suffixed for the same reason — a suffixed
    /// <c>MyCommandsNeatooFactoryRegistrar</c> would keep the user's FQN a substring of the
    /// holder's, making this a false red.
    /// </remarks>
    [Fact]
    public void StaticFactory_AssemblyAttribute_DoesNotNameConsumerType()
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(StaticFactorySource);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyCommands"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.DoesNotContain("NeatooFactoryRegistrar(typeof(global::TestNamespace.MyCommands))", generatedSource);
    }

    /// <summary>
    /// The holder forwards to the registrar that stays on the user's partial class.
    /// </summary>
    /// <remarks>
    /// Forwarding, not hosting: [Execute] methods are private static by the repo's own
    /// convention and the registrar body calls them, so a sibling holder that hosted the
    /// registrar would be CS0122. The method name is pinned because
    /// <c>AddRemoteFactoryServices</c> looks it up by that literal string and calls
    /// <c>method?.Invoke</c> — a rename stops registration silently, with no diagnostic
    /// and no exception.
    /// </remarks>
    [Fact]
    public void StaticFactory_RegistrarHolder_ForwardsToUserClass()
    {
        var (_, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(StaticFactorySource);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyCommands"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // The method signature must be asserted INSIDE the holder, not merely present in the
        // file. The user's partial class emits a byte-identical FactoryServiceRegistrar
        // signature, so a bare Contains(...) for it is satisfied by that one and would still
        // pass if the holder's method were renamed — which is the single failure mode that is
        // silent, because AddRemoteFactoryServices looks it up by literal name and calls
        // method?.Invoke. Anchoring the signature to the holder's class declaration is what
        // actually pins plan Constraint "the holder's method must remain exactly
        // FactoryServiceRegistrar".
        Assert.Matches(
            @"internal static class NeatooFactoryRegistrar_MyCommands\s*\{\s*internal static void FactoryServiceRegistrar\(IServiceCollection services, NeatooFactory remoteLocal\)",
            generatedSource);
        Assert.Contains("global::TestNamespace.MyCommands.FactoryServiceRegistrar(services, remoteLocal);", generatedSource);

        Assert.Empty(outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    #endregion

    #region Interface Factory

    /// <summary>
    /// Interface factory generated source contains the assembly-level NeatooFactoryRegistrar
    /// attribute with the fully-qualified implementation factory type name.
    /// </summary>
    [Fact]
    public void InterfaceFactory_EmitsAssemblyAttribute()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public interface IMyService
    {
        Task<string> DoWork(string input);
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyServiceFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.Contains("[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(global::TestNamespace.MyServiceFactory))]", generatedSource);
    }

    #endregion

    #region Relay Handler

    // The [FactoryEventHandler<T>] leg had NO emission tests before TRIM-008 — which is
    // how its registrar attribute went four releases naming the consumer's own class,
    // and unqualified, without anything noticing.
    //
    // NF0502 TRAP: two static handlers matching the same event type on one class is an
    // ambiguous match. The transform reports and `continue`s without adding an entry, and
    // FactoryGenerator returns early on Entries.Count == 0 — so the fixture would generate
    // NOTHING and every assertion below would fail for a reason unrelated to what it tests.
    // Exactly one handler, one event type.
    private const string RelayHandlerSource = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record MyEvent(int Id) : FactoryEventBase;

    public interface IMyPort
    {
        Task Send(string message);
    }

    [FactoryEventHandler<MyEvent>]
    public static partial class MyHandlers
    {
        internal static Task Handle(MyEvent evt, [Service] IMyPort port)
        {
            return port.Send(""handled"");
        }
    }
}
";

    /// <summary>
    /// Relay-handler generated source contains the assembly-level NeatooFactoryRegistrar
    /// attribute naming the generated holder, <c>global::</c>-qualified.
    /// </summary>
    [Fact]
    public void RelayHandler_EmitsAssemblyAttribute()
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(RelayHandlerSource);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyHandlers"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.Contains("[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(global::TestNamespace.NeatooEventHandlerRegistrar_MyHandlers))]", generatedSource);
    }

    /// <summary>
    /// The relay-handler registrar attribute must NOT name the consumer's own handler class.
    /// </summary>
    /// <remarks>
    /// Naming the handler class made ILLink retain every method on it, so the handler body
    /// and the server-only service it reaches shipped to trimmed clients. Measured present
    /// before this fix and absent after, in the publish-trimmed harness.
    /// </remarks>
    [Fact]
    public void RelayHandler_AssemblyAttribute_DoesNotNameConsumerType()
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(RelayHandlerSource);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyHandlers"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.DoesNotContain("NeatooFactoryRegistrar(typeof(global::TestNamespace.MyHandlers))", generatedSource);

        // The pre-fix emission was also missing global::. Pin its absence explicitly:
        // an unqualified argument binds to the wrong type when a consumer namespace
        // shadows the first segment of this one.
        Assert.DoesNotContain("NeatooFactoryRegistrar(typeof(TestNamespace.", generatedSource);
    }

    /// <summary>
    /// The relay-handler holder forwards to the registrar on the user's partial class.
    /// </summary>
    /// <remarks>
    /// The holder prefix differs from the static-factory leg's on purpose. A class carrying
    /// both [Factory] and [FactoryEventHandler&lt;T&gt;] already emits duplicate
    /// FactoryServiceRegistrar members (CS0111, broken at HEAD, Deferred Work item 15);
    /// a shared prefix would stack a CS0101 duplicate-type error on top of it.
    /// </remarks>
    [Fact]
    public void RelayHandler_RegistrarHolder_ForwardsToUserClass()
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(RelayHandlerSource);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MyHandlers"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Anchored to the holder's class declaration for the same reason as the static leg:
        // the user's partial class emits an identical signature, so an unanchored assertion
        // would still pass with the holder's method renamed — and that rename fails silently
        // at runtime (method?.Invoke).
        Assert.Matches(
            @"internal static class NeatooEventHandlerRegistrar_MyHandlers\s*\{\s*internal static void FactoryServiceRegistrar\(IServiceCollection services, NeatooFactory remoteLocal\)",
            generatedSource);
        Assert.Contains("global::TestNamespace.MyHandlers.FactoryServiceRegistrar(services, remoteLocal);", generatedSource);

        // Distinct from the static-factory prefix, so the two never collide.
        Assert.DoesNotContain("NeatooFactoryRegistrar_MyHandlers", generatedSource);
    }

    /// <summary>
    /// The relay-handler output compiles without errors.
    /// </summary>
    /// <remarks>
    /// Not redundant with the string assertions above. Relay output bypasses
    /// <c>NormalizeWhitespace</c> entirely, and <c>FactoryRenderer</c> parses with error
    /// recovery and swallows throws into a <c>/* Error: */</c> comment — so malformed
    /// emission yields MANGLED OUTPUT, NOT AN EXCEPTION. String containment can pass on
    /// source that does not compile. TRIM-008 adds a whole new top-level type to this
    /// unnormalized output, which is exactly the change that could produce that.
    /// </remarks>
    [Fact]
    public void RelayHandler_GeneratedOutputCompilesWithoutErrors()
    {
        var (_, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(RelayHandlerSource);

        // Without this, the test passes on ZERO generated trees: if the fixture ever hits
        // NF0502 or any transform early-out, the generator emits nothing, the input
        // compilation is clean, and Assert.Empty(errors) is trivially satisfied. The other
        // relay tests would fail in that state; this one would not.
        Assert.NotNull(runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("MyHandlers")));

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(errors);
    }

    /// <summary>
    /// A <c>private static</c> handler compiles — the reason the holder forwards rather than hosts.
    /// </summary>
    /// <remarks>
    /// <c>FactoryGenerator.RelayHandler</c> filters only on <c>IsStatic</c>, with no accessibility
    /// gate, so private handlers are legal today. That is precisely why a sibling holder cannot
    /// *host* the registrar (it could not reach them — CS0122) and must forward to the user's
    /// partial instead. The static leg already exercises this via its `private static` fixture;
    /// the relay leg did not.
    /// </remarks>
    [Fact]
    public void RelayHandler_PrivateHandler_GeneratedOutputCompilesWithoutErrors()
    {
        var source = RelayHandlerSource.Replace(
            "internal static Task Handle(",
            "private static Task Handle(");

        // If the shared fixture's text drifts, Replace silently no-ops and this test quietly
        // becomes a duplicate of the internal-handler test — green, and testing nothing.
        Assert.NotEqual(RelayHandlerSource, source);
        Assert.Contains("private static Task Handle(", source);

        var (_, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(source);

        Assert.NotNull(runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("MyHandlers")));
        Assert.Empty(outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// A class carrying BOTH <c>[Factory]</c> (static) and <c>[FactoryEventHandler&lt;T&gt;]</c>
    /// still fails with CS0111 only — the per-leg holder prefixes must not add CS0101 on top.
    /// </summary>
    /// <remarks>
    /// This shape is broken at HEAD and deliberately not fixed (Deferred Work item 15): both
    /// renderers re-open the same partial and each emits <c>FactoryServiceRegistrar</c>, which is
    /// CS0111 duplicate-member. TRIM-008 asserted in three places that distinct holder prefixes
    /// keep it at CS0111 rather than compounding it with CS0101 duplicate-type, and tested it
    /// nowhere. This pins that claim so a future prefix change cannot quietly worsen an already
    /// broken shape.
    /// </remarks>
    [Fact]
    public void BothAttributes_EmitsDuplicateMemberOnly_NotDuplicateType()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record MyEvent(int Id) : FactoryEventBase;

    [Factory]
    [FactoryEventHandler<MyEvent>]
    public static partial class MyBoth
    {
        [Execute]
        private static Task<string> _DoWork(string input) => Task.FromResult(input);

        internal static Task Handle(MyEvent evt) => Task.CompletedTask;
    }
}
";
        var (_, outputCompilation, _) = DiagnosticTestHelper.RunGenerator(source);

        var errorIds = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.Id)
            .Distinct()
            .ToList();

        Assert.Contains("CS0111", errorIds);
        Assert.DoesNotContain("CS0101", errorIds);
    }

    #endregion
}
