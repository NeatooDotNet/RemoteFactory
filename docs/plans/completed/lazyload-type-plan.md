# LazyLoad<T> Type Implementation Plan

**Date:** 2026-03-28
**Related Todo:** [Add LazyLoad<T> Type to RemoteFactory](../../todos/completed/lazyload-type.md)
**Status:** Complete
**Last Updated:** 2026-03-28

<!-- Valid status values (do not render in plan):
Draft | Under Review (Architect) | Concerns Raised (Architect) | Under Review (Developer) |
Concerns Raised | Ready for Implementation | In Progress | Awaiting Verification | Sent Back |
Requirements Documented | Documentation Complete | Complete
-->

---

## Overview

Extract a minimal, general-purpose `LazyLoad<T>` type from Neatoo into RemoteFactory. This type wraps deferred async loading with explicit `LoadAsync()` triggering (Value is passive — never triggers a load). RemoteFactory owns the core wrapper, serialization support (both named and ordinal formats), and the `ILazyLoadDeserializable` merge pattern. Neatoo will then extend this base type with its PropertyManager and meta-state interface integration.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/lazyload-type.md#requirements-review)

### Relevant Existing Requirements

1. **Serialization guide lists deferred types as unsupported** (SerializationTests.cs:36) -- "Lazy<T> or other deferred types" is in the "NO" list. `LazyLoad<T>` is a new RemoteFactory-native type with purpose-built serialization. The plan correctly addresses updating this comment (step 11).

2. **Public setter requirement exemption** (CLAUDE-DESIGN.md Critical Rule 5) -- `LazyLoad<T>` properties (`Value`, `IsLoaded`) are get-only on the type itself. This is intentional: `LazyLoad<T>` is not a `[Factory]` class and uses `ILazyLoadDeserializable.ApplyDeserializedState()` and custom converter/generator logic instead of standard property-based deserialization.

3. **Anti-Pattern 5 alignment** (CLAUDE-DESIGN.md: storing delegates in fields) -- The loader delegate (`Func<Task<T?>>`) is intentionally NOT serialized, consistent with the rule that delegates/service references are lost after serialization. The merge pattern reconstructs the loader from the constructor on deserialization.

4. **Ordinal serialization contract** (OrdinalRenderer.cs, FactoryGenerator.Types.cs) -- Current system assigns one array slot per property. `LazyLoad<T>` properties need two slots (Value + IsLoaded). This is a fundamental extension. The property collector (`CollectOrdinalProperties`, line 342) filters for properties with public getters AND setters. A `LazyLoad<T>` property with `{ get; set; }` on the owning class WILL be collected, but needs special two-slot rendering.

5. **Converter chain ordering** (NeatooJsonSerializer.cs:76-93) -- Chain is: (1) NeatooOrdinalConverterFactory (ordinal mode only), (2) NeatooJsonConverterFactory instances from DI, (3) RecordBypassConverterFactory. `LazyLoad<T>` will not be claimed by any existing converter. A new `LazyLoadJsonConverterFactory` must be added.

6. **RecordBypassConverterFactory non-conflict** (RecordBypassConverterFactory.cs:36-57) -- `LazyLoad<T>` has a public parameterless constructor, so `CanConvert()` returns false. No conflict.

7. **NeatooInterfaceJsonConverterFactory interaction** (NeatooInterfaceJsonConverterFactory.cs:19-26) -- Claims non-generic interface/abstract types. If T in `LazyLoad<T>` is an interface type, inner value serialization correctly delegates through the full options chain.

8. **IL trimming precedent** -- `RecordBypassConverterFactory` and `NeatooOrdinalConverterFactory` both use `MakeGenericType` with `Activator.CreateInstance`. The plan follows this established pattern.

9. **Multi-targeting** (CLAUDE.md) -- Plan uses only standard .NET APIs available in both net9.0 and net10.0. No concern.

10. **`[Factory]` class property collection** (FactoryGenerator.Types.cs:370-408) -- The property collector filters for public getter AND setter. A `LazyLoad<T>` property declared as `public LazyLoad<OrderLineList> Lines { get; set; }` on a `[Factory]` class will be collected with type `LazyLoad<OrderLineList>`. The two-slot encoding must be applied during rendering.

### Gaps

1. **No existing pattern for multi-slot ordinal encoding** -- Current ordinal system is strictly one-property-one-slot. The two-slot encoding for `LazyLoad<T>` is a new pattern with no precedent. Resolution: Expand `LazyLoad<T>` properties into two `OrdinalPropertyModel` entries during the model-building phase (in `BuildOrdinalSerializationModel` in `FactoryModelBuilder.cs`), using synthetic property names and custom read/write logic in OrdinalRenderer.

2. **No existing pattern for custom type-aware property rendering in OrdinalRenderer** -- OrdinalRenderer treats all properties uniformly. Resolution: Add an `IsLazyLoad` flag and `InnerType` field to `OrdinalPropertyModel` so the renderer can emit specialized two-slot read/write code.

3. **No documentation of `LazyLoad<T>` as a supported property type** -- SerializationTests.cs comment and docs need updating. Addressed in plan step 11.

4. **No existing pattern for converter-level merge on deserialization** -- The `ApplyDeserializedState()` merge pattern is novel in RemoteFactory. Resolution: The named-format converter handles merge; the ordinal-format uses reconstruction (new instances from generated code).

5. **`ILazyLoadFactory` DI registration** -- Must be registered in `AddNeatooRemoteFactory()` in `AddRemoteFactoryServices.cs`, not in generated code. This is a framework-level type, not per-factory.

### Contradictions

None found. The proposal does not violate any documented pattern, anti-pattern, critical rule, or design debt decision.

### Recommendations for Architect

1. **Converter placement**: Add `LazyLoadJsonConverterFactory` as a standalone factory directly in `NeatooJsonSerializer`'s constructor, after the ordinal converter and DI-resolved converters, but BEFORE `RecordBypassConverterFactory`. It does not need DI services and should not be a `NeatooJsonConverterFactory` subclass.

2. **OrdinalPropertyModel extension**: Add `IsLazyLoad` (bool) and `InnerType` (string?) fields to `OrdinalPropertyModel`. The `OrdinalRenderer` will check `IsLazyLoad` to emit two-slot read/write patterns. The `BuildOrdinalSerializationModel` method in `FactoryModelBuilder.cs` maps `LazyLoad<T>` properties to entries with these fields set. PropertyNames and PropertyTypes arrays will contain two entries per `LazyLoad<T>` property.

3. **Named format uses merge, ordinal format uses reconstruction**: The named-format converter uses `ILazyLoadDeserializable.ApplyDeserializedState()` for merge (preserving loader delegates). The ordinal-format generated code uses `new LazyLoad<T>(value)` or `new LazyLoad<T>()` for reconstruction -- no merge needed because ordinal deserialization always creates new parent instances, and the constructor-initialization pattern re-creates the loader.

4. **DI registration approach**: Register `ILazyLoadFactory`/`LazyLoadFactory` as singleton in `AddNeatooRemoteFactory()` (in `AddRemoteFactoryServices.cs`). Register `LazyLoadJsonConverterFactory` by adding it in the `NeatooJsonSerializer` constructor, not through DI.

5. **Trimming annotations**: Apply `[DynamicallyAccessedMembers]` or `[UnconditionalSuppressMessage]` to `LazyLoadJsonConverterFactory.CreateConverter()` following the `RecordBypassConverterFactory` pattern.

6. **Design project examples**: Add a new file in `Design.Domain/` demonstrating `LazyLoad<T>` on a `[Factory]` class with constructor-initialization and add tests to `Design.Tests/FactoryTests/`.

---

## Business Rules (Testable Assertions)

### Core LazyLoad<T> Behavior

**BR-LL-001** (NEW): WHEN a `LazyLoad<T>` is constructed with the parameterless constructor, THEN `Value` RETURNS `null`, `IsLoaded` RETURNS `false`, `IsLoading` RETURNS `false`, `HasLoadError` RETURNS `false`.

**BR-LL-002** (NEW): WHEN a `LazyLoad<T>` is constructed with a loader delegate and `LoadAsync()` has NOT been called, THEN `Value` RETURNS `null` (passive read, no load triggered).

**BR-LL-003** (NEW): WHEN `LoadAsync()` is called on a `LazyLoad<T>` with a loader delegate, THEN the loader is invoked, `IsLoading` RETURNS `true` during execution, and upon completion `Value` RETURNS the loaded value, `IsLoaded` RETURNS `true`, `IsLoading` RETURNS `false`.

**BR-LL-004** (NEW): WHEN `LoadAsync()` is called concurrently from multiple threads, THEN only one load operation executes (thread-safe, shared task).

**BR-LL-005** (NEW): WHEN `LoadAsync()` is called on a `LazyLoad<T>` constructed with the parameterless constructor (no loader), THEN `InvalidOperationException` is thrown.

**BR-LL-006** (NEW): WHEN the loader delegate throws an exception during `LoadAsync()`, THEN `HasLoadError` RETURNS `true`, `LoadError` RETURNS the exception message, and the exception is re-thrown.

**BR-LL-007** (NEW): WHEN `SetValue(value)` is called, THEN `Value` RETURNS the set value, `IsLoaded` RETURNS `true`, `HasLoadError` RETURNS `false` (errors cleared), and `PropertyChanged` fires for `Value`, `IsLoaded`, and `HasLoadError`.

**BR-LL-008** (NEW): WHEN a `LazyLoad<T>` is constructed with a pre-loaded value, THEN `Value` RETURNS that value, `IsLoaded` RETURNS `true`.

**BR-LL-009** (NEW): WHEN `Value` changes (via `LoadAsync()` or `SetValue()`), THEN `PropertyChanged` fires for `nameof(Value)`.

**BR-LL-010** (NEW): WHEN the inner value implements `INotifyPropertyChanged`, THEN `LazyLoad<T>` forwards `PropertyChanged` events from the inner value.

### ILazyLoadFactory

**BR-LL-011** (NEW): WHEN `ILazyLoadFactory.Create<TChild>(loader)` is called, THEN RETURNS a `LazyLoad<TChild>` with the loader delegate configured, `IsLoaded` RETURNS `false`.

**BR-LL-012** (NEW): WHEN `ILazyLoadFactory.Create<TChild>(value)` is called, THEN RETURNS a `LazyLoad<TChild>` with `Value` = value, `IsLoaded` RETURNS `true`.

### Named Format Serialization

**BR-LL-013** (NEW): WHEN a `LazyLoad<T>` with `IsLoaded = true` and `Value = V` is serialized in named format, THEN the JSON output is `{"value": <V>, "isLoaded": true}`.

**BR-LL-014** (NEW): WHEN a `LazyLoad<T>` with `IsLoaded = false` is serialized in named format, THEN the JSON output is `{"value": null, "isLoaded": false}`.

**BR-LL-015** (NEW): WHEN a named-format JSON `{"value": <V>, "isLoaded": true}` is deserialized into `LazyLoad<T>`, THEN the result has `Value = V`, `IsLoaded = true`.

**BR-LL-016** (NEW): WHEN a named-format JSON `{"value": null, "isLoaded": false}` is deserialized into `LazyLoad<T>`, THEN the result has `Value = null`, `IsLoaded = false`.

### Ordinal Format Serialization (Generated Code)

**BR-LL-017** (NEW): WHEN a `[Factory]` class with a `LazyLoad<T>` property is serialized in ordinal format, THEN the `LazyLoad<T>` property occupies two consecutive array slots: `[value, isLoaded]`.

**BR-LL-018** (NEW): WHEN an ordinal array with two-slot `LazyLoad<T>` encoding `[value, true]` is deserialized, THEN the owning class has a `LazyLoad<T>` property with `Value = value`, `IsLoaded = true`.

**BR-LL-019** (NEW): WHEN an ordinal array with two-slot `LazyLoad<T>` encoding `[null, false]` is deserialized, THEN the owning class has a `LazyLoad<T>` property with `Value = null`, `IsLoaded = false` (reconstructed as `new LazyLoad<T>()`).

**BR-LL-020** (NEW): WHEN the generated `PropertyNames` array is inspected for a class with a `LazyLoad<T>` property named `Lines`, THEN it contains two entries for that property: `"Lines"` and `"Lines__IsLoaded"`.

**BR-LL-021** (NEW): WHEN the generated `PropertyTypes` array is inspected for a class with a `LazyLoad<OrderLineList>` property, THEN it contains `typeof(OrderLineList)` and `typeof(bool)` for the two slots.

### Merge Pattern (ILazyLoadDeserializable)

**BR-LL-022** (NEW): WHEN `ApplyDeserializedState(value, true)` is called on a `LazyLoad<T>` that was constructed with a loader delegate, THEN `Value` RETURNS the deserialized value, `IsLoaded` RETURNS `true`, AND the loader delegate is preserved (not cleared).

**BR-LL-023** (NEW): WHEN `ApplyDeserializedState(null, false)` is called on a `LazyLoad<T>` that was constructed with a loader delegate, THEN the instance is unchanged -- `Value` RETURNS `null`, `IsLoaded` RETURNS `false`, and the loader delegate is preserved for on-demand loading.

### DI Registration

**BR-LL-024** (NEW): WHEN `AddNeatooRemoteFactory()` is called, THEN `ILazyLoadFactory` is resolvable from the service provider and RETURNS a `LazyLoadFactory` instance.

### Client/Server Round-Trip

**BR-LL-025** (NEW): WHEN a `[Factory]` class with a loaded `LazyLoad<T>` property is sent through the client-server boundary (serialized on client, deserialized on server or vice versa), THEN the `LazyLoad<T>` property on the deserialized object has the same `Value` and `IsLoaded = true`.

**BR-LL-026** (NEW): WHEN a `[Factory]` class with an unloaded `LazyLoad<T>` property is sent through the client-server boundary, THEN the `LazyLoad<T>` property on the deserialized object has `Value = null` and `IsLoaded = false`.

---

### Test Scenarios

**TS-LL-001** (BR-LL-001): Create `new LazyLoad<string>()`. Assert `Value == null`, `IsLoaded == false`, `IsLoading == false`, `HasLoadError == false`.

**TS-LL-002** (BR-LL-002): Create `new LazyLoad<string>(() => Task.FromResult<string?>("hello"))`. Assert `Value == null` without calling `LoadAsync()`. Loader should NOT have been invoked.

**TS-LL-003** (BR-LL-003): Create `LazyLoad<string>` with loader returning `"loaded"`. Call `LoadAsync()`. Assert `Value == "loaded"`, `IsLoaded == true`, `IsLoading == false`.

**TS-LL-004** (BR-LL-004): Create `LazyLoad<string>` with a loader that has a 200ms delay. Call `LoadAsync()` from 5 concurrent tasks. Assert the loader delegate was invoked exactly once (track via counter). All tasks return the same value.

**TS-LL-005** (BR-LL-005): Create `new LazyLoad<string>()`. Call `LoadAsync()`. Assert `InvalidOperationException` is thrown.

**TS-LL-006** (BR-LL-006): Create `LazyLoad<string>` with a loader that throws `Exception("fail")`. Call `LoadAsync()` (expect it to throw). Assert `HasLoadError == true`, `LoadError == "fail"`.

**TS-LL-007** (BR-LL-007): Create `LazyLoad<string>` with loader. Subscribe to `PropertyChanged`. Call `SetValue("direct")`. Assert `Value == "direct"`, `IsLoaded == true`, `HasLoadError == false`. Assert `PropertyChanged` fired for "Value", "IsLoaded", "HasLoadError".

**TS-LL-008** (BR-LL-008): Create `new LazyLoad<string>("preloaded")`. Assert `Value == "preloaded"`, `IsLoaded == true`.

**TS-LL-009** (BR-LL-003, BR-LL-009): Subscribe to `PropertyChanged` before calling `LoadAsync()`. After load completes, assert "Value" and "IsLoaded" events were received.

**TS-LL-010** (BR-LL-010): Create a mock `INotifyPropertyChanged` object. Create `LazyLoad<MockNpc>` with pre-loaded mock. Raise `PropertyChanged` on the mock. Assert `LazyLoad<T>` forwards the event.

**TS-LL-011** (BR-LL-011): Resolve `ILazyLoadFactory` from DI. Call `Create<string>(loader)`. Assert result `IsLoaded == false`.

**TS-LL-012** (BR-LL-012): Resolve `ILazyLoadFactory` from DI. Call `Create<string>("preloaded")`. Assert result `Value == "preloaded"`, `IsLoaded == true`.

**TS-LL-013** (BR-LL-013, BR-LL-015): Create `LazyLoad<string>("hello")`. Serialize to named JSON. Assert JSON contains `"value":"hello"` and `"isLoaded":true`. Deserialize back. Assert `Value == "hello"`, `IsLoaded == true`.

**TS-LL-014** (BR-LL-014, BR-LL-016): Create `new LazyLoad<string>()`. Serialize to named JSON. Assert JSON contains `"value":null` and `"isLoaded":false`. Deserialize back. Assert `Value == null`, `IsLoaded == false`.

**TS-LL-015** (BR-LL-017, BR-LL-018): Create a `[Factory]` test class with `LazyLoad<string> Lines { get; set; }`. Set `Lines = new LazyLoad<string>("data")`. Serialize in ordinal format. Assert the array contains the value and `true` in consecutive slots. Deserialize. Assert `Lines.Value == "data"`, `Lines.IsLoaded == true`.

**TS-LL-016** (BR-LL-019): Same test class. Set `Lines = new LazyLoad<string>()`. Serialize in ordinal format. Assert the array contains `null` and `false`. Deserialize. Assert `Lines.Value == null`, `Lines.IsLoaded == false`.

**TS-LL-017** (BR-LL-020, BR-LL-021): Inspect the generated `PropertyNames` and `PropertyTypes` static arrays on a `[Factory]` class with a `LazyLoad<string>` property named `Lines`. Assert PropertyNames contains `"Lines"` and `"Lines__IsLoaded"`. Assert PropertyTypes contains `typeof(string)` and `typeof(bool)`.

**TS-LL-018** (BR-LL-022): Create `LazyLoad<string>` with loader. Call `((ILazyLoadDeserializable)ll).ApplyDeserializedState("merged", true)`. Assert `Value == "merged"`, `IsLoaded == true`. Call `LoadAsync()` -- assert it returns "merged" immediately (already loaded), does not invoke the loader.

**TS-LL-019** (BR-LL-023): Create `LazyLoad<string>` with loader returning "loaded". Call `((ILazyLoadDeserializable)ll).ApplyDeserializedState(null, false)`. Assert instance unchanged. Call `LoadAsync()`. Assert `Value == "loaded"` (loader was preserved and invoked).

**TS-LL-020** (BR-LL-024): Build a service collection with `AddNeatooRemoteFactory()`. Resolve `ILazyLoadFactory`. Assert it is not null and is of type `LazyLoadFactory`.

**TS-LL-021** (BR-LL-025): Use `ClientServerContainers.Scopes()` pattern. Create a `[Factory]` class on the server with `LazyLoad<string>` property loaded to "data". Fetch from client. Assert client-side result has `Lines.Value == "data"`, `Lines.IsLoaded == true`.

**TS-LL-022** (BR-LL-026): Use `ClientServerContainers.Scopes()` pattern. Create a `[Factory]` class on the server with unloaded `LazyLoad<string>`. Fetch from client. Assert `Lines.Value == null`, `Lines.IsLoaded == false`.

---

## Approach

Port the core `LazyLoad<T>` from Neatoo (neatoodotnet/Neatoo), stripping all Neatoo-specific interfaces. Add serialization support via a custom `JsonConverterFactory` and generator changes for ordinal encoding. The existing Neatoo implementation has been through multiple iterations and is the proven design to follow.

**Key principles:**
- `Value` is passive (never triggers a load)
- `LoadAsync()` is the only way to trigger loading
- Thread-safe: concurrent `LoadAsync()` calls share a single load task
- Serialization preserves Value + IsLoaded, drops loader delegate
- Constructor-initialization pattern: loader survives serialization via constructor reconstruction + `ILazyLoadDeserializable` merge

---

## Domain Model Behavioral Design

This is a library type, not a domain model. The "behavioral design" is the type's state machine and API contract.

### State Machine

```
                  ┌──────────────────────────────────────────┐
                  │           UNLOADED                        │
                  │  Value=null, IsLoaded=false               │
                  │  IsLoading=false, HasLoadError=false      │
                  └─────┬──────────────┬──────────────┬──────┘
                        │              │              │
              LoadAsync()    SetValue(v)    ApplyDeserializedState(v, true)
                        │              │              │
                        v              v              v
                  ┌──────────┐   ┌──────────┐   ┌──────────┐
                  │ LOADING  │   │ LOADED   │   │ LOADED   │
                  │ IsLoading│──>│ Value=v  │   │ Value=v  │
                  │ =true    │   │ IsLoaded │   │ IsLoaded │
                  └────┬─────┘   │ =true   │   │ =true    │
                       │         └──────────┘   └──────────┘
                  (error)
                       │
                       v
                  ┌──────────┐
                  │ ERROR    │
                  │ HasLoad  │
                  │ Error    │
                  │ =true    │
                  └──────────┘
```

### Computed Properties
- `HasLoadError` = `_loadError != null`
- `IsLoading` = `_isLoading` (set during `LoadAsyncCore()`)

### INPC Forwarding
- When inner value implements `INotifyPropertyChanged`, `LazyLoad<T>` subscribes and forwards all property change events
- Subscription managed on value transitions (subscribe new, unsubscribe old)

### Serialization Visibility
- `Value` and `IsLoaded`: serialized (`[JsonInclude]`)
- `IsLoading`, `HasLoadError`, `LoadError`, loader delegate: not serialized (`[JsonIgnore]`)
- Loader delegate: transient state, reconstructed via constructor-initialization pattern on deserialization

---

## Design

### 1. Core Type: `LazyLoad<T>` (in `src/RemoteFactory/`)

Port from Neatoo's `LazyLoad<T>`, keeping:

```csharp
public class LazyLoad<T> : INotifyPropertyChanged, ILazyLoadDeserializable where T : class?
```

**Public API:**
- `T? Value { get; }` — passive read, `[JsonInclude]`
- `bool IsLoaded { get; }` — `[JsonInclude]`
- `bool IsLoading { get; }` — `[JsonIgnore]`
- `bool HasLoadError { get; }` — `[JsonIgnore]`
- `string? LoadError { get; }` — `[JsonIgnore]`
- `Task<T?> LoadAsync()` — explicit load trigger, thread-safe
- `void SetValue(T? value)` — direct set, bypasses loader
- `event PropertyChangedEventHandler? PropertyChanged`

**Constructors:**
- `LazyLoad()` — parameterless, `[JsonConstructor]`, no loader
- `LazyLoad(Func<Task<T?>> loader)` — with loader delegate
- `LazyLoad(T? value)` — pre-loaded

**Stripped from Neatoo version:**
- `IValidateMetaProperties` implementation (entire region)
- `IEntityMetaProperties` implementation (entire region)
- References to `IPropertyMessage`, `RunRulesFlag`, Neatoo.Rules namespace

**Kept from Neatoo version:**
- `INotifyPropertyChanged` with forwarding of child value's PropertyChanged
- `ILazyLoadDeserializable` (internal interface, explicit implementation)
- Thread-safe `_loadLock` pattern
- `SetValue()` method (subscribe/unsubscribe to child INPC)
- Load error tracking

### 2. `ILazyLoadDeserializable` (internal, in `src/RemoteFactory/`)

```csharp
internal interface ILazyLoadDeserializable
{
    bool IsLoaded { get; }
    object? BoxedValue { get; }
    void ApplyDeserializedState(object? value, bool isLoaded);
}
```

Same as Neatoo's version. Used by the converter to merge deserialized state into constructor-created instances.

### 3. `ILazyLoadFactory` + `LazyLoadFactory` (in `src/RemoteFactory/`)

```csharp
public interface ILazyLoadFactory
{
    LazyLoad<TChild> Create<TChild>(Func<Task<TChild?>> loader) where TChild : class?;
    LazyLoad<TChild> Create<TChild>(TChild? value) where TChild : class?;
}

public class LazyLoadFactory : ILazyLoadFactory { ... }
```

Direct port. Registered in DI via `AddRemoteFactoryServices()`.

### 4. Serialization: `LazyLoadJsonConverterFactory` (in `src/RemoteFactory/Internal/`)

Custom converter factory that claims `LazyLoad<T>` types.

**Named format serialization:**
```json
{ "value": { /* serialized T */ }, "isLoaded": true }
```
or
```json
{ "value": null, "isLoaded": false }
```

**Converter behavior:**
- Write: serialize Value and IsLoaded
- Read: deserialize Value and IsLoaded, construct via `new LazyLoad<T>(value)` if loaded, or `new LazyLoad<T>()` if not
- **Merge pattern:** If the target instance already exists (constructor-created with loader), use `ILazyLoadDeserializable.ApplyDeserializedState()` to merge rather than replace

### 5. Ordinal Serialization: Generator Changes

When the generator encounters a `LazyLoad<T>` property on a `[Factory]` class:

**Two-slot encoding in ordinal array:**
- Slot N: the Value (serialized using T's converter/ordinal format)
- Slot N+1: IsLoaded (bool)

**Example:** A class with `string Name` and `LazyLoad<OrderLineList> Lines`:
```
Ordinal: ["John", [/* lines data */], true]
                   ^-- Value            ^-- IsLoaded
Named:   {"Name": "John", "Lines": {"value": [...], "isLoaded": true}}
```

**Generator changes needed:**
- `FactoryGenerator.Types.cs` — detect `LazyLoad<T>` in property collection, extract inner type
- `OrdinalRenderer.cs` — emit two-slot read/write for `LazyLoad<T>` properties
- `PropertyTypes` array — include both inner type and `typeof(bool)` for each `LazyLoad<T>` property
- `PropertyNames` array — include both the property name and a synthetic `{Name}__IsLoaded` entry
- `ToOrdinalArray()` — emit `lazyLoadProp.Value` and `lazyLoadProp.IsLoaded` as two consecutive elements
- `FromOrdinalArray()` — reconstruct: if isLoaded, `new LazyLoad<T>(value)`, else `new LazyLoad<T>()`

### 6. DI Registration

In `AddRemoteFactoryServices()` (generated):
- Register `ILazyLoadFactory` as singleton `LazyLoadFactory`
- Add `LazyLoadJsonConverterFactory` to converter pipeline

### 7. Design Project Updates

Add `LazyLoad<T>` examples to `src/Design/Design.Domain/` demonstrating:
- Class factory with a `LazyLoad<T>` property
- Constructor-initialization pattern (loader in constructor)
- Serialization round-trip test

Update `SerializationTests.cs` comment to move `Lazy<T>` note and add `LazyLoad<T>` as supported.

---

## Implementation Steps

1. **Add `LazyLoad<T>` type** — Port from Neatoo, strip meta-state interfaces, add to `src/RemoteFactory/`
2. **Add `ILazyLoadDeserializable`** — Internal interface in `src/RemoteFactory/`
3. **Add `ILazyLoadFactory` + `LazyLoadFactory`** — Port from Neatoo, add to `src/RemoteFactory/`
4. **Add `LazyLoadJsonConverterFactory`** — Named format converter with merge pattern support
5. **Generator: detect `LazyLoad<T>` properties** — Update `FactoryGenerator.Types.cs` property collection
6. **Generator: ordinal two-slot encoding** — Update `OrdinalRenderer.cs` for read/write of `LazyLoad<T>` properties
7. **DI registration** — Register factory and converter in generated `AddRemoteFactoryServices()`
8. **Unit tests** — Core `LazyLoad<T>` behavior (Value passive, LoadAsync, SetValue, thread safety, error handling, INPC events)
9. **Serialization round-trip tests** — Both ordinal and named formats, loaded and unloaded states, using `ClientServerContainers`
10. **Design project** — Add examples and tests to `src/Design/`
11. **Update serialization guide** — Update `SerializationTests.cs` comment to list `LazyLoad<T>` as supported

---

## Acceptance Criteria

- [ ] `LazyLoad<T>` compiles and works without any Neatoo dependency
- [ ] `Value` is passive — never triggers a load
- [ ] `LoadAsync()` triggers loading, is thread-safe (concurrent calls share one task)
- [ ] `SetValue()` directly sets value, fires INPC, clears errors
- [ ] Named format serializes as `{"value": ..., "isLoaded": bool}`
- [ ] Ordinal format uses two-slot encoding `[value, isLoaded]`
- [ ] Unloaded state serializes as `[null, false]` / `{"value": null, "isLoaded": false}`
- [ ] `ILazyLoadDeserializable.ApplyDeserializedState()` merges into constructor-created instances
- [ ] `ILazyLoadFactory` registered in DI
- [ ] Serialization round-trip tests pass for both formats (loaded + unloaded)
- [ ] `ClientServerContainers` round-trip test passes
- [ ] Design project demonstrates the pattern with passing tests
- [ ] All existing tests continue to pass

---

## Agent Phasing

### Phase 1: Core Types + Named Serialization (Fresh Agent)

**Deliverables:**
- `LazyLoad<T>` class (port from Neatoo, strip meta-state interfaces) -- `src/RemoteFactory/LazyLoad.cs`
- `ILazyLoadDeserializable` interface -- `src/RemoteFactory/Internal/ILazyLoadDeserializable.cs`
- `ILazyLoadFactory` + `LazyLoadFactory` -- `src/RemoteFactory/ILazyLoadFactory.cs`
- `LazyLoadJsonConverterFactory` + `LazyLoadJsonConverter<T>` -- `src/RemoteFactory/Internal/LazyLoadJsonConverterFactory.cs`
- DI registration in `AddRemoteFactoryServices.cs`
- Converter insertion in `NeatooJsonSerializer.cs`
- Unit tests: Core behavior (TS-LL-001 through TS-LL-012), Named serialization (TS-LL-013, TS-LL-014), Merge pattern (TS-LL-018, TS-LL-019), DI registration (TS-LL-020)

**Verification gate:** All new unit tests pass. All existing tests pass. `LazyLoad<T>` works standalone without generator changes.

### Phase 2: Generator Changes + Ordinal Serialization (Fresh Agent)

**Deliverables:**
- `OrdinalPropertyModel` extension (add `IsLazyLoad`, `InnerType`) -- `src/Generator/Model/OrdinalSerializationModel.cs`
- `CollectOrdinalProperties` detection of `LazyLoad<T>` -- `src/Generator/FactoryGenerator.Types.cs`
- `BuildOrdinalSerializationModel` mapping -- `src/Generator/Builder/FactoryModelBuilder.cs`
- `OrdinalRenderer` two-slot read/write -- `src/Generator/Renderer/OrdinalRenderer.cs`
- Test target class with `LazyLoad<T>` property
- Unit tests: Ordinal format (TS-LL-015 through TS-LL-017)
- Integration tests: Client/server round-trip (TS-LL-021, TS-LL-022)

**Verification gate:** All new tests pass. All existing tests pass. Generated code for `LazyLoad<T>` properties produces correct ordinal arrays.

### Phase 3: Design Project + Documentation (Fresh Agent)

**Deliverables:**
- Design project example class with `LazyLoad<T>` property
- Design project tests demonstrating pattern
- Update `SerializationTests.cs` comment (move `LazyLoad<T>` to YES list)
- Update CLAUDE-DESIGN.md Design Completeness Checklist if applicable

**Verification gate:** Design project builds and tests pass. All existing design tests pass.

### Rationale for Fresh Agents per Phase

- **Phase 1** is self-contained: core types + named serialization have no generator dependency
- **Phase 2** is generator work requiring a fresh context window focused on the Roslyn source generator pipeline, which is architecturally distinct from Phase 1
- **Phase 3** is documentation/example work that benefits from seeing the completed implementation with fresh eyes

---

## Dependencies

- Reference implementation: Neatoo's `LazyLoad<T>` at `neatoodotnet/Neatoo/src/Neatoo/LazyLoad.cs`
- Reference factory: `neatoodotnet/Neatoo/src/Neatoo/ILazyLoadFactory.cs`
- RemoteFactory generator architecture (ordinal rendering, property collection)
- RemoteFactory serialization pipeline (converter factory pattern)

---

## Risks / Considerations

- **Generator complexity** — Two-slot encoding changes the ordinal array layout; must handle property count mismatches carefully
- **Neatoo migration** — After this lands, Neatoo needs to migrate its `LazyLoad<T>` to extend RemoteFactory's version (separate work, not in scope)
- **IL trimming** — `LazyLoadJsonConverterFactory` uses `MakeGenericType` which may need trimming annotations; follow existing patterns (`RecordBypassConverterFactory`)
- **Inner type serialization** — `LazyLoad<T>` where T is an interface-typed factory class needs the `NeatooInterfaceJsonConverterFactory` to handle the inner value; verify converter ordering
- **`Lazy<T>` vs `LazyLoad<T>`** — The serialization guide currently says "`Lazy<T>` or other deferred types" are unsupported. `LazyLoad<T>` is the supported deferred type; `Lazy<T>` (BCL) remains unsupported
