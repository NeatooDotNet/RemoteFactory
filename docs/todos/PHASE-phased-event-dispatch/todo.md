# Phased Factory-Event Dispatch

**ID:** PHASE
**Type:** Enhancement
**Status:** In Progress
**Priority:** High
**Created:** 2026-08-14
**Last Updated:** 2026-08-14

**Origin:** External proposal from zTreatment LAND-002 —
`zNeuropathy/zTreatmentLandingScreen/docs/proposals/2026-08-11-remotefactory-phased-event-dispatch.md`
(verified against this repo's v1.7.0 code before this todo was opened).

---

## Goal

Give `[FactoryEventHandler<T>]` registrations a **dispatch phase** so read-only projection
handlers stop inheriting the in-transaction dispatch contract built for atomic write
handlers. Three phases: `Immediate` (today's contract, stays the default), `AfterFlush`
(in-transaction, drained when the consumer signals via a new
`IFactoryEventPhaseCoordinator.DrainAsync`), and `AfterCommit` (drained by the framework
when the entry factory call completes — no ambient transaction, handler exceptions logged
and swallowed, never run if the entry call fails). Cross-phase ordering becomes a framework
guarantee; events raised by phase handlers still join the same response's relay batch.
RemoteFactory stays persistence-agnostic throughout — it never flushes or commits; it only
exposes drain points.

## Acceptance Criteria

- [ ] A handler registered `AfterCommit` runs after the entry factory call completes (works
      for both HTTP-dispatched `[Remote]` calls and direct server-side/local invocation),
      and all `Immediate` handlers for the same save complete before any `AfterFlush`
      handler, which complete before any `AfterCommit` handler.
- [ ] If the entry factory call throws, queued `AfterFlush`/`AfterCommit` handlers never
      run — the queues are discarded.
- [ ] An `AfterCommit` handler exception is logged (dedicated event id) and swallowed;
      remaining queued handlers still run; `OperationCanceledException` still propagates.
- [ ] Events raised *by* `AfterCommit` handlers still reach the client in the same HTTP
      response's relay batch.
- [ ] `IFactoryEventPhaseCoordinator.DrainAsync(AfterFlush)` drains the AfterFlush queue at
      the consumer's chosen point; AfterFlush handlers never drained by the consumer run at
      the AfterCommit point with a logged warning (fail-open).
- [ ] An event raised outside any factory call with phase-registered handlers dispatches
      immediately with a debug-level log (no throw, no silent drop).
- [ ] Backward compatibility: handlers without a phase argument behave exactly as today —
      the full existing test suite passes unmodified.
- [ ] Trimming safety preserved: phase registrations flow through the v1.7.0
      forwarding-holder pattern; nothing new ships handler bodies to trimmed clients.
- [ ] Design projects demonstrate phased handlers (source of truth updated); published
      docs and the RemoteFactory skill document the phase contract, including that
      `Immediate` handlers observe staged (unflushed) state.

## Out of Scope

- Cross-event coalescing ("any of these four events → one recompute") — proposal
  explicitly defers it; handlers can guard internally.
- Fresh-scope execution for `AfterCommit` handlers — v1 runs them in the originating
  scope (proposal open question 1, resolved: same scope).
- Any persistence concept in the framework: no flush, no commit, no transaction
  awareness. Drain points only.
- zTreatment-side consumption (deleting its flush sites, chain-link event, etc.) — that
  work lives in the zTreatment repos after a release ships.
- The KnockOff internal-interface stubbing limitation noted in the proposal's "Related,
  separate" section — different repo, different todo.

---

## Plan Index

| # | File | Title | Status |
|---|------|-------|--------|
| 001 | [001-phase-model-and-queueing](./plans/001-phase-model-and-queueing.md) | DispatchPhase enum, registry phase, dispatcher queueing | Done |
| 002 | [002-generator-phase-passthrough](./plans/002-generator-phase-passthrough.md) | Generator reads phase from attribute, threads to registration | Draft |
| 003 | [003-aftercommit-entry-call-drain](./plans/003-aftercommit-entry-call-drain.md) | Entry-call tracking in generated factories; AfterCommit drain | Draft |
| 004 | [004-afterflush-coordinator](./plans/004-afterflush-coordinator.md) | IFactoryEventPhaseCoordinator public API + fallback drain | Draft |
| 005 | [005-design-docs-skill](./plans/005-design-docs-skill.md) | Design projects, published docs, skill reference | Draft |
| 006 | [006-coalescing](./plans/006-coalescing.md) | Opt-in same-event coalescing (v2, queued per user) | Draft |
| 007 | *(not yet drafted)* | Tech debt: registry test-isolation hook (`Clear()` is internal and uncalled; every test invents unique event types) | Draft |

---

## Discovery Log

### 2026-08-14 — PHASE-001 (gate found a real defect)
- **Finding:** The test-review gate caught that the drain resolved only the requested
  phase's queue, so work a handler enqueued into an already-passed phase was silently
  dropped — the exact silent-loss class this todo exists to remove.
- **Decision:** Amend — replaced with a drain that sweeps the requested phase and every
  earlier one, earliest first; three tests verified red against the pre-fix code.
- **Follow-up:** PHASE-004 inherits the sweep (it implements that plan's fail-open path);
  constraint recorded in its draft. See [reviews/001-test-review.md](./reviews/001-test-review.md).

### 2026-08-14 — PHASE-001 (shared-source build constraint)
- **Finding:** `FactoryAttributes.cs` is linked into the netstandard2.0 Generator project,
  so putting `DispatchPhase` on the handler attribute compiled the enum into
  `Neatoo.Generator.dll` too — duplicating a public runtime type and breaking every
  project referencing both (CS0436 in RemoteFactory, CS0433 in UnitTests).
- **Decision:** Amend — moved `FactoryEventHandlerAttribute<T>` to its own unlinked file;
  the generator matches it by metadata-name string and never needed the type.
- **Follow-up:** n/a (PHASE-002 must not re-link `DispatchPhase` into the generator; the
  new file's XML doc carries the warning).

### 2026-08-14 — PHASE-001 (plan review)
- **Finding:** Plan review returned CONCERNS — 4 veto findings, the sharpest being that
  failure semantics belong to the drain point rather than the phase, and that three
  acceptance bullets pinned interim behavior PHASE-003 is chartered to invert.
- **Decision:** Amend (draft edited before implementation; all vetoes addressed)
- **Follow-up:** [reviews/001-plan-review.md](./reviews/001-plan-review.md); B-C5 token
  policy decision lands at PHASE-003.

### 2026-08-14 — PHASE-001
- **Finding:** `[FactoryEventHandler<T>]` is `AllowMultiple = true`, so one class can
  declare the same event at two different phases; the registry's
  `(eventType, handlerClassType)` dedupe would silently drop the second registration.
- **Decision:** Defer
- **Follow-up:** PHASE-002 (likely a generator diagnostic for duplicate same-event
  attributes; decide there whether to diagnose or define last-wins semantics).

---

## Skipped Steps

*(none yet)*

---

## Sibling Todos

- [ ] [RFEF — RemoteFactory.EntityFrameworkCore, declarative factory transactions](../RFEF-factory-transactions/todo.md)
  — surfaced 2026-08-14 while discussing this todo's target consumer code (per-method
  begin/commit boilerplate); doesn't advance PHASE's goal (persistence stays out of this
  framework arc) but builds directly on PHASE-003's entry-call tracking and PHASE-004's
  drain semantics, and would generate the `AfterFlush` drain call PHASE-004 otherwise
  leaves to consumer code. Blocked until those plans land. (Link resolves once the RFEF
  branch merges to main.)

---

## Close-Out Audit

*(pending — filled at Step 7)*

---

## Docs & Retro

*(pending — filled at Step 8)*

---

## Results / Conclusions

*(pending)*
