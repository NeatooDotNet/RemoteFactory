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
        Assert.Contains("[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(global::TestNamespace.MyEntityFactory))]", generatedSource);
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
        Assert.Contains("internal static class NeatooFactoryRegistrar_MyCommands", generatedSource);
        Assert.Contains("internal static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)", generatedSource);
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
        Assert.Contains("internal static class NeatooEventHandlerRegistrar_MyHandlers", generatedSource);
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
        var (_, outputCompilation, _) = DiagnosticTestHelper.RunGenerator(RelayHandlerSource);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.Empty(errors);
    }

    #endregion
}
