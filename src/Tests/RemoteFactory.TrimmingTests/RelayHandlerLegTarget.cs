using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

// =============================================================================
// RELAY-HANDLER LEG TARGET (TRIM-008)
// =============================================================================
//
// Before this file the harness had NO [FactoryEventHandler<T>] target — the
// string appeared only in a comment. The leg was therefore never measured under
// trimming, which is how the registrar-DAM defect survived on it undetected.
//
// The generator re-opens THIS class to host FactoryServiceRegistrar and (at the
// time of writing) points the assembly attribute at it:
//
//     [assembly: NeatooFactoryRegistrar(typeof(RemoteFactory.TrimmingTests.TrimRelayHandlers))]
//
// The attribute's [DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]
// retains every method on the named type, BODIES INCLUDED. So RelayLegHandlerBody
// — and with it IRelayLegPort, RelayLegInvoke, and the marker literal below — is
// retained on a trimmed client that can never legally call it.
//
// WHY THERE IS NO POSITIVE CONTROL FOR THIS LEG
//
// RelayHandlerRenderer wraps every RegisterHandler call in
// `if (NeatooRuntime.IsServerRuntime)`. On a client publish the registrar body
// folds to nothing, so there is no service, delegate, or registry entry to
// resolve — nothing this harness can assert PRESENT to prove registration still
// works. That counter-signal has to come from the untrimmed integration suite
// (FactoryEventHandlerTargets), not from here. Stated rather than papered over:
// an absence-only check passes just as happily when the feature is dead.
//
// NF0502 TRAP: two static handler methods matching the same event type on one
// class is an ambiguous match — the transform reports and skips, yielding ZERO
// entries and an empty generated registrar. A fixture shaped that way tests
// nothing. Exactly one handler method here, for exactly one event type.
// =============================================================================

/// <summary>
/// Event carried by the relay-handler leg. A <see cref="FactoryEventBase"/>
/// descendant, so the per-assembly event-preservation registrar (TRIM-007)
/// preserves it — it is expected to be PRESENT after trimming.
/// </summary>
public record TrimRelayHandlerEvent(int Id, string Message) : FactoryEventBase;

/// <summary>
/// Static server-side handler for <see cref="TrimRelayHandlerEvent"/>.
/// </summary>
[FactoryEventHandler<TrimRelayHandlerEvent>]
public static partial class TrimRelayHandlers
{
    /// <summary>
    /// Server-only work. Reaches <see cref="IRelayLegPort"/>, which is registered
    /// only behind the harness's IsServerRuntime guard — so on a trimmed client
    /// this body is unreachable and every name it mentions should be gone.
    /// </summary>
    internal static Task RelayLegHandlerBody(
        TrimRelayHandlerEvent relayEvent,
        [Service] IRelayLegPort port,
        CancellationToken cancellationToken)
    {
        // Plain concatenation, not interpolation: an interpolated string can be
        // lowered to a DefaultInterpolatedStringHandler call sequence, which
        // splits the literal. Concatenation leaves "RelayLegHandlerBody_MARKER: "
        // intact in the user-string heap, where a trimmed-DLL grep can find it.
        return port.RelayLegInvoke("RelayLegHandlerBody_MARKER: " + relayEvent.Message);
    }
}
