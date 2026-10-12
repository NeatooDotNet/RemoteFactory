# Design in CI, snippet rule, findings ledger

**Plan #:** 001
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-4, AC-7
**Status:** Done
**Last Updated:** 2026-10-11
**Plan-review opt-in:** No (CI configuration and bookkeeping; no API, schema or domain change)
**Code-review opt-in:** No (mechanical; no behavior change)
**Branch:** EXSET-001-ci-rule-ledger
**PR:** #118

---

## Scope

Add `src/Design/Design.sln` to `build.yml`'s build and test steps so Design is checked on every main push and on the arc's PR into main. Add a `.claude/rules` file naming Design as the only snippet source, with the region naming convention and the both-solutions build rule; add `*.md text eol=lf` to `.gitattributes`. Create `audit-ledger.md` in this todo: all 86 audit findings, each assigned to the plan that owns it, with an outcome column the owning plan fills in; skill-prose findings marked for Todo 2. Does not change any example, doc page or test.

---

## Intent

- A Design change that breaks the build or a Design test fails CI, the way a library change already does. Today `Design.sln` is checked by nobody but the person who remembers to build it.
- Every later session that touches Design or a doc page is told, by a path-scoped rule, where snippets come from and how regions are named, so no new hand-written C# or second example set appears.
- Every audit finding has an owner from the start, so AC-4 is a ledger to close, not a re-audit at the end.

---

## Framework & Architectural Alignment

- The repository's CI standard (one `build.yml`, Release build, trx test results) and Neatoo's equivalent step, which builds and tests `Design.sln` after the main solution.
- Neatoo's `.claude/rules/design-snippets.md` and `.gitattributes` as the model, adapted: RemoteFactory's existing region names are kept when a region migrates, so embeds need no marker edits.
- R-A (Design is the one example set), R-B (skill prose belongs to Todo 2).

---

## Constraints & Invariants

- The main solution's build, tests and the trimming verification step are unchanged.
- `build.yml`'s triggers are unchanged; per `docs/todos/CONVENTIONS.md`, plan PRs into the arc deliberately get no CI.
- No `.md` content changes from the `eol` rule; at most line endings in the working tree.

---

## Steps

1. Add build and test steps for `Design.sln` to `build.yml` after the main solution's, in Release, with their own trx results.
2. Add `.claude/rules/design-snippets.md`, scoped to `src/Design/**`, `docs/**`, `README.md` and `skills/**`: Design is the only snippet source; region names are unique, never bare words, and a migrating region keeps its name while its old definition is deleted in the same change; the only hand-written C# is a short WRONG block; build and test both solutions, then run mdsnippets with its log outside the repo.
3. Add `*.md text eol=lf` to `.gitattributes` and renormalize, confirming no content change.
4. Write `audit-ledger.md`: one row per finding (ID, one-line gist, owning plan or Todo 2, outcome), owners assigned by the page or source file each finding lives in.

---

## Acceptance

- [ ] `build.yml` builds and tests `Design.sln` in Release, and the same commands pass locally `[explicit-skip: CI configuration; verified by a local Release run now and by CI on the arc's PR into main]` · Must
- [ ] Both solutions build and test green `[explicit-skip: meta-bullet]` · Must
- [ ] The ledger has 86 rows and every row names an owning EXSET plan or Todo 2 `[explicit-skip: bookkeeping; counted]` · Must
- [ ] A rules file scoped to Design, docs, README and skill states the snippet source and region naming `[explicit-skip: guidance file]` · Could
- [ ] `.gitattributes` forces LF for `.md` and renormalizing changes no content `[explicit-skip: repository configuration; verified by git diff]` · Could

---

## Current State (Pre-Flight)

- `build.yml` restores, builds and tests only `src/Neatoo.RemoteFactory.sln`, then publishes the trimming harness. Triggers: push to main and `v*` tags, PRs into main, manual dispatch.
- `Design.sln` holds Design.Domain, Design.Tests, Design.Server and Design.Client.Blazor. Design.Domain and Design.Tests reference `RemoteFactory.csproj` and the generator by project reference, so a restore of `Design.sln` builds them from source.
- `.gitattributes` holds only `*.sh text eol=lf`. The working tree uses `core.autocrlf` (git warns "LF will be replaced by CRLF" on new `.md` files).
- `.claude/` has `agents/`, `agent-memory/` and `settings.local.json`; no `rules/`.
- Neatoo's rule uses `skill-*` / `docs-*` names for new regions. RemoteFactory's 135 existing region names are topic-prefixed (`operations-*`, `save-*`, `skill-*` ...); `src/Tests` and `src/Generator` already use bare-word region names (`Fetch`, `Remote`, ...), which mdsnippets also scans.

---

## Punchlist

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| `build.yml` builds and tests `Design.sln` in Release | Must | explicit-skip | Same commands run locally: `reviews/001-design-build.log` (0 errors, 0 warnings), `reviews/001-design-test.log` (104 passed on net9.0 and net10.0). CI confirms on the arc's PR into main. | ✓ |
| Both solutions green | Must | explicit-skip | `reviews/001-build.log` (0 errors; 2 warnings, the known WASM SQLite notice), `reviews/001-test.log` (829 unit, 632 integration + 5 pre-existing skips, both TFMs) | ✓ |
| Ledger: 86 rows, every row owned | Must | explicit-skip | `audit-ledger.md`: 86 rows (C 30, I 15, T 29, U 12), 0 with an empty Owners cell | ✓ |
| Rules file | Could | explicit-skip | `.claude/rules/design-snippets.md`, scoped to `src/Design/**`, `docs/**/*.md`, `README.md`, `skills/RemoteFactory/**` | ✓ |
| `.md` forced to LF, no content change | Could | explicit-skip | `git add --renormalize .` staged no `.md` file | ✓ |

---

## Gate Record

- Round 1 (2026-10-11): test-reviewer skipped: no test or code changed; every bullet is an explicit-skip with its evidence above. Done.

---

## Plan Amendments

### 2026-10-11 — Renormalize scope

- **Section affected:** Steps 3
- **Original said:** Add `*.md text eol=lf` and renormalize.
- **What changed:** The repository-wide renormalize also restaged `docs/todos/completed/DomainModel.PersonFactory.g.cs.bug-evidence` (CRLF in the index, not `.md`); it was unstaged and left as is.
- **Why:** Out of scope; the rule concerns `.md` only.
- **Discovery Log:** none (detail, not intent)

---

## Notes

Framework skills (`/RemoteFactory`) not loaded at Step 2: this plan changes no domain code or example.
