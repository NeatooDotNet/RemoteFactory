# Test Review — EXRM-003 — 2026-09-08 — Round 1

**Reviewer:** `test-reviewer` · **Budget:** tight · **Object:** `plans/003-trimming-gate-pair.md` Test Evidence map, branch `exrm-003-trimming-gate-pair` @ `73cb65c`

**Verdict: CLEAN**

**Logs:** build PASSED — `003-build-main.log` (0 errors, 6 warnings), `003-build-design.log` (0 errors); tests — UnitTests 777/777, IntegrationTests 626 passed + 5 skipped = 631, Design.Tests 102/102, each per TFM (net9.0 + net10.0), **0 failed** across all four runs. Counts flat vs EXRM-002 as predicted.

## Veto-tier

None. No failing test. `verify-trimmed.sh` is **66 insertions, 0 deletions** — every incumbent positive control, absence marker, and per-site discriminator is byte-identical; `check_present` is a new function beside `check_absent`, and the pair block sits after the discriminator loop, not inside any incumbent. Diffing the `ok` lines of `baseline-gate.txt` against `003-gate.txt` yields exactly `60a61,62` — two appended, none removed, renamed, or reworded. Sacred gate intact.

## Must-cover (plan-related)

**None — all four Must bullets are pinned.**

- **Bullets 1 / 2 (bare static, bare class — `[trimmed-harness]`)** — pinned twice over. `003-gate.txt` shows `ok BareStaticBody_MARKER` and `ok BareClassBody_MARKER`; `003-harness.txt` shows both "ran on the trimmed client, through the client-safe port" lines and exits "All checks passed."
- **The vacuity question resolves cleanly, in both directions.** The marker literals reach the compiled assembly only from the two `[Execute]` bodies — `TrimTestCommands.cs:86` and `ClassExecuteLegTarget.cs:114`. The four other occurrences are `//` comments (`Program.cs:227`, `TrimTestCommands.cs:74`), an XML doc `<c>` (`ClassExecuteLegTarget.cs:93`), and the gate script itself; none is a string literal, and `present()` greps only the single DLL passed as `$1` (`verify-trimmed.sh:35-53`), so no doc file is in scope. The first-attempt defect is genuinely closed.
- **The harness assertions cannot pass on a body that did not run.** `"|tallied:"` is produced in exactly one place — `ClientTallyEngine.ClientTallyCompute`, `ClientSafePorts.cs:55-56` — and that method is called from exactly two places, the two `[Execute]` bodies. `Program.cs:246-251` and `276-282` only *match* the stamp; they never produce it. The conjunction matters: echoing the caller's token alone would satisfy the token half, but nothing except the body reaching the port yields `|tallied:`. Sound.
- **Bullet 3 (siblings absent, nothing incumbent moved — `[trimmed-harness]`)** — pinned. The 60→62 arithmetic is right: `baseline-gate.txt` 60 `ok`, `003-gate.txt` 62 `ok`, `knownbad-gate.txt` 60 `ok` + exactly 2 `is MISSING` errors (the third `::error::` is the FAILED summary line, and it reads "2 check(s)"). `ClassExecBody_MARKER`, `_DoWork`, `_ProcessRecord`, `_DoAsyncWork`, `StaticAsyncBody_MARKER` and `<LocalRunExecCommand>d__` all still green in the final run.
- **Bullet 4 (red-before-green — `[explicit-skip: one-off falsification run]`)** — skip reason legitimate: a probe reverted before the implementation commit cannot exist as a standing test, and its output is archived and git-tracked. `knownbad-gate.txt` shows precisely the two present checks red with every other check ok; `knownbad-harness.txt` shows both invocations failing `NotSupportedException` from `NoOpHttpHandler` — the `[Remote]` variant routing to the wire, independent corroboration from a second mechanism.
- **Bullet 7 (meta)** — pinned by the four logs above plus the three exit-0 runs.

## Should-cover

**None.**

- **Bullet 5 (gate legend/summary — Should, `[explicit-skip: gate prose]`)** — `[P] present-by-design` in the legend (`verify-trimmed.sh:126-130`), the pair block header, and the new "Present:" paragraph, all visible in `003-gate.txt`.
- **Bullet 6 (Design sentences — Should, `[explicit-skip: comment prose]`)** — both rewrites claim only what the runs measured. `AllPatterns.cs` now says `[Remote]` is the cause and the guard the mechanism; cites the harness pair on the same shape; names the earlier claim it reverses. `ClassFactoryWithExecute.cs`'s `ScoreLocally` TRIMMING note names `RunBareCommand`, the gate's present/absent pair, and the invocation, and adds that Design.Tests run untrimmed and cannot observe it. Both accurate to `003-gate.txt` / `003-harness.txt`.

**No untested plan-introduced path.** `73cb65c` touches only the harness, the gate script, two Design comment blocks (prose only — `ScoreLocally` and `_ScoreText` came from EXRM-002), and docs. No `src/Generator` or `src/RemoteFactory` change. `IClientTallyPort`, `_ComputeTally`, `RunBareCommand` all have live callers in `Program.cs` and all ran in the archived trimmed artifact.

**Punchlist row genuinely closed.** Header now says the guard sits on a non-async `Local{X}` wrapper with `async` on `Local{X}Core`. Verified against emission: `LocalRunExecCommand` non-async, guard ahead of the `FactoryEntryCall.RunAsync` handoff; `LocalRunExecCommandCore` `private async`; bare `LocalRunBareCommand` same shape, no guard.

**Flat counts correct, no unit coverage owed.** The untrimmed half is already pinned by EXRM-001/002 (`LocalExecuteTests.cs`, `LocalClassExecuteTests.cs`); what 003 adds is observable only in a trimmed artifact, and `dotnet test` does not build this project.

## Tech-debt

- `ClassExecuteLegTarget.cs:93` and `TrimTestCommands.cs:74` name the marker literals in comments — harmless (`present()` greps one DLL; comments never reach it), but this is the file whose history is a check that could not go red.

## Theoretical

- Step 4 said "rewrite" the closing summary; implementation added the "Present:" paragraph above the retained "No shape is asserted PRESENT as a known leak" line. Both true, non-contradictory, and adding-beside is the gate's own discipline. Wording divergence only.

## Read report

Beyond brief: `TrimExecTargetFactory.g.cs:100-140` (corrected header vs emission); `git show --stat 73cb65c` (separating 003's edits from 001/002 on shared files, confirming the no-generator constraint); `todo.md` Punchlist (no open row in path). Named but unused: the three `*-publish.log` files (bullets turn on gate/harness output, verified directly); `003-plan-review.md` — confirmed B1/B2/B3/A2 amended and A1 dismissed across the plan header, constraints, Step 4, bullet 4, Punchlist, and todo Dismissed, so the review text was not load-bearing.

## Orchestrator disposition (2026-09-08)

- **Tech-debt (marker names in comments): dismissed.** `present()` greps only the DLL passed as `$1`; comments produce no metadata and no user-string entry, so the exposure is nil, and the reviewer confirmed the compiled-assembly occurrences are exactly the two bodies. The comments are what tell the next editor which literals are load-bearing and why they must not be repeated in `Program.cs` — removing them would delete the warning that prevents the defect this plan already hit once.
- **Theoretical (Step 4's "rewrite"): noted, no change.** Adding beside rather than replacing is the gate's own stated discipline, and both sentences are true: nothing is asserted present *as a leak*, and the pair is asserted present *by design*.
