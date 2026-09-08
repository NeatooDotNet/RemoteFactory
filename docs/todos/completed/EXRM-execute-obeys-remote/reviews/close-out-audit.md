# EXRM — Close-Out Audit

**Auditor:** `code-reviewer` in close-out mode, per `references/close-out-audit.md`
**Date:** 2026-09-08 · **Branch:** `EXRM` @ `64a8858`, every plan PR merged
**Budget:** the checklist plus the repo `CLAUDE.md` — no `code-review-calibration.md`, `review-calibration.md` or `code-review-rubric.md` exists in this repo

## Close-Out Audit — 2026-09-08

**Grade: A**

### Acceptance Criteria Trace

| AC | Word | Evidence (file:method:line) | Holds? | If not: accepted gap? |
|---|---|---|---|---|
| AC-1 | Must | Weld removed `FactoryGenerator.Types.cs:582,629`; flag derived `FactoryModelBuilder.cs:537-541`; unguarded registration in every mode `StaticFactoryRenderer.cs:141-158` + `RenderLocalDelegateRegistration(guarded:false)`; class path already correct `ClassFactoryRenderer.cs:842-844` (`isServerOnly: IsInternal \|\| IsRemote`). Behaviour: `LocalExecuteTests.BareExecute_ClientScope_RunsLocally_…:43-50`, `.BareExecute_ResolvesAndRuns_InRemoteLogicalAndServerContainers:81-90`; `LocalClassExecuteTests…:43-50,:80-89`; `Design.Tests/LocalExecuteTests.cs:67,93`. Counter is a live discriminator (asserts 1 at `LocalExecuteTests.cs:98-105`), so the zero assertions are not vacuous | Yes | — |
| AC-2 | Must | `StaticFactoryRenderer.RenderLocalDelegateRegistration(guarded:true)` — the auditor traced the new `indent` arithmetic (20/24 spaces) against the deleted hardcoded strings: **byte-identical output**. Class guard on the non-async wrapper unchanged (`ClassFactoryRenderer.cs:394-399`). Behaviour: `LocalExecuteTests.RemoteExecute_ClientScope_CrossesTheWireOnce:98-105`, `LocalClassExecuteTests…:97-104`, five `Comb_Execute_*_Remote` tests, pre-existing `ClassExecuteRoundTripTests` | Yes, with one recorded exception | Auth'd class-level `[Execute]` now emits `Task<T?>` (`FactoryModelBuilder.cs:481-487`) — recorded in the Discovery Log, plan 002 Amendment 2, and `v1.9.0.md:85-87`; not a silent drift |
| AC-3 | Must | `verify-trimmed.sh` `[P]` legend `:126-131` + pair block; `003-evidence/003-gate.txt` 63 ok incl. `BareStaticBody_MARKER`, `BareTallyAsyncBody_MARKER`, `BareClassBody_MARKER`; `003-harness.txt` all three bare members invoked on the trimmed client; falsification `knownbad-gate.txt` exit 1 with exactly 3 `is MISSING` and the other 60 green, `knownbad-harness.txt` all three routed to `NoOpHttpHandler`. Three pairs, not two | Yes (exceeds) | — |
| AC-4 | Should | Three-way rule `FactoryModelBuilder.cs:476-478`. `LocalClassExecuteTests.BareExecute_ClientSideAuth_{Allowed,Denied}…:125-150` (0 crossings, denial → null); `.BareExecute_AspAuthorize_Allowed_CrossesTheWireOnce_AndIsConsulted:244-255` (1 crossing, `ExecutePolicy` recorded); `.BareExecute_RemoteAuthMethod_ForcesTheWireOnce:198-209` | Yes | — |
| AC-5 | Must | Retired-claim sweep across `docs/ skills/ src/ CLAUDE.md`: zero surviving instances. Design `AllPatterns.cs:370-383`, `ClassFactoryWithExecute.cs:153-188`; `docs/trimming.md:22-40`, `docs/attributes-reference.md:203-207`; skill `references/trimming.md:138-144,163-167` — all three state the same rule, no disagreement. Release: `v1.9.0.md`, index `:19,:72`, nav_order 1 with the eleven 1.x pages verified at 2–12, `Directory.Build.props:18-19` both `1.9.0`, `PackageReleaseNotes` set, `005-pack.log` names both 1.9.0 nupkgs | Yes | — |
| AC-6 | Could | Kept with reason: `FactoryModelBuilder.cs:194-207` (addresses the real breadth — every static method, not `[Execute]`), `docs/attributes-reference.md:207`, `CLAUDE-DESIGN.md:368`, `RemoteAttribute` XML doc. Pinned `NF0105Tests.cs:161` (`[Execute]`) and `:204` (`[Create]`, the breadth case) | Yes | — |

AC-1 and AC-2 were ticked late, at 005's close. The auditor verified them by reading the generator diff and the tests rather than the Gate Records — genuinely met, not ticked to clear the board.

### Veto-Tier Findings

**One test failure, named, examined, and not open.** `CanMethodCodePathTests+CanLocalMethodTests.CanLocalMethod_IsPublic` fails in `reviews/final-test.log` (net9.0 only). The auditor verified the diagnosis independently rather than accepting the orchestrator's: `CanMethodTestAuth.ShouldAllow` is a `public static bool` at `CanMethodCodePathTests.cs:21` shared by nested test classes; there is no `DisableTestParallelization` or `CollectionBehavior` under `src/Tests/RemoteFactory.UnitTests/`, so xUnit runs those classes concurrently; `CanRemoteMethodTests.CanMethod_HasAccess_ReflectsAuthState:179` sets it `false` while `CanLocalMethod_IsPublic:231-233` sets `true` and asserts. `git log main..EXRM -- .../Can/` is empty; `final-test-rerun.log` is 779/779 on both TFMs from unchanged binaries. Correctly classified, filed as #100, dismissed on the todo.

**No veto-tier finding is OPEN.** The failure is pre-existing, non-deterministic, root-caused and dispositioned; it goes to Follow-on, not the grade.

*Honest qualification, in the auditor's words:* the arc added six UnitTests classes (773→779), widening parallel scheduling — **plausibly raising the surfacing rate, not causing it**.

No documented rule contradicted: the minor-bump departure from `v1.0.0.md:198-206` is recorded in the todo header, plan 005's Constraints, and the page blockquote; 2.0.0 was offered twice and declined twice.

### Callouts

1. **EXRM-005's Punchlist rows unchecked on a Done plan.** `plans/005:102-105` were `- [ ]`; `todo.md:54,58` still read `[→ EXRM-005]`. Plans 001/003/004 checked theirs. High. `Affects: AC-5 (Must)`. `Reachable by:` a reader of the closed todo cannot tell the version bump and release-process corrections shipped — they did. Bookkeeping only. **Punched — fixed.**
2. **Discovery Log over budget.** Six of eight entries exceed 60 words; worst 98. Goal 113/150, fine. High. `Affects: AC-3 (Must)`. `Reachable by:` a reader of the log. **Accepted** — user decision 2026-09-08; see the retro.
3. **Two new CA1062 warnings on arc code** — `ClientSafePorts.cs:64`, `ClassExecuteLegTarget.cs:123`; main-solution count 4→6. TrimmingTests does not escalate CA1062, so "build green" stayed honest. High/low-consequence. `Affects: AC-3 (Must)`. **Punched — fixed**: `CA1062` added to the harness's `NoWarn` with a reason, and the rebuild reports 0 warnings, 0 errors (`final-build-r2.log`).

### Theoretical (not triaged)

- `plans/005-release-1-9-0.memory/requirements-reviewer.md` — reviewer memory committed under `plans/`, not a plan, not indexed. (Twenty such `.memory` directories already exist in the repo; established pattern.)
- The gate summary keeps "No shape is asserted PRESENT as a known leak" beneath the new "Present: … by design" paragraph; coherent, but skimmable wrong.

### Build & Test

- **Build: PASSED** — `reviews/final-build.log`, three solutions "Build succeeded", 0 errors, 6 warnings (4 CA1062 TrimmingTests, 2 WASM Examples). After callout 3's fix, `final-build-r2.log`: **0 warnings, 0 errors**.
- **Tests: 3099 passed, 1 failed, 10 skipped** — `reviews/final-test.log`. Sole failure named above; `reviews/final-test-rerun.log` green at 779/779 both TFMs from unchanged binaries.

### Container

| # | Point | Result |
|---|---|---|
| 1 | Index ↔ `plans/` reconciled, numbering monotonic | 001–005 ✓ |
| 2 | Abandoned/Retired reasons | None to check |
| 3 | Status cells over budget | **0** |
| 4 | Every plan and log entry names its AC | ✓ |
| 5 | Untraced deferrals | **None** |
| 6 | Punchlist rows one line; open rows on Follow-on | ✓ (callout 1 fixed) |
| 7 | Out of Scope respected | ✓ |
| 8 | Prose over budget | Discovery Log (98 words) — accepted |
| 9 | Open PRs into the arc | **None** — #92/93/95/96/98/99 merged |
| 10 | Priority words complete; Must accepted as gap without demotion | **None** |

**Plans issued / cap: 5 / 8.** The `net8.0` Punchlist row's record judged adequate — the auditor verified the global `CLAUDE.md:199` now reads `net9.0;net10.0`.

### Read report

Beyond the brief: the full `src/Generator` + `src/RemoteFactory` diff; the renderer indent arithmetic; `CanMethodCodePathTests.cs`; the counter wiring; six cited test files; `verify-trimmed.sh`; the Design/docs/skill contract sites; `v1.0.0.md`, `index.md`, twelve `nav_order`s; both `CLAUDE.md`s; `gh pr list`. Unused: the per-plan `001-*`–`005-*` logs; the three `003-evidence` publish logs.

---

## Orchestrator record

**User acknowledgment (2026-09-08):** acknowledged, grade A, proceed to Step 8.

**Dispositions:** callouts 1 and 3 fixed before close-out; callout 2 accepted with the reason recorded in the retro. The report arrived in two parts (one truncation, then a capped resend) and is reproduced complete above.
