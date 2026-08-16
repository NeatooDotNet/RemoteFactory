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
      remaining queued handlers still run. A handler-internal `OperationCanceledException`
      is swallowed the same way — the entry drain passes no token, so nothing may abort a
      succeeded call's post-completion work; only genuine cooperative cancellation (the
      drain's own token cancelled) propagates, at drain points that take one.
      *(Restated by PHASE-004, exercising the decision PHASE-003's code review C2
      delegated to it; the original wording was "`OperationCanceledException` still
      propagates.")*
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
| 002 | [002-generator-phase-passthrough](./plans/002-generator-phase-passthrough.md) | Generator reads phase from attribute, threads to registration | Done |
| 003 | [003-aftercommit-entry-call-drain](./plans/003-aftercommit-entry-call-drain.md) | Entry-call tracking in generated factories; AfterCommit drain | Done |
| 004 | [004-afterflush-coordinator](./plans/004-afterflush-coordinator.md) | IFactoryEventPhaseCoordinator public API + fallback drain | Draft |
| 005 | [005-design-docs-skill](./plans/005-design-docs-skill.md) | Design projects, published docs, skill reference | Draft |
| 006 | [006-coalescing](./plans/006-coalescing.md) | Opt-in same-event coalescing (v2, queued per user) | Draft |
| 007 | *(not yet drafted)* | Tech debt: registry test-isolation hook (`Clear()` is internal and uncalled; every test invents unique event types); 9002/9004/9006 positive emission pins (unit harness now exists: `CapturingLoggerProvider` extracted by 004); `ClientServerContainers` tuple-order divergence + `ScopesWithLogging` duplication and cross-container log attribution; documenting pin for the accepted undefined-phase silent no-op; `SingleEventRelay` hard 2s poll flaking under full-parallel runs; `IEventTestService` shared-singleton Guid-filter discipline; `Enqueue` null-handler guard pin; snapshot accessor on `CapturingLoggerProvider.Entries` before more pins build on it (all routed from 004's gate) | Draft |
| 008 | *(not yet drafted)* | Generator emission hygiene: `global::`-qualify the remaining emitted type tokens (event type in relay registration, and audit the other legs); probe the partial-declaration attribute-split hint-name collision; `RunGeneratorTracked` never checks the input compilation for CS errors; `NF04xx…Tests.cs` holds `class NF05xx…Tests`. *(The `DiagnosticTestHelper` double-count was pulled forward and fixed in PHASE-002.)* | Draft |

---

## Discovery Log

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
