using Microsoft.Extensions.DependencyInjection;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Built-in <see cref="IEventScopeInitializer"/> that propagates <see cref="ICorrelationContext.CorrelationId"/>
/// from the request scope to the event handler's scope.
/// </summary>
internal sealed class CorrelationContextScopeInitializer : IEventScopeInitializer
{
    public void Initialize(IServiceProvider parentScope, IServiceProvider childScope)
    {
        var parentCorrelation = parentScope.GetService<ICorrelationContext>();
        var childCorrelation = childScope.GetService<ICorrelationContext>();
        if (parentCorrelation?.CorrelationId != null && childCorrelation != null)
        {
            childCorrelation.CorrelationId = parentCorrelation.CorrelationId;
        }
    }
}
