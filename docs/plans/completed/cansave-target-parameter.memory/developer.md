# Developer -- CanSave Target Parameter

Last updated: 2026-04-06
Current step: Developer Code Review (Step 5)

## Key Context

Reviewed all 8 changed files, 2 generated factory files (ParamAuthOrderFactory, AuthorizedOrderFactory), and the git diff. All tests pass (532 unit, 543 integration, 68 Design per framework).

## Developer Review

**Status:** Concerns
**Date:** 2026-04-06

### Summary

The implementation adds `CanSave(T target, CancellationToken)` to `IFactorySave<T>`, modifies the generator to produce two CanSave overloads when target-param auth exists, and handles the `AssignUniqueNames` challenge with a signature-based approach. The core logic is correct and all tests pass. However, I found a significant generated code quality issue (duplicate auth method calls) that, while pre-existing, is amplified by this PR and worth flagging.

### Assertion Trace Verification

| # | Business Rule | Implementation Path | Expected Result | Verified? |
|---|--------------|---------------------|-----------------|-----------|
| 1 | WHEN auth class has Write-scoped methods with target params, THEN CanSave(T target) IS generated | `FactoryModelBuilder.AddCanMethods` lines 815-850: `if (method is SaveMethodModel)` creates targetCanSave via `BuildCanMethod` with all authMethods. Generated output: `ParamAuthOrderFactory.g.cs` line 538: `public virtual Authorized CanSave(IParamAuthOrder target, ...)`. Interface line 24: `Authorized CanSave(IParamAuthOrder target, ...)` | CanSave(target) on both interface and class | YES |
| 2 | WHEN auth class has Write-scoped methods with target params, THEN CanSave() parameterless IS ALSO generated | `FactoryModelBuilder.AddCanMethods` lines 819-835: creates parameterlessCanSave with nonTargetAuthMethods. Generated: line 508: `public virtual Authorized CanSave(CancellationToken ...)`. Interface line 23: `Authorized CanSave(CancellationToken ...)` | Both overloads on interface and class | YES |
| 3 | WHEN CanSave(target) called, THEN ALL Write-scoped auth methods invoked | Generated `LocalCanSave(IParamAuthOrder target, ...)` (line 543-584): calls `CanWriteRole()` (non-target) AND `CanWrite(target)` (target). Note: called 3x each due to pre-existing dedup bug -- see Concerns. | All Write auth called | YES (with caveat) |
| 4 | WHEN CanSave() parameterless called, THEN only non-target Write auth invoked | Generated `LocalCanSave(CancellationToken)` (line 513-536): calls only `CanWriteRole()` -- never calls `CanWrite(target)`. | Only non-target auth | YES |
| 5 | WHEN CanSave() parameterless called AND no non-target Write auth, THEN returns Authorized(true) | Integration test `AuthTargetParamObj`: `AuthWithTargetParam` has only `CanWrite(IAuthTargetParamObj target)` Write auth. `CanSave_Parameterless_ReturnsTrue` (AuthParamTests.cs line 378) confirms parameterless returns true. Renderer: `RenderCanSaveExplicitInterfaceMethod` line 1243: `if (parameterlessCanSave == null)` returns `Authorized(true)` | Authorized(true) | YES |
| 6 | WHEN IFactorySave<T>.CanSave(T target) called, THEN delegates to concrete CanSave(target) | Generated `ParamAuthOrderFactory.g.cs` line 461-464: `IFactorySave<ParamAuthOrder>.CanSave(ParamAuthOrder target, ...) => Task.FromResult(CanSave(target, ...))`. Delegates to concrete overload. | Delegates correctly | YES |
| 7 | WHEN IFactorySave<T>.CanSave() parameterless called, THEN runs only non-target auth | Generated `ParamAuthOrderFactory.g.cs` line 456-459: `IFactorySave<ParamAuthOrder>.CanSave(CancellationToken) => Task.FromResult(CanSave(cancellationToken))`. Delegates to parameterless concrete which runs only non-target auth per Rule 4. | Only non-target auth | YES |
| 8 | WHEN auth class has ONLY parameterless Write auth, THEN existing behavior unchanged | `AuthorizedOrderFactory.g.cs`: interface (line 26) has only `CanSave(CancellationToken)` -- no target overload on factory-specific interface. Concrete `CanSave` (line 570) delegates to `LocalCanSave` which calls aggregated auth methods. `IFactorySave<T>.CanSave(target)` (line 457-460) returns `Authorized(true)` (no target auth exists). Existing AuthorizationTests pass unchanged. | Unchanged behavior | YES |

### Test Scenario Mapping

| # | Plan Scenario | Test Method | File | Verified? |
|---|--------------|-------------|------|-----------|
| 1 | CanSave(target) true when all auth passes | `ParamAuth_CanSaveTarget_ReturnsTrue_WhenAllAuthPasses` | ParamAuthorizationTests.cs:309 | YES |
| 2 | CanSave(target) false when target auth fails | `ParamAuth_CanSaveTarget_ReturnsFalse_WhenTargetAuthFails` | ParamAuthorizationTests.cs:334 | YES |
| 3 | CanSave(target) false when non-target auth fails | `ParamAuth_CanSaveTarget_ReturnsFalse_WhenNonTargetAuthFails` | ParamAuthorizationTests.cs:359 | YES |
| 4 | CanSave() parameterless true when non-target passes | `ParamAuth_CanSaveParameterless_ReturnsTrue_WhenNonTargetPasses` | ParamAuthorizationTests.cs:385 | YES |
| 5 | CanSave() parameterless false when non-target fails | `ParamAuth_CanSaveParameterless_ReturnsFalse_WhenNonTargetFails` | ParamAuthorizationTests.cs:405 | YES |
| 6 | IFactorySave<T>.CanSave(target) delegates correctly | `ParamAuth_CanSaveTarget_ViaFactoryInterface_DelegatesCorrectly` | ParamAuthorizationTests.cs:427 | PARTIAL -- tests via factory-specific interface, not IFactorySave<T> directly. Verified indirectly via generated explicit interface implementation. |
| 7 | IFactorySave<T>.CanSave() runs non-target auth | `ParamAuth_CanSaveParameterless_ViaFactoryInterface_RunsNonTargetAuth` | ParamAuthorizationTests.cs:451 | PARTIAL -- same as #6, tests factory-specific interface |
| 8 | AuthorizedOrder unchanged | All existing AuthorizationTests pass (26 tests in Design.Tests for auth) | AuthorizationTests.cs (unmodified) | YES |
| 9 | CanSave(target) client-server round-trip | `ParamAuth_CanSaveTarget_ThroughClientServer_WhenStatusLocked` | ParamAuthorizationTests.cs:523 | YES |
| -- | Rule 5: parameterless CanSave with zero non-target auth | `CanSave_Parameterless_ReturnsTrue` | AuthParamTests.cs:378 | YES -- AuthTargetParamObj has only target Write auth |
| -- | CanSave generated with two overloads (structural) | `CanSave_Generated_OnInterface` | AuthParamTests.cs:360 | YES -- uses reflection to verify 2 overloads exist |

### Gaps and Questions

#### Concern: Duplicate Auth Method Calls in Generated CanSave

**Severity: Medium -- Correctness is unaffected, but code quality is degraded.**

The generated `LocalCanSave` methods call auth methods 3 times instead of once. For ParamAuthOrderFactory:
- `LocalCanSave()` parameterless calls `CanWriteRole()` three times (lines 517, 523, 529)
- `LocalCanSave(target)` calls both `CanWriteRole()` and `CanWrite(target)` three times each (lines 547-578)

**Root cause:** `BuildSaveMethodFromGroup` (line 648-652) merges auth from Insert/Update/Delete via `SelectMany` + `Distinct()`. But `AuthMethodCall` is a `record` with an `IReadOnlyList<ParameterModel>` property -- record equality for collections uses reference equality, so `Distinct()` doesn't deduplicate when each write method has its own parameter list instances.

**This is a pre-existing bug** -- the AuthorizedOrderFactory (unchanged by this PR) also has the same triplication in its `LocalCanSave`. So this PR did not introduce the bug, but the new CanSave overloads inherit the same duplicated auth calls.

**Impact on correctness:** None. Auth methods are idempotent -- calling `CanWriteRole()` 3 times gives the same result as calling it once. All tests pass. But it's wasteful and the generated code looks wrong to users inspecting it.

**Recommendation:** File a separate todo to fix `AuthMethodCall` equality for `Distinct()` to work properly in `BuildSaveMethodFromGroup`. This is out of scope for the current CanSave feature.

#### Minor: Test Scenarios 6-7 Don't Test IFactorySave<T> Directly

The plan scenarios 6-7 say "WHEN IFactorySave<T>.CanSave(...) is called" but the tests call through the factory-specific interface `IParamAuthOrderFactory`, not through `IFactorySave<T>`. The generated explicit interface implementations delegate to the same concrete methods, so the behavior is verified indirectly. This is acceptable but imprecise.

#### Observation: Reflection in Integration Tests

`CanInsert_CanUpdate_CanDelete_NotGenerated_OnInterface` (AuthParamTests.cs line 349) and `CanSave_Generated_OnInterface` (line 360) use `Type.GetMethod()` / `Type.GetMethods()` for structural verification. These tests existed before this PR (created in the auth-target-param-support work) and are in-scope for this feature. The reflection is used for compile-time structural assertions, not runtime behavior -- this is a reasonable use case for test code.

### Ready to Proceed?

[x] Concerns -- The duplicate auth method call bug should be acknowledged before verification. It is pre-existing and does not affect correctness, but should be tracked separately. The implementation itself is correct and complete.

## Mistakes to Avoid

- The generated code duplication is a pre-existing `AuthMethodCall` equality bug, not introduced by this PR
- Interface factory rendering (`AddCanMethodsForInterface`) intentionally NOT changed -- interface factories don't implement `IFactorySave<T>`
