# Fix Record Deserialization with $ref Metadata

**Date:** 2026-03-20
**Related Todo:** [Record Deserialization Fails with $ref Metadata](../todos/record-deserialization-ref-metadata.md)
**Status:** Documentation Complete
**Last Updated:** 2026-03-20 (developer deliverables completed)

---

## Overview

When an Interface Factory method returns a plain record type (no `[Factory]` attribute), the `NeatooJsonSerializer` emits `$id`/`$ref` metadata via `ReferenceHandler.Preserve`. System.Text.Json cannot deserialize types with parameterized constructors when `$ref` metadata is present, causing `ObjectWithParameterizedCtorRefMetadataNotSupported` errors on the client.

The fix introduces a dual-options approach: the serializer maintains two `JsonSerializerOptions` instances -- one with `ReferenceHandler.Preserve` for Neatoo types (which handle `$id`/`$ref` manually via custom converters), and one without reference handling for non-Neatoo fallback serialization. The server response path determines which options to use based on whether the return type is a Neatoo type.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/record-deserialization-ref-metadata.md#requirements-review)

### Relevant Existing Requirements

#### Serialization Architecture

- **NeatooJsonSerializer (`src/RemoteFactory/Internal/NeatooJsonSerializer.cs:68-74`):** Applies `ReferenceHandler.Preserve` globally via `NeatooReferenceHandler`. All serialization/deserialization passes through a single `JsonSerializerOptions` instance. -- Relevance: This is the root cause. The single options instance forces `$id`/`$ref` onto all types including records that cannot handle it.

- **Custom Converter Chain (`NeatooJsonSerializer.cs:76-85`):** Three converter factories registered in order: `NeatooOrdinalConverterFactory` (for `IOrdinalSerializable`), then `NeatooJsonConverterFactory` subclasses (currently only `NeatooInterfaceJsonConverterFactory`). Types not matched by any converter fall through to STJ built-in handling. -- Relevance: The converter chain correctly handles Neatoo types. The problem is only in the STJ fallback path for non-Neatoo types.

- **NeatooInterfaceJsonTypeConverter (`src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs:44-48`):** Manages `$id`/`$ref` manually for interface-typed Neatoo properties using `$type`/`$value` wrapping. Calls `options.ReferenceHandler.CreateResolver().AddReference()` explicitly. -- Relevance: This converter already handles reference semantics itself. It does not rely on STJ's built-in `$id`/`$ref` emission; it uses its own `$type`/`$value` envelope.

- **NeatooOrdinalConverterFactory (`src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs:60-69`):** Only converts types implementing `IOrdinalSerializable` when format is `Ordinal`. Writes arrays, bypassing `$id`/`$ref` entirely. -- Relevance: Records with `[Factory]` get `IOrdinalSerializable` generated code and use ordinal format. The problem only affects records WITHOUT `[Factory]`.

#### Interface Factory Return Types

- **Design Source of Truth (`src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:204-220`):** Interface Factory methods can return arbitrary non-Neatoo types. The existing `ExampleDto` is a class with public setters and a default constructor. -- Relevance: Establishes that non-Neatoo return types are a supported pattern. The class-based `ExampleDto` works because STJ can populate it via property setters even with `$id`/`$ref`. Records with primary constructors cannot.

#### Server Response Serialization

- **HandleRemoteDelegateRequest (`src/RemoteFactory/HandleRemoteDelegateRequest.cs:126-141`):** Extracts the delegate's return type and calls `serializer.Serialize(result, returnType)`. The return type is known at this point. -- Relevance: This is the key integration point where the serialization strategy can be varied per return type.

#### Client Response Deserialization

- **MakeRemoteDelegateRequest (`src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs:69-93`):** Client calls `_neatooJsonSerializer.DeserializeRemoteResponse<T>(result)` which calls `Deserialize<T>(json)`. The `T` is the expected return type. -- Relevance: Deserialization must use matching options (no `ReferenceHandler`) to correctly parse JSON that was serialized without `$id`/`$ref`.

#### Existing Tests

- **RecordSerializationTests (`src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/RecordSerializationTests.cs`):** Tests records WITH `[Factory]` through client-server round-trips. These use ordinal format and are NOT affected by this bug. -- Relevance: Must continue to pass.

- **InterfaceFactoryTests (`src/Tests/RemoteFactory.IntegrationTests/FactoryGenerator/InterfaceFactory/InterfaceFactoryTests.cs`):** Tests Interface Factory with `bool` and `List<string>` return types. These primitive/collection types are not affected by the `$ref` issue. -- Relevance: Must continue to pass.

- **CoverageGapSerializationTests (`src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/CoverageGapSerializationTests.cs`):** Tests dictionary, enum, and large object types through round-trips. All use `[Factory]`-decorated classes. -- Relevance: Must continue to pass.

### Gaps

1. **No test for Interface Factory returning a record with parameterized constructor.** The existing Interface Factory tests return `bool` and `List<string>`. The record serialization tests use `[Factory]`-decorated records (ordinal path). No test covers the intersection: an Interface Factory method returning a plain record.

2. **No documented anti-pattern for mixing Neatoo types with records in return types.** Clarification A3 establishes this as a new anti-pattern.

3. **No documented scope for reference preservation.** Published docs describe it for Neatoo domain objects but don't state whether it applies to all types.

4. **No Design project example of Interface Factory returning a record.** `AllPatterns.cs` uses `ExampleDto` (a class).

### Contradictions

None.

### Recommendations for Architect

1. Scope the fix to the serializer layer only.
2. Preserve reference handling for Neatoo types.
3. Add a Design project example after implementation.
4. Document the new anti-pattern (Clarification A3).
5. Test with the two DI container pattern.
6. Consider the HandleRemoteDelegateRequest response path as the key integration point.
7. Verify on both net9.0 and net10.0.

---

## Business Rules (Testable Assertions)

1. WHEN a non-Neatoo type (no `[Factory]`, no `IOrdinalSerializable`) is serialized by `NeatooJsonSerializer`, THEN the output JSON does NOT contain `$id` or `$ref` metadata properties. -- Source: NEW (Gap 1, Clarification A2)

2. WHEN a Neatoo type (implements `IOrdinalSerializable` or is matched by `NeatooInterfaceJsonConverterFactory`) is serialized by `NeatooJsonSerializer`, THEN reference handling (`$id`/`$ref` for named format, or ordinal array format) continues to work as before. -- Source: NeatooInterfaceJsonTypeConverter lines 44-48, NeatooOrdinalConverterFactory lines 60-69

3. WHEN an Interface Factory method returns a record with a primary constructor, THEN the full client-server-client round-trip succeeds and the record is correctly deserialized on the client with all property values preserved. -- Source: NEW (Gap 1, todo Problem section)

4. WHEN an Interface Factory method returns a record containing a collection (e.g., `IReadOnlyList<T>`), THEN the collection is correctly deserialized with all elements preserved after the round-trip. -- Source: NEW (Gap 1, matches the minimal repro in the todo)

5. WHEN an Interface Factory method returns null for a nullable record return type, THEN the client receives null without error. -- Source: NEW (defensive rule for null handling)

6. WHEN an Interface Factory method returns a class with public setters (e.g., `ExampleDto`), THEN the round-trip continues to work as before (no regression). -- Source: Design Source of Truth AllPatterns.cs:204-220

7. WHEN a Neatoo [Factory]-decorated record with primary constructor is used (e.g., `SimpleRecord`), THEN it continues to serialize/deserialize via ordinal format without error. -- Source: RecordSerializationTests (existing passing tests)

8. WHEN a Neatoo [Factory]-decorated class is fetched remotely, THEN it continues to serialize/deserialize with full reference handling without error. -- Source: Existing round-trip tests (RemoteFetchRoundTripTests, etc.)

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Interface Factory returns simple record | `record MyResult(string Name, int Value)` returned from server | Rule 1, 3 | Record deserialized on client with `Name` and `Value` intact |
| 2 | Interface Factory returns record with collection | `record MyResult(string Name, IReadOnlyList<MyItem> Items)` where `MyItem` is a record | Rule 1, 3, 4 | Record deserialized with collection containing all items |
| 3 | Interface Factory returns record with nested record | `record Outer(string Name, Inner Child)` where `Inner` is also a record | Rule 1, 3 | Both records deserialized with all properties intact |
| 4 | Interface Factory returns null for nullable record | Method returns `Task<MyResult?>`, server returns null | Rule 5 | Client receives null |
| 5 | Interface Factory returns class with public setters (regression) | `ExampleDto` class returned from Interface Factory | Rule 6 | Class deserialized correctly, same as before |
| 6 | [Factory]-decorated record round-trip (regression) | `[Factory] public partial record SimpleRecord(string Name, int Value)` | Rule 7 | Ordinal serialization/deserialization works as before |
| 7 | [Factory]-decorated class remote fetch (regression) | Class with `[Factory]`, `[Fetch]`, `[Remote]` | Rule 8 | Full round-trip with reference handling works as before |
| 8 | Non-Neatoo type JSON has no $id/$ref | Serialize a plain record and inspect JSON output | Rule 1 | JSON string does not contain `"$id"` or `"$ref"` |

---

## Approach

### Strategy: Dual JsonSerializerOptions

The serializer will maintain TWO `JsonSerializerOptions` instances:

1. **Neatoo Options** (existing): `ReferenceHandler = NeatooReferenceHandler`, with all custom converters. Used for Neatoo types that handle `$id`/`$ref` manually.

2. **Plain Options** (new): No `ReferenceHandler`, with the same `TypeInfoResolver` and converter chain. Used for non-Neatoo types (records, DTOs, primitives, collections) where `$id`/`$ref` metadata would break parameterized constructor deserialization.

### Type Classification

A type is "Neatoo" if it meets ANY of these criteria:
- Implements `IOrdinalSerializable` (records/classes with `[Factory]`)
- Is an interface/abstract type recognized by `NeatooInterfaceJsonConverterFactory` (has entries in `IServiceAssemblies`)

All other types (plain records, classes, primitives, collections) use the plain options.

### Classification Location

The classification happens in `NeatooJsonSerializer` at the `Serialize(object? target, Type targetType)` and `Deserialize(string json, Type type)` methods. The `targetType` parameter is already available. The check is:

```
bool isNeatooType = typeof(IOrdinalSerializable).IsAssignableFrom(targetType)
                    || (targetType.IsInterface && serviceAssemblies.HasType(targetType));
```

For generic wrapper types like `Task<T>`, `IReadOnlyList<T>`, etc., the `returnType` extracted in `HandleRemoteDelegateRequest` is already unwrapped to the inner `T`, so the check works directly.

### Circular Reference Assessment (Clarification A2)

Supporting circular references in plain records/DTOs would require implementing a custom `JsonConverter` that manually handles `$id`/`$ref` for arbitrary types with parameterized constructors -- essentially reimplementing STJ's reference resolution with constructor-aware deserialization. This is NOT trivial; it would require:
- Buffering the JSON to read ahead for `$ref` targets
- Constructing objects in two passes (allocate, then populate)
- Handling all constructor parameter patterns STJ supports

**Verdict: Not trivial. Skip per Clarification A2.** Plain records/DTOs are serialized without reference handling. If users need circular references, they should use Neatoo types.

---

## Design

### Modified Files

#### 1. `src/RemoteFactory/Internal/NeatooJsonSerializer.cs`

**Changes:**
- Add a second `JsonSerializerOptions` instance (`PlainOptions`) without `ReferenceHandler`, sharing the same `TypeInfoResolver` and converter chain
- Add a private helper method `IsNeatooType(Type type)` that determines whether a type should use Neatoo options (with reference handling) or plain options
- Modify `Serialize(object? target, Type targetType)` to select options based on `IsNeatooType(targetType)`
- Modify `Deserialize(string json, Type type)` to select options based on `IsNeatooType(type)`
- Modify `Serialize(object? target)` (no type parameter) to use `IsNeatooType(target.GetType())`
- Modify `Deserialize<T>(string json)` to use `IsNeatooType(typeof(T))`

**Design detail -- IsNeatooType logic:**
```
private bool IsNeatooType(Type type)
{
    // Unwrap nullable value types
    var underlying = Nullable.GetUnderlyingType(type) ?? type;

    // IOrdinalSerializable = [Factory]-decorated type with generated ordinal support
    if (typeof(IOrdinalSerializable).IsAssignableFrom(underlying))
        return true;

    // Interface/abstract types registered in IServiceAssemblies = Neatoo interface factory types
    if ((underlying.IsInterface || underlying.IsAbstract) && serviceAssemblies.HasType(underlying))
        return true;

    return false;
}
```

**Design detail -- PlainOptions construction:**
```
this.PlainOptions = new JsonSerializerOptions
{
    // No ReferenceHandler -- this is the key difference
    TypeInfoResolver = neatooDefaultJsonTypeInfoResolver,
    WriteIndented = serializationOptions.Format == SerializationFormat.Named,
    IncludeFields = true
};

// Add the same converters (they will CanConvert=false for non-Neatoo types anyway)
if (serializationOptions.Format == SerializationFormat.Ordinal)
{
    this.PlainOptions.Converters.Add(new NeatooOrdinalConverterFactory(serializationOptions));
}
foreach (var factory in neatooJsonConverterFactories)
{
    this.PlainOptions.Converters.Add(factory);
}
```

The converters are shared because they only match Neatoo types (`CanConvert` returns false for non-Neatoo types), so they are inert in the plain options. The critical difference is the absence of `ReferenceHandler`, which prevents STJ from emitting/expecting `$id`/`$ref` metadata.

**Design detail -- Serialize method selection:**

In `Serialize(object? target, Type targetType)`:
```
var options = IsNeatooType(targetType) ? this.Options : this.PlainOptions;
// For Neatoo types, set up the reference resolver as before
if (options == this.Options)
{
    using var rr = new NeatooReferenceResolver();
    this.ReferenceHandler.ReferenceResolver.Value = rr;
}
var result = JsonSerializer.Serialize(target, targetType, options);
```

Same pattern for `Deserialize`.

#### 2. New Test Targets: `src/Tests/RemoteFactory.IntegrationTests/TestTargets/TypeSerialization/InterfaceFactoryRecordTargets.cs`

New file containing:
- `record InterfaceRecordResult(string Name, int Value)` -- simple record
- `record InterfaceRecordItem(int Id, string Description)` -- item for collection
- `record InterfaceRecordWithCollection(string Name, IReadOnlyList<InterfaceRecordItem> Items)` -- record with collection (the repro scenario)
- `record InterfaceNestedOuter(string Label, InterfaceNestedInner Child)` and `record InterfaceNestedInner(int Id)` -- nested records
- `[Factory] public interface IRecordReturnService` with methods returning these types
- `public class RecordReturnService : IRecordReturnService` -- server implementation

#### 3. New Test File: `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs`

Tests covering all scenarios in the Test Scenarios table. Uses `ClientServerContainers.Scopes(configureServer: ...)` to register the `IRecordReturnService` implementation.

### What Does NOT Change

- `NeatooReferenceHandler.cs` -- Unchanged
- `NeatooReferenceResolver.cs` -- Unchanged
- `NeatooInterfaceJsonConverterFactory.cs` -- Unchanged
- `NeatooInterfaceJsonTypeConverter.cs` -- Unchanged
- `NeatooOrdinalConverterFactory.cs` -- Unchanged
- `HandleRemoteDelegateRequest.cs` -- Unchanged (it already passes `returnType` to `Serialize`)
- `MakeRemoteDelegateRequest.cs` -- Unchanged (it calls `DeserializeRemoteResponse<T>`)
- Generator code -- No changes needed
- All existing tests -- Must continue to pass

### Data Flow (After Fix)

```
Server: HandleRemoteDelegateRequest
  |
  +-- serializer.Serialize(result, returnType)
        |
        +-- IsNeatooType(returnType)?
              |
              YES --> Use Options (with ReferenceHandler.Preserve, $id/$ref emitted)
              |         |
              |         +-- Custom converters handle $type/$value wrapping
              |
              NO  --> Use PlainOptions (no ReferenceHandler, no $id/$ref)
                        |
                        +-- STJ built-in serialization, clean JSON

Client: MakeRemoteDelegateRequest
  |
  +-- serializer.DeserializeRemoteResponse<T>(result)
        |
        +-- serializer.Deserialize<T>(json)
              |
              +-- IsNeatooType(typeof(T))?
                    |
                    YES --> Use Options (with ReferenceHandler, resolves $id/$ref)
                    |
                    NO  --> Use PlainOptions (no ReferenceHandler, no $id/$ref expected)
                              |
                              +-- STJ built-in deserialization, works with parameterized ctors
```

---

## Implementation Steps

### Phase 1: Serializer Dual-Options (Core Fix)

1. Add `PlainOptions` field to `NeatooJsonSerializer` -- a second `JsonSerializerOptions` without `ReferenceHandler`
2. Add `IsNeatooType(Type type)` private method
3. Update `Serialize(object? target)` to select options based on type
4. Update `Serialize(object? target, Type targetType)` to select options based on type
5. Update `Deserialize<T>(string json)` to select options based on `typeof(T)`
6. Update `Deserialize(string json, Type type)` to select options based on type
7. Build and verify all existing tests pass

### Phase 2: Test Targets and Tests

1. Create `InterfaceFactoryRecordTargets.cs` with record types, interface factory, and server implementation
2. Create `InterfaceFactoryRecordSerializationTests.cs` with tests for all 8 scenarios
3. Register `IRecordReturnService` -> `RecordReturnService` in test container setup
4. Run all tests -- new tests should pass, existing tests should continue to pass
5. Verify on both net9.0 and net10.0

---

## Acceptance Criteria

- [ ] Interface Factory returning `record(string Name, int Value)` completes full client-server round-trip
- [ ] Interface Factory returning `record(string Name, IReadOnlyList<record> Items)` completes full round-trip with collection intact
- [ ] Interface Factory returning null for nullable record returns null on client
- [ ] Existing `[Factory]`-decorated record tests pass (ordinal serialization unaffected)
- [ ] Existing `[Factory]`-decorated class tests pass (reference handling preserved)
- [ ] Existing Interface Factory tests pass (`bool`, `List<string>` return types)
- [ ] All existing integration tests pass
- [ ] All existing unit tests pass
- [ ] Tests pass on both net9.0 and net10.0
- [ ] JSON output for non-Neatoo types does not contain `$id` or `$ref`

---

## Dependencies

- System.Text.Json (already a dependency, no version change needed)
- No new NuGet packages required
- No generator changes required

---

## Risks / Considerations

1. **Type classification edge cases.** The `IsNeatooType` check must correctly handle:
   - Generic collection types wrapping Neatoo types (e.g., `IReadOnlyList<IMyNeatooInterface>`) -- these serialize as arrays, not objects, so the collection itself does not get `$id`. The contained Neatoo interface items are handled by `NeatooInterfaceJsonConverterFactory` which manages references internally. The collection type itself is NOT a Neatoo type, so it uses `PlainOptions`.
   - Nullable types (`T?`) -- unwrap via `Nullable.GetUnderlyingType()`
   - Primitive types (`int`, `string`, `bool`) -- not Neatoo types, use `PlainOptions`. These have always worked because they don't use parameterized constructors with `$ref`.

2. **Parameters serialized via `ToObjectJson`.** The `ToObjectJson` method calls `Serialize(target)` (no type parameter), which will use `target.GetType()`. For record parameters passed to Interface Factory methods, this is correct -- they are serialized without `$id`/`$ref`. However, these parameters are also deserialized server-side via `FromObjectJson`, which calls `Deserialize(json, type)`. Both sides must agree on whether reference handling is used. Since `IsNeatooType` is deterministic based on the type, they will agree.

3. **Breaking change to wire format.** Non-Neatoo types that previously had `$id`/`$ref` in their JSON will no longer have it. Per Clarification A4, this is acceptable (v0, full breaking change tolerance). The `$id`/`$ref` for these types was not useful anyway -- it caused failures rather than enabling features.

4. **Two `JsonSerializerOptions` memory cost.** Minimal impact. `JsonSerializerOptions` caches type metadata internally, so there will be two caches. This is expected and acceptable.

5. **NeatooInterfaceJsonConverterFactory shared across both options.** The factory is registered as `Transient` in DI, but the `NeatooJsonSerializer` constructor receives the factories via `IEnumerable<NeatooJsonConverterFactory>` and adds the same instances to both options. The `CanConvert` method on these factories only returns `true` for interface/abstract types in `IServiceAssemblies`, so they are inert for non-Neatoo types in `PlainOptions`. However, the fact that they are added to `PlainOptions.Converters` means STJ will call `CanConvert` on them for each type -- this is a minor performance consideration but is consistent with the existing behavior.

---

## Architectural Verification

**Scope Table:**

| Pattern/Feature | Affected? | Current Status | After Fix |
|----------------|-----------|---------------|-----------|
| Interface Factory returning record (primary ctor) | YES | Broken ($ref error) | Fixed (PlainOptions, no $ref) |
| Interface Factory returning class (public setters) | NO (regression check) | Working | Unchanged (PlainOptions, no $ref -- but was working with $ref too) |
| Interface Factory returning primitive/collection | NO (regression check) | Working | Unchanged |
| [Factory]-decorated record (ordinal) | NO | Working | Unchanged (uses NeatooOptions, IOrdinalSerializable) |
| [Factory]-decorated class (ordinal) | NO | Working | Unchanged (uses NeatooOptions, IOrdinalSerializable) |
| [Factory]-decorated class (named format) | NO | Working | Unchanged (uses NeatooOptions, ReferenceHandler active) |
| Neatoo interface serialization ($type/$value) | NO | Working | Unchanged (NeatooInterfaceJsonConverterFactory handles) |

**Verification Evidence:**
- Interface Factory returning record: Needs Implementation (no test exists today)
- All other patterns: Verified by existing passing tests

**Breaking Changes:** Yes -- JSON wire format changes for non-Neatoo types (no `$id`/`$ref` emitted). Acceptable per Clarification A4 (v0).

**Codebase Analysis:**
- `NeatooJsonSerializer.cs`: 343 lines, well-structured. Adding a second options instance and type classification is clean.
- `HandleRemoteDelegateRequest.cs`: Already extracts `returnType` and passes it to `Serialize(result, returnType)`. No changes needed.
- `MakeRemoteDelegateRequest.cs`: Calls `DeserializeRemoteResponse<T>` which calls `Deserialize<T>`. No changes needed.
- Converter chain: All converters use `CanConvert` to self-select. Adding them to PlainOptions is safe.

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Serializer Dual-Options | developer | Yes | Core runtime library change, focused scope (1 file) | None |
| Phase 2: Test Targets and Tests | developer | No (continue) | Same context needed, tests validate Phase 1 changes | Phase 1 |

**Parallelizable phases:** None. Phase 2 depends on Phase 1.

**Notes:** Both phases are small enough to fit in a single agent invocation. The developer can implement Phase 1, run existing tests to verify no regression, then implement Phase 2 and run all tests.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-20

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `NeatooJsonSerializer.Serialize(object? target, Type targetType)` calls `IsNeatooType(targetType)`. For a plain record (no `[Factory]`), `typeof(IOrdinalSerializable).IsAssignableFrom(recordType)` is `false` and `recordType.IsInterface` is `false`, so `IsNeatooType` returns `false`. Method selects `PlainOptions` (no `ReferenceHandler`). STJ serializes without `$id`/`$ref`. | JSON output has no `$id`/`$ref` | Yes | Verified: `HandleRemoteDelegateRequest.cs:141` calls `serializer.Serialize(result, returnType)` where `returnType` is unwrapped from `Task<T>` at line 132. The unwrapped type for a record is the concrete record type. |
| 2 | `NeatooJsonSerializer.Serialize(object? target, Type targetType)` calls `IsNeatooType(targetType)`. For a Neatoo type implementing `IOrdinalSerializable`, `typeof(IOrdinalSerializable).IsAssignableFrom(neatooType)` is `true`, so `IsNeatooType` returns `true`. Method selects `Options` (with `ReferenceHandler = NeatooReferenceHandler`). For interface-typed Neatoo properties, `NeatooInterfaceJsonConverterFactory.CanConvert` returns `true` (line 23: `typeToConvert.IsInterface && !typeToConvert.IsGenericType && serviceAssemblies.HasType(typeToConvert)`), routing to `NeatooInterfaceJsonTypeConverter` which handles `$type`/`$value` wrapping and `$id`/`$ref` manually (lines 44-48). For ordinal format, `NeatooOrdinalConverterFactory.CanConvert` returns `true` (line 69: `typeof(IOrdinalSerializable).IsAssignableFrom(typeToConvert)`), writing arrays. | Reference handling works as before (ordinal arrays or named with `$id`/`$ref`) | Yes | Both converter paths are unchanged. The `Options` instance is identical to the current single instance. |
| 3 | Server: `HandleRemoteDelegateRequest` at line 141 calls `serializer.Serialize(result, returnType)` where `returnType` is the unwrapped record type. `IsNeatooType(recordType)` returns `false` (not `IOrdinalSerializable`, not interface/abstract). Uses `PlainOptions` -- STJ serializes with parameterized ctor properties as standard JSON. Client: `MakeRemoteDelegateRequest.ForDelegateNullable<T>` at line 93 calls `DeserializeRemoteResponse<T>` which calls `Deserialize<T>(json)`. `IsNeatooType(typeof(T))` returns `false` for the record. Uses `PlainOptions` -- STJ deserializes via parameterized constructor without `$ref` metadata interference. | Full round-trip succeeds, record properties preserved | Yes | Symmetric classification on both sides ensures matching options. |
| 4 | Same path as Rule 3. The record type `record MyResult(string Name, IReadOnlyList<MyItem> Items)` is not `IOrdinalSerializable`, not interface/abstract. `IsNeatooType` returns `false`. With `PlainOptions` (no `ReferenceHandler`), STJ serializes `IReadOnlyList<MyItem>` as a standard JSON array. `MyItem` is also a plain record, so STJ handles it with built-in constructor-based serialization. No `$ref` metadata is emitted for collection elements. | Collection deserialized with all elements intact | Yes | STJ natively handles `IReadOnlyList<T>` and records with primary ctors when no `ReferenceHandler` is active. |
| 5 | Server: `NeatooJsonSerializer.Serialize(object? target, Type targetType)` at line 124 -- when `target == null`, returns `null` immediately (before `IsNeatooType` is even called). `HandleRemoteDelegateRequest` at line 141 creates `RemoteResponseDto(null)`. Client: `DeserializeRemoteResponse<T>` at line 323 checks `remoteResponse?.Json == null`, returns `default` (which is `null` for a nullable record). | Client receives null without error | Yes | The null path short-circuits before any options selection. No serialization/deserialization of the null value occurs. |
| 6 | Server: `Serialize(result, returnType)` where `returnType` is `ExampleDto` (a class). `IsNeatooType(typeof(ExampleDto))` returns `false` -- not `IOrdinalSerializable`, not interface/abstract. Uses `PlainOptions`. STJ serializes as standard JSON object (no `$id`/`$ref`). Client: `Deserialize<T>` where `T` is `ExampleDto`. `IsNeatooType` returns `false`. Uses `PlainOptions`. STJ deserializes via public setters. | Class deserialized correctly | Yes | Wire format changes (no `$id`/`$ref` where there was before), but deserialization still works because public-setter classes do not need `$ref` resolution. Acceptable per Clarification A4. |
| 7 | Server: `Serialize(result, returnType)` where `returnType` is a `[Factory]`-decorated record. `typeof(IOrdinalSerializable).IsAssignableFrom(recordType)` is `true` (generator implements `IOrdinalSerializable`). `IsNeatooType` returns `true`. Uses `Options` (with `ReferenceHandler`). `NeatooOrdinalConverterFactory.CanConvert` returns `true`, serializes as array. | Ordinal serialization works as before | Yes | The `Options` instance is identical to the current code. No change to the Neatoo type path. |
| 8 | Same path as Rule 2. All `[Factory]`-decorated classes implement `IOrdinalSerializable`. `IsNeatooType` returns `true`. `Options` (with `ReferenceHandler`) is used. Custom converters handle `$id`/`$ref`/`$type`/`$value` as before. | Full round-trip with reference handling works | Yes | No change to the Neatoo type serialization path. |

### Concerns

**Non-blocking observations (no changes to plan required):**

1. **`HasType` semantics vs. plan description.** The plan's `IsNeatooType` description says `serviceAssemblies.HasType(underlying)` checks if the type is "recognized by NeatooInterfaceJsonConverterFactory (has entries in IServiceAssemblies)." The actual `HasType` implementation (`ServiceAssemblies.cs:49`) checks `this.Assemblies.Contains(type.Assembly)` -- it checks whether the type's assembly is registered, not whether the specific type is a Neatoo type. However, this does not affect correctness because the `IsNeatooType` check guards the `HasType` call with `underlying.IsInterface || underlying.IsAbstract`. Plain record types are concrete, so they never reach the `HasType` check. The `IsNeatooType` classification is correct for the problem being solved.

2. **Generic interface Neatoo types.** The plan's `IsNeatooType` checks `(underlying.IsInterface || underlying.IsAbstract) && serviceAssemblies.HasType(underlying)` but does not include the `!underlying.IsGenericType` check that `NeatooInterfaceJsonConverterFactory.CanConvert` includes (line 23). This means a generic Neatoo interface would be classified as Neatoo by `IsNeatooType` (using `Options` with `ReferenceHandler`) but NOT matched by the converter's `CanConvert`, falling through to STJ built-in handling with `ReferenceHandler.Preserve` active. In practice this is unlikely to occur -- Interface Factory return types are not generic interfaces -- and the behavior is no worse than the current code (which also uses `ReferenceHandler.Preserve` for everything). Noting for completeness only.

3. **`ToObjectJson`/`FromObjectJson` parameter serialization.** The plan correctly identifies (Risk 2) that `ToObjectJson` calls `Serialize(target)` (no type parameter), which the plan modifies to use `IsNeatooType(target.GetType())`. The server-side `FromObjectJson` calls `Deserialize(json, type)` where `type` comes from `serviceAssemblies.FindType()`. Both sides will use `IsNeatooType` on the same concrete type, so classification is symmetric. No issue.

4. **Agent Phasing is practical.** Both phases are small enough for a single agent invocation. Phase 2 depends on Phase 1. The "No (continue)" decision for Phase 2 is correct -- the developer needs the Phase 1 context to write tests against the changes.

**Verdict: Approved.** The plan is complete, correct, and implementable. All assertion traces produce results consistent with the business rules. The design is minimal and surgical -- one file modified, two new test files created. The dual-options approach is clean and the `IsNeatooType` classification is correct for all traced scenarios.

---

## Implementation Contract

**Created:** 2026-03-20
**Approved by:** developer agent (remotefactory-developer)

### Verification Acceptance Criteria

No pre-existing failing acceptance criteria from the architect. All existing tests must continue to pass.

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1 | `InterfaceFactoryRecordSerializationTests.InterfaceFactory_SimpleRecord_RoundTrip` | Simple record with primary ctor through client-server |
| 2 | `InterfaceFactoryRecordSerializationTests.InterfaceFactory_RecordWithCollection_RoundTrip` | Record containing `IReadOnlyList<T>` of records |
| 3 | `InterfaceFactoryRecordSerializationTests.InterfaceFactory_NestedRecord_RoundTrip` | Record containing another record as a property |
| 4 | `InterfaceFactoryRecordSerializationTests.InterfaceFactory_NullableRecord_ReturnsNull` | Nullable record method returns null |
| 5 | Covered by existing `InterfaceFactoryTests` (bool, List<string>) + no-regression gate | ExampleDto class regression covered by existing Design tests |
| 6 | Covered by existing `RecordSerializationTests` (all passing) | [Factory]-decorated record regression |
| 7 | Covered by existing round-trip tests (RemoteFetchRoundTripTests, etc.) | [Factory]-decorated class regression |
| 8 | `InterfaceFactoryRecordSerializationTests.InterfaceFactory_NonNeatooType_NoRefMetadata` | Direct JSON inspection for absence of `$id`/`$ref` |

### In Scope

- [ ] `src/RemoteFactory/Internal/NeatooJsonSerializer.cs`: Add `PlainOptions` field, `IsNeatooType(Type)` method, modify all 4 Serialize/Deserialize methods to select options
- [ ] `src/Tests/RemoteFactory.IntegrationTests/TestTargets/TypeSerialization/InterfaceFactoryRecordTargets.cs`: New file with record types, `IRecordReturnService` interface factory, and server implementation
- [ ] `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs`: New file with 5+ tests covering scenarios 1-4 and 8
- [ ] Checkpoint: After Phase 1, run `dotnet test` on full solution -- all existing tests must pass
- [ ] Checkpoint: After Phase 2, run `dotnet test` on full solution -- all existing + new tests must pass on both net9.0 and net10.0

### Out of Scope

- `NeatooReferenceHandler.cs` -- No changes
- `NeatooReferenceResolver.cs` -- No changes
- `NeatooInterfaceJsonConverterFactory.cs` -- No changes
- `NeatooInterfaceJsonTypeConverter.cs` -- No changes
- `NeatooOrdinalConverterFactory.cs` -- No changes
- `HandleRemoteDelegateRequest.cs` -- No changes
- `MakeRemoteDelegateRequest.cs` -- No changes
- Generator code -- No changes
- All existing test files -- Must NOT be modified
- Documentation files (CLAUDE-DESIGN.md, Design project examples, release notes) -- Handled in Step 9

### Verification Gates

1. After Phase 1 (serializer changes): `dotnet test src/Neatoo.RemoteFactory.sln` -- all existing tests pass (zero failures)
2. After Phase 2 (new tests): `dotnet test src/Neatoo.RemoteFactory.sln` -- all existing + new tests pass on both net9.0 and net10.0
3. Final: Full solution builds and tests pass with zero warnings from new code

### Stop Conditions

If any occur, STOP and report:
- Any existing test failure after Phase 1 changes (indicates regression in serializer)
- `NeatooInterfaceJsonConverterFactory` or `NeatooOrdinalConverterFactory` behavior changes (converters must remain untouched)
- Out-of-scope test failure
- Architectural contradiction discovered (e.g., a Neatoo type that does not implement `IOrdinalSerializable` and is not interface/abstract)

---

## Implementation Progress

**Started:** 2026-03-20
**Developer:** remotefactory-developer

### Phase 1: Serializer Dual-Options (Core Fix)

- [x] Added `PlainOptions` field to `NeatooJsonSerializer` -- a second `JsonSerializerOptions` without `ReferenceHandler`, sharing the same `TypeInfoResolver` and converter chain
- [x] Added `IsNeatooType(Type type)` private method with nullable unwrapping, `IOrdinalSerializable` check, and interface/abstract + `serviceAssemblies.HasType` check
- [x] Updated `Serialize(object? target)` to select options based on `target.GetType()`
- [x] Updated `Serialize(object? target, Type targetType)` to select options based on `targetType`
- [x] Updated `Deserialize<T>(string? json)` to select options based on `typeof(T)`
- [x] Updated `Deserialize(string? json, Type type)` to select options based on `type`
- [x] Fixed CA1851 (multiple enumeration of IEnumerable) by materializing `neatooJsonConverterFactories` to a list before iterating twice
- [x] Build: 0 errors, 0 new warnings
- [x] All existing tests pass: 490 unit (net9.0) + 490 unit (net10.0) + 481 integration (net9.0) + 481 integration (net10.0)

### Phase 2: Test Targets and Tests

- [x] Created `InterfaceFactoryRecordTargets.cs` with 5 record types, `IRecordReturnService` interface factory, and `RecordReturnService` server implementation
- [x] Created `InterfaceFactoryRecordSerializationTests.cs` with 6 tests covering all scenarios:
  - `InterfaceFactory_SimpleRecord_RoundTrip` (Scenario 1, Rules 1/3)
  - `InterfaceFactory_RecordWithCollection_RoundTrip` (Scenario 2, Rules 1/3/4)
  - `InterfaceFactory_NestedRecord_RoundTrip` (Scenario 3, Rules 1/3)
  - `InterfaceFactory_NullableRecord_ReturnsNull` (Scenario 4, Rule 5)
  - `InterfaceFactory_NullableRecord_ReturnsValue` (additional coverage for non-null nullable path)
  - `InterfaceFactory_NonNeatooType_NoRefMetadata` (Scenario 8, Rule 1 -- direct JSON inspection)
- [x] All tests pass on both net9.0 and net10.0

---

## Completion Evidence

**Reported:** 2026-03-20

- **Tests Passing:**
  - `RemoteFactory.UnitTests.dll (net9.0)`: 490 passed, 0 failed
  - `RemoteFactory.UnitTests.dll (net10.0)`: 490 passed, 0 failed
  - `RemoteFactory.IntegrationTests.dll (net9.0)`: 487 passed, 0 failed, 3 skipped (pre-existing ShowcasePerformanceTests)
  - `RemoteFactory.IntegrationTests.dll (net10.0)`: 487 passed, 0 failed, 3 skipped (pre-existing ShowcasePerformanceTests)
  - Integration test count increased from 481 to 487 (6 new tests added)
- **Verification Resources Pass:** N/A (no Design project changes in implementation scope)
- **All Contract Items:** Confirmed 100% complete

### Contract Checklist

- [x] `src/RemoteFactory/Internal/NeatooJsonSerializer.cs`: `PlainOptions` field, `IsNeatooType(Type)` method, all 4 Serialize/Deserialize methods updated
- [x] `src/Tests/RemoteFactory.IntegrationTests/TestTargets/TypeSerialization/InterfaceFactoryRecordTargets.cs`: New file with record types, `IRecordReturnService`, and `RecordReturnService`
- [x] `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs`: New file with 6 tests covering scenarios 1-4 and 8
- [x] Checkpoint 1 (after Phase 1): All existing tests pass -- 0 failures
- [x] Checkpoint 2 (after Phase 2): All existing + new tests pass on both net9.0 and net10.0 -- 0 failures

### Out-of-Scope Files NOT Modified

- `NeatooReferenceHandler.cs` -- Not modified
- `NeatooReferenceResolver.cs` -- Not modified
- `NeatooInterfaceJsonConverterFactory.cs` -- Not modified
- `NeatooInterfaceJsonTypeConverter.cs` -- Not modified
- `NeatooOrdinalConverterFactory.cs` -- Not modified
- `HandleRemoteDelegateRequest.cs` -- Not modified
- `MakeRemoteDelegateRequest.cs` -- Not modified
- Generator code -- Not modified
- All existing test files -- Not modified
- Documentation files -- Not modified (handled in Step 9)

### Stop Conditions Encountered

None. No existing test failures, no out-of-scope modifications needed, no architectural contradictions discovered.

---

## Documentation

**Agent:** business-requirements-documenter, documentation-writer, remotefactory-developer
**Completed:** 2026-03-20
**Status:** Complete (all deliverables fulfilled)

### Files Directly Updated

1. **`src/Design/CLAUDE-DESIGN.md`:**
   - Added Anti-Pattern 9: Mixing Neatoo types with records in Interface Factory return types (per Clarification A3, Gap 2). Includes WRONG/RIGHT examples and explanation of the serialization mismatch.
   - Added item 10 to Common Mistakes summary: "Mixing Neatoo types with records in Interface Factory return types"
   - Added Quick Decisions entry: "Can Interface Factory return a record?" -- Yes, plain records/DTOs without Neatoo types
   - Added Design Completeness Checklist item: "Interface Factory returning a record type" (unchecked -- awaiting Design project example)

2. **`docs/serialization.md`:**
   - Added "Scope: Neatoo Types Only" subsection under Reference Preservation. Documents that `$id`/`$ref` applies only to Neatoo types, plain records/DTOs are serialized without reference handling, and mixing the two in a single return type is an anti-pattern.

### Developer Deliverables

These require source code changes and cannot be made by the documentation agent.

1. **[COMPLETED]** **`src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs`:** Added `ExampleRecordResult` record type with primary constructor `(int Id, string Name)`. Added `GetRecordByIdAsync(int id)` method to `IExampleRepository` interface returning `Task<ExampleRecordResult?>`. Added implementation in `ExampleRepository` and `NullReturningRepository`. Checked off the Design Completeness Checklist item in `CLAUDE-DESIGN.md`.

2. **[COMPLETED]** **`src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs`:** Added `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` test demonstrating the Interface Factory record return type pattern works through a full client-server round-trip using `DesignClientServerContainers.Scopes()`. Test passes on both net9.0 and net10.0.

3. **[COMPLETED]** **Skill updates:** Updated `skills/RemoteFactory/references/interface-factory.md` with a "Record Return Types" section showing hand-written examples of records as Interface Factory return types plus the anti-pattern note. Updated `skills/RemoteFactory/references/anti-patterns.md` with Anti-Pattern 14: Mixing Neatoo Types with Records in Interface Factory Return Types, and added it to the Summary Table. No mdsnippets run needed -- all additions are hand-written content (anti-pattern examples and guidance), not compiled code snippets.

4. **Release notes:** Create `docs/release-notes/vX.Y.Z.md` documenting this as a bug fix (patch version bump). The fix resolves `ObjectWithParameterizedCtorRefMetadataNotSupported` errors when Interface Factory methods return record types with primary constructors.

### General Documentation (documentation-writer)

**Agent:** documentation-writer
**Completed:** 2026-03-20

1. **`docs/release-notes/v0.21.3.md`:** Created release notes for patch version 0.21.3. Documents the bug fix, wire format breaking change for non-Neatoo types, migration guide, and commit summary.
2. **`docs/release-notes/index.md`:** Added v0.21.3 to both the Highlights table and All Releases list.
3. **`docs/release-notes/v0.21.2.md`:** Bumped nav_order from 1 to 2 to make room for v0.21.3.
4. **`src/Directory.Build.props`:** Updated `PackageVersion` from `0.21.2` to `0.21.3`.

---

## Architect Verification

**Verified:** 2026-03-20
**Verdict:** VERIFIED

**Independent build results:**
- `dotnet build src/Neatoo.RemoteFactory.sln`: 0 errors, 1 warning (pre-existing Blazor WASM NativeFileReference warning, unrelated)

**Independent test results:**
- `RemoteFactory.UnitTests.dll (net9.0)`: 490 passed, 0 failed, 0 skipped
- `RemoteFactory.UnitTests.dll (net10.0)`: 490 passed, 0 failed, 0 skipped
- `RemoteFactory.IntegrationTests.dll (net9.0)`: 487 passed, 0 failed, 3 skipped (pre-existing ShowcasePerformanceTests)
- `RemoteFactory.IntegrationTests.dll (net10.0)`: 487 passed, 0 failed, 3 skipped (pre-existing ShowcasePerformanceTests)

**Design match:** Yes -- implementation matches the original plan in all respects.

1. **`PlainOptions` field (lines 98-113):** Second `JsonSerializerOptions` without `ReferenceHandler`, shares `TypeInfoResolver` and converter chain. The developer also addressed CA1851 by materializing `neatooJsonConverterFactories` to a list before iterating twice (line 74). Matches plan.
2. **`IsNeatooType(Type type)` method (lines 123-136):** Nullable unwrapping via `Nullable.GetUnderlyingType`, `IOrdinalSerializable.IsAssignableFrom` check, then `IsInterface || IsAbstract` with `serviceAssemblies.HasType` check. Exact match to plan's design detail.
3. **All four Serialize/Deserialize methods updated:** Each selects options via `IsNeatooType` and guards `NeatooReferenceResolver` setup with `if (options == this.Options)`. Matches plan.
4. **Test targets (`InterfaceFactoryRecordTargets.cs`):** 5 record types, `[Factory] IRecordReturnService` interface, `RecordReturnService` implementation. Matches plan.
5. **Test file (`InterfaceFactoryRecordSerializationTests.cs`):** 6 tests covering scenarios 1-4, 8, plus bonus non-null nullable path. Uses `ClientServerContainers.Scopes` with `configureServer` for server-side DI registration. Matches plan with additional coverage.
6. **Scope discipline:** `git status` confirms only `NeatooJsonSerializer.cs` modified, 2 new test files added. No out-of-scope files touched (no converters, no handlers, no generator code, no existing test files).

**Issues found:** None

---

## Requirements Verification

**Reviewer:** business-requirements-reviewer
**Verified:** 2026-03-20
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Serialization Architecture -- NeatooJsonSerializer applies ReferenceHandler.Preserve globally (root cause) | Satisfied | Verified in `NeatooJsonSerializer.cs`: `Options` (line 76-82) retains `ReferenceHandler = this.ReferenceHandler` for Neatoo types. New `PlainOptions` (lines 98-113) omits `ReferenceHandler`. The `IsNeatooType` method (lines 123-136) correctly routes types. Non-Neatoo types no longer receive `$id`/`$ref` metadata. |
| Custom Converter Chain -- NeatooOrdinalConverterFactory and NeatooInterfaceJsonConverterFactory unchanged | Satisfied | Verified: `NeatooOrdinalConverterFactory.cs` and `NeatooInterfaceJsonConverterFactory.cs` were not modified (confirmed via git status and file reading). Both converters are added to `PlainOptions` (lines 105-113) but are inert for non-Neatoo types because their `CanConvert` methods return `false` for plain records. |
| NeatooInterfaceJsonTypeConverter manages $id/$ref manually (lines 44-48) | Satisfied | Verified: `NeatooInterfaceJsonTypeConverter.cs` was not modified. It calls `options.ReferenceHandler!.CreateResolver().AddReference(id, result)` at line 46-47. This converter only runs for interface/abstract types matched by `NeatooInterfaceJsonConverterFactory.CanConvert` (line 23: `typeToConvert.IsInterface && !typeToConvert.IsGenericType && serviceAssemblies.HasType(typeToConvert)`). All such types pass `IsNeatooType` and route to `Options` (which has `ReferenceHandler`), so the converter always has a valid `ReferenceHandler` available. |
| NeatooOrdinalConverterFactory handles [Factory]-decorated records via IOrdinalSerializable | Satisfied | Verified: `NeatooOrdinalConverterFactory.CanConvert` (line 60-69) checks `typeof(IOrdinalSerializable).IsAssignableFrom(typeToConvert)`. `IsNeatooType` also checks `IOrdinalSerializable.IsAssignableFrom` first (line 128), so all ordinal types route to `Options`. No change to ordinal serialization path. |
| Interface Factory return types can be arbitrary non-Neatoo types (AllPatterns.cs:204-220) | Satisfied | Verified: The implementation makes this pattern work for records with parameterized constructors, not just classes with public setters. `IsNeatooType` returns `false` for concrete non-Neatoo types (records and classes alike), routing them to `PlainOptions`. Test `InterfaceFactory_SimpleRecord_RoundTrip` demonstrates the record round-trip through `ClientServerContainers.Scopes`. |
| HandleRemoteDelegateRequest extracts returnType and calls Serialize(result, returnType) | Satisfied | Verified: `HandleRemoteDelegateRequest.cs` was not modified (confirmed via git status). Line 141 calls `serializer.Serialize(result, returnType)` where `returnType` is unwrapped from `Task<T>` at lines 130-133. The serializer now classifies this via `IsNeatooType(targetType)`. |
| MakeRemoteDelegateRequest calls DeserializeRemoteResponse<T> on client | Satisfied | Verified: `MakeRemoteDelegateRequest.cs` was not modified. Client-side deserialization calls `Deserialize<T>(json)` which now uses `IsNeatooType(typeof(T))` -- symmetric classification with the server side. |
| Existing RecordSerializationTests (ordinal format) must continue to pass | Satisfied | Completion evidence: 487 integration tests pass on both net9.0 and net10.0. [Factory]-decorated records implement `IOrdinalSerializable`, so `IsNeatooType` returns `true`, routing to `Options` with full reference handling -- identical to pre-change behavior. |
| Existing InterfaceFactoryTests (bool, List<string>) must continue to pass | Satisfied | Completion evidence: 487 integration tests pass. Primitive and collection types are not Neatoo types, so they route to `PlainOptions`. These types never had parameterized constructor issues, so removing `$id`/`$ref` is a wire format change only (acceptable per Clarification A4). |
| Design Debt -- no deliberately deferred features implemented | Satisfied | Verified: The 5 design debt items in `CLAUDE-DESIGN.md` (lines 685-691) are: private setters, OR logic for AspAuthorize, automatic Remote detection, collection factory injection, IEnumerable<T> serialization. None are implemented by this change. The fix is scoped to the serializer's options selection logic. |
| Anti-Patterns 1-8 -- no violations | Satisfied | Verified: All 8 anti-patterns in `CLAUDE-DESIGN.md` (lines 156-372) are unrelated to serializer options selection. The new test targets follow correct patterns: `[Factory]` on the interface, no operation attributes on interface methods, `public` interface with server implementation. |

### Unintended Side Effects

1. **Wire format change for non-Neatoo types (acceptable).** Non-Neatoo types (e.g., `ExampleDto` class returned from Interface Factories) previously had `$id`/`$ref` in their JSON. After this change, they do not. This is a breaking wire format change. Per Clarification A4, this is acceptable (v0, full breaking change tolerance). The `$id`/`$ref` metadata was never useful for these types and caused failures for records with parameterized constructors.

2. **ToObjectJson/FromObjectJson parameter path (no issue).** `ToObjectJson` (line 329-339) calls `this.Serialize(target)`, which now uses `IsNeatooType(target.GetType())`. `FromObjectJson` (line 401-411) calls `this.Deserialize(objectTypeJson.Json, targetType)`, which uses `IsNeatooType(type)`. Both sides classify the same concrete type identically, so serialization/deserialization are symmetric. For record parameters passed to Interface Factory methods, both sides will use `PlainOptions`. For Neatoo types, both sides will use `Options`. No issue.

3. **PlainOptions shares TypeInfoResolver with Options (no issue).** The `NeatooJsonTypeInfoResolver` overrides `CreateObject` for DI-registered types. For non-Neatoo record types, the resolver does not find them in DI and falls back to STJ's default constructor-based deserialization. This is the correct behavior for records with primary constructors.

4. **NeatooInterfaceJsonConverterFactory registered in PlainOptions (no issue).** The factory is added to `PlainOptions.Converters` (lines 110-113), but its `CanConvert` method only returns `true` for non-generic interface/abstract types in registered assemblies. Plain records are concrete types and never match. The factory is inert in `PlainOptions`. Minor performance cost from STJ calling `CanConvert` on each type, consistent with existing behavior.

5. **HasType semantics (no issue).** `ServiceAssemblies.HasType` (line 49) checks assembly membership, not specific type identity. However, `IsNeatooType` guards the `HasType` call with `underlying.IsInterface || underlying.IsAbstract` (line 132), so concrete record types in registered assemblies never reach the `HasType` check. Classification is correct.

### Issues Found

None. The implementation satisfies all documented requirements, follows the established serialization architecture, does not violate any anti-pattern, and does not implement any deliberately deferred feature. The scope is minimal (one production file modified, two new test files) and all existing tests pass on both net9.0 and net10.0.
