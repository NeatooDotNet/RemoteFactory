# Developer -- Generator-Emitted DTO Constructor Lambdas

Last updated: 2026-03-25
Current step: Post-implementation -- added DynamicallyAccessedMembers annotation to Register<T>

## Key Context

- Plan proposes generator-emitted `() => new Dto()` lambdas to replace `Activator.CreateInstance` in `NeatooJsonTypeInfoResolver`
- This is the second attempt (v0.23.2 used Activator.CreateInstance, which also fails under IL trimming)
- Pipeline: `MethodInfo` constructor (ITypeSymbol) -> `TypeInfo.DtoReturnTypes` -> Model `DtoReturnTypes` -> Renderer emits `DtoConstructorRegistry.Register<T>()`
- Baseline: 490 unit tests, 502 integration tests, 42 design tests all pass on both net9.0/net10.0

## Mistakes to Avoid

- The `DiscoverDtoReturnTypes` method must handle both explicit and implicit parameterless constructors (the `IsImplicitlyDeclared` check). Final logic: `c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length == 0` which includes both.
- Both `MethodInfo` constructors (BaseMethodDeclarationSyntax at line 666 and RecordDeclarationSyntax at line 703) must initialize `DtoReturnTypes` to avoid uninitialized EquatableArray.

## User Corrections

(None)

## Developer Review

**Status:** Approved
**Date:** 2026-03-25

### Summary

The plan adds compile-time DTO constructor registration to survive IL trimming. The generator discovers plain DTO return types in factory method signatures (where `ITypeSymbol` is available), stores them as `EquatableArray<string>`, and emits `DtoConstructorRegistry.Register<T>(() => new T())` calls in each factory's `FactoryServiceRegistrar`. At runtime, `NeatooJsonTypeInfoResolver` uses the registry instead of `Activator.CreateInstance`.

### Assertion Trace Verification

| # | Business Rule | Implementation Path | Expected Result | Verified? |
|---|--------------|---------------------|-----------------|-----------|
| 1 | WHEN return type is a class with public parameterless ctor, NOT [Factory], NOT primitive -- THEN emit `DtoConstructorRegistry.Register<Dto>()` | `MethodInfo` ctor at `FactoryGenerator.Types.cs:666-698` -- new `DiscoverDtoReturnTypes(methodSymbol.ReturnType)` checks: unwrap Task/nullable/collection, then filter: has public parameterless ctor AND no `[Factory]` attr AND not primitive/framework AND not abstract/interface. Qualifying names stored in `MethodInfo.DtoReturnTypes`. Aggregated in `TypeInfo.DtoReturnTypes`. Passed to model's `DtoReturnTypes`. Renderer emits `DtoConstructorRegistry.Register<{type}>(() => new {type}())` in `RenderFactoryServiceRegistrar`. | `DtoConstructorRegistry.Register<ExampleDto>(() => new ExampleDto())` emitted for `IExampleRepository` | Yes |
| 2 | WHEN return type is generic collection (e.g. `IReadOnlyList<ExampleDto>`) -- THEN unwrap to discover inner DTO types | `DiscoverDtoReturnTypes` -- after unwrapping Task<T> and nullable, checks if type implements `IEnumerable<T>` and extracts T. Then applies Rule 1 checks to T. | `IReadOnlyList<ExampleDto>` unwraps to `ExampleDto`, which passes all checks and gets registered | Yes |
| 3 | WHEN return type is a record (no parameterless ctor + has parameterized ctors) -- THEN no DTO registration | `DiscoverDtoReturnTypes` -- "skip if no public parameterless constructor" check. `ExampleRecordResult(int Id, string Name)` has only a parameterized ctor. Fails the "has public parameterless ctor" check. | `ExampleRecordResult` NOT registered | Yes |
| 4 | WHEN return type is a [Factory]-annotated type -- THEN no DTO registration | `DiscoverDtoReturnTypes` -- "skip if has [Factory] attribute" check. These types are DI-registered and handled by the `ServiceProviderIsService.IsService(type)` branch in `NeatooJsonTypeInfoResolver`. | `[Factory]` types like `Order` NOT registered | Yes |
| 5 | WHEN type has registered constructor in `DtoConstructorRegistry` and `CreateObject is null` -- THEN `CreateObject` uses registered lambda | `NeatooJsonTypeInfoResolver.GetTypeInfo()` -- replace `else if (type.GetConstructor(...))` block with `else if (DtoConstructorRegistry.TryCreate(type, out var factory))` block. Sets `jsonTypeInfo.CreateObject = factory`. | `ExampleDto` deserialization uses `() => new ExampleDto()` lambda | Yes |
| 6 | WHEN type NOT in DI AND NOT in `DtoConstructorRegistry` -- THEN `CreateObject` NOT set | After replacing the `Activator.CreateInstance` block, if `DtoConstructorRegistry.TryCreate` returns false, control falls through to `return jsonTypeInfo` with `CreateObject` still null. STJ uses its default behavior. | Clear error under trimming instead of mysterious `Activator.CreateInstance` failure | Yes |
| 7 | WHEN `ExampleDto` round-trips through client/server serialization -- THEN all properties preserved | `DtoConstructorRegistry.Register<ExampleDto>()` called during startup. `NeatooJsonTypeInfoResolver` uses the lambda. STJ can create the object. Property setters work normally. | `InterfaceFactory_GetAllAsync_ReturnsDataFromServer` continues to pass | Yes |
| 8 | WHEN `ExampleRecordResult` round-trips -- THEN `RecordBypassConverterFactory` handles it | `RecordBypassConverterFactory.CanConvert()` at line 36-57 claims types with `!hasParameterlessCtor && hasParameterizedCtor`. `ExampleRecordResult` matches. `NeatooJsonTypeInfoResolver` never reaches the DTO branch. | `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` passes unchanged | Yes |
| 9 | WHEN Neatoo DI-registered type deserialized -- THEN DI-based `CreateObject` used | `NeatooJsonTypeInfoResolver.GetTypeInfo()` first checks `ServiceProviderIsService.IsService(type)`. DI-registered types match here and never reach the DTO registry branch. | No regression for entity deserialization | Yes |
| 10 | WHEN DTO registration emitted -- THEN in `FactoryServiceRegistrar` method, executes during `AddNeatooRemoteFactory()` | Renderer emits `DtoConstructorRegistry.Register<T>()` calls inside the same `FactoryServiceRegistrar` method that already has auth type registrations. This method is called by `AddNeatooRemoteFactory()` during startup. | Registration happens at application startup, before any deserialization | Yes |

### Test Scenario Verification

| # | Scenario | Maps to Rule(s) | Implementation Trace | Verified? |
|---|----------|-----------------|---------------------|-----------|
| 1 | Interface factory returns `Task<IReadOnlyList<ExampleDto>>` | 1, 2, 5, 7 | Existing `InterfaceFactory_GetAllAsync_ReturnsDataFromServer` test. Generator emits Register for ExampleDto. Resolver uses lambda. Round-trip works. | Yes |
| 2 | Interface factory returns `Task<ExampleDto?>` | 1, 5, 7 | Existing test coverage for nullable DTO. Unwrap nullable, register ExampleDto. | Yes |
| 3 | Interface factory returns record `Task<ExampleRecordResult?>` | 3, 8 | Existing `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer`. RecordBypassConverterFactory claims it. No DTO registration. | Yes |
| 4 | Class factory returns its own type | 4, 9 | Class factory methods return types that are [Factory]-annotated. DiscoverDtoReturnTypes skips them. DI handles deserialization. | Yes |
| 5 | Static factory `[Execute]` returns `Task<bool>` | 1 (exclusion) | `bool` is a primitive (SpecialType check). DiscoverDtoReturnTypes skips it. | Yes |
| 6 | Generated code contains `DtoConstructorRegistry.Register` | 1, 10 | Inspect generated FactoryServiceRegistrar output for IExampleRepository. | Yes -- verified |
| 7 | No `Activator.CreateInstance` in resolver | 5, 6 | Source code inspection after change. | Yes -- verified |

### Gaps and Questions

None remaining.

### Implementation Concerns

All concerns from the review were addressed during implementation (see Implementation Progress).

### Ready to Proceed?

[x] Implementation complete.

## Implementation Contract

### Scope

**Files Created:**
- `src/RemoteFactory/Internal/DtoConstructorRegistry.cs` -- Static registry with `Register<T>(Func<object>)` and `TryCreate(Type, out Func<object>?)` (create)

**Files Modified:**
- `src/Generator/FactoryGenerator.Types.cs` -- Add `EquatableArray<string> DtoReturnTypes` to `MethodInfo` base record (both constructors); add `DiscoverDtoReturnTypes(ITypeSymbol)` static method; aggregate DtoReturnTypes in `TypeInfo` (modify)
- `src/Generator/Model/InterfaceFactoryModel.cs` -- Add `IReadOnlyList<string> DtoReturnTypes` property (modify)
- `src/Generator/Model/ClassFactoryModel.cs` -- Add `IReadOnlyList<string> DtoReturnTypes` property (modify)
- `src/Generator/Model/StaticFactoryModel.cs` -- Add `IReadOnlyList<string> DtoReturnTypes` property (modify)
- `src/Generator/Builder/FactoryModelBuilder.cs` -- Pass `typeInfo.DtoReturnTypes` to each model constructor (modify)
- `src/Generator/Renderer/InterfaceFactoryRenderer.cs` -- Emit `DtoConstructorRegistry.Register<T>()` calls in `RenderFactoryServiceRegistrar` (modify)
- `src/Generator/Renderer/ClassFactoryRenderer.cs` -- Emit `DtoConstructorRegistry.Register<T>()` calls in `RenderFactoryServiceRegistrar` (modify)
- `src/Generator/Renderer/StaticFactoryRenderer.cs` -- Emit `DtoConstructorRegistry.Register<T>()` calls in `RenderFactoryServiceRegistrar` (modify)
- `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs` -- Replace `Activator.CreateInstance` block with `DtoConstructorRegistry.TryCreate` call; remove unused `System.Reflection` import (modify)

### Out of Scope

- All existing test files -- MUST NOT be modified
- `src/Generator/Model/FactoryGenerationUnit.cs` -- NOT modified (data flows through models)
- Any documentation files (CLAUDE-DESIGN.md, skill docs, etc.) -- handled by documenter agent

### Tests to Add

No new test files needed. Verification is through:
1. All existing tests continue to pass (490 unit + 502 integration + 42 design = 1034 total per framework)
2. Inspection of generated code for `IExampleRepository` to confirm `DtoConstructorRegistry.Register<ExampleDto>` is emitted
3. Inspection of generated code to confirm `ExampleRecordResult` is NOT registered
4. Source code inspection of `NeatooJsonTypeInfoResolver` to confirm `Activator.CreateInstance` is removed

### Test Scenario Mapping

| # | Plan Scenario | Verification Method | Source |
|---|--------------|---------------------|--------|
| 1 | Interface factory returns `Task<IReadOnlyList<ExampleDto>>` | Existing `InterfaceFactory_GetAllAsync_ReturnsDataFromServer` | Design.Tests |
| 2 | Interface factory returns `Task<ExampleDto?>` | Existing DTO round-trip tests + generated code inspection | Design.Tests |
| 3 | Interface factory returns record | Existing `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` | Design.Tests |
| 4 | Class factory returns own type | Existing class factory tests + generated code inspection | IntegrationTests |
| 5 | Static factory returns `Task<bool>` | Existing static factory tests + generated code inspection | Design.Tests |
| 6 | Generated code contains Register | Inspect generated FactoryServiceRegistrar output | Generated code |
| 7 | No Activator.CreateInstance | Source inspection of NeatooJsonTypeInfoResolver.cs | Source code |

### Verification Gates

1. After Phase 1 (runtime infrastructure): Build succeeds, all tests pass -- PASSED
2. After Phase 2 (generator DTO discovery): Build succeeds -- PASSED
3. After Phase 3 (generator emission): Build succeeds, all tests pass, generated code includes `DtoConstructorRegistry.Register<ExampleDto>` -- PASSED
4. After Phase 4 (verification): All 1034+ tests pass per framework, generated code inspection complete -- PASSED

### Stop Conditions

None triggered.

## Implementation Progress

### Phase 1: Runtime Infrastructure -- COMPLETED
- Created `src/RemoteFactory/Internal/DtoConstructorRegistry.cs` with static `Register<T>` and `TryCreate` API
- Modified `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs`:
  - Replaced `Activator.CreateInstance` block with `DtoConstructorRegistry.TryCreate` lookup
  - Removed unused `System.Reflection`, `System.Collections.Generic`, `System.Linq`, `System.Text`, `System.Threading.Tasks` imports
- Build verified: 0 errors, 0 warnings

### Phase 2: Generator DTO Discovery -- COMPLETED
- Added `using System.Linq;` to `FactoryGenerator.Types.cs`
- Added `DiscoverDtoReturnTypes(ITypeSymbol)` private static method to `MethodInfo`:
  - Unwraps Task<T>, nullable annotations, Nullable<T>
  - Unwraps generic collections (IEnumerable<T> check on AllInterfaces), arrays
  - Filters: skip primitives (SpecialType), System namespace, abstract/interface, [Factory] types, types implementing [Factory] interfaces, types without public parameterless ctor
  - Returns `EquatableArray<string>` of fully-qualified display names
- Added `DtoReturnTypes` property to `MethodInfo` base record
- Initialized `DtoReturnTypes` in both `MethodInfo` constructors:
  - `BaseMethodDeclarationSyntax` path: `DiscoverDtoReturnTypes(methodSymbol.ReturnType)`
  - `RecordDeclarationSyntax` path: `DiscoverDtoReturnTypes(constructorSymbol.ContainingType)`
- Added `DtoReturnTypes` aggregation in `TypeInfo` constructor (HashSet for deduplication)
- Added `DtoReturnTypes` property declaration to `TypeInfo`
- Added `DtoReturnTypes` property to `InterfaceFactoryModel`, `ClassFactoryModel`, `StaticFactoryModel`
- Wired `typeInfo.DtoReturnTypes.ToList()` through `FactoryModelBuilder.BuildStaticFactory`, `BuildInterfaceFactory`, `BuildClassFactory`
- Generator build verified: 0 errors, 0 warnings

### Phase 3: Renderer Emission -- COMPLETED
- `InterfaceFactoryRenderer.RenderFactoryServiceRegistrar`: Emit DTO registration after auth type registrations, outside `if(remoteLocal)` blocks
- `ClassFactoryRenderer.RenderFactoryServiceRegistrar`: Emit DTO registration after auth type registrations, before ordinal converter registration
- `StaticFactoryRenderer.RenderFactoryServiceRegistrar`: Emit DTO registration after both `if(remoteLocal)` blocks
- Full solution build verified: 0 errors, 2 pre-existing Blazor WASM warnings

### Phase 4: Verification -- COMPLETED
- All tests pass (see Completion Evidence)
- Generated code verified (see Completion Evidence)
- Source inspection confirmed (see Completion Evidence)

### Post-Implementation: IL Trimmer Annotation -- COMPLETED
- Added `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]` to `T` type parameter on `Register<T>` method in `DtoConstructorRegistry.cs`
- Added `using System.Diagnostics.CodeAnalysis;` import
- This tells the IL trimmer to preserve constructors AND properties for any concrete type passed to `Register<T>`. Since the generator emits concrete calls like `Register<ExampleDto>(...)`, the trimmer traces the annotation to the specific type.
- Full solution build verified: 0 errors, 2 pre-existing Blazor WASM warnings

## Completion Evidence

### Test Results

**Unit Tests:**
- net9.0: 490 passed, 0 failed, 0 skipped
- net10.0: 490 passed, 0 failed, 0 skipped

**Integration Tests:**
- net9.0: 502 passed, 0 failed, 3 skipped (pre-existing performance test skips)
- net10.0: 502 passed, 0 failed, 3 skipped (pre-existing performance test skips)

**Design Tests:**
- net9.0: 42 passed, 0 failed, 0 skipped
- net10.0: 42 passed, 0 failed, 0 skipped

**Total: 2068 tests passed across both frameworks. Zero failures.**

### Generated Code Verification

**Scenario 6 -- DtoConstructorRegistry.Register emitted:**
File: `src/Design/Design.Domain/Generated/Neatoo.Generator/Neatoo.Factory/Design.Domain.FactoryPatterns.IExampleRepositoryFactory.g.cs`
Line 137: `DtoConstructorRegistry.Register<Design.Domain.FactoryPatterns.ExampleDto>(() => new Design.Domain.FactoryPatterns.ExampleDto());`
- Emitted OUTSIDE `if(remoteLocal == ...)` blocks -- correct
- Only `ExampleDto` registered across ALL Design project generated files -- correct

**ExampleRecordResult NOT registered:**
- `ExampleRecordResult` appears only in delegate/method signatures, NOT in DtoConstructorRegistry calls -- correct

**Class factory types NOT registered:**
- No DtoConstructorRegistry calls in any class factory generated files (Order, AuthorizedOrder, etc.) -- correct

**Static factory types NOT registered:**
- No DtoConstructorRegistry calls in ExampleCommands generated file -- correct (returns bool, a primitive)

### Source Code Verification

**Scenario 7 -- No Activator.CreateInstance:**
- `Activator.CreateInstance` is absent from `NeatooJsonTypeInfoResolver.cs` -- confirmed via grep
- `System.Reflection` import removed -- confirmed via grep
- `BindingFlags` usage removed -- confirmed via grep

### Contract Status

All acceptance criteria met:
- [x] `DtoConstructorRegistry` class exists in `src/RemoteFactory/Internal/`
- [x] `NeatooJsonTypeInfoResolver` uses `DtoConstructorRegistry.TryCreate` instead of `Activator.CreateInstance`
- [x] Generated `FactoryServiceRegistrar` for `IExampleRepository` includes `DtoConstructorRegistry.Register<ExampleDto>(() => new ExampleDto())`
- [x] Generated code does NOT register `ExampleRecordResult` (no parameterless constructor)
- [x] All existing tests pass (net9.0 and net10.0)
- [x] Design project `InterfaceFactoryTests.InterfaceFactory_GetAllAsync_ReturnsDataFromServer` passes
- [x] Design project `InterfaceFactoryTests.InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` passes
- [x] `Activator.CreateInstance` call is removed from `NeatooJsonTypeInfoResolver`
- [x] No new `System.Reflection` usage introduced (net improvement -- reflection removed)
