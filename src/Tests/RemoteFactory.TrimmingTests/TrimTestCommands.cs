using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// Positional-record DTOs carried by <see cref="TrimTestCommands._ProcessRecord"/>.
/// None of these are constructed anywhere in client-reachable code — their
/// constructors and properties survive trimming only through the generator-emitted
/// DtoConstructorRegistry.PreserveType&lt;T&gt;() calls (TRIM-001). RecordDtoSmokeTest
/// deserializes them from JSON literals to prove that preservation.
/// </summary>
public record TrimRecordDetail(string Notes);
public record TrimRecordResult(int Id, string Message, TrimRecordDetail Detail);
public record TrimRecordCommand(int PatientId, string Reason);

/// <summary>
/// Static factory used to test IL trimming of server-only dependencies.
/// Static factories use delegate types (not factory interfaces), which are
/// the original scenario where IL trimming broke factory registration.
/// </summary>
[Factory]
public static partial class TrimTestCommands
{
    [Remote]
    [Execute]
    private static Task<string> _DoWork(string input, [Service] IServerOnlyRepository repo)
    {
        return Task.FromResult(repo.DoServerWork(input));
    }

    // Positional records as [Execute] return type (with a nested record) and as a
    // non-service parameter — the zTreatment StartVisitResultV2 shape (TRIM-001).
    //
    // DTO discovery is signature-based, so the body deliberately never constructs the
    // records. A `new TrimRecordResult(...)` here would root the ctor from this method
    // body and make RecordDtoSmokeTest pass even without the generator's PreserveType
    // emission — a vacuous check.
    //
    // This comment used to justify that by calling the body "retained, guarded-dead",
    // per the TRIM-005 story that the trimmer keeps guarded-dead bodies. That story was
    // disproven, and since TRIM-008 this body is measurably GONE from the trimmed client
    // (_ProcessRecord is one of the gate's absence markers). The precaution still stands
    // on its own footing though: the fixture must not depend on trimming behavior to stay
    // non-vacuous, because a change that started retaining bodies again would silently
    // re-root the ctor and turn RecordDtoSmokeTest green for the wrong reason.
    [Remote]
    [Execute]
    private static Task<TrimRecordResult?> _ProcessRecord(TrimRecordCommand command, [Service] IServerOnlyRepository repo)
    {
        repo.DoServerWork(command.Reason);
        return Task.FromResult<TrimRecordResult?>(null);
    }

    // ASYNC [Execute]. Every leg TRIM-008 proved clean was measured with a synchronous
    // server-only body; the one leg with async bodies came back leaking (TRIM-009). Without
    // this target, "the static leg is clean" generalizes from sync to async by inference —
    // the same inference TRIM-009 falsified for class factories.
    [Remote]
    [Execute]
    private static async Task<string> _DoAsyncWork(string input, [Service] IAsyncLegPort port)
    {
        return await port.AsyncLegInvoke("StaticAsyncBody_MARKER: " + input).ConfigureAwait(false);
    }

    // THE BARE HALF OF THE STATIC PAIR (EXRM-003).
    //
    // Identical to the [Remote] methods above in every respect the trimmer can see
    // -- same class, same generated registrar, same holder, same [Service] injection
    // -- except that it carries no [Remote]. Since EXRM-001 that single difference
    // decides the emission: a bare delegate gets ONE unguarded local registration in
    // every factory mode and no remote registration, so no IsServerRuntime guard is
    // emitted, nothing folds, and this body is expected to SURVIVE trimming.
    //
    // That expectation is the point. The gate asserts BareStaticBody_MARKER PRESENT
    // beside _DoWork's marker ABSENT, so the two halves cannot both be satisfied by
    // a dead feature: if the guard ever returned to bare [Execute] this goes red,
    // and if it were ever lost from [Remote, Execute] the other half goes red.
    //
    // The [Service] is IClientTallyPort, registered OUTSIDE Program.cs's feature
    // switch guard -- a bare [Execute] resolves its services from whichever
    // container runs it, and on a client that is the client's. Concatenated, not
    // interpolated, so the literal survives intact in the user-string heap.
    [Execute]
    private static Task<string> _ComputeTally(string input, [Service] IClientTallyPort port)
    {
        return Task.FromResult(port.ClientTallyCompute("BareStaticBody_MARKER: " + input));
    }
}
