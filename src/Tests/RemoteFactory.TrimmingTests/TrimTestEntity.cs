using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// DTO types reachable ONLY as properties of <see cref="TrimTestEntity"/> — never
/// in any factory method signature and never constructed in client-reachable code.
/// Their trimming survival depends solely on the entity property-graph discovery
/// (TRIM-002): the generator walks TrimTestEntity's properties and emits
/// Register/PreserveType in the entity's own registrar. EntityPropertyDtoSmokeTest
/// deserializes them from JSON literals to prove that preservation.
/// </summary>
public class TrimEntityCarriedInfo
{
    public string? Text { get; set; }
}

public record TrimEntityCarriedBanner(string Text, string Severity);

/// <summary>
/// Domain entity used to test IL trimming of server-only dependencies.
/// The Create method uses a server-only [Service] parameter.
/// When published with IsServerRuntime=false and PublishTrimmed=true,
/// the LocalCreate method body should be eliminated by the trimmer,
/// and IServerOnlyRepository/ServerOnlyRepository should be absent
/// from the published output.
/// </summary>
[Factory]
public class TrimTestEntity
{
    public string? Name { get; set; }
    public string? ServerResult { get; set; }

    // Reachable only via these properties — see comment above (TRIM-002).
    public TrimEntityCarriedInfo? Info { get; set; }
    public TrimEntityCarriedBanner? Banner { get; set; }

    // Uses IClassLegPort, not the shared IServerOnlyRepository. Sharing a port with the
    // static-factory target meant a leak in either leg produced the same marker, so the CI
    // gate filed both under "static factory" — defeating per-leg attribution exactly when
    // TRIM-009 starts changing this leg. Retains the IServerOnlyRepository dependency too,
    // so the pre-existing markers keep their meaning.
    [Remote]
    [Create]
    internal void Create(string name, [Service] IServerOnlyRepository repo, [Service] IClassLegPort classPort)
    {
        Name = name;
        // ClassSyncBody_MARKER is the sync half of the controlled pair below. It must be a
        // literal in THIS body: ClassLegBackend_MARKER lives on the port implementation, which
        // is reached through IClassLegPort, so it is behind an interface hop and cannot report
        // on whether this body survived.
        ServerResult = repo.DoServerWork(name) + classPort.ClassLegInvoke("ClassSyncBody_MARKER: " + name);
    }

    // THE CONTROLLED ASYNC COMPARISON.
    //
    // The earlier sync-vs-async pair (this class's sync Create vs TrimSaveTarget's async
    // Insert) was presented as isolating `async`. It does not: those two differ in at least
    // four other ways — an [AuthorizeFactory<T>] block, target-from-DI vs target-from-
    // parameter, one-hop vs two-hop rooting (there is no InsertDelegate; Insert is reached
    // through SaveDelegate -> LocalSave), and an extra catch arm plus lifecycle probes. That
    // last difference matters most, because the arc's disproven TRIM-004 story blamed exactly
    // "early-throw guard + try/catch defeats unreachable-code elimination".
    //
    // This method controls all of it. Same class, same factory type, same registrar, same
    // absence of auth, same one-hop delegate rooting, same direct concrete call, marker literal
    // in the domain body on both sides. Both halves are also rooted TWICE and identically —
    // by DAM on TrimTestEntityFactory and by their own unguarded delegate registration — which
    // closes the "maybe the sync one just was not rooted" alternative outright.
    //
    // DO NOT "TIDY UP" THE SERVICE PARAMETERS TO MATCH. The asymmetry is deliberate and
    // load-bearing: Create takes IServerOnlyRepository and IClassLegPort, this takes only
    // IClassLegPort. Because this body SURVIVES trimming, giving it IServerOnlyRepository would
    // make IServerOnlyRepository and DoServerWork present in the trimmed output and turn the
    // gate's static-factory [D] markers red — a real failure with a completely misleading
    // cause. Keep them asymmetric until TRIM-009 lands.
    //
    // WHAT THIS PAIR DOES NOT ISOLATE. The generator emits several things only for async
    // methods: an extra catch (OperationCanceledException) arm, and type-tests for
    // IFactoryOnStartAsync / IFactoryOnCompleteAsync / IFactoryOnCancelled /
    // IFactoryOnCancelledAsync. They move with `async` by construction, so from outside the
    // generator this pair isolates "async-shaped emission" as a bundle, not the keyword. The
    // interface type-tests matter because they are a DIFFERENT ILLink retention mechanism from
    // a state machine — see the TRIM-009 stub, which carries the competing hypotheses.
    [Remote]
    [Fetch]
    internal async Task FetchAsync(string name, [Service] IClassLegPort classPort)
    {
        Name = name;
        ServerResult = await Task.FromResult(classPort.ClassLegInvoke("ClassAsyncBody_MARKER: " + name));
    }
}
