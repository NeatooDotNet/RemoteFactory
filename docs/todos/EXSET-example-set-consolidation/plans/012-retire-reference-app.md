# Retire reference app; path and design sweep

**Plan #:** 012
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-6
**Status:** Draft
**Last Updated:** 2026-10-11
**Plan-review opt-in:** set at Step 2
**Code-review opt-in:** set at Step 2
**Branch:** cut from EXSET at Step 2
**PR:** —

---

## Scope

Delete `src/docs/reference-app` once nothing embeds from it, and update every path that names it: `CLAUDE.md`'s skill-sample workflow and stale facts (T-16), the `.claude/agents` files, `scripts/extract-snippets.ps1`, and `SKILL.md`'s snippet-source line. Delete or regenerate `CODING_STANDARDS.md` (T-15). Sweep `CLAUDE-DESIGN.md` and `src/Design/README.md` for what plans 002-004 did not touch (D18 N+1 wording, "server-only" method injection, the checklist and debt sections). Close every remaining ledger row.
