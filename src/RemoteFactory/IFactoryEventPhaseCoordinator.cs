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
    /// Runs the dispatches deferred at <paramref name="phase"/> <b>in this DI scope</b>,
    /// including dispatches that handlers running in this drain enqueue at or before
    /// <paramref name="phase"/>, until none are left.
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
    /// this call and abandons the rest of the drain, leaving those dispatches queued.
    /// What happens to them next follows the entry call: if the cancellation fails it —
    /// the usual case, since it propagates out of the factory method — the exit clear
    /// discards them; if the consumer swallows it and the call still succeeds, they are
    /// swept at the <see cref="DispatchPhase.AfterCommit"/> point with the fail-open
    /// warning, like any other undrained work.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="phase"/> is anything other than
    /// <see cref="DispatchPhase.AfterFlush"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Scope-wide, not call-wide: the scope is the framework's isolation unit. If two
    /// factory calls share one scope — concurrent flows on a Blazor Server circuit, in
    /// Logical mode, or in a reused test scope — this drain runs the other flow's
    /// deferred work too, on this caller's token and inside this caller's transaction.
    /// That is the same per-scope granularity that already governs entry-call tracking
    /// and every scoped service; the guidance is one factory call per scope at a time.
    /// </para>
    /// <para>
    /// Outside an entry factory call this method returns without draining anything.
    /// That is deliberate, not just harmless: by the same per-scope granularity, "no
    /// entry call of mine is active" can coincide with another flow's live entry call —
    /// and an unconditional drain would run that flow's in-transaction work on the wrong
    /// token. Events raised outside any entry call dispatch immediately and never queue,
    /// so there is nothing of the caller's to drain. Note the consequence for a
    /// transaction abstraction that wraps the factory call from <i>outside</i>: by the
    /// time it drains, the entry call has closed and its work has already been swept —
    /// the drain must run inside the factory method body.
    /// </para>
    /// </remarks>
    Task DrainAsync(DispatchPhase phase, CancellationToken cancellationToken = default);
}
