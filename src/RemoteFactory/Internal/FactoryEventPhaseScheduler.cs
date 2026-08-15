using Microsoft.Extensions.Logging;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Scope-scoped store of factory-event dispatches deferred by their
/// <see cref="DispatchPhase"/>, plus the drain primitive that runs them.
/// </summary>
/// <remarks>
/// <para>
/// Public because generated factory code calls <see cref="DrainAsync"/> at the entry-call
/// boundary; it lives in the <c>Internal</c> namespace alongside
/// <see cref="IMakeRemoteDelegateRequest"/> and <see cref="ICorrelationContext"/>, which
/// are public for the same reason.
/// </para>
/// <para>
/// State is per DI scope and holds no persistence concepts. A scope whose factory
/// operation fails is simply never drained — that is what makes rollback-discard
/// structural rather than a rule anyone has to remember.
/// </para>
/// </remarks>
public interface IFactoryEventPhaseScheduler
{
    /// <summary>True when any phase has deferred dispatches waiting.</summary>
    bool HasPending { get; }

    /// <summary>Defers a handler dispatch until <paramref name="phase"/> drains.</summary>
    void Enqueue(DispatchPhase phase, FactoryEventBase factoryEvent, RaiseOptions options, Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handler);

    /// <summary>
    /// Runs the deferred dispatches for <paramref name="phase"/> <b>and every earlier
    /// phase</b>, earliest first, until none are left.
    /// </summary>
    /// <remarks>
    /// Draining earlier phases too is what makes the drain total: a handler running here
    /// can raise an event whose handlers sit in this phase or in one whose drain point has
    /// already passed, and a consumer may never drain <see cref="DispatchPhase.AfterFlush"/>
    /// at all. Either way the work joins this drain rather than being silently dropped.
    /// Later phases than <paramref name="phase"/> are left alone.
    /// </remarks>
    /// <param name="phase">The latest phase to drain; earlier phases drain first.</param>
    /// <param name="inTransaction">
    /// Declares the <i>drain point</i>, which is what failure semantics key off — not the
    /// phase. <see langword="true"/> when the caller still has a transaction open, so a
    /// handler exception propagates and the caller can roll back. <see langword="false"/>
    /// for a post-completion drain, where a throw can no longer roll anything back and is
    /// therefore logged and swallowed per handler;
    /// <see cref="OperationCanceledException"/> still propagates.
    /// </param>
    /// <param name="cancellationToken">Token passed to the drained handlers.</param>
    Task DrainAsync(DispatchPhase phase, bool inTransaction, CancellationToken cancellationToken = default);
}

internal sealed class FactoryEventPhaseScheduler : IFactoryEventPhaseScheduler
{
    private readonly IServiceProvider _sp;
    private readonly ILogger? _logger;
    private readonly Dictionary<DispatchPhase, Queue<QueuedDispatch>> _deferred = new();

    public FactoryEventPhaseScheduler(IServiceProvider sp, ILoggerFactory? loggerFactory = null)
    {
        _sp = sp;
        _logger = loggerFactory?.CreateLogger(NeatooLoggerCategories.Server);
    }

    private readonly record struct QueuedDispatch(
        FactoryEventBase Event,
        RaiseOptions Options,
        Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> Handler);

    public bool HasPending => _deferred.Any(q => q.Value.Count > 0);

    public void Enqueue(DispatchPhase phase, FactoryEventBase factoryEvent, RaiseOptions options, Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(factoryEvent);
        ArgumentNullException.ThrowIfNull(handler);

        if (!_deferred.TryGetValue(phase, out var queue))
        {
            queue = new Queue<QueuedDispatch>();
            _deferred[phase] = queue;
        }

        queue.Enqueue(new QueuedDispatch(factoryEvent, options, handler));

        if (_logger?.IsEnabled(LogLevel.Debug) == true)
        {
            _logger.FactoryEventPhaseQueued(factoryEvent.GetType().Name, phase);
        }
    }

    public async Task DrainAsync(DispatchPhase phase, bool inTransaction, CancellationToken cancellationToken = default)
    {
        var drained = 0;

        // Dequeue one at a time rather than snapshotting: a handler running here may raise
        // an event whose handlers are deferred, and those dispatches belong to this drain.
        // TryDequeueThrough also picks up earlier phases, which a handler can still enqueue
        // into after that phase's own drain point has passed — without this they would sit
        // in a scope nobody drains again. An unterminated raise loop is the consumer's bug,
        // exactly as it is for today's synchronous chained raises.
        while (TryDequeueThrough(phase, out var dispatch, out var dispatchPhase))
        {
            drained++;

            if (inTransaction)
            {
                await dispatch.Handler(_sp, dispatch.Event, dispatch.Options, cancellationToken).ConfigureAwait(false);
                continue;
            }

#pragma warning disable CA1031 // Post-completion handler exceptions cannot roll anything back; swallowing is the contract.
            try
            {
                await dispatch.Handler(_sp, dispatch.Event, dispatch.Options, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.FactoryEventPhaseHandlerFailed(dispatchPhase, dispatch.Event.GetType().Name, ex);
            }
#pragma warning restore CA1031
        }

        if (drained > 0 && _logger?.IsEnabled(LogLevel.Debug) == true)
        {
            _logger.FactoryEventPhaseDrained(drained, phase);
        }
    }

    /// <summary>
    /// Takes the next dispatch from the earliest non-empty phase at or before
    /// <paramref name="through"/>, so cross-phase ordering holds even for work a handler
    /// enqueues mid-drain.
    /// </summary>
    private bool TryDequeueThrough(DispatchPhase through, out QueuedDispatch dispatch, out DispatchPhase phase)
    {
        foreach (var candidate in _deferred.Keys.Where(p => p <= through).OrderBy(p => p))
        {
            var queue = _deferred[candidate];
            if (queue.Count > 0)
            {
                dispatch = queue.Dequeue();
                phase = candidate;
                return true;
            }
        }

        dispatch = default;
        phase = through;
        return false;
    }
}
