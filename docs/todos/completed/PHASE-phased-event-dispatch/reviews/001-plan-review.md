# PHASE-001 Plan Review — 2026-08-14

**Reviewer:** plan-reviewer agent
**Verdict:** CONCERNS
**Disposition:** All four veto-tier findings addressed by editing the Draft plan before
implementation (2026-08-14). Callouts folded into the plan's Constraints/Notes or deferred
with owners named below.

## Veto-tier findings and dispositions

- **A-V1** `[DynamicallyAccessedMembers(All)]` on `RegisterHandler<TEvent>` is a documented
  trimming invariant (skills/RemoteFactory/references/trimming.md:232) the plan touched
  without listing; its loss on a new overload would be silent and untestable.
  → Added to Constraints & Invariants.
- **B-V1** Acceptance bullets 2/3/6 pinned raise-time queueing that PHASE-003 is chartered
  to invert (no-entry-call raises dispatch immediately once entry tracking exists).
  → Bullets annotated as interim semantics PHASE-003 will restate (planned amendment, not
  test-gutting); primary unit tests target the queue service's own contract.
- **B-V2** Propagate-vs-swallow was keyed to phase; the todo's fail-open rule drains
  AfterFlush handlers at the post-completion point where propagation would fail an
  already-succeeded call. → Failure semantics re-keyed to **drain point** (in-transaction
  drain propagates; post-completion drain logs-and-swallows), matching the proposal's own
  rationale.
- **B-V3** Re-entrant enqueue during a drain was unspecified; snapshot-and-drop would ship
  green. → Stance set: drain-until-empty; once the post-completion drain begins, newly
  queued dispatches of any phase join the current drain; infinite raise loops remain the
  consumer's bug exactly as with today's synchronous chained raises. New acceptance bullet
  added.

## Callout dispositions

- **A-C1** Runtime XML-doc deltas (`RaiseOptions.cs`, `IFactoryEvents.cs`,
  `FactoryEventsDispatcher.cs`, `FactoryAttributes.cs`) → owned by PHASE-001 (doc ships
  with behavior).
- **A-C2** CLAUDE-DESIGN mode-registration story + Common Mistakes #9 → explicitly traced
  to PHASE-005.
- **A-C3** Planning-guidelines evaluation recorded in plan Notes (no wire crossing in 001;
  no new diagnostics — deferred to PHASE-002).
- **B-C1** Registry constraint narrowed: additive change only; the two-argument
  `RegisterHandler` call the in-repo generator emits today must keep compiling mid-todo.
- **B-C2** Drain-primitive accessibility decided in Notes: public type in the
  `Neatoo.RemoteFactory.Internal` namespace (precedent: `IMakeRemoteDelegateRequest`),
  so PHASE-003's generated-code call sites can reach it.
- **B-C3** Relay-collection bullet needs a Server-mode container (collector is
  Server-only) → noted in plan.
- **B-C4** Interim dedupe stance documented: first-registration-wins for the life of the
  process; PHASE-002 decides diagnose-vs-last-wins from that baseline.
- **B-C5** Cancellation-token question (raise-time vs drain-time token; OCE from a
  post-success drain) → added to open design points; policy decision deferred to
  PHASE-003's call site.
- **B-C6** `TryAddScoped<IFactoryEvents>` consumer-override footgun (queue registered but
  fed by nothing if a consumer replaces the dispatcher) → recorded here so it isn't
  re-discovered as a bug; no action in 001.

This file is the durable record of the review; findings above are complete (4 veto-tier,
9 callouts as merged by the reviewer's recommendation list).
