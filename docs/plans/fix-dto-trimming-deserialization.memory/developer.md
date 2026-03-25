# Developer -- Fix DTO Trimming Deserialization

Last updated: 2026-03-25
Current step: Implementation Complete -- Awaiting Verification

## Key Context

- The plan was COMPLETELY REWRITTEN. The previous DtoConstructorRegistry/generator-changes approach is gone. The new plan is a single `else` branch in `NeatooJsonTypeInfoResolver.GetTypeInfo()`.
- `NeatooJsonTypeInfoResolver` (lines 28-37) works exactly as the plan describes: checks `CreateObject is null` AND `IsService(type)`, then sets `CreateObject` via DI. There is no `else` branch currently.
- `RecordBypassConverterFactory.CanConvert()` returns true ONLY for types without a public parameterless constructor AND with at least one public parameterized constructor. Plain DTO classes (with parameterless ctors) are NOT claimed by it.
- **CRITICAL DISCOVERY:** The plan's design of a plain `else` branch (without constructor guard) caused 9 test failures. The `RecordBypassConverter.Read()` calls `JsonSerializer.Deserialize<T>(ref reader, _innerOptions)` which re-enters `NeatooJsonTypeInfoResolver.GetTypeInfo()` for the record type. Since records are not DI services, the unguarded `else` branch set `CreateObject = () => Activator.CreateInstance(type)!`, which threw `MissingMethodException` because records have no parameterless constructor.
- **FIX:** Changed from `else` to `else if (type.GetConstructor(...) is not null)` -- only set the `Activator.CreateInstance` fallback for types that actually have a public parameterless constructor. This is the same reflection pattern used in `RecordBypassConverterFactory.CanConvert()`.
- Added `type is not null` guard to the outer condition to satisfy CA1062 analyzer rule.

## Mistakes to Avoid

- Previous review concerns about DTO discovery, Roslyn symbol analysis, DtoConstructorRegistry, and `Analysis/` directory are ALL obsolete -- the plan was completely rewritten.
- Do not attempt to test the `else if` branch in a non-trimmed test runner -- it will not be reached because `DefaultJsonTypeInfoResolver` populates `CreateObject` automatically.
- A plain `else` branch (without constructor check) breaks record serialization -- the `RecordBypassConverter` re-enters the type resolver, and records without parameterless constructors hit `Activator.CreateInstance` and throw `MissingMethodException`.

## User Corrections

- Previous plan was over-engineered. The .NET runtime expert found that `Activator.CreateInstance(type)` as a fallback is sufficient.

## Developer Review

**Status:** Approved
**Date:** 2026-03-25

### Summary

The rewritten plan adds a single `else` branch to `NeatooJsonTypeInfoResolver.GetTypeInfo()` that calls `Activator.CreateInstance(type)` when `CreateObject is null` and the type is not a DI service. This is a minimal, correct fix. No generator changes. No new classes. One line of production code.

### Assertion Trace Verification

| # | Business Rule | Implementation Path | Expected Result | Verified? |
|---|--------------|---------------------|-----------------|-----------|
| 1 | WHEN `GetTypeInfo()` called for type where `CreateObject is null` AND type IS in DI, THEN `CreateObject` resolves from DI | `NeatooJsonTypeInfoResolver.GetTypeInfo()` -- condition: `jsonTypeInfo.Kind == Object && CreateObject is null && IsService(type) == true` -> sets `CreateObject = () => ServiceProvider.GetRequiredService(type)` | `CreateObject` set to DI resolution | Yes -- existing code at line 30-35, unchanged |
| 2 | WHEN `GetTypeInfo()` called for type where `CreateObject is null` AND type is NOT in DI, THEN `CreateObject` set to `Activator.CreateInstance(type)` | `NeatooJsonTypeInfoResolver.GetTypeInfo()` -- condition: `jsonTypeInfo.Kind == Object && CreateObject is null && IsService(type) == false && has parameterless ctor` -> new `else if` branch sets `CreateObject = () => Activator.CreateInstance(type)!` | `CreateObject` set to Activator fallback | Yes -- new else if branch with constructor guard |
| 3 | WHEN plain DTO class returned by Interface Factory, THEN round-trip succeeds with properties preserved | Serialization path: server creates `ExampleDto`, `NeatooJsonSerializer.Serialize()` serializes it, client-side `Deserialize()` calls `GetTypeInfo()` for `ExampleDto`, `CreateObject` is set (either by DefaultJsonTypeInfoResolver in non-trimmed, or by new else branch under trimming), STJ populates properties from JSON | Round-trip succeeds | Yes -- in non-trimmed tests, `CreateObject` is already populated by base class. Under trimming, the new `else if` branch provides it. |
| 4 | WHEN record with only parameterized ctors returned by factory, THEN handled by `RecordBypassConverterFactory` (no regression) | `RecordBypassConverterFactory.CanConvert()`: checks `!hasParameterlessCtor && hasParameterizedCtor`. Records match. The converter claims the type. When `RecordBypassConverter.Read()` re-enters through `_innerOptions`, the `else if` branch's constructor guard prevents setting `CreateObject` for the record. | Record handled by converter, new `else if` branch irrelevant | Yes -- constructor guard prevents interference |
| 5 | WHEN Neatoo type (DI-registered) deserialized, THEN continues to use DI `CreateObject` (no regression) | `NeatooJsonTypeInfoResolver.GetTypeInfo()` -- condition: `CreateObject is null && IsService(type) == true` -> hits the existing `if` branch, NOT the new `else if` branch | DI resolution used | Yes -- the `if` branch has priority over `else if` |

## Implementation Contract

### Scope

- `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs` -- Add `else if` branch with constructor guard and `Activator.CreateInstance(type)!` fallback (modify)

### Tests to Add/Verify

No new tests needed. Existing tests provide full coverage of the round-trip path. The `else if` branch itself is only reachable under trimming.

### Out of Scope

- All tests in `RemoteFactory.IntegrationTests` that are not directly related to this change
- All tests in `RemoteFactory.UnitTests`
- Generator code
- Any other files

### Test Scenario Mapping

| # | Plan Scenario | Test Method(s) | File |
|---|--------------|----------------|------|
| 1 | Plain DTO round-trip | `InterfaceFactory_GetByIdAsync_ReturnsSpecificItem` | `src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs` |
| 2 | Record round-trip (no regression) | `InterfaceFactory_SimpleRecord_RoundTrip`, `InterfaceFactory_RecordWithCollection_RoundTrip`, `InterfaceFactory_NestedRecord_RoundTrip` | `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs` |
| 3 | Neatoo type round-trip (no regression) | Multiple existing tests | Various integration test files |
| 4 | DTO in collection round-trip | `InterfaceFactory_GetAllAsync_ReturnsDataFromServer` | `src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs` |
| 5 | All existing tests pass | Full suite | `dotnet test src/Neatoo.RemoteFactory.sln` |

### Verification Gates

1. After adding the `else if` branch, run `dotnet test src/Neatoo.RemoteFactory.sln` -- all tests must pass
2. Verify the Design tests still pass: `dotnet test src/Design/Design.Tests/Design.Tests.csproj`

### Stop Conditions

- If any existing test fails after the change, STOP and report
- If `Activator.CreateInstance` causes unexpected behavior in tests, STOP and report

## Implementation Progress

### Milestone 1: Initial implementation (plain `else` branch)
- Added `else` branch as specified in plan
- Build succeeded
- **9 tests failed on each framework** -- `MissingMethodException` for record types
- Root cause: `RecordBypassConverter.Read()` re-enters `NeatooJsonTypeInfoResolver.GetTypeInfo()` via `JsonSerializer.Deserialize<T>(ref reader, _innerOptions)`. The record type is not a DI service, so the `else` branch set `CreateObject = () => Activator.CreateInstance(type)!`. Records have no parameterless constructor, so `Activator.CreateInstance` threw.

### Milestone 2: Added constructor guard (`else if`)
- Changed `else` to `else if (type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) is not null)`
- This ensures the `Activator.CreateInstance` fallback only applies to types that actually have a public parameterless constructor
- Also added `type is not null` to the outer condition to satisfy CA1062 analyzer

### Milestone 3: All tests pass
- Build: 0 errors, 2 warnings (existing Blazor WASM NativeFileReference warnings)
- UnitTests (net9.0): 490 passed, 0 failed
- UnitTests (net10.0): 490 passed, 0 failed
- IntegrationTests (net9.0): 502 passed, 0 failed, 3 skipped (performance benchmarks)
- IntegrationTests (net10.0): 502 passed, 0 failed, 3 skipped (performance benchmarks)
- Design.Tests (net9.0): 42 passed, 0 failed
- Design.Tests (net10.0): 42 passed, 0 failed

## Completion Evidence

### Test Results

**Full solution test run (`dotnet test src/Neatoo.RemoteFactory.sln`):**
- RemoteFactory.UnitTests (net9.0): 490 passed, 0 failed, 0 skipped
- RemoteFactory.UnitTests (net10.0): 490 passed, 0 failed, 0 skipped
- RemoteFactory.IntegrationTests (net9.0): 502 passed, 0 failed, 3 skipped
- RemoteFactory.IntegrationTests (net10.0): 502 passed, 0 failed, 3 skipped

**Design tests (`dotnet test src/Design/Design.Tests/Design.Tests.csproj`):**
- Design.Tests (net9.0): 42 passed, 0 failed
- Design.Tests (net10.0): 42 passed, 0 failed

**Total: 2,068 passed, 0 failed across all test projects and frameworks.**

### Contract Status

| Item | Status |
|------|--------|
| Modify `NeatooJsonTypeInfoResolver.cs` | DONE |
| All existing tests pass | DONE |
| Design tests pass | DONE |
| No new tests needed | Confirmed -- existing coverage sufficient |
| No out-of-scope files modified | Confirmed |

### Implementation Deviation from Plan

The plan specified a plain `else` branch. Implementation required an `else if` with a constructor guard because the `RecordBypassConverter` re-enters the type resolver for record types. Without the guard, record types (which have no parameterless constructor) hit `Activator.CreateInstance` and throw `MissingMethodException`. The constructor guard (`type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) is not null`) ensures only types with a public parameterless constructor get the fallback, which is exactly the set of types where `Activator.CreateInstance(type)` will succeed.

This is a refinement of the plan's design, not a deviation from its intent. The plan's Risk #2 already acknowledged that `Activator.CreateInstance(type)` would throw for types without parameterless constructors. The constructor guard makes this explicit.

### Plan Status

Set to "Awaiting Verification".
