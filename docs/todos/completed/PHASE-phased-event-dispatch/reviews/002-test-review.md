# PHASE-002 — Test Review (Step 5 Gate)

**Date:** 2026-08-15
**Reviewer:** `test-reviewer`
**Result:** no must-cover; 4 should-cover (all plan-related), 5 nice-to-have, 4 tech-debt
**Disposition:** all 4 should-cover closed; 2 of 5 nice-to-have closed; tech-debt routed.

The gate verified the test-count delta against PHASE-003's log (668 unit / 579 integration →
677 / 581) to confirm nothing had been deleted, and independently re-derived the end-to-end
tests' falsifiability rather than taking it from the plan — confirming no `RegisterHandler`
call exists in the new targets or tests, that the generated file emits the AfterCommit
registration, and that the two new event types appear nowhere else in `src/`.

---

## Should-cover — all closed

| # | Finding | Disposition |
|---|---|---|
| 1 | **The server-runtime guard on the relay leg had no assertion anywhere in the unit suite**, and the evidence row claimed ✓ from three tests that assert only the assembly-attribute/holder half. The only real control was the CI publish-trimmed absence gate — reasoned from marker reachability, not in the Step 5 logs. | Closed — `RelayHandler_PhasedRegistration_StaysInsideTheServerRuntimeGuard` asserts guard-then-registration *in order*; red-proofed (RP-6). Evidence row now cites it. |
| 2 | **NF0504's "names the surviving phase" only exercised where the survivor is `Immediate`** — which is also the hardcoded default, the malformed-argument fallback, and what a deleted placeholder prints. Green against three wrong implementations. | Closed — `RelayHandler_DuplicateEventType_PhasedFirst_KeepsThatPhaseAndNamesItInTheMessage`; red-proofed (RP-5). Also pins source-order-wins, which nothing else did. Independently raised as code review V1. |
| 3 | **NF0504's location unasserted** while the Acceptance bullet says "located at the class". | Closed — `RelayHandler_DuplicateEventType_DiagnosticIsLocatedAtTheClass`, asserting through the source span rather than a line/column pair so a fixture edit cannot make it a false red. |
| 4 | **The incremental-cache fixture's phase argument can silently stop binding** — the transform's malformed-argument fallback returns `Immediate`, `RunGeneratorTracked` never checks the input compilation for CS errors, and `Fixture_ProducesNoDiagnostics` filters `NF` ids only. The fixture would degrade to two defaulted attributes with every test still green. | Closed — `IncrementalCacheTests.Fixture_PopulatesThePhaseArgument`, the file's third fixture-health guard. |

## Nice-to-have

| Finding | Disposition |
|---|---|
| `AfterFlush` never rendered in any emission test | Closed — third event type added to `PhasedRelayHandlerSource` at `AfterFlush` |
| Phase + `[Service]` + `CancellationToken` not pinned at emission | Closed — the same new handler carries both, so the phase token and the parameter list are pinned interacting |
| Duplicate variants: triple declaration, undefined-cast survivor, aliased generics | Declined for this plan — the aliased-generics case is reasoned in the plan review; the others are low-value permutations |
| NF0503 emission count changes in one shape | Recorded in the transform's comment (code review C4), no test |
| NF0504 tests live in `AssemblyAttributeEmissionTests` rather than the NF05xx diagnostics file | Accepted — they assert emission alongside the diagnostic, which is what that file is for |

## Tech debt routed

Both reviewers agreed **not** to widen PHASE-007 (already three unrelated items). New plan 008:

- The **event-type token** in the emitted registration is not `global::`-qualified — the same
  hazard this plan spent a Constraint, a veto, and a negative pin on, three tokens to the left
  in the same statement. Note the ratchet: five new assertions hardcode the unqualified form.
- **Attributes split across partial declarations** — `ForAttributeWithMetadataName` fires per
  syntax node while the transform reads `symbol.GetAttributes()`, so two attributed partials
  should produce duplicate hint names. Inferred from reading, not measured.
- `DiagnosticTestHelper.RunGenerator` **returns every generator diagnostic twice** (driver
  out-param concatenated with `runResult.Diagnostics`). No current test makes a count
  assertion, so nothing is wrong today; it is a live trap. Found while writing these tests.
- `RunGeneratorTracked` never checks the input compilation for CS errors — the enabler for
  should-cover #4, and it applies to every future cache fixture.
- `Diagnostics/NF04xxFactoryEventHandlerTests.cs` contains `class NF05xxFactoryEventHandlerTests`.

## Handoff to PHASE-004

As of this plan, `[FactoryEventHandler<T>(DispatchPhase.AfterFlush)]` is consumer-reachable
for the first time, and the scheduler's sweep already drains it at the AfterCommit entry
point — fail-open *without* the logged warning PHASE-004 promises. PHASE-004's acceptance
should cover an **attribute-declared** AfterFlush handler, not only a hand-registered one.
Recorded in the Discovery Log and in PHASE-004's inherited section.

## Sacred tests

Verified unweakened. `RegisterHandler_SameHandlerClassTwoPhases_KeepsTheFirstRegistration` —
comment rewrite only, four statements byte-identical. `IncrementalCacheTests` fixture — one
attribute gained a phase argument, no assertion changed, NF0502-trap avoidance preserved.
