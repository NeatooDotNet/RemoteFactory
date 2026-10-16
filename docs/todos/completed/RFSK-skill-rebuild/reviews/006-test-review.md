# Test Review — RFSK-006 — 2026-10-16 — Round 1

Written by the orchestrator from the test-reviewer's report.

**Verdict: APPROVED**

**Logs:** no build or test on this branch, which changed only files under `docs/todos/`; the arc's last run (`reviews/005-test.log`, `005-design-test.log`) is green, and the close-out audit runs it again.

## Veto-tier / Callouts / Tech-debt

None.

- **Bullet 1:** the reviewer ran `diff -rq skills/RemoteFactory ~/.claude/skills/RemoteFactory` itself: empty. AC-8 is re-checked at close.
- **Bullet 2:** `006-runs/` holds ten prompts and ten outputs of 258–766 lines; s1–s5 are byte-identical to SKR-007's (`cmp`); the scoring file records the mechanism.
- **Bullet 3:** the scoring is a judgment, evidenced by line and re-read by the code review.
- No test changed.

## Could not verify (both resolved)

- Exit codes were not recorded. **Resolved:** the run's printed summary (each `exit 0`, with line counts) is added to `006-fresh-session.md` under "Run record".
- The command line was not kept. **Resolved:** the exact command is in the same section.

---

# Test Review — RFSK-006 — 2026-10-16 — Round 2

Written by the orchestrator from the test-reviewer's report, after Amendments 1 and 2 changed the evidence behind the map.

**Verdict: APPROVED**

**Logs:** none on this branch (`git diff d85feaa HEAD -- src/` is empty). The arc's last run, `005-test.log` and `005-design-test.log`, is green.

## Veto-tier / Callouts / Tech-debt

None.

- **Bullet 1:** the reviewer ran `diff -rq skills/RemoteFactory ~/.claude/skills/RemoteFactory` itself at `6b52b3a`: empty.
  - The only change under `skills/` between the runs is the one `setup.md` line.
  - The run2 transcripts hold the old sentence, and the run3 transcript holds the new one. So each run read the skill as it stood at that run.
- **Bullet 2:** the reviewer audited all eleven run2 and run3 transcripts with its own script:
  - `dontAsk` in every one, and version 2.1.301;
  - each session in its own empty directory, with empty memory;
  - no repository `CLAUDE.md` loaded;
  - only Skill, Read, Grep and Glob were used, every call into `~/.claude/skills` except the two denied attempts the scoring names;
  - `iso-run.sh` matches the documented command;
  - the probe output matches its table;
  - all twelve `.err` files are identical, holding only the two settings notices;
  - the outputs are byte-identical to the scratch copies.
- **Bullet 3:** the T18 fall and its cause are confirmed (run2 out10:1, `setup.md` read), and the run3 lines show what the scoring cites. The signature screen is clean.

## Could not verify

- **Exit codes.** `iso-run.sh` printed them to the orchestrator's console only. **Resolved:** that printed summary is saved as `006-runs/exit-codes.txt`.
- **The sync at the moment of each run.** This cannot be re-checked now. The `setup.md` text each transcript saw supports it.
- **The code review of the run2 and run3 scoring.** Round 2 of the code review is pending, and this closes when it lands.

## Theoretical (taken)

- The scoring file now says in one line that s1–s9 ran before Amendment 2, and why "none fallen into after the amendment" still holds.
- run3 out10:21 ("the `[Remote]` method bodies … only ever run on the server") describes `[Remote] internal` bodies. It is now named in the T18 cell as accurate.
- Built-in deferred tools (LSP, Monitor, RemoteTrigger and others) were not denied, and no session used one. This is recorded in the Run record.
