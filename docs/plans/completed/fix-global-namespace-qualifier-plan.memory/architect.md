# Architect -- Fix Global Namespace Qualifier

Last updated: 2026-03-26
Current step: Post-implementation verification complete -- VERIFIED

## Key Context

### Root Cause
The generator extracts and renders fully-qualified type names WITHOUT `global::` prefix. This causes C# name resolution failures when a class name in scope matches a namespace segment (e.g., class `PersonModel` in namespace `Person.DomainModel` -- the compiler resolves the leading `Person` as the class rather than the namespace).

### All 7 Fix Locations (verified by reading source)

**Category 1 -- Roslyn symbol to string (switch to FullyQualifiedFormat):**
1. `src/Generator/FactoryGenerator.Types.cs:397` -- `OrdinalPropertyInfo.Type` via `.ToDisplayString(FullyQualifiedFormatWithNullable)`
2. `src/Generator/FactoryGenerator.Types.cs:745` -- Constructor ReturnType via `.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)`

**Category 2 -- Stop stripping global::**
3. `src/Generator/FactoryGenerator.Types.cs:909` -- Removed `.Replace("global::", "")` on DTO types

**Category 3 -- String concatenation (prepend global::):**
4. `src/Generator/Builder/FactoryModelBuilder.cs:981` -- `OrdinalSerializationModel.FullTypeName`
5. `src/Generator/Renderer/ClassFactoryRenderer.cs:60` -- Assembly attribute typeof
6. `src/Generator/Renderer/InterfaceFactoryRenderer.cs:48` -- Assembly attribute typeof
7. `src/Generator/Renderer/StaticFactoryRenderer.cs:47` -- Assembly attribute typeof

### Developer Discovery: FullyQualifiedFormatWithNullable
The developer discovered that `SymbolDisplayFormat.FullyQualifiedFormat` alone strips inner nullable annotations from generic types (e.g., `List<string?>` becomes `List<string>`). They created a custom format combining `FullyQualifiedFormat` with `IncludeNullableReferenceTypeModifier` to preserve nullable annotations. This is a sound approach -- it prevents data loss in the type string while still adding `global::` prefixes.

## Mistakes to Avoid

- Do NOT modify OrdinalRenderer.cs -- it gets correct types automatically from upstream fixes
- Do NOT modify any model type files (Model/*.cs) -- they are data carriers
- The `TrimEnd('?')` logic must be preserved -- it strips nullable annotation for the base type

## User Corrections

None.

## Architectural Verification (Pre-Handoff)

### Scope Table

| # | Location | Change Type | Verified In Source |
|---|----------|------------|-------------------|
| 1 | FactoryGenerator.Types.cs:397 | FullyQualifiedFormatWithNullable | Yes |
| 2 | FactoryGenerator.Types.cs:745 | FullyQualifiedFormat | Yes |
| 3 | FactoryGenerator.Types.cs:909 | Remove .Replace("global::", "") | Yes |
| 4 | FactoryModelBuilder.cs:981 | Prepend global:: | Yes |
| 5 | ClassFactoryRenderer.cs:60 | Prepend global:: | Yes |
| 6 | InterfaceFactoryRenderer.cs:48 | Prepend global:: | Yes |
| 7 | StaticFactoryRenderer.cs:47 | Prepend global:: | Yes |

### Breaking Changes
None. `global::` is purely additive -- it cannot change the semantics of correct code.

## Architect Verification (Post-Implementation)

### Verdict: VERIFIED

### Independent Build Results
- Generator build (netstandard2.0): **0 errors, 0 warnings**
- Person example (`Person.Server.csproj`): **0 errors, 0 warnings** -- this was the project that originally triggered the bug
- Full solution build: **Build succeeded**
- Design tests build: **Build succeeded**

### Independent Test Results

| Test Project | Framework | Total | Passed | Skipped | Failed |
|---|---|---|---|---|---|
| RemoteFactory.UnitTests | net9.0 | 490 | 490 | 0 | 0 |
| RemoteFactory.UnitTests | net10.0 | 490 | 490 | 0 | 0 |
| RemoteFactory.IntegrationTests | net9.0 | 505 | 502 | 3 | 0 |
| RemoteFactory.IntegrationTests | net10.0 | 505 | 502 | 3 | 0 |
| Design.Tests | net9.0 | 42 | 42 | 0 | 0 |
| Design.Tests | net10.0 | 42 | 42 | 0 | 0 |

Zero failures across all test projects and both target frameworks.

### Test Scenario Coverage: 10 of 10 verified

| # | Scenario | Status | Evidence |
|---|----------|--------|----------|
| 1 | Person example builds successfully | PASS | Person.Server.csproj builds with 0 errors |
| 2 | Assembly attribute uses global:: prefix | PASS | `AssemblyAttributeEmissionTests.ClassFactory_EmitsAssemblyAttribute` passes, asserts `typeof(global::TestNamespace.MyEntityFactory)` |
| 3 | Ordinal property types have global:: prefix | PASS | `FullyQualifiedFormatWithNullable` used at extraction; all integration tests pass (ordinal serialization round-trips exercise this) |
| 4 | DTO return types preserve global:: prefix | PASS | `.Replace("global::", "")` removed; integration tests pass (DTO constructor lambdas use these types) |
| 5 | OrdinalSerializationModel.FullTypeName has global:: | PASS | `FactoryModelBuilder.cs:981` confirmed: `$"global::{typeInfo.Namespace}.{typeInfo.ImplementationTypeName}"` |
| 6 | Constructor return type has global:: prefix | PASS | `FactoryGenerator.Types.cs:745` confirmed: `ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)` |
| 7 | All existing integration tests pass | PASS | 502 passed + 3 skipped on both net9.0 and net10.0 |
| 8 | All existing unit tests pass | PASS | 490 passed on both net9.0 and net10.0 |
| 9 | Design project tests pass | PASS | 42 passed on both net9.0 and net10.0 |
| 10 | Built-in types are unaffected | PASS | All tests pass; tests exercise int, string, DateTime properties in ordinal serialization |

### Design Match Verification

All 7 planned change locations were modified as designed. The only deviation from the plan is the `FullyQualifiedFormatWithNullable` custom format -- the plan specified using `SymbolDisplayFormat.FullyQualifiedFormat` directly for property types, but the developer discovered that this strips inner nullable annotations from generic types. The custom format is a correct and necessary refinement that preserves both `global::` prefixes and nullable type information.

### Out-of-Scope Changes: None
Only 6 files modified, all in scope:
- 4 changes in `FactoryGenerator.Types.cs` (3 planned + 1 custom format field)
- 1 change in `FactoryModelBuilder.cs`
- 1 change in each of the 3 renderers
- 3 assertion updates in `AssemblyAttributeEmissionTests.cs` (expected -- old assertions were wrong)
