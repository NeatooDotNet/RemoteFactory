namespace Neatoo.RemoteFactory;

/// <summary>
/// Declares <i>when</i> a <c>[FactoryEventHandler&lt;T&gt;]</c> handler runs relative to the
/// factory operation that raised the event.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Immediate"/> handlers dispatch at <see cref="IFactoryEvents.Raise{T}"/> time,
/// inside whatever transaction the caller has open, observing the caller's staged
/// (unflushed) state. <see cref="AfterFlush"/> and <see cref="AfterCommit"/> handlers are
/// queued per DI scope at raise time and drained later; if the factory operation fails,
/// the queues are discarded and the handlers never run.
/// </para>
/// <para>
/// Cross-phase ordering is guaranteed: for one factory operation, all
/// <see cref="Immediate"/> handlers complete before any <see cref="AfterFlush"/> handler
/// runs, and all <see cref="AfterFlush"/> handlers complete before any
/// <see cref="AfterCommit"/> handler runs. Order <i>within</i> a phase is unspecified.
/// </para>
/// <para>
/// One carve-out: a handler running in a drain can itself raise an event whose handlers
/// belong to a phase whose drain point has already passed. That work runs in the current
/// drain — before any dispatch still queued for later phases — rather than being dropped.
/// So a handler for an earlier phase can observe a later phase's handler having already
/// run, when the earlier-phase work was created by that later-phase handler.
/// </para>
/// <para>
/// RemoteFactory owns no persistence concepts — it never flushes or commits. The phase
/// names describe consumer intent at the drain points the framework exposes.
/// </para>
/// </remarks>
public enum DispatchPhase
{
    /// <summary>
    /// Today's contract and the default: dispatched synchronously at
    /// <see cref="IFactoryEvents.Raise{T}"/> time in the caller's DI scope, sequential and
    /// awaited, mid-transaction with the caller's writes still staged. A handler exception
    /// aborts the remaining handlers and propagates so the caller's transaction can roll
    /// back. The right phase for handlers that must be atomic with the save.
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// Queued at raise time; drained when the consumer signals — typically between the
    /// outermost flush and the commit, via
    /// <c>IFactoryEventPhaseCoordinator.DrainAsync</c>. Handlers run in-transaction but
    /// see flushed state; an exception propagates to the drain caller so the transaction
    /// can still roll back. Handlers the consumer never drains run at the
    /// <see cref="AfterCommit"/> point instead, with a logged warning (fail-open).
    /// </summary>
    AfterFlush = 1,

    /// <summary>
    /// Queued at raise time; drained by the framework when the <i>entry</i> factory call
    /// completes successfully. Strictly, this means "after the entry factory method body
    /// returns" — it is "after commit" because consumers commit inside their factory
    /// bodies, which is the universal RemoteFactory pattern. Handlers run with no ambient
    /// transaction; a handler exception cannot roll anything back, so the framework logs
    /// it and continues (<see cref="OperationCanceledException"/> still propagates).
    /// Events raised by these handlers still join the same response's relay batch.
    /// The right phase for read-only projections.
    /// </summary>
    AfterCommit = 2,
}
