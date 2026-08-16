using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.Core;

/// <summary>
/// Guards the generator's incremental caching (TRIM-006). Every pipeline branch
/// calls RegisterSourceOutput directly on its transform node and does model-building
/// and rendering inside the output stage, so the transform-output record's equality
/// is the entire cache boundary. A field whose equality falls back to reference
/// semantics — an IReadOnlyList&lt;T&gt; or a raw array instead of EquatableArray&lt;T&gt; —
/// makes every consumer re-render the world on any unrelated edit, with no other
/// symptom. These tests are the only thing standing between that and shipping.
/// </summary>
public class IncrementalCacheTests
{
    /// <summary>
    /// Mirrors <c>Neatoo.TrackingNames</c> in the generator. Deliberately duplicated as
    /// literals: the generator is loaded dynamically by the helper, and mirroring keeps
    /// a rename loud — <see cref="Fixture_ExercisesEveryPipelineBranch"/> fails if any
    /// name here stops matching a real tracked step.
    /// </summary>
    private static readonly string[] AllBranches =
    [
        "FactoryClass",
        "FactoryInterface",
        "RelayHandler",
        "FactoryEvents"
    ];

    /// <summary>
    /// Exercises all four pipeline branches, and deliberately POPULATES the
    /// collection-bearing fields on each transform output. A sparse fixture would
    /// leave those collections empty, where two independently-allocated empties can
    /// still compare equal — and the guard would pass vacuously over exactly the
    /// fields it exists to protect.
    /// </summary>
    private const string Fixture = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public interface IServerService
    {
        string Work(string input);
    }

    public interface INotifier
    {
        Task SendAsync(string to, string body);
    }

    // DTO reachable through a factory signature and as an entity property —
    // populates DtoReturnTypes / DtoPreserveTypes on TypeInfo.
    public record OrderSummary(int Id, string Label);

    // Branch 4: FactoryEventBase descendants.
    public record OrderPlacedEvent(int OrderId, string CustomerEmail) : FactoryEventBase;
    public record OrderShippedEvent(int OrderId, string Carrier) : FactoryEventBase;

    // Branch 1: [Factory] class — multiple methods, services, DTO in the signature.
    [Factory]
    public partial class OrderEntity
    {
        public string Name { get; set; } = string.Empty;
        public OrderSummary? Summary { get; set; }

        [Create]
        public void Create(string name)
        {
            this.Name = name;
        }

        [Fetch]
        [Remote]
        internal OrderSummary? FetchSummary(int id, [Service] IServerService service)
        {
            return null;
        }
    }

    // Branch 2: [Factory] interface. No operation attributes on the members — the
    // interface IS the remote boundary, and [Fetch] here is NF0106.
    [Factory]
    public interface IOrderReader
    {
        OrderSummary? Read(int id);
    }

    // Branch 3: [FactoryEventHandler<T>] — TWO DISTINCT event types, one matching
    // handler each, so RelayHandlerModel.Entries holds two EventHandlerEntry values
    // and each entry's Parameters / ServiceParameters / AllParameters are non-empty.
    //
    // The two attributes must name DIFFERENT events. Two handlers for the SAME event
    // is the NF0502 ambiguous-match shape: the transform reports the diagnostic and
    // `continue`s WITHOUT adding an entry, leaving Entries empty and every
    // EventHandlerEntry collection unconstructed — the guard would then cover none of
    // them while appearing to. Fixture_ProducesNoDiagnostics pins this.
    //
    // One phased and one defaulted, so EventHandlerEntry's phase fields are populated on
    // both paths. Note what this does and does NOT buy: the guard compares transform outputs
    // across two runs, so a scalar phase field is equal either way — including if the
    // generator hardcoded Immediate. It would catch a future phase representation that went
    // COLLECTION-shaped, which is the regression class this fixture exists for. Correctness
    // of the phase read is pinned in AssemblyAttributeEmissionTests, where it can go red.
    [FactoryEventHandler<OrderPlacedEvent>(DispatchPhase.AfterCommit)]
    [FactoryEventHandler<OrderShippedEvent>]
    public static partial class OrderHandlers
    {
        internal static async Task Notify(
            OrderPlacedEvent orderEvent,
            [Service] INotifier notifier,
            CancellationToken cancellationToken)
        {
            await notifier.SendAsync(orderEvent.CustomerEmail, ""placed"");
        }

        internal static async Task Audit(
            OrderShippedEvent orderEvent,
            [Service] INotifier notifier,
            CancellationToken cancellationToken)
        {
            await notifier.SendAsync(""audit@example.com"", orderEvent.Carrier);
        }
    }
}
";

    /// <summary>
    /// Appended, never inserted — see <c>DiagnosticTestHelper.RunGeneratorTracked</c>.
    /// Touches nothing the generator cares about.
    /// </summary>
    private const string UnrelatedAppendix = @"
namespace UnrelatedNamespace
{
    public class UnrelatedType
    {
        public string Value { get; set; } = string.Empty;
    }
}
";

    private static IReadOnlyList<IncrementalStepRunReason> ReasonsFor(
        GeneratorDriverRunResult result,
        string trackingName)
    {
        var run = result.Results.Single();
        return run.TrackedSteps.TryGetValue(trackingName, out var steps)
            ? steps.SelectMany(s => s.Outputs.Select(o => o.Reason)).ToList()
            : [];
    }

    [Theory]
    [InlineData("FactoryClass")]
    [InlineData("FactoryInterface")]
    [InlineData("RelayHandler")]
    [InlineData("FactoryEvents")]
    public void UnrelatedEdit_TransformOutputStaysCached(string trackingName)
    {
        var (_, second) = DiagnosticTestHelper.RunGeneratorTracked(Fixture, UnrelatedAppendix);

        var reasons = ReasonsFor(second, trackingName);

        Assert.True(
            reasons.Count > 0,
            $"Pipeline branch '{trackingName}' produced no tracked steps. The fixture no longer "
                + "exercises it, so the branch is unguarded — fix the fixture, do not delete the case.");

        var uncached = reasons
            .Where(r => r is not (IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged))
            .ToList();

        Assert.True(
            uncached.Count == 0,
            $"Pipeline branch '{trackingName}' lost its cache across an unrelated edit "
                + $"(reasons: {string.Join(", ", uncached)}). A transform-output record most likely "
                + "gained a field whose equality falls back to reference semantics. Collections on "
                + "transform outputs must be EquatableArray<T>, and their element types must be "
                + "value-equatable all the way down.");
    }

    /// <summary>
    /// Fixture health. A branch can report tracked steps while its transform bailed
    /// early on a diagnostic, leaving the very collections under test unconstructed —
    /// the guard then covers nothing and still passes. That is not hypothetical: the
    /// first version of this fixture declared two handlers for one event, hit NF0502,
    /// and silently guarded an empty <c>Entries</c> for the whole of TRIM-006.
    /// </summary>
    [Fact]
    public void Fixture_ProducesNoDiagnostics()
    {
        var (_, second) = DiagnosticTestHelper.RunGeneratorTracked(Fixture, UnrelatedAppendix);

        var nf = second.Diagnostics.Where(d => d.Id.StartsWith("NF")).ToList();

        Assert.True(
            nf.Count == 0,
            "The caching fixture must generate cleanly. A diagnostic means some transform took an "
                + "early-out path and never built the model the guard is supposed to protect: "
                + string.Join("; ", nf.Select(d => $"{d.Id} {d.GetMessage()}")));
    }

    /// <summary>
    /// Pins that the relay-handler branch actually reached emission. Complements
    /// <see cref="Fixture_ProducesNoDiagnostics"/>: the relay output stage returns early
    /// when <c>Entries.Count == 0</c>, so an empty-entry fixture produces no file here.
    /// </summary>
    [Fact]
    public void Fixture_EmitsRelayHandlerOutput()
    {
        var (_, second) = DiagnosticTestHelper.RunGeneratorTracked(Fixture, UnrelatedAppendix);

        var files = second.GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).ToList();

        Assert.True(
            files.Any(f => f.EndsWith(".FactoryEventHandler.g.cs")),
            "No relay-handler source was emitted, so RelayHandlerModel.Entries was empty and "
                + "EventHandlerEntry's collections were never constructed — the RelayHandler case of "
                + $"the caching guard would be vacuous. Emitted: {string.Join(", ", files)}");
    }

    /// <summary>
    /// Pins that the fixture's phase argument still binds, so the phase fields it claims to
    /// populate are actually populated.
    /// </summary>
    /// <remarks>
    /// Third fixture-health guard in this file, and the same vacuity class as the other two.
    /// If the phase argument ever stopped binding — a <c>using</c> dropped, a fixture edit —
    /// the transform's malformed-argument fallback returns <c>Immediate</c> silently, the
    /// fixture degrades to two defaulted attributes, and every test here stays green while
    /// covering less than its comment claims. Nothing else would notice:
    /// <c>RunGeneratorTracked</c> never inspects the input compilation for CS errors, and
    /// <see cref="Fixture_ProducesNoDiagnostics"/> filters on <c>NF</c> ids only.
    /// </remarks>
    [Fact]
    public void Fixture_PopulatesThePhaseArgument()
    {
        var (_, second) = DiagnosticTestHelper.RunGeneratorTracked(Fixture, UnrelatedAppendix);

        var relaySource = second.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith(".FactoryEventHandler.g.cs"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(relaySource);
        Assert.Contains("DispatchPhase.AfterCommit", relaySource);
        Assert.Contains("DispatchPhase.Immediate", relaySource);
    }

    [Fact]
    public void Fixture_ExercisesEveryPipelineBranch()
    {
        var (_, second) = DiagnosticTestHelper.RunGeneratorTracked(Fixture, UnrelatedAppendix);
        var tracked = second.Results.Single().TrackedSteps;

        var missing = AllBranches.Where(b => !tracked.ContainsKey(b)).ToList();

        Assert.True(
            missing.Count == 0,
            $"Tracked steps missing for: {string.Join(", ", missing)}. Either the fixture stopped "
                + "exercising these branches, or a WithTrackingName constant in the generator's "
                + $"TrackingNames was renamed without updating this test. Present: "
                + $"{string.Join(", ", tracked.Keys.OrderBy(k => k))}");
    }

    [Fact]
    public void UnrelatedEdit_GeneratedOutputIsIdentical()
    {
        // Caching must not be bought by skipping work that would have produced
        // different output. Also a determinism guard on the emissions themselves.
        var (first, second) = DiagnosticTestHelper.RunGeneratorTracked(Fixture, UnrelatedAppendix);

        static Dictionary<string, string> Sources(GeneratorDriverRunResult r) =>
            r.GeneratedTrees.ToDictionary(t => t.FilePath, t => t.GetText().ToString());

        var before = Sources(first);
        var after = Sources(second);

        Assert.Equal(before.Keys.OrderBy(k => k), after.Keys.OrderBy(k => k));

        foreach (var file in before.Keys)
        {
            Assert.Equal(before[file], after[file]);
        }
    }
}
