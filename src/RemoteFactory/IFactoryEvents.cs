namespace Neatoo.RemoteFactory;

/// <summary>
/// Mediator for publishing events to source-generated handlers.
/// Inject this interface to raise events without depending on specific handler types.
/// </summary>
public interface IFactoryEvents
{
    /// <summary>
    /// Raises an event, dispatching to all registered <c>[FactoryEventHandler]</c> methods
    /// that accept the event type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The event type (must inherit from <see cref="FactoryEventBase"/>).</typeparam>
    /// <param name="event">The event object to dispatch.</param>
    /// <param name="options">Options controlling dispatch behavior (fire-and-forget, await remote, continue on fail).</param>
    /// <returns>A task representing the dispatch operation.</returns>
    Task Raise<T>(T factoryEvent, RaiseOptions options = RaiseOptions.None) where T : FactoryEventBase;

    /// <summary>
    /// Raises an event using the runtime type for dispatch. Used by the server
    /// when handling remote <c>Raise</c> requests where the concrete type is only known at runtime.
    /// </summary>
    Task RaiseUntyped(FactoryEventBase factoryEvent, RaiseOptions options = RaiseOptions.None);
}
