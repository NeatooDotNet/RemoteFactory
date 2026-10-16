# Fresh-session test, then sync

**Plan #:** 006
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-7, AC-8
**Status:** In Progress
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (no code; a test of the skill)
**Code-review opt-in:** Yes (the scoring is a judgment; a second reader checks it against the outputs)
**Branch:** RFSK-006-fresh-session-and-sync
**PR:** —

---

## Scope

Sync the rebuilt skill to `~/.claude/skills/RemoteFactory` (`diff -rq` empty), then run the fresh-session test as SKR-007 did: one headless `claude -p` per scenario in an empty directory with `--allowedTools "Skill Read Glob Grep"`, the five zCRM scenarios from `Neatoo/.../SKR-neatoo-skill-rebuild/reviews/007-runs/` plus new scenarios that give T4, T6, T11, T14 and T18 an occasion and exercise the four findings SKR's run never reached: the "persistence routing" framing, N+1 as the reason child operations are `internal`, a repository-named interface factory, and `[Remote, Create]` as the lead example. Scored row by scenario in `reviews/006-fresh-session.md`; the bar is no RemoteFactory row fallen into. Does NOT edit the skill except to fix a row the run falls into, recorded as an amendment.

---

## Intent

- Evidence, not belief, that a session with only the installed skills answers the habit questions the way the model does.
- Any row the skill still lets a session fall into is found here, fixed, and re-run before the todo closes.

---

## Framework & Architectural Alignment

- SKR-007's mechanism and scorer (`Neatoo/docs/todos/completed/SKR-neatoo-skill-rebuild/reviews/007-fresh-session.md`): the same preamble, the same tool restriction, the same cell format (`pass - what it did (outN:lines)`, or `-` when the scenario gives the row no occasion).
- The rows are VisionAlignment section 4's, as `SKILL.md` states them; the four findings are SKR-007's "For the RemoteFactory sibling" list.

---

## Constraints & Invariants

- The installed copy is byte-identical to the repository's at the run, and again at close.
- The sessions run outside the repository: no repository `CLAUDE.md`, no project memory, no `src/Design`.
- A row is fallen into when the output does what the row's question assumes; a scenario that does not reach a row scores `-`, not a pass.

---

## Steps

1. Sync the skill to `~/.claude/skills/RemoteFactory` and confirm `diff -rq` is empty.
2. Copy SKR-007's five prompts; write five new ones, three of them for plain RemoteFactory without Neatoo, so the RemoteFactory skill is the only design guidance.
3. Run each prompt in its own empty directory with the read-only tool set; keep prompts and outputs in `reviews/006-runs/`.
4. Score every output against the eleven rows and the four findings in `reviews/006-fresh-session.md`.
5. If a row is fallen into, fix the skill, re-sync, re-run that scenario, and score the re-run beside the first.

---

## Acceptance

- [ ] `~/.claude/skills/RemoteFactory` matches the repository copy `[explicit-skip: diff -rq]`
- [ ] Ten fresh sessions, one per scenario, ran with only the installed skills and read-only tools `[explicit-skip: the run's mechanism, recorded in the scoring file]`
- [ ] No output falls into a RemoteFactory trap row or one of the four findings `[explicit-skip: scored by reading; checked by the code review]`

---

## Current State (Pre-Flight)

Walked 2026-10-16 on the arc at `d85feaa`.

- `~/.claude/skills/RemoteFactory` differed from the repository copy in all eleven files and still held `references/lazyload.md`.
- Claude Code 2.1.301 is on the path (`~/.local/bin/claude`); SKR-007 ran on 2.1.296.
- SKR-007's prompts are `007-runs/s1.txt`–`s5.txt`, each a shared preamble, a blank line and the scenario. Its scored rows for RemoteFactory were T3, T5, T13, T15, T19, T20 (and T14, T18 passed without a specific occasion); T4, T6, T11 had no occasion.

---

## Punchlist

- 

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Installed copy matches | Required | `[explicit-skip]` | `diff -rq skills/RemoteFactory ~/.claude/skills/RemoteFactory` empty after the sync (the stale `lazyload.md` removed) | ✓ |
| Ten fresh sessions, read-only tools | Required | `[explicit-skip]` | `reviews/006-runs/s1-s10.txt` and `out1-out10.md`; mechanism in `reviews/006-fresh-session.md`; every session exited 0 | ✓ |
| No row or finding fallen into | Required | `[explicit-skip]` | `reviews/006-fresh-session.md`: every row reached by at least two scenarios except T3 (s4), every finding reached, none fallen into | ✓ |

No code changed on this branch, so the full suite was not re-run here; the arc's last run is `reviews/005-*.log` (green), and the close-out audit runs it again.

---

## Gate Record

- 

---

## Plan Amendments

---

## Notes

- New scenarios: s6 (an aggregate with a child list, plain RemoteFactory: T4, T11, T15, the N+1 framing), s7 (a merge screen that binds and confirms: T6, T13), s8 (a pure-computation service and a server-only one, plain RemoteFactory: T14), s9 (a recent-contacts list and a server-only external lookup: T5, T18, a repository-named interface factory), s10 (what RemoteFactory is for, and a create that needs the server beside one that does not, plain RemoteFactory: the persistence-routing framing, `[Remote, Create]`).
