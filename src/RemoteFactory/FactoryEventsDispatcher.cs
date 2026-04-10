using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Runtime implementation of <see cref="IFactoryEvents"/> that dispatches to handlers
/// registered in <see cref="FactoryEventHandlerRegistry"/>.
/// Each handler was registered by a generated FactoryServiceRegistrar during DI setup.
/// </summary>
internal sealed class FactoryEventsDispatcher : IFactoryEvents
{
    private readonly IServiceProvider _sp;
    private readonly IFactoryEventCollector? _collector;

    public FactoryEventsDispatcher(IServiceProvider sp)
    {
        _sp = sp;
        _collector = sp.GetService<IFactoryEventCollector>();
    }

    public Task Raise<T>(T factoryEvent, RaiseOptions options = RaiseOptions.None) where T : FactoryEventBase
    {
        return DispatchToHandlers(typeof(T), factoryEvent!, options);
    }

    public Task RaiseUntyped(FactoryEventBase factoryEvent, RaiseOptions options = RaiseOptions.None)
    {
        return DispatchToHandlers(factoryEvent.GetType(), factoryEvent, options);
    }

    private Task DispatchToHandlers(Type eventType, object factoryEvent, RaiseOptions options)
    {
        // Capture for client relay unless ServerOnly is set
        if (_collector != null && !options.HasFlag(RaiseOptions.ServerOnly))
        {
            _collector.Collect((FactoryEventBase)factoryEvent);
        }

        var handlers = FactoryEventHandlerRegistry.GetHandlers(eventType);
        if (handlers == null || handlers.Count == 0)
            return Task.CompletedTask;

        var tasks = new List<Task>(handlers.Count);
        foreach (var handler in handlers)
        {
            tasks.Add(handler(_sp, factoryEvent, options));
        }

        if (options.HasFlag(RaiseOptions.ContinueOnFail))
            return WhenAllContinueOnFail(tasks);

        return Task.WhenAll(tasks);
    }

#pragma warning disable CA1031 // Intentional: aggregating all handler exceptions for ContinueOnFail semantics
    private static async Task WhenAllContinueOnFail(List<Task> tasks)
    {
        var exceptions = new List<Exception>();
        foreach (var task in tasks)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }
        if (exceptions.Count == 1)
            throw exceptions[0];
        if (exceptions.Count > 0)
            throw new AggregateException(exceptions);
    }
#pragma warning restore CA1031
}
