# PHASE-004 Code Review (Step 5, per-plan, findings-only)

**Plan:** [../plans/004-afterflush-coordinator.md](../plans/004-afterflush-coordinator.md)
**Reviewer:** code-reviewer agent
**Date:** 2026-08-16
**Reviewed at:** 9af8e96, diffed against `d72d2a4` (the PHASE-002 merge this branch stacks on)
**Result:** 1 veto-tier, 5 callout-tier. Veto and all five callouts addressed; dispositions below.

---

## What the review verified independently (not inherited)

The reviewer re-derived the load-bearing claims rather than trusting comments or prior gates:

- **`_activeDrains` accounting is correct.** Increment under `_gate` immediately before the
  `try`, decrement in the `finally`; no statement between them, `DrainAsync` is not
  `async void`, so no path leaks a nonzero count on the exception route. The stamp is read
  under the same lock as the enqueue — no torn read. Handlers running outside the lock is
  fine because the stamp selects a warning, never affects dispatch correctness.
- **The overlap test's premise holds.** `EndEntryCallAsync` does not decrement depth before
  the outermost drain (the `> 1` branch returns; depth 1 falls through with `ClearAtExit`
  in the `finally`), so `IsEntryCallActive` is true throughout the entry sweep and the
  coordinator's short-circuit does not fire inside it. The counter is genuinely required.
- **Registration closes B-C2 for real:** an explicit factory lambda resolving
  `GetRequiredService<IFactoryEventPhaseScheduler>()` in the non-Remote branch — same shape
  as the scheduler's own registration, and no DAM-rooted ctor.
- **Validation precedes the short-circuit**, so that pin tests real ordering, not incidental.
- **Both pre-declared pin amendments are faithful and strengthened**, no other pre-existing
  test edited, harness extraction verbatim + additive. An independent semantic-weakening
  sweep for 9007's arrival found nothing hollowed (the only level/count-based log
  assertions are id-scoped or reporting-only).
- **Trimming posture untouched:** zero `src/Generator/` changes in the diff; the new type
  graph is statically rooted from `AddRemoteFactoryServices` exactly as the scheduler was.
- **Build warnings:** 4, all pre-existing and identical in `003-build.log`. None new.

## Veto-tier

**V1 — todo AC-1 is now literally false, contradicted by a green test this plan shipped,
and was not restated.** AC-1 promised "all `Immediate` handlers for the same save complete
before any `AfterFlush` handler." The shipped integration pin asserts
`["ord-immediate", "ord-flush", "ord-immediate", "ord-method-done", "ord-commit"]` — a
second `Immediate` handler completing *after* an `AfterFlush` one, for the same entry call,
because `_RunOrdered` raises again after its drain. Not pre-existing: before the coordinator
there was no in-body AfterFlush drain point, so this interleave was unreachable outside the
documented handler-raise carve-out. **PHASE-004 falsified AC-1 exactly as it falsified
AC-3 — and restated only AC-3.** Identical ground to plan review A-V1 ("the requirements doc
must not contradict shipped behavior"), identical remedy. The plan's own Acceptance bullet 3
carried the same unscoped phrasing while its test asserted five markers.

**Disposition: adopted.** AC-1 restated in `todo.md` with an AC-3-form provenance note
(ordering anchored per drain point, not a global barrier over the operation); Acceptance
bullet 3 reworded to the five-marker sequence its test actually asserts. No code change —
`DispatchPhase.cs` had already been rescoped correctly per A-C3; this is the requirements
doc catching up to shipped, tested behavior.

## Callout-tier

| # | Finding | Disposition |
|---|---|---|
| C1 | `DrainAsync`'s summary said "for the factory call currently in flight in this scope" — the drain is **scope**-scoped, so under per-scope granularity it also runs a concurrent flow's work, on this caller's token and in this caller's transaction. A subtler form of the "empty by construction" overclaim the plan Constraint told this gate to catch. | **Fixed now** (permanent minor-release contract text). Summary says "in this DI scope"; a new `<remarks>` paragraph states the scope-wide consequence and the one-call-per-scope guidance. The two `<remarks>` blocks were merged into one with `<para>`s rather than left duplicated. |
| C2 | "Abandoned dispatches are discarded at the entry call's exit" stated unconditionally in three places, but it holds only on the **failure** exit: if the consumer swallows the cancellation and the call still succeeds, the dispatches are swept at the AfterCommit point with 9007 instead. The interface's own `<remarks>` already said it correctly — internal inconsistency in one file. | **Fixed now** in all three: the coordinator's `cancellationToken` param doc, the scheduler's `inTransaction` param doc, and `CLAUDE-DESIGN.md`'s 9006 row. |
| C3 | The short-circuit is silent, and 9007's message tells the consumer to do what they just did. A transaction abstraction wrapping the factory call from *outside* drains into a closed entry call, sees nothing, and gets advice it already followed — where the framework logs 9004/9005 for the analogous "raised where I can't queue" cases. | **Routed to PHASE-007** (index row updated) alongside its queued 9002/9004/9006 emission pins; the harness already exists. Partial mitigation shipped now: the coordinator's `<remarks>` states explicitly that the drain must run inside the factory method body. |
| C4 | The plan Constraint described the carve-out as "work created during that sweep by a later-phase handler"; the implementation stamps work enqueued while **any** drain is in flight. They diverge only in the aborted-drain corner. `CLAUDE-DESIGN.md` already documented the broader (shipped) rule, so code and requirements agreed — the plan's phrasing was the narrow one. | **Constraint widened** to the shipped rule, naming the aborted-drain corner and the concurrent-flow consequence, so a future reader does not "fix" the stamp toward a rule the scheduler does not implement. |
| C5 | Plan Index row 004 still read `Draft` while the plan header read `In Progress`. | **Fixed** — both set to `Done` at plan close. |

**Traced, no action needed (per the review):** Design-project obligation correctly chartered
to PHASE-005 and named in the plan's Notes; the client-raise relay gap correctly names the
Step 7 close-out audit as fallback venue with the "warrants work needs a Draft row first"
rule attached; registry dedupe key untouched; sweep not re-plumbed; per-dispatch (not
per-entry-call) discrimination confirmed against both rejected variants (RP-7, RP-9).

## Post-fix verification

Build 0 errors; unit 705×2 TFMs, integration 587 passed + 5 standing skips ×2, Design 86×2 —
all green. The V1/C1/C2/C4 changes are documentation and XML only; C3's shipped half is one
`<remarks>` sentence. No production logic changed after the code review.
