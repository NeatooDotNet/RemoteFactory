namespace Neatoo.RemoteFactory;

/// <summary>
/// Consumer-facing trigger for the drain points a consumer owns. Today that is exactly
/// one: <see cref="DispatchPhase.AfterFlush"/>, drained between the consumer's outermost
/// flush and its commit so handlers observe flushed state while the transaction can
/// still roll back.
/// </summary>
/// <remarks>
/// <para>
/// RemoteFactory owns no persistence concepts — it never flushes or commits. This
/// interface is how a consumer's own transaction code (typically inside a factory
/// method, or an abstraction wrapping one) tells the framework "my flush point has
/// passed; run the <see cref="DispatchPhase.AfterFlush"/> work now." Inject it with
/// <c>[Service]</c> on the factory method — it is a server-only service, absent from
/// Remote-mode (client) containers, where no handlers dispatch.
/// </para>
/// <para>
/// Failure semantics come from the drain point: this is an in-transaction drain, so a
/// handler exception (including a cancellation) propagates to the caller and aborts the
/// rest of the drain — the consumer's transaction can still roll back, and the entry
/// call's failure exit discards whatever remained queued.
/// </para>
/// <para>
/// <see cref="DispatchPhase.AfterFlush"/> handlers never drained through this interface
/// are not lost: the framework sweeps them at the <see cref="DispatchPhase.AfterCommit"/>
/// point with a logged warning (fail-open) — they run, but after the transaction, under
/// post-completion semantics they did not ask for.
/// </para>
/// </remarks>
public interface IFactoryEventPhaseCoordinator
{
    /// <summary>
    /// Runs the deferred dispatches queued at <paramref name="phase"/> for the factory
    /// call currently in flight in this scope, including dispatches that handlers
    /// running in this drain enqueue at or before <paramref name="phase"/>, until none
    /// are left.
    /// </summary>
    /// <param name="phase">
    /// The consumer-owned phase to drain. <see cref="DispatchPhase.AfterFlush"/> is the
    /// only accepted value: <see cref="DispatchPhase.Immediate"/> never queues and
    /// <see cref="DispatchPhase.AfterCommit"/>'s drain point belongs to the framework —
    /// draining it early would run post-completion handlers inside the consumer's
    /// transaction and break the "never run if the entry call fails" guarantee. Every
    /// other value, defined or not, is rejected.
    /// </param>
    /// <param name="cancellationToken">
    /// The consumer's token, passed to the drained handlers. Cancellation propagates to
    /// this call and abandons the rest of the drain; abandoned dispatches stay queued
    /// and are discarded at the entry call's exit.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="phase"/> is anything other than
    /// <see cref="DispatchPhase.AfterFlush"/>.
    /// </exception>
    /// <remarks>
    /// Outside an entry factory call this method returns without draining anything.
    /// That is deliberate, not just harmless: scopes can be shared by concurrent flows
    /// (the framework's documented per-scope granularity), so "no entry call of mine is
    /// active" can coincide with another flow's live entry call — and an unconditional
    /// drain would run that flow's in-transaction work on the wrong token. Events raised
    /// outside any entry call dispatch immediately and never queue, so there is nothing
    /// of the caller's to drain.
    /// </remarks>
    Task DrainAsync(DispatchPhase phase, CancellationToken cancellationToken = default);
}
