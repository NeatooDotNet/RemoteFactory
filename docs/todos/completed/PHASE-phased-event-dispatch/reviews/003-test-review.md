# PHASE-003 Test Review — Step 5 Gate

**Plan:** [../plans/003-aftercommit-entry-call-drain.md](../plans/003-aftercommit-entry-call-drain.md)
**Logs:** `003-build.log`, `003-test.log`, `003-redproof.log` (regenerated after each round)

## Round 1 — 2026-08-14

Reviewer verdict shape: 3 must-cover, 6 should-cover, 5 nice-to-have (plan-related);
1 must / 2 should / 2 nice (pre-existing tech debt). Baseline cross-check and red-proof
log verified genuine. Three Test Evidence rows found overstating their citations.

### Must-cover (all closed)

1. **`AspForbidException` never exercised — and the noted mitigation was wrong.** The
   reviewer showed the exception type is public in the core package (only its producers
   need ASP.NET), so a direct-throw target needs no pipeline. Closed with
   `AspForbidException_AfterEnqueueingPhasedWork_ClearsWithoutDraining`, which also pins
   the pre-existing empty-shape → `default` client observable. Evidence row corrected.
2. **Concurrent flows in one scope: lock ≠ flow isolation, semantics unrecorded.** A
   failed flow's exit is a nested exit (no clear); the surviving flow's drain runs both
   flows' work. Closed by pinning exactly those semantics
   (`ConcurrentFlowsInOneScope_ShareEntryState_FailedFlowsWorkRidesTheSurvivingDrain`)
   and recording the per-scope-granularity posture as a Discovery Log entry — scopes are
   the framework's isolation unit; concurrent flows sharing one scope already share
   every scoped service.
3. **Interface-renderer emission shape unpinned** (the leg where the split is new and
   trimming is UNVERIFIED). Closed with
   `InterfaceFactory_GuardedLocalMethod_SplitsIntoSyncWrapperAndCore` (wrapper non-async
   + guard + helper forward; core private and unguarded; an `async` wrapper goes red).

### Should-cover (all closed)

4. Nesting tests couldn't discriminate inner-vs-outer drain (and the evidence row's
   "depth mismatch throws" justification was wrong — a balanced always-drain stays
   green). Closed with `NestedChildSave_DoesNotDrainAtTheChildsCompletion` (parent saves
   a child, then records a marker; an inner drain lands between the markers). Row
   corrected.
5. Post-OCE entry-exit clear and the double-`EndEntryCallAsync` tolerance it relies on:
   `HandlerThrowsOperationCanceled_MidDrain_EntryExitStillClearsAndDepthSurvives`.
6. Two sacred relay-collection tests were silently weakened by the production change
   (ran with no entry active → deferral premise vacuous). Restored with
   `BeginEntryCall` + `HasPending`; disclosed in the evidence as the failure mode
   pre-declaration cannot catch.
7. 9005 had no positive emission pin ("absence elsewhere" ≠ pin). Closed with a
   capturing logger in the outside-entry unit test.
8. Caught-nested-failure and drained-handler-invokes-a-factory:
   `NestedEntryFails_OuterCatchesAndSucceeds_TheEntryStillDrains` (pins that only the
   outermost exit decides drain-vs-clear) and
   `DrainedHandlerInvokingAFactory_NestsWithoutDrainingOrClearingTheDrainInProgress`.
9. Interface-leg success-path drain as the outermost entry
   (`InterfaceFactory_AsTheOutermostEntry_DrainsOnSuccess`) and the generated sync
   non-`Task` shape with pending work
   (`SyncFactoryMethod_WithPendingPhasedWork_BlockDrainsAtCompletion`).

### Nice-to-have

Closed: sync no-scheduler mirror; strengthened End-without-Begin pin (depth survival +
follow-on cycle). Open by choice: 9006 emission pin (routed to PHASE-007 with 9002/9004),
relay-batch red-proof (structurally sound — the drain sits before relay collection in the
same method; noted).

### Tech debt routed to PHASE-007

9002/9004 (and now 9006) positive emission pins; `ClientServerContainers` tuple-order
divergence + `ScopesWithLogging` duplication. The pre-existing AspForbid response-shape
gap is now partially covered by the new integration test.

### Bookkeeping corrections applied

Unit-count breakdown (+9, not +8+1); backward-compat Acceptance bullet re-worded to name
the full disclosed amendment set (six pins + two TRIM-009 emission pins + two
relay-collection restorations).

**Post-closure totals:** unit 668×2, integration 579×2 (+5 skipped), Design 86×2 — 0
failures. Logs regenerated.

## Round 2 — 2026-08-14 (re-review after the add-tests loop)

All 3 must-cover and all 6 should-cover round-1 findings **verified closed** by reading
the closing tests against production: each pins what the disposition claims, at the
right tier, and would go red on the regression it names. Both nice-to-haves closed. The
closure commit (`595d195`) touched only tests and docs — no production code was reshaped
to fit a test — and the red-proofed tests are byte-identical to their state when
`003-redproof.log` was captured, so reusing that log is valid. Suite arithmetic checks
out (+6 unit, +4 integration → 668×2 / 579×2 +5 / 86×2, 0 failures; build warnings all
pre-existing and unrelated).

Two new **should-cover** findings, both one-liners, neither invalidating a closed must:

1. `AspForbidException_AfterEnqueueingPhasedWork_ClearsWithoutDraining` asserted only the
   "without draining" half — a forbid route that skipped `EndEntryCallAsync(false)`
   would leave a long-lived scope at depth ≥ 1 and silently kill every subsequent drain
   while staying green.
2. `Raise_DeferredHandlerWithServerOnly_IsNotCollectedForRelay` received
   `BeginEntryCall()` but not the `HasPending` premise assertion its sibling got — the
   exact round-1 vacating failure mode, and the Gate-closure row overstated by one test.

Nice-to-have: the concurrent-flows pin covers only the enqueue-before-either-exits
interleaving; the enqueue-during-the-survivor's-drain window is timing-dependent
(joins the drain or is cleared) and was unrecorded. Also noted: the OCE mid-drain test
depends on registry handler order (fails loudly, not falsely), and the forbidden-path
tests asserted consequence without premise.

Open-by-choice items and tech-debt routing re-checked and found honestly recorded.

### Round 2 disposition (orchestrator)

- **S1 closed:** the AspForbid test now follows the forbidden call with a successful
  `Create` in the same server scope and asserts that call's full drain — a stuck depth
  fails it.
- **S2 closed:** `HasPending` premise assertion added to the ServerOnly sibling;
  Gate-closure row corrected to record the two-round history honestly.
- **N1 recorded:** the Discovery Log's concurrent-flows entry now names the
  enqueue-during-drain window as inherent to per-scope granularity (not pinned — same
  documented-limitation posture).
- N2/N3 noted, no action: the OCE order dependence fails loudly; the forbidden-path
  premise is now indirectly asserted by S1's follow-on drain.

**Gate closed.** Final totals: unit 668×2, integration 579×2 +5 skipped, Design 86×2 —
0 failures (both round-2 closures strengthened existing tests rather than adding new
ones; logs regenerated).
