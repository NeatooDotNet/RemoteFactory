using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Client-side implementation of <see cref="IFactoryEvents"/> for Remote mode.
/// Serializes the event object and sends it to the server via <see cref="IMakeRemoteDelegateRequest"/>.
/// The server dispatches to local handlers.
/// </summary>
internal sealed class RemoteFactoryEvents : IFactoryEvents
{
    private readonly IMakeRemoteDelegateRequest _remoteRequest;

    public RemoteFactoryEvents(IMakeRemoteDelegateRequest remoteRequest)
    {
        _remoteRequest = remoteRequest;
    }

    public Task Raise<T>(T factoryEvent, RaiseOptions options = RaiseOptions.None) where T : FactoryEventBase
    {
        // Send event to server — the server's FactoryEventsDispatcher handles local dispatch.
        // Fire-and-forget: await server acknowledgment only.
        // AwaitRemote is not yet supported — deferred to v2.
        return _remoteRequest.ForDelegateEvent(typeof(RaiseFactoryEventRemote), [factoryEvent, (int)options]);
    }

    public Task RaiseUntyped(FactoryEventBase factoryEvent, RaiseOptions options = RaiseOptions.None)
    {
        return _remoteRequest.ForDelegateEvent(typeof(RaiseFactoryEventRemote), [factoryEvent, (int)options]);
    }
}

/// <summary>
/// Marker delegate type used for remote IFactoryEvents.Raise requests.
/// The server recognizes this delegate type and dispatches to the local IFactoryEvents.
/// </summary>
public delegate Task RaiseFactoryEventRemote(FactoryEventBase factoryEvent, int options);
