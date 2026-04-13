# Fix Auth Method Triplication in Generated Save Methods

**Date:** 2026-04-06
**Related Todo:** [Fix Auth Method Triplication](../todos/fix-auth-method-triplication.md)
**Status:** Awaiting Code Review
**Last Updated:** 2026-04-06

---

## Overview

Generated `LocalCanSave` and `LocalSave` auth checks call each auth method 3 times instead of once. The root cause is that `AuthMethodCall` is a `sealed record` with an `IReadOnlyList<ParameterModel>` property — record-generated equality uses reference equality for collections, so `Distinct()` in `BuildSaveMethodFromGroup` fails to deduplicate when Insert, Update, and Delete each create separate parameter list instances.

The fix is to override `Equals`/`GetHashCode` on `AuthMethodCall` (and `AspAuthorizeCall`) to use sequence-based comparison for their collection properties.

---

## Skills

- `skills/RemoteFactory/SKILL.md` — RemoteFactory domain knowledge (authorization patterns, factory generation)

---

## Business Rules (Testable Assertions)

1. WHEN Insert, Update, and Delete methods share identical auth method calls (same class, method, parameters), THEN the merged Save method's authorization should contain each auth method call exactly once — Source: NEW (bug fix)
2. WHEN `AuthMethodCall` instances have the same ClassName, MethodName, IsTask, IsRemote, IsInternal, ConcreteClassName, and structurally equivalent Parameters lists, THEN they should be considered equal — Source: NEW (correctness fix)
3. WHEN `AspAuthorizeCall` instances have the same ConstructorArgs and NamedArgs (by sequence), THEN they should be considered equal — Source: NEW (correctness fix, same pattern)
4. WHEN auth methods differ in any property (different ClassName, MethodName, or Parameters), THEN they should NOT be deduplicated — Source: existing behavior preserved

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | AuthMethodCall equality — identical instances | Two AuthMethodCall with same ClassName, MethodName, IsTask, IsRemote, IsInternal, ConcreteClassName, and same Parameters (by value) | Rule 2 | `Equals` returns true, same `GetHashCode` |
| 2 | AuthMethodCall inequality — different method name | Two AuthMethodCall differing only in MethodName | Rule 4 | `Equals` returns false |
| 3 | AuthMethodCall inequality — different parameters | Two AuthMethodCall with same scalar props but different parameter lists | Rule 4 | `Equals` returns false |
| 4 | AuthMethodCall equality — separate list instances with same content | Two AuthMethodCall where Parameters are different `IReadOnlyList` instances containing equivalent `ParameterModel` values | Rule 2 | `Equals` returns true (the actual bug scenario) |
| 5 | Distinct deduplication — 3 identical auth methods merge to 1 | List of 3 AuthMethodCall instances (from Insert/Update/Delete) with same values but separate parameter list instances, call `.Distinct()` | Rule 1 | Result contains exactly 1 AuthMethodCall |
| 6 | AspAuthorizeCall equality — identical instances | Two AspAuthorizeCall with same ConstructorArgs and NamedArgs by value | Rule 3 | `Equals` returns true |
| 7 | AspAuthorizeCall inequality — different args | Two AspAuthorizeCall with different ConstructorArgs | Rule 3 | `Equals` returns false |
| 8 | Generated code — auth methods not tripled | Build Design project, inspect generated ParamAuthOrderFactory | Rule 1 | `LocalCanSave` calls each auth method exactly once |

---

## Approach

Override `Equals(T?)` and `GetHashCode()` on the `AuthMethodCall` and `AspAuthorizeCall` sealed records to use sequence-based comparison for their `IReadOnlyList<T>` properties. This is the minimal fix that addresses the root cause without changing the type's public API.

The generator project already has `HashCode` available (used in `EquatableArray<T>`), and `ParameterModel` is a sealed record with only scalar properties, so its compiler-generated equality is already correct.

---

## Domain Model Behavioral Design

N/A — this is a generator-internal bug fix. No domain model changes.

---

## Design

### Files to Modify

1. **`src/Generator/Model/Supporting/AuthorizationModel.cs`** — Add `Equals`/`GetHashCode` overrides to `AuthMethodCall` and `AspAuthorizeCall`

### Files to Add

2. **`src/Tests/RemoteFactory.UnitTests/Model/AuthMethodCallEqualityTests.cs`** — Unit tests for the equality fix

### How the Fix Works

For `AuthMethodCall`:
```csharp
public bool Equals(AuthMethodCall? other)
{
    if (other is null) return false;
    if (ReferenceEquals(this, other)) return true;
    return ClassName == other.ClassName
        && MethodName == other.MethodName
        && IsTask == other.IsTask
        && IsRemote == other.IsRemote
        && IsInternal == other.IsInternal
        && ConcreteClassName == other.ConcreteClassName
        && Parameters.SequenceEqual(other.Parameters);
}
```

`GetHashCode` uses `System.HashCode` (already available in the project) to combine all scalar properties plus each `ParameterModel` in the list.

Same pattern for `AspAuthorizeCall` with its `ConstructorArgs` and `NamedArgs` lists.

---

## Implementation Steps

1. Add `Equals(AuthMethodCall?)` and `GetHashCode()` overrides to `AuthMethodCall` in `AuthorizationModel.cs`
2. Add `Equals(AspAuthorizeCall?)` and `GetHashCode()` overrides to `AspAuthorizeCall` in `AuthorizationModel.cs`
3. Add `using System.Linq;` to `AuthorizationModel.cs` if not already present (needed for `SequenceEqual`)
4. Write unit tests in `AuthMethodCallEqualityTests.cs` covering scenarios 1-7
5. Build the solution and run all tests
6. Verify generated output — inspect the Design project's generated `ParamAuthOrderFactory` to confirm auth methods appear once (scenario 8)

---

## Acceptance Criteria

- [ ] `AuthMethodCall.Equals` uses value-based comparison for `Parameters`
- [ ] `AspAuthorizeCall.Equals` uses value-based comparison for `ConstructorArgs` and `NamedArgs`
- [ ] Unit tests cover equality/inequality for both types
- [ ] Generated `LocalCanSave` in ParamAuthOrderFactory calls each auth method exactly once (not 3 times)
- [ ] All existing tests pass (546+ tests)
- [ ] No changes to generated code behavior beyond removing duplicate auth calls

---

## Dependencies

None — self-contained generator fix.

---

## Risks / Considerations

1. **Incremental generation cache**: Changing equality on `AuthMethodCall` affects Roslyn's incremental generator cache key comparison. Since we're fixing equality to be more correct (fewer false negatives), this means the cache will now correctly identify identical models as equal, which is a performance improvement (fewer unnecessary regenerations). No risk of incorrect caching.
2. **netstandard2.0 constraints**: The generator targets netstandard2.0. `SequenceEqual` is available via `System.Linq` (already used). `HashCode` is available (already used in `EquatableArray`). No new dependencies needed.
