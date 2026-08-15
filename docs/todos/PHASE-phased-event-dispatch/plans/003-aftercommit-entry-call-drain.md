# AfterCommit: Entry-Call Tracking and Framework-Owned Drain

**Plan #:** 003
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-14
**Plan-review opt-in:** Yes (touches all three factory renderers; entry-shape subtleties found at recon make this the riskiest plan)
**Code-review opt-in:** Yes (behavior-changing across generated code and runtime)

---

## Scope

Make the `AfterCommit` queue drain when the *entry* factory call completes successfully,
uniformly for HTTP-dispatched `[Remote]` calls and direct server-side/local invocation,
with rollback-discard on failure, swallow-and-log exception semantics (dedicated log event
ids), and drained-handler events still joining the same response's relay batch. Also owns
the "Raise outside any factory call" semantics (dispatch immediately, debug log), since
that is the absence-of-entry-tracking case. Known recon risks this plan must resolve at
the keyboard: the three renderers share no pipeline helper; static factories have no
`Local*` methods (DI delegate lambdas instead); public wrappers are mostly non-async;
`LocalSave` nests into `LocalInsert`/`LocalUpdate`/`LocalDelete`; HTTP calls enter `Local*`
directly, bypassing public wrappers. This plan does NOT own the consumer-facing drain API
(PHASE-004).

---

## Constraints inherited from PHASE-001 (recorded at its Step 5 gate)

- **The drain call sits on the success path only** — never in a `finally`, never in a
  scope-disposal hook or middleware that runs on failure. Rollback-discard is emergent
  ("a scope that fails simply never drains"), so a drain on the failure path breaks the
  todo's AC-2 silently and no primitive-level test can catch it (code review C4).
- The scheduler API to call is `IFactoryEventPhaseScheduler.DrainAsync(phase,
  inTransaction, ct)` in `Neatoo.RemoteFactory.Internal` — public so generated code can
  reach it. Pass `inTransaction: false` at the entry-call drain point.
- The cancellation-token *policy* question is open here: queued dispatches currently
  receive the drain-time token (pinned by
  `FactoryEventPhaseSchedulerTests.DrainAsync_HandlerReceivesTheDrainTimeCancellationToken`).
  Decide whether a post-completion drain should pass the request token at all — an
  `OperationCanceledException` from it fails a call that already succeeded (plan review
  B-C5).
- `IFactoryEvents.RaiseUntyped` has no general test coverage repo-wide; it is the
  server-side landing point for client-raised events, so this plan's remote-entry work is
  the natural place to add it (tech debt raised at PHASE-001's gate).

*(Stub — Intent, Alignment, remaining Constraints, Steps, Acceptance filled at Step 2.)*
