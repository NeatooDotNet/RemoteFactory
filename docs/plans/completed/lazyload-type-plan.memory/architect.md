# Architect -- LazyLoad<T> Type Implementation

Last updated: 2026-03-28
Current step: Step 7A Architect Verification -- VERIFIED

## Key Context

### Codebase Files Examined

**Generator pipeline (ordinal serialization):**
- `src/Generator/FactoryGenerator.Types.cs` (lines 330-428) -- `CollectOrdinalProperties`, `CollectPropertiesRecursive`, `OrdinalPropertyInfo` record. Property collection filters for public getter + setter. `LazyLoad<T>` property on a `[Factory]` class with `{ get; set; }` WILL be collected as type `LazyLoad<T>`.
- `src/Generator/Model/OrdinalSerializationModel.cs` -- `OrdinalSerializationModel` and `OrdinalPropertyModel` records. Model currently has `Name`, `Type`, `IsNullable`. Needs `IsLazyLoad` and `InnerType` fields.
- `src/Generator/Builder/FactoryModelBuilder.cs` (lines 965-990) -- `BuildOrdinalSerializationModel` maps `OrdinalPropertyInfo` to `OrdinalPropertyModel`. This is where `LazyLoad<T>` detection and two-slot expansion happens.
- `src/Generator/Renderer/OrdinalRenderer.cs` -- Full read. Renders `PropertyNames`, `PropertyTypes`, `ToOrdinalArray`, `FromOrdinalArray`, and the converter class. All rendering loops iterate `model.Properties` uniformly. Must be extended with `IsLazyLoad` branching.

**Serialization pipeline:**
- `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` (full read) -- Constructor builds converter chain: (1) NeatooOrdinalConverterFactory if ordinal, (2) NeatooJsonConverterFactory instances from DI, (3) RecordBypassConverterFactory. `LazyLoadJsonConverterFactory` goes AFTER DI converters but BEFORE RecordBypassConverterFactory.
- `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs` (full read) -- Claims `IOrdinalSerializable` types. `LazyLoad<T>` does NOT implement this interface, so no conflict.
- `src/RemoteFactory/Internal/RecordBypassConverterFactory.cs` (full read) -- Claims types with no parameterless constructor. `LazyLoad<T>` HAS a parameterless constructor, so `CanConvert` returns false. No conflict.
- `src/RemoteFactory/Internal/NeatooInterfaceJsonConverterFactory.cs` (full read) -- Claims non-generic interfaces/abstract types. `LazyLoad<T>` is a concrete generic class. No conflict. Inner type T may be an interface, which is correctly handled via delegation through the full options chain.
- `src/RemoteFactory/IOrdinalSerializable.cs` (full read) -- Interfaces: `IOrdinalSerializable`, `IOrdinalSerializationMetadata`, `IOrdinalConverterProvider<TSelf>`.

**DI registration:**
- `src/RemoteFactory/AddRemoteFactoryServices.cs` (full read) -- `AddNeatooRemoteFactory()` is the central registration method. `ILazyLoadFactory`/`LazyLoadFactory` should be registered here as a singleton (framework-level, not per-factory). Line 125: `services.RegisterFactories()` iterates assembly-level `NeatooFactoryRegistrarAttribute` to discover and register per-factory services.
- `src/Generator/Renderer/ClassFactoryRenderer.cs` (lines 1537-1544) -- Generated `FactoryServiceRegistrar` registers ordinal converters per factory type. This is where `RegisterConverter` calls happen for generated ordinal converters.

**Reference implementation (Neatoo):**
- `neatoodotnet/Neatoo/src/Neatoo/LazyLoad.cs` (full read) -- Source of truth for the port. Lines 1-17: `ILazyLoadDeserializable` interface. Lines 37-353: `LazyLoad<T>` class with `IValidateMetaProperties` and `IEntityMetaProperties` regions (lines 270-352) that will be stripped.
- `neatoodotnet/Neatoo/src/Neatoo/ILazyLoadFactory.cs` (full read) -- Direct port target.

**Design project:**
- `src/Design/Design.Domain/Aggregates/` -- Order.cs, AuthorizedOrder.cs, SecureOrder.cs, AuthorizedOrderAuth.cs
- `src/Design/Design.Domain/FactoryPatterns/` -- AllPatterns.cs, ClassFactoryWithExecute.cs
- `src/Design/Design.Tests/FactoryTests/SerializationTests.cs` (full read) -- Serialization guide comment and existing tests. Line 36: "Lazy<T> or other deferred types" in the NO list.
- `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs` (full read) -- Test container setup pattern.

**Integration test containers:**
- `src/Tests/RemoteFactory.IntegrationTests/TestContainers/ClientServerContainers.cs` (first 80 lines) -- Same pattern as Design containers.

### Architecture Validation

1. **Two-slot encoding approach**: The plan proposes expanding `LazyLoad<T>` properties into two ordinal slots. After examining the full rendering pipeline (`OrdinalPropertyModel` -> `OrdinalRenderer`), the cleanest approach is to add `IsLazyLoad` and `InnerType` to `OrdinalPropertyModel` and branch in the renderer, rather than expanding to two separate entries. This keeps the property list 1:1 with actual properties and avoids breaking constructor parameter matching. The renderer will emit two array reads/writes when it encounters `IsLazyLoad == true`.

2. **PropertyNames/PropertyTypes expansion**: These arrays WILL have more entries than the property count when `LazyLoad<T>` properties exist. The generated `PropertyNames` and `PropertyTypes` are static metadata arrays -- they can have synthetic entries. The key invariant is that the ordinal array length matches `PropertyTypes.Length`, not `Properties.Count`. This needs careful implementation but is architecturally sound.

3. **Converter chain placement**: `LazyLoadJsonConverterFactory` should be added in the `NeatooJsonSerializer` constructor at line ~93, after the DI-resolved converters loop but before `RecordBypassConverterFactory`. It does not need DI services and should not be registered as a `NeatooJsonConverterFactory` subclass.

4. **`ILazyLoadFactory` registration**: Singleton in `AddNeatooRemoteFactory()` at `AddRemoteFactoryServices.cs`, alongside other framework-level registrations like `IServiceAssemblies`, `ICorrelationContext`, etc. This is not per-factory generated code.

5. **Merge pattern scope**: Named-format converter uses `ILazyLoadDeserializable.ApplyDeserializedState()` for merge. Ordinal-format generated code uses reconstruction (`new LazyLoad<T>(value)` or `new LazyLoad<T>()`). This distinction is correct because ordinal deserialization creates new parent instances, so the constructor-initialization pattern re-creates the loader from scratch. The merge pattern is only needed when the converter encounters an existing instance (which happens in the named-format converter's populate path, not in the ordinal converter which always creates new instances).

6. **No breaking changes**: `LazyLoad<T>` is entirely additive. No existing generated code is affected. The generator changes only activate when a `LazyLoad<T>` property is detected. Existing `[Factory]` classes without `LazyLoad<T>` properties produce identical output.

### Pre-Handoff Verification

**Scope table:**

| Component | Files Affected | Change Type |
|-----------|---------------|-------------|
| Core library | `src/RemoteFactory/LazyLoad.cs` (NEW) | New file |
| Core library | `src/RemoteFactory/Internal/ILazyLoadDeserializable.cs` (NEW) | New file |
| Core library | `src/RemoteFactory/ILazyLoadFactory.cs` (NEW) | New file |
| Core library | `src/RemoteFactory/Internal/LazyLoadJsonConverterFactory.cs` (NEW) | New file |
| Core library | `src/RemoteFactory/AddRemoteFactoryServices.cs` | Add singleton registration |
| Core library | `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` | Add converter to chain |
| Generator | `src/Generator/Model/OrdinalSerializationModel.cs` | Add IsLazyLoad, InnerType |
| Generator | `src/Generator/FactoryGenerator.Types.cs` | Detect LazyLoad<T> in property collection |
| Generator | `src/Generator/Builder/FactoryModelBuilder.cs` | Map LazyLoad<T> properties |
| Generator | `src/Generator/Renderer/OrdinalRenderer.cs` | Two-slot rendering |
| Tests | Multiple new test files | New tests |
| Design | `src/Design/Design.Domain/` (new example file) | New file |
| Design | `src/Design/Design.Tests/FactoryTests/` (new test file) | New file |
| Design | `src/Design/Design.Tests/FactoryTests/SerializationTests.cs` | Update comment |

**Breaking changes:** None. Entirely additive.

**Verification resources used:** Built the solution (`dotnet build src/Neatoo.RemoteFactory.sln`) -- 0 errors, 3 warnings (WASM warnings in OrderEntry example, unrelated).

## Mistakes to Avoid

- Do not add `LazyLoadJsonConverterFactory` as a `NeatooJsonConverterFactory` subclass. It does not need DI and should be a standalone factory added directly to `Options.Converters`.
- Do not try to make `LazyLoad<T>` implement `IOrdinalSerializable`. It is not a `[Factory]` class. The ordinal support is for `LazyLoad<T>` properties ON `[Factory]` classes, handled by the generated code.
- Do not expand `LazyLoad<T>` properties into two separate `OrdinalPropertyModel` entries. Keep one entry with `IsLazyLoad` flag, and emit two slots in the renderer.
- Do not add support or diagnostics for `LazyLoad<T>` as a record primary constructor parameter. It is out of scope (see Clarification Loop Resolution below).

## User Corrections

None yet -- first run.

## Clarification Loop Resolution (Step 5)

### Developer Concern: `LazyLoad<T>` as record primary constructor parameter

**Question:** Is `LazyLoad<T>` as a record primary constructor parameter in scope?

**Resolution: OUT OF SCOPE -- no diagnostic needed.**

**Rationale:**

1. **Semantically nonsensical.** `LazyLoad<T>` is designed around the constructor-initialization pattern: the owning class creates the `LazyLoad<T>` in its constructor body with a loader delegate that captures injected services. A record primary constructor parameter (`record MyRecord(LazyLoad<T> Lines)`) would require the factory *caller* to pass in a fully constructed `LazyLoad<T>`, which defeats the purpose -- the caller doesn't have the loader delegate. That's a server-side concern.

2. **Self-evidently wrong at usage level.** The generated factory interface from `[Create]` on such a record would expose `LazyLoad<OrderLineList>` as a parameter to the consumer. No user would naturally do this, and if they did, the API would be obviously wrong before any generator issue surfaces.

3. **No diagnostic warranted.** Detecting `LazyLoad<T>` in record primary constructor parameters requires touching the `[Create]` processing path in `FactoryGenerator.Types.cs` (lines 176-204), which is separate from the ordinal property serialization path. The complexity is not justified for a pattern that is naturally prevented by its own semantics.

4. **Generator impact if not handled.** If someone writes this anyway, the `BuildConstructorArgs` and `BuildConstructorArgsForFromArray` methods in `OrdinalRenderer.cs` (lines 291-348) would produce incorrect code: they map `ConstructorParameterNames` to `OrdinalPropertyModel` entries by name, but the two-slot expansion means the property index no longer corresponds to the ordinal slot index. The generated constructor call would pass wrong values. This would be a compile-time error in the generated code (type mismatch), which is an acceptable failure mode for an unsupported pattern.

**Developer action:** Skip `LazyLoad<T>` record primary constructor parameter handling entirely. The `BuildConstructorArgs` methods need no changes. The `IsLazyLoad` flag and two-slot rendering only affect the `ToOrdinalArray`/`FromOrdinalArray` object-initializer paths and the `PropertyNames`/`PropertyTypes` arrays. If a record primary constructor has a `LazyLoad<T>` parameter, the generated code will produce a compile error -- which is acceptable for an unsupported, semantically invalid pattern.

## Architectural Verification (Pre-Handoff)

**Verdict: APPROVED**

The plan is architecturally sound. Key findings:

1. The design correctly identifies all affected components and the changes needed in each.
2. The two-slot ordinal encoding is feasible: `OrdinalPropertyModel` needs `IsLazyLoad`/`InnerType`, and `OrdinalRenderer` branches on the flag. The generated `PropertyNames`/`PropertyTypes` arrays grow by one entry per `LazyLoad<T>` property.
3. The converter chain placement is correct: add after DI converters, before `RecordBypassConverterFactory`.
4. The `ILazyLoadFactory` DI registration belongs in `AddRemoteFactoryServices.cs`, not in generated code.
5. No breaking changes to existing functionality.
6. All 26 business rules and 22 test scenarios trace cleanly to implementation paths.
7. The agent phasing is logical: Phase 1 (core types + named serialization) is self-contained, Phase 2 (generator changes) builds on Phase 1, Phase 3 (design project) builds on both.

## Architect Verification (Post-Implementation)

**Verdict: VERIFIED**

### Independent Build Results

All solutions build with 0 errors:
- `dotnet build src/Neatoo.RemoteFactory.sln` -- Build succeeded (3 warnings: WASM-related in OrderEntry example, pre-existing)
- `dotnet build src/Design/Design.sln` -- Build succeeded (0 warnings)

### Independent Test Results

All tests pass with 0 failures:
- **Unit Tests (net9.0 + net10.0):** 517 passed, 0 failed
- **Integration Tests (net9.0 + net10.0):** 506 passed, 3 skipped, 0 failed (x2 framework runs)
- **Design Tests (net9.0 + net10.0):** 48 passed, 0 failed

These match the developer's reported numbers exactly.

### Test Scenario Coverage

Every test scenario from the plan (TS-LL-001 through TS-LL-022) has a corresponding passing test:

| Scenario | Test Method | File |
|----------|-----------|------|
| TS-LL-001 | `ParameterlessConstructor_AllDefaultValues` | LazyLoadCoreTests.cs |
| TS-LL-002 | `LoaderConstructor_ValueIsNull_WithoutLoad` | LazyLoadCoreTests.cs |
| TS-LL-003 | `LoadAsync_SetsValueAndIsLoaded` | LazyLoadCoreTests.cs |
| TS-LL-004 | `ConcurrentLoadAsync_SingleInvocation` | LazyLoadCoreTests.cs |
| TS-LL-005 | `LoadAsync_NoLoader_ThrowsInvalidOperationException` | LazyLoadCoreTests.cs |
| TS-LL-006 | `LoadAsync_LoaderThrows_SetsHasLoadError` | LazyLoadCoreTests.cs |
| TS-LL-007 | `SetValue_SetsValueAndClearsErrors_FiresPropertyChanged` | LazyLoadCoreTests.cs |
| TS-LL-008 | `PreLoadedConstructor_ValueAndIsLoaded` | LazyLoadCoreTests.cs |
| TS-LL-009 | `LoadAsync_FiresPropertyChanged` | LazyLoadCoreTests.cs |
| TS-LL-010 | `InnerValue_PropertyChanged_Forwarded` | LazyLoadCoreTests.cs |
| TS-LL-011 | `Factory_CreateWithLoader_IsLoadedFalse` | LazyLoadDiTests.cs |
| TS-LL-012 | `Factory_CreateWithValue_IsLoadedTrue` | LazyLoadDiTests.cs |
| TS-LL-013 | `NamedFormat_Loaded_RoundTrip` | LazyLoadNamedSerializationTests.cs |
| TS-LL-014 | `NamedFormat_Unloaded_RoundTrip` | LazyLoadNamedSerializationTests.cs |
| TS-LL-015 | `OrdinalFormat_Loaded_RoundTrip` | LazyLoadOrdinalTests.cs |
| TS-LL-016 | `OrdinalFormat_Unloaded_RoundTrip` | LazyLoadOrdinalTests.cs |
| TS-LL-017 | `OrdinalMetadata_PropertyNamesAndTypes` | LazyLoadOrdinalTests.cs |
| TS-LL-018 | `ApplyDeserializedState_Loaded_PreservesLoader` | LazyLoadMergeTests.cs |
| TS-LL-019 | `ApplyDeserializedState_Unloaded_PreservesLoader` | LazyLoadMergeTests.cs |
| TS-LL-020 | `AddNeatooRemoteFactory_RegistersILazyLoadFactory` | LazyLoadDiTests.cs |
| TS-LL-021 | `ClientServer_Loaded_RoundTrip` | LazyLoadRoundTripTests.cs |
| TS-LL-022 | `ClientServer_Unloaded_RoundTrip` | LazyLoadRoundTripTests.cs |

### Design Match Verification

1. **LazyLoad<T> has NO Neatoo-specific interfaces** -- Confirmed. The class implements only `INotifyPropertyChanged` and `ILazyLoadDeserializable`. No `IValidateMetaProperties`, no `IEntityMetaProperties`.

2. **ILazyLoadDeserializable is internal** -- Confirmed. Declared as `internal interface ILazyLoadDeserializable` in `src/RemoteFactory/Internal/ILazyLoadDeserializable.cs`.

3. **Named format** -- Confirmed. `LazyLoadJsonConverter<T>` writes `{"value": ..., "isLoaded": bool}` with lowercase property names. Read validates both properties present.

4. **Ordinal two-slot encoding** -- Confirmed. `OrdinalRenderer.cs` emits two consecutive slots per `LazyLoad<T>` property (Value + IsLoaded). `GetTotalSlotCount` and `GetSlotIndex` helper methods correctly compute slot counts. Test TS-LL-017 confirms `PropertyNames = ["Lines", "Lines__IsLoaded", "Name"]` and `PropertyTypes = [typeof(string), typeof(bool), typeof(string)]`.

5. **Converter chain placement** -- Confirmed. `LazyLoadJsonConverterFactory` is added in `NeatooJsonSerializer.cs` at line 94, after the DI-resolved converters loop and before `RecordBypassConverterFactory`. It is a standalone `JsonConverterFactory`, NOT a `NeatooJsonConverterFactory` subclass.

6. **DI registration** -- Confirmed. `ILazyLoadFactory` registered as singleton `LazyLoadFactory` in `AddRemoteFactoryServices.cs` line 67.

7. **Generator: IsLazyLoad flag on OrdinalPropertyModel** -- Confirmed. `OrdinalPropertyModel` has `IsLazyLoad` (bool) and `InnerType` (string?) fields. Not expanded to two entries -- single entry with branching in renderer.

8. **Design project** -- Confirmed. `LazyLoadExample.cs` demonstrates constructor-initialization pattern with `IProductReviewService`. `LazyLoadTests.cs` has 6 tests covering create, load, set, and client-server round-trips for both loaded and unloaded states. `SerializationTests.cs` updated to list `LazyLoad<T>` as supported and `Lazy<T>` (BCL) as unsupported. `CLAUDE-DESIGN.md` updated with Quick Decisions, Completeness Checklist, and Files to Consult entries.

9. **No breaking changes** -- Confirmed. All 464 pre-existing unit tests and 503 pre-existing integration tests continue to pass (the additional tests are the new LazyLoad tests and Design tests).

### Conclusion

The implementation fully matches the plan's design. All 22 test scenarios pass. All existing tests pass. No test failures of any kind.
