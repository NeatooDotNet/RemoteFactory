using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

// =============================================================================
// INTERFACE-FACTORY LEG TARGET (TRIM-008, closing plan-review B9)
// =============================================================================
//
// The interface-factory leg is the one the arc has always CLAIMED is safe, on
// the structural grounds that its assembly attribute names the GENERATED proxy
// (TrimIfaceQueryFactory) rather than any user type — so DAM has nothing of the
// consumer's to over-retain.
//
// That claim was never measured. This target measures it, which is the whole
// point of B9: an arc that asserts two legs are safe while only ever probing one
// of them is asserting, not verifying.
//
// Note the shape difference from the broken legs. There is no [Remote] method
// body here to strip — an interface factory's client side is a proxy that
// serializes calls. The server-only IP lives in the IMPLEMENTATION class
// (TrimIfaceServerSide), which carries no factory attribute at all and is
// registered only behind the harness's IsServerRuntime guard. If the interface
// leg is genuinely clean, nothing roots that class and its body is absent.
//
// NF0106 TRAP: operation attributes ([Fetch], [Execute], ...) on an interface-
// factory member are a diagnostic, not a no-op — the member degrades and the
// generated factory silently loses it. No operation attributes below; [Factory]
// on the interface is the whole contract.
// =============================================================================

/// <summary>
/// Interface factory. The generator emits <c>ITrimIfaceQueryFactory</c> and a
/// <c>TrimIfaceQueryFactory</c> proxy; the assembly attribute names the proxy.
/// </summary>
[Factory]
public interface ITrimIfaceQuery
{
    Task<string> LookupAsync(string key);
}

/// <summary>
/// Server-side implementation. Deliberately carries NO factory attribute — the
/// [Factory] on the interface is sufficient, and adding one here would cause
/// duplicate registration.
/// </summary>
/// <remarks>
/// Every name in this class is an absence marker: the type name, its
/// <see cref="IIfaceLegPort"/> dependency, and the literal in the body. All three
/// should be gone from a trimmed client publish.
/// </remarks>
public sealed class TrimIfaceServerSide : ITrimIfaceQuery
{
    private readonly IIfaceLegPort port;

    public TrimIfaceServerSide(IIfaceLegPort port)
    {
        this.port = port;
    }

    public Task<string> LookupAsync(string key)
    {
        // Concatenated, not interpolated — see the note in RelayHandlerLegTarget.
        return Task.FromResult(port.IfaceLegInvoke("IfaceLegServerBody_MARKER: " + key));
    }
}
