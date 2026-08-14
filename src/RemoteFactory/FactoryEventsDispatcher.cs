using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Runtime implementation of <see cref="IFactoryEvents"/> that dispatches to handlers
/// registered in <see cref="FactoryEventHandlerRegistry"/>.
/// Each handler was registered by a generated <c>FactoryServiceRegistrar</c> during DI setup.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DispatchPhase.Immediate"/> handlers — the default — run <b>sequentially</b>
/// in the caller's <see cref="IServiceProvider"/> so that a <c>DbContext</c> (or any other
/// scoped service) is shared between the factory method and its handlers. A handler
/// exception aborts the remaining handlers and propagates to the caller, so the caller can
/// let the transaction roll back.
/// </para>
/// <para>
/// Handlers registered at another phase are queued in the scope's
/// <see cref="IFactoryEventPhaseScheduler"/> instead, and run when that phase drains.
/// </para>
/// </remarks>
internal sealed class FactoryEventsDispatcher : IFactoryEvents
{
    private readonly IServiceProvider _sp;
    private readonly IFactoryEventCollector? _collector;
    private readonly IFactoryEventPhaseScheduler? _phaseQueue;

    public FactoryEventsDispatcher(IServiceProvider sp)
    {
        _sp = sp;
        _collector = sp.GetService<IFactoryEventCollector>();
        _phaseQueue = sp.GetService<IFactoryEventPhaseScheduler>();
    }

    public Task Raise<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T factoryEvent, RaiseOptions options = RaiseOptions.None, CancellationToken cancellationToken = default) where T : FactoryEventBase
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
        //
        // Phased handlers are queued instead, and run when their phase drains. Without a
        // queue in the scope they fall back to immediate dispatch rather than vanishing.
        foreach (var (phase, handler) in handlers)
        {
            if (phase != DispatchPhase.Immediate)
            {
                if (_phaseQueue != null)
                {
                    _phaseQueue.Enqueue(phase, (FactoryEventBase)factoryEvent, options, handler);
                    continue;
                }

                _sp.GetService<ILoggerFactory>()?
                    .CreateLogger(NeatooLoggerCategories.Server)
                    .FactoryEventPhaseNoQueueInScope(eventType.Name, phase);
            }

            await handler(_sp, factoryEvent, options, cancellationToken).ConfigureAwait(false);
        }
    }
}
