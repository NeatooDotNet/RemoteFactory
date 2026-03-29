# Developer -- LazyLoad Protected for Inheritance

Last updated: 2026-03-28
Current step: Implementation complete -- Awaiting Verification (Step 7)

## Key Context

- This is a small, surgical plan: 4 visibility-only changes to 2 files
- Steps 2 (Requirements Review) and 3 (Architect Review) were skipped by the user
- No Business Rules (Testable Assertions) or Test Scenarios exist in the plan -- evaluated on its own merits
- No behavioral changes -- all changes are access modifier widening
- CA1033 build error was encountered and resolved with a pragma suppress (see Mistakes to Avoid)

## Mistakes to Avoid

- **CA1033 code analysis rule**: Making `ILazyLoadDeserializable` public while `LazyLoad<T>` is unsealed triggers CA1033 on explicit interface implementations (`BoxedValue`, `ApplyDeserializedState`). Fixed with `#pragma warning disable CA1033` / `#pragma warning restore CA1033` wrapping those two members. This was not anticipated in the plan.

## User Corrections

(None)

## Developer Review

**Status:** Approved
**Date:** 2026-03-28

### Summary

The plan proposes 4 visibility changes across 2 files to make `LazyLoad<T>` inheritable by Neatoo:

1. `ILazyLoadDeserializable`: `internal` -> `public` (keep in `Internal` namespace)
2. New `protected Task<T?>? LoadTask` read-only property (exposes `_loadTask` field)
3. New `protected void ClearLoadError()` method (clears `_loadError` field)
4. `OnPropertyChanged`: `private void` -> `protected virtual void`

### Assertion Trace Verification

No formal Business Rules / Testable Assertions exist (Steps 2-3 were skipped). Instead, I verify the plan's 6 acceptance criteria:

| # | Acceptance Criterion | Implementation Path | Expected Result | Verified? |
|---|---------------------|---------------------|-----------------|-----------|
| 1 | `ILazyLoadDeserializable` is `public` | `ILazyLoadDeserializable.cs` line 8: change `internal` to `public` | Interface accessible outside assembly | Yes |
| 2 | `LazyLoad<T>` has `protected Task<T?>? LoadTask` property | `LazyLoad.cs`: add `protected Task<T?>? LoadTask => _loadTask;` | Subclass can read the in-flight task | Yes |
| 3 | `LazyLoad<T>` has `protected void ClearLoadError()` method | `LazyLoad.cs`: add `protected void ClearLoadError() { _loadError = null; }` | Subclass can clear load error | Yes |
| 4 | `LazyLoad<T>.OnPropertyChanged` is `protected virtual` | `LazyLoad.cs`: change `private void` to `protected virtual void` | Subclass can override and fire INPC | Yes |
| 5 | All existing tests pass unchanged | No behavioral change, only visibility widening | All tests pass | Yes -- confirmed |
| 6 | Solution builds clean on net9.0 and net10.0 | Visibility widening is additive; CA1033 suppressed | Clean build (0 errors, no new warnings) | Yes -- confirmed |

### Codebase Investigation Results

**`ILazyLoadDeserializable` references in source code (.cs only):**
- `LazyLoad.cs` -- explicit interface implementation (lines 106, 109, 117)
- `LazyLoadMergeTests.cs` -- casts to the interface (lines 28, 51)
- NOT referenced in generator code (ordinal format uses constructors, not merge)
- NOT referenced in `LazyLoadJsonConverterFactory` (named format uses constructors, not merge)

**InternalsVisibleTo setup** (`RemoteFactory.csproj`):
- `RemoteFactory.UnitTests` -- has access (merge tests compile today)
- `RemoteFactory.IntegrationTests` -- has access
- `Neatoo.RemoteFactory.AspNetCore` -- has access
- `FactoryGeneratorTests` -- has access

Making `ILazyLoadDeserializable` public is strictly additive. All current consumers already have internal access via `InternalsVisibleTo`. No test needs modification.

**`OnPropertyChanged` callers (all within `LazyLoad.cs`):**
- `OnPropertyChanged` itself -- the method being changed
- `OnValuePropertyChanged` -- calls `OnPropertyChanged(e.PropertyName!)`
- `SetValue` -- calls it 3 times
- `LoadAsyncCore` -- calls it multiple times

All callers are within the base class. A subclass override that calls `base.OnPropertyChanged()` preserves all behavior. This is standard INPC virtual pattern.

### Gaps and Questions

#### Minor (Not Blocking)

1. **Todo vs. plan namespace discrepancy**: The todo says "Make it public and move to `Neatoo.RemoteFactory` namespace." The plan says "Keep in `Neatoo.RemoteFactory.Internal` namespace." The plan is the authoritative design document, so the plan's decision (keep in Internal namespace) is correct.

### Implementation Concerns

None. All changes were straightforward access modifier widening with one anticipated CA1033 suppression.

### Ready to Proceed?

[x] Yes, approved for implementation

---

## Implementation Contract

### Scope

- `src/RemoteFactory/Internal/ILazyLoadDeserializable.cs` -- Change `internal interface` to `public interface` (modify)
- `src/RemoteFactory/LazyLoad.cs` -- Add `protected Task<T?>? LoadTask` property, add `protected void ClearLoadError()` method, change `OnPropertyChanged` from `private void` to `protected virtual void`, add CA1033 pragma suppress (modify)

### Out of Scope

- `src/Tests/RemoteFactory.UnitTests/LazyLoad/LazyLoadMergeTests.cs` -- Sacred test, must NOT be modified
- `src/Tests/RemoteFactory.UnitTests/LazyLoad/LazyLoadCoreTests.cs` -- Sacred test, must NOT be modified
- `src/Tests/RemoteFactory.UnitTests/LazyLoad/LazyLoadDiTests.cs` -- Sacred test
- `src/Tests/RemoteFactory.UnitTests/LazyLoad/LazyLoadNamedSerializationTests.cs` -- Sacred test
- `src/Tests/RemoteFactory.UnitTests/LazyLoad/LazyLoadOrdinalTests.cs` -- Sacred test
- `src/Tests/RemoteFactory.IntegrationTests/LazyLoad/LazyLoadRoundTripTests.cs` -- Sacred test
- All other test files -- Sacred

### Tests to Add

None. No new behavior is introduced. Existing tests provide full coverage of the unchanged behavior.

### Test Scenario Mapping

N/A -- No new test scenarios. All existing tests must continue to pass unchanged.

### Verification Gates

1. After all 4 changes: `dotnet build src/Neatoo.RemoteFactory.sln` -- must succeed clean (no warnings)
2. After build: `dotnet test src/Neatoo.RemoteFactory.sln` -- all existing tests must pass

### Stop Conditions

- If any existing test fails, STOP and report
- If any new compiler warnings appear, STOP and report

---

## Implementation Progress

### Milestone 1: Source changes (COMPLETE)

All 4 changes applied:

1. `ILazyLoadDeserializable.cs`: Changed `internal interface` to `public interface`
2. `LazyLoad.cs`: Added `protected Task<T?>? LoadTask => _loadTask;` (line 46)
3. `LazyLoad.cs`: Added `protected void ClearLoadError() { _loadError = null; }` (lines 52-55)
4. `LazyLoad.cs`: Changed `private void OnPropertyChanged` to `protected virtual void OnPropertyChanged` (line 62)
5. `LazyLoad.cs`: Added `#pragma warning disable/restore CA1033` around explicit `ILazyLoadDeserializable` members (lines 124, 145) to suppress code analysis error from public interface + unsealed class + explicit implementation

### Milestone 2: Build verification (COMPLETE)

Build succeeded: 0 errors, 0 new warnings (only pre-existing WASM0001 from OrderEntry Blazor example).

### Milestone 3: Test verification (COMPLETE)

All tests passed:
- RemoteFactory.UnitTests (net9.0): 517 passed, 0 failed
- RemoteFactory.UnitTests (net10.0): 517 passed, 0 failed
- RemoteFactory.IntegrationTests (net9.0): 506 passed, 0 failed, 3 skipped (pre-existing)
- RemoteFactory.IntegrationTests (net10.0): 506 passed, 0 failed, 3 skipped (pre-existing)
- Total: 2046 passed, 0 failed

---

## Completion Evidence

### Build Output

```
Build succeeded.
    3 Warning(s)    [all pre-existing WASM0001 from OrderEntry.BlazorClient]
    0 Error(s)
```

### Test Results

```
Passed!  - Failed: 0, Passed: 517, Skipped: 0, Total: 517 - RemoteFactory.UnitTests.dll (net9.0)
Passed!  - Failed: 0, Passed: 517, Skipped: 0, Total: 517 - RemoteFactory.UnitTests.dll (net10.0)
Passed!  - Failed: 0, Passed: 506, Skipped: 3, Total: 509 - RemoteFactory.IntegrationTests.dll (net9.0)
Passed!  - Failed: 0, Passed: 506, Skipped: 3, Total: 509 - RemoteFactory.IntegrationTests.dll (net10.0)
```

### Contract Status

| Contract Item | Status |
|--------------|--------|
| `ILazyLoadDeserializable` public | Done |
| `protected LoadTask` property | Done |
| `protected ClearLoadError()` method | Done |
| `OnPropertyChanged` protected virtual | Done |
| CA1033 pragma suppress | Done (not in original contract, needed for build) |
| No test files modified | Confirmed |
| All existing tests pass | Confirmed (2046/2046) |
| Clean build on net9.0 and net10.0 | Confirmed |

### Files Modified

1. `src/RemoteFactory/Internal/ILazyLoadDeserializable.cs` -- `internal` -> `public`
2. `src/RemoteFactory/LazyLoad.cs` -- 3 new members + 1 access modifier change + CA1033 pragma
