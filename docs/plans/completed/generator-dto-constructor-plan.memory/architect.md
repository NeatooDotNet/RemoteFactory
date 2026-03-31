# Architect -- Generator DTO Constructor Emission

Last updated: 2026-03-30
Current step: Step 7A complete -- Post-implementation verification VERIFIED

## Key Context

### Pipeline Architecture (verified by reading actual source)

The critical insight: `ITypeSymbol` is available in the `MethodInfo` constructor at `FactoryGenerator.Types.cs:761` where `methodSymbol.ReturnType` is used. By the time `FactoryModelBuilder` runs, `ReturnType` is already a string. DTO analysis happens in `MethodInfo` constructor via `DiscoverDtoReturnTypes()`.

### Data Flow Path (verified post-implementation)

```
MethodInfo constructor (ITypeSymbol) --> MethodInfo.DtoReturnTypes (EquatableArray<string>)
    |
TypeInfo constructor aggregates --> TypeInfo.DtoReturnTypes (deduplicated EquatableArray<string>)
    |
FactoryModelBuilder.Build*() passes through --> Model.DtoReturnTypes (IReadOnlyList<string>)
    |
Each Renderer's RenderFactoryServiceRegistrar emits:
    DtoConstructorRegistry.Register<{DtoType}>(() => new {DtoType}());
```

### Three Model Types Have DtoReturnTypes (verified)

- `ClassFactoryModel` (constructor param `dtoReturnTypes`, property `DtoReturnTypes`)
- `InterfaceFactoryModel` (constructor param `dtoReturnTypes`, property `DtoReturnTypes`)
- `StaticFactoryModel` (constructor param `dtoReturnTypes`, property `DtoReturnTypes`)

All three are passed `typeInfo.DtoReturnTypes.ToList()` in `FactoryModelBuilder`.

### EquatableArray Constraint (verified)

`EquatableArray<string>` satisfies the incremental generator pipeline's equality requirement.

### RecordBypassConverterFactory Rule (verified)

Records excluded correctly: `DiscoverDtoReturnTypes` checks for public parameterless constructor (line 927-934). Types without one are skipped.

## Mistakes to Avoid

1. **Do NOT add DTO discovery in FactoryModelBuilder** -- `ReturnType` is already a string there. Must happen in `MethodInfo` constructor where `ITypeSymbol` is available.

2. **Do NOT over-engineer** -- Keep it simple: static dictionary, string-keyed discovery, emitted registrations in existing `FactoryServiceRegistrar`.

3. **Do NOT supplement Activator.CreateInstance -- REPLACE it** -- The user explicitly wants the `Activator.CreateInstance` fallback removed.

4. **Do NOT modify FactoryGenerationUnit** -- Data flows through TypeInfo -> Model path.

## User Corrections

None.

## Architectural Verification (Pre-Handoff)

### Scope Table

| Claim | Evidence | Status |
|-------|----------|--------|
| `MethodInfo` constructor has `ITypeSymbol` access | `FactoryGenerator.Types.cs:761` -- `methodSymbol.ReturnType` passed to `DiscoverDtoReturnTypes` | Verified |
| `EquatableArray<string>` works for pipeline | `EquatableArray<T>` constraint is `IEquatable<T>`; `string` satisfies | Verified |
| Auth type registration is the precedent | Renderers emit registrations in `FactoryServiceRegistrar` | Verified |
| `RecordBypassConverterFactory` excludes records | `RecordBypassConverterFactory.cs:56-57`: `!hasParameterlessCtor && hasParameterizedCtor` | Verified |
| `ExampleDto` has public parameterless constructor | `AllPatterns.cs` | Verified |
| `ExampleRecordResult` has no parameterless constructor | `AllPatterns.cs` -- record with primary constructor | Verified |
| Design tests exist for both DTO and record round-trip | `InterfaceFactoryTests.cs:35` and `:96` | Verified |

## Architect Verification (Post-Implementation)

### Verdict: VERIFIED

**Date:** 2026-03-30
**Verification method:** Independent build and test execution

### Build Results

- Full solution build: **0 errors, 3 warnings** (all WASM0001 warnings from OrderEntry.BlazorClient, pre-existing)

### Test Results (independently run)

| Project | Framework | Passed | Failed | Skipped |
|---------|-----------|--------|--------|---------|
| RemoteFactory.UnitTests | net9.0 | 517 | 0 | 0 |
| RemoteFactory.UnitTests | net10.0 | 517 | 0 | 0 |
| RemoteFactory.IntegrationTests | net9.0 | 506 | 0 | 3 |
| RemoteFactory.IntegrationTests | net10.0 | 506 | 0 | 3 |
| Design.Tests | net9.0 | 48 | 0 | 0 |
| Design.Tests | net10.0 | 48 | 0 | 0 |
| **TOTAL** | | **2142** | **0** | **6** |

**Note:** Test counts are slightly higher than developer's reported numbers (2068). The difference (74 more tests) is likely from the `lazyload-protected-for-inheritance` merge that occurred between the developer's run and this verification. All 6 skipped tests are `ShowcasePerformanceTests` -- pre-existing skips, not related to this feature.

### Source Code Verification

1. **`Activator.CreateInstance` removed from `NeatooJsonTypeInfoResolver.cs`**: CONFIRMED. No matches for `Activator.CreateInstance` in the file.
2. **`System.Reflection` import removed**: CONFIRMED. No matches for `System.Reflection` in the file.
3. **`BindingFlags` usage removed**: CONFIRMED. No matches for `BindingFlags` in the file.
4. **`DtoConstructorRegistry.TryCreate` used instead**: CONFIRMED. Lines 33-39 of `NeatooJsonTypeInfoResolver.cs` use `DtoConstructorRegistry.TryCreate(type, out var factory)`.
5. **`[DynamicallyAccessedMembers]` annotation on `Register<T>`**: CONFIRMED. Line 22 of `DtoConstructorRegistry.cs`.

### Generated Code Verification

Forced generated file emission (`EmitCompilerGeneratedFiles=true`, already configured in Design.Domain.csproj) and inspected:

1. **`IExampleRepositoryFactory.g.cs` line 137**: `DtoConstructorRegistry.Register<global::Design.Domain.FactoryPatterns.ExampleDto>(() => new global::Design.Domain.FactoryPatterns.ExampleDto())` -- CONFIRMED.
2. **Emitted OUTSIDE `if(remoteLocal)` block**: CONFIRMED. The `if(remoteLocal)` block closes at line 134; the registration is at line 137.
3. **`ExampleRecordResult` NOT registered**: CONFIRMED. `ExampleRecordResult` appears in delegate/method signatures but NOT in any `DtoConstructorRegistry.Register` call.
4. **No `DtoConstructorRegistry` calls in class factory or static factory generated files**: CONFIRMED. Neither `ExampleClassFactoryFactory.g.cs` nor `ExampleCommandsFactory.g.cs` contain `DtoConstructorRegistry`.
5. **Integration test generated code also emits correctly**: CONFIRMED. `DuplicateSaveWithFetchBugFactory.g.cs:673` emits `DtoConstructorRegistry.Register<TestEntityForBug>`.

### Test Scenario Cross-Check (7 of 7 verified)

| # | Plan Scenario | Verification Method | Result |
|---|--------------|---------------------|--------|
| 1 | Interface factory returns `Task<IReadOnlyList<ExampleDto>>` | `InterfaceFactory_GetAllAsync_ReturnsDataFromServer` -- ran individually, PASSED | VERIFIED |
| 2 | Interface factory returns `Task<ExampleDto?>` | `InterfaceFactory_GetByIdAsync_ReturnsSpecificItem` -- ran individually, PASSED | VERIFIED |
| 3 | Interface factory returns record `Task<ExampleRecordResult?>` | `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` -- ran individually, PASSED | VERIFIED |
| 4 | Class factory returns own type | Existing class factory tests all pass (506 integration tests, 0 failures). Generated code inspection confirms NO `DtoConstructorRegistry` calls for class factories. | VERIFIED |
| 5 | Static factory `[Execute]` returns `Task<bool>` | Existing static factory tests all pass. Generated `ExampleCommandsFactory.g.cs` confirms NO `DtoConstructorRegistry` calls (bool is primitive). | VERIFIED |
| 6 | Generated code contains `DtoConstructorRegistry.Register` for ExampleDto | Inspected `IExampleRepositoryFactory.g.cs:137` -- exact call confirmed. | VERIFIED |
| 7 | No `Activator.CreateInstance` in NeatooJsonTypeInfoResolver | Source inspection confirms removal. `BindingFlags` and `System.Reflection` also removed. | VERIFIED |

**7 of 7 test scenarios verified with passing tests and/or generated code inspection.**

### Acceptance Criteria Cross-Check

| Criterion | Status |
|-----------|--------|
| `DtoConstructorRegistry` class exists in `src/RemoteFactory/Internal/` | VERIFIED |
| `NeatooJsonTypeInfoResolver` uses `DtoConstructorRegistry.TryCreate` instead of `Activator.CreateInstance` | VERIFIED |
| Generated `FactoryServiceRegistrar` for `IExampleRepository` includes `DtoConstructorRegistry.Register<ExampleDto>` | VERIFIED |
| Generated code does NOT register `ExampleRecordResult` | VERIFIED |
| All existing tests pass (net9.0 and net10.0) | VERIFIED (2142 passed, 0 failed) |
| Design `InterfaceFactory_GetAllAsync_ReturnsDataFromServer` passes | VERIFIED |
| Design `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` passes | VERIFIED |
| `Activator.CreateInstance` call removed from `NeatooJsonTypeInfoResolver` | VERIFIED |
| No new `System.Reflection` usage | VERIFIED (net improvement: removed import and `BindingFlags`) |

### Known Scope Limitation

The generator does NOT walk DTO properties recursively (nested DTOs are not discovered). For example, if `ExampleDto` had a property `List<InnerDto> Items`, `InnerDto` would NOT be registered. This is documented in the todo's Progress Log (2026-03-31) and is being tracked separately. It does NOT block verification of the current scope.

### Minor Code Quality Observation

In `FactoryGenerator.Types.cs:927-934`, there is a dead first assignment to `hasPublicParameterlessCtor` (lines 927-931 with a logic error involving `!c.IsImplicitlyDeclared == false`) that is immediately overwritten by the correct simplified version on lines 933-934. The net behavior is correct, but the first assignment is dead code. Not a functional issue.

### Files Created

| File | Purpose | Verified |
|------|---------|----------|
| `src/RemoteFactory/Internal/DtoConstructorRegistry.cs` | Static registry for DTO constructor lambdas | Yes |

### Files Modified

| File | Change | Verified |
|------|--------|----------|
| `src/Generator/FactoryGenerator.Types.cs` | `DtoReturnTypes` on `MethodInfo`, `DiscoverDtoReturnTypes` method, `TypeInfo` aggregation | Yes |
| `src/Generator/Model/InterfaceFactoryModel.cs` | `DtoReturnTypes` property | Yes |
| `src/Generator/Model/ClassFactoryModel.cs` | `DtoReturnTypes` property | Yes |
| `src/Generator/Model/StaticFactoryModel.cs` | `DtoReturnTypes` property | Yes |
| `src/Generator/Builder/FactoryModelBuilder.cs` | Pass `DtoReturnTypes` to each model | Yes |
| `src/Generator/Renderer/InterfaceFactoryRenderer.cs` | Emit `DtoConstructorRegistry.Register` calls | Yes |
| `src/Generator/Renderer/ClassFactoryRenderer.cs` | Emit `DtoConstructorRegistry.Register` calls | Yes |
| `src/Generator/Renderer/StaticFactoryRenderer.cs` | Emit `DtoConstructorRegistry.Register` calls | Yes |
| `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs` | Replace `Activator.CreateInstance` with `DtoConstructorRegistry.TryCreate` | Yes |
