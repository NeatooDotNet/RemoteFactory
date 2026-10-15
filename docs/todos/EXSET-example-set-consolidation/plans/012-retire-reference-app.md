# Retire reference app; path and design sweep

**Plan #:** 012
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-6
**Status:** Done
**Last Updated:** 2026-10-15
**Plan-review opt-in:** No. The deletion is gated by mdsnippets and the builds, and every wording change applies a ruling already made (R-A, R-G, R-K, D15, D18).
**Code-review opt-in:** Yes. It edits the project's instruction files (`CLAUDE.md`, `.claude/agents`) and the requirements doc (`CLAUDE-DESIGN.md`).
**Branch:** EXSET-012-retire-reference-app
**PR:** #147

---

## Scope

- **Delete `src/docs/reference-app`** (AC-6). `src/docs/samples` is already gone. Its findings' unpublished sites (C-13, C-23, T-29, and the unpublished halves of C-07, C-09, C-14, C-18, C-24) retire with it.
- **Delete the dead snippet tooling:** `scripts/extract-snippets.ps1` reads `docs/samples` and `#region docs:` markers, and neither exists.
- **Delete `CODING_STANDARDS.md`** (T-15, I-04). It is stale throughout, nothing links it, and `Directory.Build.props`, `.editorconfig` and `CLAUDE.md` are what it described.
- **`CLAUDE.md`:**
  - the skill-sample workflow and key files name Design (R-A, T-22);
  - the tree drops `reference-docs`, and "26 tests" goes (T-16);
  - "Data Mapper Factory" becomes D15's framing (C-28);
  - method injection resolves on the tier that runs the method (I-02);
  - runtime enforcement gives both outcomes with their conditions (T-16);
  - entity duality is stated as R-G (U-09).
- **`.claude/agents`:** the docs-writer and requirements-documenter name Design, not the reference app.
- **`SKILL.md` "Code Sample Sources":** names Design as the source, without the "guaranteed to compile" overclaim (T-22). The rest of the skill's prose is Todo 2's.
- **`CLAUDE-DESIGN.md`:** the LazyLoad Quick Decisions rows and checklist line (R-K), the checklist's states, and the private-setter debt row's reason. Its D18 and method-injection wording is already fixed.
- **Close every remaining ledger row** that names 012.

Does NOT touch:
- Design's `LazyLoadExample` and its tests, or the library's LazyLoad types (#114);
- `src/Examples` (Follow-on);
- the skill's reference prose (Todo 2).

---

## Intent

Design is the only example set, and nothing in the repository's instructions points anywhere else. `CLAUDE.md` and `CLAUDE-DESIGN.md` state what the rulings say.

---

## Framework & Architectural Alignment

- **Rulings:** R-A, R-G, R-K, D15, D18.
- **`.claude/rules/design-snippets.md`.**

---

## Constraints & Invariants

- **Nothing embeds from the reference app** before it is deleted: mdsnippets reports no missing snippet after.
- **The whole still works:** both solutions green, mdsnippets swallows nothing.

---

## Steps

1. Delete the reference app, the extract script and `CODING_STANDARDS.md`.
2. Rewrite the `CLAUDE.md`, agent and `SKILL.md` paths and statements.
3. Sweep `CLAUDE-DESIGN.md`.
4. Verify and record: mdsnippets, both solutions, the ledger.

---

## Acceptance

- [x] `src/docs/reference-app` and `src/docs/samples` do not exist, and no file outside the historical records names them `[explicit-skip: grep]` · Must (AC-6)
- [x] `CLAUDE.md`, `.claude/agents` and `SKILL.md`'s source line name Design as the snippet source, and carry none of T-16's, I-02's, C-28's or U-09's statements `[explicit-skip: grep]` · Must (AC-4)
- [x] `CLAUDE-DESIGN.md` has no LazyLoad guidance `[explicit-skip: grep]` · Should (AC-5)
- [x] Every ledger row naming 012 has its outcome `[explicit-skip: ledger]` · Must (AC-4)
- [x] mdsnippets clean with no missing snippet; both solutions green `[explicit-skip: meta]` · Must

---

## Current State (Pre-Flight)

- **`src/docs/reference-app`:** 58 tracked files. It is in neither solution and not in CI. It holds 2 regions, and no page embeds either one.
- **Names it:** `CLAUDE.md` (the tree at `:56` as `reference-docs`, and the skill-sample workflow, categories and key files at `:73-99`), `.claude/agents/business-requirements-documenter.md:46, 178`, `.claude/agents/remotefactory-docs-writer.md:41`, and `SKILL.md:119, 137` ("the reference application").
- **`scripts/extract-snippets.ps1`:** defaults to `docs/samples`, which does not exist; nothing calls it.
- **`CODING_STANDARDS.md`:** 677 lines, linked from nowhere.
- **`CLAUDE.md`'s stale statements:** "Data Mapper Factory" (`:7`), "26 passing tests" (`:26`, `:48`), "Server-only services" (`:152`), "not-registered DI exceptions" (`:166`), and entity duality as "(Neatoo/CSLA pattern)" (`:164`). Design.Tests runs 176.
- **`CLAUDE-DESIGN.md`:** Quick Decisions rows `:405-406` answer deferred loading with `LazyLoad<T>`; checklist `:1102` lists it; the checklist mixes `[ ]` and `[x]` for patterns all present; the debt row at `:1112` gives "adds reflection, incompatible with IL trimming" as the private-setter reason, while the cause is that the generated converter cannot call a private setter.

---

## Punchlist

---

## Test Evidence

| Acceptance bullet | Evidence | Tier |
|---|---|---|
| 1 · the reference app and samples are gone and unnamed | Neither directory exists (`src/docs` is gone). `git grep -i` for `reference-app`, `reference app`, `reference application`, `reference-docs`, `src/docs/samples`, `EmployeeManagement`, `CODING_STANDARDS` and `extract-snippets`, outside `docs/todos`, `docs/plans` and `docs/release-notes`, finds two files: `.claude/rules/design-snippets.md`, whose line records the deletion, and `docs/VisionAlignment.md`, the ruling record | explicit-skip: grep, confirmed |
| 2 · instructions name Design, without the stale statements | `CLAUDE.md`'s skill-sample workflow, categories and key files, the agent files and `SKILL.md:119, 137` name Design. `git grep -i` for `Data Mapper`, `26 passing`, `26 tests`, `not-registered`, `Server-only services` and `Neatoo/CSLA` over `CLAUDE.md`, `.claude`, `CLAUDE-DESIGN.md`, Design's README, `README.md` and `SKILL.md` finds nothing (case-insensitive since the code review found the architect agent's lowercase "data mapper") | explicit-skip: grep, confirmed |
| 3 · no LazyLoad guidance in `CLAUDE-DESIGN.md` | The deferred-loading row answers with R-K; the `Lazy<T>` row and the checklist line are gone; the two Design Files rows say the files are kept only until #114 | explicit-skip: grep, confirmed |
| 4 · every 012 ledger row has its outcome | `audit-ledger.md`: 86 rows, none with an empty Outcome. The 9 T2-only rows say T2 | explicit-skip: ledger, confirmed |
| 5 · mdsnippets, both solutions | `reviews/012-build-{main,design}.log`: 0 errors; main's 2 warnings are the known `OrderEntry.BlazorClient` wasm notice. `012-test-main.log`: unit 855, integration 657 + 7 skipped; `012-test-design.log`: 175 + 1 skipped (#104); both TFMs. `012-mdsnippets.log` and, after the review punches, `012-mdsnippets-r2.log`: exit 0, no missing snippet. The only rendered page that changed is Design's README, which gained its six new embeds | explicit-skip: meta, confirmed |

---

## Gate Record

- **Test review** ([012-test-review.md](../reviews/012-test-review.md)): APPROVED, with one tech-debt item (the private-setter Quick Decisions reason), amended.
- **Code review** ([012-code-review.md](../reviews/012-code-review.md)): APPROVED, with 4 callouts, all amended:
  - Design's README hand-written blocks are now Design embeds;
  - the architect agent's "data mapper" framing is gone;
  - the private-setter debt row names `[UnsafeAccessor]` as the R-M revisit path;
  - `CLAUDE.md` and `README.md` say the library maps the endpoint.

  Three theoretical items were taken: the region-prefix sentence, `Design.Tests` in Key files, and a Follow-on line for `SKILL.md`'s "partial examples".
- **After the punches:** markdown only, so the round-1 build and test logs stand (unit 855, integration 657 + 7 known skips, Design 175 + 1 known-bug skip, both TFMs). `012-mdsnippets-r2.log` is clean.
- **Open, by design:** the skill's prose (Todo 2), `src/Examples` (Follow-on).

---

## Plan Amendments

- **Design's README** (code review callout 1): its Class, Interface and Static Factory blocks and the `IFactorySaveMeta` block were hand-written. One showed `[Remote] public` operations and another named types Design no longer has. All four are now Design embeds, and the save-routing list matches the generator, where `IsNew` and `IsDeleted` together means nothing to do.
- **Deleted, not updated:** `scripts/extract-snippets.ps1`. It reads a `docs/samples` directory and `#region docs:` markers that no longer exist, and nothing calls it.
- **Design's README** also had stale statements, which are now corrected: `OrderLine` as "server-side only", `Money` as a `[Factory]` record, and method injection as "server-only". Its tree now lists the read model, the context and `TaxRate`.
- **`CLAUDE.md`'s `[Remote]` list** now says a root `[Create]` is local unless it needs the server (D14, U-02's share). Its code-block categories drop "Partial/illustrative: hand-written", which R-A forbids. The skill's own prose keeps that category until Todo 2.
- **The 9 T2-only ledger rows** were given their T2 outcome here. Without it, AC-4's "one recorded outcome" would not hold.

---

## Notes

- **Run unattended** under Keith's standing instruction.
