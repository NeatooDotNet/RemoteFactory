# PHASE-006 Test Review (Step 5 Gate)

**Plan:** [../plans/006-coalescing.md](../plans/006-coalescing.md)
**Logs:** `006-build.log`, `006-test.log` (round 2 overwrote round 1 — headers say so), `006-redproof.log`

---

## Round 1 — 2026-08-18

**Verdict: the gate can close. Zero must-cover findings, plan-related or tech-debt.**
The reviewer verified the logs by *count* (726 = 705+21, 590 = 587+3, 94 = 93+1, each
reconciled against the new-test census), confirmed both red-proofs as genuine positive
controls with exact predicted signatures measuring B-V1 and B-V2 directly, confirmed the
five A-V1 survivor-rule strings 5/5 updated, and judged the evidence map **honest with
one phrasing inaccuracy** (row 9 said "the 5 emission assertions and nothing else" —
understating the additive `CapturingLoggerProvider` change by one file).

Sacred-tests verdict: nothing weakened. The 5 emission assertions preserved and
strengthened (phase token intact, flag default now pinned positively); the PHASE-002
NF0504 message pins still discriminate under the reworded format (the `DoesNotContain`
half still binds); `FactoryEventPhaseRegistrationTests` untouched and correctly so
(tuple widened with element names preserved).

### Findings and disposition

| # | Tier | Finding | Disposition |
|---|------|---------|-------------|
| 1 | should-cover (plan) | Which collapsed instance survives is unstated and unpinned — under the documented custom-`Equals` over-collapse hazard, the surviving payload is what the consumer sees; a latest-wins refactor changes delivered payloads with the suite green | **Closed round 2:** `Coalesce_CustomEqualsCollapse_TheHandlerReceivesTheFirstRaisedInstance` + the first-raised sentence added to the attribute XML |
| 2 | should-cover (plan) | B-V3's reference-typed-member no-op documented in four surfaces, zero executable evidence — a structural-comparer "improvement" flips all four claims silently | **Closed round 2:** `Coalesce_ReferenceTypedMember_DefeatsEqualityAndDoesNotCollapse` |
| 3 | should-cover (plan) | The todo AC's relay-unaffected clause structurally true but unpinned — the natural cross-event next step (dedupe at raise) would drop relayed events green | **Closed round 2:** `RemoteExecute_CoalescingHandler_RelayStillReceivesEveryRaise` (3 relayed, 1 run) via the existing relay harness |
| 4 | nice-to-have (plan) | Runtime inertness on unqueued paths claim-only; NF0505 location unpinned; 9002 collapsed drained-count unasserted | **Routed to PHASE-007** |
| 5 | should-cover (tech debt) | `FactoryEventPhaseScheduler` has zero concurrency coverage against its own shared-scope contract; predates this plan, stakes slightly raised by Queue→List + the in-lock scan | **Routed: candidate for its own plan** (deterministic harness, not a `Task.WhenAll` race) — recorded in the 007 row pending a re-split decision |
| 6 | nice-to-have (tech debt) | Registry-`Clear()` and `CapturingLoggerProvider`-snapshot 007 items each gained dependents under this plan | **007 row annotated** |
| 7 | — | Evidence row 9 understated the modified-test surface by one file | **Corrected round 2** |

Also noted for the code review: the warn-preserving-merge comment block is duplicated
verbatim in `FactoryEventPhaseScheduler.cs`.

## Round 2 — 2026-08-18

Closures: 3 new tests (unit 726 → 728, integration 590 → 591; Design unchanged at 94),
logs overwritten with round-2 runs, all green with expected totals. Red-proof addendum
records why the three closures are argued-not-sabotaged (two-way exact assertions whose
failure mode is the alternative implementation; wiring measured by RP-1/RP-2).

**Round-2 reviewer verification: CONFIRMED — all three closures genuinely closed, nothing reopened, the gate closes.** Zero must-cover findings across both rounds; Done-eligible from this gate's side, pending the opted-in code review.

Verified from the round-2 logs and the diff, not the closure descriptions:

- Logs re-verified by count with round-2 headers: 728 = 726+2, 591 = 590+1, 94 unchanged — every delta reconciles exactly with the three new methods.
- **Closure 1 judged stronger than requested:** the `["first"]` assertion discriminates three ways at once (latest-wins → `["second"]`, no-collapse → `["first","second"]`, dead handler → `[]`), and its green state also proves the custom `Equals` is reached through the `FactoryEventBase`-typed virtual dispatch — a leg round 1 could only reason about. The XML sentence (the half that mattered most) landed.
- **Closure 3 correctly paired:** relay-count-only would stay green if coalescing broke; run-count-only would stay green if the relay dropped — both halves are two-way, and the poll timeout fails safe (red, never false green).
- **The argued-not-sabotaged case accepted**, with one wording correction applied to the red-proof addendum: closure 2 is a one-way boundary pin (red only under a *stronger* comparer; RP-2 measures the working direction) — the blanket "two-way" phrasing over-claimed it.
- **Evidence map honest:** row 9's correction precise, the two new rows accurate and correctly tiered, header totals reconcile, all 23 cited methods exist, no padding.
- Two nice-to-have observations, both acted on post-verification: the duplicated warn-merge comment block (introduced by the round-2 commit's sabotage revert — **deduplicated before the code review**, with provenance passed along), and the relay-harness duplication annotated onto the two existing PHASE-007 rows (2s-poll flake and tuple-order items each gained a dependent).
