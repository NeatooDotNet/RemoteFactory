# Developer -- LazyLoad<T> Type Implementation Plan

Last updated: 2026-03-28
Current step: Phase 3 implementation complete, plan status set to Awaiting Verification

## Key Context

- Plan proposes porting Neatoo's `LazyLoad<T>` to RemoteFactory, stripping meta-state interfaces
- Three-phase agent approach: (1) Core types + named serialization, (2) Generator + ordinal, (3) Design project
- Two-slot ordinal encoding for `LazyLoad<T>` properties is a new pattern with no precedent
- Neatoo reference implementation examined at `C:/Users/KeithVoels/source/repos/neatoodotnet/Neatoo/src/Neatoo/LazyLoad.cs`
- **Record primary constructor + LazyLoad<T> is OUT OF SCOPE** (architect resolution): semantically nonsensical, self-evidently wrong at usage level, no diagnostic needed, acceptable failure mode (compile-time error in generated code)
- `BuildConstructorArgs` and `BuildConstructorArgsForFromArray` need NO changes -- the `IsLazyLoad` flag and two-slot rendering only affect `ToOrdinalArray`/`FromOrdinalArray` object-initializer paths and the `PropertyNames`/`PropertyTypes` arrays
- Generator targets netstandard2.0 with `#nullable enable` -- must use `string?` for nullable reference type fields

## Mistakes to Avoid

- When adding nullable fields (`string?`) to generator records in netstandard2.0 projects with `#nullable enable`, always use `?` annotation. The generator project has `<Nullable>enable</Nullable>` in the csproj.
- Do NOT use `string innerType = null` -- use `string? innerType = null` to avoid CS8625.
- Design.Tests project has `TreatWarningsAsErrors` -- `Assert.Contains(string, string?)` requires `StringComparison` parameter (CA1307).

## User Corrections

- **Record primary constructor + LazyLoad<T>**: Architect resolved as out of scope. No diagnostic. No changes to `BuildConstructorArgs`/`BuildConstructorArgsForFromArray`. Compile-time error in generated code is the acceptable failure mode.

## Developer Review

**Status:** Approved
**Date:** 2026-03-28

### Summary

The plan proposes extracting `LazyLoad<T>` from Neatoo into RemoteFactory with full serialization support (named + ordinal formats). The design is well-structured with 26 business rule assertions covering core behavior, factory creation, serialization in both formats, merge pattern, DI registration, and client/server round-trip. The overall approach is sound. All concerns have been resolved.

### Assertion Trace Verification

| # | Business Rule | Implementation Path | Expected Result | Verified? |
|---|--------------|---------------------|-----------------|-----------|
| BR-LL-001 | Parameterless ctor -> null/false/false/false | `LazyLoad()` ctor | All properties correct | Yes |
| BR-LL-002 | Loader ctor, no LoadAsync -> Value null (passive) | `LazyLoad(Func<Task<T?>> loader)` ctor | Value == null, no side effect | Yes |
| BR-LL-003 | LoadAsync with loader -> invokes loader, sets Value/IsLoaded | `LoadAsync()` | Value == loaded value, IsLoaded == true | Yes |
| BR-LL-004 | Concurrent LoadAsync -> single load task | `LoadAsync()` -- `lock (_loadLock)` | Only one invocation | Yes |
| BR-LL-005 | LoadAsync with no loader -> InvalidOperationException | `LoadAsync()` -- null check | Exception thrown | Yes |
| BR-LL-006 | Loader throws -> HasLoadError=true, LoadError=message | `LoadAsyncCore()` -- catch block | HasLoadError==true | Yes |
| BR-LL-007 | SetValue -> sets Value, IsLoaded=true, clears errors, fires INPC | `SetValue(T? value)` | All properties and events correct | Yes |
| BR-LL-008 | Pre-loaded ctor -> Value=value, IsLoaded=true | `LazyLoad(T? value)` ctor | Value==value, IsLoaded==true | Yes |
| BR-LL-009 | Value changes -> PropertyChanged fires for "Value" | `LoadAsyncCore()` / `SetValue()` | Event received | Yes |
| BR-LL-010 | Inner value INPC forwarding | `SubscribeToValuePropertyChanged()` | Events forwarded | Yes |
| BR-LL-011 | ILazyLoadFactory.Create(loader) -> IsLoaded=false | `LazyLoadFactory.Create<TChild>(loader)` | IsLoaded==false | Yes |
| BR-LL-012 | ILazyLoadFactory.Create(value) -> Value=value, IsLoaded=true | `LazyLoadFactory.Create<TChild>(value)` | Value==value, IsLoaded==true | Yes |
| BR-LL-013 | Loaded LazyLoad serialized named -> {"value": V, "isLoaded": true} | `LazyLoadJsonConverter<T>.Write()` | JSON correct | Yes |
| BR-LL-014 | Unloaded LazyLoad serialized named -> {"value": null, "isLoaded": false} | `LazyLoadJsonConverter<T>.Write()` | JSON correct | Yes |
| BR-LL-015 | Named JSON deserialized loaded | `LazyLoadJsonConverter<T>.Read()` | Correct reconstruction | Yes |
| BR-LL-016 | Named JSON deserialized unloaded | `LazyLoadJsonConverter<T>.Read()` | Parameterless ctor | Yes |
| BR-LL-017 | Ordinal format two-slot encoding | `OrdinalRenderer` two-slot write in generated code | Two slots in array | Yes |
| BR-LL-018 | Ordinal two-slot [value, true] -> loaded LazyLoad | `OrdinalRenderer` two-slot read in generated code | Value + IsLoaded=true | Yes |
| BR-LL-019 | Ordinal two-slot [null, false] -> unloaded LazyLoad | `OrdinalRenderer` two-slot read in generated code | Value=null, IsLoaded=false | Yes |
| BR-LL-020 | PropertyNames contains "Lines" and "Lines__IsLoaded" | Generated `PropertyNames` array | Two entries | Yes |
| BR-LL-021 | PropertyTypes contains typeof(T) and typeof(bool) | Generated `PropertyTypes` array | Two entries | Yes |
| BR-LL-022 | ApplyDeserializedState(value, true) preserves loader | `ILazyLoadDeserializable.ApplyDeserializedState()` | Loader preserved | Yes |
| BR-LL-023 | ApplyDeserializedState(null, false) leaves instance unchanged | `ILazyLoadDeserializable.ApplyDeserializedState()` | Instance unchanged | Yes |
| BR-LL-024 | AddNeatooRemoteFactory registers ILazyLoadFactory | `AddRemoteFactoryServices.cs` | Resolvable from DI | Yes |
| BR-LL-025 | Client-server loaded round-trip | ClientServerContainers + ordinal serialization | Value preserved | Yes |
| BR-LL-026 | Client-server unloaded round-trip | ClientServerContainers + ordinal serialization | State preserved | Yes |

---

## Implementation Contract

### Phase 1: Core Types + Named Serialization -- COMPLETE (prior agent)

(See Phase 1 contract details in prior version of this file.)

### Phase 2: Generator Changes + Ordinal Serialization -- COMPLETE (prior agent)

(See Phase 2 contract details in prior version of this file.)

### Phase 3: Design Project + Documentation

#### Scope (Create)
- `src/Design/Design.Domain/FactoryPatterns/LazyLoadExample.cs` -- `[Factory]` class with `LazyLoad<string>` property, constructor-initialization pattern, three factory methods (Create, Fetch, FetchWithReviews), supporting service interface and mock implementation
- `src/Design/Design.Tests/FactoryTests/LazyLoadTests.cs` -- 6 tests covering local mode create, LoadAsync, client-server round-trip (loaded and unloaded), SetValue

#### Scope (Modify)
- `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs` -- Added `IProductReviewService` / `InMemoryProductReviewService` registration in server and local containers
- `src/Design/Design.Tests/FactoryTests/SerializationTests.cs` -- Updated serialization guide comment: added `LazyLoad<T>` to YES list with two-slot ordinal note; updated NO list to clarify `Lazy<T>` (BCL) vs `LazyLoad<T>`
- `src/Design/CLAUDE-DESIGN.md` -- Added `LazyLoad<T>` to Quick Decisions Table (2 rows), Design Completeness Checklist, and Design Files to Consult table (2 entries)

#### Out of Scope
- All existing Design tests -- not modified (sacred)
- All unit tests and integration tests -- not modified
- Source generator code -- Phase 2, complete
- Core library code -- Phase 1, complete

#### Verification Gates
1. After Design.Domain example: `dotnet build src/Design/Design.Domain/Design.Domain.csproj` -- PASSED
2. After Design tests: `dotnet test src/Design/Design.Tests/Design.Tests.csproj` -- PASSED (48/48 on both TFMs)
3. Full solution: `dotnet test src/Neatoo.RemoteFactory.sln` -- PASSED (all TFMs, all projects)

#### Stop Conditions
- No existing Design tests failed (0 regressions)
- No existing unit or integration tests failed (0 regressions)

### Test Scenario Mapping

| # | Plan Scenario | Test Method | File | Phase |
|---|--------------|-------------|------|-------|
| TS-LL-001 | Parameterless ctor defaults | ParameterlessConstructor_AllDefaultValues | LazyLoadCoreTests.cs | 1 |
| TS-LL-002 | Loader ctor passive value | LoaderConstructor_ValueIsNull_WithoutLoad | LazyLoadCoreTests.cs | 1 |
| TS-LL-003 | LoadAsync sets value | LoadAsync_SetsValueAndIsLoaded | LazyLoadCoreTests.cs | 1 |
| TS-LL-004 | Concurrent LoadAsync | ConcurrentLoadAsync_SingleInvocation | LazyLoadCoreTests.cs | 1 |
| TS-LL-005 | No loader throws | LoadAsync_NoLoader_ThrowsInvalidOperationException | LazyLoadCoreTests.cs | 1 |
| TS-LL-006 | Loader error | LoadAsync_LoaderThrows_SetsHasLoadError | LazyLoadCoreTests.cs | 1 |
| TS-LL-007 | SetValue behavior | SetValue_SetsValueAndClearsErrors_FiresPropertyChanged | LazyLoadCoreTests.cs | 1 |
| TS-LL-008 | Pre-loaded ctor | PreLoadedConstructor_ValueAndIsLoaded | LazyLoadCoreTests.cs | 1 |
| TS-LL-009 | PropertyChanged on load | LoadAsync_FiresPropertyChanged | LazyLoadCoreTests.cs | 1 |
| TS-LL-010 | INPC forwarding | InnerValue_PropertyChanged_Forwarded | LazyLoadCoreTests.cs | 1 |
| TS-LL-011 | Factory create with loader | Factory_CreateWithLoader_IsLoadedFalse | LazyLoadDiTests.cs | 1 |
| TS-LL-012 | Factory create with value | Factory_CreateWithValue_IsLoadedTrue | LazyLoadDiTests.cs | 1 |
| TS-LL-013 | Named format loaded round-trip | NamedFormat_Loaded_RoundTrip | LazyLoadNamedSerializationTests.cs | 1 |
| TS-LL-014 | Named format unloaded round-trip | NamedFormat_Unloaded_RoundTrip | LazyLoadNamedSerializationTests.cs | 1 |
| TS-LL-015 | Ordinal format loaded round-trip | OrdinalFormat_Loaded_RoundTrip | LazyLoadOrdinalTests.cs | 2 |
| TS-LL-016 | Ordinal format unloaded round-trip | OrdinalFormat_Unloaded_RoundTrip | LazyLoadOrdinalTests.cs | 2 |
| TS-LL-017 | PropertyNames/PropertyTypes arrays | OrdinalMetadata_PropertyNamesAndTypes | LazyLoadOrdinalTests.cs | 2 |
| TS-LL-018 | Merge preserves loader (loaded) | ApplyDeserializedState_Loaded_PreservesLoader | LazyLoadMergeTests.cs | 1 |
| TS-LL-019 | Merge preserves loader (unloaded) | ApplyDeserializedState_Unloaded_PreservesLoader | LazyLoadMergeTests.cs | 1 |
| TS-LL-020 | DI registration | AddNeatooRemoteFactory_RegistersILazyLoadFactory | LazyLoadDiTests.cs | 1 |
| TS-LL-021 | Client-server loaded round-trip | ClientServer_Loaded_RoundTrip | LazyLoadRoundTripTests.cs | 2 |
| TS-LL-022 | Client-server unloaded round-trip | ClientServer_Unloaded_RoundTrip | LazyLoadRoundTripTests.cs | 2 |
| DS-LL-001 | Design: Local create, unloaded | Create_InLocalMode_LazyPropertyIsUnloaded | LazyLoadTests.cs | 3 |
| DS-LL-002 | Design: Create then LoadAsync | Create_ThenLoadAsync_LoadsValue | LazyLoadTests.cs | 3 |
| DS-LL-003 | Design: Unloaded round-trip | Fetch_UnloadedLazyProperty_SurvivesClientServerRoundTrip | LazyLoadTests.cs | 3 |
| DS-LL-004 | Design: Loaded round-trip | FetchWithReviews_LoadedLazyProperty_SurvivesClientServerRoundTrip | LazyLoadTests.cs | 3 |
| DS-LL-005 | Design: FetchWithReviews not included | FetchWithReviews_NotIncluded_LazyPropertyIsUnloaded | LazyLoadTests.cs | 3 |
| DS-LL-006 | Design: SetValue direct set | SetValue_DirectlyLoadsLazyProperty | LazyLoadTests.cs | 3 |

---

## Implementation Progress

### Phase 1 -- COMPLETE (2026-03-28, prior agent)

All milestones achieved. 17 unit tests passing. All existing tests passing.

### Phase 2 -- COMPLETE (2026-03-28, prior agent)

All milestones achieved. 6 ordinal unit tests + 4 integration round-trip tests passing. All existing tests passing.

### Phase 3 -- COMPLETE (2026-03-28)

All milestones achieved in order:

1. **Created Design Domain example** -- `src/Design/Design.Domain/FactoryPatterns/LazyLoadExample.cs`
   - `[Factory] internal partial class ProductWithReviews : IProductWithReviews`
   - Public interface `IProductWithReviews` with `LazyLoad<string> Reviews` property
   - Three factory methods: Create, Fetch, FetchWithReviews (demonstrating both deferred and eager loading)
   - Constructor-initialization pattern using `ILazyLoadFactory` and `IProductReviewService`
   - Extensive comments explaining the pattern, DID NOT DO THIS sections, COMMON MISTAKE examples
   - Supporting types: `IProductReviewService`, `InMemoryProductReviewService`

2. **Verification Gate 1: PASSED** -- `dotnet build src/Design/Design.Domain/Design.Domain.csproj` (0 errors, both TFMs)

3. **Verified generated code** -- `ProductWithReviews.Ordinal.g.cs`:
   - PropertyNames: `["Id", "Name", "Price", "Reviews", "Reviews__IsLoaded"]` -- correct
   - PropertyTypes: `[typeof(int), typeof(string), typeof(decimal), typeof(string), typeof(bool)]` -- correct
   - Read/Write: two-slot LazyLoad encoding for slots 3/4 -- correct
   - ToOrdinalArray/FromOrdinalArray: correct two-slot encoding/reconstruction

4. **Registered IProductReviewService** -- `DesignClientServerContainers.cs`
   - Added `IProductReviewService`/`InMemoryProductReviewService` to server and local containers

5. **Created Design tests** -- `src/Design/Design.Tests/FactoryTests/LazyLoadTests.cs` (6 tests)
   - `Create_InLocalMode_LazyPropertyIsUnloaded` -- local create, verify unloaded state
   - `Create_ThenLoadAsync_LoadsValue` -- local create, explicit load, verify loaded state
   - `Fetch_UnloadedLazyProperty_SurvivesClientServerRoundTrip` -- client-server round-trip, unloaded
   - `FetchWithReviews_LoadedLazyProperty_SurvivesClientServerRoundTrip` -- client-server round-trip, loaded
   - `FetchWithReviews_NotIncluded_LazyPropertyIsUnloaded` -- client-server, includeReviews=false
   - `SetValue_DirectlyLoadsLazyProperty` -- local, direct SetValue

6. **Verification Gate 2: PASSED** -- `dotnet test src/Design/Design.Tests/Design.Tests.csproj`
   - net9.0: 48 passed, 0 failed
   - net10.0: 48 passed, 0 failed
   - (42 existing + 6 new LazyLoad tests)

7. **Updated SerializationTests.cs comments** -- line 30 and 36
   - YES list: Added `LazyLoad<T>` with two-slot ordinal encoding note
   - NO list: Changed `Lazy<T> or other deferred types` to `Lazy<T> (BCL) -- use LazyLoad<T> instead...`

8. **Updated CLAUDE-DESIGN.md**
   - Quick Decisions Table: 2 new rows (LazyLoad<T> usage, BCL Lazy<T> prohibition)
   - Design Completeness Checklist: Added `[x] LazyLoad<T> property with constructor-initialization pattern`
   - Design Files to Consult: 2 new entries (LazyLoadExample.cs, LazyLoadTests.cs)

9. **Verification Gate 3: PASSED** -- Full solution test
   - UnitTests net9.0: 517 passed, 0 failed
   - UnitTests net10.0: 517 passed, 0 failed
   - IntegrationTests net9.0: 506 passed, 3 skipped (pre-existing perf benchmarks), 0 failed
   - IntegrationTests net10.0: 506 passed, 3 skipped (pre-existing perf benchmarks), 0 failed
   - Design.Tests net9.0: 48 passed, 0 failed
   - Design.Tests net10.0: 48 passed, 0 failed

No existing tests were modified. No stop conditions were triggered.

## Completion Evidence

### Test Results (2026-03-28)

```
UnitTests net9.0:        Passed: 517, Failed: 0, Skipped: 0
UnitTests net10.0:       Passed: 517, Failed: 0, Skipped: 0
IntegrationTests net9.0: Passed: 506, Failed: 0, Skipped: 3
IntegrationTests net10.0:Passed: 506, Failed: 0, Skipped: 3
Design.Tests net9.0:     Passed: 48, Failed: 0
Design.Tests net10.0:    Passed: 48, Failed: 0
```

Test count changes from Phase 2 baseline:
- Unit tests: 517 -> 517 (unchanged)
- Integration tests: 506 -> 506 (unchanged)
- Design tests: 42 -> 48 (+6 new LazyLoad tests)

### Contract Status

| Contract Item | Status |
|--------------|--------|
| Design Domain example with `LazyLoad<T>` property | Done |
| Constructor-initialization pattern demonstrated | Done |
| `[Fetch]` method that does NOT load lazy property | Done |
| `[Fetch]` method that pre-loads lazy property | Done |
| Comments explaining pattern, COMMON MISTAKE, DID NOT DO THIS | Done |
| `IProductReviewService` registered in containers | Done |
| 6 Design tests created | Done |
| Local mode: create, verify unloaded | Done, passing |
| Local mode: create, LoadAsync, verify loaded | Done, passing |
| Client-server: unloaded round-trip | Done, passing |
| Client-server: loaded round-trip | Done, passing |
| Client-server: FetchWithReviews not included | Done, passing |
| Local mode: SetValue direct set | Done, passing |
| SerializationTests.cs YES list updated | Done |
| SerializationTests.cs NO list updated | Done |
| CLAUDE-DESIGN.md Quick Decisions Table updated | Done |
| CLAUDE-DESIGN.md Design Completeness Checklist updated | Done |
| CLAUDE-DESIGN.md Design Files to Consult updated | Done |
| Generated code verified (two-slot format) | Done |
| Existing tests unmodified | Confirmed |
| No out-of-scope test failures | Confirmed |
| No reflection used | Confirmed |
| No stop conditions triggered | Confirmed |
| Plan status set to "Awaiting Verification" | Done |

### Files Created (Phase 3)
- `src/Design/Design.Domain/FactoryPatterns/LazyLoadExample.cs`
- `src/Design/Design.Tests/FactoryTests/LazyLoadTests.cs`

### Files Modified (Phase 3)
- `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs` -- Added IProductReviewService registration
- `src/Design/Design.Tests/FactoryTests/SerializationTests.cs` -- Updated YES/NO lists in serialization guide comment
- `src/Design/CLAUDE-DESIGN.md` -- Added LazyLoad to Quick Decisions, Completeness Checklist, and Files to Consult

### Files Created (Phase 2, prior agent)
- `src/Tests/RemoteFactory.UnitTests/TestTargets/LazyLoad/LazyLoadTargets.cs`
- `src/Tests/RemoteFactory.UnitTests/LazyLoad/LazyLoadOrdinalTests.cs`
- `src/Tests/RemoteFactory.IntegrationTests/TestTargets/LazyLoad/LazyLoadTargets.cs`
- `src/Tests/RemoteFactory.IntegrationTests/LazyLoad/LazyLoadRoundTripTests.cs`

### Files Modified (Phase 2, prior agent)
- `src/Generator/Model/OrdinalSerializationModel.cs` -- Added `IsLazyLoad`, `InnerType` to `OrdinalPropertyModel`
- `src/Generator/FactoryGenerator.Types.cs` -- Added `IsLazyLoad`, `InnerType` to `OrdinalPropertyInfo`; LazyLoad detection in `CollectPropertiesRecursive`
- `src/Generator/Builder/FactoryModelBuilder.cs` -- Mapping `IsLazyLoad`/`InnerType` in `BuildOrdinalSerializationModel`
- `src/Generator/Renderer/OrdinalRenderer.cs` -- Two-slot rendering in 5 methods + 2 new helper methods

### Files Created (Phase 1, prior agent)
- `src/RemoteFactory/Internal/ILazyLoadDeserializable.cs`
- `src/RemoteFactory/LazyLoad.cs`
- `src/RemoteFactory/ILazyLoadFactory.cs`
- `src/RemoteFactory/Internal/LazyLoadJsonConverterFactory.cs`
- `src/Tests/RemoteFactory.UnitTests/LazyLoad/LazyLoadCoreTests.cs`
- `src/Tests/RemoteFactory.UnitTests/LazyLoad/LazyLoadNamedSerializationTests.cs`
- `src/Tests/RemoteFactory.UnitTests/LazyLoad/LazyLoadMergeTests.cs`
- `src/Tests/RemoteFactory.UnitTests/LazyLoad/LazyLoadDiTests.cs`

### Files Modified (Phase 1, prior agent)
- `src/RemoteFactory/AddRemoteFactoryServices.cs` -- Added `ILazyLoadFactory` singleton registration
- `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` -- Added `LazyLoadJsonConverterFactory` to converter chain
