namespace Neatoo.RemoteFactory;

/// <summary>
/// Client-side service for registering/unregistering factory event handlers.
/// Handlers decorated with <c>[FactoryEventHandler&lt;T&gt;]</c> register here to receive
/// events relayed from the server after factory operations complete.
/// </summary>
public interface IFactoryEventRelay
{
    void Register(object handler);
    void Unregister(object handler);
}
