using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Runtime implementation of <see cref="IFactoryEvents"/> that dispatches to handlers
/// registered in <see cref="FactoryEventHandlerRegistry"/>.
/// Each handler was registered by a generated <c>FactoryServiceRegistrar</c> during DI setup.
/// </summary>
/// <remarks>
/// Handlers run <b>sequentially</b> in the caller's <see cref="IServiceProvider"/> so that
/// a <c>DbContext</c> (or any other scoped service) is shared between the factory method
/// and its handlers. A handler exception aborts the remaining handlers and propagates to
/// the caller, so the caller can let the transaction roll back.
/// </remarks>
internal sealed class FactoryEventsDispatcher : IFactoryEvents
{
    private readonly IServiceProvider _sp;
    private readonly IFactoryEventCollector? _collector;

    public FactoryEventsDispatcher(IServiceProvider sp)
    {
        _sp = sp;
        _collector = sp.GetService<IFactoryEventCollector>();
    }

    public Task Raise<T>(T factoryEvent, RaiseOptions options = RaiseOptions.None, CancellationToken cancellationToken = default) where T : FactoryEventBase
    {
        return DispatchToHandlers(typeof(T), factoryEvent!, options, cancellationToken);
    }

    public Task RaiseUntyped(FactoryEventBase factoryEvent, RaiseOptions options = RaiseOptions.None, CancellationToken cancellationToken = default)
    {
        return DispatchToHandlers(factoryEvent.GetType(), factoryEvent, options, cancellationToken);
    }

    private async Task DispatchToHandlers(Type eventType, object factoryEvent, RaiseOptions options, CancellationToken cancellationToken)
    {
        // Capture for client relay unless ServerOnly is set
        if (_collector != null && !options.HasFlag(RaiseOptions.ServerOnly))
        {
            _collector.Collect((FactoryEventBase)factoryEvent);
        }

        var handlers = FactoryEventHandlerRegistry.GetHandlers(eventType);
        if (handlers == null || handlers.Count == 0)
            return;

        // Sequential dispatch in the caller's scope. Handler order is unspecified —
        // callers must not depend on it. Exceptions propagate immediately and abort
        // the remaining handlers so the caller's transaction can roll back.
        foreach (var handler in handlers)
        {
            await handler(_sp, factoryEvent, options, cancellationToken).ConfigureAwait(false);
        }
    }
}
