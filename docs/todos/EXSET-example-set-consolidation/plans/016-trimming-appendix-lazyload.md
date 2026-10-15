# Trimming, appendix, LazyLoad removal

**Plan #:** 016
**Date:** 2026-10-15
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4, AC-5
**Status:** Done
**Last Updated:** 2026-10-15
**Plan-review opt-in:** Yes. The trimming page makes claims about what the trimmer keeps. The IFactorySaveMeta section is rewritten to R-M on a claim no test pins today. The required `TrimMode` is settled by experiment.
**Code-review opt-in:** Yes.
**Branch:** EXSET-016-trimming-appendix-lazyload
**PR:** —

---

## Scope

Split from EXSET-011 at its Step 2 (2026-10-15).

**`docs/trimming.md`:** replace its 8 C# fences with Design embeds, prose or WRONG blocks. Fix:
- **C-26:** "business rules/logic" becomes "persistence code" (R-N);
- **I-10:** one `TrimMode` statement (R-N), from the experiment in Current State;
- **I-01 and U-12:** the IFactorySaveMeta section is rewritten to public `get; set;` (R-M). "Either design is viable" goes, and so do the private-setter, `[JsonInclude]` and `[DynamicDependency]` recipe. The punchlist row closes;
- **T-02:** `partial`;
- **T-07:** `[Save]` in the prerequisite list;
- **T-09:** "client-side relay handlers";
- **T-26:** the CSLA `DataPortal_*` names in the diagram;
- **`EmployeeDto`:** found at the EXSET-015 gate.

**Trimming harness:** add a trimmed round-trip check that a public-setter `IFactorySaveMeta` object's `IsNew` and `IsDeleted` survive serialization in both formats. That pins the page's R-M statement under trimming.

**Appendix pages** (`appendix/serialization.md`, `appendix/record-reference-handling.md`):
- replace their C# fences with Design embeds or prose;
- T-27: describe RemoteFactory's own converters, and name Neatoo's types as an external framework's.

**LazyLoad (R-K):**
- delete `docs/lazyload.md`, the skill's `references/lazyload.md` and `LazyLoadSamples.cs`;
- remove LazyLoad from `SKILL.md` (trigger words, the decision rows, the reference index);
- record C-22 and T-01 as retired.

Design's `LazyLoadExample` and its tests stay for #114. `CLAUDE-DESIGN.md` is EXSET-012's.

Does NOT touch:
- the skill's `trimming.md` prose (Todo 2);
- the library's LazyLoad types (#114).

---

## Intent

The trimming reference states what a publish actually removes and keeps, with one `TrimMode` answer and the R-M shape for `IsNew`/`IsDeleted`. LazyLoad appears nowhere in the docs or the skill.

---

## Framework & Architectural Alignment

- **Rulings:** R-K, R-M, R-N and D15.
- **`.claude/rules/design-snippets.md`.**
- **`CLAUDE.md`'s LazyLoad section,** and the skill's self-containment rule.

---

## Constraints & Invariants

- **Embeds.** Every C# block on the three pages is a Design embed or WRONG. Generated code is not committed, so a block that shows generated output becomes prose.
- **The trimming gate stays green:** CI's publish, `verify-trimmed.sh` and the harness run.
- **The whole still works:** both solutions green, mdsnippets swallows nothing, the reference app builds.

---

## Steps

1. Run the TrimMode experiment, and add the IsNew/IsDeleted round-trip check to the trimming harness.
2. Rewrite `trimming.md`.
3. Rewrite the two appendix pages.
4. Delete the LazyLoad pages and sample, and edit `SKILL.md`.
5. Verify and record: mdsnippets, both solutions, the trimming publish (full, as CI runs it), and the ledger.

---

## Acceptance

- [x] Every C# block on `trimming.md` and both appendix pages is a Design embed or WRONG `[explicit-skip: grep]` · Must
- [x] `trimming.md` states one `TrimMode`, matching the experiment `[explicit-skip: reviews/016-trim-*.log]` · Must
- [x] A public-setter `IFactorySaveMeta` object keeps `IsNew`/`IsDeleted` across a trimmed round trip, in both formats `[integration: trimming harness, publish-trimmed]` · Must
- [x] LazyLoad appears in no doc page, README section or skill file `[explicit-skip: grep]` · Should (AC-5)
- [x] mdsnippets clean; both solutions green; the trimming gate green `[explicit-skip: meta]` · Must

---

## Current State (Pre-Flight)

- **`trimming.md`:**
  - 522 lines, 8 C# fences, mostly generated-code illustrations.
  - Its prerequisite list names `[Save]`, which does not exist (T-07).
  - The diagram uses `DataPortal_*` (T-26).
  - The checklist says "`TrimMode` is set to `full` (not `partial` or omitted)", while the DTO section says Blazor's `partial` trims `IsTrimmable` assemblies (I-10).
  - The IFactorySaveMeta section leads with private setters, `[JsonInclude]` and `[DynamicDependency]`, and calls public setters a tradeoff (I-01, U-12).
- **TrimMode experiment (2026-10-15).** The trimming harness was published from clean outputs (win-x64, self-contained), with `verify-trimmed.sh` and the harness run each time:

  | Setting | Absence gate | Harness |
  |---|---|---|
  | `TrimMode=full` (CI) | pass | pass |
  | `TrimMode=partial` | **fail** (43 checks: server-only code survives) | pass |
  | `TrimMode=partial` + `IsTrimmable=true` | pass | pass |

  So Blazor's default `partial` suffices once the domain assembly is `IsTrimmable` (Step 1 of the page), and `full` is not required. Logs: `reviews/016-trim-{full,partial-plain,partial-trimmable}-{publish,verify,run}.log`.
- **The harness** has a public-setter `IFactorySaveMeta` target (`SaveCanLegTarget`) but no serialization round trip of its flags.
- **LazyLoad** remains only in `docs/lazyload.md`, the skill's `references/lazyload.md` (both embedding `LazyLoadSamples.cs`), and `SKILL.md` (lines 4, 90, 91 and 104).

---

## Punchlist

---

## Test Evidence

| Acceptance bullet | Evidence | Tier |
|---|---|---|
| 1 · C# blocks are embeds or WRONG | Every fence on `trimming.md` and `appendix/*.md`, tagged or not, listed with its tag (awk over the opening fences). C# fences: `trimming.md`'s IL2091 block, labelled `// WRONG`, and the mdsnippets embeds (`trimming-event-nested-record`, `service-injection-constructor`, `getting-started-client-program`). Untagged fences: `trimming.md`'s publish diagram, the appendix's exception text and reference-flow diagram, and the type-name map; none holds C#. The ILSpy "Expected output" fence the round-1 test review found is now prose | explicit-skip: grep, confirmed |
| 2 · one `TrimMode`, matching the experiment | `trimming.md` says `TrimMode` needs no setting (Blazor's default `partial`, domain `IsTrimmable`), and checklist item 1 names `IsTrimmable`. Experiment: `reviews/016-trim-*.log`. CI's harness now publishes in that configuration: `reviews/016-ci-{publish,verify,run}.log`, all exit 0 | explicit-skip: logs, confirmed |
| 3 · `IsNew`/`IsDeleted` survive a trimmed round trip, both formats | `SaveMetaRoundTripSmokeTest` on `TrimCtorSaveTarget` (ctor-injected, so no ordinal converter): `IsNew=false`, `IsDeleted=true`, deserialized and serialized back, asserted on the serialized text, in Ordinal and in Named. Red before the generator fix (`reviews/016-savemeta-unfixed-run.log`, `{}` in both formats), green after (`016-trimfix-*-run.log`, `016-ci-run.log`). Emission pinned by `PreservePropertiesEmissionTests` (3 unit tests) | integration: publish-trimmed harness, confirmed |
| 4 · no LazyLoad in docs, README, skill | grep `lazyload\|lazy load\|ILazyLoadFactory` over `docs`, `README.md`, `skills`. Exempt: `docs/release-notes/` (historical; the v0.25.0 link is unlinked), `docs/todos/`, `docs/plans/`, `docs/VisionAlignment.md` (the ruling itself). One hit left: `SKILL.md`'s "lazy loading" trigger, kept so the defer-loading row's R-K answer is reached (plan-review callout 9) | explicit-skip: grep, confirmed |
| 5 · mdsnippets, both solutions, the trimming gate | Builds: `reviews/016-build-{main,design,refapp}.log`, 0 errors; the main build's 2 warnings are the known `OrderEntry.BlazorClient` wasm `NativeFileReference` notice. Tests: `016-test-main.log` (unit 855, integration 657 + 7 skipped, both TFMs), `016-test-design.log` (175 + 1 skipped, #104). `016-mdsnippets.log` exit 0; `git diff --stat` shows no swallowed section. Trimming gate: `016-ci-*.log` | explicit-skip: meta, confirmed |

---

## Gate Record

- **Plan review** ([016-plan-review.md](../reviews/016-plan-review.md)): DECISION NEEDED. Callout 1, a trimmed client writing `{}` for a public-setter `IFactorySaveMeta` class with no ordinal converter, went to Keith, who ruled "Fix in generator". The other 9 callouts were amended, apart from Person's recipe, which went to Follow-on.
- **Test review** ([016-test-review.md](../reviews/016-test-review.md)):
  - **Round 1: CONCERNS.** One veto-tier finding: bullet 1's grep saw only tagged fences, and missed the ILSpy "Expected output" fence that held C#. That fence is now prose, and row 1 lists every fence.
  - **Tech-debt:** CI no longer publishes the harness at `full`; it went to Follow-on (Could).
  - **Round 2: APPROVED,** with no findings.
- **Code review** ([016-code-review.md](../reviews/016-code-review.md)): APPROVED, with 5 callouts, all amended:
  - Named-format tracking is limited to `[Factory]` classes with a parameterless constructor;
  - the intro says four paths;
  - the harness claim is narrowed to the path it pins;
  - the "preserved via DI registration" claims are corrected;
  - row 5 names the 2 known warnings.
- **After the punches:** prose only, so the round-1 logs stand: main unit 855 and integration 657 + 7 known skips, Design 175 + 1 known-bug skip, on both TFMs. mdsnippets is clean, and the CI-shaped trimming gate is green.
- **Open, by design:** the skill's `trimming.md` prose (Todo 2); Person's recipe and a `full` CI publish (Follow-on); the release-note line for `PreserveProperties<T>()` (close-out).

---

## Plan Amendments

- **Library fix (Keith, 2026-10-15: "Fix in generator").** The plan review measured a trimmed client writing `{}` for a public-setter `IFactorySaveMeta` class with no ordinal converter. The registrar now emits `DtoConstructorRegistry.PreserveProperties<T>()` for every class factory without an ordinal converter. The harness check moved from `TrimSaveTarget`, which is on the converter path and would always pass, to the new `TrimCtorSaveTarget`. Release-note line owed.
- **Step 5's trimming publish** is in the configuration the page prescribes, not `full`: the harness csproj now sets `TrimMode=partial` with `IsTrimmable=true` (plan-review callout 5), so CI measures what the page tells readers to ship.
- **The appendix's T-27 rewrite** became four paths, not three: the Ordinal converter, which tracks no references (#133), is Path 1. `appendix/serialization.md` §3 no longer says identity is always preserved (callout 4).
- **`trimming.md` line 3** dropped the reference app's `IEmployeeRepository` for "your repositories".
- **Design build:** the first Design build failed with MSB3552 (`**/*.resx` cannot be found, `Design.Domain`, net10.0), a glob that did not expand; the unchanged retry built clean. The log kept is the retry's.

---

## Notes

- **Run unattended** under Keith's standing instruction.
