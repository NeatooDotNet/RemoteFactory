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

    /// <summary>
    /// NF0505 is located at the <b>attribute</b>, not at the handler class's identifier —
    /// deliberately diverging from NF0501/NF0502/NF0504's class-identifier convention.
    /// </summary>
    /// <remarks>
    /// The divergence is the point: <c>[FactoryEventHandler&lt;T&gt;]</c> is
    /// <c>AllowMultiple</c>, so a class can carry several, and only one of them declared
    /// the inert flag. A class-located squiggle would tell a consumer with four stacked
    /// attributes that something on this class is wrong without saying which — the
    /// complaint NF0504's own location remark records as a callout. Asserted through the
    /// source span rather than a line/column pair so an edit to the fixture cannot leave
    /// this green while pointing somewhere else. If someone "fixes" the inconsistency by
    /// moving NF0505 to the class, this goes red and the choice gets made on purpose.
    /// </remarks>
    [Fact]
    public void NF0505_DiagnosticIsLocatedAtTheAttributeNotTheClass()
    {
        var source = HandlerSource("[FactoryEventHandler<CoalesceEvent>(Coalesce = true)]");

        var (diagnostics, _, _) = DiagnosticTestHelper.RunGenerator(source);

        var coalesceOnImmediate = Assert.Single(diagnostics.Where(d => d.Id == "NF0505"));
        var span = coalesceOnImmediate.Location.SourceSpan;
        var located = source.Substring(span.Start, span.Length);

        // The AttributeSyntax node, which excludes the enclosing brackets of its
        // AttributeList — Roslyn's convention, and what a squiggle over "this attribute"
        // means.
        Assert.Equal("FactoryEventHandler<CoalesceEvent>(Coalesce = true)", located);
        Assert.NotEqual("CoalesceHandlers", located);
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
