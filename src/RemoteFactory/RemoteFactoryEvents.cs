using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Client-side implementation of <see cref="IFactoryEvents"/> for Remote mode.
/// Serializes the event object and sends it to the server via <see cref="IMakeRemoteDelegateRequest"/>.
/// The server dispatches to its <c>[FactoryEventHandler&lt;T&gt;]</c> handlers in the request scope
/// and the HTTP call stays open until every handler has completed.
/// </summary>
internal sealed class RemoteFactoryEvents : IFactoryEvents
{
    private readonly IMakeRemoteDelegateRequest _remoteRequest;

    public RemoteFactoryEvents(IMakeRemoteDelegateRequest remoteRequest)
    {
        _remoteRequest = remoteRequest;
    }

    public Task Raise<T>(T factoryEvent, RaiseOptions options = RaiseOptions.None, CancellationToken cancellationToken = default) where T : FactoryEventBase
    {
        return _remoteRequest.ForDelegateEvent(typeof(RaiseFactoryEventRemote), [factoryEvent, (int)options], cancellationToken);
    }

    public Task RaiseUntyped(FactoryEventBase factoryEvent, RaiseOptions options = RaiseOptions.None, CancellationToken cancellationToken = default)
    {
        return _remoteRequest.ForDelegateEvent(typeof(RaiseFactoryEventRemote), [factoryEvent, (int)options], cancellationToken);
    }
}

/// <summary>
/// Marker delegate type used for remote <see cref="IFactoryEvents.Raise{T}"/> requests.
/// The server recognizes this delegate type and dispatches to the local <see cref="IFactoryEvents"/>.
/// The <see cref="CancellationToken"/> is injected by the server from the HTTP request pipeline.
/// </summary>
public delegate Task RaiseFactoryEventRemote(FactoryEventBase factoryEvent, int options, CancellationToken cancellationToken);
