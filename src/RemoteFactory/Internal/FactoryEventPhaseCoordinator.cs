using Microsoft.Extensions.Logging;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Delegates <see cref="IFactoryEventPhaseCoordinator.DrainAsync"/> to the scope's
/// <see cref="IFactoryEventPhaseScheduler"/> with in-transaction semantics.
/// </summary>
/// <remarks>
/// <para>
/// This is infrastructure that supports the RemoteFactory runtime and is not subject to
/// the same compatibility standards as the rest of the public API. It may change or be
/// removed in any release. Deriving from it or calling it directly is supported in the
/// sense that nothing stops you — the <c>Internal</c> namespace is the warning, not a
/// wall — but do so knowing a future version may break you.
/// </para>
/// <para>
/// Constructed over the scope's existing scheduler instance — the DI registration
/// resolves <see cref="IFactoryEventPhaseScheduler"/> rather than constructing one, so
/// there is exactly one queue per scope. A registration that newed up its own scheduler
/// would drain an always-empty twin while the dispatcher queues into the real one; a
/// derived type replacing this registration must preserve that.
/// </para>
/// </remarks>
public class FactoryEventPhaseCoordinator : IFactoryEventPhaseCoordinator
{
    /// <summary>
    /// The scope's queue and drain primitive. Exposed so a derived type can add behavior
    /// around <see cref="DrainAsync"/> without re-resolving or duplicating it.
    /// </summary>
    protected IFactoryEventPhaseScheduler Scheduler { get; }

    private readonly ILogger? _logger;

    /// <summary>Creates a coordinator over the scope's existing scheduler.</summary>
    /// <param name="scheduler">The scope's queue and drain primitive.</param>
    /// <param name="loggerFactory">
    /// Optional, and optional in the same shape as
    /// <see cref="FactoryEventPhaseScheduler"/>'s: without it the coordinator still
    /// drains correctly and only loses the 9009 breadcrumb below.
    /// </param>
    public FactoryEventPhaseCoordinator(IFactoryEventPhaseScheduler scheduler, ILoggerFactory? loggerFactory = null)
    {
        this.Scheduler = scheduler;
        _logger = loggerFactory?.CreateLogger(NeatooLoggerCategories.Server);
    }

    /// <inheritdoc />
    public virtual Task DrainAsync(DispatchPhase phase, CancellationToken cancellationToken = default)
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
        if (!this.Scheduler.IsEntryCallActive)
        {
            // The short-circuit used to be silent, which left the consumer whose
            // transaction abstraction wraps the factory call from OUTSIDE with no
            // signal at all — their drain did nothing, and the only thing they ever
            // heard was a 9007 afterwards telling them to do what they had just done.
            // Debug, not Warning: a drain outside an entry call is also the correct
            // steady state for a scope with no factory work in flight.
            _logger?.FactoryEventPhaseDrainWithoutEntryCall(phase);
            return Task.CompletedTask;
        }

        return this.Scheduler.DrainAsync(DispatchPhase.AfterFlush, inTransaction: true, cancellationToken);
    }
}
