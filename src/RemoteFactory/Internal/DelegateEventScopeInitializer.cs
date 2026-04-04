namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Wraps an <see cref="Action{T1, T2}"/> as an <see cref="IEventScopeInitializer"/>.
/// </summary>
internal sealed class DelegateEventScopeInitializer(
    Action<IServiceProvider, IServiceProvider> initializer) : IEventScopeInitializer
{
    public void Initialize(IServiceProvider parentScope, IServiceProvider childScope)
        => initializer(parentScope, childScope);
}
