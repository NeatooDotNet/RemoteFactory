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

    // Branch 4: FactoryEventBase descendant.
    public record OrderPlacedEvent(int OrderId, string CustomerEmail) : FactoryEventBase;

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

    // Branch 2: [Factory] interface.
    [Factory]
    public interface IOrderReader
    {
        [Fetch]
        OrderSummary? Read(int id);
    }

    // Branch 3: [FactoryEventHandler<T>] — two handlers, each with non-service
    // parameters, service parameters, and a CancellationToken, so Entries and all
    // three parameter collections on EventHandlerEntry are non-empty.
    [FactoryEventHandler<OrderPlacedEvent>]
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
            OrderPlacedEvent orderEvent,
            [Service] INotifier notifier,
            CancellationToken cancellationToken)
        {
            await notifier.SendAsync(""audit@example.com"", ""audited"");
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
