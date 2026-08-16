# PHASE-004 Test Review (Step 5 Gate)

**Plan:** [../plans/004-afterflush-coordinator.md](../plans/004-afterflush-coordinator.md)
**Reviewer:** test-reviewer agent
**Date:** 2026-08-15
**Round 1 result:** 1 must-cover, 3 should-cover (2 plan-related + 1 tech-debt), 7 nice-to-have, 5 test-quality notes. Sacred-tests sweep clean (both pre-declared pin amendments honored and strengthened; semantic-weakening sweep of unedited tests found nothing).
**Closure:** must-cover and both plan-related should-covers closed with tests + a measured RP-7; two nice-to-haves taken; the rest recorded below. Final counts after closure: unit 705×2, integration 587+5skip×2, Design 86×2 — all green.

---

## Round 1 — The Must-Cover (the gate's core catch)

**Finding:** Acceptance bullet 5's case 3 ("work the consumer raised after their own
drain still warns") was pinned only on a bare scheduler with no entry call — a state
production cannot reach, since the dispatcher only enqueues while an entry call is
active. Consequence: the rejected per-entry-call "consumer drained" flag — implemented
carefully, latched only while entry-active — **passes the entire pre-gate suite**,
because the bare-scheduler test never opens an entry call so the flag never latches
there. Worse, the red-proof log *claimed* that design would turn the bare test red —
asserted, never measured, and false for the guarded variant. The arc's signature
"asserted, not proven" shape, inside the artifact that exists to prevent it.

**Closure (both halves):**
1. New entry-call-scoped pin —
   `FactoryEventPhaseCoordinatorTests.DrainAsync_RaiseAfterTheDrain_InARealEntryCall_SweepsAndWarnsExactlyOnce`:
   real Server scope, real dispatcher, raise → coordinator drain → raise again → entry
   sweep; asserts both dispatches ran (drained one mid-body) and **exactly one** 9007
   naming the event type.
2. **RP-7** — the guarded flag actually implemented as a sabotage and measured: the new
   test went RED (flag latched, zero 9007s), the bare-scheduler test stayed GREEN
   (measuring the reviewer's diagnosis), and the carve-out test's collateral red is
   annotated with the flag-plus-stamp variant that only the new test catches. RP-3's
   false sentence corrected in place with the correction note, per the arc's
   record-the-miss precedent (PHASE-002 RP-8).

## Round 1 — Should-Covers (plan-related, both closed)

1. **Cooperative cancellation at the coordinator's own drain point was unpinned** and
   Evidence row 6 cited the scheduler's post-completion drain instead. Closed:
   `DrainAsync_ConsumersTokenCancelledMidDrain_PropagatesAndLeavesTheRestQueued`
   (cancel inside a drained handler → OCE reaches the coordinator caller, sibling
   dispatch stays queued, exit clear discards). Evidence row corrected.
2. **Overlapping drains unpinned — `_activeDrains`-as-counter was a load-bearing
   comment with nothing enforcing it** (a bool passed the whole suite). Closed:
   `DrainAsync_CoordinatorCalledInsideTheEntrySweep_LaterMidSweepWorkStaysWarningFree`
   (a swept handler opens an inner coordinator drain; work a later swept handler
   creates must still stamp as mid-drain — a bool drops the state when the inner drain
   exits and warns spuriously).

## Round 1 — Nice-to-Haves (2 taken, 3 recorded open)

- **Taken:** validation-before-short-circuit ordering
  (`DrainAsync_RejectedPhaseOutsideAnyEntryCall_StillThrows`); the A-C3 scoped-ordering
  sentence pinned by adding a post-drain Immediate raise to
  `CoordinatorOrderingCommands._RunOrdered` (both ordering sequences now assert the
  interleave). Also taken from test-quality notes: the dead `NoOp` member removed, the
  Current State `Scopes()` tuple-order note corrected, the `CLAUDE-DESIGN.md` 9007
  row's carve-out wording now names the per-scope-concurrency consequence.
- **Open by choice (recorded, not queued):**
  - Coordinator registration *lifetime* pin (presence is pinned; a `TryAddSingleton`
    slip would surface loudly through the DI-resolved drain tests).
  - Relay-batch membership for events raised by coordinator-drained AfterFlush handlers
    (the drain runs inside the method body, structurally identical to Immediate for
    relay purposes; PHASE-003 pins the AfterCommit analogue).

## Round 1 — Tech-Debt Findings (routing decisions)

- **Undefined-phase registration silently loses work** (`(DispatchPhase)99` queues,
  never sweeps, discards behind a Debug 9006) — consciously accepted in the Discovery
  Log (2026-08-15) but invisible in the suite. **Routed: fold into PHASE-007** as one
  documenting pin alongside its 9002/9004/9006 emission pins.
- **`IEventTestService` singleton over shared `FormatContainers`** (Guid-filter
  discipline unenforced) and **`ScopesWithLogging` sharing one provider across all
  three containers** (log pins cannot attribute container) — both fold into PHASE-007's
  existing harness items.
- **`Enqueue` null-handler guard unpinned** — recorded; take with PHASE-007's pins or
  skip.
- **`SingleEventRelay_ConsumerReceivesEvent` flakes under full-parallel load** (hard
  2-second poll on a fire-and-forget; 2 of 3 full-solution runs today, never on
  serialized runs; siblings already permanently skipped for timing) — recorded for
  PHASE-007's harness scope; out-of-scope test, not modified.

## Sacred Tests

Both pre-declared pin amendments verified by the reviewer as honored and
*strengthened* (the entry-call amendment keeps every original recovery/cleanliness
assertion); RP-4 proved both red against the reverted behavior. No other pre-existing
test edited; the reviewer's semantic-weakening sweep (total-count and level-based log
assertions that 9007's arrival could hollow out) found none.

## Test-Quality Notes Disposition

`DrainAsync_ConsumerDrainedAfterFlush_NeverWarns` acknowledged near-tautological —
retained as happy-path smoke, demoted in the Evidence table with that framing. The
`ScopesWithLogging` attribution weakness noted (harmless today: only the server
container can emit 9007). Evidence rows 3/5/6/8/10 updated for the closure state.
