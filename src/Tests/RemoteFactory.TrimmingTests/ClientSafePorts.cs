namespace RemoteFactory.TrimmingTests;

// =============================================================================
// CLIENT-SAFE DEPENDENCY FOR THE BARE [Execute] PAIR (EXRM-003)
// =============================================================================
//
// Every other port in this harness lives in LegServerOnlyPorts.cs and is
// registered INSIDE Program.cs's `if (NeatooRuntime.IsServerRuntime)` block, so
// that nothing roots it on a client publish. This one is the opposite by design.
//
// A bare `[Execute]` -- no [Remote] -- is registered unguarded and runs on
// whichever tier resolves it, taking its [Service] parameters from that tier's
// container. That is the shape EXRM-003 measures, and it needs a service the
// CLIENT can actually resolve: a bare target depending on a server-only port
// would fail at call time on the client, which is a different (and already
// covered) property. So this port is registered unconditionally and is expected
// to be PRESENT in the trimmed output.
//
// WHY ITS NAME IS NOT A GATE ASSERTION. Being registered unguarded, it is rooted
// from the DI graph whether or not the [Execute] bodies survive -- so a "present"
// check on this type could never go red, and asserting it would be a check that
// cannot fail (EXRM-003 plan review, callout B2). What the gate asserts is the
// BODY LITERALS of the two bare methods. This port's job is to make those bodies
// resolvable and executable on the trimmed client, which Program.cs verifies by
// calling them.
//
// NAMING. The harness's substring rule (see LegServerOnlyPorts.cs) is sharper
// here than anywhere else, because these names are PRESENT while their
// neighbours are asserted ABSENT: `present()` is a substring grep, so a name
// like `BareExecLegInvoke` would satisfy the `ExecLegInvoke` ABSENCE check and
// turn it red for entirely the wrong reason. A distinct stem, never an existing
// marker with an affix.
// =============================================================================

/// <summary>
/// Client-safe dependency of the bare <c>[Execute]</c> pair. Pure computation, no
/// server-side reach, registered outside the harness's feature-switch guard.
/// </summary>
public interface IClientTallyPort
{
    string ClientTallyCompute(string input);
}

/// <summary>
/// Implementation of <see cref="IClientTallyPort"/>. Suffix differs from the
/// interface's (<c>Engine</c> vs <c>Port</c>) so neither name contains the other.
/// </summary>
/// <remarks>
/// The <c>|tallied:</c> stamp is what Program.cs matches to confirm a bare body
/// actually ran, and it is deliberately NOT one of the gate's markers — see the
/// note there on why the harness must never mention a marker literal.
/// </remarks>
public sealed class ClientTallyEngine : IClientTallyPort
{
    public string ClientTallyCompute(string input) =>
        input + "|tallied:" + input.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
