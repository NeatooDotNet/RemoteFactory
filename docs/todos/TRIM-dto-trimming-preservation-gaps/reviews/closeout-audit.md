# TRIM — Close-Out Audit (Step 7, whole arc)

**Gate:** mandatory. **Pass:** one (2026-08-14). **Verdict: CONCERNS** — 5 veto-tier, 10 callout-tier. All veto-tier closed below.
**Scope audited:** the container in full (todo, 9 plans, 12 review files, 2 evidence sets), the generator/library/test/doc surface it touches, and 7 provided logs.

Every checkable finding was independently re-derived at the keyboard before being accepted. **All of them held.**

---

## Veto-tier

| # | Finding | Disposition |
|---|---|---|
| **V1** | **Two integration tests failed** — `FactoryEventRelayTests.SingleEventRelay_ConsumerReceivesEvent` and `MultipleEventsRelay_ArriveInServerRaiseOrder`, one failure per TFM. The auditor was asked to challenge the "known flake" label rather than accept it, and did: it traced the relay path to `git log` stopping at v1.4.0 (before the arc began), identified the mechanism as `Task.Run` + `Task.Yield` against a 2s poll deadline, noted both failures burned exactly `[2 s]`, and observed MSBuild still compiling Blazor WASM projects *interleaved* with two concurrent TFM hosts | **Closed by re-run: 561+561, 0 failed, both TFMs.** The arc does not close on an unexplained red. Diagnosis confirmed load-dependent, matching deferred item 10 and the 2026-07-06 Discovery Log entry |
| **V2** | **The AC6 carve-out was dishonest in the one place a generator editor reads first.** `InterfaceFactoryRenderer.cs:260` still said *"the trimmer removes the entire body"* — precisely the claim H1 measured insufficient for `async`. The leg emits the guard inline with no wrapper split (zero `Core(` methods in its emitted output) and still names `{ImplName}Factory`. `todo.md` claimed the retraction landed in "all three" places; it had landed in five, and missed this sixth | **Fixed.** The comment now states what folds, what does not, that this leg has received neither TRIM-009 fix, and that body elimination here is **unverified** — with the item 19 / item 20 dependency named. `todo.md`'s "all three" corrected to six, with the miss recorded rather than quietly fixed |
| **V3** | **AC4's release step had no enumerated obligations, and two concrete ones were untracked.** (a) Nine artifacts describe `v1.7.0` behaviour while the package is `1.6.1` — two in *undated present tense* on the published site, so a current consumer is told their `[Execute]` bodies are protected by a holder that ships in no installable version. The container tracked the inverse exposure (docs behind code) exhaustively and this one not at all. (b) The synchronous-throw behaviour change lives only in a Done plan's prose, and its commit is `fix:`-prefixed — so `CLAUDE.md`'s commit-scanning release process would emit a patch bug-fix line and omit it | **Fixed.** New deferred row **22** carries both, queued to the release step |
| **V4** | **Deferred item 4's own stated trigger fired inside the arc and the row was never updated.** It read "queue if the guard's message or shape is ever edited" while marked *not introduced by this arc*. TRIM-009 changed both the shape and the observable semantics. The test review had spotted it (S4); the row was not touched | **Fixed.** Row moved to **QUEUED**, with the root cause named: the `IsServerRuntime == false` path has never executed in any in-process test |
| **V5** | **TRIM-008 marked `Done` with 9 of 10 Acceptance bullets unchecked** — the only plan in the arc in that state, and the one closing half of AC6. The bullets were in fact satisfied (auditor traced each) | **Fixed.** All ten ticked |

## Callout-tier — disposition

**Fixed inline:** C1 (gate header still said `ClassAsyncBody_MARKER` "expected PRESENT", contradicting the code two lines below it), C5 (item 2 stale), C6 (`FactoryAttributes.cs`'s bolded CONTRACT was absolute where the interface leg is the exception — now cross-referenced at the claim), C4 (AC5 was self-cancelling: it demanded consumer proof, then made itself non-binding in its own parenthetical).

**Queued as new rows:** C2 → row **23** (two `FactoryEventRelayTests` tests pass vacuously under exactly the condition that reddens their siblings — *a check that cannot go red, inside the class this arc has called a flake for five weeks*, which makes item 10's frequency an undercount by construction). C3 → item **11** widened (TRIM-009 routed its Step 7B to a row whose rationale does not cover it; `ClassFactoryWithExecute.cs` is the one Design source-of-truth file silent on the shape AC6 was held open for).

**Accepted with reason:** C7 (`attributes-reference.md` attributes `[Execute]` body removal to the guard alone — incomplete, not false, and it links to `trimming.md`), C8 (`TrimmingTests/README.md` "How It Works" predates the wrapper/holder — omission, to fix at the release step), C10 (AC6 measured on one TFM and one RID; same SDK therefore same ILLink, so the risk is small, but the wording is "every shape that can be measured" and the measurement is single-TFM).

**Queued separately, pre-existing and outside arc scope:** C9 — `skills/RemoteFactory/references/polymorphic-hierarchy.md` cites a file in the private zTreatment repo, violating `CLAUDE.md`'s self-containment rule for the distributable skill.

## What the audit confirmed

- **AC1–AC3 trace to specific generator code and are proven in a publish-trimmed artifact**, not inferred — the distinction this arc paid three cycles to learn. AC3 in particular was *verified red first* (TRIM-003) and then fixed (TRIM-007).
- **AC6 closes as written**, not narrowed: five emission sites through one helper, three holder prefixes distinct at character 7, a CI gate with 11 positive controls and ~45 absence assertions across 8 legs plus 6 state-machine discriminators, and a liveness check proving the holders actually forward.
- **41 of 41 cited unit-test methods exist** across all nine plans. Zero fabricated citations. The sacred-tests rule holds — the single modification to a pre-existing test is an inversion that preserves intent and adds a regression assertion.
- **Plan Index reconciliation is clean**: 9 files, 9 rows, no orphans, every header `Status` matching its row, abandonment reason filled and specific, all three skipped gates carrying recorded reasons.

## On evidence quality across the arc — the auditor's most useful observation

Earlier plans are **honest but structurally weaker** than the recent two, and they say so themselves rather than hiding it. TRIM-001/002/007 backed their trimmed-harness claims with keyboard negative controls that were performed and described but **not archived** — real, but not reproducible by a later reader the way `009-evidence/` is. TRIM-003 and TRIM-004 are `explicit-skip` throughout. TRIM-006 is the weakest, and the container already records why.

The auditor named the recent plans' *self-corrections* as the strongest honesty signal in the arc: TRIM-009 **withdrew** a size comparison rather than restate it from memory, corrected a mis-cited log, and downgraded its own red-before-green claim to "filtered to one test class" rather than let it read as a blast-radius statement.

## Remaining before the arc can close

AC4 and AC5 are correctly still open. What remains is the release itself:

1. Bump `src/Directory.Build.props` to `1.7.0`
2. Author `docs/release-notes/v1.7.0.md` — **including the synchronous-throw behaviour change with migration guidance**, which commit-prefix scanning will not surface (row 22)
3. Update `docs/release-notes/index.md` (highlights table, all-releases list, `nav_order` renumber)
4. Tag `v1.7.0`, let CI publish
5. AC4 closes there; AC5 is discharged by the release, with consumer rollout tracked in zTreatment PCB-003

**Worth queuing as Draft plans regardless of the release:** rows 23 + 10 together (the relay family — the vacuous-test half is cheap and makes the flake measurable), and items 19 → 20 in that order, since 19 is what makes 20 verifiable at all.
