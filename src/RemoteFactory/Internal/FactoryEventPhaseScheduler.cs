using Microsoft.Extensions.Logging;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Scope-scoped store of factory-event dispatches deferred by their
/// <see cref="DispatchPhase"/>, plus the drain primitive that runs them and the
/// entry-call tracking that decides when the framework drains on its own.
/// </summary>
/// <remarks>
/// <para>
/// Public because generated factory code calls the entry-call members at the entry-call
/// boundary; it lives in the <c>Internal</c> namespace alongside
/// <see cref="IMakeRemoteDelegateRequest"/> and <see cref="ICorrelationContext"/>, which
/// are public for the same reason.
/// </para>
/// <para>
/// State is per DI scope and holds no persistence concepts. Entry-call tracking is
/// depth-aware: nested factory work (a save cascading into an insert, one factory
/// invoking another, the remote request handler wrapping a local method) increments
/// depth rather than starting a second entry, and only the outermost completion is
/// "the entry call completing." A failed entry call <b>clears</b> its deferred work at
/// the outermost exit — never drains it — so between entry calls the scheduler is
/// always empty. Scopes can be long-lived (Blazor Server circuits, Logical mode); the
/// clear is what keeps a failed call's work from riding into the next call's drain.
/// </para>
/// </remarks>
public interface IFactoryEventPhaseScheduler
{
    /// <summary>True when any phase has deferred dispatches waiting.</summary>
    bool HasPending { get; }

    /// <summary>
    /// True while an entry factory call is in flight in this scope — including for the
    /// duration of the entry-call drain itself, so work a drained handler raises still
    /// queues and joins the current drain.
    /// </summary>
    bool IsEntryCallActive { get; }

    /// <summary>Marks the start of a factory call. Nested calls increment depth.</summary>
    void BeginEntryCall();

    /// <summary>
    /// Marks the end of a factory call. At the outermost exit: a successful entry drains
    /// <see cref="DispatchPhase.AfterCommit"/> (which sweeps earlier phases first) with
    /// no cancellation token — the entry call already succeeded, so nothing may abort its
    /// post-completion work — while a failed entry discards all deferred dispatches
    /// without running any. Nested exits only decrement depth.
    /// </summary>
    /// <param name="success">
    /// Whether the factory call completed successfully. Callers pass
    /// <see langword="false"/> from failure paths only; this method never throws when
    /// <paramref name="success"/> is <see langword="false"/>.
    /// </param>
    Task EndEntryCallAsync(bool success);

    /// <summary>Defers a handler dispatch until <paramref name="phase"/> drains.</summary>
    void Enqueue(DispatchPhase phase, FactoryEventBase factoryEvent, RaiseOptions options, Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handler);

    /// <summary>
    /// Defers a handler dispatch until <paramref name="phase"/> drains, optionally
    /// coalescing it into an identical pending dispatch.
    /// </summary>
    /// <remarks>
    /// With <paramref name="coalesce"/> set, an already-pending dispatch with the same
    /// handler delegate, an <see cref="object.Equals(object)"/>-equal event, and the same
    /// <see cref="RaiseOptions"/> absorbs this one: the scope holds at most one pending
    /// dispatch per such identity, the survivor keeps its (earliest) queue position, and
    /// the collapsed state is what <see cref="HasPending"/> and the drain/discard counts
    /// observe. The merge preserves the fail-open obligation — the survivor warns at the
    /// post-completion sweep if <i>any</i> absorbed raise would have. Identity looks only
    /// at pending work: a dispatch already taken by a running drain is history, and a
    /// raise arriving after it collapses with nothing. An overload rather than a
    /// parameter so existing callers (and their pins) stand unchanged — noting that a
    /// new interface member is still a break for third-party implementors, which the
    /// <c>Internal</c> namespace's may-change-in-any-release contract covers.
    /// </remarks>
    void Enqueue(DispatchPhase phase, FactoryEventBase factoryEvent, RaiseOptions options, Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handler, bool coalesce);

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
    /// therefore logged and swallowed per handler — including a handler-internal
    /// <see cref="OperationCanceledException"/>. Only genuine cooperative cancellation
    /// (an <see cref="OperationCanceledException"/> while
    /// <paramref name="cancellationToken"/> is cancelled) propagates, abandoning the rest
    /// of the drain; those dispatches stay queued, to be discarded by the exit clear if
    /// the cancellation fails the entry call, or swept at the
    /// <see cref="DispatchPhase.AfterCommit"/> point if it does not.
    /// </param>
    /// <param name="cancellationToken">Token passed to the drained handlers.</param>
    Task DrainAsync(DispatchPhase phase, bool inTransaction, CancellationToken cancellationToken = default);
}

/// <summary>
/// The per-scope queue and drain primitive behind <see cref="IFactoryEventPhaseScheduler"/>.
/// </summary>
/// <remarks>
/// Infrastructure supporting the RemoteFactory runtime, not subject to the same
/// compatibility standards as the rest of the public API — it may change or be removed in
/// any release. The <c>Internal</c> namespace is the warning, not a wall: nothing here is
/// closed off, and extending it is your call to make with that risk understood.
/// <para>
/// Members are deliberately non-virtual for now. The queueing, entry-depth, and drain
/// semantics are a single interlocking contract — pinned by
/// <c>FactoryEventPhaseSchedulerTests</c> and <c>FactoryEntryCallTests</c> — and opening
/// individual members to override would publish seams that cannot be honored
/// independently of each other. Replace the whole implementation through
/// <see cref="IFactoryEventPhaseScheduler"/> if you need different behavior; the DI
/// registration is <c>TryAddScoped</c>, so registering your own first wins.
/// </para>
/// </remarks>
public class FactoryEventPhaseScheduler : IFactoryEventPhaseScheduler
{
    private readonly IServiceProvider _sp;
    private readonly ILogger? _logger;

    // List rather than Queue: the coalescing warn-merge rewrites one pending entry's
    // EnqueuedMidDrain bit in place, which the readonly record struct element cannot
    // express inside a Queue<T> (a mutable reference-typed element could; the struct
    // was kept). Dequeue is a front-removal — O(n) per dequeue, paid by every path
    // including non-coalescing ones — accepted on the ASSUMPTION that pending queues
    // live for one entry call and stay small; nothing enforces that, and a bulk save
    // raising thousands of deferred events would feel it (recorded, PHASE-007). The
    // coalescing identity scan (opted-in enqueues only) is O(pending) under _gate.
    private readonly Dictionary<DispatchPhase, List<QueuedDispatch>> _deferred = new();

    // Guards _deferred, _entryDepth, and _activeDrains. Scopes can be shared by
    // concurrent flows (Blazor Server circuits, Logical mode, a reused test scope);
    // handlers are invoked outside the lock. One consumer call IS made under it: the
    // coalescing identity scan invokes the event's Equals (custom overrides included)
    // per pending entry — keep event Equals cheap and non-re-entrant; a re-entrant
    // Equals that touches this scheduler mutates the queue mid-scan.
    private readonly object _gate = new();
    private int _entryDepth;

    // Nonzero while any DrainAsync loop is in flight in this scope. Enqueue stamps each
    // dispatch with it, which is what lets the post-completion sweep tell "the consumer
    // never drained this" (warn) from "a handler created this mid-drain, after every
    // drain point it could have used had passed" (the documented carve-out, silent).
    // A counter rather than a bool: a consumer AfterFlush drain can run while the entry
    // drain is active (a swept handler calling the coordinator), and the mark must not
    // drop early.
    private int _activeDrains;

    public FactoryEventPhaseScheduler(IServiceProvider sp, ILoggerFactory? loggerFactory = null)
    {
        _sp = sp;
        _logger = loggerFactory?.CreateLogger(NeatooLoggerCategories.Server);
    }

    private readonly record struct QueuedDispatch(
        FactoryEventBase Event,
        RaiseOptions Options,
        Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> Handler,
        bool EnqueuedMidDrain);

    public bool HasPending
    {
        get
        {
            lock (_gate)
            {
                return _deferred.Any(q => q.Value.Count > 0);
            }
        }
    }

    public bool IsEntryCallActive
    {
        get
        {
            lock (_gate)
            {
                return _entryDepth > 0;
            }
        }
    }

    public void BeginEntryCall()
    {
        lock (_gate)
        {
            _entryDepth++;
        }
    }

    public async Task EndEntryCallAsync(bool success)
    {
        if (!success)
        {
            ClearAtExit();
            return;
        }

        // Single lock acquisition: a check-then-decrement across two acquisitions opens
        // a window (interleaved with a concurrent flow's failure exit) where depth hits
        // zero having neither drained nor cleared (code review C1).
        lock (_gate)
        {
            if (_entryDepth == 0)
            {
                throw new InvalidOperationException(
                    $"{nameof(EndEntryCallAsync)} called without a matching {nameof(BeginEntryCall)}.");
            }

            if (_entryDepth > 1)
            {
                _entryDepth--;
                return;
            }
        }

        // The entry stays active (depth 1) for the duration of the drain, so an event a
        // drained handler raises still queues through the dispatcher and joins this drain
        // via drain-until-empty. No token: the entry call already succeeded, so nothing
        // may abort its post-completion work.
        try
        {
            await DrainAsync(DispatchPhase.AfterCommit, inTransaction: false, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            // Depth release. With CancellationToken.None the drain's cancellation filter
            // is constant-false and every handler exception is swallowed, so this drain
            // no longer throws — the finally stays as armor for that invariant rather
            // than for a known path, preserving "between entry calls the scheduler is
            // empty" even if the drain's contract drifts.
            ClearAtExit();
        }
    }

    public void Enqueue(DispatchPhase phase, FactoryEventBase factoryEvent, RaiseOptions options, Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handler)
        => Enqueue(phase, factoryEvent, options, handler, coalesce: false);

    public void Enqueue(DispatchPhase phase, FactoryEventBase factoryEvent, RaiseOptions options, Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handler, bool coalesce)
    {
        ArgumentNullException.ThrowIfNull(factoryEvent);
        ArgumentNullException.ThrowIfNull(handler);

        var coalesced = false;

        lock (_gate)
        {
            if (!_deferred.TryGetValue(phase, out var queue))
            {
                queue = new List<QueuedDispatch>();
                _deferred[phase] = queue;
            }

            var enqueuedMidDrain = _activeDrains > 0;

            if (coalesce)
            {
                // Identity: same handler delegate (one delegate instance per surviving
                // registration, so reference equality is "same registration"), an
                // Equals-equal event, and the same options. Only PENDING work is a
                // collapse target — a dispatch a running drain has already taken is gone
                // from this list, so a raise after it starts a fresh pending dispatch.
                for (var i = 0; i < queue.Count; i++)
                {
                    var existing = queue[i];
                    if (ReferenceEquals(existing.Handler, handler)
                        && existing.Options == options
                        && existing.Event.Equals(factoryEvent))
                    {
                        // Warn-preserving merge: the survivor warns at the fail-open
                        // sweep if ANY absorbed raise would have — the bit only ever
                        // moves toward "would warn" (false). A latest-bit-wins merge
                        // would let a mid-drain re-raise silently erase the 9007 a
                        // consumer who never drained is owed.
                        if (existing.EnqueuedMidDrain && !enqueuedMidDrain)
                        {
                            queue[i] = existing with { EnqueuedMidDrain = false };
                        }

                        coalesced = true;
                        break;
                    }
                }
            }

            if (!coalesced)
            {
                queue.Add(new QueuedDispatch(factoryEvent, options, handler, enqueuedMidDrain));
            }
        }

        if (_logger?.IsEnabled(LogLevel.Debug) == true)
        {
            if (coalesced)
            {
                // 9008 instead of a second 9001: the raise produced no new pending
                // dispatch, and the queued/drained/discarded counts stay collapsed.
                _logger.FactoryEventPhaseCoalesced(factoryEvent.GetType().Name, phase);
            }
            else
            {
                _logger.FactoryEventPhaseQueued(factoryEvent.GetType().Name, phase);
            }
        }
    }

    public async Task DrainAsync(DispatchPhase phase, bool inTransaction, CancellationToken cancellationToken = default)
    {
        var drained = 0;

        lock (_gate)
        {
            _activeDrains++;
        }

        try
        {
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

                // Fail-open announcement (AC-5): an AfterFlush dispatch reaching a
                // post-completion drain was never drained by the consumer — whether they
                // wired no drain at all or raised this after their drain had run — UNLESS
                // it was created mid-drain, where every drain point it could have used had
                // already passed (the documented carve-out). Logged before invoking: the
                // warning is about when the work ran, not whether it succeeded.
                if (dispatchPhase == DispatchPhase.AfterFlush && !dispatch.EnqueuedMidDrain)
                {
                    _logger?.FactoryEventPhaseNeverDrained(dispatch.Event.GetType().Name);
                }

#pragma warning disable CA1031 // Post-completion handler exceptions cannot roll anything back; swallowing is the contract.
                try
                {
                    await dispatch.Handler(_sp, dispatch.Event, dispatch.Options, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Genuine cooperative cancellation: the drain's own token fired, so the
                    // caller asked to stop — propagate, abandoning the rest of the queue
                    // (the entry exit's clear discards it). The framework's entry drain
                    // passes CancellationToken.None, so this filter is constant-false
                    // there by design: nothing may abort a succeeded call's
                    // post-completion work, and a handler-internal OCE falls through to
                    // the swallow below like any other post-completion failure (PHASE-004,
                    // restating the earlier "OCE still propagates" contract).
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.FactoryEventPhaseHandlerFailed(dispatchPhase, dispatch.Event.GetType().Name, ex);
                }
#pragma warning restore CA1031
            }
        }
        finally
        {
            lock (_gate)
            {
                _activeDrains--;
            }
        }

        if (drained > 0 && _logger?.IsEnabled(LogLevel.Debug) == true)
        {
            _logger.FactoryEventPhaseDrained(drained, phase);
        }
    }

    /// <summary>
    /// Outermost-exit cleanup shared by the failure path and the post-drain release:
    /// decrements depth (tolerantly — failure paths run inside catch blocks and must
    /// never throw) and, at depth zero, discards whatever is still deferred.
    /// </summary>
    private void ClearAtExit()
    {
        int discarded = 0;
        lock (_gate)
        {
            if (_entryDepth > 0)
            {
                _entryDepth--;
            }

            if (_entryDepth == 0)
            {
                foreach (var queue in _deferred.Values)
                {
                    discarded += queue.Count;
                    queue.Clear();
                }
            }
        }

        if (discarded > 0)
        {
            _logger?.FactoryEventPhaseDiscardedAtExit(discarded);
        }
    }

    /// <summary>
    /// Takes the next dispatch from the earliest non-empty phase at or before
    /// <paramref name="through"/>, so cross-phase ordering holds even for work a handler
    /// enqueues mid-drain.
    /// </summary>
    private bool TryDequeueThrough(DispatchPhase through, out QueuedDispatch dispatch, out DispatchPhase phase)
    {
        lock (_gate)
        {
            foreach (var candidate in _deferred.Keys.Where(p => p <= through).OrderBy(p => p))
            {
                var queue = _deferred[candidate];
                if (queue.Count > 0)
                {
                    dispatch = queue[0];
                    queue.RemoveAt(0);
                    phase = candidate;
                    return true;
                }
            }

            dispatch = default;
            phase = through;
            return false;
        }
    }
}
