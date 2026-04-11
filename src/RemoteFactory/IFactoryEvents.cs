namespace Neatoo.RemoteFactory;

/// <summary>
/// Mediator for publishing events to source-generated <c>[FactoryEventHandler&lt;T&gt;]</c> handlers.
/// Inject this interface to raise events without depending on specific handler types.
/// </summary>
/// <remarks>
/// <para>
/// <b>Execution model.</b> <c>Raise</c> is always shared-scope, sequential, and awaited:
/// handlers share the caller's DI scope (so a <c>DbContext</c> injected into the factory
/// method and a <c>DbContext</c> injected into a handler are the same instance), run one
/// after another in unspecified order, and any handler exception aborts the remaining
/// handlers and propagates to the caller. Across the client/server boundary the HTTP call
/// stays open until every server-side handler has completed.
/// </para>
/// <para>
/// This makes <c>FactoryEvent</c> the right tool for domain events that must participate
/// in the caller's transaction. For fire-and-forget work, use the <c>[Event]</c> delegate
/// pattern instead.
/// </para>
/// </remarks>
public interface IFactoryEvents
{
    /// <summary>
    /// Raises an event, dispatching to all registered <c>[FactoryEventHandler&lt;T&gt;]</c>
    /// handlers for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The event type (must inherit from <see cref="FactoryEventBase"/>).</typeparam>
    /// <param name="factoryEvent">The event object to dispatch.</param>
    /// <param name="options">Options controlling dispatch behavior.</param>
    /// <param name="cancellationToken">
    /// Cancellation token passed to every handler that declares a <see cref="CancellationToken"/> parameter.
    /// </param>
    Task Raise<T>(T factoryEvent, RaiseOptions options = RaiseOptions.None, CancellationToken cancellationToken = default) where T : FactoryEventBase;

    /// <summary>
    /// Raises an event using the runtime type for dispatch. Used by the server
    /// when handling remote <c>Raise</c> requests where the concrete type is only known at runtime.
    /// </summary>
    Task RaiseUntyped(FactoryEventBase factoryEvent, RaiseOptions options = RaiseOptions.None, CancellationToken cancellationToken = default);
}
