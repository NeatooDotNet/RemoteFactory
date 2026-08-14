# AfterFlush: Consumer-Signaled Drain via IFactoryEventPhaseCoordinator

**Plan #:** 004
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-14
**Plan-review opt-in:** Yes (new public interface; contract with consumer transaction abstractions)
**Code-review opt-in:** Yes (behavior-changing)

---

## Scope

Expose the public `IFactoryEventPhaseCoordinator` with `DrainAsync(DispatchPhase, ct)` so a
consumer's transaction abstraction can drain the `AfterFlush` queue between its outermost
flush and commit, and implement the fail-open fallback: `AfterFlush` handlers never drained
by the consumer run at the `AfterCommit` point with a logged warning. Includes the
cross-phase ordering guarantee tests (all `Immediate` before any `AfterFlush` before any
`AfterCommit`). This plan does NOT add transaction awareness to the framework — the
coordinator is a drain trigger, nothing more.

---

## Inherited from PHASE-001 (recorded at its Step 5 gate)

- **The fail-open sweep is already implemented and test-pinned.** `DrainAsync(phase, …)`
  drains the requested phase *and every earlier one*, earliest first, so `AfterFlush`
  handlers a consumer never drained are swept up at the `AfterCommit` point under that
  drain point's swallow semantics (`FactoryEventPhaseSchedulerTests.DrainAsync_SweepsAnEarlierPhaseTheConsumerNeverDrained`).
  What is missing is only the **logged warning** the todo's AC-5 requires — and the
  discriminator is already plumbed: `TryDequeueThrough` returns the phase each dispatch was
  queued at (code review C3). Wire the warning; do not re-plumb the sweep.
- `IFactoryEventPhaseCoordinator.DrainAsync(AfterFlush)` should call through to the
  scheduler with `inTransaction: true` so handler exceptions propagate and the consumer's
  transaction can still roll back.

*(Stub — Intent, Alignment, remaining Constraints, Steps, Acceptance filled at Step 2.)*
