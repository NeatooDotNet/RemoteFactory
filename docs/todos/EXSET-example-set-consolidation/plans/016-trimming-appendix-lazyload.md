# Trimming, appendix, LazyLoad removal

**Plan #:** 016
**Date:** 2026-10-15
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4, AC-5
**Status:** In Progress
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

- [ ] Every C# block on `trimming.md` and both appendix pages is a Design embed or WRONG `[explicit-skip: grep]` · Must
- [ ] `trimming.md` states one `TrimMode`, matching the experiment `[explicit-skip: reviews/016-trim-*.log]` · Must
- [ ] A public-setter `IFactorySaveMeta` object keeps `IsNew`/`IsDeleted` across a trimmed round trip, in both formats `[integration: trimming harness, publish-trimmed]` · Must
- [ ] LazyLoad appears in no doc page, README section or skill file `[explicit-skip: grep]` · Should (AC-5)
- [ ] mdsnippets clean; both solutions green; the trimming gate green `[explicit-skip: meta]` · Must

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

---

## Gate Record

---

## Plan Amendments

---

## Notes

- **Run unattended** under Keith's standing instruction.
