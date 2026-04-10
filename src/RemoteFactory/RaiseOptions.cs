namespace Neatoo.RemoteFactory;

/// <summary>
/// Options controlling how <see cref="IFactoryEvents.Raise{T}"/> dispatches event handlers.
/// </summary>
[Flags]
public enum RaiseOptions
{
    /// <summary>
    /// Default behavior: remote handlers are fire-and-forget (await server acknowledgment only),
    /// and the first handler failure propagates immediately.
    /// </summary>
    None = 0,

    /// <summary>
    /// Await full completion of remote handlers (HTTP connection stays open until handlers finish).
    /// Without this flag, remote handlers use fire-and-forget semantics.
    /// </summary>
    AwaitRemote = 1,

    /// <summary>
    /// Continue executing remaining handlers if one fails.
    /// All exceptions are collected into an <see cref="AggregateException"/>.
    /// Without this flag, the first handler failure propagates immediately.
    /// </summary>
    ContinueOnFail = 2,

    /// <summary>
    /// Dispatch to server-side handlers only. The event is NOT captured for relay
    /// back to the client in the <see cref="RemoteResponseDto"/>.
    /// </summary>
    ServerOnly = 4
}
