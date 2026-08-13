namespace RemoteFactory.TrimmingTests;

// =============================================================================
// PER-LEG SERVER-ONLY DEPENDENCIES (TRIM-008)
// =============================================================================
//
// The harness previously had ONE server-only dependency (IServerOnlyRepository)
// shared by the class-factory and static-factory targets. That is enough to say
// "something leaked" and not enough to say WHICH leg leaked it — the CI gate's
// failure message could only name the marker, not the culprit.
//
// Each factory shape now gets its own port. A marker in the trimmed output names
// exactly one leg.
//
// NAMING IS LOAD-BEARING. Every name below is chosen so that no marker is a
// substring of another marker, and no marker is a substring of a name that is
// EXPECTED to survive. The harness's original naming failed this: grepping for
// `ServerOnlyRepository` also matched `IServerOnlyRepository`, which is why
// build.yml carries a `(?<!I)` lookbehind. Deferred item 5 tracks that class of
// fragility; these names do not reproduce it.
//
// Specifically:
//   - The interface name always carries a suffix the implementation does not
//     (`...Port` vs `...Backend`), so neither contains the other.
//   - No name contains `DoWork`, `ServerOnly`, `ProcessRecord`, or any other
//     marker already in use by the pre-existing targets.
//
// The MARKER string literals live in the bodies that must disappear, NOT here.
// A literal in an implementation class only proves the implementation was rooted;
// a literal inside the [Remote] method / handler body proves the BODY was
// retained, which is the property under test.
// =============================================================================

/// <summary>
/// Server-only dependency of the <c>[FactoryEventHandler&lt;T&gt;]</c> leg.
/// Absent from a trimmed client publish iff the relay-handler body was eliminated.
/// </summary>
public interface IRelayLegPort
{
    Task RelayLegInvoke(string payload);
}

/// <summary>
/// Server-side implementation of <see cref="IRelayLegPort"/>. Registered only
/// inside the harness's <c>if (NeatooRuntime.IsServerRuntime)</c> block.
/// </summary>
public sealed class RelayLegBackend : IRelayLegPort
{
    public Task RelayLegInvoke(string payload) => Task.CompletedTask;
}

/// <summary>
/// Server-only dependency of the interface-factory leg.
/// </summary>
public interface IIfaceLegPort
{
    string IfaceLegInvoke(string input);
}

/// <summary>
/// Server-side implementation of <see cref="IIfaceLegPort"/>.
/// </summary>
public sealed class IfaceLegBackend : IIfaceLegPort
{
    public string IfaceLegInvoke(string input) => input;
}

/// <summary>
/// Server-only dependency of the Save/Can* leg.
/// </summary>
public interface ISaveLegPort
{
    Task SaveLegInvoke(string operation);
}

/// <summary>
/// Server-side implementation of <see cref="ISaveLegPort"/>.
/// </summary>
public sealed class SaveLegBackend : ISaveLegPort
{
    public Task SaveLegInvoke(string operation) => Task.CompletedTask;
}

/// <summary>
/// Server-only dependency of the CLASS-factory read path (<see cref="TrimTestEntity"/>).
/// </summary>
/// <remarks>
/// Added by TRIM-008's test review. `TrimTestEntity` previously shared
/// <see cref="IServerOnlyRepository"/> with the static-factory target, so a leak in either
/// leg produced the same marker and the CI gate filed both under "static factory". That
/// defeats the per-leg attribution this file exists for — and it matters now, because
/// TRIM-009 works on the class-factory leg and its regressions would have been reported
/// as static-factory failures.
/// </remarks>
public interface IClassLegPort
{
    string ClassLegInvoke(string input);
}

/// <summary>
/// Server-side implementation of <see cref="IClassLegPort"/>.
/// </summary>
public sealed class ClassLegBackend : IClassLegPort
{
    public string ClassLegInvoke(string input) => "ClassLegBackend_MARKER: " + input;
}

/// <summary>
/// Server-only dependency reached ONLY from `async` bodies.
/// </summary>
/// <remarks>
/// Every leg TRIM-008 proved clean was measured with a SYNCHRONOUS server-only body, and the
/// one leg with async bodies (Save/Can*) came back leaking. That made "static and relay are
/// clean" an inference of exactly the class TRIM-009 falsified. These markers convert it into
/// a measurement: an async `[Execute]` method and an async relay handler each reach this port.
/// </remarks>
public interface IAsyncLegPort
{
    Task<string> AsyncLegInvoke(string input);
}

/// <summary>
/// Server-side implementation of <see cref="IAsyncLegPort"/>.
/// </summary>
public sealed class AsyncLegBackend : IAsyncLegPort
{
    public Task<string> AsyncLegInvoke(string input) => Task.FromResult("AsyncLegBackend_MARKER: " + input);
}
