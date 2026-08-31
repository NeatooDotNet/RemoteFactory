using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;
using System.Globalization;

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
        // The usings match StaticFactorySource: the generated factory references
        // CancellationToken and IServiceProvider, so without them the compile assertion
        // below fails on the FIXTURE rather than on the emission.
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
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
        var (_, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(source);

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

        // The class leg gained a new top-level type AND a new method per guarded async local,
        // and FactoryRenderer swallows render exceptions into a /* Error: */ comment while
        // NormalizeWhitespace parses with error recovery — malformed emission yields mangled
        // output, not a throw. The static and relay legs already assert this; the class leg
        // did not until TRIM-009's code review (C2).
        Assert.Empty(outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error));
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

        // Wrapper: NOT async, carries the guard, forwards to the core through the
        // entry-call helper (PHASE-003). The trimming-relevant property is unchanged:
        // the guard sits in a non-async method, never inside a state machine.
        Assert.Matches(
            @"public Task<MyEntity> LocalFetchIt\(string name, CancellationToken cancellationToken = default\)\s*\{\s*if \(!NeatooRuntime\.IsServerRuntime\)",
            generatedSource);
        Assert.Contains("return global::Neatoo.RemoteFactory.Internal.FactoryEntryCall.RunAsync(ServiceProvider, () => LocalFetchItCore(name, cancellationToken));", generatedSource);

        // Core: async, private, and carries NO guard — the guard already ran in the wrapper.
        Assert.Contains("private async Task<MyEntity> LocalFetchItCore(", generatedSource);
        Assert.Matches(
            @"private async Task<MyEntity> LocalFetchItCore\([^)]*\)\s*\{\s*(?!\s*if \(!NeatooRuntime\.IsServerRuntime\))",
            generatedSource);
    }

    // NO UNIT TEST FOR THE ASYNC GUARDED Can* SITE, AND THIS IS WHY.
    //
    // `RenderCanLocalMethod` is the fifth wrapper site and the only one with no emission
    // assertion here — raised at TRIM-009's test review. An attempt to add one was removed
    // rather than kept, because it did not test what it claimed: an `[AuthorizeFactory<T>]`
    // whose method returns `Task<bool>` produces a Can* that is async but NOT server-only,
    // so no guard is emitted and no split occurs. The assertion passed or failed for reasons
    // unrelated to the wrapper.
    //
    // The shape that DOES produce a guarded async Can* is `[AspAuthorize]` policy auth, whose
    // generated check is async by nature. That needs ASP.NET Core references, which
    // `DiagnosticTestHelper.BuildReferences()` does not carry.
    //
    // The site is not unexercised: `Design.Domain.Aggregates.SecureOrder` and
    // `RemoteFactory.AspNetCore.TestLibrary` both emit `LocalCan*Core` wrappers, both compile,
    // and both are covered by passing suites (Design 86+86). What is missing is a dedicated
    // emission assertion, which needs an ASP-auth fixture in this harness.

    /// <summary>
    /// A SYNCHRONOUS guarded <c>Local*</c> method splits into a non-async guarded wrapper
    /// and a NON-async private core.
    /// </summary>
    /// <remarks>
    /// Amended by PHASE-003 (was <c>ClassFactory_GuardedSyncLocalMethod_IsNotSplit</c>):
    /// every <c>Local*</c> method now splits so the wrapper can route through the
    /// entry-call helper. The trimming intent this test pins is unchanged from TRIM-009 —
    /// the guard must sit ahead of any protected region — and the sync shape still
    /// satisfies it: the wrapper is not async, and the core keeps the body's original
    /// synchronous form (no <c>async</c> keyword, no state machine hosting the guard).
    /// </remarks>
    [Fact]
    public void ClassFactory_GuardedSyncLocalMethod_SplitsIntoSyncWrapperAndSyncCore()
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

        // Wrapper: NOT async, carries the guard, forwards through the entry-call helper.
        Assert.Matches(
            @"public Task<MyEntity> LocalCreate\(string name, CancellationToken cancellationToken = default\)\s*\{\s*if \(!NeatooRuntime\.IsServerRuntime\)",
            generatedSource);
        Assert.Contains("return global::Neatoo.RemoteFactory.Internal.FactoryEntryCall.RunAsync(ServiceProvider, () => LocalCreateCore(name, cancellationToken));", generatedSource);

        // Core: private, NOT async (the sync body keeps its shape), and carries no guard.
        Assert.Matches(
            @"private Task<MyEntity> LocalCreateCore\([^)]*\)\s*\{\s*(?!\s*if \(!NeatooRuntime\.IsServerRuntime\))",
            generatedSource);
        Assert.DoesNotContain("private async Task<MyEntity> LocalCreateCore(", generatedSource);
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
    /// Interface factory generated source points the assembly-level NeatooFactoryRegistrar
    /// attribute at a single-method registrar holder — never at the factory class, whose
    /// methods (including the private Local*Core bodies) the attribute's
    /// [DynamicallyAccessedMembers] would otherwise root into trimmed clients.
    /// </summary>
    /// <remarks>
    /// Amended by PHASE-003 (code review V1): previously the attribute named
    /// MyServiceFactory itself, which became the TRIM-009 measured-insufficient
    /// configuration once the Local* wrapper/core split landed on this leg.
    /// </remarks>
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
        Assert.Contains("[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(global::TestNamespace.NeatooInterfaceFactoryRegistrar_MyService))]", generatedSource);

        // The attribute must not name the factory type (the prefix convention keeps the
        // factory's qualified name from being a substring of the holder's).
        Assert.DoesNotContain("NeatooFactoryRegistrar(typeof(global::TestNamespace.MyServiceFactory)", generatedSource);

        // The holder exists and forwards to the factory's registrar.
        Assert.Matches(
            @"internal static class NeatooInterfaceFactoryRegistrar_MyService\s*\{\s*internal static void FactoryServiceRegistrar\(IServiceCollection services, NeatooFactory remoteLocal\)",
            generatedSource);
        Assert.Contains("global::TestNamespace.MyServiceFactory.FactoryServiceRegistrar(services, remoteLocal);", generatedSource);
    }

    /// <summary>
    /// The interface leg's <c>Local*</c> methods split into a NON-async guarded wrapper
    /// forwarding through the entry-call helper to a private, unguarded core.
    /// </summary>
    /// <remarks>
    /// Introduced by PHASE-003 — this leg previously emitted the guard inline on the
    /// method itself with a conditional <c>async</c> keyword, the shape TRIM-009
    /// measured as guard-inside-<c>MoveNext</c> on the class leg whenever the method
    /// went async. Body elimination on the interface leg is still UNVERIFIED (TRIM
    /// Deferred Work item 20: the single-method registrar holder half of the fix is
    /// absent here), so this emission pin is the only obtainable evidence that the
    /// guard sits in a non-async wrapper. An <c>async</c> keyword appearing on the
    /// wrapper must go red here.
    /// </remarks>
    [Fact]
    public void InterfaceFactory_GuardedLocalMethod_SplitsIntoSyncWrapperAndCore()
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

        // Wrapper: NOT async, carries the guard, forwards through the entry-call helper.
        Assert.Matches(
            @"public Task<string> LocalDoWork\([^)]*\)\s*\{\s*if \(!NeatooRuntime\.IsServerRuntime\)",
            generatedSource);
        Assert.DoesNotContain("public async Task<string> LocalDoWork(", generatedSource);
        Assert.Contains("return global::Neatoo.RemoteFactory.Internal.FactoryEntryCall.RunAsync(ServiceProvider, () => LocalDoWorkCore(", generatedSource);

        // Core: private and unguarded (this fixture's body forwards the target's task
        // without awaiting, so no async keyword; the guard already ran in the wrapper).
        Assert.Matches(
            @"private (async )?Task<string> LocalDoWorkCore\([^)]*\)\s*\{\s*(?!\s*if \(!NeatooRuntime\.IsServerRuntime\))",
            generatedSource);
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
            @"internal static class NeatooEventHandlerRegistrar_MyHandlers\s*\{\s*internal static void FactoryServiceRegistrar\(global::Microsoft\.Extensions\.DependencyInjection\.IServiceCollection services, global::Neatoo\.RemoteFactory\.NeatooFactory remoteLocal\)",
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

    #region Relay Handler — Dispatch Phase (PHASE-002)

    /// <summary>
    /// Two event types on one class, one explicitly phased and one defaulted.
    /// </summary>
    /// <remarks>
    /// The two attributes must name DIFFERENT events — same-event stacking is NF0504 and skips
    /// the duplicate's entry, which would silently remove whichever registration a test here
    /// was trying to assert. Distinct handler methods for the same reason: two methods matching
    /// one event is NF0502 and emits nothing at all.
    /// </remarks>
    private const string PhasedRelayHandlerSource = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record ProjectionEvent(int Id) : FactoryEventBase;
    public record AtomicEvent(int Id) : FactoryEventBase;
    public record StagedEvent(int Id) : FactoryEventBase;

    public interface IProjectionPort
    {
        Task Send(string message);
    }

    [FactoryEventHandler<ProjectionEvent>(DispatchPhase.AfterCommit)]
    [FactoryEventHandler<AtomicEvent>]
    [FactoryEventHandler<StagedEvent>(DispatchPhase.AfterFlush)]
    public static partial class MixedHandlers
    {
        internal static Task Project(ProjectionEvent evt) => Task.CompletedTask;

        internal static Task Apply(AtomicEvent evt) => Task.CompletedTask;

        internal static Task Stage(StagedEvent evt, [Service] IProjectionPort port, CancellationToken ct)
            => port.Send(""staged"");
    }
}
";

    private static string GeneratedSourceFor(string source, string hintFragment)
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains(hintFragment))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        return generatedSource;
    }

    /// <summary>
    /// A handler declaring no phase argument registers at <c>Immediate</c> — the pre-PHASE-002
    /// contract, now stated positively in the emitted call rather than implied by an absence.
    /// </summary>
    /// <remarks>
    /// The phase-taking overload is emitted for every handler, including defaulted ones, so this
    /// asserts the argument is present and correct rather than asserting the two-argument form.
    /// </remarks>
    [Fact]
    public void RelayHandler_UnphasedHandler_RegistersAtImmediate()
    {
        var generatedSource = GeneratedSourceFor(RelayHandlerSource, "MyHandlers");

        Assert.Contains(
            "global::Neatoo.RemoteFactory.FactoryEventHandlerRegistry.RegisterHandler<global::TestNamespace.MyEvent>(typeof(MyHandlers), global::Neatoo.RemoteFactory.DispatchPhase.Immediate, coalesce: false, async (sp, eventObj, options, ct) =>",
            generatedSource);
    }

    /// <summary>
    /// An explicitly phased handler registers at the phase the attribute declared.
    /// </summary>
    /// <remarks>
    /// Before PHASE-002 the generator read the attribute's type argument and never its
    /// constructor arguments, so this assertion is the one that would have caught the phase
    /// being silently dropped — every registration landed at <c>Immediate</c> no matter what
    /// the consumer wrote.
    /// </remarks>
    [Fact]
    public void RelayHandler_PhasedHandler_RegistersAtTheDeclaredPhase()
    {
        var generatedSource = GeneratedSourceFor(PhasedRelayHandlerSource, "MixedHandlers");

        Assert.Contains(
            "global::Neatoo.RemoteFactory.FactoryEventHandlerRegistry.RegisterHandler<global::TestNamespace.ProjectionEvent>(typeof(MixedHandlers), global::Neatoo.RemoteFactory.DispatchPhase.AfterCommit, coalesce: false, async (sp, eventObj, options, ct) =>",
            generatedSource);
    }

    /// <summary>
    /// One class declaring several event types at different phases registers each at its own
    /// phase — the phase is per-attribute, not per-class.
    /// </summary>
    [Fact]
    public void RelayHandler_SeveralEventTypes_EachRegistersAtItsOwnPhase()
    {
        var generatedSource = GeneratedSourceFor(PhasedRelayHandlerSource, "MixedHandlers");

        Assert.Contains(
            "RegisterHandler<global::TestNamespace.ProjectionEvent>(typeof(MixedHandlers), global::Neatoo.RemoteFactory.DispatchPhase.AfterCommit,",
            generatedSource);
        Assert.Contains(
            "RegisterHandler<global::TestNamespace.AtomicEvent>(typeof(MixedHandlers), global::Neatoo.RemoteFactory.DispatchPhase.Immediate,",
            generatedSource);

        // AfterFlush has no drain point of its own until PHASE-004, but the member-name lookup
        // is generic over the enum symbol and this is the only phase not otherwise emitted
        // anywhere. Its handler also carries [Service] + CancellationToken alongside a phase,
        // so the phase token and the parameter list are pinned interacting.
        Assert.Contains(
            "RegisterHandler<global::TestNamespace.StagedEvent>(typeof(MixedHandlers), global::Neatoo.RemoteFactory.DispatchPhase.AfterFlush, coalesce: false, async (sp, eventObj, options, ct) =>",
            generatedSource);
    }

    /// <summary>
    /// The registration stays inside the server-runtime guard, and the guard's body is the
    /// registration — asserted in order, not as two independent containments.
    /// </summary>
    /// <remarks>
    /// The relay leg had no <c>IsServerRuntime</c> assertion anywhere in the unit suite before
    /// this; the only control on it was the CI publish-trimmed gate's absence check, which is
    /// reasoned from the marker's reachability, runs only on push/PR, and is not in the Step 5
    /// logs. The guard is what keeps handler bodies and their server-only services out of a
    /// trimmed client, and this plan's renderer edit is inside the method that emits it.
    /// </remarks>
    [Fact]
    public void RelayHandler_PhasedRegistration_StaysInsideTheServerRuntimeGuard()
    {
        var generatedSource = GeneratedSourceFor(PhasedRelayHandlerSource, "MixedHandlers");

        Assert.Matches(
            @"if \(global::Neatoo\.RemoteFactory\.NeatooRuntime\.IsServerRuntime\)\s*\{\s*global::Neatoo\.RemoteFactory\.FactoryEventHandlerRegistry\.RegisterHandler<",
            generatedSource);
    }

    /// <summary>
    /// A consumer whose own namespace shadows every name the generated body would otherwise
    /// resolve through a <c>using</c> — including the segment their event type hangs off.
    /// </summary>
    /// <remarks>
    /// Every decoy here is reachable by C#'s innermost-first namespace lookup from inside
    /// <c>namespace TestNamespace</c>, which is where the registration body is emitted:
    /// <c>TestNamespace.TestNamespace</c> captures a bare <c>TestNamespace.MyEvent</c>, and
    /// <c>TestNamespace.Neatoo</c> captures a bare <c>Neatoo.RemoteFactory.*</c>. The decoys
    /// are deliberately the WRONG SHAPE — <c>MyEvent</c> does not derive
    /// <c>FactoryEventBase</c>, the runtime decoy has no <c>IsServerRuntime</c>, the registry
    /// decoy has no <c>RegisterHandler</c> — so binding to one is a compile error rather than
    /// a silently wrong registration. That is what makes the compile check discriminating.
    /// </remarks>
    private const string ShadowingRelayHandlerSource = @"
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

    public static class NeatooRuntime { }
    public static class FactoryEventHandlerRegistry { }
    public sealed class IServiceCollection { }
    public sealed class NeatooFactory { }
}

namespace TestNamespace.TestNamespace
{
    public sealed class MyEvent { }
    public interface IMyPort { }
}

namespace TestNamespace.Neatoo
{
    public sealed class Decoy { }
}
";

    /// <summary>
    /// The generated registration binds to the consumer's real types even when the consumer's
    /// own namespace shadows every unqualified route to them.
    /// </summary>
    /// <remarks>
    /// This is the behavioral half of the qualification work, and the reason it is a compile
    /// check rather than a string assertion: a <c>Contains</c> on the qualified form is
    /// satisfied by the bare token as a substring, the false green PHASE-002 documented and
    /// the reason its phase-argument pin is written negatively.
    /// <para>
    /// Before PHASE-008 the event type, the <c>[Service]</c> parameter type, and the framework
    /// tokens were all emitted bare, so this fixture produced a compilation with errors while
    /// every string assertion in this class stayed green.
    /// </para>
    /// </remarks>
    [Fact]
    public void RelayHandler_ConsumerNamespaceShadowsEveryUnqualifiedRoute_OutputStillCompiles()
    {
        var (_, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(ShadowingRelayHandlerSource);

        // Same zero-trees guard as RelayHandler_GeneratedOutputCompilesWithoutErrors: a
        // transform early-out would leave a clean compilation and satisfy Assert.Empty
        // while emitting nothing at all.
        Assert.NotNull(runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("MyHandlers")));

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(errors);
    }

    /// <summary>
    /// Every type-bearing token the registration emits is <c>global::</c>-qualified — asserted
    /// negatively, because the positive form contains the bare form as a substring.
    /// </summary>
    /// <remarks>
    /// Two deliberate exceptions, both recorded in the renderer rather than qualified
    /// reflexively. <c>typeof({className})</c> stays bare because the body is emitted inside
    /// the user's own namespace AND inside their own partial class, where the only name that
    /// could shadow the enclosing class is a member of the same name — CS0542. And the
    /// assembly attribute's own NAME stays bare because assembly-level attributes bind at
    /// compilation-unit scope, outside any namespace, where nothing consumer-declared is in
    /// scope to shadow it. This test's scope is therefore the registration BODY, which is the
    /// only part emitted inside the consumer's namespace. (Both exceptions named by the
    /// PHASE-008 gate; the second was previously unstated.)
    /// </remarks>
    [Fact]
    public void RelayHandler_EveryEmittedTypeToken_IsGlobalQualified()
    {
        var generatedSource = GeneratedSourceFor(RelayHandlerSource, "MyHandlers");

        // Consumer-derived tokens: the generic argument and the cast that follows it.
        Assert.DoesNotContain("RegisterHandler<TestNamespace.", generatedSource);
        Assert.DoesNotContain("(TestNamespace.MyEvent)eventObj", generatedSource);

        // The [Service] parameter's type, resolved from the consumer's namespace too.
        Assert.DoesNotContain("GetRequiredService<TestNamespace.", generatedSource);

        // Framework tokens the generated body would otherwise take from the file's using.
        Assert.DoesNotContain("if (NeatooRuntime.", generatedSource);
        Assert.DoesNotContain(" FactoryEventHandlerRegistry.", generatedSource);
        Assert.DoesNotContain("FactoryServiceRegistrar(IServiceCollection", generatedSource);

        // And the positives, so this cannot pass by emitting nothing at all.
        Assert.Contains("RegisterHandler<global::TestNamespace.MyEvent>", generatedSource);
        Assert.Contains("GetRequiredService<global::TestNamespace.IMyPort>", generatedSource);
        Assert.Contains("global::Neatoo.RemoteFactory.NeatooRuntime.IsServerRuntime", generatedSource);
    }

    /// <summary>
    /// One handler class whose two <c>[FactoryEventHandler&lt;T&gt;]</c> attributes are split
    /// across two partial declarations.
    /// </summary>
    /// <remarks>
    /// <c>ForAttributeWithMetadataName</c> yields one value per attributed SYNTAX NODE, while
    /// the transform reads <c>symbol.GetAttributes()</c> and derives its hint name from the
    /// SYMBOL — so this shape puts two nodes, two identical models, and one hint name into the
    /// same pipeline. PHASE-002 inferred a collision here and never measured it; PHASE-008
    /// does.
    /// </remarks>
    private const string SplitPartialRelayHandlerSource = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record FirstEvent(int Id) : FactoryEventBase;
    public record SecondEvent(int Id) : FactoryEventBase;

    [FactoryEventHandler<FirstEvent>]
    public static partial class SplitHandlers
    {
        internal static Task HandleFirst(FirstEvent evt)
        {
            return Task.CompletedTask;
        }
    }

    [FactoryEventHandler<SecondEvent>]
    public static partial class SplitHandlers
    {
        internal static Task HandleSecond(SecondEvent evt)
        {
            return Task.CompletedTask;
        }
    }
}
";

    /// <summary>
    /// A partial handler class where only the LATER-declared partial carries the attribute.
    /// </summary>
    /// <remarks>
    /// The shape that separates "one model per symbol" from "one model per attributed
    /// declaration": <c>ForAttributeWithMetadataName</c> only ever yields the attributed
    /// node, so a canonical choice made across <i>all</i> declarations can select one the
    /// transform is never handed.
    /// </remarks>
    private const string LateAttributedPartialRelayHandlerSource = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record LateEvent(int Id) : FactoryEventBase;

    public static partial class LateHandlers
    {
        internal static Task Unrelated()
        {
            return Task.CompletedTask;
        }
    }

    [FactoryEventHandler<LateEvent>]
    public static partial class LateHandlers
    {
        internal static Task HandleLate(LateEvent evt)
        {
            return Task.CompletedTask;
        }
    }
}
";

    /// <summary>
    /// A handler class whose attribute sits on a partial declared after an unattributed one
    /// still registers.
    /// </summary>
    /// <remarks>
    /// Guards the failure mode the split-partial fix could introduce, and which would be
    /// strictly worse than the CS8785 it replaced: CS8785 is loud, whereas selecting an
    /// unattributed declaration as canonical emits nothing and reports nothing, so the
    /// handler simply never runs at runtime. The canonical choice must therefore range over
    /// the ATTRIBUTED declarations only — the ones the pipeline actually yields.
    /// </remarks>
    [Fact]
    public void RelayHandler_AttributeOnALaterPartialOnly_StillRegistersTheHandler()
    {
        var (_, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(LateAttributedPartialRelayHandlerSource);

        var handlerFiles = runResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("LateHandlers"))
            .ToList();

        Assert.Single(handlerFiles);
        Assert.Empty(outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var generatedSource = handlerFiles[0].GetText().ToString();
        Assert.Equal(1, CountOf(generatedSource, "RegisterHandler<global::TestNamespace.LateEvent>"));
    }

    /// <summary>
    /// Attributes split across partial declarations generate valid output exactly once.
    /// </summary>
    /// <remarks>
    /// The measurement PHASE-002's Discovery Log recorded as inferred. Whatever the generator
    /// does with this shape is consumer-visible — a duplicate hint name is a hard generator
    /// failure (CS8785 / duplicate-source), and a doubled registration would double-dispatch
    /// every handler on the class. Both are load-bearing enough to pin rather than reason about.
    /// <para>
    /// Asserts the outcome three ways because the failure modes are distinct: no generator
    /// crash, exactly one emitted file for the class, and each event registered exactly once.
    /// </para>
    /// </remarks>
    [Fact]
    public void RelayHandler_AttributesSplitAcrossPartials_EmitOneFileWithEachRegistrationOnce()
    {
        var (diagnostics, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(SplitPartialRelayHandlerSource);

        // A generator that throws surfaces as CS8785 rather than as an exception here.
        Assert.DoesNotContain(diagnostics, d => d.Id == "CS8785");
        Assert.Empty(outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        var handlerFiles = runResult.GeneratedTrees
            .Where(t => t.FilePath.Contains("SplitHandlers"))
            .ToList();

        Assert.Single(handlerFiles);

        var generatedSource = handlerFiles[0].GetText().ToString();

        Assert.Equal(1, CountOf(generatedSource, "RegisterHandler<global::TestNamespace.FirstEvent>"));
        Assert.Equal(1, CountOf(generatedSource, "RegisterHandler<global::TestNamespace.SecondEvent>"));
    }

    private static int CountOf(string haystack, string needle)
        => haystack.Split([needle], StringSplitOptions.None).Length - 1;

    // =====================================================================================
    // Namespace-shadowing guards for the other four renderers (PHASE-011, from the
    // PHASE-008 gate's tech-debt item T2).
    //
    // The relay leg got its guard when PHASE-008 fixed a real defect there.
    //
    // These four were EXPECTED to be regression guards, on the reasoning that the global::
    // strip which caused the relay bug lives only in FactoryGenerator.RelayHandler.cs. That
    // reasoning was measured and KILLED (PHASE-011 RP-0): all four reddened, because these
    // legs never asked for qualification in the first place -- FactoryGenerator.Types.cs took
    // return types via ITypeSymbol.ToString(), a MINIMALLY qualified name. Two of the four
    // caught live defects and two are genuine regression guards, and each test below says
    // which it is. Do not restore the old blanket claim: the sentence "these legs keep their
    // qualification" is true only BECAUSE this plan changed them to, which makes it circular
    // as a justification for the tests that forced the change.
    //
    // An honest label matters more here than an impressive one -- a guard that never could
    // have failed is the shape this arc has caught twelve times, and a guard mislabeled as
    // one is how the record stops being trustworthy.
    //
    // Every fixture shadows two ways, both reachable by C# name lookup from inside
    // `namespace TestNamespace` where the generated members are emitted:
    //   - TestNamespace.TestNamespace  captures a bare `TestNamespace.Foo`
    //   - TestNamespace.Neatoo         captures a bare `Neatoo.RemoteFactory.Foo`
    // Decoys are deliberately the WRONG SHAPE, so binding to one is a compile error rather
    // than a silently wrong emission — which is what makes the compile check discriminating.
    //
    // A THIRD decoy (`TestNamespace.System`, capturing bare BCL tokens) was written, run, and
    // deliberately REMOVED. It reddened all four legs on CS0246/CS0234 — the renderers emit
    // Task, CancellationToken, IServiceProvider, Exception and System.Diagnostics unqualified,
    // 128 occurrences across the four files. That is a real hazard but a DIFFERENT one, and
    // materially less severe: a shadowed BCL token fails loudly in the consumer's own build,
    // whereas a shadowed consumer type binds to the WRONG TYPE, which is what this fixture
    // catches and what PHASE-011 fixed. Queued as its own row rather than swept in at arc-end
    // behind a 128-token diff. See the plan's Amendments A2 and A3 (A1 is the return-type
    // qualification, a different finding).
    // =====================================================================================

    private const string ShadowingClassFactorySource = @"
using Neatoo.RemoteFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public sealed class Payload
    {
        public string Value { get; set; } = string.Empty;
    }

    [Factory]
    public partial class ShadowedTarget
    {
        [Create]
        public ShadowedTarget() { }

        [Fetch]
        public void Fetch(Payload payload) { }
    }
}

namespace TestNamespace.TestNamespace
{
    public sealed class Payload { }
    public sealed class ShadowedTarget { }
}

namespace TestNamespace.Neatoo
{
    public sealed class Decoy { }
}
";

    private const string ShadowingInterfaceFactorySource = @"
using Neatoo.RemoteFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public sealed class Payload
    {
        public string Value { get; set; } = string.Empty;
    }

    [Factory]
    public interface IShadowedService
    {
        Task<Payload> Load(Payload request);
    }

    public class ShadowedService : IShadowedService
    {
        public Task<Payload> Load(Payload request) => Task.FromResult(request);
    }
}

namespace TestNamespace.TestNamespace
{
    public sealed class Payload { }
    public interface IShadowedService { }
}

namespace TestNamespace.Neatoo
{
    public sealed class Decoy { }
}
";

    private const string ShadowingStaticFactorySource = @"
using Neatoo.RemoteFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public sealed class Payload
    {
        public string Value { get; set; } = string.Empty;
    }

    [Factory]
    public static partial class ShadowedCommands
    {
        [Execute]
        private static Task<Payload> _DoWork(Payload input)
        {
            return Task.FromResult(input);
        }

        // Synchronous on purpose, and it did NOT achieve what it was added for -- kept with
        // that result recorded rather than removed. The Task<T> overload above exercises only
        // the GENERIC branch of MethodInfo's return-type capture, which overwrites the
        // non-Task assignment before it, so the gate (S1) asked for a sync operation to make
        // that earlier line load-bearing. Measured as PHASE-011 RP-2: sabotaging the non-Task
        // line ALONE leaves all 762 green even with this method present. The static leg wraps
        // every delegate in Task<> at emission (StaticFactoryRenderer:99), so an [Execute]
        // returning T and one returning Task<T> converge before the shadowable position.
        // The non-Task line is therefore recorded as UNMEASURED in the red-proof log, not
        // claimed as covered.
        [Execute]
        private static Payload _DoWorkSync(Payload input)
        {
            return input;
        }
    }
}

namespace TestNamespace.TestNamespace
{
    public sealed class Payload { }
    public sealed class ShadowedCommands { }
}

namespace TestNamespace.Neatoo
{
    public sealed class Decoy { }
}
";

    private const string ShadowingEventPreservationSource = @"
using Neatoo.RemoteFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record PreservedEvent(int Id) : FactoryEventBase;

    // The handler is what makes the event DISCOVERED. Without a subscriber the preservation
    // walker never reaches PreservedEvent, the registrar is emitted without it, and the guard
    // proves only that an empty registrar compiles. (PHASE-011 RP-5.)
    [FactoryEventHandler<PreservedEvent>]
    public static partial class PreservationHandlers
    {
        internal static Task Handle(PreservedEvent evt)
        {
            return Task.CompletedTask;
        }
    }

    [Factory]
    public partial class PreservationTarget
    {
        [Create]
        public PreservationTarget() { }
    }
}

namespace TestAssembly.TestNamespace
{
    public sealed class PreservedEvent { }
}

namespace TestAssembly.Neatoo
{
    public sealed class Decoy { }
}
";

    /// <summary>
    /// Class-factory output compiles when the consumer's namespace shadows every unqualified
    /// route out of it.
    /// </summary>
    /// <remarks>
    /// <b>Regression guard</b> — it stays green under RP-1's sabotage, so nothing in this plan
    /// measured it catching anything. Precisely stated, because the first version of this
    /// remark said "passed unmodified on first run" and that was false twice over: RP-0 records
    /// all four legs red on first run (this one went green only once the fixture gained
    /// <c>using System;</c>), and RP-1 is a sabotage run rather than a first run.
    /// <para>
    /// Kept because the property is an easy one to lose — the relay leg had it too, until it
    /// did not.
    /// </para>
    /// </remarks>
    [Fact]
    public void ClassFactory_ConsumerNamespaceShadowsEveryUnqualifiedRoute_OutputStillCompiles()
        => AssertShadowedOutputCompiles(ShadowingClassFactorySource, "ShadowedTarget");

    /// <summary>
    /// Interface-factory output compiles under the same hostile namespace layout.
    /// </summary>
    /// <remarks>
    /// <b>This guard caught a live defect</b> — not a regression guard, whatever the row that
    /// queued it assumed. The proxy's method signatures carry consumer types, and those types
    /// arrived minimally qualified, so the emitted implementation failed to satisfy its own
    /// interface: <c>CS0738 — 'ShadowedServiceFactory.Load(Payload)' cannot implement
    /// 'IShadowedService.Load(Payload)' because it does not have the matching return type</c>.
    /// RP-1 measures it as <i>sole</i> coverage: no pre-existing test reddens under the same
    /// sabotage.
    /// </remarks>
    [Fact]
    public void InterfaceFactory_ConsumerNamespaceShadowsEveryUnqualifiedRoute_OutputStillCompiles()
        => AssertShadowedOutputCompiles(ShadowingInterfaceFactorySource, "ShadowedService");

    /// <summary>
    /// Static-factory output compiles under the same hostile namespace layout.
    /// </summary>
    /// <remarks>
    /// <b>This guard caught a live defect</b> — not a regression guard. The delegate's return
    /// type arrived minimally qualified and bound to the decoy:
    /// <c>CS0029 — cannot implicitly convert Task&lt;TestNamespace.Payload&gt; to
    /// Task&lt;TestNamespace.TestNamespace.Payload&gt;</c>. That is a WRONG-TYPE binding, which
    /// is the severe half of what these fixtures can find — it is the shape that could compile
    /// and be wrong rather than fail loudly. RP-1 measures it as sole coverage.
    /// </remarks>
    [Fact]
    public void StaticFactory_ConsumerNamespaceShadowsEveryUnqualifiedRoute_OutputStillCompiles()
        => AssertShadowedOutputCompiles(ShadowingStaticFactorySource, "ShadowedCommands");

    /// <summary>
    /// Event-preservation output compiles when the namespace it is emitted into is shadowed.
    /// </summary>
    /// <remarks>
    /// <b>A smoke test, and deliberately labeled as the weakest of the four.</b> It proves the
    /// preservation registrar is emitted and compiles when the namespace it is emitted into is
    /// shadowed. It does <b>not</b> pin consumer-type qualification, and it cannot — see below.
    /// <para>
    /// <b>Its decoys sit under <c>TestAssembly</c>, not <c>TestNamespace</c>.</b> Unlike the
    /// other three legs, this renderer does not emit into the consumer's namespace: it emits
    /// into <c>namespace {SanitizeNamespace(assemblyName)}</c>
    /// (<c>EventPreservationRenderer:62,77</c>), which under the harness is
    /// <c>TestAssembly</c> (<c>DiagnosticTestHelper.cs:79</c>). A decoy under
    /// <c>TestNamespace</c> is unreachable from the emitted body and constrains nothing — the
    /// mistake the first version of this test made, along with an existence assertion pointed
    /// at <c>PreservationTarget</c> (the CLASS-FACTORY output) rather than at
    /// <c>NeatooEventPreservation</c>. Caught by the PHASE-011 gate as M1; it is the same
    /// unreachable-decoy error the PHASE-008 gate had already flagged once.
    /// </para>
    /// <para>
    /// <b>Why the corrected version still cannot catch a mis-binding, measured across four
    /// sabotages (PHASE-011 RP-3 … RP-6).</b> Stripping <c>global::</c> from the registrar's
    /// emitted type arguments reddens six <c>EventPreservationDiscoveryTests</c> and leaves
    /// this test green — even with the fixture's event given a handler so it is genuinely
    /// discovered and emitted. The reason is that <c>DtoConstructorRegistry.Register&lt;T&gt;</c>
    /// and <c>PreserveType&lt;T&gt;</c> declare <b>no type constraint</b>, so a type argument
    /// that binds to the wrong type compiles perfectly well. A compile check is structurally
    /// blind here.
    /// </para>
    /// <para>
    /// The leg is therefore covered — by <c>EventPreservationDiscoveryTests</c>, which assert
    /// on the emitted text and do redden — and this test sits on top of them as a compile
    /// smoke test. Kept for that, and for the emission-position property no other test states.
    /// </para>
    /// </remarks>
    [Fact]
    public void EventPreservation_EmittedNamespaceIsShadowed_OutputStillCompiles()
        => AssertShadowedOutputCompiles(ShadowingEventPreservationSource, "NeatooEventPreservation");

    /// <summary>
    /// Shared body for the four shadowing guards: the generator ran, emitted something for
    /// this consumer type, and what it emitted compiles.
    /// </summary>
    /// <remarks>
    /// The emitted-file assertion is not decoration. Without it the test passes on ZERO
    /// generated trees — a transform early-out leaves the input compilation clean and
    /// <c>Assert.Empty(errors)</c> is trivially satisfied, which is the vacuity mode
    /// <c>RelayHandler_GeneratedOutputCompilesWithoutErrors</c> documents.
    /// </remarks>
    private static void AssertShadowedOutputCompiles(string source, string hintFragment)
    {
        var (_, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(source);

        Assert.NotNull(runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains(hintFragment)));

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(errors);
    }

    /// <summary>
    /// The emitted phase argument is <c>global::</c>-qualified.
    /// </summary>
    /// <remarks>
    /// The positive assertions above would pass just as happily on a bare
    /// <c>DispatchPhase.AfterCommit</c> bound by the generated file's <c>using</c>, so they
    /// cannot pin this. The unqualified form is the latent bug documented at
    /// <c>RelayHandlerRenderer.cs:38-40</c> — it shipped for four releases on the registrar
    /// attribute, where a consumer namespace shadowing the first segment of ours bound the
    /// argument to the wrong type. This plan emits a NEW type-bearing token into the same file,
    /// so it needs the same negative pin the registrar attribute got.
    /// </remarks>
    [Fact]
    public void RelayHandler_PhaseArgument_IsGlobalQualified()
    {
        var generatedSource = GeneratedSourceFor(PhasedRelayHandlerSource, "MixedHandlers");

        // The argument position specifically: qualified emission always reads
        // ", global::Neatoo.RemoteFactory.DispatchPhase.", never ", DispatchPhase.".
        Assert.DoesNotContain(", DispatchPhase.", generatedSource);
        Assert.Contains(", global::Neatoo.RemoteFactory.DispatchPhase.", generatedSource);
    }

    /// <summary>
    /// A value cast onto the enum that matches no member renders as a cast rather than being
    /// coerced to a phase the consumer did not ask for.
    /// </summary>
    /// <remarks>
    /// Pins a decision, not an aspiration: such a handler registers and then never drains,
    /// because the scheduler sweeps only defined phases. Diagnosing it was judged out of
    /// proportion for this plan (todo Discovery Log, 2026-08-15). Silently coercing it to
    /// <c>Immediate</c> would be worse — it would run handlers at a phase nobody declared.
    /// </remarks>
    /// <param name="literal">The cast value as written in source.</param>
    /// <param name="expected">The value as it must appear in the emitted argument.</param>
    [Theory]
    [InlineData("99", "99")]
    // Negative values reach the numeric fallback too, and interpolation would format them with
    // the BUILD MACHINE's culture. On a culture whose negative sign is not ASCII '-' — sv-SE
    // resolves to U+2212 under ICU — an unqualified format emits CS1056 into the consumer's
    // build. This case exists so the invariant formatting has something holding it down.
    [InlineData("-1", "-1")]
    public void RelayHandler_UndefinedPhaseValue_RendersAsACast(string literal, string expected)
    {
        var source = PhasedRelayHandlerSource.Replace(
            "[FactoryEventHandler<ProjectionEvent>(DispatchPhase.AfterCommit)]",
            $"[FactoryEventHandler<ProjectionEvent>((DispatchPhase)({literal}))]");

        Assert.NotEqual(PhasedRelayHandlerSource, source);

        var generatedSource = GeneratedSourceFor(source, "MixedHandlers");

        Assert.Contains(
            $"typeof(MixedHandlers), (global::Neatoo.RemoteFactory.DispatchPhase){expected}, coalesce: false, async (sp, eventObj, options, ct) =>",
            generatedSource);
    }

    /// <summary>
    /// Declaring the same event type twice on one class reports NF0504 as a Warning.
    /// </summary>
    /// <remarks>
    /// Warning, not Error, deliberately: the severity split in this generator tracks what gets
    /// emitted. NF0501/NF0502 add no entry and the class emits no file, so those declarations
    /// are dead. A duplicate still produces a working registration and only the second
    /// declaration is inert — NF0503's shape, whose Warning severity was chosen to keep the
    /// build green. Pinning the severity keeps that reasoning from being reversed silently.
    /// </remarks>
    [Fact]
    public void RelayHandler_DuplicateEventType_ReportsNF0504AsWarning()
    {
        var source = RelayHandlerSource.Replace(
            "[FactoryEventHandler<MyEvent>]",
            "[FactoryEventHandler<MyEvent>]\n    [FactoryEventHandler<MyEvent>(DispatchPhase.AfterCommit)]");

        Assert.NotEqual(RelayHandlerSource, source);

        var (diagnostics, _, _) = DiagnosticTestHelper.RunGenerator(source);

        var duplicate = Assert.Single(diagnostics.Where(d => d.Id == "NF0504"));
        Assert.Equal(DiagnosticSeverity.Warning, duplicate.Severity);

        // The message names the phase that survives, so the consumer knows which declaration
        // won rather than having to reason about registry dedupe order. NOTE: the survivor here
        // is Immediate, which is also what a hardcoded message would say —
        // RelayHandler_DuplicateEventType_PhasedFirst_... is the test that discriminates.
        Assert.Contains("DispatchPhase.Immediate", duplicate.GetMessage(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// With the PHASED declaration first, the message names <c>AfterCommit</c> and the emitted
    /// registration is <c>AfterCommit</c> — source order wins, and the message reports it.
    /// </summary>
    /// <remarks>
    /// This is the discriminating half of the pair, and the reason it exists is worth keeping.
    /// Its sibling stacks unphased-first, so the surviving phase there is <c>Immediate</c> —
    /// which is also the hardcoded default constant, the value the malformed-argument fallback
    /// returns, and what a <c>messageFormat</c> with the phase placeholder deleted would print.
    /// That test stays green against three separate wrong implementations. Both gates on this
    /// plan flagged it independently; this is the version that can go red, and it pins source
    /// order at the same time, which nothing else did.
    /// </remarks>
    [Fact]
    public void RelayHandler_DuplicateEventType_PhasedFirst_KeepsThatPhaseAndNamesItInTheMessage()
    {
        var source = RelayHandlerSource.Replace(
            "[FactoryEventHandler<MyEvent>]",
            "[FactoryEventHandler<MyEvent>(DispatchPhase.AfterCommit)]\n    [FactoryEventHandler<MyEvent>]");

        Assert.NotEqual(RelayHandlerSource, source);

        var (diagnostics, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var duplicate = Assert.Single(diagnostics.Where(d => d.Id == "NF0504"));
        var message = duplicate.GetMessage(CultureInfo.InvariantCulture);
        Assert.Contains("DispatchPhase.AfterCommit", message);
        Assert.DoesNotContain("DispatchPhase.Immediate", message);

        var generatedSource = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("MyHandlers"))
            .GetText()
            .ToString();

        Assert.Contains("global::Neatoo.RemoteFactory.DispatchPhase.AfterCommit,", generatedSource);
        Assert.DoesNotContain("DispatchPhase.Immediate", generatedSource);
    }

    /// <summary>
    /// NF0504 is located at the handler class's identifier.
    /// </summary>
    /// <remarks>
    /// Asserted through the source span rather than a line/column pair, so an edit to the
    /// shared fixture cannot silently turn this into a false red — or, worse, leave it green
    /// while pointing somewhere else. The class location matches NF0501/NF0502's convention;
    /// pointing at the redundant attribute itself would serve a consumer with several stacked
    /// attributes better, and is recorded as a callout rather than done here.
    /// </remarks>
    [Fact]
    public void RelayHandler_DuplicateEventType_DiagnosticIsLocatedAtTheClass()
    {
        var source = RelayHandlerSource.Replace(
            "[FactoryEventHandler<MyEvent>]",
            "[FactoryEventHandler<MyEvent>]\n    [FactoryEventHandler<MyEvent>(DispatchPhase.AfterCommit)]");

        var (diagnostics, _, _) = DiagnosticTestHelper.RunGenerator(source);

        var duplicate = Assert.Single(diagnostics.Where(d => d.Id == "NF0504"));
        var span = duplicate.Location.SourceSpan;

        Assert.Equal("MyHandlers", source.Substring(span.Start, span.Length));
    }

    /// <summary>
    /// The duplicate's entry is skipped, so one registration is emitted rather than two.
    /// </summary>
    /// <remarks>
    /// This is the half that keeps Warning honest. Left emitting both, a duplicate declaring
    /// two different phases would emit <c>Immediate</c> then <c>AfterCommit</c> and the
    /// registry's first-wins dedupe would silently pick one — reintroducing exactly the silent
    /// phase loss this todo exists to remove, under a diagnostic that says the duplicate is
    /// "ignored". Skipping the entry makes the emitted code match the message.
    /// </remarks>
    [Fact]
    public void RelayHandler_DuplicateEventType_EmitsOneRegistrationNotTwo()
    {
        var source = RelayHandlerSource.Replace(
            "[FactoryEventHandler<MyEvent>]",
            "[FactoryEventHandler<MyEvent>]\n    [FactoryEventHandler<MyEvent>(DispatchPhase.AfterCommit)]");

        var generatedSource = GeneratedSourceFor(source, "MyHandlers");

        var registrations = generatedSource.Split(["RegisterHandler<global::TestNamespace.MyEvent>"], StringSplitOptions.None).Length - 1;

        Assert.Equal(1, registrations);
        Assert.DoesNotContain("DispatchPhase.AfterCommit", generatedSource);
    }

    /// <summary>
    /// The attribute's <c>Coalesce</c> named argument reaches the emitted registration
    /// (PHASE-006). The default is pinned positively as <c>coalesce: false</c> by the
    /// phase-emission tests above; this is the <c>true</c> half.
    /// </summary>
    [Fact]
    public void RelayHandler_CoalesceTrue_EmitsTheFlagOnTheRegistration()
    {
        var source = PhasedRelayHandlerSource.Replace(
            "[FactoryEventHandler<ProjectionEvent>(DispatchPhase.AfterCommit)]",
            "[FactoryEventHandler<ProjectionEvent>(DispatchPhase.AfterCommit, Coalesce = true)]");

        Assert.NotEqual(PhasedRelayHandlerSource, source);

        var generatedSource = GeneratedSourceFor(source, "MixedHandlers");

        Assert.Contains(
            "RegisterHandler<global::TestNamespace.ProjectionEvent>(typeof(MixedHandlers), global::Neatoo.RemoteFactory.DispatchPhase.AfterCommit, coalesce: true, async (sp, eventObj, options, ct) =>",
            generatedSource);
    }

    /// <summary>
    /// NF0504's survivor sentence covers the whole registration: with a coalescing
    /// declaration first, the message names the surviving phase AND flag, and the emitted
    /// registration carries them (PHASE-006, plan review A-V1).
    /// </summary>
    [Fact]
    public void RelayHandler_DuplicateEventType_CoalescingSurvivor_NamesTheFlagInTheMessage()
    {
        var source = RelayHandlerSource.Replace(
            "[FactoryEventHandler<MyEvent>]",
            "[FactoryEventHandler<MyEvent>(DispatchPhase.AfterFlush, Coalesce = true)]\n    [FactoryEventHandler<MyEvent>]");

        Assert.NotEqual(RelayHandlerSource, source);

        var (diagnostics, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var duplicate = Assert.Single(diagnostics.Where(d => d.Id == "NF0504"));
        Assert.Contains("DispatchPhase.AfterFlush, Coalesce = true", duplicate.GetMessage(CultureInfo.InvariantCulture));

        var generatedSource = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("MyHandlers"))
            .GetText()
            .ToString();

        Assert.Contains("global::Neatoo.RemoteFactory.DispatchPhase.AfterFlush, coalesce: true,", generatedSource);
    }

    /// <summary>
    /// When the FIRST declaration of an event type fails to produce an entry, the duplicate is
    /// not reported as one — the original diagnostic repeats instead, exactly as it did before
    /// NF0504 existed.
    /// </summary>
    /// <remarks>
    /// The duplicate tracker is populated only on success, and this pins why. If it were
    /// populated on sight of the attribute, this shape would report NF0502 once plus an NF0504
    /// claiming the handler "is registered at Immediate" — a message that is simply false here,
    /// since two matching methods means nothing is registered at all. This also holds the
    /// pre-existing NF0502 emission count steady, which was not something NF0504 was allowed
    /// to change.
    /// </remarks>
    [Fact]
    public void RelayHandler_DuplicateAfterAFailedFirstDeclaration_RepeatsTheOriginalDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record MyEvent(int Id) : FactoryEventBase;

    [FactoryEventHandler<MyEvent>]
    [FactoryEventHandler<MyEvent>(DispatchPhase.AfterCommit)]
    public static partial class AmbiguousHandlers
    {
        internal static Task HandleOne(MyEvent evt) => Task.CompletedTask;

        internal static Task HandleTwo(MyEvent evt) => Task.CompletedTask;
    }
}
";
        var (diagnostics, _, _) = DiagnosticTestHelper.RunGenerator(source);

        Assert.Equal(2, diagnostics.Count(d => d.Id == "NF0502"));
        Assert.Empty(diagnostics.Where(d => d.Id == "NF0504"));
    }

    /// <summary>
    /// Phased emission compiles.
    /// </summary>
    /// <remarks>
    /// Same reasoning as <see cref="RelayHandler_GeneratedOutputCompilesWithoutErrors"/>: relay
    /// output bypasses <c>NormalizeWhitespace</c> and the renderer swallows throws into a
    /// comment, so string containment can pass on source that does not compile. A malformed
    /// phase argument is precisely the shape that would do that.
    /// </remarks>
    [Fact]
    public void RelayHandler_PhasedOutput_CompilesWithoutErrors()
    {
        var (_, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(PhasedRelayHandlerSource);

        Assert.NotNull(runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("MixedHandlers")));
        Assert.Empty(outputCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
    }

    #endregion
}
