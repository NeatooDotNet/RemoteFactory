# AfterFlush: Consumer-Signaled Drain via IFactoryEventPhaseCoordinator

**Plan #:** 004
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-15
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

---

## Inherited from PHASE-003 (recorded at its Step 5 gates)

- **Open question this plan owns (code review C2):** a drained handler's
  `OperationCanceledException` at the *AfterCommit entry drain* propagates and fails a
  call that already succeeded — chartered by the todo's AC ("OCE still propagates") but
  in tension with the no-token entry-drain policy. Decide here, as the owner of the
  consumer-facing drain surface, whether post-completion drains should swallow OCE too
  (and if so, restate the AC as a planned amendment).
- The entry drain passes `CancellationToken.None`; the coordinator's `AfterFlush` drain
  is in-transaction and consumer-invoked, so it takes the consumer's token — the two
  drain points deliberately differ.

---

## Inherited from PHASE-002 (recorded at its Step 5 gates)

- **`AfterFlush` is already consumer-reachable, and already fail-opens — silently.** PHASE-002
  made the attribute's phase argument real, so `[FactoryEventHandler<T>(DispatchPhase.AfterFlush)]`
  works today and the scheduler's sweep drains it at the AfterCommit entry point. What is
  missing is only AC-5's logged warning. Acceptance here must cover an **attribute-declared**
  AfterFlush handler, not only one registered through the registry's 3-arg overload — the
  consumer-facing path is otherwise untested end to end.
- NF0504 (Warning) now fires when one class declares the same event type twice, and the
  duplicate's entry is skipped. If this plan widens the registry's `(event type, handler class
  type)` dedupe key for any reason, that diagnostic's premise changes and it must be revisited.

---

## Intent

- A consumer's transaction abstraction gets a first-class, public drain trigger for
  `AfterFlush` work — the last missing piece of the phase feature's consumer surface. The
  consumer pattern is a factory method (or the abstraction wrapping it) that flushes,
  drains via the coordinator, then commits; handlers see flushed state while the
  transaction can still roll back.
- The fail-open path becomes honest. Since PHASE-002, never-drained `AfterFlush` work is
  silently swept at the `AfterCommit` point; after this plan the sweep announces itself
  with a dedicated warning naming the event type. The warning covers every `AfterFlush`
  dispatch that reaches the post-completion sweep — whether the consumer wired no drain at
  all or raised the work after their drain had already run; both are AC-5's letter
  ("never drained by the consumer") and both are actionable from the log line. The one
  silent case is the documented carve-out: work *created during* that sweep by a
  later-phase handler had no drain point left to miss.
- This plan settles the PHASE-003 C2 question it owns. **Intended decision:** at
  post-completion drain points, only *genuine cooperative cancellation* (a live, cancelled
  token) propagates; a handler-internal `OperationCanceledException` is logged and
  swallowed like any other post-completion failure, and the dispatches queued behind it
  still run. Rationale: the entry drain deliberately passes no token, so an OCE reaching it
  can never mean "the caller cancelled" — letting it fail an already-committed call and
  discard the remaining queue is exactly the failure AC-3 exists to prevent. The todo's
  AC-3 wording ("OCE still propagates") is restated accordingly as a planned amendment.
- The todo's cross-phase ordering guarantee (AC-1) becomes verified end to end with the
  consumer drain in play, not just at the scheduler seam.

---

## Framework & Architectural Alignment

- **Failure semantics key off the drain point, not the phase** (PHASE-001 plan review) —
  the coordinator's drain is in-transaction (exceptions propagate, consumer token honored);
  the entry drain is post-completion (log-and-swallow, no token).
- **Persistence-agnostic:** the framework exposes drain points; it never flushes or
  commits. The coordinator is a trigger the consumer's own transaction code calls.
- **Consumer-facing public surface** lives in the root `Neatoo.RemoteFactory` namespace
  (like `IFactoryEvents`); scope-scoped runtime services; registration parity with the
  scheduler — Server and Logical, absent in Remote, where no handlers dispatch.
- **`[Service]` method injection** is how consumer factory code reaches the coordinator —
  the standard server-only service pattern from the Design projects.
- **Logging via the LoggerMessage source-generated `Log` class**, 9xxx phased-dispatch
  event-id range, next free id.

---

## Constraints & Invariants

- The sweep is already implemented and pinned — wire the warning into it; do not re-plumb
  it (inherited PHASE-001).
- The coordinator delegates to the scheduler's in-transaction drain so handler exceptions
  propagate and the consumer's transaction can roll back (inherited PHASE-001).
- The registry's `(event type, handler class type)` dedupe key stays untouched — NF0504's
  premise depends on it (inherited PHASE-002).
- The fail-open warning discriminates **per dispatch, not per entry call**: any
  `AfterFlush` dispatch swept at the post-completion drain warns — including work the
  consumer raised after their own drain — *except* work created during that sweep by a
  later-phase handler (the documented carve-out, which had no drain point left to miss).
  AC-5 stands as written. A per-entry-call "consumer drained" flag was considered and
  rejected at plan review (A-V2: it silently under-warns the raised-after-the-drain case
  and carries a reset hazard in long-lived scopes, B-C1).
- The coordinator validates by **whitelist**: `AfterFlush` is the only phase a consumer
  may drain; every other value — `AfterCommit`, `Immediate`, and undefined casts like
  `(DispatchPhase)99` — is rejected. A blacklist would let an undefined value sweep the
  `AfterCommit` queue in-transaction through the `p <= through` sweep (plan review B-V1),
  and a public surface shipped in a minor release cannot be tightened later.
- Outside an entry call the coordinator **short-circuits** — it never delegates to the
  scheduler — because under the documented per-scope concurrency limitation "outside my
  entry call" can mean "inside another flow's," where an unconditional delegate would
  drain that flow's in-transaction work on the wrong token (plan review B-V3). Its XML
  acknowledges that limitation rather than overclaiming "empty by construction."
- The coordinator resolves the scope's **existing scheduler instance**; a registration
  that constructs its own would give each scope two schedulers with independent queues
  and a drain that quietly finds nothing (plan review B-C2).
- **Pre-declared pin-amendment set for the OCE decision** (sacred-tests rule — declared
  before the first edit, amended only as chartered):
  - `FactoryEventPhaseSchedulerTests.DrainAsync_PostCompletion_StillPropagatesCancellation`
    — intent inverts to pin the swallow (plus the behind-the-OCE dispatch running).
  - `FactoryEntryCallTests.HandlerThrowsOperationCanceled_MidDrain_EntryExitStillClearsAndDepthSurvives`
    — its premise (the one path where a successful entry exit throws) dissolves; repurpose
    to pin that the entry completes, the sibling handler behind the OCE runs, and the scope
    stays reusable.
  - XML docs stating "OCE still propagates" (`DispatchPhase`, the scheduler interface).
  - `src/Design/CLAUDE-DESIGN.md` Runtime Log Events rows for 9003 ("OCE still
    propagates") and 9006 (its "drain a handler's OCE aborted" cause becomes unreachable)
    — the requirements doc must not contradict shipped behavior (plan review A-V1); the
    new warning's row is added in the same edit.
  Every other existing pin stays green unmodified — including PHASE-003's six named
  pins and the integration cancellation tests (the choke point's post-invoke cancellation
  check is cooperative cancellation and keeps propagating).
- Public API addition only; no signature or behavior breaks outside the pre-declared set —
  the arc stays a minor release.
- No reflection; no generator emission changes, so the trimming posture is untouched.

---

## Steps

1. Define the public coordinator contract — interface plus XML docs that state the
   consumer-owned drain-point model: `AfterFlush` is the only drainable phase (whitelist —
   every other value, defined or undefined, is rejected), a drain outside any entry call
   short-circuits without touching the scheduler, and failure semantics come from the
   drain point. The XML acknowledges the per-scope concurrency limitation.
2. Implement it as a thin, scope-scoped delegation to the scope's existing scheduler's
   in-transaction drain honoring the consumer's token, and register it with scheduler
   parity (Server and Logical, absent Remote).
3. Teach the post-completion sweep to warn (new 9xxx id, Warning level, naming the event
   type) for each `AfterFlush` dispatch it picks up that was not created during the sweep
   itself — the per-dispatch discriminator that leaves the documented carve-out
   warning-free while catching both never-drained and raised-after-the-drain work.
4. Enact the OCE decision at the post-completion drain: only a live, cancelled token
   propagates; handler-internal OCE is logged and swallowed and the queue keeps draining.
   A propagating cancellation abandons the rest of the drain; the abandoned dispatches
   stay queued for the entry exit's clear (the existing contract). Amend the two
   pre-declared pins and restate todo AC-3 in `todo.md` itself.
5. Add integration targets exercising the real consumer pattern: factory methods that
   inject the coordinator via `[Service]`, with attribute-declared `AfterFlush` handlers
   registered only through the generated registrar — covering drained, never-drained
   (fail-open + warning), and full three-phase ordering scenarios.
6. Update the phase-contract XML docs (`DispatchPhase`, scheduler, dispatcher,
   `FactoryEventHandlerAttribute`) wherever the coordinator's arrival or the OCE
   restatement changes them — including scoping `DispatchPhase`'s unconditional
   cross-phase ordering sentence, which a consumer raising an `Immediate` event after
   their drain now inverts (plan review A-C3) — plus the `CLAUDE-DESIGN.md` log-event
   rows named in the pre-declared amendment set.
7. Red-proof the discriminators that could pass against a wrong implementation — first on
   the list, the consumer-drain marker ordering, which is the only thing separating the
   coordinator from the fail-open sweep PHASE-002 already ships (plan review B-V2); then
   the three-phase ordering sequence, the warning's fire/no-fire pair, and the
   behind-the-OCE dispatch still running.

---

## Acceptance

- [ ] Mid-entry-call, `IFactoryEventPhaseCoordinator.DrainAsync(AfterFlush, ct)` runs the
      queued `AfterFlush` handlers at that point, in-transaction: a handler exception
      propagates to the drain caller, and when the entry call then fails, nothing queued
      survives into the next entry call. `[unit]`
- [ ] End to end, an attribute-declared `[FactoryEventHandler<T>(DispatchPhase.AfterFlush)]`
      handler — registered only through the generated registrar, no hand registration —
      runs at the consumer's drain point, for both remote and logical invocation: its
      marker is recorded **before** the factory method's completion marker, the one
      ordering a no-op coordinator (whose markers land after completion, via the
      PHASE-002 fail-open sweep) cannot produce. `[integration]`
- [ ] Cross-phase ordering holds as one observed sequence for a factory operation raising
      events in all three phases: `Immediate` markers, then the consumer drain's
      `AfterFlush` markers, then the method-completion marker, then `AfterCommit` markers. `[integration]`
- [ ] Fail-open end to end: the same attribute-declared `AfterFlush` handler in a call
      with no consumer drain runs **after** the method-completion marker (the sweep — 
      shipping since PHASE-002), and the new dedicated warning event id appears in the
      captured server logs. The warning is the load-bearing half of this bullet. `[integration]`
- [ ] The warning discriminates per dispatch: `AfterFlush` work created mid-sweep by a
      later-phase handler runs warning-free (the documented carve-out), while work the
      consumer raised after their own drain completed still warns. `[unit]`
- [ ] The consumer's token reaches the drained handlers, and cooperative cancellation
      propagates to the drain caller. `[unit]`
- [ ] OCE policy: at a post-completion drain, a handler-internal OCE (no live cancelled
      token) is logged and swallowed and the dispatches queued behind it still run; a
      genuinely cancelled token still propagates, abandoning the rest of the drain — the
      abandoned dispatches stay queued for the exit clear. `[unit]`
- [ ] Coordinator surface: resolvable in Server and Logical scopes, absent in Remote;
      every phase but `AfterFlush` is rejected — `AfterCommit` and an undefined cast such
      as `(DispatchPhase)99` both throw; outside any entry call the coordinator
      short-circuits — work enqueued directly (no entry call active) is left untouched
      rather than drained. `[unit]`
- [ ] The restated OCE contract is reflected in the `DispatchPhase` and scheduler XML docs. `[explicit-skip: doc bullet — checked at code review]`
- [ ] Full existing suite green with only the pre-declared pin amendments touched. `[explicit-skip: meta-bullet — verified from the Step 5 gate logs]`

---

## Current State (Pre-Flight)

*(filled at Step 3)*

---

## Test Evidence

*(filled after implementation, before the Step 5 gate)*

---

## Plan Amendments

*(none yet)*

---

## Notes

- **Client-raise relay gap (Discovery Log 2026-08-14, review A-V1)** — flagged for revisit
  "at PHASE-004 or at todo close." Recommendation: out of this plan. It is pre-existing,
  not phase-related, and needs its own design decision (echo-to-self semantics vs. the
  "one `[Remote]` call = one `Relay` invocation" contract). Decision recorded when the
  user rules on it; **fallback venue is the Step 7 close-out audit**, and a "warrants
  work" ruling needs a `Draft` index row first (no orphan plans).
- **Warning pins:** both tiers already have capture harnesses — integration via
  `ClientServerContainers.ScopesWithLogging` (PHASE-003's 9001/9004/9005 assertions) and
  unit via `FactoryEventPhaseSchedulerTests`' existing `CapturingLoggerProvider` /
  `NewDispatcher(out logs)`, which already pins 9003. Nothing to build (plan review B-C3
  corrected this note's earlier claim); PHASE-007's queued 9002/9004/9006 pins reuse the
  same harnesses.
- **Keyboard gotchas carried from plan review:** whatever mark implements the
  per-dispatch warning discriminator must not outlive its drain in long-lived scopes
  (B-C1, transposed); under the per-scope concurrency limitation, one flow's drain
  affects a concurrent flow's warning — a consequence of documented granularity, not a
  redesign trigger (B-C1); if provenance-based OCE discrimination reads better than token
  state at the keyboard, `OperationCanceledException.CancellationToken` is the sharper
  tool (B-C4).
- **PHASE-005 owns** the Design-project demonstration of the coordinator, the
  `CLAUDE-DESIGN.md` pattern narrative, and the published-docs/skill updates (its Scope
  already covers the log-id table's narrative). This plan touches only the log-event
  table rows its behavior change falsifies (plan review A-V1/A-C2).
- The consumer-drain seam in integration tests is a factory method body calling the
  coordinator — consumer transaction code runs inside factory methods, so no harness
  surgery is needed to model "between flush and commit."
