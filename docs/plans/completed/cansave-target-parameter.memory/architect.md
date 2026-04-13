# Architect -- CanSave Target Parameter

Last updated: 2026-04-06
Current step: Post-implementation verification (Step 6A) -- VERIFIED

## Key Context

### Files Examined

- `src/Generator/Builder/FactoryModelBuilder.cs` -- AddCanMethods (line 793), AssignUniqueNames (line 893)
- `src/Generator/Renderer/ClassFactoryRenderer.cs` -- RenderCanSaveExplicitInterfaceMethod (line 1225)
- `src/RemoteFactory/IFactorySave.cs` -- interface with both CanSave overloads
- `src/Design/Design.Domain/Aggregates/ParamAuthOrder.cs` -- entity with updated comments
- `src/Design/Design.Domain/Aggregates/ParamAuthOrderAuth.cs` -- auth class with CanWriteRole() + CanWrite(target)
- `src/Design/Design.Tests/FactoryTests/ParamAuthorizationTests.cs` -- 8 new tests (scenarios 1-7, 9)
- `src/Tests/RemoteFactory.IntegrationTests/Authorization/AuthParamTests.cs` -- 4 CanSave tests + suppression test
- `src/Tests/RemoteFactory.IntegrationTests/TestTargets/Authorization/AuthParamTargets.cs` -- updated comments
- Generated: `Design.Domain.Aggregates.ParamAuthOrderFactory.g.cs` -- two CanSave overloads
- Generated: `Design.Domain.Aggregates.AuthorizedOrderFactory.g.cs` -- regression: target overload returns Authorized(true)
- Generated: `RemoteFactory.IntegrationTests.TestTargets.Authorization.AuthTargetParamObjFactory.g.cs` -- ONLY-target case

## Architect Verification (Post-Implementation)

### Build Result

**Build succeeded.** 0 errors, 2 warnings (unrelated NativeFileReference warnings from Blazor WASM projects).

### Test Result

**All tests pass.** 546 passed, 0 failed, 3 skipped (across net9.0 and net10.0).

### Test Scenario Cross-Check

| Scenario | Rule(s) | Test Method | File | Pass |
|----------|---------|-------------|------|------|
| 1: CanSave(target) true | 1, 3 | ParamAuth_CanSaveTarget_ReturnsTrue_WhenAllAuthPasses | Design.Tests/ParamAuthorizationTests.cs:309 | YES |
| 2: CanSave(target) false (target auth fails) | 1, 3 | ParamAuth_CanSaveTarget_ReturnsFalse_WhenTargetAuthFails | Design.Tests/ParamAuthorizationTests.cs:334 | YES |
| 3: CanSave(target) false (non-target auth fails) | 3 | ParamAuth_CanSaveTarget_ReturnsFalse_WhenNonTargetAuthFails | Design.Tests/ParamAuthorizationTests.cs:359 | YES |
| 4: CanSave() parameterless true | 2, 4 | ParamAuth_CanSaveParameterless_ReturnsTrue_WhenNonTargetPasses | Design.Tests/ParamAuthorizationTests.cs:385 | YES |
| 5: CanSave() parameterless false | 2, 4 | ParamAuth_CanSaveParameterless_ReturnsFalse_WhenNonTargetFails | Design.Tests/ParamAuthorizationTests.cs:405 | YES |
| 6: IFactorySave<T>.CanSave(target) delegates | 6 | ParamAuth_CanSaveTarget_ViaFactoryInterface_DelegatesCorrectly | Design.Tests/ParamAuthorizationTests.cs:427 | YES |
| 7: IFactorySave<T>.CanSave() parameterless | 7 | ParamAuth_CanSaveParameterless_ViaFactoryInterface_RunsNonTargetAuth | Design.Tests/ParamAuthorizationTests.cs:451 | YES |
| 8: AuthorizedOrder regression | 8 | All existing AuthorizationTests pass; generated factory at line 457-460 has target overload returning Authorized(true) | AuthorizedOrderFactory.g.cs:457 | YES |
| 9: Client-server round-trip | 1, 3 | ParamAuth_CanSaveTarget_ThroughClientServer_WhenStatusLocked | Design.Tests/ParamAuthorizationTests.cs:523 | YES |

### Additional Test Coverage (Integration Tests)

The integration tests provide additional coverage:
- `CanSave_Generated_OnInterface` -- verifies two CanSave overloads exist via reflection (line 360)
- `CanSave_Parameterless_ReturnsTrue` -- tests Rule 5 edge case (only-target auth, parameterless returns true) (line 378)
- `CanSave_WithTarget_ReturnsTrue_WhenStatusActive` (line 387)
- `CanSave_WithTarget_ReturnsFalse_WhenStatusLocked` (line 396)

### Design Match Verification

1. **IFactorySave<T> interface** -- Confirmed both overloads present with correct signatures and XML docs
2. **Generator AddCanMethods** -- Confirmed two-overload generation for Save methods with target auth, suppression continues for Insert/Update/Delete
3. **AssignUniqueNames** -- Confirmed CanMethodModel overloading support via signature-based deduplication (lines 900-935)
4. **RenderCanSaveExplicitInterfaceMethod** -- Confirmed renders both overloads for all three cases: (a) both overloads have auth, (b) only parameterless has auth, (c) neither has auth
5. **Generated output** -- ParamAuthOrderFactory has two CanSave overloads on interface (line 23-24), concrete class (lines 508-536), and explicit interface (lines 456-464)
6. **AuthorizedOrder regression** -- Target overload correctly returns Authorized(true) since no target-param auth exists

### Known Pre-Existing Issue

The developer code review correctly identified a pre-existing triplication bug in LocalCanSave methods. Both `ParamAuthOrderFactory.g.cs` (lines 517-533) and `AuthTargetParamObjFactory.g.cs` (lines 518-536) call auth methods 3 times instead of 1. This is caused by `BuildSaveMethodFromGroup` merging auth via `SelectMany().Distinct()` with reference equality failing on `AuthMethodCall` records containing `IReadOnlyList<ParameterModel>`. This is NOT introduced by this PR -- it exists in the AuthorizedOrder factory too. Impact is none (auth methods are idempotent, all tests pass).

### Verdict: VERIFIED

All builds pass. All 546 tests pass (0 failures). All 9 test scenarios from the plan have corresponding passing tests. The implementation matches the plan's design. The generated code correctly produces both CanSave overloads. Existing behavior (AuthorizedOrder, integration tests) is preserved. The only noted issue (auth method triplication) is pre-existing and not introduced by this change.

## Mistakes to Avoid

- Do not assume C# method overloading works in the generated code without accounting for `AssignUniqueNames` behavior -- this was handled correctly by the implementer with signature-based deduplication
- Do not forget the no-auth case for the new `IFactorySave<T>.CanSave(T, CancellationToken)` overload -- it must render even when there's no auth configured
- Do not accidentally un-suppress CanInsert/CanUpdate/CanDelete -- the suppression must remain for non-Save write operations

## User Corrections

(None)
