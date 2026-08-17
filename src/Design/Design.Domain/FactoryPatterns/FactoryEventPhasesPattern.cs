using Neatoo.RemoteFactory;

namespace Design.Domain.FactoryPatterns;

// =============================================================================
// DISPATCH PHASES — WHEN A [FactoryEventHandler<T>] HANDLER RUNS
// =============================================================================
//
// FactoryEventHandlerPattern.cs shows the default contract: handlers dispatch at
// IFactoryEvents.Raise time, inside the caller's transaction, observing the
// caller's staged (unflushed) state. That contract is the IMMEDIATE phase, and
// it is what a [FactoryEventHandler<T>] attribute with no argument declares.
//
// The attribute's DispatchPhase argument moves a handler to a later point:
//
//   Immediate   (default) At Raise, mid-transaction, staged state visible.
//                         Exceptions propagate to the raiser — atomic with the
//                         save. The right phase for handlers that must roll
//                         back with the aggregate.
//
//   AfterFlush            Queued at Raise; drained when the FACTORY METHOD
//                         calls IFactoryEventPhaseCoordinator.DrainAsync —
//                         typically between its flush and its commit. Handlers
//                         run in-transaction but see flushed state (database-
//                         generated keys, computed columns). Exceptions
//                         propagate to the drain call, so the transaction can
//                         still roll back.
//
//   AfterCommit           Queued at Raise; drained by the FRAMEWORK after the
//                         entry factory call completes successfully. No ambient
//                         transaction — an exception can no longer roll anything
//                         back, so it is logged (EventId 9003) and swallowed,
//                         and the remaining queued handlers still run. The right
//                         phase for read-only projections.
//
// THE CONTRACT, IN FOUR SENTENCES:
//
//   1. Failure semantics belong to the DRAIN POINT, not the phase: in-transaction
//      drains (Immediate dispatch, the coordinator's AfterFlush drain) propagate;
//      the post-completion drain logs and swallows.
//   2. Ordering is anchored PER DRAIN POINT: for events raised before a given
//      drain point, all Immediate handlers complete before any AfterFlush
//      handler, which complete before any AfterCommit handler. Code that raises
//      again after its own drain interleaves that later Immediate work between
//      drain points — there is no global barrier over the operation. Order
//      WITHIN a phase is unspecified.
//   3. If the entry factory call throws, queued AfterFlush/AfterCommit work is
//      discarded and never runs. (Immediate handlers already ran at Raise —
//      inside the transaction the consumer's rollback unwinds.)
//   4. An AfterFlush handler the factory method never drains does not vanish:
//      it runs at the AfterCommit point instead, fail-open, with a logged
//      warning (EventId 9007) naming the event type.
//
// DESIGN DECISION: RemoteFactory owns NO persistence concepts.
//
// Reasons:
// 1. The framework never flushes and never commits — it only exposes drain
//    points. The phase names describe consumer intent at those points; the
//    consumer's own persistence code (EF Core SaveChanges + transaction, Dapper,
//    anything) decides where "after flush" actually is by calling DrainAsync.
// 2. This keeps the factory pipeline persistence-agnostic: the same phased
//    handlers work against any storage — or none.
//
// DID NOT DO THIS: An IUnitOfWork/transaction abstraction inside RemoteFactory
// that flushes, drains, and commits on the consumer's behalf.
//
// Reasons:
// 1. Every ORM already has one; wrapping it would couple the factory pipeline
//    to a persistence model and force a lowest-common-denominator API.
// 2. A consumer who wants declarative drain calls can build a small transaction
//    helper that wraps its factory bodies — the coordinator is public precisely
//    so consumer abstractions can compose it.
//
// SHARP EDGES (each demonstrated or stated below):
//
// - Immediate handlers observe STAGED (unflushed) state. A projection that
//   queries the database at Immediate reads the world WITHOUT the aggregate's
//   pending writes. That is the motivating case for AfterFlush/AfterCommit.
// - One factory call per DI scope at a time. Queues and entry-call tracking are
//   per-scope, not per-flow: two concurrent factory calls in ONE scope share a
//   depth counter and queues, so a failed flow's discard and a surviving flow's
//   drain can interleave. Scopes are the framework's isolation unit — give
//   concurrent flows their own scopes (an ASP.NET Core request already is one).
// - Synchronous factory bodies should not raise phased events on context-bound
//   scopes (e.g. Logical mode inside a Blazor Server circuit): the completion
//   drain must block to avoid losing deferred work, and blocking under a
//   captured SynchronizationContext can deadlock if a drained handler awaits
//   without ConfigureAwait(false).
//
// =============================================================================

// -----------------------------------------------------------------------------
// RECORDING SEAM
// -----------------------------------------------------------------------------

/// <summary>
/// Ordered audit trail the phase demonstrations write to, so Design.Tests can
/// observe WHEN each handler ran relative to the factory method body — the
/// stand-in for "which transaction state would this handler have seen."
/// </summary>
public interface IPhaseAuditService
{
    void Record(Guid invoiceId, string entry);
    IReadOnlyList<string> EntriesFor(Guid invoiceId);
}

/// <summary>
/// Scoped implementation — each container scope gets its own trail, which is
/// exactly the isolation the per-scope phase queues have.
/// </summary>
public class PhaseAuditService : IPhaseAuditService
{
    private readonly object _gate = new();
    private readonly List<(Guid Id, string Entry)> _entries = [];

    public void Record(Guid invoiceId, string entry)
    {
        lock (_gate)
        {
            _entries.Add((invoiceId, entry));
        }
    }

    public IReadOnlyList<string> EntriesFor(Guid invoiceId)
    {
        lock (_gate)
        {
            return _entries.Where(e => e.Id == invoiceId).Select(e => e.Entry).ToList();
        }
    }
}

// =============================================================================
// SCENARIO 1: ONE EVENT, THREE PHASES — THE CONSUMER DRAIN PATTERN
// =============================================================================
//
// InvoiceFinalizedEvent has a handler at every phase. Invoice.Finalize raises it
// ONCE; each handler runs at its own drain point:
//
//   await events.Raise(...)          → "invoice-immediate"   (at Raise)
//   /* flush here */
//   await coordinator.DrainAsync(...)→ "invoice-flush"       (at the drain call)
//   audit.Record("invoice-method-done")
//   /* commit here */                                         (body returns)
//                                    → "invoice-commit"      (entry completion)
//
// "invoice-flush" landing BEFORE "invoice-method-done" is the proof the
// consumer's drain ran the handler inside the method body — the fail-open sweep
// (Scenario 3) can only produce the reverse.
// =============================================================================

/// <summary>Raised once when an invoice is finalized; handled at all three phases.</summary>
public record InvoiceFinalizedEvent(Guid InvoiceId, decimal Total) : FactoryEventBase;

/// <summary>
/// Immediate (no attribute argument — the default). Runs at <c>Raise</c>, inside
/// the caller's transaction, seeing staged state. A throw here aborts
/// <c>Finalize</c> itself — this handler is atomic with the save.
/// </summary>
[FactoryEventHandler<InvoiceFinalizedEvent>]
public static partial class InvoiceBalanceGuard
{
    internal static Task Enforce(
        InvoiceFinalizedEvent finalized,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(finalized.InvoiceId, "invoice-immediate");
        return Task.CompletedTask;
    }
}

/// <summary>
/// AfterFlush: queued at <c>Raise</c>, run at the factory method's
/// <c>DrainAsync</c> call. In a real consumer this is the projection that needs
/// database-generated state (identity keys, computed totals) while the
/// transaction is still open — it can still roll the operation back by throwing.
/// </summary>
[FactoryEventHandler<InvoiceFinalizedEvent>(DispatchPhase.AfterFlush)]
public static partial class InvoiceLedgerProjection
{
    internal static Task Project(
        InvoiceFinalizedEvent finalized,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(finalized.InvoiceId, "invoice-flush");
        return Task.CompletedTask;
    }
}

/// <summary>
/// AfterCommit: queued at <c>Raise</c>, run by the framework after the entry
/// call completes. No ambient transaction — a throw is logged (9003) and
/// swallowed. The right phase for read-only refresh work that must never fail
/// the save it follows.
/// </summary>
[FactoryEventHandler<InvoiceFinalizedEvent>(DispatchPhase.AfterCommit)]
public static partial class InvoiceSearchIndexRefresh
{
    internal static Task Refresh(
        InvoiceFinalizedEvent finalized,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(finalized.InvoiceId, "invoice-commit");
        return Task.CompletedTask;
    }
}

/// <summary>
/// The consumer drain pattern. <c>[Service] IFactoryEventPhaseCoordinator</c> is
/// method injection — server-only, like the persistence services it belongs
/// beside. The drain call sits where a real body's flush/commit seam is.
/// </summary>
/// <remarks>
/// DESIGN DECISION: The drain must run INSIDE the factory method body.
///
/// Reasons:
/// 1. The coordinator short-circuits (returns without draining) when no entry
///    factory call is active in the scope — a scope can be shared, so outside an
///    entry call, draining could run another flow's in-transaction work on this
///    caller's token. A transaction helper that wraps the factory call from
///    OUTSIDE therefore drains into nothing; the wrapper has to run inside the
///    body (or be composed into it).
/// 2. Only `AfterFlush` is consumer-drainable. `Immediate` is never queued, and
///    the `AfterCommit` drain point belongs to the framework — it runs only
///    after the entry call has succeeded. `DrainAsync` rejects other values.
/// </remarks>
[Factory]
public partial class Invoice
{
    public Guid Id { get; set; }
    public decimal Total { get; set; }

    [Create]
    [Remote]
    internal async Task Finalize(
        Guid id,
        decimal total,
        [Service] IFactoryEvents events,
        [Service] IFactoryEventPhaseCoordinator coordinator,
        [Service] IPhaseAuditService audit,
        CancellationToken ct)
    {
        Id = id;
        Total = total;

        // Immediate handlers run HERE, atomic with the staged writes.
        await events.Raise(new InvoiceFinalizedEvent(id, total), RaiseOptions.None, ct);

        // A real consumer flushes here (e.g. DbContext.SaveChangesAsync) —
        // database-generated state now exists, transaction still open.

        // AfterFlush handlers run HERE, in-transaction, seeing flushed state.
        // An exception propagates out of this call so the transaction rolls back.
        await coordinator.DrainAsync(DispatchPhase.AfterFlush, ct);

        audit.Record(id, "invoice-method-done");

        // A real consumer commits here. AfterCommit handlers run after this
        // method returns and the entry call completes.
    }
}

// =============================================================================
// SCENARIO 2: PER-DRAIN-POINT ORDERING, NOT A GLOBAL BARRIER
// =============================================================================
//
// Three event types raised in REVERSE phase order (commit, flush, immediate),
// so the observed sequence cannot be an echo of raise order. The second
// Immediate raise sits AFTER the drain: its handler runs at that later Raise —
// between the AfterFlush and AfterCommit drain points — which is the
// "per drain point, not a global barrier" sentence made observable.
//
// Expected: q-immediate, q-flush, q-immediate, q-method-done, q-commit
// =============================================================================

public record QuarterCloseImmediateEvent(Guid RunId) : FactoryEventBase;
public record QuarterCloseFlushEvent(Guid RunId) : FactoryEventBase;
public record QuarterCloseCommitEvent(Guid RunId) : FactoryEventBase;

[FactoryEventHandler<QuarterCloseImmediateEvent>]
public static partial class QuarterCloseImmediateHandler
{
    internal static Task Apply(
        QuarterCloseImmediateEvent quarterEvent,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(quarterEvent.RunId, "q-immediate");
        return Task.CompletedTask;
    }
}

[FactoryEventHandler<QuarterCloseFlushEvent>(DispatchPhase.AfterFlush)]
public static partial class QuarterCloseFlushHandler
{
    internal static Task Project(
        QuarterCloseFlushEvent quarterEvent,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(quarterEvent.RunId, "q-flush");
        return Task.CompletedTask;
    }
}

[FactoryEventHandler<QuarterCloseCommitEvent>(DispatchPhase.AfterCommit)]
public static partial class QuarterCloseCommitHandler
{
    internal static Task Project(
        QuarterCloseCommitEvent quarterEvent,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(quarterEvent.RunId, "q-commit");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Static factory command driving the ordering demonstration — phases work the
/// same from a static <c>[Execute]</c> as from a class factory method.
/// </summary>
[Factory]
public static partial class QuarterClose
{
    [Execute]
    [Remote]
    internal static async Task<Guid> _Run(
        Guid runId,
        [Service] IFactoryEvents events,
        [Service] IFactoryEventPhaseCoordinator coordinator,
        [Service] IPhaseAuditService audit,
        CancellationToken ct)
    {
        // Reverse phase order on purpose — phase, not raise order, decides.
        await events.Raise(new QuarterCloseCommitEvent(runId), RaiseOptions.None, ct);
        await events.Raise(new QuarterCloseFlushEvent(runId), RaiseOptions.None, ct);
        await events.Raise(new QuarterCloseImmediateEvent(runId), RaiseOptions.None, ct);

        await coordinator.DrainAsync(DispatchPhase.AfterFlush, ct);

        // Raised AFTER the drain: its Immediate handler runs at this Raise,
        // interleaving between the AfterFlush and AfterCommit drain points.
        await events.Raise(new QuarterCloseImmediateEvent(runId), RaiseOptions.None, ct);

        audit.Record(runId, "q-method-done");
        return runId;
    }
}

// =============================================================================
// SCENARIO 3: NEVER-DRAINED AfterFlush — FAIL-OPEN, WITH A WARNING
// =============================================================================
//
// InvoiceArchiver declares an AfterFlush handler but its factory method never
// calls DrainAsync. The handler still runs — at the AfterCommit point, after
// the body ("archive-method-done" first) — under post-completion swallow
// semantics it did not ask for, and the framework logs Warning 9007 naming the
// event type. Fail-open: forgetting the drain costs the intended transaction
// placement, never the handler.
// =============================================================================

public record InvoiceArchivedEvent(Guid InvoiceId) : FactoryEventBase;

[FactoryEventHandler<InvoiceArchivedEvent>(DispatchPhase.AfterFlush)]
public static partial class InvoiceArchiveProjection
{
    internal static Task Project(
        InvoiceArchivedEvent archived,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(archived.InvoiceId, "archive-flush");
        return Task.CompletedTask;
    }
}

/// <summary>Declares AfterFlush work but never drains — the mistake 9007 warns about.</summary>
[Factory]
public partial class InvoiceArchiver
{
    public Guid Id { get; set; }

    [Create]
    [Remote]
    internal async Task Archive(
        Guid id,
        [Service] IFactoryEvents events,
        [Service] IPhaseAuditService audit,
        CancellationToken ct)
    {
        Id = id;
        await events.Raise(new InvoiceArchivedEvent(id), RaiseOptions.None, ct);
        audit.Record(id, "archive-method-done");
        // No DrainAsync: the AfterFlush handler runs at the AfterCommit point.
    }
}

// =============================================================================
// SCENARIO 4: A FAILED ENTRY CALL DISCARDS QUEUED WORK
// =============================================================================
//
// _Record raises an event with handlers at all three phases, then either
// completes the consumer pattern (drain, marker) or throws before the drain.
//
// On the failure path the Immediate handler has already run at Raise — inside
// the transaction the consumer's rollback unwinds, which is exactly why
// Immediate is the atomic phase. The queued AfterFlush and AfterCommit work is
// DISCARDED (logged at Debug, 9006): deferred handlers never observe an
// operation that failed. Discarded, not merely skipped — a later successful
// call in the same scope drains only its own work; the failed call's queue
// does not ride along (the success path exists in this scenario precisely so
// a test can prove that from the audit trail).
// =============================================================================

public record PaymentRecordedEvent(Guid PaymentId) : FactoryEventBase;

[FactoryEventHandler<PaymentRecordedEvent>]
public static partial class PaymentImmediateGuard
{
    internal static Task Enforce(
        PaymentRecordedEvent payment,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(payment.PaymentId, "pay-immediate");
        return Task.CompletedTask;
    }
}

[FactoryEventHandler<PaymentRecordedEvent>(DispatchPhase.AfterFlush)]
public static partial class PaymentLedgerProjection
{
    internal static Task Project(
        PaymentRecordedEvent payment,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(payment.PaymentId, "pay-flush");
        return Task.CompletedTask;
    }
}

[FactoryEventHandler<PaymentRecordedEvent>(DispatchPhase.AfterCommit)]
public static partial class PaymentReceiptProjection
{
    internal static Task Project(
        PaymentRecordedEvent payment,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(payment.PaymentId, "pay-commit");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Raises phased work, then either completes the consumer pattern (accept) or
/// throws before the drain (reject) — the queues must be discarded on rejection.
/// </summary>
[Factory]
public static partial class PaymentIntake
{
    [Execute]
    [Remote]
    internal static async Task<Guid> _Record(
        Guid paymentId,
        bool reject,
        [Service] IFactoryEvents events,
        [Service] IFactoryEventPhaseCoordinator coordinator,
        [Service] IPhaseAuditService audit,
        CancellationToken ct)
    {
        audit.Record(paymentId, "pay-started");
        await events.Raise(new PaymentRecordedEvent(paymentId), RaiseOptions.None, ct);

        if (reject)
        {
            // Failing before the drain: the AfterFlush and AfterCommit queues are
            // discarded at entry-call exit. The Immediate handler already ran.
            throw new InvalidOperationException("Payment rejected after the raise — the entry call fails.");
        }

        await coordinator.DrainAsync(DispatchPhase.AfterFlush, ct);
        audit.Record(paymentId, "pay-done");
        return paymentId;
    }
}
