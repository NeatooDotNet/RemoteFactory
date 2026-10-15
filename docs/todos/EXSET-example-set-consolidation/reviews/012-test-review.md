## Test Review — EXSET-012 — [2026-10-15] — Round 1

**Verdict: APPROVED**
**Logs:** build PASSED — `reviews/012-build-main.log` (0 errors, 2 warnings: the `OrderEntry.BlazorClient` wasm `NativeFileReference` notice, net9.0 and net10.0), `reviews/012-build-design.log` (0 errors, 0 warnings); tests 3,374 passed, 0 failed — `reviews/012-test-main.log` (UnitTests 855/855 x2 TFMs; IntegrationTests 657 passed + 7 skipped x2 TFMs), `reviews/012-test-design.log` (Design.Tests 175 passed + 1 skipped x2 TFMs); `reviews/012-mdsnippets.log` finished with no missing-snippet line and no source file under `src/docs`

Reviewed: `git diff EXSET...HEAD` at 78838e8 (70 files, +191 / -6126).

### Findings

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| 1 | Tech-debt | `CLAUDE-DESIGN.md:377` (Quick Decisions, "Can I use private setters?") still gives "IL trimming + source generation" as the reason. The debt row at `:1110` now gives the converter-cannot-call-a-private-setter cause. | — | Amend: the row now gives the ordinal-converter reason (R-M), matching the debt row |

### Veto-tier
None.

- **Bullet 1 (Must, grep):** I re-ran the grep. `src/docs` does not exist, and `git ls-files src/docs` returns 0 files. A `git grep -il` for all eight cited terms, outside `docs/todos`, `docs/plans` and `docs/release-notes`, finds the same two files the map lists: `.claude/rules/design-snippets.md`, whose line records the deletion, and `docs/VisionAlignment.md`. The todo's Dismissed section already accepts the VisionAlignment hit as the as-found inventory (EXSET-005). An untracked-file grep finds the same two.
- **Bullet 2 (Must, grep):** The grep for `Data Mapper|26 passing|26 tests|not-registered|Server-only services|Neatoo/CSLA` over the cited scope finds nothing (exit 1). `CLAUDE.md:72-96`, both agent files and `SKILL.md:119, 137` name Design.
- **Bullet 3 (Should, grep):** `CLAUDE-DESIGN.md` mentions "lazy" in three places only. `:405` answers with R-K and says `LazyLoad<T>` is being removed. `:1219` and `:1225` are the Design Files rows, which say the files are "kept only until #114". The checklist has no LazyLoad line.
- **Bullet 4 (Must, ledger):** I parsed the ledger. It has 86 data rows and none has an empty Outcome. All 18 rows that list 012 as an owner have a 012 outcome. The 9 rows owned only by T2 (C-08, C-17, C-25, I-15, T-05, T-08, T-21, T-23, T-28) each record a T2 outcome.
- **Bullet 5 (Must, meta):** The log figures match the map. The logs are dated 17:15-17:16, before the 17:17 commit.
- **Sacred tests:** `git diff --name-status EXSET...HEAD -- src/Tests src/Design/Design.Tests` returns nothing. The deleted `EmployeeManagement.Tests` project is in scope. AC-6 deletes `src/docs/reference-app` as a whole, those tests pinned only types deleted with it, and the project was in neither solution nor CI. Nothing was weakened.

### Callouts (≤ 5 Optional)
None. No bullet is Optional. The plan adds no code path: every change is a deletion or edits markdown.

### Tech-debt (untiered, ≤ 5, one line each)
- `CLAUDE-DESIGN.md:377` "Can I use private setters?" still gives "IL trimming + source generation" as its reason, which disagrees with the `:1110` debt row this plan corrected.

### Could not verify
None.

### Theoretical (not triaged)
None.

### Read report
- Read beyond the brief: `src/Design/CLAUDE-DESIGN.md` (lazy, checklist and private-setter greps, to confirm bullet 3); `src/Design/README.md` (grep, for the Plan Amendment's server-only and `Money` claims); `reviews/016-test-review.md` (table format only).
- Named but unused: none.
