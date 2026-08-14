# PHASE-001 Test Review — 2026-08-14

**Reviewer:** test-reviewer agent (two rounds)
**Closing tier:** must-cover closed; all round-1 should-cover closed; round-2 should-cover
closed. Remaining nice-to-have accepted or queued as tech debt.

## Round 1 — findings and dispositions

**must-cover (all closed):**

1. **Re-entrant enqueue into an already-passed phase was untested — and was a real
   production defect.** `DrainAsync` resolved exactly one queue, so a handler enqueueing
   into a phase whose drain point had passed was silently dropped. Fixed by
   `TryDequeueThrough`, which takes the next dispatch from the earliest non-empty phase at
   or before the requested one, looping until all are empty. Pinned by
   `DrainAsync_ReentrantEnqueueIntoAnAlreadyPassedPhase_StillRunsInThisDrain`.
2. **The 9xxx logging path never executed in any test** (the test helper left the optional
   `ILoggerFactory` null). `NewDispatcher` now wires a capturing provider;
   `DrainAsync_PostCompletionSwallow_LogsTheDedicatedEventIdWithTheException` asserts id
   9003, `LogLevel.Error`, and the exact exception instance.
3. **Queued-dispatch payload fidelity unasserted.**
   `DrainAsync_HandlerReceivesTheEventAndOptionsItWasQueuedWith` asserts the exact
   (event value, `RaiseOptions`) pairs and — after code-review C5 — `Assert.Same` on the
   originating scope provider.

**should-cover (all closed):** real scope-disposal rollback-discard test replacing the
vacuous `NeverDrained_RunsNothing`; `RaiseUntyped` parity; drain-time cancellation-token
plumbing; attribute default/explicit phase round-trip; the no-queue fallback tested rather
than deleted; re-entrancy exercised through the real `handler → IFactoryEvents.Raise →
registry → defer` path.

## Round 2 — findings and dispositions

**must-cover (closed):** the multi-phase drain fix introduced a *new* untested behavior —
a later-phase drain now also sweeps an earlier phase the consumer never drained (PHASE-004's
fail-open path, implemented here as a side effect). Closed by
`DrainAsync_SweepsAnEarlierPhaseTheConsumerNeverDrained`, which also asserts 9003
attributes the failure to the phase the dispatch was *queued* at rather than the phase
requested.

**should-cover (closed):** mid-drain earlier-phase work preempts remaining later-phase work
— pinned deliberately by `DrainAsync_MidDrainEarlierPhaseWork_PreemptsRemainingLaterPhaseWork`
so the ordering is a decision, not an accident; and the stale `DrainAsync` XML doc, rewritten
to state that the drain covers the requested phase and every earlier one.

**nice-to-have (accepted, not actioned):** `Raise_DeferredHandler_StillCollectsForRelayAtRaiseTime`
does not also assert `HasPending` (reviewer confirmed not must-fix — the premise is
independently pinned); 9002's count spans phases while its `{Phase}` names only the
requested one; `DrainAsync_OnlyDrainsTheRequestedPhase` is now a mild misnomer;
`Enqueue(DispatchPhase.Immediate, ...)` remains unspecified on the interface.

## Red-verification

Per the project memory *"a check that could never go red is not evidence"*, the three
multi-phase drain tests were run against the pre-fix implementation (`p <= through`
reverted to `p == through`). All three failed; log retained at
[`001-redproof.log`](./001-redproof.log). The fix was then restored and the full suite
re-run green.

## Tech debt queued (not absorbed into this plan)

- `IFactoryEvents.RaiseUntyped` has no general coverage anywhere in the repo — pairs
  naturally with PHASE-003's remote-entry work.
- `FactoryEventHandlerRegistry` is process-global mutable static with no test-isolation
  hook (`Clear()` is internal and uncalled), forcing every test to invent unique event
  types.

Both are recorded as Plan Index entries in the parent todo.

## Final state

Build 0 errors; unit 653 × net9.0/net10.0, integration 561 passed / 5 skipped × 2, Design
86 × 2 — all 0 failures. No existing test modified.
