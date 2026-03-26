# Developer -- Fix Missing global:: Namespace Qualifier

Last updated: 2026-03-26
Current step: Implementation complete, all verification gates passed

## Key Context

This is a bug fix for the RemoteFactory source generator: it emits namespace-qualified type references without `global::` prefix, causing potential compilation errors when a class name matches a namespace segment.

The plan identified 7 locations across 5 files. All 7 were modified successfully, plus one additional fix was needed: a custom `SymbolDisplayFormat` (`FullyQualifiedFormatWithNullable`) was created to preserve inner nullable annotations in generic types (e.g., `List<string?>` must not lose the inner `?`).

## Mistakes to Avoid

- Do not modify parameter type extraction (`MethodParameterInfo.Type`) -- it uses syntax-level `ToFullString()` which preserves the user's original text.
- Do not modify `MethodInfo.ReturnType` (lines 694, 704 of Types.cs) -- these flow into renderers using simple names within namespace blocks.
- Lines 81, 154, 156, 292, 362 of Transform.cs are comparison-only uses -- they do NOT flow into generated code.
- **IMPORTANT**: `SymbolDisplayFormat.FullyQualifiedFormat` alone strips inner nullable annotations from generic types (e.g., `List<string?>` becomes `List<string>`). Must use a custom format with `IncludeNullableReferenceTypeModifier` for property type extraction. This was discovered during implementation when `RecordWithComplexNullableGenerics` tests failed with CS8620.

## User Corrections

(none)

## Developer Review

**Status:** Approved
**Date:** 2026-03-26

### Summary

The plan proposes adding `global::` prefix to 7 namespace-qualified type references across 5 generator source files. This is a mechanical, low-risk fix following source generator best practices.

### Assertion Trace Verification

| # | Business Rule | Implementation Path | Expected Result | Verified? |
|---|--------------|---------------------|-----------------|-----------|
| 1 | Assembly attribute typeof must use `global::` | ClassFactoryRenderer.cs:60, InterfaceFactoryRenderer.cs:48, StaticFactoryRenderer.cs:47 -- string interpolation `typeof({unit.Namespace}.{model.XxxTypeName}...)` gains `global::` prefix | `typeof(global::Ns.TypeFactory)` | Yes |
| 2 | OrdinalPropertyInfo.Type must use FullyQualifiedFormat | FactoryGenerator.Types.cs:396 -- `.ToDisplayString(FullyQualifiedFormatWithNullable)` (custom format preserving inner nullable annotations) | Type strings like `global::Some.Namespace.SomeType` instead of `Some.Namespace.SomeType`; built-in types like `int` stay as `int`; inner nullable annotations preserved | Yes |
| 3 | OrdinalSerializationModel.FullTypeName must have `global::` | FactoryModelBuilder.cs:991 -- `$"global::{typeInfo.Namespace}.{typeInfo.ImplementationTypeName}"` | `global::Ns.TypeName` | Yes |
| 4 | DTO return types must keep `global::` | FactoryGenerator.Types.cs:908 -- `.Replace("global::", "")` removed | DTO type strings retain `global::` prefix from FullyQualifiedFormat | Yes |
| 5 | Constructor ReturnType must use FullyQualifiedFormat | FactoryGenerator.Types.cs:744 -- `.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)` | Constructor return type like `global::Ns.RecordType` | Yes |
| 6 | Namespace+TypeName in assembly attrs must have `global::` | Same as Rule 1 | `typeof(global::Ns.TypeFactory)` | Yes |
| 7 | Person example with name/namespace collision must compile | End-to-end: all fixes combined | Build succeeds | Yes |
| 8 | Property type refs in ordinal code get `global::` from extraction | OrdinalRenderer.cs consumes `prop.Type` from model -- after Rule 2 fix, all downstream uses automatically get `global::` | `typeof(global::Ns.Type)` in ordinal serialization code | Yes |
| 9 | Existing tests must continue to pass | After all changes, run full test suite | All pass | Yes |

## Implementation Contract

### Scope

**Files modified (source):**

1. `src/Generator/FactoryGenerator.Types.cs` (4 changes)
   - Added `FullyQualifiedFormatWithNullable` static field (custom format combining FullyQualifiedFormat + IncludeNullableReferenceTypeModifier)
   - Line ~396: `.ToDisplayString()` -> `.ToDisplayString(FullyQualifiedFormatWithNullable)` (uses custom format to preserve inner nullable annotations)
   - Line ~744: `.ToDisplayString()` -> `.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)`
   - Line ~908: Removed `.Replace("global::", "")`

2. `src/Generator/Builder/FactoryModelBuilder.cs` (1 change)
   - Line ~991: `$"{typeInfo.Namespace}.{typeInfo.ImplementationTypeName}"` -> `$"global::{typeInfo.Namespace}.{typeInfo.ImplementationTypeName}"`

3. `src/Generator/Renderer/ClassFactoryRenderer.cs` (1 change)
   - Line 60: Added `global::` before `{unit.Namespace}`

4. `src/Generator/Renderer/InterfaceFactoryRenderer.cs` (1 change)
   - Line 48: Added `global::` before `{unit.Namespace}`

5. `src/Generator/Renderer/StaticFactoryRenderer.cs` (1 change)
   - Line 47: Added `global::` before `{unit.Namespace}`

**Files modified (tests -- in-scope expected string updates):**

6. `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/AssemblyAttributeEmissionTests.cs` (3 assertions)
   - Line 42: `typeof(TestNamespace.MyEntityFactory)` -> `typeof(global::TestNamespace.MyEntityFactory)`
   - Line 142: `typeof(TestNamespace.MyCommands)` -> `typeof(global::TestNamespace.MyCommands)`
   - Line 176: `typeof(TestNamespace.MyServiceFactory)` -> `typeof(global::TestNamespace.MyServiceFactory)`

### Out of Scope

- `src/Generator/FactoryGenerator.Transform.cs` -- comparison-only uses
- `src/Generator/FactoryGenerator.Types.cs` lines 694, 704 -- MethodInfo.ReturnType
- `src/Generator/Renderer/OrdinalRenderer.cs` -- consumes model data, no changes needed
- All tests NOT listed above

### Test Scenario Mapping

| # | Plan Scenario | Test Method / Verification | File / Command | Result |
|---|--------------|---------------------------|----------------|--------|
| 1 | Person example builds | `dotnet build src/Examples/Person/Person.Server/Person.Server.csproj` | CLI | PASS |
| 2 | Assembly attribute uses global:: | `ClassFactory_EmitsAssemblyAttribute`, `StaticFactory_EmitsAssemblyAttribute`, `InterfaceFactory_EmitsAssemblyAttribute` | AssemblyAttributeEmissionTests.cs | PASS |
| 3 | Ordinal property types have global:: | Covered by integration tests (ordinal serialization round-trip) | Integration test suite | PASS |
| 4 | DTO return types preserve global:: | Covered by integration tests (DTO serialization) | Integration test suite | PASS |
| 5 | OrdinalSerializationModel.FullTypeName has global:: | Covered by integration tests | Integration test suite | PASS |
| 6 | Constructor return type has global:: | Covered by integration tests (record factory) | Integration test suite | PASS |
| 7 | All existing integration tests pass | `dotnet test src/Tests/RemoteFactory.IntegrationTests/` | CLI | PASS (502 passed, 3 skipped per TFM) |
| 8 | All existing unit tests pass | `dotnet test src/Tests/RemoteFactory.UnitTests/` | CLI | PASS (490 passed per TFM) |
| 9 | Design project tests pass | `dotnet test src/Design/Design.Tests/` | CLI | PASS (42 passed per TFM) |
| 10 | Built-in types unaffected | Covered by existing tests using int, string, DateTime properties | Integration test suite | PASS |

## Implementation Progress

All milestones completed:

1. Applied 7 source changes across 5 generator files -- DONE
2. Discovered that `FullyQualifiedFormat` strips inner nullable annotations from generic types -- FIXED by creating `FullyQualifiedFormatWithNullable` custom format (8th change, beyond the original 7)
3. Updated 3 test assertions in AssemblyAttributeEmissionTests -- DONE
4. Verification Gate 1 (generator build): PASS
5. Verification Gate 2 (unit tests): PASS (490/490 on both TFMs)
6. Verification Gate 3 (full solution tests): PASS (490/490 unit, 502/502 integration on both TFMs)
7. Verification Gate 4 (Person example build): PASS
8. Verification Gate 5 (Design tests): PASS (42/42 on both TFMs)

## Completion Evidence

### Test Results

**Unit Tests:** 490 passed, 0 failed, 0 skipped (net9.0); 490 passed, 0 failed, 0 skipped (net10.0)
**Integration Tests:** 502 passed, 0 failed, 3 skipped (net9.0); 502 passed, 0 failed, 3 skipped (net10.0)
**Design Tests:** 42 passed, 0 failed, 0 skipped (net9.0); 42 passed, 0 failed, 0 skipped (net10.0)
**Person Example:** Build succeeded, 0 warnings, 0 errors

### Contract Status

All contract items fulfilled. One additional change was required beyond the contract: a custom `SymbolDisplayFormat` (`FullyQualifiedFormatWithNullable`) was created and used at the OrdinalPropertyInfo.Type extraction site (instead of plain `FullyQualifiedFormat`) to preserve inner nullable annotations in generic types like `List<string?>`.

### Note on Flaky Tests

During one full-solution test run, 2 tests in `CanMethodCodePathTests` failed on net10.0 only (`CanMethod_Public_ReturnsAuthorized_WhenAllowed` and `CanLocalMethod_IsPublic`). These were confirmed as pre-existing flaky tests caused by shared static state (`CanMethodTestAuth.ShouldAllow`) in parallel test execution -- they pass when run in isolation and passed on subsequent full-solution runs. They are NOT related to the global:: changes.
