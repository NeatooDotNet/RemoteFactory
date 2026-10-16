## Test Review — RFSK-001 — 2026-10-16 — Round 1

**Verdict: DECISION NEEDED — does the green unit-project re-run (`001-unit-rerun.log`) clear the net9.0 `CanMethod_Public_ReturnsAuthorized_WhenAllowed` failure in `001-test.log`, or does the plan need a full green run?**
**Logs:** build PASSED — `reviews/001-build.log` (0 errors, 2 warnings), `reviews/001-design-build.log` (0 errors, 0 warnings); tests — `reviews/001-test.log`: UnitTests net9.0 870 passed / **1 failed**, UnitTests net10.0 871 passed, IntegrationTests 664 passed / 5 skipped on both TFMs; `reviews/001-design-test.log`: Design.Tests 177 passed / 1 skipped on both TFMs; `reviews/001-unit-rerun.log`: UnitTests 871 passed on both TFMs.

### Veto-tier

- **Failing test in the provided log:** `RemoteFactory.UnitTests.FactoryGenerator.Can.CanMethodCodePathTests+CanPublicMethodTests.CanMethod_Public_ReturnsAuthorized_WhenAllowed` (net9.0), `Assert.True() Failure` at `CanMethodCodePathTests.cs:82`, in `001-test.log`. The unit-project re-run is green. This review does not decide whether the failure was there before the change or is the #100 flake. That call belongs to the user, so it is the verdict's question. The plan touches no file under `src/Tests` or `src/RemoteFactory`.

No other veto. All five behavioural Required bullets are pinned at `[integration]` on `DesignClientServerContainers`, and the assertions can fail:
- Checkout: `RemoteCallCount == 1` on load; `Assert.Null` for 999_999; call count unchanged across `AddProduct`/`Submit`, then `+1` on `SaveAsync`, `NotSame(before, checkout.Order)`, `IsNew == false`, and the server row persisted (`ReadModelAndContextTests.cs:81-163`).
- Summary: field values, line records and total, plus `Assert.Null` (`:33-66`).
- `ClassExecuteDemo`: the new `Execute_ClassFactory_WithRemote_TriesTheWire` requires an `InvalidOperationException` whose message contains `tried to cross the wire`. Remove `[Remote]` from `RunCommand` and the call runs locally. Either it returns, so `ThrowsAsync` fails, or it throws a DI-resolution `InvalidOperationException` without that text, so `Assert.Contains` fails. The same call through the standard stand-in succeeds (`ClassFactoryExecuteTests.Execute_OnClassFactory_WorksThroughClientServer`), which shows the throw comes from the NoWire stand-in. `ScoreLocally` is pinned on the client under NoWire; `ArchiveOnServer` is pinned on the server container.
- Order line: root fetch costs one call and returns the right values; unknown id returns null; a cascade save through the order persists (`OrderLineDualityTests.cs`).
- `OpenDraft`: existing draft in one call, not new, with its lines; new unsaved order in two cases, and the store is untouched (`CreateOrFetchTests.cs`).

Bullet 3, "the `internal` one runs on the server only": the integration test pins "runs on the server". The "only" part (the client is refused) cannot be observed in-process, because `IsServerRuntime` is a process-wide feature switch and the Design harness runs with the default `true`. It is pinned at `[unit]` by `InternalVisibilityTests.ClassExecute_InternalStaticWithoutRemote_IsGuardedAndNotPromoted` (`InternalVisibilityTests.cs:300-362`), which checks the guard in the `Local` wrapper and that the member is not promoted on the interface. The Test Evidence row declares this split. It is the split EXRM-004's test review accepted for the same method (`completed/EXRM-execute-obeys-remote/reviews/004-test-review.md:41`). Not a tier mismatch.

Sacred tests: none weakened. Under `src/Design/Design.Tests` and `src/Tests` the only change is the one added method in `LocalExecuteTests.cs`. No existing assertion was touched.

### Callouts (≤ 5 Optional)

None. The plan adds no executable path: the source diff is `#region`/`#endregion` markers, plus one comment block in `OrderLine.cs` moved from between `orderline-child` and `orderline-duality` to above both. That comment sits outside every pre-existing region, so neither region's rendered content changes. The markdown diff is `snippet source` attribution lines only.

### Tech-debt (untiered, ≤ 5, one line each)

- None.

### Could not verify

- `001-design-test.log` is not verbose, so the new test does not appear in it by name. The total of 178 equals EXSET's final 176, plus 1 from `b6824a1`, plus the new test, and `Design.Tests` was compiled in `001-design-build.log` (11:10:15). That is consistent with the new test having run, but it is not direct proof.

### Theoretical (not triaged)

- Bullet 3's Test Evidence row cites `InternalVisibilityTests` without a method name; the method that pins the guard is `ClassExecute_InternalStaticWithoutRemote_IsGuardedAndNotPromoted`.

### Read report

- Read beyond the brief: `InternalVisibilityTests.cs:288-364` (to confirm the unit-tier half of bullet 3); `completed/EXRM-execute-obeys-remote/reviews/004-test-review.md` (earlier acceptance of the same tier split); issue #100 title and body; EXSET design-test logs and two later commits (test-count baseline); todo header standing orders (lines 1-12).
- Named but unused: CLAUDE.md "Skill Code Samples" beyond the marker-pair rule (no markdown markers were written); the todo's Punchlist, Dismissed and Parked sections (all empty).

---

## Test Review — RFSK-001 — 2026-10-16 — Round 2

**Verdict: APPROVED**
**Logs:** build PASSED (unchanged — `reviews/001-build.log`, `reviews/001-design-build.log`); tests — `reviews/001-test.log` (fresh full run, 11:14:48): UnitTests 871 passed / 0 failed on net9.0 and net10.0, IntegrationTests 664 passed / 5 skipped / 0 failed on both; `reviews/001-design-test.log` unchanged, Design.Tests 177 passed / 1 skipped on both. No `[FAIL]` line in `001-test.log`.

### Veto-tier

None. The round-1 veto (red log) is closed: the current `001-test.log` is a full main-solution run with zero failures on both TFMs. The round-1 run is kept as `001-test-run1.log`, still showing the net9.0 `CanMethod_Public_ReturnsAuthorized_WhenAllowed` failure, and it no longer stands as the plan's evidence. No file under `src/` has changed since `b691c0a` (the only working-tree change is the plan file), so the green run and the Design run are on the code under review.

### Callouts (≤ 5 Optional)

None.

### Tech-debt (untiered, ≤ 5, one line each)

- None.

### Could not verify

- Same as round 1: the Design test log does not list tests by name; the count (178) is consistent with the new test having run.

### Theoretical (not triaged)

- None. The round-1 item is closed: the bullet-3 row (plan line 102) now names `InternalVisibilityTests.ClassExecute_InternalStaticWithoutRemote_IsGuardedAndNotPromoted`.

### Read report

- Read beyond the brief: `git status` and `git diff b691c0a -- src/` (to confirm the fresh run is on unchanged code).
- Named but unused: `001-unit-rerun.log` (superseded by the full run).
