# Developer -- Fix Auth Method Triplication

Last updated: 2026-04-06
Current step: Code Review (Step 5)

## Key Context

- Bug: `AuthMethodCall` is a sealed record with `IReadOnlyList<ParameterModel> Parameters`. Record-generated equality uses reference equality for collections, so `Distinct()` in `BuildSaveMethodFromGroup` (line 651) failed to deduplicate when Insert, Update, and Delete each created separate parameter list instances via `BuildParameters()` at line 1021.
- Fix: Override `Equals(AuthMethodCall?)` and `GetHashCode()` on `AuthMethodCall` and `AspAuthorizeCall` to use `SequenceEqual` for collection properties.
- `ParameterModel` is a sealed record with only scalar properties (string, bool), so its compiler-generated equality is already correct and supports `SequenceEqual`.
- `HashCode` polyfill exists at `src/Generator/HashCode.cs` with `Add<T>()` method. Works correctly for nullable types (maps null to hash 0).

## Developer Review

**Status:** Approved
**Date:** 2026-04-06

### Summary

The implementation fixes the root cause of auth method triplication by adding proper value-based equality to `AuthMethodCall` and `AspAuthorizeCall` sealed records. The fix is minimal, correct, and well-targeted.

### Code Review Trace

| # | Business Rule | Implementation Path | Expected Result | Verified? |
|---|--------------|---------------------|-----------------|-----------|
| 1 | WHEN Insert, Update, Delete share identical auth method calls, THEN merged Save should contain each once | `FactoryModelBuilder.BuildSaveMethodFromGroup` lines 648-652: `SelectMany` + `Distinct()` now uses the custom `Equals`/`GetHashCode` on `AuthMethodCall` which does `SequenceEqual` on Parameters. Behavioral test `CanSave_CallsAuthMethodExactlyOnce` verifies count=1. Generated output `ParamAuthOrderFactory.g.cs` confirmed to call `CanWriteRole()` once. | Auth method called exactly once | Yes |
| 2 | WHEN AuthMethodCall instances have same scalar props and structurally equivalent Parameters, THEN equal | `AuthorizationModel.cs` lines 69-80: `Equals` compares all 6 scalar props with `==` and Parameters with `SequenceEqual`. `ParameterModel` is sealed record with all scalars, so its compiler-generated equality is structural. | Equals returns true | Yes |
| 3 | WHEN AspAuthorizeCall instances have same ConstructorArgs and NamedArgs by sequence, THEN equal | `AuthorizationModel.cs` lines 113-119: `Equals` uses `SequenceEqual` on both lists. String equality is value-based. | Equals returns true | Yes |
| 4 | WHEN auth methods differ in any property, THEN NOT deduplicated | `AuthorizationModel.cs` lines 73-79: Each scalar property is compared; any difference returns false. `SequenceEqual` returns false for different parameter lists. | Equals returns false | Yes (by code inspection; no direct unit test for inequality) |

### Test Scenario Coverage

| # | Plan Scenario | Covered? | How |
|---|--------------|----------|-----|
| 1 | AuthMethodCall equality -- identical instances | Indirectly | Behavioral test implies equality works (Distinct reduces 3 to 1) |
| 2 | AuthMethodCall inequality -- different method name | No | No unit test |
| 3 | AuthMethodCall inequality -- different parameters | No | No unit test |
| 4 | AuthMethodCall equality -- separate list instances with same content | Yes | This is the exact bug scenario; behavioral test covers it |
| 5 | Distinct deduplication -- 3 identical merge to 1 | Yes | `CanSave_CallsAuthMethodExactlyOnce` asserts count=1 |
| 6 | AspAuthorizeCall equality -- identical instances | No | No unit test |
| 7 | AspAuthorizeCall inequality -- different args | No | No unit test |
| 8 | Generated code -- auth methods not tripled | Yes | Generated output verified; behavioral test confirms |

**Missing:** The plan specified creating `src/Tests/RemoteFactory.UnitTests/Model/AuthMethodCallEqualityTests.cs` with unit tests for scenarios 1-7. This file was not created. The behavioral test in `BugScenarios/AuthMethodTriplicationTests.cs` covers scenarios 4, 5, and 8. Scenarios 2, 3, 6, and 7 (inequality tests and AspAuthorizeCall equality tests) have no direct test coverage.

### Implementation Correctness Analysis

**AuthMethodCall.Equals (lines 69-80):** Correct.
- Null check handles null input
- ReferenceEquals short-circuits self-comparison
- All 6 scalar properties compared (ClassName, MethodName, IsTask, IsRemote, IsInternal, ConcreteClassName)
- ConcreteClassName is `string?` -- `==` handles null correctly
- Parameters compared with SequenceEqual which delegates to ParameterModel's correct compiler-generated equality

**AuthMethodCall.GetHashCode (lines 82-94):** Correct.
- All 6 scalar properties added to hash (matches Equals)
- ConcreteClassName null safely handled by `HashCode.Add<T>` (maps null to 0)
- Parameters iterated in order, each added to hash (order-sensitive, matching SequenceEqual)

**AspAuthorizeCall.Equals (lines 113-119):** Correct.
- Null check and ReferenceEquals present
- Both lists compared with SequenceEqual

**AspAuthorizeCall.GetHashCode (lines 121-129):** Correct with minor observation.
- Both lists iterated and added. No separator between ConstructorArgs and NamedArgs means theoretical hash collisions between `ConstructorArgs=["a"]` + `NamedArgs=["b"]` vs `ConstructorArgs=[]` + `NamedArgs=["a","b"]`, but Equals is still correct so this is only a performance concern (hash collision, not correctness bug). In practice, negligible.

**Record sealed semantics:** For sealed records, the compiler-generated `Equals(object?)` delegates to the typed `Equals(T?)`. Since we override the typed version, all equality paths (==, Equals, Distinct, etc.) use the custom implementation. `GetHashCode` is also overridden, so hash-based collections work correctly.

**Null safety:** `Parameters`, `ConstructorArgs`, and `NamedArgs` are never null due to null-coalescing in constructors (`?? Array.Empty<T>()`), so `SequenceEqual` cannot throw NullReferenceException.

**netstandard2.0 compatibility:** `SequenceEqual` available via `System.Linq` (already imported). `HashCode` polyfill already in the project. No new dependencies.

### Concerns

1. **Missing unit tests (Moderate):** The plan specified 7 unit test scenarios for equality/inequality. Only a behavioral test was written. The behavioral test is good and proves the bug is fixed, but direct equality unit tests would guard against future regressions if someone modifies the Equals/GetHashCode implementations. The inequality scenarios (2, 3, 7) and AspAuthorizeCall equality scenario (6) have no test coverage.

   **Assessment:** This is a moderate concern, not blocking. The behavioral test proves the fix works for the actual bug scenario. The missing unit tests are belt-and-suspenders -- nice to have for a generator-internal type that rarely changes. I flag this but do not block approval on it.

### Ready to Proceed?

[x] Yes -- Approved with one moderate observation about missing unit tests (scenarios 2, 3, 6, 7). The implementation is correct, the core bug is fixed and tested, and the equality/GetHashCode implementations are sound. The orchestrator may choose to add the missing unit tests or proceed as-is.
