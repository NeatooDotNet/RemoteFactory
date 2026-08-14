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

*(Stub — Intent, Alignment, Constraints, Steps, Acceptance filled at Step 2.)*
