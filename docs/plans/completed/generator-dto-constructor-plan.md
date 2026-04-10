# Generator-Emitted DTO Constructor Lambdas for IL Trimming

**Date:** 2026-03-25
**Related Todo:** [Generator-Emitted DTO Constructor Lambdas](../todos/generator-dto-constructor-emission.md)
**Status:** Complete
**Last Updated:** 2026-03-30

---

## Overview

When a Blazor WASM client is published with IL trimming, `NeatooJsonSerializer.Deserialize<T>()` fails for plain DTO classes (like `ExampleDto`) because the trimmer strips constructor metadata. The v0.23.2 `Activator.CreateInstance` fallback did not fix this -- `Activator.CreateInstance` also relies on the same stripped metadata.

The fix: the source generator discovers plain DTO return types at compile time and emits `() => new Dto()` constructor lambdas. These static references survive the trimmer. At runtime, `NeatooJsonTypeInfoResolver` uses the registered lambdas instead of `Activator.CreateInstance`.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/generator-dto-constructor-emission.md#requirements-review)

### Relevant Existing Requirements

#### Business Rules

- **Trimming-Safe Registration Pattern** (`CLAUDE-DESIGN.md`, "Trimming-Safe Factory Registration"): The generator already emits `[assembly: NeatooFactoryRegistrar(typeof(X))]` with `[DynamicallyAccessedMembers]` to create static references the trimmer preserves. The DTO constructor pattern follows the same strategy.

- **Auth Type Auto-Registration Precedent** (`CLAUDE-DESIGN.md`, "Auth Type Auto-Registration for Trimming"): The generator already emits explicit `services.TryAddTransient<IFooAuth, FooAuth>()` in `FactoryServiceRegistrar` to create trimmer-visible static references. DTO constructor registration follows this exact pattern: emit registration calls in `FactoryServiceRegistrar`.

- **NeatooJsonTypeInfoResolver CreateObject Pattern** (`src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs:29-48`): The resolver sets `CreateObject` for DI-registered types via `GetRequiredService` and for plain DTOs via `Activator.CreateInstance`. The DTO constructor registry replaces the `Activator.CreateInstance` fallback.

- **RecordBypassConverterFactory Exclusion** (`src/RemoteFactory/Internal/RecordBypassConverterFactory.cs:36-57`): Types with no public parameterless constructor and at least one parameterized constructor are claimed by `RecordBypassConverterFactory`. DTO constructor registration must only target types with public parameterless constructors.

- **Interface Factory Returns Non-Neatoo Types** (`src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:204-230`): Interface factories can return plain DTOs and records. DTO discovery scope includes return types from all three factory patterns.

- **No Reflection Policy** (`CLAUDE.md` global instructions): The fix removes an `Activator.CreateInstance` call and replaces it with a compile-time lambda. Net improvement in reflection posture.

#### Existing Tests

- `src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs:35-55` (`InterfaceFactory_GetAllAsync_ReturnsDataFromServer`): Tests `ExampleDto` round-trip through two DI containers. Must continue to pass.

- `src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs:96-112` (`InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer`): Tests `ExampleRecordResult` record round-trip. Must not regress (records are excluded from DTO registration).

### Gaps

1. **DTO Discovery Scope**: No documented requirement defines exactly which return types qualify. The architect must establish criteria (see Business Rules below).

2. **Generic Collection Unwrapping**: Return types like `IReadOnlyList<ExampleDto>` need unwrapping to discover the inner DTO type.

3. **Registry Initialization Timing**: Must be populated before first deserialization. Following the `FactoryServiceRegistrar` pattern handles this.

4. **Cross-Assembly DTO Types**: If a DTO is in a different assembly, the generator can still see it via `ITypeSymbol` and emit `new Dto()` if the type is accessible.

### Contradictions

None.

### Recommendations for Architect

1. Follow the auth type registration precedent in `FactoryServiceRegistrar`
2. DTO discovery must happen in the transform phase where `ITypeSymbol` is available
3. Exclude records (same rule as `RecordBypassConverterFactory`)
4. Unwrap generic collection types
5. Design project `ExampleDto` is the verification target
6. Replace (don't supplement) the `Activator.CreateInstance` fallback

---

## Business Rules (Testable Assertions)

1. WHEN the generator discovers a factory method whose return type (after unwrapping `Task<T>` and nullable `T?`) is a class with a public parameterless constructor that is NOT a Neatoo `[Factory]`-annotated type and NOT a framework/primitive type, THEN the generator emits a `DtoConstructorRegistry.Register<Dto>(() => new Dto())` call in `FactoryServiceRegistrar`. -- Source: NEW (gap #1 from Requirements Review)

2. WHEN the generator discovers a factory method whose return type (after unwrapping `Task<T>` and nullable `T?`) is a generic collection type (e.g., `IReadOnlyList<ExampleDto>`, `List<SomeDto>`), THEN the generator unwraps the collection to discover inner DTO types and registers each qualifying inner type. -- Source: NEW (gap #2 from Requirements Review; Recommendation #4)

3. WHEN the generator discovers a factory method whose return type is a record (no public parameterless constructor + has parameterized constructors), THEN the generator does NOT emit a DTO constructor registration for that type. -- Source: RecordBypassConverterFactory exclusion (Requirement #4)

4. WHEN the generator discovers a factory method whose return type is a Neatoo `[Factory]`-annotated type, THEN the generator does NOT emit a DTO constructor registration (those types are already DI-registered). -- Source: Existing DI registration pattern

5. WHEN `NeatooJsonTypeInfoResolver.GetTypeInfo()` is called for a type where `CreateObject is null` AND the type has a registered constructor in `DtoConstructorRegistry`, THEN `CreateObject` uses the registered lambda. -- Source: NEW (replaces Activator.CreateInstance; Recommendation #6)

6. WHEN `NeatooJsonTypeInfoResolver.GetTypeInfo()` is called for a type where `CreateObject is null` AND the type is NOT in DI AND NOT in `DtoConstructorRegistry`, THEN `CreateObject` is NOT set (STJ uses its own default behavior, which produces a clear error if the constructor was trimmed). -- Source: NEW (Recommendation #6: replace, don't supplement)

7. WHEN a plain DTO class (`ExampleDto`) is returned by an Interface Factory method and serialized/deserialized through `NeatooJsonSerializer`, THEN the round-trip succeeds with all properties preserved. -- Source: Interface Factory DTO requirement + Existing test

8. WHEN a record (`ExampleRecordResult`) is returned by an Interface Factory method, THEN it continues to be handled by `RecordBypassConverterFactory` (no regression). -- Source: RecordBypassConverterFactory requirement + Existing test

9. WHEN a Neatoo type (DI-registered) is deserialized, THEN it continues to use DI-based `CreateObject` (no regression). -- Source: Existing `NeatooJsonTypeInfoResolver` pattern

10. WHEN DTO constructor registration is emitted, THEN it appears in the same `FactoryServiceRegistrar` method as auth type registrations, and executes during `AddNeatooRemoteFactory()`. -- Source: Auth Type Auto-Registration Precedent (Recommendation #1)

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Interface factory returns `Task<IReadOnlyList<ExampleDto>>` | `IExampleRepository.GetAllAsync()` through client/server containers | 1, 2, 5, 7 | Round-trip succeeds; `ExampleDto` has Id and Name preserved |
| 2 | Interface factory returns `Task<ExampleDto?>` | `IExampleRepository.GetByIdAsync(42)` through client/server containers | 1, 5, 7 | Round-trip succeeds; nullable DTO with Id=42 preserved |
| 3 | Interface factory returns record `Task<ExampleRecordResult?>` | `IExampleRepository.GetRecordByIdAsync(42)` through client/server containers | 3, 8 | Record round-trip succeeds via RecordBypassConverterFactory; no DTO registration emitted |
| 4 | Class factory Create/Fetch returns its own type | `Order` factory `Create()` returning `IOrder` | 4, 9 | No DTO registration; uses DI-based CreateObject |
| 5 | Static factory `[Execute]` returns `Task<bool>` | `ExampleCommands.SendNotification` | 1 (exclusion) | `bool` is a primitive; no DTO registration emitted |
| 6 | Generated code contains `DtoConstructorRegistry.Register` for ExampleDto | Inspect generated `FactoryServiceRegistrar` for `IExampleRepository` | 1, 10 | Generated code includes `DtoConstructorRegistry.Register<ExampleDto>(() => new ExampleDto())` |
| 7 | `NeatooJsonTypeInfoResolver` does NOT use `Activator.CreateInstance` | Review source after changes | 5, 6 | `Activator.CreateInstance` branch removed; registry lookup replaces it |

---

## Approach

Three focused changes, no over-engineering:

**Generator side (compile time):**
1. In `MethodInfo` constructor (where `methodSymbol.ReturnType` is still an `ITypeSymbol`), analyze return types to discover DTO types that need constructor registration
2. Store discovered DTO type names as `EquatableArray<string>` in `TypeInfo`
3. In each renderer's `RenderFactoryServiceRegistrar`, emit `DtoConstructorRegistry.Register` calls

**Runtime side:**
4. Create `DtoConstructorRegistry` -- a static `ConcurrentDictionary<Type, Func<object>>` with a `Register<T>` and `TryCreate` API
5. Replace the `Activator.CreateInstance` branch in `NeatooJsonTypeInfoResolver` with a `DtoConstructorRegistry.TryCreate` call

---

## Design

### Pipeline Data Flow

```
MethodInfo constructor (ITypeSymbol available)
    |
    v
Analyze methodSymbol.ReturnType:
  - Unwrap Task<T>  (already done at line 680-688)
  - Unwrap nullable T?
  - Unwrap collection generics (IReadOnlyList<T>, List<T>, IEnumerable<T>, etc.)
  - Check: has public parameterless constructor?
  - Check: NOT a [Factory]-annotated type?
  - Check: NOT a primitive/string/framework type?
  - Check: NOT a record (no parameterless ctor + has parameterized ctor)?
    |
    v
Store qualifying type's fully-qualified name as string
    |
    v
TypeInfo.DtoReturnTypes: EquatableArray<string>
  (aggregated from all FactoryMethods)
    |
    v
FactoryGenerationUnit (unchanged -- TypeInfo feeds into FactoryModelBuilder
  which feeds data into the model and renderers)
    |
    v
Each renderer's RenderFactoryServiceRegistrar emits:
  DtoConstructorRegistry.Register<{DtoType}>(() => new {DtoType}());
```

### DTO Discovery Logic (in MethodInfo constructor)

The `MethodInfo` constructor at `FactoryGenerator.Types.cs:666` already has `methodSymbol.ReturnType` as an `ITypeSymbol`. The key insight: DTO analysis must happen HERE, before the return type becomes a string.

**New method: `DiscoverDtoReturnTypes(ITypeSymbol returnType) -> List<string>`**

```
1. Start with the raw return type ITypeSymbol
2. If it's Task<T>, extract T (already done for ReturnType string, but we need
   the ITypeSymbol, not the string)
3. Strip nullable annotation
4. If it's a generic collection interface/class, extract the type argument(s):
   - IReadOnlyList<T>, IList<T>, List<T>, IEnumerable<T>,
     ICollection<T>, IReadOnlyCollection<T>, etc.
   - Check: implements IEnumerable<T>? Get T from the type argument.
5. For each candidate type:
   a. Skip if it's a primitive, string, or well-known framework type
      (System.* namespace check, SpecialType != None check)
   b. Skip if it has [Factory] attribute (Neatoo type, already DI-registered)
   c. Skip if it's abstract or an interface
   d. Skip if it has no public parameterless constructor
      (detect by checking constructors: same logic as RecordBypassConverterFactory)
   e. If it passes all checks: add its ToDisplayString() to the DTO list
```

**Where this lives:** A new static method on the `Factory` partial class in `FactoryGenerator.Types.cs`, called from the `MethodInfo` constructor. The discovered types are stored as a new property `EquatableArray<string> DtoReturnTypes` on `MethodInfo`.

**Aggregation in TypeInfo:** `TypeInfo` aggregates all `DtoReturnTypes` from its `FactoryMethods` into a deduplicated `EquatableArray<string>` property.

### Runtime: DtoConstructorRegistry

**File:** `src/RemoteFactory/Internal/DtoConstructorRegistry.cs`

```csharp
namespace Neatoo.RemoteFactory.Internal;

public static class DtoConstructorRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<object>> Constructors = new();

    public static void Register<T>(Func<object> factory)
    {
        Constructors.TryAdd(typeof(T), factory);
    }

    public static bool TryCreate(Type type, out Func<object>? factory)
    {
        return Constructors.TryGetValue(type, out factory);
    }
}
```

Simple, static, no DI dependency. Called from generated `FactoryServiceRegistrar` methods during startup.

### NeatooJsonTypeInfoResolver Change

**Before (line 38-48):**
```csharp
else if (type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) is not null)
{
    jsonTypeInfo.CreateObject = () => Activator.CreateInstance(type)!;
}
```

**After:**
```csharp
else if (DtoConstructorRegistry.TryCreate(type, out var factory))
{
    jsonTypeInfo.CreateObject = factory;
}
```

This removes the `Activator.CreateInstance` fallback entirely. If a DTO is not registered, STJ falls through to its default behavior (which will produce a clear error under trimming, rather than a mysterious `Activator.CreateInstance` failure).

### Generated Code Example

For `IExampleRepository` with methods returning `Task<IReadOnlyList<ExampleDto>>` and `Task<ExampleDto?>`:

```csharp
public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
{
    // ... existing registrations ...

    // DTO constructor registrations (IL trimming support)
    DtoConstructorRegistry.Register<Design.Domain.FactoryPatterns.ExampleDto>(
        () => new Design.Domain.FactoryPatterns.ExampleDto());
}
```

The `ExampleRecordResult` is NOT registered because it has no public parameterless constructor.

### Files to Create

| File | Purpose |
|------|---------|
| `src/RemoteFactory/Internal/DtoConstructorRegistry.cs` | Static registry for DTO constructor lambdas |

### Files to Modify

| File | Change |
|------|--------|
| `src/Generator/FactoryGenerator.Types.cs` | Add `DtoReturnTypes` property to `MethodInfo`; add `DiscoverDtoReturnTypes` method; aggregate in `TypeInfo` |
| `src/Generator/Renderer/ClassFactoryRenderer.cs` | Emit `DtoConstructorRegistry.Register` calls in `RenderFactoryServiceRegistrar` |
| `src/Generator/Renderer/InterfaceFactoryRenderer.cs` | Emit `DtoConstructorRegistry.Register` calls in `RenderFactoryServiceRegistrar` |
| `src/Generator/Renderer/StaticFactoryRenderer.cs` | Emit `DtoConstructorRegistry.Register` calls in `RenderFactoryServiceRegistrar` |
| `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs` | Replace `Activator.CreateInstance` with `DtoConstructorRegistry.TryCreate` |

---

## Implementation Steps

### Phase 1: Runtime Infrastructure

1. Create `DtoConstructorRegistry.cs` in `src/RemoteFactory/Internal/` with the static `Register<T>` and `TryCreate` API
2. Modify `NeatooJsonTypeInfoResolver.GetTypeInfo()` to replace the `Activator.CreateInstance` fallback with `DtoConstructorRegistry.TryCreate`
3. Remove the `System.Reflection` import from `NeatooJsonTypeInfoResolver` if it becomes unused

### Phase 2: Generator DTO Discovery

4. Add `DiscoverDtoReturnTypes(ITypeSymbol returnType)` static method to `Factory` partial class in `FactoryGenerator.Types.cs`:
   - Unwrap `Task<T>` (use the `ITypeSymbol`, not the string that's already extracted)
   - Strip nullable
   - Unwrap generic collections (check if type implements `IEnumerable<T>`, extract T)
   - Apply exclusion rules: primitives, `[Factory]` types, abstract/interface, no parameterless ctor
   - Return fully-qualified type name strings for qualifying types

5. Add `EquatableArray<string> DtoReturnTypes` property to the `MethodInfo` base record. Populate it in the `MethodInfo` constructor by calling `DiscoverDtoReturnTypes(methodSymbol.ReturnType)`.

6. Add `EquatableArray<string> DtoReturnTypes` property to `TypeInfo`. Aggregate and deduplicate from all `FactoryMethods[].DtoReturnTypes`.

### Phase 3: Generator Emission

7. In `ClassFactoryRenderer.RenderFactoryServiceRegistrar`: after auth type registrations, emit `DtoConstructorRegistry.Register<T>(() => new T())` for each type in the model's DTO list. Use `TypeInfo.DtoReturnTypes` -- the data needs to flow through the model. Add a `DtoReturnTypes` property (list of strings) to `ClassFactoryModel`.

8. In `InterfaceFactoryRenderer.RenderFactoryServiceRegistrar`: same as above. Add `DtoReturnTypes` to `InterfaceFactoryModel`.

9. In `StaticFactoryRenderer.RenderFactoryServiceRegistrar`: same as above. Add `DtoReturnTypes` to `StaticFactoryModel`.

10. In `FactoryModelBuilder.Build*` methods: pass `typeInfo.DtoReturnTypes` through to each model type.

### Phase 4: Verification

11. Build the solution and verify all existing tests pass
12. Inspect generated code for `IExampleRepository` factory to confirm `DtoConstructorRegistry.Register<ExampleDto>` is emitted
13. Confirm `ExampleRecordResult` is NOT registered
14. Run Design project tests to verify DTO round-trip works

---

## Acceptance Criteria

- [ ] `DtoConstructorRegistry` class exists in `src/RemoteFactory/Internal/`
- [ ] `NeatooJsonTypeInfoResolver` uses `DtoConstructorRegistry.TryCreate` instead of `Activator.CreateInstance`
- [ ] Generated `FactoryServiceRegistrar` for `IExampleRepository` includes `DtoConstructorRegistry.Register<ExampleDto>(() => new ExampleDto())`
- [ ] Generated code does NOT register `ExampleRecordResult` (no parameterless constructor)
- [ ] All existing tests pass (net9.0 and net10.0)
- [ ] Design project `InterfaceFactoryTests.InterfaceFactory_GetAllAsync_ReturnsDataFromServer` passes
- [ ] Design project `InterfaceFactoryTests.InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` passes
- [ ] `Activator.CreateInstance` call is removed from `NeatooJsonTypeInfoResolver`
- [ ] No new `System.Reflection` usage introduced (net improvement)

**Documentation deliverables (Step 9):** Update `CLAUDE-DESIGN.md` to document the DTO constructor registry pattern alongside the existing Trimming-Safe Factory Registration section.

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Runtime Infrastructure | developer | Yes | Small scope (2 files), clean starting context | None |
| Phase 2-3: Generator Discovery + Emission | developer | No (continue) | Tightly coupled; same agent needs to see the full pipeline | Phase 1 |
| Phase 4: Verification | developer | No (continue) | Same agent runs tests and inspects generated output | Phase 2-3 |

**Parallelizable phases:** None -- these must be sequential.

**Notes:** All four phases should be handled by a single developer agent invocation. The phases are logical groupings for the developer's implementation order, not separate agent sessions. The total scope is approximately 7 files modified + 1 file created, well within a single agent's context.

---

## Dependencies

- No external dependencies
- All changes are within the existing `Neatoo.RemoteFactory` and `Generator` projects
- `ConcurrentDictionary` is available in netstandard2.0 (Generator) and net9.0/net10.0 (Runtime)
- Note: `DtoConstructorRegistry` is in the runtime library (`src/RemoteFactory/`), NOT in the generator (`src/Generator/`). The generator emits code that calls `DtoConstructorRegistry.Register`, but the registry class itself is part of the runtime.

---

## Risks / Considerations

1. **Generator incrementality**: Adding `DtoReturnTypes` to `MethodInfo` and `TypeInfo` must use `EquatableArray<string>` (which implements value equality) to preserve incremental compilation caching. Since `string` implements `IEquatable<string>`, this works with the existing `EquatableArray<T>` constraint.

2. **Generic unwrapping completeness**: The collection unwrapping must handle common collection types (`IReadOnlyList<T>`, `List<T>`, `IEnumerable<T>`, `ICollection<T>`, `IReadOnlyCollection<T>`, arrays). A pragmatic approach: check if the type implements `IEnumerable<T>` and extract `T` from the type argument. This catches all standard collections without enumerating specific types.

3. **Cross-assembly DTOs**: If `ExampleDto` is defined in a different assembly than the factory, the generator can still see it (via `ITypeSymbol`) and emit `new ExampleDto()`. The only constraint is that the type must be accessible (public). This should work for all practical cases since DTOs returned by public interfaces must be public.

4. **Duplicate registrations**: If multiple factories return the same DTO type, each factory's `FactoryServiceRegistrar` will emit a `Register` call. `ConcurrentDictionary.TryAdd` is idempotent -- the second registration is silently ignored. No issue.

5. **Nested generic types**: `Task<IReadOnlyList<ExampleDto>>` requires two levels of unwrapping: first `Task<T>` to `IReadOnlyList<ExampleDto>`, then `IReadOnlyList<T>` to `ExampleDto`. The `MethodInfo` constructor already unwraps `Task<T>` for the `ReturnType` string, but the DTO discovery needs the `ITypeSymbol` at that point. The discovery method receives the original `methodSymbol.ReturnType` and does its own unwrapping.

6. **This is the second attempt**. The first attempt (v0.23.2) used `Activator.CreateInstance` which also fails under trimming. This approach uses generator-emitted static references which is the proven pattern in this codebase (auth types, factory registrar attributes).
