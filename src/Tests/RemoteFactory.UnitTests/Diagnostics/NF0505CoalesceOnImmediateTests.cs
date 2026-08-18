using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// NF0505: Coalesce = true on an Immediate-declared registration. Immediate dispatches
/// are never queued, so the flag is inert there — Warning per the NF0503/NF0504
/// compiles-but-inert precedent. The registration is still emitted faithfully.
/// </summary>
public class NF0505CoalesceOnImmediateTests
{
    private static string HandlerSource(string attribute) => @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record CoalesceEvent(int Id) : FactoryEventBase;

    " + attribute + @"
    public static partial class CoalesceHandlers
    {
        internal static Task Handle(CoalesceEvent evt) => Task.CompletedTask;
    }
}
";

    [Fact]
    public void NF0505_CoalesceWithNoPhaseArgument_ReportsWarning()
    {
        // No phase argument IS Immediate — the default must not dodge the diagnostic.
        var source = HandlerSource("[FactoryEventHandler<CoalesceEvent>(Coalesce = true)]");

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0505", DiagnosticSeverity.Warning);
        Assert.Contains("CoalesceHandlers", diagnostic.GetMessage());
        Assert.Contains("CoalesceEvent", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0505_CoalesceAtExplicitImmediate_ReportsWarning()
    {
        var source = HandlerSource("[FactoryEventHandler<CoalesceEvent>(DispatchPhase.Immediate, Coalesce = true)]");

        DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0505", DiagnosticSeverity.Warning);
    }

    [Fact]
    public void NF0505_CoalesceAtADeferredPhase_NoDiagnostic()
    {
        var source = HandlerSource("[FactoryEventHandler<CoalesceEvent>(DispatchPhase.AfterFlush, Coalesce = true)]");

        DiagnosticTestHelper.AssertNoRemoteFactoryDiagnostics(source);
    }

    [Fact]
    public void NF0505_CoalesceFalseAtImmediate_NoDiagnostic()
    {
        // The diagnostic keys off the VALUE, not the argument's presence — an explicit
        // false is not a mistake to warn about.
        var source = HandlerSource("[FactoryEventHandler<CoalesceEvent>(Coalesce = false)]");

        DiagnosticTestHelper.AssertNoRemoteFactoryDiagnostics(source);
    }

    [Fact]
    public void NF0505_RegistrationIsStillEmittedFaithfully()
    {
        // The warning is the loudness; the emitted registration carries the declared
        // (inert) flag rather than being coerced or suppressed.
        var source = HandlerSource("[FactoryEventHandler<CoalesceEvent>(Coalesce = true)]");

        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("CoalesceHandlers"))
            .GetText()
            .ToString();

        Assert.Contains(
            "global::Neatoo.RemoteFactory.DispatchPhase.Immediate, coalesce: true, async (sp, eventObj, options, ct) =>",
            generatedSource);
    }
}
