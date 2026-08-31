# PHASE-011 — Test Review

**Plan:** [../plans/011-hardening.md](../plans/011-hardening.md)
**Gate:** Step 5, mandatory per-plan test review
**Budget:** `deep`. A `code-reviewer` was launched in parallel on the same diff and **failed**
(stalled with no output); it is being relaunched against the corrected state, so this gate
was the only eye on round 1.

---

## Round 1 — 2026-08-31

**Verdict: CONCERNS → closed.** 2 must-cover, 1 should-cover, 4 tech-debt notes. Zero
veto-tier. All plan-related findings addressed.

The reviewer verified the pre-flight independently: both solutions `Build succeeded` with 0
errors, six green test summaries, totals reconciling 758 → 762 against a count of new
`[Fact]`s in the diff. It also confirmed the test diff is **+239 / −0** — not one existing
line of test code edited, no assertion removed, no expected value bent — and independently
re-ran the `Clear()`-is-uncalled grep before accepting the deletion.

### Must-cover

| # | Finding | Disposition |
|---|---------|-------------|
| M1 | **The event-preservation guard was vacuous, and was a duplicate of the class-factory guard.** That renderer does not emit into the consumer's namespace — it emits into `namespace {SanitizeNamespace(assemblyName)}` (`EventPreservationRenderer:62,77`), i.e. `TestAssembly` under the harness. Both decoys lived under `TestNamespace` and were unreachable. Compounding it, the anti-vacuity assertion matched `FilePath.Contains("PreservationTarget")` — the **class-factory** output for that type — so the test would have stayed green if the preservation renderer stopped emitting entirely. | **Fixed, then re-scoped.** Decoys moved under `TestAssembly`; existence assertion now targets the `NeatooEventPreservation` hint. Four further sabotages (RP-3…RP-6) then established the guard *cannot* catch a consumer-type mis-binding **at all**, so it is relabeled a **smoke test**. See below. |
| M2 | **All four tests were labeled `Regression guard` in their XML**, including the two RP-0/RP-1 measured as catching live defects (CS0738 interface, CS0029 static) — while the plan's Test Evidence row asserted the split *had* been made in the XML. Only the red-proof log was correct. Two adjacent errors in the same block: the section header shipped the killed prediction as present-tense fact, and the class-leg XML claimed "passed unmodified on first run" when RP-0 records all four red on first run. | **Fixed.** Two labeled as having caught a defect, one regression guard, one smoke test; header rewritten; the false Test Evidence row corrected in place with the error named. |

**M1's tail is the round's real finding.** Correcting the decoys did *not* make the guard
discriminate. Four sabotages later the reason is settled: `DtoConstructorRegistry.Register<T>`
and `PreserveType<T>` declare **no type constraint** (`DtoConstructorRegistry.cs:22,:43`), so
a type argument that binds to the wrong type compiles perfectly well. **A compile check is
structurally blind on this leg.** The leg *is* covered — by `EventPreservationDiscoveryTests`,
which assert on emitted text and reddened on RP-3, RP-4 and RP-5 (1, 3 and 6 tests
respectively). The new test sits on top of them as a smoke test and is labeled as such.

### Should-cover

| # | Finding | Disposition |
|---|---------|-------------|
| S1 | The **non-`Task`** return-type assignment is changed but never discriminated: both fixtures return `Task<Payload>`, so the generic branch overwrites it, and RP-1 reverted both lines as one edit. | **Attempted, failed, declared.** A synchronous `[Execute]` was added per the reviewer's suggested shape; sabotaging the non-`Task` line alone still left 762 green (**RP-2**), because `StaticFactoryRenderer:99` wraps every delegate in `Task<>` so both paths converge before reaching a shadowable position. Recorded as **unmeasured**; the sync operation is kept with that result written beside it, since removing it would delete the evidence that the obvious fix does not work. |

### Tech debt — fixed in place (all four were one-line accuracy defects)

- `FactoryEventPhaseSchedulerConcurrencyTests.cs` remarks described `Clear()` in the present
  tense and pointed the reader at "`Clear()`'s own XML doc" — a member **this plan deleted**.
  The plan's own Current State had enumerated that exact line and removed only the other hit.
- `AssemblyAttributeEmissionTests.cs` cited "Amendment A1" for the removed-decoy story, which
  is **A2/A3**.
- Recorded but not actioned (pre-existing, no behavior change here): `Types.cs:692` classifies
  a return type by substring-matching a minimally-qualified name — same brittle source as the
  bug A1 fixed, three lines up; and `Types.cs:841` takes parameter types from **source text**
  (`ToFullString()`), so generated signatures inherit whatever `using`s the consumer's file
  carried. The second is the consumer-type sibling of A3 and **row 013's framing does not name
  it** — worth folding in when 013 is drafted.

The reviewer also answered a brief question directly: there is **no other emission-path
`.ToString()`** in `FactoryGenerator.Types.cs`.

### Sacred tests

**None touched.** Test diff `+239 / −0`. The only deletion in the plan is production
(`FactoryEventHandlerRegistry.Clear()`), which the plan chartered and the reviewer verified
had zero call sites. The surviving pin —
`RegistryEntriesAreKeyedByEventType_SoPerTestEventTypesAreSufficientIsolation` — was checked
and is not vacuous: it exercises **both** halves of the `(event type, handler class)` key.

### Suites at close of round 1

Unit **762×2 TFMs**, integration **595×2 (+5 standing skips)**, Design **98×2**. Both
solutions built explicitly. Logs: `011-build.log` (0 errors), `011-test.log`,
`011-redproof.log` (RP-0 … RP-6).

### Worth carrying forward

**Five sabotages this round, four of them green against prediction — and not one was a bad
sabotage.** Each green result was the finding: RP-2 showed the obvious fix for S1 doesn't
work and why; RP-3 confirmed an immunity from the opposite side; RP-4 and RP-5 walked down
the wrong bucket and then the undiscovered event; RP-6 ended the line by locating the actual
reason (no generic constraint). The lesson is not "predict better" — it is that **a green
sabotage is data, and stopping at the first one would have shipped a guard whose label
implied coverage it could never provide.** The cost was five cycles; the alternative was a
fourth "guard" in the suite that could not fail.
