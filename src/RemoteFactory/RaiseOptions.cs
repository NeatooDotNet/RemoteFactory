namespace Neatoo.RemoteFactory;

/// <summary>
/// Options controlling how <see cref="IFactoryEvents.Raise{T}"/> dispatches event handlers.
/// </summary>
/// <remarks>
/// <para>
/// <c>FactoryEvent</c> raises are <b>shared-scope, sequential, and awaited</b>: handlers
/// share the caller's DI scope (and therefore the caller's <c>DbContext</c> and
/// transaction), run one after another in unspecified order, and any handler exception
/// aborts the remaining handlers and propagates to the caller. Across the client/server
/// boundary the HTTP call stays open until every server-side handler completes.
/// </para>
/// <para>
/// That describes <see cref="DispatchPhase.Immediate"/> handlers, which is the default and
/// was the only behavior before phases existed. Handlers registered at another
/// <see cref="DispatchPhase"/> are queued at raise time and run when their phase drains —
/// still in the same scope, but no longer before <c>Raise</c> returns.
/// </para>
/// </remarks>
[Flags]
public enum RaiseOptions
{
    /// <summary>
    /// Default behavior: dispatch to server-side handlers and capture the event for
    /// relay back to the client.
    /// </summary>
    None = 0,

    /// <summary>
    /// Dispatch to server-side handlers only. The event is NOT captured for relay
    /// back to the client in the <c>RemoteResponseDto</c>.
    /// </summary>
    ServerOnly = 1
}
