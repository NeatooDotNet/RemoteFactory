# Fresh-session test, then sync

**Plan #:** 006
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-7, AC-8
**Status:** Done
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (no code; a test of the skill)
**Code-review opt-in:** Yes (the scoring is a judgment; a second reader checks it against the outputs)
**Branch:** RFSK-006-fresh-session-and-sync
**PR:** #172

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
| Installed copy matches | Required | `[explicit-skip]` | `diff -rq skills/RemoteFactory ~/.claude/skills/RemoteFactory` empty: after the first sync (the stale `lazyload.md` removed), before run2, and again after the `setup.md` amendment, before run3 | ✓ |
| Ten fresh sessions, read-only tools | Required | `[explicit-skip]` | `reviews/006-runs/run2/out1-out10.md` and `run3/out10.md`. The isolated mechanism, the probe, the exit codes and the transcript audit are in `reviews/006-fresh-session.md` "Run record". run1 is kept but not scored, because it was not isolated (Amendment 1) | ✓ |
| No row or finding fallen into | Required | `[explicit-skip]` | `reviews/006-fresh-session.md`. run2 fell into T18 in s10's prose. The skill was amended (Amendment 2) and s10's re-run passes. Every row and finding is reached, and after the amendment none is fallen into | ✓ |

No code changed on this branch, so the full suite was not re-run here; the arc's last run is `reviews/005-*.log` (green), and the close-out audit runs it again.

---

## Gate Record

- Round 1 (2026-10-16): test review APPROVED; the exit codes and the command, which it could not verify, are now recorded — `reviews/006-test-review.md`. Code review CONCERNS, with veto V1: run1 was not isolated (Amendment 1); callouts 1–5 were taken in the re-score — `reviews/006-code-review.md`
- Round 2 (2026-10-16), after run2 and run3 and Amendment 2: test review APPROVED, with its theoretical items taken. Code review APPROVED: callouts 1 (the T18 boundary) and 2 (T20 records) were taken, and so were all its theoretical items, including the `static-factory.md:130` phrase — Done

---

## Plan Amendments

1. **2026-10-16, the run's mechanism (code review V1).** `--allowedTools` narrowed nothing: the user settings carry `defaultMode: auto` and a blanket `Read`/`Grep`/`Glob` allow. Six run1 sessions therefore read the repositories, three of them RemoteFactory's `src/Design`.
   - run1 is kept as `006-runs/run1/` and not scored.
   - All ten scenarios ran again as run2, under `--permission-mode dontAsk`, `--strict-mcp-config`, Read, Glob and Grep scoped to `~/.claude/skills/**`, and denies on the shells, agents, web tools, edit tools and the source, temp and project trees.
   - The isolation was proven by a probe first and verified from every transcript afterwards.
   - SKR-007's sessions ran the same way and also used Bash. That was reported to the Neatoo session (`desktopNeatooSkills`) on 2026-10-16, at the close-out audit (todo Discovery Log).
2. **2026-10-16, the T18 row (run2 s10).** s10's explanation gave the habit's answer: "`[Remote]` runs on the server … without `[Remote]` runs on the client". It came from `references/setup.md`, whose factory-modes sentence ended "everything else runs on the client".
   - The sentence now says the client's call to a `[Remote]` operation crosses, any other call runs on the tier that makes it, the same operation called on the server runs there locally, and `internal` without `[Remote]` is server-only.
   - The skill was re-synced and s10 re-run as run3, which passes.
   - `docs/getting-started.md:107` has the same sentence. It is published documentation, not the skill, so it is parked as #171.
   - At the round-2 code review, `references/static-factory.md`'s "`[Remote]` alone decides where it runs" became "decides whether the client's call crosses". The skill was re-synced; no re-run, because the phrase only narrows toward the row's answer.

---

## Notes

- New scenarios: s6 (an aggregate with a child list, plain RemoteFactory: T4, T11, T15, the N+1 framing), s7 (a merge screen that binds and confirms: T6, T13), s8 (a pure-computation service and a server-only one, plain RemoteFactory: T14), s9 (a recent-contacts list and a server-only external lookup: T5, T18, a repository-named interface factory), s10 (what RemoteFactory is for, and a create that needs the server beside one that does not, plain RemoteFactory: the persistence-routing framing, `[Remote, Create]`).
