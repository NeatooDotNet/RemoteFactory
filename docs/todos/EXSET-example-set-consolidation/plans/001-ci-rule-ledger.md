# Design in CI, snippet rule, findings ledger

**Plan #:** 001
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-4, AC-7
**Status:** Draft
**Last Updated:** 2026-10-11
**Plan-review opt-in:** set at Step 2
**Code-review opt-in:** set at Step 2
**Branch:** cut from EXSET at Step 2
**PR:** —

---

## Scope

Add `src/Design/Design.sln` to `build.yml`'s build and test steps so Design is checked on every main push and arc PR. Add a `.claude/rules` file naming Design as the only snippet source, with the region naming convention (no bare-word names, no name still defined elsewhere) and the both-solutions build rule; add `*.md text eol=lf` to `.gitattributes`. Create `audit-ledger.md` in this todo: all 86 audit findings, each assigned to the plan that owns it, with an outcome column the owning plan fills in; skill-prose findings marked for Todo 2. Does not change any example or doc page.
