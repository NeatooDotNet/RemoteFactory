using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator;

/// <summary>
/// A <c>[FactoryEventHandler&lt;T&gt;]</c> whose <c>[Service]</c> parameter is a factory
/// interface produced by the SAME generator run.
/// </summary>
/// <remarks>
/// <para>
/// <b>The regression (v1.8.0).</b> PHASE-011 made the handler pipeline's <c>[Service]</c>
/// parameter types <c>global::</c>-qualified so generated code binds correctly under a
/// shadowing consumer namespace. That transform asks the symbol for its fully-qualified
/// name. But a factory interface this run is about to generate does not exist in the input
/// compilation, so its symbol is <see cref="TypeKind.Error"/> and its display string is the
/// bare identifier — <c>IPlanFactory</c>, no namespace. Prefixing <c>global::</c> to that
/// produced <c>sp.GetRequiredService&lt;global::IPlanFactory&gt;()</c>, which is CS0400 in the
/// consumer's build.
/// </para>
/// <para>
/// Under v1.5.0 the bare name was emitted and the <c>using</c> directives the generator copies
/// into the generated file resolved it once the factory interface existed. That is also how
/// every other renderer's <c>[Service]</c> parameters have always been emitted — the
/// class-factory path captures parameter types from syntax text and never asks the symbol,
/// which is why <c>[Fetch]</c> methods taking the same interface kept compiling.
/// </para>
/// <para>
/// Reported against zTreatment while bumping 1.5.0 → 1.8.0. No fixture in this repo had a
/// handler taking a generated factory interface, so the hostile-namespace fixtures that
/// found three other qualification defects could not have found this one.
/// </para>
/// </remarks>
public class RelayHandlerGeneratedServiceTypeTests
{
    /// <summary>
    /// A <c>[Factory]</c> class and a handler that injects its generated interface, in one
    /// compilation. <c>IPlanFactory</c> does not exist until the generator runs.
    /// </summary>
    private const string HandlerTakingGeneratedFactorySource = @"
using Neatoo.RemoteFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record PlanEvent(int Id) : FactoryEventBase;

    [Factory]
    public partial class Plan
    {
        [Create]
        public Plan() { }

        [Fetch]
        public void Fetch(int id) { }
    }

    [FactoryEventHandler<PlanEvent>]
    public static partial class PlanHandlers
    {
        internal static Task Handle(PlanEvent evt, [Service] IPlanFactory planFactory)
        {
            return Task.CompletedTask;
        }
    }
}
";

    /// <summary>
    /// Same shape, but the factory lives in a nested namespace and the consumer wrote the
    /// parameter type partially qualified.
    /// </summary>
    /// <remarks>
    /// <b>Not a regression test — measured green on the v1.8.0 generator too.</b> When the
    /// qualifier resolves to a real namespace, Roslyn's error type keeps that containing
    /// namespace, so v1.8.0 emitted <c>global::TestNamespace.Sub.IPlanFactory</c> and it
    /// compiled. Only the BARE spelling, resolved through a <c>using</c>, lost its namespace.
    /// <para>
    /// What this guards is the fix's own path: an error type is now emitted as syntax text,
    /// which for this spelling is <c>Sub.IPlanFactory</c>, resolved relative to the
    /// handler's namespace in the generated file. That must keep working.
    /// </para>
    /// </remarks>
    private const string HandlerTakingPartiallyQualifiedGeneratedFactorySource = @"
using Neatoo.RemoteFactory;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace.Sub
{
    [Factory]
    public partial class Plan
    {
        [Create]
        public Plan() { }

        [Fetch]
        public void Fetch(int id) { }
    }
}

namespace TestNamespace
{
    public record PlanEvent(int Id) : FactoryEventBase;

    [FactoryEventHandler<PlanEvent>]
    public static partial class PlanHandlers
    {
        internal static Task Handle(PlanEvent evt, [Service] Sub.IPlanFactory planFactory)
        {
            return Task.CompletedTask;
        }
    }
}
";

    /// <summary>
    /// The regression test. The consumer's build must succeed.
    /// </summary>
    [Fact]
    public void RelayHandler_ServiceParamIsGeneratedFactoryInterface_OutputCompiles()
        => AssertOutputCompiles(HandlerTakingGeneratedFactorySource);

    /// <summary>
    /// Pins the mechanism: the token is emitted as the consumer wrote it, and is NOT
    /// <c>global::</c>-prefixed — there is no namespace to prefix it into.
    /// </summary>
    [Fact]
    public void RelayHandler_ServiceParamIsGeneratedFactoryInterface_EmitsNameAsWritten()
    {
        var generated = GeneratedHandlerSource(HandlerTakingGeneratedFactorySource);

        Assert.Contains("sp.GetRequiredService<IPlanFactory>()", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("global::IPlanFactory", generated, StringComparison.Ordinal);
    }

    /// <summary>
    /// A partially qualified spelling survives. This is the case a bare symbol name would
    /// get wrong even without the <c>global::</c> prefix.
    /// </summary>
    [Fact]
    public void RelayHandler_ServiceParamIsPartiallyQualifiedGeneratedFactory_OutputCompiles()
        => AssertOutputCompiles(HandlerTakingPartiallyQualifiedGeneratedFactorySource);

    private static void AssertOutputCompiles(string source)
    {
        var (_, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(source);

        // Both pipelines must have produced output — the factory (which declares
        // IPlanFactory) AND the handler registration — or a clean compile proves nothing.
        Assert.NotNull(runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("PlanHandlers")));
        Assert.NotNull(runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.Contains("PlanFactory")));

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToList();

        // Not Assert.Empty: xUnit truncates each item to ~50 chars, which is exactly enough
        // to show the generated file's path prefix and none of the error.
        Assert.True(errors.Count == 0,
            "Generated output did not compile:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
    }

    private static string GeneratedHandlerSource(string source)
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generated = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("PlanHandlers"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generated);
        return generated;
    }
}
