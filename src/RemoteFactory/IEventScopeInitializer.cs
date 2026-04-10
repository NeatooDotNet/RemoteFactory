namespace Neatoo.RemoteFactory;

/// <summary>
/// Initializes an event handler's DI scope with ambient context from the parent (request) scope.
/// </summary>
/// <remarks>
/// <para>
/// Event handlers run in isolated DI scopes created via <c>IServiceScopeFactory.CreateScope()</c>.
/// Implementations of this interface propagate scoped state (tenant context, user identity, etc.)
/// from the request scope into the event scope.
/// </para>
/// <para>
/// Initializers run inside <c>Task.Run</c> after <c>CreateScope()</c> but before handler services
/// are resolved. For fire-and-forget events, the parent scope may be disposed after the initializer
/// runs — <b>copy values, do not hold references to parent-scope services</b>.
/// </para>
/// <para>
/// Register custom initializers via
/// <see cref="RemoteFactoryServices.AddRemoteFactoryEventScopeInitializer"/>.
/// A built-in initializer propagates <see cref="ICorrelationContext.CorrelationId"/> automatically.
/// </para>
/// </remarks>
public interface IEventScopeInitializer
{
    /// <summary>
    /// Copies ambient context from the parent scope to the event's child scope.
    /// </summary>
    /// <param name="parentScope">The request-scoped service provider (may be disposed after this call in fire-and-forget scenarios).</param>
    /// <param name="childScope">The event handler's newly created scope.</param>
    void Initialize(IServiceProvider parentScope, IServiceProvider childScope);
}
