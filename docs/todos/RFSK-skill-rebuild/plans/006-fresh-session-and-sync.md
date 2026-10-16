# Fresh-session test, then sync

**Plan #:** 006
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-7, AC-8
**Status:** Draft
**Last Updated:** 2026-10-16
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** —
**PR:** —

---

## Scope

Sync the rebuilt skill to `~/.claude/skills/RemoteFactory` (`diff -rq` empty), then run the fresh-session test as SKR-007 did: one headless `claude -p` per scenario in an empty directory with `--allowedTools "Skill Read Glob Grep"`, the five zCRM scenarios from `Neatoo/.../SKR-neatoo-skill-rebuild/reviews/007-runs/` plus new scenarios that give T4, T6, T11, T14 and T18 an occasion and exercise the four findings SKR's run never reached: the "persistence routing" framing, N+1 as the reason child operations are `internal`, a repository-named interface factory, and `[Remote, Create]` as the lead example. Scored row by scenario in `reviews/006-fresh-session.md`; the bar is no RemoteFactory row fallen into. Does NOT edit the skill except to fix a row the run falls into, recorded as an amendment.
