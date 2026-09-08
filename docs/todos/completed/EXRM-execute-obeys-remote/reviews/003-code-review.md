# Code Review — EXRM-003 — 2026-09-08 — Round 1

**Reviewer:** `code-reviewer` · **Budget:** tight · **Object:** commit `73cb65c` on `exrm-003-trimming-gate-pair`

**Verdict: CLEAN**

**Logs:** `003-build-main.log` PASSED (0 errors), `003-build-design.log` PASSED (0 errors, 0 warnings); `003-test-main.log` — UnitTests 777/777, IntegrationTests 626 passed + 5 skipped = 631, both per TFM (net9.0, net10.0), 0 failed; `003-test-design.log` — Design.Tests 102/102 per TFM, 0 failed. Counts flat against the EXRM-002 baselines exactly as the plan predicted.

## Direction & shape

The intent landed where it should. The gate diff is additive only — `check_present`, a `[P]` legend entry, a pair block, and a `Present:` paragraph in the closing summary — with no incumbent `check_absent` line edited, widened, renamed or removed. The evidence corroborates the diff claim: `baseline-gate.txt` 60 ok → `003-gate.txt` 62 ok, and `knownbad-gate.txt` shows exactly the two new checks red with all 60 incumbent checks green in the same run.

**The naming rule holds.** The full absence list from `verify-trimmed.sh` (all 41 name/literal markers plus the six `<Local…>d__` discriminators) was run against every name this commit introduces — `IClientTallyPort`, `ClientTallyEngine`, `ClientTallyCompute`, `_ComputeTally`, `ComputeTally`, `RunBareCommand`, `LocalRunBareCommand(Core)`, `<LocalRunBareCommandCore>d__`, both `Bare*_MARKER`s, `|tallied:`, `bare-static`, `bare-class`. **Zero substring collisions.** B1's distinct-stem requirement is satisfied literally, not just in spirit.

The B2 correction landed and is load-bearing: the gate asserts only the two body literals, and `ClientSafePorts.cs` records why the port name is not asserted. The marker literals are compiled into exactly one place each — `TrimTestCommands.cs` and `ClassExecuteLegTarget.cs`; the three other occurrences are comment/XML-doc text and reach no user-string heap. The Plan Amendment's account of the vacuous first draft matches what the code now does.

The invocation checks are sound and cannot pass on a dead body: `ClientTallyCompute` is the only producer of `|tallied:`, and the caller's token can only reach it through the `[Execute]` body, so requiring both in the result forecloses a retained-but-unreachable body. Both blocks are try/catch, append to `failedChecks`, never throw out of `Program`, and feed the existing non-zero exit.

Body literals are plain concatenation in both bodies, per the harness's recorded requirement. The corrected file header is accurate: `ClassFactoryRenderer.RenderLocalMethodOpening` emits the guard in the `Local{X}` wrapper, which never carries `async`, and applies `asyncKeyword` only to `Local{X}Core`.

`AllPatterns.cs`'s reversal is correct. `isServerOnly` is `method.IsInternal || method.IsRemote` (`ClassFactoryRenderer.cs:420`), so `[Remote]` decides whether a guard exists at all — attribute as cause, guard as mechanism. **The known-bad run also rules out the confound that would have undermined it:** with `IClientTallyPort` still registered unconditionally, adding `[Remote]` alone took both markers to MISSING, so the bodies survive because no guard is emitted, not because the port is rooted.

## Veto-tier

None.

## Callouts

**1. `ClassExecuteLegTarget.cs:64-65` — the B3 correction is incomplete, and the file now contradicts itself.** High confidence. `Affects: AC-3 (Must)`. The header was corrected to say the guard sits on the non-async `Local{X}` wrapper post-TRIM-009. Forty-five lines down, `RunExecCommand`'s own summary still read "emitted `async` unconditionally by `RenderClassExecuteLocalMethod`, with the feature-switch guard inside" — the same pre-TRIM-009 claim B3 was punched to remove, on the `[Remote]` half of the pair under review. `Reachable by:` a maintainer comparing the halves reads the nearer method-level doc and takes the disproven shape. Disposition: **punch** — one sentence, same fix as the header.

**2. "`[Remote]` is the only variable" overclaims.** Medium. `Affects: AC-3 (Must)`. `ClassExecuteLegTarget.cs`, `verify-trimmed.sh`, `AllPatterns.cs`, `003-evidence/README.md`. Two things besides the attribute vary. The `[Service]` type differs on both pairs — forced, documented, neutralised by the known-bad run, but it makes the remark self-inconsistent. More substantively the class pair varies in async-ness: `RunExecCommand` is `async` and awaits its port, `RunBareCommand` returns `Task.FromResult`. So no bare **async** `[Execute]` body is measured on either shape (`_DoAsyncWork` has no bare sibling), while TRIM-009 found async was what broke this leg. `Reachable by:` a reader taking the green present checks as covering async bare bodies. Disposition: **punch** a qualifier naming what is and is not controlled; an async bare leg is a Follow-on.

## Theoretical (not triaged)

- `docs/trimming.md:35` and `skills/RemoteFactory/references/static-factory.md:87` still state the old contract; already inventoried in `plans/004` — hand-off covered.
- Build warnings 4 → 6: two new CA1062 matching the pre-existing one on `RunExecCommand`; guarding only the bare half would break pair symmetry.
- Plan header and Plan Index row still read `Status: Draft` with all Acceptance boxes checked — Step 6 bookkeeping.

## Read report

Beyond the brief: `ClassFactoryRenderer.cs:370-425` (guard placement, `isServerOnly`); `verify-trimmed.sh` in full to `:380` (the substring scan needs every marker); `TrimTestCommands.cs:1-65`, `ClassExecuteLegTarget.cs:60-118` (pair symmetry); `plans/004:64-71`. Named but unused: TRIM `todo.md:119-120` — interface leg untouched. Not found: `reviews/003-test-review.md` (ran in parallel), so no closures verified; no calibration or rubric file exists. Checked Dismissed; A1 not re-raised.

## Orchestrator disposition (2026-09-08)

| # | Finding | Affects | Priority | Disposition |
|---|---|---|---|---|
| 1 | B3 correction incomplete — `RunExecCommand`'s summary kept the pre-TRIM-009 claim | AC-3 | Must | **Punched now** — `7d3d537`. Verified by grep that the three remaining "emitted async unconditionally" statements are accurate: they describe the body, not the guard's placement (`ClassFactoryWithExecute.cs:73` immediately explains the non-async wrapper) |
| 2a | Class pair varied in async-ness — the axis TRIM-009 found broke this leg | AC-3 | Must | **Fixed, not merely qualified.** `IClientTallyPort` gained `ClientTallyComputeAsync`; `RunBareCommand` is now `async` and awaits it, so both halves of the class pair await a port call. Full evidence set re-run, including the falsification against the changed target |
| 2b | `[Service]` type differs on both pairs | AC-3 | Must | **Punched** — qualifier added in all four places. It is forced (the `[Remote]` half's port is server-only by construction) and neutralised by the known-bad run, which is now stated rather than left implicit |
| 2c | No bare **async static** `[Execute]` is measured (`_DoAsyncWork` has no bare sibling) | AC-3 | Could (proposed) | Stated as a named gap in `verify-trimmed.sh`, `TrimTestCommands.cs` and the evidence README. Proposed for the todo Punchlist rather than done here — AC-3 asks for both *shapes*, which are covered; this is an additional case |

**Theoretical items:** the two doc lines are EXRM-004's, already in its inventory (and A1's dismissal covers the same family); the CA1062 pair matches the harness's existing warnings, and guarding only the bare half would break the symmetry the pair depends on; the Draft-status bookkeeping is closed at the Gate Record.

**Re-verification after the callout fixes:** publish exit 0, gate exit 0 with 62 ok, harness exit 0; known-bad re-run against the async target exit 1 with exactly 2 `is MISSING` and 60 ok; both solutions rebuilt and retested green (777 / 631 / 102 per TFM). One new CA1849 appeared when the port gained its async form — the analyzer flagging the static half's deliberately synchronous call — and is suppressed in the harness csproj with the reason recorded there; the warning set is back to CA1062 only.
