using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.Core;

/// <summary>
/// Guards the diagnostic test harness itself.
/// </summary>
/// <remarks>
/// <c>RunGenerator</c> used to concatenate the driver's out-param with
/// <c>GetRunResult().Diagnostics</c> — the same set twice — so every diagnostic came back
/// doubled. No test was red, because no caller counted; the 48 call sites all discard the
/// diagnostics slot, and the convenience wrappers use <c>Where</c> / <c>First</c> / "any?".
/// It only surfaced when NF0504's tests asserted <c>Single()</c> and got two.
/// <para>
/// A harness that silently doubles its output is worse than one that is merely awkward: it
/// makes the natural assertion wrong in a way that reads as a product bug. These tests exist
/// so the next person to touch the helper cannot reintroduce the doubling, and cannot lose
/// multiplicity while fixing it.
/// </para>
/// </remarks>
public class DiagnosticTestHelperTests
{
    /// <summary>
    /// One duplicate attribute produces exactly one NF0504, not two.
    /// </summary>
    [Fact]
    public void RunGenerator_ReturnsEachDiagnosticOnce()
    {
        const string source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record HelperEvent(int Id) : FactoryEventBase;

    [FactoryEventHandler<HelperEvent>]
    [FactoryEventHandler<HelperEvent>(DispatchPhase.AfterCommit)]
    public static partial class HelperHandlers
    {
        internal static Task Handle(HelperEvent evt) => Task.CompletedTask;
    }
}
";
        var (diagnostics, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        Assert.Single(diagnostics.Where(d => d.Id == "NF0504"));

        // Same count from the raw run result — i.e. the returned array is one of the two
        // sources, not a concatenation of both.
        Assert.Equal(
            runResult.Diagnostics.Count(d => d.Id == "NF0504"),
            diagnostics.Count(d => d.Id == "NF0504"));
    }

    /// <summary>
    /// A diagnostic genuinely reported twice comes back twice.
    /// </summary>
    /// <remarks>
    /// Two attributes for one event type on a class with two matching handler methods make the
    /// transform report NF0502 once per attribute, at the same location, with byte-identical
    /// messages. That count is real signal — collapsing it would turn a reported-twice into a
    /// reported-once with nothing to notice.
    /// <para>
    /// Worth recording what this test measured rather than what was expected of it: a
    /// <c>Distinct()</c>-based fix for the doubling bug was predicted to break this pin and did
    /// not. <c>Diagnostic</c> comparison here behaves by identity, and the doubling came from
    /// concatenating two collections that hold the SAME instances — so identity dedupe removes
    /// the copies and leaves genuine repeats, which are distinct instances, alone. The helper
    /// still avoids the concatenation instead, because that correctness depends on an identity
    /// guarantee nothing in this repo controls. This pin covers both implementations.
    /// </para>
    /// </remarks>
    [Fact]
    public void RunGenerator_PreservesGenuinelyRepeatedDiagnostics()
    {
        const string source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record RepeatEvent(int Id) : FactoryEventBase;

    [FactoryEventHandler<RepeatEvent>]
    [FactoryEventHandler<RepeatEvent>]
    public static partial class RepeatHandlers
    {
        internal static Task HandleOne(RepeatEvent evt) => Task.CompletedTask;

        internal static Task HandleTwo(RepeatEvent evt) => Task.CompletedTask;
    }
}
";
        var (diagnostics, _, _) = DiagnosticTestHelper.RunGenerator(source);

        var ambiguous = diagnostics.Where(d => d.Id == "NF0502").ToList();

        Assert.Equal(2, ambiguous.Count);

        // And they really are indistinguishable — which is what makes Distinct() destructive
        // here rather than harmless.
        Assert.Equal(ambiguous[0].Location, ambiguous[1].Location);
        Assert.Equal(ambiguous[0].GetMessage(), ambiguous[1].GetMessage());
    }

    /// <summary>
    /// Clean source returns nothing, so neither test above passes by counting noise.
    /// </summary>
    [Fact]
    public void RunGenerator_CleanSource_ReturnsNoRemoteFactoryDiagnostics()
    {
        const string source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record CleanEvent(int Id) : FactoryEventBase;

    [FactoryEventHandler<CleanEvent>]
    public static partial class CleanHandlers
    {
        internal static Task Handle(CleanEvent evt) => Task.CompletedTask;
    }
}
";
        var (diagnostics, _, _) = DiagnosticTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Id.StartsWith("NF", StringComparison.Ordinal)));
    }
}
