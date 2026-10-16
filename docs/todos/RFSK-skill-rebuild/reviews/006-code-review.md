# Code Review — RFSK-006 — 2026-10-16 — Round 1

Written by the orchestrator from the code-reviewer's report.

**Verdict: CONCERNS** (one veto-tier finding)

**Logs:** none; no code changed on this plan. The arc's last run, `005-test.log` and `005-design-test.log`, is green.

**On the outputs:** the reviewer read all ten in full and found none falling into a trap row or into any of the four findings. Its finding is about the mechanism, not the outputs.

## Veto-tier

**V1. The first run was not isolated.**
- **Cause:** the user settings set `defaultMode: auto`, and they allow `Read`, `Grep` and `Glob` everywhere. `--allowedTools` therefore narrowed nothing.
- **What leaked:** the transcripts (`permissionMode: auto`) show six sessions reading the repositories:
  - s1, s3 and s7 read Neatoo `src` and `src/Design` through Bash;
  - s6 read RemoteFactory `src/Design` with Read and Grep;
  - s8 and s10 read RemoteFactory `src/Design` through Bash.
- **What it breaks:** the plan's invariant ("no `src/Design`"), AC-7's "given only the installed skills", and the mechanism line in `006-fresh-session.md`.
- **What was hit hardest:** s8 is T14's dedicated scenario; s10 is F-a's only scenario and F-d's only positive case.

**Disposition: Amend.**
- The first run's outputs are kept as `006-runs/run1/`, marked contaminated.
- All ten scenarios ran again into `006-runs/run2/`, under a mechanism that was first proven by a probe (`006-fresh-session.md`, "Run record"):
  - `--permission-mode dontAsk` and `--strict-mcp-config`;
  - Skill plus Read, Glob and Grep scoped to `~/.claude/skills/**`;
  - Bash, PowerShell, Agent, the Web tools and the edit tools denied;
  - Read denied under `~/source`, `C:\src`, `~/AppData`, and `~/.claude/projects`, `todos` and `plans`.
- **Verified afterwards:** each run2 transcript was checked for any tool call that reached outside `~/.claude/skills`.
- The scoring now rests on run2 alone.

## Callouts (all taken)

1. **s8 says "`[Remote]` methods run on the server" (out8:8, 316) and was not scored under T18.** Taken: T18's signature phrase is added to the screening list. Every run2 hit of the phrase is read and scored.
2. **The Passes column disagreed with its evidence; some reaching scenarios were not credited** (T18's out4:127, which is about `internal` operations; T13 missed s3 and s8; T14 credited s7 with no line and missed s3 and s4). Taken: the table is re-scored from run2. Each Passes entry carries its own line, and every scenario that reaches a row is credited.
3. **F-c credited s2 and s3, which have no interface factory, and missed s7's `IContactSearch`.** Taken in the re-score: F-c is credited only to scenarios that declare an interface factory.
4. **F-b's clean evidence (s4's placement reason) was uncited.** Taken in the re-score.
5. **s7's `ContactCard` record was the nearest thing to a transfer type, and T20 did not discuss it.** Taken: T20 now covers any hand-written record that crosses, with R-H's reading.

## Could not verify (resolved)

- **Exit codes.** Recorded for both runs in "Run record".
- **The sync before the runs.** `diff -rq` was empty immediately before run2, and the result is recorded.
- **The Claude Code version.** `claude --version` printed 2.1.301 before run2, and the result is recorded.
- **The stderr line.** It now names both notices.

## Theoretical (not triaged)

- **Observation 2 and the two-root transaction.** VisionAlignment §3 ("Across roots, an `[Execute]` seam or an event") covers it, so the observation is reworded if run2 repeats it.
- **SKR-007's own sessions ran Bash.** That is outside this plan. It is recorded for the Neatoo session in the message sent at close.
- **The skill's snippet-source links name `/src/Design` paths.** They point only at the repository, so only the test is affected.

---

# Code Review — RFSK-006 — 2026-10-16 — Round 2

Written by the orchestrator from the code-reviewer's report.

**Verdict: APPROVED**

**Logs:** none apply. Only markdown changed (`docs/todos/**` and prose in the skill's references).

## What was confirmed

- **V1 resolved.** All eleven run2 and run3 transcripts were parsed:
  - `dontAsk` throughout, with only Skill, Read, Grep and Glob used;
  - every call into `~/.claude/skills`, except the two denied attempts the scoring names;
  - only `~/.claude/CLAUDE.md` loaded;
  - each output is identical to its transcript's final text;
  - the probe, the stderr and the command all match the Run record.
- **The `setup.md` amendment** matches VisionAlignment §2 statement 4 and D18 nearly word for word, and contradicts nothing in the skill. The sync is clean, and #171 is the right home for the `docs/` copy.
- **The scoring holds.** All eleven outputs were read in full, and every cited range was checked. On the three observations:
  - **1, s7's `[Remote, Create]`:** not a fall.
  - **2, run3's leftover clause:** not a fall under the scoring's rule.
  - **3, s7's unassigned server-side Saves:** not a fall, but a departure from "reassign either way" that is rightly recorded for Keith.
- **Round-1 callouts:** 1–4 addressed, and 5 partly (callout 2 below).

## Callouts (both taken)

1. **The T18 boundary between run2's fall and run3's pass was recorded unevenly.** Observation 2's excuse also covers run2's second clause, so the fall rests on the first. **Taken:**
   - the Verdict names the first clause as the fall;
   - Observation 2 cites run3 out10:21, :215 and :299, and calls the pass narrow.
2. **T20 omitted s2's `ContactSearchFilter` and `ContactListItem`.** **Taken:** both are added to the T20 cell with their R-H reading.

## Could not verify

- **Exit codes:** the evidence is `006-runs/exit-codes.txt`, a copy of the console summary. The scoring file now says so.
- **s1–s5 identical to SKR-007's prompts:** not re-checked here; the test review closed it.

## Theoretical (all taken)

- **Observation 1's counter-reading** ("`[Fetch]` … takes a key"): it is now answered in Observation 1.
- **s1–s9 ran against the old sentence:** the Verdict now says so, and says why the claim holds.
- **F-a's quote was a paraphrase:** the cell now quotes both runs verbatim.
- **The deferred tools were not denied:** recorded in the Run record.
- **`references/static-factory.md:130`**, "`[Remote]` alone decides where it runs": it now reads "decides whether the client's call crosses". The skill was re-synced, and no scenario was re-run for a phrase narrowed toward the row's answer.
