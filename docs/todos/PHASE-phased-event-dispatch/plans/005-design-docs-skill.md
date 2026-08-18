# Design Projects, Published Docs, and Skill Updates

**Plan #:** 005
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done
**Last Updated:** 2026-08-17
**Plan-review opt-in:** No (documentation of already-reviewed behavior)
**Code-review opt-in:** No (doc-only; Design code samples are exercised by Design.Tests)

---

## Scope

Update the source-of-truth Design projects with a phased-handler pattern example and
passing tests, update `CLAUDE-DESIGN.md` (pattern narrative and the log-id table), add the
published docs (Jekyll) coverage for dispatch phases, and update the RemoteFactory skill's
factory-events reference — including the proposal's flagged gap that `Immediate` handlers
observe staged (unflushed) state. Skill code samples follow the MarkdownSnippets flow
(reference-app regions + `mdsnippets`) where compilable samples are used. This plan does
NOT change any behavior.

---

## Inherited from PHASE-002 (recorded at its plan review)

PHASE-002 is the plan that makes the attribute's phase real, so these anchors go stale the
day it lands. They are listed concretely because the stub's "document the phase contract"
would not have found them:

- **Prose that becomes conditionally false** — it describes every handler the way only an
  `Immediate` handler now behaves:
  - `docs/attributes-reference.md:218` — "Runs in the caller's DI scope … triggered by
    `IFactoryEvents.Raise` during a factory method. All handlers … sharing the caller's
    `DbContext` and transaction. A throwing handler aborts the chain and propagates to the
    caller." All three clauses are false for an `AfterCommit` handler.
  - `skills/RemoteFactory/references/factory-events.md:115` — "All of them run in the caller's
    scope, sequentially, in unspecified order, **before `Raise` returns**."
- **Diagnostics tables needing the new duplicate-attribute row (NF05xx, Warning):**
  `docs/factory-events.md:370-372`, `skills/RemoteFactory/references/factory-events.md:541-543`,
  `docs/attributes-reference.md:202`.
- **Contract prose worth tightening rather than replacing:** `docs/attributes-reference.md:222`
  and skill `factory-events.md:133` already scope attribute stacking to *several event types* —
  which is the documented basis PHASE-002's diagnostic enforces. Say so explicitly.
- Also inherited from PHASE-003: document "one factory call per scope at a time" as the
  concurrency guidance, and the sync block-drain deadlock caveat.

---

## Intent

After this plan, a consumer who reads any one of the three documentation surfaces —
Design projects, published docs, or the skill — gets the same, accurate phase contract:

- Handlers without a phase argument behave exactly as always (`Immediate` is the default
  and today's contract); the existing docs' invariants survive as the *Immediate-phase*
  contract rather than the universal one.
- `AfterFlush` and `AfterCommit` exist, what each is for (in-transaction post-flush work
  vs. read-only projections), how the consumer drains `AfterFlush`
  (`IFactoryEventPhaseCoordinator` via `[Service]`, inside the factory body), and what
  happens when they don't (fail-open at the `AfterCommit` point, 9007 warning).
- The sharp edges are stated where consumers will hit them: `Immediate` handlers observe
  staged (unflushed) state; failure semantics belong to the drain point, not the phase;
  ordering is per drain point, not a global barrier; one factory call per scope at a
  time; the sync-body blocking-drain deadlock caveat.
- The Design projects — the requirements source of truth — demonstrate the whole contract
  with attribute-declared handlers and passing tests, satisfying the todo's AC-9.

---

## Framework & Architectural Alignment

- **Design projects are the requirements source of truth** (project rule): behavior shipped
  by PHASE-001..004 is not "done" until Design demonstrates it. This plan is that
  demonstration, not a re-design — the contract prose derives from the shipped XML docs
  (`DispatchPhase`, `IFactoryEventPhaseCoordinator`), which plan reviews and code reviews
  already vetted.
- **DDD documentation guidelines** (user global rules): DDD vocabulary used freely, never
  explained; focus on what RemoteFactory does, not what patterns mean.
- **Skill self-containment** (project rule): `skills/RemoteFactory/` references nothing
  outside its own directory.
- **Established file conventions win:** the factory-events doc family (published doc and
  skill reference) is hand-written illustrative code today — zero MarkdownSnippets anchors,
  samples use non-compiling stand-ins like `AppDbContext` by design. Phase additions follow
  the per-file convention; the compiled, tested truth lives in Design.Domain/Design.Tests.
  (Deviation from the Scope sentence's mdsnippets default — recorded, see Notes.)

---

## Constraints & Invariants

- **No behavior change.** `src/RemoteFactory`, `src/RemoteFactory.AspNetCore`, and
  `src/Generator` are untouched. Design.Domain/Design.Tests changes are additive.
- **Existing tests are sacred:** no existing Design test is modified; the full suite
  passes unmodified (todo AC-7's standing bar).
- **Docs must not contradict shipped semantics.** The authoritative wording for the two
  restated ACs — per-drain-point ordering (AC-1) and post-completion OCE swallow (AC-3) —
  is the todo's restated text and the `DispatchPhase` XML. No doc may reintroduce the
  original global-barrier or OCE-propagates wording.
- **The Immediate contract must remain literally true for phase-less handlers.** Backward
  compatibility is a todo AC; the docs' job is to rescope the old invariants to
  `Immediate`, not to weaken them.
- **The skill stays self-contained** — no links or references out of `skills/RemoteFactory/`.
- **`Neatoo.RemoteFactory.Internal` types are not consumer API.** The coordinator
  (root namespace) is the documented drain surface; the scheduler is mentioned at most as
  the extend-at-your-own-risk seam per the CLAUDE-DESIGN Internal-namespace policy.
- Release notes are NOT this plan's deliverable — they belong to the arc's release step.

---

## Steps

1. **Design.Domain: phased-handler pattern file.** A new FactoryPatterns file declaring an
   event (or events) with attribute-declared handlers at all three phases, and a factory
   whose method drains `AfterFlush` via a `[Service]`-injected
   `IFactoryEventPhaseCoordinator` at its simulated flush/commit seam. Comments carry the
   drain-point model, the fail-open contract, and the "did not do" decisions, in the
   established heavily-commented Design style. Qualify the existing pattern file's
   three-invariants comment to the `Immediate` phase rather than duplicating it.
2. **Design.Tests: demonstrations.** New tests observing (a) three-phase drain-point
   ordering against a reverse raise order, (b) the consumer drain running AfterFlush work
   inside the method body, (c) the fail-open sweep running never-drained AfterFlush work
   after the body, and (d) a failed entry call discarding queued work. Ordering asserted
   through a recording seam, not `Assert.True(true)`.
3. **CLAUDE-DESIGN.md:** extend Pattern 4 with the phased-dispatch narrative (three
   phases, drain points, coordinator usage, fail-open, failure-semantics-per-drain-point,
   staged-state note, per-scope concurrency guidance, sync-body deadlock caveat); add
   Quick Decisions rows for phase choice; add the new Design files to the consult table.
4. **`docs/factory-events.md`:** rescope the header table and Execution Model invariants
   to the `Immediate` default; add a Dispatch Phases section (contract, coordinator,
   fail-open, ordering, discard-on-failure, relay interaction); extend the diagnostics
   table (NF0504) and runtime log-event table (9001–9007); extend the DI registration
   table with the phase services per mode.
5. **`docs/attributes-reference.md` and `docs/interfaces-reference.md`:** document the
   attribute's `DispatchPhase` argument and NF0504; correct the line-218 prose to
   phase-scoped truth; tighten the stacking sentence per the inherited anchor; add an
   `IFactoryEventPhaseCoordinator` interface entry and summary-table row.
6. **Skill:** mirror the phase contract into `references/factory-events.md` (invariants
   rescope, phases section, diagnostics + log tables, When-to-Use-What rows) and add
   SKILL.md quick-decision rows and the updated reference-file description.
7. **Stale-claim sweep:** grep docs/ and skills/ for the invalidated universal claims
   (all-handlers-in-transaction, before-`Raise`-returns, exceptions-always-propagate) and
   fix any instance the anchor list missed; `docs/events.md` (redirect stub) expected to
   need nothing.
8. **Gate:** fill Test Evidence, run build + full test suite once to logs, invoke
   test-reviewer.

---

## Acceptance

- [x] Design.Domain declares attribute-registered handlers at all three phases and a
      factory method that drains `AfterFlush` through a `[Service]`-injected
      `IFactoryEventPhaseCoordinator`; Design.Tests observe the three-phase drain-point
      ordering (Immediate at raise, AfterFlush at the coordinator call inside the body,
      AfterCommit after completion) against a raise order chosen to differ. `[integration]`
- [x] Design.Tests observe the fail-open path: an attribute-declared `AfterFlush` handler
      whose factory never drains still runs, after the method body returns. `[integration]`
- [x] Design.Tests observe that a failed entry call discards queued phased work — the
      handlers never run. `[integration]`
- [x] `docs/factory-events.md` documents the full phase contract: three phases with the
      Immediate-default rescope, coordinator drain, fail-open + 9007, per-drain-point
      ordering, discard on failure, staged-state note, per-scope concurrency guidance,
      NF0504 row, 9001–9007 rows, phase-service DI rows.
      `[explicit-skip: prose deliverable — verified by the stale-claim sweep and review]`
- [x] `docs/attributes-reference.md` documents the phase argument with corrected
      phase-scoped prose; `docs/interfaces-reference.md` gains the coordinator entry.
      `[explicit-skip: prose deliverable]`
- [x] The skill's `factory-events.md` and SKILL.md carry the same contract; every code
      sample and reference stays inside the skill directory.
      `[explicit-skip: prose deliverable + self-containment check]`
- [x] A sweep of docs/ and skills/ finds no remaining instance of the invalidated
      universal claims. `[explicit-skip: grep gate, recorded in the plan]`
- [x] Existing Design tests unmodified; full solution build and test green.
      `[explicit-skip: meta-bullet, satisfied by the Step 8 gate run]`

---

## Current State (Pre-Flight)

Walked 2026-08-17, before any edit. All anchors verified against the working tree at
`PHASE` tip (`9d8e3df`).

**Shipped contract sources (what the docs must agree with):**
- `src/RemoteFactory/DispatchPhase.cs:1-75` — the four-`<para>` remarks block is the
  canonical ordering/carve-out/persistence-agnostic wording; per-member XML carries the
  staged-state note (Immediate), the flush/commit drain guidance (AfterFlush), and the
  OCE-swallow rationale (AfterCommit).
- `src/RemoteFactory/IFactoryEventPhaseCoordinator.cs` (81 lines) — scope-wide-not-call-wide
  and drain-inside-the-body remarks; whitelist rejection of non-AfterFlush phases.
- `src/RemoteFactory/Internal/FactoryEntryCall.cs:88-93` — the sync-body blocking-drain
  deadlock caveat wording to adapt for docs.
- Integration exemplar for the Design work:
  `src/Tests/RemoteFactory.IntegrationTests/TestTargets/Events/FactoryEventPhaseCoordinatorTargets.cs`
  + `Events/Phases/FactoryEventPhaseCoordinatorTests.cs` — attribute-declared handlers,
  `[Service]` coordinator injection, marker-sequence assertions
  (`["ord-immediate", "ord-flush", "ord-immediate", "ord-method-done", "ord-commit"]`).

**Doc surfaces as they stand:**
- `docs/factory-events.md` (485 lines): header table rows "DI scope / Dispatch /
  Exceptions" (5-13) and the three Execution Model invariants (21-27) state the
  Immediate contract as universal. Diagnostics table (368-372) ends at NF0503; runtime
  log table (376-381) ends at 3012; DI table (391-395) has no phase services.
- `docs/attributes-reference.md`: `[FactoryEventHandler<T>]` section 191-222. Matching
  rules (197-202) don't mention the phase argument or NF0504; line 218 carries the three
  false-for-AfterCommit clauses; line 222 is the stacking sentence to tighten.
- `docs/interfaces-reference.md`: `IFactoryEvents` (608), `IFactoryEventRelay` (625),
  summary table rows (685-686). No coordinator entry.
- `docs/events.md`: v1.5.0 removal/redirect stub — needs nothing.
- `skills/RemoteFactory/references/factory-events.md` (582 lines): header table (5-17),
  three invariants (21-27), the line-115 "before `Raise` returns" claim, stacking prose
  (133-156), diagnostics (539-543), log events (547-554), DI table (558-568),
  When-to-Use-What (572-583).
- `skills/RemoteFactory/SKILL.md`: quick-decision event rows (74-80), reference-file
  description (95). Zero snippet anchors in either skill events file — hand-written
  convention confirmed.
- `src/Design/CLAUDE-DESIGN.md` (1103 lines): Pattern 4 spans 154-264; the only phase
  mention is the PHASE-002 bullet at 264. Log table already current through 9007
  (1049-1055, added by 003/004); NF0504 row present (1039). Quick Decisions event rows
  (287-293) and the Design-files table (1092-1096) know nothing of phases.
- `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs`: the
  three-invariants comment block (12-27) states the Immediate model as the model — needs
  the phase-scoped qualifier, not duplication. `Design.Tests/FactoryTests/
  FactoryEventHandlerTests.cs` demonstrations assert via `Assert.True(true)` — the new
  tests will not follow that shape (ordering via a recording seam instead).
- `Design.Tests/TestInfrastructure/DesignClientServerContainers.cs` provides the
  three-container `Scopes()` pattern the new tests will use.

**Registration reality check:** `IFactoryEventPhaseCoordinator` is registered in Server
and Logical modes only (`AddRemoteFactoryServices.cs:91-92`), resolving the scope's
existing scheduler — `[Service]` injection on a server-executing factory method is the
demonstrated consumption shape (per the integration exemplar), valid for Design.Server
and the Design test containers.

**No surprises → no pre-flight amendments.** The stub's anchor list held; the one
decision recorded at draft time is the hand-written-vs-mdsnippets convention call
(see Framework Alignment and Notes).

---

## Test Evidence

Filled 2026-08-17, after implementation, before the gate; amended after gate round 1
(two should-cover closures added 2 tests). All cited tests are new in this plan, in
`Design.Tests.FactoryTests.FactoryEventPhasesTests`, run through
`DesignClientServerContainers` (client/server/local containers with real serialization
— the Design tier's integration shape). Gate logs: `reviews/005-build.log`
(both solutions — see the RP-0 note), `reviews/005-test.log` (round 2: unit 705×2,
integration 587×2 + 5 standing skips, Design 93×2 — 86 pre-existing + these 7).
Red-proof: `reviews/005-redproof.log` (RP-1 measured — exact predicted 2-red
signature; RP-0 records the stale-DLL false-green incident and the resulting
Design-count gate rule, now 93). Gate record: `reviews/005-test-review.md`.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| Three phases attribute-declared; coordinator drain inside the body; ordering vs. differing raise order | `[integration]` | `Finalize_Remote_RunsEachPhaseAtItsDrainPoint`, `Finalize_Logical_RunsEachPhaseAtItsDrainPoint` (drain position, both modes), `QuarterClose_Remote_RunsInPhaseOrderNotRaiseOrder` (reverse raise order + post-drain interleave) | ✓ |
| Fail-open: never-drained AfterFlush runs after the body | `[integration]` | `Archive_NeverDrained_AfterFlushHandlerRunsAtTheSweep` | ✓ |
| Failed entry call discards queued phased work | `[integration]` | `PaymentIntake_EntryCallThrows_QueuedPhasedWorkIsDiscarded`; `PaymentIntake_FailedThenSuccessfulCall_SameScope_DiscardsRatherThanLeaks` (gate round 1: positive control + discard-vs-leak) | ✓ |
| `docs/factory-events.md` full phase contract | `[explicit-skip: prose]` | — (Dispatch Phases section + NF0504 + 9001–9007 + DI rows shipped; verified by the Step 7 sweep and the gate's independent prose-vs-code check; the DI table's Remote-mode row additionally pinned by `Coordinator_NotRegisteredInTheRemoteClientContainer`, gate round 1) | n/a |
| attributes-reference phase argument; interfaces-reference coordinator entry | `[explicit-skip: prose]` | — (both shipped; line-218 clauses and stacking prose corrected per the inherited anchors) | n/a |
| Skill carries the contract, stays self-contained | `[explicit-skip: prose]` | — (phases section, tables, SKILL.md rows; link grep found no reference outside the skill directory) | n/a |
| Stale-claim sweep clean | `[explicit-skip: grep gate]` | — (patterns: before-Raise-returns, propagates-to-caller/aborts-chain, same-transaction/sharing-caller, Raise-returns-only-after, all/every-handlers-complete; fixed beyond the anchor list: `docs/decision-guide.md:120`, `docs/factory-operations.md:448`, `docs/interfaces-reference.md` IFactoryEvents entry, both relay data-flow diagrams, 3 quick-decision rows. Exclusions: `docs/release-notes/**`, `docs/plans/**`, `docs/todos/**` — versioned history and working documents) | n/a |
| Existing Design tests unmodified; build/test green | `[explicit-skip: meta]` | — (`git diff` touches no existing test file except the additive container registration; gate logs green) | n/a |

---

## Plan Amendments

*(none yet)*

---

## Notes

- **mdsnippets deviation, recorded:** the Scope sentence (written at stub time) defaults
  skill samples to the MarkdownSnippets flow. The pre-flight found the entire
  factory-events doc family is hand-written illustrative code (no anchors, deliberate
  non-compiling stand-ins like `AppDbContext`), while compiled truth lives in the Design
  projects. This plan follows the per-file convention: hand-written phase samples in
  docs + skill, compiled demonstrations in Design.Domain/Design.Tests. If the user
  prefers the mdsnippets flow for the new samples, that's an amendment, not a re-plan.
- The `docs/decision-guide.md` page and other cross-cutting pages are covered by the
  Step 7 sweep rather than enumerated here — the sweep greps for the invalidated claims,
  not for the word "event."
- PHASE-006 (coalescing) may later add an attribute flag; nothing in this plan's prose
  should promise or preclude it.
