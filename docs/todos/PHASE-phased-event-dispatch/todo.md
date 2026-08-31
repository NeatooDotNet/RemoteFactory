# Phased Factory-Event Dispatch

**ID:** PHASE
**Type:** Enhancement
**Status:** In Progress
**Priority:** High
**Created:** 2026-08-14
**Last Updated:** 2026-08-31
**Arc branch:** `PHASE` — predates the `{id}-arc` default; the header wins.
**Plan cap:** 13 issued / cap declared retroactively at 13 during the 0.9.0 conversion.
Not a budget this todo was held to — see the Retro.

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

- [x] A handler registered `AfterCommit` runs after the entry factory call completes (works
      for both HTTP-dispatched `[Remote]` calls and direct server-side/local invocation),
      and cross-phase ordering is anchored **per drain point**: for work raised before a
      given drain point, all `Immediate` handlers complete before any `AfterFlush` handler,
      which complete before any `AfterCommit` handler. Code that raises *after* its own
      `AfterFlush` drain interleaves that later `Immediate` work between drain points —
      the guarantee is per drain point, not a global barrier over the operation.
      *(Restated by PHASE-004, which created the in-body consumer drain point that makes
      the interleave reachable; the original wording was "all `Immediate` handlers for the
      same save complete before any `AfterFlush` handler, which complete before any
      `AfterCommit` handler.")*
- [x] If the entry factory call throws, queued `AfterFlush`/`AfterCommit` handlers never
      run — the queues are discarded.
- [x] An `AfterCommit` handler exception is logged (dedicated event id) and swallowed;
      remaining queued handlers still run. A handler-internal `OperationCanceledException`
      is swallowed the same way — the entry drain passes no token, so nothing may abort a
      succeeded call's post-completion work; only genuine cooperative cancellation (the
      drain's own token cancelled) propagates, at drain points that take one.
      *(Restated by PHASE-004, exercising the decision PHASE-003's code review C2
      delegated to it; the original wording was "`OperationCanceledException` still
      propagates.")*
- [x] Events raised *by* `AfterCommit` handlers still reach the client in the same HTTP
      response's relay batch.
- [x] `IFactoryEventPhaseCoordinator.DrainAsync(AfterFlush)` drains the AfterFlush queue at
      the consumer's chosen point; AfterFlush handlers never drained by the consumer run at
      the AfterCommit point with a logged warning (fail-open).
- [x] An event raised outside any factory call with phase-registered handlers dispatches
      immediately with a debug-level log (no throw, no silent drop).
- [x] Backward compatibility: handlers without a phase argument behave exactly as today —
      the full existing test suite passes unmodified.
- [x] Opt-in same-event coalescing (added 2026-08-18, tracing the 2026-08-14 user
      decision that queued PHASE-006): a handler that opts in on its attribute runs
      once per drain when the same `Equals`-identical event was raised N times during
      the entry call; without the flag, N raises still produce N dispatches; no
      collapse erases a fail-open 9007 obligation; the relay batch is unaffected.
- [x] Trimming safety preserved: phase registrations flow through the v1.7.0
      forwarding-holder pattern; nothing new ships handler bodies to trimmed clients.
- [x] Design projects demonstrate phased handlers (source of truth updated); published
      docs and the RemoteFactory skill document the phase contract, including that
      `Immediate` handlers observe staged (unflushed) state.

*All ten checked 2026-08-31 during the 0.9.0 conversion, each traced to a named test before
ticking — the criteria had gone unticked since 2026-08-14 while the arc ran on "queue empty."
Evidence: AC-1 `RemoteCreate_AfterCommitHandlerRunsAfterTheEntryCallCompletes` +
`EntryDrain_SweepsAfterFlushBeforeAfterCommit`; AC-2 `RemoteEntryFails_QueuedHandlersNeverRun`
+ `FailedCall_ThenSuccessfulCall_InTheSameServerScope_RunsOnlyTheSecond`; AC-3
`ThrowingAfterCommitHandler_IsSwallowed_…` + `TokenCancelledAfterTheEntryCallSucceeds_DrainStillRuns`;
AC-4 `EventsRaisedByAfterCommitHandlers_JoinTheSameResponsesRelayBatch`; AC-5
`FactoryEventPhaseCoordinatorTests`, plus `FactoryEventPhaseSchedulerTests.DrainAsync_NeverDrainedWarning_TellsTheConsumerWhereToPutTheDrain`;
AC-6 `FactoryEventsDispatcherPhaseTests.Raise_PhasedHandlerOutsideAnyFactoryCall_DispatchesImmediately`
(the **9005** fallback pin — *not* 9009, which is the coordinator short-circuit and a different
scenario); AC-7 the full
suite green with existing assertions unmodified; AC-8 `FactoryEventPhaseCoalescingTests`;
AC-9 the registrar-holder pins + `RemoteFactory.TrimmingTests`; AC-10 PHASE-005, kept current
through 007/008/011. The close-out audit traces these independently.*

## Punchlist

*(none — this todo predates the tier. Items that would have been punchlist rows were worked
inside plans 007, 008 and 011, which is a large part of why those three grew as they did.)*

## Dismissed

- PHASE-011 · Bare BCL tokens + missing `using System;` injection in generated output (former row 013) · Serves no AC; not reachable — needs a consumer namespace literally named `System`, and `ImplicitUsings` (on by default) masks the injection gap.
- PHASE-011 · `Types.cs:860` takes parameter types from source text (`ToFullString()`) · Serves no AC; no observed failure or live caller — works today because the generator copies the consumer's usings. Fragile, not broken.
- PHASE-008 · Mechanical guard against `FactoryEventHandlerRegistry.Clear()` reaching the suite (former row 011 T1) · Superseded — PHASE-011 deleted the method, so there is nothing left to guard.

## Out of Scope

- Cross-event coalescing ("any of these four events → one recompute") — proposal
  explicitly defers it; handlers can guard internally. *Same-event* coalescing is NOT
  out of scope: the user carved it in as PHASE-006 (2026-08-14, "implement only if
  001–005 land smoothly" — condition met 2026-08-17).
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

| # | File | Title | Status | PR |
|---|------|-------|--------|----|
| 001 | [001-phase-model-and-queueing](./plans/001-phase-model-and-queueing.md) | DispatchPhase enum, registry phase, dispatcher queueing | Done | #79 |
| 002 | [002-generator-phase-passthrough](./plans/002-generator-phase-passthrough.md) | Generator reads phase from attribute, threads to registration | Done | #81 |
| 003 | [003-aftercommit-entry-call-drain](./plans/003-aftercommit-entry-call-drain.md) | Entry-call tracking in generated factories; AfterCommit drain | Done | #80 |
| 004 | [004-afterflush-coordinator](./plans/004-afterflush-coordinator.md) | IFactoryEventPhaseCoordinator public API + fallback drain | Done | #82 |
| 005 | [005-design-docs-skill](./plans/005-design-docs-skill.md) | Design projects, published docs, skill reference | Done | #83 |
| 006 | [006-coalescing](./plans/006-coalescing.md) | Opt-in same-event coalescing (v2, queued per user) | Done | #84 |
| 007 | [007-tech-debt](./plans/007-tech-debt.md) | Tech debt: emission + documenting pins, coordinator short-circuit observability, Design.Server test, harness consolidation | Done | #85 |
| 008 | [008-arc-tail](./plans/008-arc-tail.md) | Arc tail (folds former rows 009 + 010): 9007's drain-*placement* qualifier; generator emission qualification + partial-attribute probe + test-helper CS-error check + misfiled test class; deterministic scheduler concurrency harness and the routed items on that class | Done | #86 |
| 009 | *(folded)* | Scheduler concurrency harness — **Retired**, folded into PHASE-008 | Retired | — |
| 010 | *(folded)* | 9007 drain-placement qualifier — **Retired**, folded into PHASE-008 | Retired | — |
| 011 | [011-hardening](./plans/011-hardening.md) | Hardening (folds former row 012): remove `FactoryEventHandlerRegistry.Clear()` rather than document it; namespace-shadowing compile guards for the class, interface, static and event-preservation legs — which found a live wrong-type binding, not just a regression guard | Done | #87 |
| 012 | *(folded)* | Namespace-shadowing guards for the other renderers — **Retired**, folded into PHASE-011 | Retired | — |

---

## Discovery Log

### 2026-08-31 — PHASE-011 (code review: the red-proof log carried a confident, unmeasured mechanism — the exact thing it exists to prevent)

- **Finding:** Code review returned **CLEAN**, no vetoes, five callouts. C1 is the one worth
  keeping: RP-2 recorded the sync `[Execute]` as green because the static renderer "wraps
  both shapes in `Task<>` so they converge." **They never converge** — a non-`Task`
  `[Execute]` is an NF0102 *error* and is skipped before any delegate is built, so the method
  never reached the renderer. The fixture didn't notice either, because the shared assert
  helper discards the generator-diagnostic element and checks only the output compilation.
  C2 was a live consumer-visible regression this plan introduced: `MethodInfo.ReturnType` is
  NF0102's message argument, so `[Execute] public static Payload Run(…)` began reporting
  ``not 'global::MyApp.Payload'`` — unpinned, because the existing fixture returns `string`,
  which renders identically under both formats.
- **Decision:** Amend — C2 fixed and pinned (RP-7, sole coverage); `_DoWorkSync` removed and
  its false mechanism deleted rather than reworded, since the true statement is *narrower*
  (the non-`Task` line is dead on the `[Execute]` path; reachability via non-`Task` interface
  methods is an **open question**). C3's terminology corrected in four places — `ToString()`
  is namespace-qualified *without* `global::`, not "minimally qualified," and the difference
  is load-bearing: a bare `Payload` would have bound *correctly*. C4 doc fixed; C5's wrong
  authority struck through rather than swapped. Unit 762 → 763. **Plan Done.**
- **Follow-up:** [reviews/011-code-review.md](./reviews/011-code-review.md). Worth keeping:
  this is the arc's signature failure mode — a confident sentence with no run behind it —
  found *inside the red-proof log*, whose entire purpose is to stop that. Fifth instance in
  the arc, first located in a log rather than a test. The tell is unchanged and now sharper:
  a **causal explanation of a measurement, written without measuring the explanation**. Also
  carried to Step 7: the reviewer asked the close-out to *confirm* rather than inherit the
  judgement that this emission change needs no Design-project update.

### 2026-08-31 — PHASE-011 (gate round 1: a guard that could not fail, and four green sabotages that were each the finding)

- **Finding:** The gate returned **2 must-cover**, no vetoes. M1: the event-preservation
  guard was **vacuous and a duplicate of the class-factory guard** — that renderer emits
  into `namespace {SanitizeNamespace(assemblyName)}`, not the consumer's namespace, so both
  decoys were unreachable; and its anti-vacuity assertion matched the *class-factory* output
  for the same type, so it would have stayed green if the preservation renderer stopped
  emitting entirely. M2: all four tests carried `Regression guard` in their XML, including
  the two measured as catching CS0738 and CS0029, while the plan's Test Evidence claimed the
  split had been made there — the record was wrong on the one bullet whose entire content is
  accuracy of the record.
- **Decision:** Amend — both must-cover closed, S1 attempted and declared unmeasured, four
  tech-debt accuracy defects fixed in place. The preservation guard is now labeled a **smoke
  test**: RP-3…RP-6 established it cannot catch a consumer-type mis-binding at all, because
  `DtoConstructorRegistry.Register<T>`/`PreserveType<T>` carry **no type constraint**, so a
  wrong binding compiles. That leg's real coverage is `EventPreservationDiscoveryTests`.
- **Follow-up:** [reviews/011-test-review.md](./reviews/011-test-review.md);
  [reviews/011-redproof.log](./reviews/011-redproof.log) RP-2…RP-6. Worth keeping: **five
  sabotages this round, four green against prediction, and none of them a bad sabotage.**
  Each green was the finding — the wrong bucket, the undiscovered event, the missing
  constraint. Stopping at the first green would have shipped a fourth "guard" whose label
  implied coverage it could never provide. The parallel code review **failed** (stalled with
  no output) and is being relaunched against the corrected state.

### 2026-08-31 — PHASE-011 (the "regression guard" row was hiding a live wrong-type binding, and the prediction that it wouldn't was mine)

- **Finding:** Row 012 was queued as cloning a guard across four legs that pre-flight
  predicted were already correct — the `global::` strip that caused the relay bug is
  relay-only. **All four reddened.** The premise was right and the conclusion wrong: the
  other legs never *asked* for qualification. `FactoryGenerator.Types.cs:696` and `:706`
  took delegate and method return types via `ITypeSymbol.ToString()`, which renders a
  **minimally qualified** name, while `:736` three lines below already used
  `FullyQualifiedFormat`. A consumer type therefore bound to a shadowing decoy — CS0029 on
  the static leg, CS0738 on the interface leg. Same defect class PHASE-008 fixed on the
  relay leg, reached by the opposite route: there a strip removed qualification, here it was
  never requested.
- **Decision:** Amend — both return-type assignments qualified (A1). Row 011's own item
  resolved by **deleting** `FactoryEventHandlerRegistry.Clear()` rather than documenting it:
  it was `internal`, uncalled, and its stated rationale (a single-threaded host) describes a
  caller that cannot exist, since `internal` limits reach to this repo's test projects plus
  AspNetCore. The BCL-token half of the fixture — 128 bare occurrences across four renderers,
  plus the discovery that no renderer injects `using System;` — was **removed and queued as
  row 013** (A2/A3) rather than swept in at arc-end.
- **Index changes:** 011 now points at the plan; 012 `Retired` into it; 013 added as Draft.
- **Follow-up:** [plans/011-hardening.md](./plans/011-hardening.md),
  [reviews/011-redproof.log](./reviews/011-redproof.log). Sixth wrong prediction in this arc
  and the most valuable: it was written into the plan's Notes *specifically so the run could
  kill it*, and it did. Worth keeping — the failure mode here is not "I didn't check" but "I
  checked one mechanism and generalised from it." Pre-flight verified the strip was
  relay-only and stopped, without asking whether the other legs qualified by some *other*
  route. Reading one line further in the same method would have shown `:736` doing it
  correctly next to `:696` doing it wrong.

### 2026-08-27 — PHASE-008 (gate round 1: the plan's own fix had introduced a silent-drop regression, and only a code read caught it)

- **Finding:** The gate returned **2 must-cover**, both in code this plan introduced. M1 is
  the one that justifies the step: `IsCanonicalDeclaration` — the fix for the split-partial
  CS8785 — chose its canonical declaration from *all* of `symbol.DeclaringSyntaxReferences`,
  while `ForAttributeWithMetadataName` only ever yields **attributed** nodes. A partial class
  carrying its attribute on a later declaration therefore matched nothing and emitted
  **nothing, with no diagnostic** — strictly worse than the loud CS8785 it replaced. Found by
  reading code, with nothing run, in the plan that had declined plan review. M2: no test drove
  two overlapping *entry calls*, though the Acceptance bullet claimed entry-state semantics
  and the evidence row was ticked.
- **Decision:** Amend — both must-cover fixed and all 5 should-cover plus the nice-to-have
  closed; the two tech-debt items queued as new rows **011** and **012** rather than absorbed.
  Unit 755 → 758.
- **Follow-up:** [reviews/008-test-review.md](./reviews/008-test-review.md);
  [reviews/008-redproof.log](./reviews/008-redproof.log) RP-8-rerun and RP-9…RP-11. Two
  lessons, both about how evidence was *scoped* rather than whether it existed. **RP-8 had
  been run filtered to the class under test**, which answers "does my test go red" while
  declining to answer "was it already covered" — re-run at full scope it turned 16 red,
  including two pre-existing pins the reviewer had named from a code read, so the new test is
  a second witness. And **RP-10 disproved the rationale the M2 closures were written with**:
  flow A's drain is total, so the queue is already empty when its exit clear runs and the
  deferred clear is unobservable on the success path. RP-11 then established what the pair
  actually pins — depth accounting across an in-flight drain. Tests kept, remarks rewritten to
  the measured answer, disproved claims left visible.

### 2026-08-27 — PHASE-008 (three routed remedies, two of them wrong — the routing said what to do, the measurement said what was true)

- **Finding:** Of the concurrency-half items this plan inherited, two named a remedy that
  measurement rejected. The registry-isolation item asked for `Clear()` to become the
  enforceable escape hatch; a test calling it turned an existing `FactoryEntryCallTests`
  case red (passes alone, fails beside the new one) — the registry is process-wide static
  and xUnit runs classes in parallel, so the routed hatch actively breaks the suite. And
  the re-entrant-`Equals` item's sharpest predicted consequence — one coalescing identity
  holding two pending dispatches — **does not happen**: the re-entrant `Enqueue` runs its
  own identity scan against the live queue and collapses, so the contract survives.
- **Decision:** Amend — A3 pins the property the discipline actually rests on (entries
  keyed by `(event type, handler class)`) and writes the correction onto `Clear()`'s own
  XML doc; the re-entrancy test is kept and **inverted**, carrying the disproved reasoning
  in its remarks. A4 fixed both allocation items rather than accepting them —
  `TryDequeueThrough`'s `Where`+`OrderBy` ran per dequeued dispatch, the cost PHASE-007's
  O(1) fix did not touch.
- **Follow-up:** [reviews/008-redproof.log](./reviews/008-redproof.log), RP-5 through RP-8.
  Worth keeping: this arc has spent eleven entries on tests that could not go red, and this
  is the mirror image — a *routed remedy* that could not be right, surviving three gates as
  a plausible sentence because nobody had run it. The tell is the same one, pointed at
  planning rather than at tests: a confident instruction with no measurement behind it.

### 2026-08-27 — PHASE-008 (the inferred collision was real, and it takes the whole assembly with it)

- **Finding:** PHASE-002 inferred that attributes split across partial declarations "should
  collide on hint name" and recorded it unmeasured. Measured: they do. Two partials each
  carrying a `[FactoryEventHandler<T>]` yield two syntax nodes, two identical models (the
  transform reads the *symbol*), one hint name, and an `ArgumentException` on the second
  `AddSource` → **CS8785**. The severity is the part the row understated: CS8785 means the
  generator "will not contribute to the output," so every factory in the assembly vanishes and
  the consumer sees missing-type errors pointing nowhere near the split partial.
- **Decision:** Amend — one model per symbol from a canonically-chosen declaration (plan
  amendment A1). Two adjacent corrections in the same pass: the emission fix covers three token
  classes rather than the one routed (A2), and this plan's own pre-flight got the
  service-parameter case backwards, claiming a strip that exists one line below the line it
  cited.
- **Follow-up:** [reviews/008-redproof.log](./reviews/008-redproof.log) — RP-3 is unusual in
  being a *probe* whose "before" run is the positive control, which is stronger than a
  sabotage because nothing about it was built to fail. RP-4 is the arc's twelfth can't-go-red
  instance and the second one authored by the plan hunting for them: a single test claimed to
  pin the helper's base-fixture guard while the appended-edit compilation *contains* the base
  fixture, so deleting the first guard left the assertion satisfied by the second. Split into
  two tests that each name which input failed.

### 2026-08-27 — PHASE-008 (the arc tail merges: three rows, one plan, one branch — ceremony was the cost being cut)

- **Finding:** The three remaining rows are the whole arc tail and carry no ordering
  dependency on each other, but the workflow's per-plan gate meant three of everything —
  three Test Evidence maps, three mandatory gates, three review files, three log entries —
  for what is one trivial message edit, one mostly-mechanical generator sweep, and one
  genuine harness design. Merging the plans is the only move that cuts the ceremony, since
  the gate is per plan and not per branch.
- **Decision:** Re-split — rows 009 and 010 fold into 008, which widens to the full tail;
  their per-item provenance transplants into the new plan's Inherited section so retiring the
  rows loses no routing history. Plan review declined **by user direction, not by risk
  assessment** — this arc's plan reviews returned 4–6 vetoes on nearly every plan that ran
  one, and 008 carries thirteen acceptance bullets; the mandatory Step 5 gate is the backstop.
  Mitigation for the merge: implement cheap-and-deterministic first (message, emission,
  helper, rename), harness last, so it can still split back out without stranding anything.
- **Index changes:** 008 rewritten as the merged arc-tail plan (`Draft`); 009 and 010 kept as
  `Retired` tombstones pointing at it, because committed review files cite both by name.
- **Follow-up:** [plans/008-arc-tail.md](./plans/008-arc-tail.md). One in-source pointer —
  the scheduler's `_gate` comment routing to PHASE-009 — goes stale on this merge and is
  Step 1 of the plan; it is the fourth instance of this arc's incidental-doc-invalidation
  species, and the first one caught *before* it shipped rather than at review.

### 2026-08-18 — PHASE-007 (code review: the new log event's explanation was wrong about the code it was explaining — and a trace stopped me changing code I had already decided to change)

- **Finding:** Code review returned **1 veto** and 9 callouts. V1: 9009's shipped
  message and its CLAUDE-DESIGN row explained the short-circuit by saying a drain
  wrapping the factory call from outside "runs before the work it means to flush has
  been queued." That is false for the after-the-call case, which is the one the
  sentence describes: the dispatcher queues only while an entry call is active, and
  `EndEntryCallAsync(true)` always drains at the outermost exit — so the work was
  queued and had already been swept, and the 9007 fired *earlier*, not later. The
  event is still worth having and its actionable half was right; the causal story
  shipped wrong, in the one consumer-facing contract this plan adds. C3 was the
  familiar sibling: moving the composition out of `Program.cs` orphaned that file's
  own "the server only needs…" list and CLAUDE-DESIGN's Key Files row — the third
  incidental-doc-invalidation catch in this arc.
- **Decision:** Amend — V1 closed in all three places after verifying the trace
  myself; C1 (a can't-go-red `{Phase}` assertion inside the plan's own headline
  pins), C3, C4, C5, C7, C8, C9 closed; C6 routed to PHASE-009; C2 became new Index
  row **010** (9007's Warning needs the drain-*placement* qualifier, because 9009
  carries it only at Debug and the consumer who needs it most runs at Information).
- **Follow-up:** [reviews/007-code-review.md](./reviews/007-code-review.md). Worth
  keeping, and the opposite of this arc's usual lesson: I had independently decided
  the `ReadOnlySpan` over the live backing array was a regression and was holding a
  fix for it. The reviewer's trace showed it is safe — a re-entrant grow leaves the
  span on a live GC-tracked array, `Clear()` zeroes rather than shrinks, and a
  blanked slot short-circuits on `ReferenceEquals` — so **the change was not made**
  and the two real deltas were documented instead. "Verify, don't inherit" cuts both
  ways: my own confident diagnosis needed a trace before it justified touching the
  arc's most safety-critical class.

### 2026-08-18 — PHASE-007 (gate round 1: the correction of a reasoning-dressed-as-evidence finding was itself reasoning dressed as evidence)

- **Finding:** The gate returned **2 must-cover**, both in code this plan itself
  introduced — 9009's DI wiring (both pins built the coordinator by hand, so
  dropping the logger factory from the registration silenced the feature in every
  real application with the suite green) and `PhaseQueue.Replace`'s head offset (the
  warn-merge's only write path, exercised by every merge test at cursor zero where
  the offset is a no-op). The sharpest, though, was a should-cover: **Plan Amendment
  A4 was false.** PHASE-006's code review C5 said the warn-merge pins drive states
  the dispatcher never produces; A4 answered by arguing the state is *unreachable*,
  resting on "every drain sweeps earliest-phase-first and runs until empty." The
  in-transaction branch of `DrainAsync` has no catch — a handler exception abandons
  the queue, as a test pinned since PHASE-001 says — so the merge is reachable
  through the drain point that ships today, and C5's complaint stood.
- **Decision:** Amend — all 2 must-cover and all 5 should-cover closed; 4 of 6
  nice-to-haves taken. A4 **retracted** in the plan and the in-file reachability
  block rewritten; `Coalesce_AbortedConsumerDrain_…StillWarns9007` is the
  production-shaped variant C5 asked for and closes the `Replace` finding too. The
  Design.Server seam widened to include the framework registration call, because the
  test had been restating it (a drifting assembly argument would have gone unnoticed).
  Three sabotages measured (RP-5/6/7), one of which — RP-7 — **came back green** on
  its first attempt because a fully-drained queue resets the cursor, so the discard
  test was rebuilt around an aborted drain. Unit 740 → 743.
- **Follow-up:** [reviews/007-test-review.md](./reviews/007-test-review.md). Worth
  keeping: this is the arc's reasoning-dressed-as-evidence failure mode appearing
  *inside a correction of that same failure mode* — the first attempt replaced C5's
  wrong remedy with a wrong claim instead of with a test. PHASE-004's round 2 hit the
  identical recursion. The tell is unchanged and now has a third instance behind it:
  a confident sentence about what code does, with no run behind it.
  **Round 2 (2026-08-18): gate closed at must- and should-cover** — every closure
  verified by re-tracing rather than by citation, and each sabotage log checked by
  failing-test *name*, not count. It caught the retracted A4 claim still standing
  verbatim in the red-proof log's round-1 section, ~35 lines below its own
  retraction (the stale-sentence species PHASE-004's RP-3 and PHASE-005's RP-0 both
  hit; corrected in place), and it sharpened this entry's own lesson. **Three
  predictions in this plan were wrong** (RP-2 twice, RP-7 once), and the first
  diagnosis — "a cursor change needs a test that leaves the structure partially
  consumed" — is true as a slogan but wrong as a procedure: RP-2 round 2 *did* leave
  the queue partially consumed and still came back green, because `Dequeue` blanks
  the slot it vacates. The invariant that covers all three: **every `_head`-dependent
  member is observable only inside the window `0 < _head < _items.Count`, and two
  independent housekeeping actions attack it — `Clear()` collapses the window,
  blanking neuters its contents — so a cursor test must leave the queue partially
  consumed *and* assert something the blanking cannot also satisfy.** The audit of
  every `PhaseQueue` member found one thing nothing can fail on (the `Clear()` call
  inside `Dequeue`, pure memory hygiene); by the reviewer's own recommendation that
  is recorded in the comment as unpinnable rather than given a brittle white-box
  test.

### 2026-08-18 — PHASE-007 (pre-flight and implementation: the sample server could not serve the domain it hosts, and the convention that would have fixed it registers transient)

- **Finding:** Design.Server had **no tests at all** and was missing four server-only
  registrations, not the two the row recorded. Closing the gap with
  `RegisterMatchingName` — the convention `Person.Server` uses and the docs teach —
  produced a *second*, quieter defect: it registers **transient**, so the factory
  method, each handler, and the assertion each held a different `IPhaseAuditService`;
  every phase ran and the audit read back empty. The resolution-only test passed in
  both states. Separately, pre-flight found that two log ids the row called unpinned
  were not: 9005 was already pinned, and 9004's only reference matched `9005 || 9004`
  and so could not discriminate between the two fallbacks at all.
- **Decision:** Implement — the registrations moved to a seam
  (`ServerServices.AddDesignServer`) that `Program.cs` and the new composition test
  both call, so the check runs against the server's own list rather than a copy;
  stateful services registered explicitly as scoped with the failure mode written
  where the next person adds one. The reflective `[Service]`-enumeration drift
  detector the plan's Acceptance implied was **rejected under the no-reflection
  rule** and its residual stated rather than papered over.
- **Follow-up:** [reviews/007-redproof.log](./reviews/007-redproof.log) (local-only
  evidence per the 2026-08-17 ruling). Worth keeping: `RegisterMatchingName` is a
  transient convention, and the repo teaches it in several places without saying so —
  a candidate doc fix beyond this arc. Also: RP-4 measured that swapping
  `ClientServerContainers`' tuple order silently sends **35** integration tests the
  wrong container with no compiler complaint, which is why 007 pinned the three
  orders instead of aligning them.

### 2026-08-18 — PHASE-007 drafted; PHASE-009 re-split (scheduler concurrency gets its own plan)

- **Finding:** Drafting the 007 row's seventeen accumulated items forced the queued
  re-split decision early: 006's gate round 1 sent the scheduler-concurrency candidacy
  "to the close-out re-split decision," but 007's Scope had to either contain that work
  or exclude it, so the call could not wait. It does not belong in 007 — every 007 item
  is a pin, a documenting test, or a consolidation of harnesses over *existing* behavior,
  while the concurrency work needs a deterministic harness designed from scratch (both
  006 reviewers said so explicitly, warning against a `Task.WhenAll` race), and 006 code
  review C4 raised its stakes: the coalescing identity scan now runs consumer `Equals`
  under `_gate`.
- **Decision:** Re-split — new Index row 009 rather than widening 007, the same call the
  008 re-split made for the same reason (both of that round's reviewers noted 007 already
  carried too many unrelated items). 007 drafted with plan-review opt-out (pins and
  harness work; the one behavior addition is a Debug log event) and code-review opt-in
  (it touches sacred harness files broadly). Branch `PHASE-007-tech-debt` stacked on
  `PHASE-006-coalescing` (PR #84 open at branch time) — several items pin 006's code.
- **Follow-up:** [plans/007-tech-debt.md](./plans/007-tech-debt.md) — the routed items
  now live in its Inherited section with per-item gate provenance. The 009 row carries
  the concurrency provenance, including the 003 round-2 N1 timing window as a candidate
  pin for the deterministic harness.

### 2026-08-18 — PHASE-006 (code review: the veto-adopted constraint's own branch was dead code to the suite — and the anchor list that guarded against stale docs was itself incomplete)

- **Finding:** Code review returned zero vetoes and seven callouts. C1, the sharpest:
  the warn-preserving merge's true→false branch — the mechanism behind the plan's #1
  veto-adopted constraint — was **dead code as far as the suite was concerned**.
  RP-1 had measured the flip direction (a latest-bit-wins merge erasing a warning),
  but no test ordered the mid-drain raise *first*, so deleting the merge assignment
  outright left all 728/591/94 green. Eleventh "can't go red" instance in the arc,
  and the first found sitting directly on a constraint a red-proof had already
  "covered" from the other side. C2, the transferable one: CLAUDE-DESIGN's narrative
  diagnostics bullet and pass-through bullet were a sixth and seventh
  survivor-species string that plan review A-V1's five-string enumeration missed —
  the enumeration that exists to prevent incidental doc invalidation is itself a
  claim that can be incomplete. C4: the coalescing identity scan runs consumer
  `Equals` under `_gate`, contradicting the class's own "handlers are invoked outside
  the lock" comment.
- **Decision:** Amend — C1 closed with the mirror-ordering pin and the omission
  sabotage **measured as RP-3** (exactly the predicted 1 red ×2 TFMs); C2's two
  bullets fixed; C3/C4/C6/C7 comment and XML corrections in place; C3's
  storage-shape question (O(n) front-dequeue paid by the non-opted-in path on an
  unenforced smallness assumption) and C5's synthetic-state note (the warn pin
  drives `Enqueue(Immediate, …)`, which the dispatcher never produces) routed to
  PHASE-007. Unit 728 → 729.
- **Follow-up:** [reviews/006-code-review.md](./reviews/006-code-review.md). Worth
  keeping: a red-proof that measures one direction of a two-directional mechanism
  reads as full coverage — RP-1's "signature exact" was true and still left the
  deletion sabotage green. The tell is a guard whose *taken* branch no test drives,
  which is checkable mechanically (branch coverage would have shown it).

### 2026-08-18 — PHASE-006 (gate round 1: clean at must-cover; the survivor's payload was contract nobody had stated)

- **Finding:** The test-review gate returned **zero must-cover findings** and verified
  the logs by count and both red-proofs as genuine positive controls. Its sharpest
  should-cover: *which collapsed instance the handler receives* is consumer-visible
  contract under the documented custom-`Equals` over-collapse hazard — and it was
  neither stated in any doc nor pinned by any test; a latest-wins refactor would have
  changed delivered payloads with the whole suite green. Siblings: B-V3's
  reference-typed-member no-op had four doc surfaces and zero executable evidence, and
  the todo-AC relay-unaffected clause was structurally true but unpinned against the
  cross-event future that would break it. Tech debt surfaced: the scheduler has zero
  concurrency coverage against its own shared-scope contract (predates the arc;
  candidate for its own plan, deterministic harness).
- **Decision:** Amend — all three should-covers closed with tests (first-raised
  survivor pinned via an Id-only-`Equals` event + the contract sentence added to the
  attribute XML; the no-op hazard made executable; relay-unaffected pinned end to end:
  3 relayed events, 1 handler run). Evidence row 9's "and nothing else" understatement
  corrected. Unit 726 → 728, integration 590 → 591.
- **Follow-up:** [reviews/006-test-review.md](./reviews/006-test-review.md) — full
  disposition; nice-to-haves and the tech-debt items routed to the PHASE-007 row
  (which now also records its `Clear()`/snapshot items gaining dependents). The
  scheduler-concurrency plan candidacy goes to the close-out re-split decision.

### 2026-08-18 — PHASE-006 (plan review: the collapse can delete a promised warning, and a ninth "can't go red" caught at draft)

- **Finding:** Plan review returned CONCERNS — 4 vetoes. The sharpest: `QueuedDispatch`
  is not just (handler, event) — the `EnqueuedMidDrain` bit is load-bearing for 9007,
  and a collapse keyed without it silently picks one warn-bit; latest-wins erases the
  warning todo AC-5 promises, with no trace. Also: the draft's composition bullet
  bundled three claims of which the discard leg was green against a do-nothing
  implementation (the discriminating 9006 count depended on an undecided design
  question — the ninth "can't go red" instance, caught at draft like the sixth);
  "events are records → value equality" is a false universal (reference-typed payloads
  silently defeat coalescing for exactly the motivating shape; custom `Equals` can
  over-collapse); and the NF0504 survivor rule is published in five strings the doc
  step hadn't named — the incidental-invalidation species from PHASE-004's code
  review, nearly repeated.
- **Decision:** Amend — all 4 vetoes adopted by draft amendment before implementation:
  warn-preserving merge as a Constraint with a red-proof-required pin; the bullet
  split, which forced settling the collapse point to pending-queue semantics (counts
  reflect the collapsed state — the reviewer showed falsifiability and that design
  choice were coupled, not independent); identity restated as the `Equals` contract
  with both hazards; Step 7 widened to the five survivor-rule strings. All 10
  callouts folded in — including that two of the draft's five "open questions" were
  already answered elsewhere in the draft itself, and that `Enqueue`'s 53 pinned call
  sites force the overload shape.
- **Follow-up:** [reviews/006-plan-review.md](./reviews/006-plan-review.md) — full
  disposition. Todo edits in this entry's commit: coalescing AC bullet added (tracing
  the 2026-08-14 user decision that queued 006), Out of Scope bullet now
  distinguishes cross-event (out) from same-event (carved in).

### 2026-08-17 — PHASE-005 (gate round 1: clean at must-cover; the demonstration's own handlers had never been observed running)

- **Finding:** The test-review gate returned **zero must-cover findings** and verified
  the evidence map independently (its decisive check: all five expected sequences go
  red under a generator phase-pass-through regression — the plan's charter is pinned,
  not assumed). The sharpest should-cover: the discard demonstration's load-bearing
  content is the *absence* of two markers, and nothing in the Design assembly ever ran
  `pay-flush`/`pay-commit` — delete a Payment handler class and the test stays green
  while demonstrating nothing. Its sibling: "discarded" vs. "leaked" was
  undiscriminated at this tier because no second call follows in the scope.
- **Decision:** Amend — one edit closed both (a `reject` flag on `PaymentIntake._Record`
  plus `PaymentIntake_FailedThenSuccessfulCall_SameScope_DiscardsRatherThanLeaks`:
  success path asserts all four markers; the rejected call's trail must not grow
  during the survivor's drains). Three nice-to-haves taken (9007 prose softened to
  name where the emission is actually pinned; Remote-mode coordinator-absent DI pin;
  `Finalize` round-trip `Id`/`Total` assertions). Design 91 → 93.
- **Follow-up:** [reviews/005-test-review.md](./reviews/005-test-review.md) — full
  disposition. Routed to PHASE-007: a Design.Server composition test (it registers 3
  services while Design.Domain `[Service]`-injects `INotificationService` — drift
  predating this plan — and now `IPhaseAuditService`; no Design.Server test exists at
  all), and the `FactoryEventHandlerTests` `Assert.True(true)` trio, which after this
  plan's rescope is the Design tier's nominal `Immediate` pin yet asserts nothing.
  **Escalated to the user:** `.gitignore:94` (`*.log`) keeps every arc's
  `reviews/*.log` evidence out of the repo — the todo docs cite files that exist on
  one machine, and the Step 7 close-out audit will cite them again. Fix is an ignore
  exception for `docs/todos/**/reviews/*.log` (or `.md` extensions); arc-level call.
  **Ruled 2026-08-17: leave as local-only evidence** — the `.log` files stay
  gitignored; the committed gate records (`reviews/*-test-review.md` etc.) carry the
  numbers and verdicts, and the logs back them up on the machine that ran them. The
  close-out audit cites the logs as local artifacts, not repo files.
  **Round 2 (2026-08-17): all six closures confirmed, nothing reopened — gate closed
  at must- and should-cover.** The reviewer traced the no-leak discriminator
  mechanically (event-id attribution means the second assertion catches a leak and
  cannot be masked by the first; the shipped sweep-earlier-phases semantics route a
  leaked queue into the survivor's drain → red) rather than accepting the inheritance
  argument, and confirmed the round-2 red-proof addendum plainly labels its
  not-measured claim — the arc's reasoning-dressed-as-evidence failure mode, not
  recurring this time. One residual fixed in place: RP-0's rule sentence said "91"
  above an addendum saying 93 — the stale-gate-rule species, caught before it aged.

### 2026-08-17 — PHASE-005 (the main solution does not contain the Design projects — a green run without your tests in it)

- **Finding:** After writing PHASE-005's five new Design tests, the first test run
  reported **86/86 green — the pre-plan count**, new tests absent. Cause:
  `src/Neatoo.RemoteFactory.sln` does not include the Design projects (they live in
  `src/Design/Design.sln`), so the solution build "succeeded" without compiling the
  new files, and `dotnet test --no-build` ran the Aug-15 DLLs. Eighth "can't go red"
  instance in the arc, and the first produced by *solution topology* rather than test
  design — every assertion was sound; the binaries just predated them. What caught it
  was the test **count**, not a failure.
- **Decision:** Amend (procedural) — this plan's gate builds both solutions into
  `005-build.log` and requires the Design total to read 91 (86 + 5). Recorded as RP-0
  in [reviews/005-redproof.log](./reviews/005-redproof.log); RP-1 then measured the
  load-bearing drain-position discriminator with a compiling sabotage (exact predicted
  2-red signature on both TFMs).
- **Follow-up:** The Step 7 close-out audit's verification run must build
  `Design.sln` explicitly — CLAUDE.md's build commands mention only the main
  solution, which is how this trap arms itself. Candidate PHASE-007 harness item.

### 2026-08-16 — PHASE-004 (the `Internal` namespace is a warning, not a wall — and the repo already said so)

- **Finding:** User review of the open PR: `FactoryEventPhaseCoordinator` shipped
  `internal sealed`, against the framework's extensibility policy — `Internal` conveys
  "extend at your own risk," but nothing is to be cut off. The repo already followed this
  (`FactoryEntryCall` is `public static` in `Internal`; `IFactoryEventPhaseScheduler` is
  `public` in `Internal`) while both implementations behind those types were
  `internal sealed`. The convention existed; it was just never written down, so this plan
  reproduced the exception rather than the rule.
- **Decision:** Amend — coordinator and scheduler both `public` and unsealed; the
  coordinator's `DrainAsync` is `virtual` with a `protected` scheduler property; the
  scheduler's members stay non-virtual with the reason stated in XML (interlocking
  contract, replace via the interface). Policy written into `CLAUDE-DESIGN.md` so the next
  type in that namespace inherits the rule instead of a coin flip.
- **Follow-up:** Verified against EF Core 10.0.3 rather than asserted: ~4,500 public
  documented members in `*.Internal`, `DbContextServices` is `public` and unsealed with no
  marker attribute — the namespace alone. **The gap worth acting on:** EF's policy has a
  third leg this repo lacks — `InternalUsageDiagnosticAnalyzer` (EF1001, `Usage`, Warning
  by default) flags consumer code touching `*.Internal` *at the point of use*. Without it
  the warning only reaches people who read XML docs. Candidate for its own plan; the
  generator's NF-diagnostic infrastructure already exists. Also worth a sweep: ~20 other
  types in `Neatoo.RemoteFactory.Internal` have not been audited against this policy.

### 2026-08-16 — PHASE-004 (code review: the plan restated the AC it was chartered to restate, and missed the one it broke)

- **Finding:** Code review V1. PHASE-004 falsified **two** acceptance criteria and restated
  one. AC-3's restatement was chartered, anticipated, executed with provenance. AC-1 —
  "all `Immediate` handlers complete before any `AfterFlush` handler" — was falsified as a
  side effect of creating the in-body consumer drain point, and a test this plan shipped
  asserts the contradiction outright (`["ord-immediate", "ord-flush", "ord-immediate", …]`).
  The code side had been handled correctly: plan review A-C3 caught the ordering sentence
  and `DispatchPhase.cs` was rescoped. Nobody carried that same rescoping back to the
  requirements doc — including me, one entry after adopting A-V1, whose whole content was
  "the requirements doc must not contradict shipped behavior."
- **Decision:** Amend — AC-1 restated in AC-3's exact form with provenance; plan Acceptance
  bullet 3 reworded to the five-marker sequence its test asserts. Four callouts closed in
  place (two public-XML precision fixes shipped now as permanent contract text, the plan's
  carve-out Constraint widened to the shipped "any drain in flight" rule).
- **Follow-up:** [reviews/004-code-review.md](./reviews/004-code-review.md). The pattern
  worth carrying: an *expected* doc invalidation gets tracked and executed; an *incidental*
  one — same plan, same file, discovered by the same review lineage — slips, because the
  attention goes to the change that was planned for. When a plan restates one AC, that is
  the moment to re-read all of them. PHASE-007 also grew an item: the coordinator's
  short-circuit is silent, so a consumer who wires the drain *outside* the factory call
  gets nothing plus a 9007 telling them to do what they just did.

### 2026-08-15 — PHASE-004 (gate: the case-3 pin ran where production can't, and the red-proof log had an unmeasured claim)

- **Finding:** The test-review gate's must-cover: the A-V2 case-3 warning pin drove a
  bare scheduler with no entry call — a state the dispatcher never produces (it only
  enqueues while an entry call is active) — and the *properly guarded* variant of the
  rejected per-entry-call flag passed the entire suite, because that flag never latches
  without an entry call. The red-proof log even claimed the flag design "would turn
  [the bare test] red" — asserted, never measured, and false for the guarded variant.
  Seventh "can't go red" instance in the arc, and the first found inside the red-proof
  log itself. Two should-covers landed nearby: cooperative cancellation was pinned only
  at the scheduler's post-completion drain (the evidence row cited the wrong drain
  point), and `_activeDrains`-as-counter was a load-bearing comment a bool satisfied.
- **Decision:** Amend — all three closed with tests: an entry-call-scoped case-3 pin
  (raise → coordinator drain → raise again → exactly one 9007), a
  cancel-mid-coordinator-drain pin, and an overlapping-drains pin. RP-7 added: the
  guarded flag *actually implemented* and measured — new pin red, bare pin green
  (measuring the gate's diagnosis) — and RP-3's false sentence corrected in place.
  Two nice-to-haves taken (validation-before-short-circuit; the A-C3 interleave raise
  added to the ordering sequences). Unit 701→705.
- **Follow-up:** [reviews/004-test-review.md](./reviews/004-test-review.md) — includes
  routing: the accepted undefined-phase silent no-op gets a documenting pin in
  PHASE-007; `IEventTestService`/`ScopesWithLogging` attribution weaknesses fold into
  PHASE-007's harness items; `SingleEventRelay_ConsumerReceivesEvent`'s hard 2-second
  poll flakes under full-parallel load (2 of 3 full-solution runs today, never
  serialized) — PHASE-007's harness scope grows by that timeout.
  **Round 2 (2026-08-16): clean close** at must- and should-cover — the reviewer traced
  both rejected designs against the shipped source and derived that the new pin catches
  the subtler flag-plus-stamp variant too. It also caught that RP-7's closing sentence
  claimed that variant *without measuring it* — the same species as the RP-3 miss the
  round-1 closure had just corrected, one entry below it in the same file. Measured as
  RP-9 (derivation held); RP-8 added for the counter-vs-bool claim; overlap test given a
  witness dispatch; gate log now records its `-m:1` invocation. Worth keeping: the
  reasoning-dressed-as-evidence habit reappeared *inside the fix for itself*, which
  suggests the tell is the confident sentence with no run behind it, not the topic.

### 2026-08-15 — PHASE-004 (plan review: 5 vetoes, all adopted before implementation)

- **Finding:** Plan review returned CONCERNS. The sharpest: the draft's per-entry-call
  "consumer drained" flag was narrower than AC-5's own wording — work a consumer raises
  *after* their drain is literally "never drained by the consumer," yet the flag silences
  it, and the draft's Intent and Constraints described two different promises without
  noticing (A-V2). Also: the OCE restatement contradicted two `CLAUDE-DESIGN.md` log-table
  rows outside the pre-declared amendment set (A-V1); "framework-owned phases rejected"
  invited a blacklist that would let `(DispatchPhase)99` sweep the AfterCommit queue
  in-transaction through the `p <= through` sweep (B-V1); the attribute-declared
  consumer-drain bullet was green against a no-op coordinator — the arc's sixth "can't go
  red" instance, caught at draft this time (B-V2); and "benign no-op outside an entry
  call" was unfalsifiable while hiding an undecided delegate-vs-short-circuit choice that
  matters under the per-scope concurrency limitation (B-V3).
- **Decision:** Amend — all five vetoes adopted: per-dispatch warning discriminator
  (created-mid-sweep is the only silent case; AC-5 stands as written), CLAUDE-DESIGN rows
  into the amendment set, whitelist validation, before-method-done marker ordering as the
  coordinator's red-proofed discriminator, short-circuit outside entry calls pinned via
  direct `Enqueue`. Reviewer also verified (not inherited) that the AC-3 restatement is
  legitimately chartered (003-code-review C2) and that the choke point's post-invoke
  cancellation check keeps its integration pins green under the OCE change.
- **Follow-up:** [reviews/004-plan-review.md](./reviews/004-plan-review.md) — full
  disposition table. B-C3 note: the draft claimed a unit-tier log-capture harness needed
  building; `FactoryEventPhaseSchedulerTests` already has one pinning 9003 — "verify,
  don't inherit," caught by the reviewer this time.

### 2026-08-15 — PHASE-002 (both gates found the same unfalsifiable assertion)

- **Finding:** The test-review gate (should-cover #2) and the code review (veto V1) landed
  independently on the same test: NF0504's message assertion stacked the *unphased* attribute
  first, so the surviving phase was `Immediate` — which is simultaneously the hardcoded
  default constant, the value the malformed-argument fallback returns, and what a message
  format with the placeholder deleted would print. Green against three distinct wrong
  implementations. Four other bullets *were* red-proofed; this was the one claim neither
  pinned nor declared unpinnable.
- **Decision:** Amend — added the phased-first case (which also pins source-order-wins,
  previously unpinned) and red-proofed it by hardcoding the phase into the message format.
  Three further should-cover gaps closed the same way: the relay leg had **no**
  `IsServerRuntime` assertion anywhere in the unit suite, NF0504's location was unasserted
  while the bullet claimed it, and the cache fixture's phase argument could silently stop
  binding and degrade the fixture with every test still green.
- **Follow-up:** [reviews/002-test-review.md](./reviews/002-test-review.md),
  [reviews/002-code-review.md](./reviews/002-code-review.md),
  [reviews/002-redproof.log](./reviews/002-redproof.log) (second round). Fourth instance of
  the "can't go red" shape in this arc — and the first where red-proofing four discriminators
  did *not* by itself prevent a fifth from slipping through, because the unproofed one was
  not on the list.

### 2026-08-15 — PHASE-002 (a red-proof that disproved its own premise)

- **Finding:** Fixing `DiagnosticTestHelper`'s double-count, I wrote a comment warning that
  `Distinct()` would be a destructive "obvious fix" — it would collapse genuinely repeated
  diagnostics (NF0502 fires once per attribute, same location, identical message) because
  `Diagnostic` has value equality. Red-proofing that claim showed it is **false**: comparison
  behaves by identity, the doubling came from concatenating two collections holding the *same
  instances*, and genuine repeats are distinct instances that survive. `Distinct()` would have
  worked.
- **Decision:** Keep the chosen fix — return the driver's out-param, no concatenation — but on
  the corrected, narrower ground: `Distinct()`'s correctness depends on the two collections
  sharing object identity, a Roslyn implementation detail nothing here controls. Rewrote the
  helper comment and the test remarks, which had stated the disproved claim as fact.
- **Follow-up:** Recorded in [reviews/002-redproof.log](./reviews/002-redproof.log) (RP-8)
  rather than deleted. A confident-and-wrong warning comment in a shared test helper outlives
  whoever wrote it; this one lasted three minutes because it was tested. Worth remembering that
  red-proofing pays out twice — it confirms the tests that go red, and it kills the reasoning
  that turns out to be decoration.

### 2026-08-15 — PHASE-002 (generator emission hygiene — new plan 008)

- **Finding:** Both gates surfaced defects adjacent to this plan but outside it. The sharpest:
  the **event type** token in the same emitted registration statement is *not* `global::`-
  qualified — the identical hazard this plan spent a Constraint, a veto, and a negative pin on,
  three tokens to the left. Also: attributes split across partial declarations should collide
  on hint name (`ForAttributeWithMetadataName` fires per syntax node, the transform reads
  `symbol.GetAttributes()`) — inferred, not measured; `DiagnosticTestHelper.RunGenerator`
  returns every generator diagnostic **twice**, which no current test trips but which silently
  breaks any count assertion; `RunGeneratorTracked` never checks the input compilation for CS
  errors; and `NF04xx…Tests.cs` contains `class NF05xx…Tests`.
- **Decision:** Re-split — new Index row 008 rather than widening PHASE-007, which both
  reviewers noted already carries three unrelated items. Index changes: 008 added as Draft.
- **Follow-up:** Note the ratchet on the `global::` item — five new assertions in this plan
  hardcode the unqualified event-type form and will need editing when 008 lands.

### 2026-08-15 — PHASE-002 (AfterFlush became consumer-reachable ahead of its drain point)

- **Finding:** As of this plan `[FactoryEventHandler<T>(DispatchPhase.AfterFlush)]` is
  expressible by consumers for the first time, and the scheduler's sweep already drains it at
  the AfterCommit entry point — fail-open, but *without* the logged warning PHASE-004's
  AC-5 promises. The feature is live and half-documented in the window between the two plans.
- **Decision:** Accept the window; PHASE-002 does not own the warning. But PHASE-004's
  acceptance must cover an **attribute-declared** AfterFlush handler, not only a
  hand-registered one — otherwise the consumer-facing path stays untested.
- **Follow-up:** recorded in PHASE-004's inherited section.

### 2026-08-15 — PHASE-002 (plan review: the duplicate-attribute severity, settled)

- **Finding:** The draft took the deferred duplicate-same-event decision toward an **Error**
  diagnostic, reasoning from "NF0501/NF0502 are errors". The review found the project's own
  documented precedent pointing the other way: NF0503 chose Warning *explicitly* to keep the
  build green for the identical shape (a declaration that compiles and is silently inert),
  and the real dividing line in this generator is what gets emitted — NF0501/NF0502 add no
  entry and the class emits no file at all, whereas a duplicate attribute still produces a
  working registration.
- **Decision:** Amend — **Warning**, paired with skipping the duplicate's entry at emission so
  the generated output matches the diagnostic's message. Last-wins was the other option the
  PHASE-001 entry queued by name; rejected because it needs the registry's dedupe key widened
  (runtime work, not this plan's) and makes the winner depend on assembly-scan order. Warning
  also keeps source that compiles at v1.7.0 compiling, so the arc stays a minor release.
- **Follow-up:** [reviews/002-plan-review.md](./reviews/002-plan-review.md). Closes the
  2026-08-14 PHASE-001 deferral below.

### 2026-08-15 — PHASE-002 (a third "can't go red" bullet, caught before implementation)

- **Finding:** Two of the draft's acceptance bullets could not fail for what they claimed.
  The incremental-cache bullet asserts equality of transform outputs across runs, which any
  deterministic scalar satisfies — a phase field hardcoded to `Immediate` passes it, and
  because `ReplaceSyntaxTree` reuses the reference manager, even a `TypedConstant` field
  (the actual cache hazard) would likely stay green. The emission bullet would have been
  satisfied by a `Contains("DispatchPhase.AfterCommit")` that cannot distinguish the
  `global::`-qualified form from the bare one — the latent bug `RelayHandlerRenderer.cs:38-40`
  documents as having shipped for four releases.
- **Decision:** Amend before the first edit — the cache bullet now claims only determinism
  plus future collection-shaped-field coverage; the representation rule became a Constraint
  enforced by code review rather than a claimed test; the qualification became a Constraint
  with the acceptance bullet worded to require a negative pin on the bare form. A second
  non-discriminating test was considered as a replacement and declined.
- **Follow-up:** This is the "verify, don't inherit" failure mode recurring in a plan written
  with that lesson loaded — the third instance in this arc. The pre-flight had *spotted* the
  qualification and recorded it as an observation with nothing enforcing it, which is how it
  would have been lost.

### 2026-08-15 — PHASE-002 (docs this plan invalidates — handed to PHASE-005)

- **Finding:** PHASE-002 is the plan that makes the attribute's phase real, so on the day it
  lands, published prose describing all handlers as in-scope/in-transaction/before-`Raise`-
  returns becomes conditionally false, and three diagnostics tables go stale. Concrete
  anchors: `docs/attributes-reference.md:218` and `:202`,
  `skills/RemoteFactory/references/factory-events.md:115` and `:541-543`,
  `docs/factory-events.md:370-372`. PHASE-005's stub named the phase contract but not the
  diagnostics tables.
- **Decision:** Defer to PHASE-005 with the anchors recorded in its Scope, rather than
  widening PHASE-002.
- **Follow-up:** PHASE-005.

### 2026-08-15 — PHASE-002 (undefined enum values are expressible and will not drain)

- **Finding:** `[FactoryEventHandler<T>((DispatchPhase)99)]` compiles. Faithful pass-through
  renders the cast, and the handler then never runs — the scheduler's drain sweeps only
  defined phases, so the registration is a silent no-op.
- **Decision:** Not diagnosed. Undefined enum values are a C# hazard generally, and policing
  them is out of proportion to this plan. Recorded so the choice is visible rather than
  accidental.
- **Follow-up:** none. Revisit only if a consumer hits it.

### 2026-08-14 — PHASE-003 (code review: interface leg aligned on the registrar-holder shape)
- **Finding:** Code review V1: introducing the wrapper/core split on the interface leg
  moved its bodies into private `Local*Core` methods while the assembly attribute still
  pointed at `{Impl}Factory` — whose `[DynamicallyAccessedMembers]` roots every method —
  i.e. the configuration TRIM-009 *measured* as insufficient on the class leg. "TRIM
  item 20 status unchanged" understated a direction-of-change.
- **Decision:** Amend — the interface renderer now emits a single-method registrar
  holder (`NeatooInterfaceFactoryRegistrar_` prefix) and points the attribute at it,
  aligning all three legs on the measured-good shape. Elimination on this leg is still
  UNVERIFIED (TRIM Deferred Work item 20; fixture blocked by item 19) — but the shape is
  no longer the measured-bad one.
- **Follow-up:** TRIM item 20's eventual verification now tests the holder shape.
  [reviews/003-code-review.md](./reviews/003-code-review.md).

### 2026-08-14 — PHASE-003 (the RFEF substrate, as actually built)
- **Finding:** RFEF plans to build declarative transactions on this plan's entry-call
  tracking. What landed, for its Current State: tracking lives on
  `IFactoryEventPhaseScheduler` (events-named, scoped, Server+Logical) with **no
  observer hook** — RFEF needs a seam that does not exist yet; granularity is per-scope
  (concurrent flows share depth; see the limitation entry below); and generated
  `Can*`/`LocalCan*` authorization probes are now full entry calls — under RFEF a
  read-only auth probe would open and commit a transaction unless excluded.
- **Decision:** Record here; no code change in PHASE. RFEF-001's draft inherits these
  three facts as Current State constraints.
- **Follow-up:** RFEF todo (sibling; blocked on PHASE-003/004 — 003 is now landing).

### 2026-08-14 — PHASE-003 (concurrent flows share entry state — documented limitation)
- **Finding:** The test-review gate pressed on plan-review B-C2: the scheduler's lock
  gives data-race safety, but entry tracking is per-scope, not per-flow. Two concurrent
  flows in one scope interleave on one depth counter — a failed flow's exit is a nested
  exit (no clear), so its queued work rides the surviving flow's drain.
- **Decision:** Document, don't redesign. Scopes are the framework's isolation unit;
  concurrent flows sharing a scope already share DbContexts and every scoped service.
  The actual semantics are pinned
  (`ConcurrentFlowsInOneScope_ShareEntryState_FailedFlowsWorkRidesTheSurvivingDrain`)
  so any change to them is a conscious one. A second window exists and is recorded but
  not pinned (round-2 N1): work a concurrent flow enqueues *while* the survivor's
  outermost drain is running either joins that drain or is discarded by the post-drain
  clear, depending on timing — inherent to the same per-scope granularity. Per-flow
  tracking (AsyncLocal) would be a design change with its own hazards — revisit only if
  a real consumer hits this.
- **Follow-up:** PHASE-005 documents "one factory call per scope at a time" as the
  concurrency guidance.

### 2026-08-14 — PHASE-003 (test-review round 1: 3 must-cover, all closed)
- **Finding:** The gate's sharpest catches: the `AspForbidException` mitigation note was
  factually wrong (the type is public in core — no ASP.NET pipeline needed to exercise
  it); the nested-save tests could not discriminate inner-vs-outer drain and the
  evidence row's justification was incorrect; and two sacred relay-collection tests had
  been silently weakened by the dispatcher change without being edited — the failure
  mode pre-declaration cannot catch.
- **Decision:** Amend — all 3 must-cover and all 6 should-cover findings closed with
  tests (+6 unit, +4 integration); evidence rows corrected; two nice-to-haves left open
  by choice and recorded.
- **Follow-up:** [reviews/003-test-review.md](./reviews/003-test-review.md); 9002/9004/
  9006 emission pins and the `ClientServerContainers` tuple-order/duplication hazard
  routed to PHASE-007 (its scope grows accordingly).

### 2026-08-14 — PHASE-003 (plan review)
- **Finding:** Plan review returned CONCERNS — 6 veto findings. The sharpest: the
  "structural" rollback-discard story is false for long-lived scopes (a failed call's
  queues would drain into the next successful call in the same scope — Logical mode,
  Blazor Server, the integration harness); the interface renderer is not the class
  renderer's shape (inline guard, no `*Core` split, trimming UNVERIFIED — TRIM item 20);
  and the drain-vs-cancellation-check ordering inside the choke point could silently
  recreate the B-C5 failure mode.
- **Decision:** Amend (draft edited before implementation; all vetoes addressed —
  failure now *clears* explicitly at outermost exit, never drains; entry stays active
  through the drain; pre-declared pin-amendment set widened to six named tests).
- **Follow-up:** [reviews/003-plan-review.md](./reviews/003-plan-review.md) — includes
  the full disposition table.

### 2026-08-14 — Client-raise relay gap (pre-existing, deferred)
- **Finding:** Review A-V1: `MakeRemoteDelegateRequest.ForDelegateEvent` discards the
  server response, so events raised during a client-initiated `Raise` — by handlers of
  any phase, including Immediate, today — are collected server-side but never relayed
  back. Naively wiring relay in would echo the client's own event back to its own
  relay, against the "one `[Remote]` call = exactly one `Relay` invocation" contract.
- **Decision:** Defer — pre-existing gap, not phase-related; PHASE-003's remote-raise
  acceptance claims the drain only. Needs a user decision on whether it warrants a plan
  row (echo-to-self semantics are a real design question) or is working-as-intended.
- **Follow-up:** revisit at PHASE-004 (which owns consumer-facing drain/relay surface)
  or at todo close.

### 2026-08-14 — Ordering: PHASE-003 worked ahead of PHASE-002
- **Finding:** PHASE-002 (generator threads the attribute's phase argument to
  registration) and PHASE-003 (entry-call tracking + AfterCommit drain) are independent:
  PHASE-003's tests can register phased handlers through the registry's 3-arg overload
  directly, so nothing in it waits on the generator pass-through.
- **Decision:** Re-split (ordering only — no index rows change). Work PHASE-003 next:
  it is the plan the review flagged as riskiest, it restates PHASE-001's interim
  acceptance pins, and the RFEF sibling todo is blocked on it. PHASE-002 follows and
  makes the attribute phase flow end-to-end.
- **Follow-up:** PHASE-003's branch is stacked on the PHASE-001 plan branch (its
  scheduler work isn't on `main` yet) — a recorded deviation from the "plan branches off
  the todo branch" convention; see the plan's Notes.

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
  **Closed** 2026-08-15 — diagnose at Warning, skip the duplicate entry; see the PHASE-002
  plan-review entry at the top of this log.

---

## Skipped Steps

- **PHASE-008 — Step 5 code review not run, despite the plan declaring
  `Code-review opt-in: Yes`.** An **omission, not a decision** — there was no skip reasoning
  at the time; it was simply never invoked, and this section said "(none yet)" until the
  close-out audit found it (C1). It matters because 008 is the arc's widest plan (13
  acceptance bullets, three folded rows, generator emission across all legs, the concurrency
  harness) **and** the only one with neither review, having also declined plan review by user
  direction. Partially mitigated in substance, not in record: 008's test-review gate caught
  the canonical-declaration silent-drop, and PHASE-011's code review independently re-derived
  the emission-site consumer set. Carried to Follow-on.

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

**2026-08-31 — Grade A.** Full record: [reviews/close-out-audit.md](./reviews/close-out-audit.md).

All ten Acceptance Criteria traced to named tests the auditor **read and confirmed assert the
criterion** — not accepted from the evidence note this container carries. No veto-tier
findings; both solutions build with 0 errors; **2,912 tests pass, 0 fail, 10 standing skips**.
Container clean: 13 plans issued against a cap of 13, monotonic numbering, no reuse, every
`Retired` row carrying its folding reason, no open PRs, Out of Scope verified respected
(same-event-only coalescing, originating-scope `AfterCommit`, zero persistence concepts in
`src/RemoteFactory/` or `src/Generator/`).

Five callouts, none blocking. Four are closed in the same commit as this entry: the stale
`Clear()` remark at `ClientServerContainers.cs:146` (C3), **two wrong citations in this
todo's own criteria-evidence note** (C4 — AC-6's pin is 9005, not 9009; the 9007 placement pin
lives in `FactoryEventPhaseSchedulerTests`, not the coordinator tests), and the Plan Index
table break (C5). C1 and C2 go to Follow-on.

The auditor **confirmed rather than inherited** the judgement PHASE-011's code review deferred
here: that plan's emission change needed no Design-project update, because demonstrating it
would require planting a deliberate namespace decoy in `Design.Domain` — an anti-pattern in
the project whose role is authoritative examples, and already covered where it belongs, in the
generator's own emission fixtures.

Two findings worth carrying beyond this todo: **PHASE-008's declared code review never ran**
(now in Skipped Steps, recorded as an omission rather than a decision), and the **client-raise
relay gap** deferred on 2026-08-14 to "PHASE-004 or todo close" — close is now, and it had no
destination until this table.

---

## Follow-on

*Everything this todo did not do. A list, not a commitment — a successor that adopts an item
writes it as an acceptance bullet there.*

- Client-raise relay gap: `MakeRemoteDelegateRequest.ForDelegateEvent` discards the response, so events raised by handlers of a client-initiated `Raise` never relay back · needs the echo-to-self design decision, not a fix · origin: Discovery Log 2026-08-14, audit C2
- PHASE-008's code review, declared `Yes` and never run · origin: audit C1 / Skipped Steps
- `StaticFactoryRenderer:99` non-`Task` line — reachable via non-`Task` interface methods? · origin: PHASE-011 code review, `reviews/011-redproof.log`
- Bare BCL tokens in generated output + no `using System;` injection (former row 013) · dismissed here as unreachable; revisit only if a consumer hits it · origin: PHASE-011 A2/A3
- `Types.cs:860` takes parameter types from source text · dismissed as fragile-not-broken · origin: PHASE-011 gate
- TRIM item 20 — interface-leg trim elimination UNVERIFIED, blocked on TRIM item 19; AC-9's second clause leans on it · origin: PHASE-003 code review
- EF1001-equivalent `Internal`-usage analyzer, plus auditing ~20 other `Neatoo.RemoteFactory.Internal` types against the policy PHASE-004 wrote · origin: Discovery Log 2026-08-16
- `RegisterMatchingName` registers **transient** and the repo teaches it without saying so · origin: Discovery Log 2026-08-18
- Release notes + version decision — no `docs/release-notes/v1.8.0.md`, `Directory.Build.props` still 1.7.0, and a `refactor!:` tagged commit would drive a **major** bump under CLAUDE.md's table although it only widens visibility · Step 8 work
- Unmeasured tests shipped by design: 3 in PHASE-008, 4 in PHASE-011 · declared in full in the red-proof logs · accepted, no action
- RFEF sibling todo now unblocked — PHASE-003/004 have landed · its own arc

---

## Docs & Retro

*(pending — filled at Step 8)*

---

## Results / Conclusions

*(pending)*
