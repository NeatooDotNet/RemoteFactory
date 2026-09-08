# EXRM-002 — Test Review

**Date:** 2026-09-08
**Reviewer:** test-reviewer (budget: tight)
**Object:** `plans/002-local-execute-coverage.md` Test Evidence map, against the tests at `8f02dfc`
**Verdict (Round 1):** CLEAN — all 8 Acceptance bullets pinned at their declared tier; no vacuous pin found

## Must-cover

None.

- **Bullet 1** (`BareExecute_*_ClientScope_*`, 5 tests): read from `_clientScope`, which holds the live `MakeSerializedServerStandinDelegateRequest` that increments the counter — `Equal(0, ClientRemoteCalls)` is a real discrimination, not a structural zero. Targets are the new `_Local` combinations; `CombinationGenerator` emits `[Remote]` only in Remote mode, so the bare ones are genuinely bare.
- **Bullet 2**: positive, negative and server legs all assert behavior. `Assert.Contains(nameof(IServerOnlyService), ex.Message)` is reached (it follows `ThrowsAsync`) and does discriminate: `RunLocalServerOnly` contains no throw of its own, so the only `InvalidOperationException` naming that type is the DI resolution failure.
- **Bullet 3**: `Equal(1, ClientRemoteCalls)` plus the server-only-service value in the result string — two independent proofs of the crossing.
- **Bullet 7**: Design's `NoWireDelegateRequest` is registered by `configureClient`, which runs after the base `IMakeRemoteDelegateRequest` at `DesignClientServerContainers.cs:259-263`, so it wins. `Execute_WithRemote_StillTriesTheWire` does its control job — it asserts the stand-in's own message text, so the two local tests cannot pass against an unreachable stand-in. Round-trip controls unchanged (no diff).
- **Bullet 8**: counts 773→777, 613→631, 99→102 match exactly the new test bodies.

## Should-cover

- **Bullets 4 / 5 pinned.** `RecordingAspAuthorize` is a singleton the server collection owns, and the client's `ServerServiceProvider` is linked to that scope, so the instance read back is the one consulted; `Assert.Single(asp.Policies)` plus `Equal("ExecutePolicy")` cannot pass with zero crossings (an empty list fails `Single`).
- **Bullet 6 pinned, one weak point (addressed).** `ClassExecuteWithAuth_FactoryMethod_IsNullable`'s comment claimed a nullable-local assignment would not compile against `Task<T>` — it would; assigning non-nullable to a nullable local is legal, so the test was redundant with the two beside it. The bullet still holds: `TreatWarningsAsErrors` with CS8603 in no `NoWarn` makes the green build the real compile assertion, and denial→null is pinned on both shapes. **Orchestrator response:** comment corrected and the test renamed to what it actually does (`ClassExecuteWithAuth_BothOutcomes_ThroughOneFactory`); the same false claim removed from the Test Evidence note.
- **No static-flag leak.** `LocalExecAuth` is referenced only by `LocalClassExecuteTests`, `ClassExecAuth` only by `ClassExecuteAuthTests` (which also resets in `Dispose`); xunit serializes within a class, and no other class touches either flag.
- **Tech-debt (not findings):** Design `LocalExecuteTests` disposes scopes without `try/finally` (leak on assert failure); `constraints` in `CombinationDimensions.json` is still parsed and consumed nowhere.

## Sacred-test integrity

`ExecuteBehaviorTests` — all ten pre-existing tests keep their names, setup and every assertion; the diff is additive (counter assertions, comments, the new region). The Logical-scope zeros are honestly annotated as documentation rather than discrimination.

## Logs

Build main PASSED (0 errors, 4 pre-existing warnings in TrimmingTests/OrderEntry); build design PASSED. Tests: 777/777, 626 passed + 5 skipped of 631, 102/102, 0 failed, per TFM.

## Read report

Beyond the brief: `src/Directory.Build.props` and the test `.csproj` `NoWarn` sets (bullet 6's compile clause turns on CS8603 being fatal); the generated `ClassExecWithAuthFactory.g.cs` signature (confirmed `Task<ClassExecWithAuth?>`); the `FactoryModelBuilder.cs` and `CombinationGenerator.cs` diffs; `RemoteCallCounter.cs`; `DesignClientServerContainers.cs:236-270`. Named but unused: `reviews/002-plan-review.md` (its callouts are dispositioned in the plan's Amendments, read instead).
