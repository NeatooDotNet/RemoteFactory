namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Delegates <see cref="IFactoryEventPhaseCoordinator.DrainAsync"/> to the scope's
/// <see cref="IFactoryEventPhaseScheduler"/> with in-transaction semantics.
/// </summary>
/// <remarks>
/// Constructed over the scope's existing scheduler instance — the DI registration
/// resolves <see cref="IFactoryEventPhaseScheduler"/> rather than constructing one, so
/// there is exactly one queue per scope. A registration that newed up its own scheduler
/// would drain an always-empty twin while the dispatcher queues into the real one.
/// </remarks>
internal sealed class FactoryEventPhaseCoordinator : IFactoryEventPhaseCoordinator
{
    private readonly IFactoryEventPhaseScheduler _scheduler;

    public FactoryEventPhaseCoordinator(IFactoryEventPhaseScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public Task DrainAsync(DispatchPhase phase, CancellationToken cancellationToken = default)
    {
        // Whitelist, not a blacklist: the scheduler's drain sweeps every phase at or
        // before the requested one, so an undefined value like (DispatchPhase)99 waved
        // through by a "!= AfterCommit" check would sweep the framework-owned
        // AfterCommit queue in-transaction — post-completion handlers running inside
        // the consumer's transaction with propagating exceptions.
        if (phase != DispatchPhase.AfterFlush)
        {
            throw new ArgumentOutOfRangeException(
                nameof(phase),
                phase,
                $"{DispatchPhase.AfterFlush} is the only consumer-drainable phase. " +
                $"{DispatchPhase.Immediate} handlers are never queued, and the " +
                $"{DispatchPhase.AfterCommit} drain point belongs to the framework (it runs " +
                "only after the entry call has succeeded).");
        }

        // Short-circuit outside an entry call rather than delegating: scopes can be
        // shared by concurrent flows, so "no entry call is active" is the only state in
        // which draining is provably not running someone else's in-transaction work on
        // this caller's token. When an entry call IS active, per-scope granularity is
        // the documented contract and the drain proceeds.
        if (!_scheduler.IsEntryCallActive)
        {
            return Task.CompletedTask;
        }

        return _scheduler.DrainAsync(DispatchPhase.AfterFlush, inTransaction: true, cancellationToken);
    }
}
