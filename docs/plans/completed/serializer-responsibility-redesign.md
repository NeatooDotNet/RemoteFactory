# Serializer Responsibility Redesign

**Date:** 2026-03-20
**Related Todo:** [serializer-responsibility-redesign](../todos/serializer-responsibility-redesign.md)
**Status:** Requirements Documented
**Last Updated:** 2026-03-20

---

## Overview

Remove `ReferenceHandler` from `JsonSerializerOptions` entirely and provide `NeatooReferenceResolver` through a static `AsyncLocal` accessor. Delete `IsNeatooType()`, `PlainOptions`, and `NeatooReferenceHandler`. Return to a single `JsonSerializerOptions` instance. Converters that need reference tracking access the resolver directly via the `AsyncLocal` accessor instead of through `options.ReferenceHandler`.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/serializer-responsibility-redesign.md#requirements-review)

### Relevant Existing Requirements

#### Serialization Architecture

- **`NeatooReferenceHandler` already uses `AsyncLocal` (`NeatooReferenceHandler.cs:8`):** The proposed `AsyncLocal` accessor on `NeatooReferenceResolver` is an evolution of the existing pattern, not a new mechanism. -- Relevance: validates the approach.

- **Only one RemoteFactory converter accesses `options.ReferenceHandler` (`NeatooInterfaceJsonTypeConverter.cs:46`):** The `Read()` method calls `options.ReferenceHandler!.CreateResolver().AddReference(id, result)`. However, as discovered during analysis, the `id` variable is initialized to `string.Empty` and never assigned, so this call is dead code -- the guard `if (!string.IsNullOrEmpty(id))` always prevents execution. -- Relevance: the only RemoteFactory call site is dead code, simplifying the migration.

- **Downstream Neatoo converters are the primary consumer of `ReferenceHandler` (`isneatootype-missing-validatebase-check.md:32-33`):** Neatoo's `NeatooBaseJsonTypeConverter.Write()` calls `options.ReferenceHandler.CreateResolver().GetReference(value, out var alreadyExists)`. These call sites must migrate to the new `AsyncLocal` accessor.

- **`NeatooJsonSerializer` constructor builds two `JsonSerializerOptions` (`NeatooJsonSerializer.cs:76-113`):** The `Options` set has `ReferenceHandler`, `PlainOptions` does not. The redesign deletes `PlainOptions` and removes `ReferenceHandler` from `Options`, returning to a single options set.

- **`NeatooJsonTypeInfoResolver` is independent of `ReferenceHandler` (`NeatooJsonTypeInfoResolver.cs:24-39`):** This resolver provides DI-aware deserialization by overriding `CreateObject` for registered service types. It is unaffected by the redesign.

- **`NeatooJsonConverterFactory` is the DI extension point for downstream converters (`NeatooJsonConverterFactory.cs`):** Neatoo registers its own converter factories through this abstract base class. After the redesign, downstream converters must access the resolver via the `AsyncLocal` accessor rather than `options.ReferenceHandler`.

#### Anti-Patterns and Design Rules

- **Anti-Pattern 9 (`CLAUDE-DESIGN.md:378-419`):** "Do not mix Neatoo types with records in Interface Factory return types." The user-facing rule remains valid. The technical explanation changes: converters for Neatoo types add `$id`/`$ref` using the resolver directly; STJ's native record handling cannot process `$ref` in parameterized constructors. The rule itself is unchanged; only the mechanism description needs updating.

- **Quick Decisions Table record entry (`CLAUDE-DESIGN.md:153`):** "Records are serialized without `$id`/`$ref`; do not mix Neatoo types into record properties." Remains valid -- converters decide, not the serializer.

#### Existing Tests

- **Design serialization tests (`Design.Tests/FactoryTests/SerializationTests.cs`):** 6 tests verify Create, Fetch, value object, collection, nullable, and Save round-trips. Must pass after redesign.

- **Interface Factory record round-trip test (`Design.Tests/FactoryTests/InterfaceFactoryTests.cs:96-117`):** Validates `GetRecordByIdAsync()` returning an `ExampleRecordResult` record through client-server serialization. This is the scenario that prompted the original v0.21.3 fix. Must pass after redesign.

- **Interface Factory record serialization tests (`InterfaceFactoryRecordSerializationTests.cs`):** 6 tests including `InterfaceFactory_NonNeatooType_NoRefMetadata` which asserts that serialized JSON for non-Neatoo records contains no `$id` or `$ref` metadata. Must pass after redesign.

- **Interface collection serialization tests (`InterfaceCollectionSerializationTests.cs`):** 11 tests verifying round-trip serialization for single interface properties, concrete and interface collections with interface elements, nested structures, save operations, empty/null collections. Must pass after redesign.

- **Record serialization tests (`RecordSerializationTests.cs`):** 12 tests (plus 4 ClientServer tests) covering simple records, complex records with all primitive types, collections, nullables, nested records, equality preservation. Must pass after redesign.

### Gaps

1. **No documented contract for `AsyncLocal` resolver accessibility by third-party converters.** The architect must define visibility, thread safety, lifecycle, and null safety.

2. **No documented contract for `NeatooInterfaceJsonTypeConverter` reference access after redesign.** The current call site is dead code (see analysis above). The redesign must decide whether to remove it or fix it.

3. **No documented migration path for Neatoo's converter call sites.** The coordinated Neatoo update must migrate `options.ReferenceHandler.CreateResolver()` calls to the new `AsyncLocal` accessor.

4. **No test for interface-typed property reference preservation with the new accessor.** The existing tests exercise the full pipeline but do not independently verify `$id`/`$ref` registration for interface-typed properties via the `AsyncLocal` path.

### Contradictions

None found. The redesign aligns with all documented requirements and existing anti-patterns.

### Recommendations for Architect

1. Define the `AsyncLocal` accessor API precisely (visibility, null safety, lifecycle).
2. Preserve `NeatooReferenceResolver` lifecycle management in `NeatooJsonSerializer`.
3. Address the dead code in `NeatooInterfaceJsonTypeConverter.Read()` line 46.
4. Verify Anti-Pattern 9 remains valid.
5. Add a test for interface-typed property reference preservation.
6. Document the Neatoo migration path.
7. Keep the record round-trip bug visible.

---

## Business Rules (Testable Assertions)

### Serializer Configuration Rules

1. WHEN `NeatooJsonSerializer` is constructed, THEN `Options.ReferenceHandler` IS `null`. A single `JsonSerializerOptions` instance is used for all types. -- Source: Todo clarification A6 (approved direction), Requirements Review item 7

2. WHEN `NeatooJsonSerializer` is constructed, THEN only one `JsonSerializerOptions` instance exists (no `PlainOptions`). -- Source: Todo clarification A6, Requirements Review item 7

3. WHEN `NeatooJsonSerializer` is constructed, THEN `IsNeatooType()` method does not exist. -- Source: Todo problem statement items 1-3, Requirements Review item 8

### Resolver Lifecycle Rules

4. WHEN `NeatooJsonSerializer.Serialize()` is called, THEN a new `NeatooReferenceResolver` is created, assigned to the `AsyncLocal`, and disposed after serialization completes. -- Source: Requirements Review recommendation 2, existing pattern at `NeatooJsonSerializer.cs:156-158`

5. WHEN `NeatooJsonSerializer.Deserialize()` is called, THEN a new `NeatooReferenceResolver` is created, assigned to the `AsyncLocal`, and disposed after deserialization completes. -- Source: Requirements Review recommendation 2, existing pattern at `NeatooJsonSerializer.cs:229-230`

6. WHEN no serialization operation is in progress, THEN `NeatooReferenceResolver.Current` returns `null`. -- Source: NEW (Gap 1: null safety contract)

7. WHEN a serialization operation completes (normally or via exception), THEN the `AsyncLocal` resolver is cleared (set to `null`). -- Source: NEW (Gap 1: lifecycle contract)

### Resolver Access Rules

8. WHEN a converter needs reference tracking during serialization or deserialization, THEN it accesses the resolver via `NeatooReferenceResolver.Current` (static property). -- Source: Todo clarification A6 (approved direction), Requirements Review recommendation 1

9. WHEN `NeatooReferenceResolver.Current` is accessed from a converter and a serialization operation is in progress, THEN a non-null `NeatooReferenceResolver` is returned. -- Source: NEW (Gap 1: accessibility contract)

10. WHEN `NeatooReferenceResolver.Current` is a `public` static property, THEN downstream converters in separate assemblies (e.g., Neatoo) can access it. -- Source: Requirements Review recommendation 1, Gap 1

### Record Serialization Rules

11. WHEN a plain record type (no `[Factory]`, not implementing `IOrdinalSerializable`) is serialized via `NeatooJsonSerializer`, THEN the JSON output contains no `$id` or `$ref` metadata. -- Source: Anti-Pattern 9 (`CLAUDE-DESIGN.md:378-419`), Quick Decisions (`CLAUDE-DESIGN.md:153`), verified by `InterfaceFactory_NonNeatooType_NoRefMetadata` test

12. WHEN a plain record with a parameterized constructor is deserialized via `NeatooJsonSerializer`, THEN deserialization succeeds without `ObjectWithParameterizedCtorRefMetadataNotSupported` error. -- Source: Original v0.21.3 fix motivation, verified by `InterfaceFactory_SimpleRecord_RoundTrip` test

### Interface Type Serialization Rules

13. WHEN an interface-typed property is serialized, THEN the `NeatooInterfaceJsonTypeConverter.Write()` emits `$type` and `$value` wrapper properties (unchanged from current behavior). -- Source: Existing behavior at `NeatooInterfaceJsonTypeConverter.cs:83-102`

14. WHEN an interface-typed property is deserialized, THEN the `NeatooInterfaceJsonTypeConverter.Read()` reads `$type` and `$value`, resolves the concrete type, and deserializes the value (unchanged from current behavior). -- Source: Existing behavior at `NeatooInterfaceJsonTypeConverter.cs:22-81`

15. WHEN `NeatooInterfaceJsonTypeConverter.Read()` deserializes an interface-typed value, THEN it does NOT call `options.ReferenceHandler` (that code path is removed). -- Source: Dead code finding (see analysis), Requirements Review item 2

### Deletion Rules

16. WHEN the redesign is complete, THEN the file `NeatooReferenceHandler.cs` is deleted. -- Source: Todo approved direction (remove `ReferenceHandler` from options entirely)

17. WHEN the redesign is complete, THEN `NeatooJsonSerializer` has no `PlainOptions` property and no `IsNeatooType()` method. -- Source: Todo approved direction, Requirements Review items 7-8

### Backward Compatibility Rules

18. WHEN all existing serialization tests are run after the redesign, THEN they all pass. This includes Design project tests (6), interface factory record tests (6), interface collection tests (11), record serialization tests (16), and all other serialization-related tests. -- Source: Requirements Review items 11-12, existing test suites

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Construct serializer, verify no ReferenceHandler | `new NeatooJsonSerializer(...)` | 1, 2 | `Options.ReferenceHandler == null`, no `PlainOptions` property |
| 2 | Serialize plain record | `InterfaceRecordResult("Test", 42)` via `Serialize()` | 4, 11 | JSON has no `$id`/`$ref`; resolver created and disposed |
| 3 | Deserialize plain record with parameterized ctor | JSON `{"Name":"Test","Value":42}` via `Deserialize<InterfaceRecordResult>()` | 5, 12 | `InterfaceRecordResult` with Name="Test", Value=42; no exception |
| 4 | Serialize record with collection | `InterfaceRecordWithCollection` with 3 items | 4, 11 | JSON has no `$id`/`$ref`; all items present |
| 5 | Resolver is null outside serialization | Access `NeatooReferenceResolver.Current` with no operation active | 6 | Returns `null` |
| 6 | Resolver is non-null during serialization | Converter accesses `NeatooReferenceResolver.Current` during `Serialize()` | 8, 9 | Returns non-null `NeatooReferenceResolver` |
| 7 | Resolver is cleared after exception | `Serialize()` throws, then check `NeatooReferenceResolver.Current` | 7 | Returns `null` |
| 8 | Interface-typed property serialization | Object with `ITestChild` property, serialize via client/server | 13, 14 | JSON contains `$type`/`$value`; deserializes to correct concrete type |
| 9 | No reference handler in options | After construction, inspect `Options` | 1, 15 | `Options.ReferenceHandler` is `null`; converter does not access it |
| 10 | Full client-server round-trip with Class Factory type | `ExampleClassFactory.Create("Test")` via client container | 18 | Object survives round-trip with all properties preserved |
| 11 | Full client-server round-trip with Interface Factory record | `IExampleRepository.GetRecordByIdAsync(42)` via client container | 11, 12, 18 | Record survives round-trip without `$id`/`$ref` error |
| 12 | Interface collection round-trip | `List<ITestChild>` with 2 elements via client container | 13, 14, 18 | All elements survive with correct concrete types |
| 13 | NeatooReferenceHandler.cs is deleted | Check file system | 16 | File does not exist |
| 14 | IsNeatooType is deleted | Search `NeatooJsonSerializer.cs` for `IsNeatooType` | 17 | Method does not exist |

---

## Approach

The redesign follows the approved direction from the comprehension check (clarification A6): remove `ReferenceHandler` from `JsonSerializerOptions` entirely and provide the resolver via `AsyncLocal`.

**Key insight from codebase analysis:** The `NeatooInterfaceJsonTypeConverter.Read()` call to `options.ReferenceHandler` at line 46 is dead code. The `id` variable is initialized to `string.Empty` and never assigned, so the `AddReference` call is never reached. This means removing `ReferenceHandler` from `options` has zero behavioral impact on RemoteFactory's own converters. The only consumers that will notice are downstream Neatoo converters that access `options.ReferenceHandler.CreateResolver()` -- and those are the ones that need the coordinated migration.

**Strategy:**

1. Add a static `AsyncLocal`-based `Current` property to `NeatooReferenceResolver`
2. Modify `NeatooJsonSerializer` to set/clear `NeatooReferenceResolver.Current` instead of `NeatooReferenceHandler.ReferenceResolver.Value`
3. Remove `ReferenceHandler` from `JsonSerializerOptions`, delete `PlainOptions`, delete `IsNeatooType()`
4. Remove dead reference handler code from `NeatooInterfaceJsonTypeConverter.Read()`
5. Delete `NeatooReferenceHandler.cs`
6. All existing tests must pass without modification

---

## Design

### AsyncLocal Accessor on NeatooReferenceResolver

Add a static `AsyncLocal<NeatooReferenceResolver?>` property to the existing `NeatooReferenceResolver` class:

```
NeatooReferenceResolver (public sealed class)
    EXISTING: AddReference(), GetReference(), ResolveReference(), AlreadyExists(), Dispose()
    NEW: static AsyncLocal<NeatooReferenceResolver?> _current
    NEW: public static NeatooReferenceResolver? Current { get => _current.Value; internal set => _current.Value = value; }
```

Design decisions:
- **Getter is `public`** -- downstream converters (Neatoo) in separate assemblies must read it
- **Setter is `internal`** -- only `NeatooJsonSerializer` within RemoteFactory should set/clear it
- **Nullable return** -- `null` means no serialization in progress; callers must null-check
- **`AsyncLocal` semantics** -- each async flow gets its own resolver, safe for concurrent operations

### NeatooJsonSerializer Changes

**Constructor:** Remove `ReferenceHandler = this.ReferenceHandler` from `Options`. Delete `PlainOptions` entirely. Delete `ReferenceHandler` property. Delete `IsNeatooType()` method. Remove `IServiceAssemblies` dependency (only used by `IsNeatooType`). Single `Options` instance used for all serialization.

**Serialize/Deserialize methods:** Replace the conditional `IsNeatooType` branching:

```
BEFORE (each method):
    var options = IsNeatooType(type) ? this.Options : this.PlainOptions;
    if (options == this.Options) {
        using var rr = new NeatooReferenceResolver();
        this.ReferenceHandler.ReferenceResolver.Value = rr;
    }
    JsonSerializer.Serialize/Deserialize(... options);

AFTER (each method):
    using var rr = new NeatooReferenceResolver();
    NeatooReferenceResolver.Current = rr;
    try {
        JsonSerializer.Serialize/Deserialize(... this.Options);
    } finally {
        NeatooReferenceResolver.Current = null;
    }
```

Key change: the resolver is always created and always available. Whether a converter uses it is the converter's decision. Records and DTOs use no custom converter, so they serialize natively via STJ without any `$id`/`$ref` interference.

### NeatooInterfaceJsonTypeConverter.Read() Cleanup

Remove the dead reference handler code (lines 44-48):

```
BEFORE:
    if (!string.IsNullOrEmpty(id))
    {
        options.ReferenceHandler!.CreateResolver()
                         .AddReference(id, result);
    }

AFTER:
    (removed entirely -- id was never assigned, this code was dead)
```

The `var id = string.Empty;` declaration at line 32 is also removed.

### NeatooReferenceHandler.cs Deletion

The entire file is deleted. This class was a bridge between `JsonSerializerOptions.ReferenceHandler` and the `AsyncLocal<ReferenceResolver>`. With the new design, `NeatooReferenceResolver.Current` provides direct access, eliminating the need for this intermediate class.

### Files NOT Changed

- `NeatooOrdinalConverterFactory.cs` / `NeatooOrdinalConverter<T>` -- does not use `ReferenceHandler`
- `NeatooInterfaceJsonConverterFactory.cs` -- creates converters, does not use `ReferenceHandler`
- `NeatooJsonTypeInfoResolver.cs` -- independent of `ReferenceHandler`
- `NeatooJsonConverterFactory.cs` -- abstract base class, no `ReferenceHandler` access
- `AddRemoteFactoryServices.cs` -- DI registration unchanged
- All test files -- no test changes required; all tests must pass as-is

### Neatoo Coordinated Migration (Out of Scope for This Plan)

Neatoo's converters will need to migrate from `options.ReferenceHandler.CreateResolver()` to `NeatooReferenceResolver.Current`. This is a Neatoo-side change and is out of scope for this RemoteFactory plan. However, the migration path is:

```
BEFORE (Neatoo converter):
    var resolver = options.ReferenceHandler!.CreateResolver();
    var id = resolver.GetReference(value, out var alreadyExists);

AFTER (Neatoo converter):
    var resolver = NeatooReferenceResolver.Current;
    if (resolver != null) {
        var id = resolver.GetReference(value, out var alreadyExists);
        // ... reference handling logic
    }
```

The null check is important: when Neatoo types are serialized outside of `NeatooJsonSerializer` (e.g., in unit tests using bare STJ), `Current` will be `null`. Neatoo's converters should gracefully skip reference handling in that case.

---

## Implementation Steps

### Phase 1: Add AsyncLocal Accessor to NeatooReferenceResolver

1. Add `private static readonly AsyncLocal<NeatooReferenceResolver?> _current = new();` field
2. Add `public static NeatooReferenceResolver? Current` property with `public` getter and `internal` setter
3. Verify build succeeds

### Phase 2: Simplify NeatooJsonSerializer

1. Remove `PlainOptions` property and its constructor initialization (lines 98-113)
2. Remove `ReferenceHandler = this.ReferenceHandler` from `Options` construction (line 78)
3. Remove `private NeatooReferenceHandler ReferenceHandler { get; }` property (line 43)
4. Remove `IsNeatooType()` method (lines 123-136)
5. Remove `IServiceAssemblies serviceAssemblies` field (line 31) -- only used by `IsNeatooType`. Note: keep the `serviceAssemblies` parameter in the constructor if it is used elsewhere (check `FromObjectJson` and `DeserializeRemoteDelegateRequest`). **Update:** `serviceAssemblies` is used in `DeserializeRemoteDelegateRequest()` line 364 and `FromObjectJson()` line 408. The field must be kept.
6. Update all four `Serialize`/`Deserialize` method bodies to use the new pattern:
   - Remove `IsNeatooType` branching
   - Always create `NeatooReferenceResolver`, set `NeatooReferenceResolver.Current`, use `try/finally` to clear
   - Use single `this.Options` for all calls
7. Remove `PlainOptions`-related comments
8. Verify build succeeds

### Phase 3: Clean Up NeatooInterfaceJsonTypeConverter

1. Remove `var id = string.Empty;` (line 32)
2. Remove the dead reference handler block (lines 44-48)
3. Remove `options.ReferenceHandler` -- the converter no longer accesses `ReferenceHandler` at all
4. Verify build succeeds

### Phase 4: Delete NeatooReferenceHandler

1. Delete `src/RemoteFactory/Internal/NeatooReferenceHandler.cs`
2. Verify build succeeds

### Phase 5: Run All Tests

1. Run full test suite: `dotnet test src/Neatoo.RemoteFactory.sln`
2. All tests must pass
3. Pay special attention to:
   - `InterfaceFactory_NonNeatooType_NoRefMetadata` -- verifies no `$id`/`$ref` for records
   - `InterfaceFactory_SimpleRecord_RoundTrip` -- verifies record round-trip works
   - All `InterfaceCollectionSerializationTests` -- verifies interface type serialization
   - All `SerializationTests` in Design project -- verifies Class Factory round-trips
   - `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` -- the original v0.21.3 test case

---

## Acceptance Criteria

- [ ] `NeatooReferenceResolver.Current` is a `public static` property with `internal` setter
- [ ] `NeatooJsonSerializer` has one `JsonSerializerOptions` instance (no `PlainOptions`)
- [ ] `NeatooJsonSerializer` has no `IsNeatooType()` method
- [ ] `NeatooJsonSerializer` has no `ReferenceHandler` property
- [ ] `Options.ReferenceHandler` is `null` (not set)
- [ ] `NeatooReferenceHandler.cs` is deleted
- [ ] Dead code removed from `NeatooInterfaceJsonTypeConverter.Read()`
- [ ] Resolver is always created and cleaned up in `try/finally` in all Serialize/Deserialize methods
- [ ] All existing tests pass without modification
- [ ] No `$id`/`$ref` metadata in serialized JSON for plain records

---

## Dependencies

- **No external dependencies.** All changes are within the `RemoteFactory` project.
- **Neatoo coordinated update** is out of scope for this plan but must happen before Neatoo's next release. The migration path is documented in the Design section.

---

## Risks / Considerations

1. **Neatoo test failures.** Neatoo's converters currently access `options.ReferenceHandler.CreateResolver()`. After this change, `options.ReferenceHandler` will be `null`, causing `NullReferenceException` in Neatoo's converters. This is expected and intentional -- Neatoo must migrate to `NeatooReferenceResolver.Current`. The 88-test failure documented in `isneatootype-missing-validatebase-check.md` will be resolved by the coordinated Neatoo update, not by this RemoteFactory plan.

2. **Concurrent serialization.** `AsyncLocal` provides per-async-flow isolation, so concurrent serialization operations on different threads will each have their own resolver. This matches the existing behavior (the current `NeatooReferenceHandler` also uses `AsyncLocal`).

3. **Resolver outside NeatooJsonSerializer.** If someone constructs a `NeatooReferenceResolver` manually or serializes Neatoo types using bare STJ (without `NeatooJsonSerializer`), `NeatooReferenceResolver.Current` will be `null`. Downstream converters must null-check. This is a new responsibility for Neatoo's converters.

4. **Breaking change for Neatoo.** The removal of `ReferenceHandler` from `options` is a breaking change for Neatoo's converters. This was acknowledged in clarification A7. The minimum RemoteFactory version for the new Neatoo converters must be documented.

---

## Architectural Verification

**Scope Table:**

| Component | Affected? | Change |
|-----------|-----------|--------|
| `NeatooReferenceResolver` | Yes | Add static `AsyncLocal` accessor |
| `NeatooJsonSerializer` | Yes | Remove dual options, remove `IsNeatooType`, remove `ReferenceHandler`, simplify all methods |
| `NeatooReferenceHandler` | Yes | Deleted entirely |
| `NeatooInterfaceJsonTypeConverter` | Yes | Remove dead reference handler code |
| `NeatooOrdinalConverterFactory` | No | Does not use `ReferenceHandler` |
| `NeatooOrdinalConverter<T>` | No | Does not use `ReferenceHandler` |
| `NeatooInterfaceJsonConverterFactory` | No | Does not use `ReferenceHandler` |
| `NeatooJsonTypeInfoResolver` | No | Independent of `ReferenceHandler` |
| `NeatooJsonConverterFactory` | No | Abstract base, no `ReferenceHandler` |
| `AddRemoteFactoryServices` | No | DI registration unchanged |
| All test files | No | Must pass without modification |

**Verification Evidence:**

- `NeatooInterfaceJsonTypeConverter.Read()` reference handler code is dead: `id` is initialized to `string.Empty` at line 32 and never assigned. The guard `!string.IsNullOrEmpty(id)` at line 44 always evaluates to `true` (isEmpty), so `AddReference` is never called. Evidence: `grep "id =" NeatooInterfaceJsonTypeConverter.cs` returns only line 32.

- `NeatooOrdinalConverterFactory` and `NeatooOrdinalConverter<T>` do not reference `ReferenceHandler`: confirmed via grep -- zero matches.

- `NeatooJsonTypeInfoResolver` does not reference `ReferenceHandler`: confirmed via grep -- zero matches.

- `serviceAssemblies` field in `NeatooJsonSerializer` is used in `DeserializeRemoteDelegateRequest()` (line 364) and `FromObjectJson()` (line 408), so it must be retained even though `IsNeatooType()` is deleted.

**Breaking Changes:** Yes -- for Neatoo's converters (documented, acknowledged in A7). Not breaking for RemoteFactory's own tests or public API.

**Codebase Analysis:**

Key files examined:
- `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` -- dual options, `IsNeatooType`, resolver lifecycle
- `src/RemoteFactory/Internal/NeatooReferenceHandler.cs` -- `AsyncLocal<ReferenceResolver>` bridge class
- `src/RemoteFactory/Internal/NeatooReferenceResolver.cs` -- resolver implementation
- `src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs` -- dead reference handler code
- `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs` -- no `ReferenceHandler` usage
- `src/RemoteFactory/Internal/NeatooInterfaceJsonConverterFactory.cs` -- no `ReferenceHandler` usage
- `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs` -- independent of `ReferenceHandler`
- `src/RemoteFactory/AddRemoteFactoryServices.cs` -- DI registration
- `src/Design/Design.Tests/FactoryTests/SerializationTests.cs` -- 6 tests
- `src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs` -- record round-trip test
- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs` -- 6 tests
- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceCollectionSerializationTests.cs` -- 11 tests
- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/RecordSerializationTests.cs` -- 16 tests

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1-4: Core Changes | developer | Yes | All source changes are tightly coupled (4 files + 1 deletion), best done in a single pass | None |
| Phase 5: Test Verification | developer | No (same agent) | Same agent runs tests immediately after changes | Phases 1-4 |

**Parallelizable phases:** None -- all changes are sequential.

**Notes:** The changes touch only 4 files plus 1 deletion. The total scope is small enough for a single agent pass. A fresh agent for test verification is unnecessary since the developer needs the implementation context to diagnose any failures.

---

## Domain Model Behavioral Design

Not applicable -- this is an infrastructure/serialization change with no domain model impact.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-20

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `NeatooJsonSerializer` constructor: remove `ReferenceHandler = this.ReferenceHandler` from `Options` initialization (line 78). STJ defaults `ReferenceHandler` to `null`. | `Options.ReferenceHandler == null` | Yes | |
| 2 | `NeatooJsonSerializer` constructor: delete `PlainOptions` property (line 41) and initialization (lines 98-113). Only `this.Options` remains. | Single `JsonSerializerOptions` instance | Yes | |
| 3 | `NeatooJsonSerializer`: delete `IsNeatooType()` method (lines 123-136). Replace 4 call sites (lines 152, 190, 225, 262) with unconditional `this.Options`. | `IsNeatooType()` does not exist | Yes | |
| 4 | `NeatooJsonSerializer.Serialize(object?)` (line 138): replace lines 152-158 with `using var rr = new NeatooReferenceResolver(); NeatooReferenceResolver.Current = rr;` in `try/finally` that clears `Current` to `null`. | Resolver created, assigned to AsyncLocal, disposed after completion | Yes | Also incidentally fixes existing scoping bug where `using var rr` was inside `if` block and disposed before serialization. |
| 5 | `NeatooJsonSerializer.Deserialize<T>(string?)` (line 213): same pattern as Rule 4, replacing lines 225-231. | Resolver created, assigned to AsyncLocal, disposed after completion | Yes | Same scoping fix applies. |
| 6 | `NeatooReferenceResolver`: new `private static readonly AsyncLocal<NeatooReferenceResolver?> _current = new()`. `Current` getter returns `_current.Value`. `AsyncLocal<T>` default for reference types is `null`. | `NeatooReferenceResolver.Current` returns `null` | Yes | |
| 7 | `NeatooJsonSerializer.Serialize`/`Deserialize`: `finally` block sets `NeatooReferenceResolver.Current = null`. Executes on all exit paths including exceptions. | AsyncLocal cleared to `null` | Yes | |
| 8 | `NeatooReferenceResolver`: `public static NeatooReferenceResolver? Current` property with `public` getter. Converters call `NeatooReferenceResolver.Current` instead of `options.ReferenceHandler.CreateResolver()`. | Converters access resolver via static property | Yes | |
| 9 | `NeatooJsonSerializer.Serialize`/`Deserialize`: `NeatooReferenceResolver.Current = rr` assigned before `JsonSerializer.Serialize`/`Deserialize` call. Converters execute within that scope. | Non-null resolver during serialization | Yes | |
| 10 | `NeatooReferenceResolver` class is `public sealed` (line 8). `Current` getter is `public`. Both accessible from any assembly. | Downstream assemblies can access `Current` | Yes | |
| 11 | For plain records: `NeatooOrdinalConverterFactory.CanConvert()` checks `IOrdinalSerializable.IsAssignableFrom(typeToConvert)` -- records do not implement this. `NeatooInterfaceJsonConverterFactory.CanConvert()` checks `IsInterface \|\| IsAbstract` -- records are neither. With `Options.ReferenceHandler == null`, STJ default path never injects `$id`/`$ref`. | No `$id`/`$ref` in output | Yes | |
| 12 | For records with parameterized constructors: same converter bypass as Rule 11. No `$id` in JSON means no `ObjectWithParameterizedCtorRefMetadataNotSupported` error. STJ native parameterized constructor support handles deserialization. | Deserialization succeeds without error | Yes | |
| 13 | `NeatooInterfaceJsonTypeConverter<T>.Write()` (lines 83-102): writes `$type` and `$value` explicitly. Does NOT use `options.ReferenceHandler`. No changes to this method. | `$type`/`$value` wrapper emitted | Yes | |
| 14 | `NeatooInterfaceJsonTypeConverter<T>.Read()` (lines 22-81): reads `$type`/`$value`, resolves via `serviceAssemblies.FindType()`, deserializes via `JsonSerializer.Deserialize(ref reader, concreteType, options)`. Dead code removed but core logic unchanged. | Type resolution and deserialization unchanged | Yes | |
| 15 | `NeatooInterfaceJsonTypeConverter<T>.Read()`: delete `var id = string.Empty` (line 32) and `if (!string.IsNullOrEmpty(id))` block (lines 44-48). `id` was never assigned from JSON stream, so `AddReference` was never called. | No `options.ReferenceHandler` call | Yes | Confirmed dead: grep shows `id` only assigned at line 32 (`string.Empty`), never reassigned. |
| 16 | Phase 4: delete file `src/RemoteFactory/Internal/NeatooReferenceHandler.cs`. Only 2 files reference `NeatooReferenceHandler`: the file itself and `NeatooJsonSerializer.cs` (which removes its reference in Phase 2). | File deleted, no dangling references | Yes | |
| 17 | Phase 2: delete `PlainOptions` property (line 41) + initialization (lines 98-113), delete `IsNeatooType()` (lines 123-136). All references confined to `NeatooJsonSerializer.cs`. | Neither exists after redesign | Yes | |
| 18 | Phase 5: run `dotnet test src/Neatoo.RemoteFactory.sln`. All existing tests pass. Critical tests: `InterfaceFactory_NonNeatooType_NoRefMetadata`, `InterfaceFactory_SimpleRecord_RoundTrip`, all `InterfaceCollectionSerializationTests`, Design project `SerializationTests`. | All tests pass | Yes | Depends on correct implementation of Rules 1-17. |

### Concerns

All concerns raised during review were acknowledged by the user:

1. **Existing scoping bug (acknowledged):** The BEFORE code disposes the resolver before serialization due to `using var` inside `if` block. The AFTER pattern fixes this as an intentional improvement.

2. **Stale test comment (acknowledged):** `InterfaceFactoryRecordSerializationTests.cs:133` references `PlainOptions`. Approved for comment-only update.

3. **`serviceAssemblies` field retention (acknowledged):** Keep the field (line 31), constructor parameters, and field assignment (line 71). Only delete `IsNeatooType()` and `PlainOptions`.

4. **Implementation ordering (acknowledged):** Steps 2.3 (remove `ReferenceHandler` property) and 2.6 (update method bodies) must happen together to avoid intermediate build failures.

5. **`ReferenceResolver` base class (acknowledged):** Inheritance is unnecessary after redesign but harmless. Future cleanup.

---

## Implementation Contract

**Created:** 2026-03-20
**Approved by:** remotefactory-developer

### Verification Acceptance Criteria

No failing acceptance criteria from verification resources. All existing tests must continue to pass.

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1 | Verified by construction: inspect `NeatooJsonSerializer` constructor | No dedicated test -- structural verification |
| 2 | `InterfaceFactory_NonNeatooType_NoRefMetadata` | Existing test validates no `$id`/`$ref` for records |
| 3 | `InterfaceFactory_SimpleRecord_RoundTrip` | Existing test validates parameterized ctor round-trip |
| 4 | `InterfaceFactory_RecordWithCollection_RoundTrip` | Existing test validates collection record round-trip |
| 5 | Verified by construction: `AsyncLocal<T>` default is `null` | No dedicated test -- language-level guarantee |
| 6 | Verified by construction: converter executes within `try` block scope | No dedicated test -- structural verification |
| 7 | Verified by construction: `finally` block always executes | No dedicated test -- language-level guarantee |
| 8 | `SingleInterface_SurvivesRoundTrip` + all `InterfaceCollectionSerializationTests` | Existing tests validate interface type serialization |
| 9 | Same as Scenario 8 | `$type`/`$value` wrapper tested by existing tests |
| 10 | Full test suite (`dotnet test src/Neatoo.RemoteFactory.sln`) | All tests must pass |
| 11 | `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` | Design project record round-trip test |
| 12 | `ListOfInterface_JsonIncludesTypeInformation` | Validates `$type` presence in serialized JSON |
| 13 | File system check: `NeatooReferenceHandler.cs` does not exist | Verified post-deletion |
| 14 | Grep check: no `IsNeatooType` in `NeatooJsonSerializer.cs` | Verified post-deletion |

### In Scope

- [ ] `src/RemoteFactory/Internal/NeatooReferenceResolver.cs`: Add `private static readonly AsyncLocal<NeatooReferenceResolver?> _current` field and `public static NeatooReferenceResolver? Current` property (`public` getter, `internal` setter)
- [ ] `src/RemoteFactory/Internal/NeatooJsonSerializer.cs`: Remove `PlainOptions` property + initialization (lines 41, 98-113). Remove `ReferenceHandler` property + `Options` assignment (lines 43, 78). Remove `IsNeatooType()` method (lines 123-136). Remove `PlainOptions` comments (lines 37-40, 95-97). Update all four `Serialize`/`Deserialize` methods to use `NeatooReferenceResolver.Current` in `try/finally` pattern with single `this.Options`. Keep `serviceAssemblies` field (line 31), all constructor parameters, and field assignment (line 71).
- [ ] `src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs`: Remove `var id = string.Empty` (line 32). Remove dead `if (!string.IsNullOrEmpty(id))` block (lines 44-48).
- [ ] `src/RemoteFactory/Internal/NeatooReferenceHandler.cs`: Delete entire file.
- [ ] `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs`: Update comment at line 133 (replace `PlainOptions` reference with accurate description). Comment-only change, no test logic.

### Out of Scope

- All test assertions and test logic (no test behavior changes)
- `NeatooOrdinalConverterFactory.cs` / `NeatooOrdinalConverter<T>` (no `ReferenceHandler` usage)
- `NeatooInterfaceJsonConverterFactory.cs` (no `ReferenceHandler` usage)
- `NeatooJsonTypeInfoResolver.cs` (independent of `ReferenceHandler`)
- `NeatooJsonConverterFactory.cs` (abstract base, no `ReferenceHandler` access)
- `AddRemoteFactoryServices.cs` (DI registration unchanged)
- Neatoo coordinated migration (separate repo, separate plan)
- Documentation updates (handled in Step 9)
- `NeatooReferenceResolver` base class removal (future cleanup, Concern 5)

### Verification Gates

1. After Phase 1 (AsyncLocal accessor): `dotnet build src/Neatoo.RemoteFactory.sln` succeeds
2. After Phase 2 (NeatooJsonSerializer simplification): `dotnet build src/Neatoo.RemoteFactory.sln` succeeds
3. After Phase 3 (dead code removal from converter): `dotnet build src/Neatoo.RemoteFactory.sln` succeeds
4. After Phase 4 (NeatooReferenceHandler deletion): `dotnet build src/Neatoo.RemoteFactory.sln` succeeds
5. Final: `dotnet test src/Neatoo.RemoteFactory.sln` -- all tests pass, zero failures

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure (any test not listed in In Scope)
- Build failure after any phase that cannot be resolved by the planned changes
- Any test failure in Phase 5 that is not explained by the planned changes
- Discovery that `options.ReferenceHandler` is accessed by code not identified in the plan

---

## Implementation Progress

**Started:** 2026-03-20
**Developer:** remotefactory-developer

### Milestones

| Phase | Description | Status | Build Result |
|-------|-------------|--------|-------------|
| 1 | Add `AsyncLocal` accessor to `NeatooReferenceResolver` | Complete | 0 errors, 3 warnings (pre-existing WASM warnings) |
| 2 | Simplify `NeatooJsonSerializer` (remove `PlainOptions`, `IsNeatooType()`, `ReferenceHandler` property; single options; `try/finally` resolver lifecycle) | Complete | 0 errors, 3 warnings |
| 3 | Remove dead code from `NeatooInterfaceJsonTypeConverter` (`var id = string.Empty` and `if (!string.IsNullOrEmpty(id))` block) | Complete | 0 errors, 3 warnings |
| 4 | Delete `NeatooReferenceHandler.cs` | Complete | 0 errors, 3 warnings |
| 5 | Update stale comment in `InterfaceFactoryRecordSerializationTests.cs:133`; run full test suite | Complete | All tests pass |

### Changes Made

**`src/RemoteFactory/Internal/NeatooReferenceResolver.cs`:**
- Added `using System.Threading;`
- Added `private static readonly AsyncLocal<NeatooReferenceResolver?> _current = new();`
- Added `public static NeatooReferenceResolver? Current` property with `public` getter and `internal` setter

**`src/RemoteFactory/Internal/NeatooJsonSerializer.cs`:**
- Removed `PlainOptions` property (line 41) and its comment block (lines 37-40)
- Removed `private NeatooReferenceHandler ReferenceHandler` property (line 43)
- Removed `ReferenceHandler = this.ReferenceHandler` from `Options` construction (line 78)
- Removed `PlainOptions` construction block (lines 95-113) including ordinal converter and converter factory duplication
- Removed `IsNeatooType()` method (lines 117-136)
- Kept `serviceAssemblies` field (line 31), all constructor parameters, and field assignment (line 63)
- Updated `Serialize(object?)`: replaced `IsNeatooType` branching with `using var rr = new NeatooReferenceResolver(); NeatooReferenceResolver.Current = rr;` at method body scope, `try/finally` to clear `Current`
- Updated `Serialize(object?, Type)`: same pattern
- Updated `Deserialize<T>(string?)`: same pattern
- Updated `Deserialize(string?, Type)`: same pattern

**`src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs`:**
- Removed `var id = string.Empty;` (line 32)
- Removed dead `if (!string.IsNullOrEmpty(id))` block (lines 44-48) that called `options.ReferenceHandler!.CreateResolver().AddReference(id, result)`

**`src/RemoteFactory/Internal/NeatooReferenceHandler.cs`:**
- Deleted entire file

**`src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs`:**
- Updated comment at line 133 from `"serialize via the serializer that now selects PlainOptions for non-Neatoo types"` to `"serialize via the serializer (no ReferenceHandler in options, so records get no $id/$ref)"`

---

## Completion Evidence

**Reported:** 2026-03-20

- **Tests Passing:**
  - RemoteFactory.UnitTests: 490 passed, 0 failed (net9.0); 490 passed, 0 failed (net10.0)
  - RemoteFactory.IntegrationTests: 487 passed, 3 skipped, 0 failed (net9.0); 487 passed, 3 skipped, 0 failed (net10.0)
  - Design.Tests: 42 passed, 0 failed (net9.0); 42 passed, 0 failed (net10.0)
  - Total: 2,036 passed, 0 failed, 6 skipped (3 pre-existing ShowcasePerformanceTests skipped per TFM)
- **Verification Resources Pass:** Yes -- Design.Tests (42 tests x 2 TFMs) all pass
- **All Contract Items:** Confirmed 100% complete

### Contract Item Verification

| Contract Item | Status | Evidence |
|---------------|--------|----------|
| `NeatooReferenceResolver.cs`: Add `AsyncLocal` field and `Current` property | Done | `private static readonly AsyncLocal<NeatooReferenceResolver?> _current = new()` and `public static NeatooReferenceResolver? Current` with `public` getter, `internal` setter |
| `NeatooJsonSerializer.cs`: Remove `PlainOptions` property + initialization | Done | No `PlainOptions` in file |
| `NeatooJsonSerializer.cs`: Remove `ReferenceHandler` property + `Options` assignment | Done | No `ReferenceHandler` property; `Options` has no `ReferenceHandler =` line |
| `NeatooJsonSerializer.cs`: Remove `IsNeatooType()` method | Done | No `IsNeatooType` in file |
| `NeatooJsonSerializer.cs`: Update all four Serialize/Deserialize methods | Done | All use `using var rr` + `NeatooReferenceResolver.Current = rr` + `try/finally` pattern |
| `NeatooJsonSerializer.cs`: Keep `serviceAssemblies` field and constructor params | Done | Field at line 31, assignment at line 63, used in `DeserializeRemoteDelegateRequest` and `FromObjectJson` |
| `NeatooInterfaceJsonTypeConverter.cs`: Remove `var id = string.Empty` | Done | Line removed |
| `NeatooInterfaceJsonTypeConverter.cs`: Remove dead `if (!string.IsNullOrEmpty(id))` block | Done | Block removed |
| `NeatooReferenceHandler.cs`: Delete entire file | Done | File does not exist |
| `InterfaceFactoryRecordSerializationTests.cs`: Update stale comment at line 133 | Done | Comment updated, no test logic changed |

### Stop Condition Checks

- No out-of-scope test failures: zero test failures across all projects
- No unexpected build failures: all four phase builds succeeded with zero errors
- No undocumented `options.ReferenceHandler` access sites: grep confirms no remaining references outside the deleted file

---

## Documentation

**Agent:** business-requirements-documenter
**Completed:** 2026-03-20

### Expected Deliverables

- [x] `docs/serialization.md` -- Update "Scope: Neatoo Types Only" section to describe the new mechanism (converter-level reference handling, not serializer-level type classification)
- [x] `src/Design/CLAUDE-DESIGN.md` -- Update Anti-Pattern 9 "Why it matters" explanation to describe converter-level mechanism instead of dual-options split
- [x] `src/Design/CLAUDE-DESIGN.md` -- Quick Decisions Table record entry (line 153): no change needed (already accurate: "Records are serialized without `$id`/`$ref`")
- [x] `src/Design/CLAUDE-DESIGN.md` -- Common Mistakes item 10: no change needed (already accurate)
- [x] `src/Design/CLAUDE-DESIGN.md` -- design_version bumped to 1.2, last_updated to 2026-03-20
- [ ] Neatoo migration path documentation: Create a brief document or todo describing the required Neatoo converter changes (before/after pattern, minimum RemoteFactory version) -- **Developer Deliverable** (see below)
- [x] Skill updates: N/A (no skill content references `IsNeatooType` or `PlainOptions`)
- [x] Sample updates: N/A

### Files Directly Updated

1. **`src/Design/CLAUDE-DESIGN.md`**
   - Updated `design_version` from 1.1 to 1.2, `last_updated` from 2026-03-08 to 2026-03-20
   - Updated Anti-Pattern 9 "Why it matters" explanation (line 419): replaced dual-options/type-classification description with converter-level mechanism description. Now explains that `JsonSerializerOptions` has no `ReferenceHandler`, converters access `NeatooReferenceResolver.Current` via `AsyncLocal`, and STJ's native record deserialization cannot process `$ref` in parameterized constructors.
   - No changes to Quick Decisions Table (line 153), Common Mistakes item 10 (line 753), or Design Debt table -- all remain accurate as-is.

2. **`docs/serialization.md`**
   - Updated "Scope: Neatoo Types Only" section (lines 120-124): renamed heading to "Scope: Converter-Level, Not Serializer-Level". Replaced `IOrdinalSerializable` / factory assembly type classification description with converter-level mechanism. Now explains that `JsonSerializerOptions` has no `ReferenceHandler`, Neatoo converters access `NeatooReferenceResolver.Current`, and plain records/DTOs are serialized natively by STJ.

### Developer Deliverables

1. **Neatoo migration path documentation**: Create a todo or brief document (in the Neatoo repo, not RemoteFactory) describing the required Neatoo converter migration:
   - **Before**: `var resolver = options.ReferenceHandler!.CreateResolver();`
   - **After**: `var resolver = NeatooReferenceResolver.Current; if (resolver != null) { ... }`
   - **Call sites**: `NeatooBaseJsonTypeConverter.Write()` and any other Neatoo converter accessing `options.ReferenceHandler.CreateResolver()`
   - **Null check**: Required because `NeatooReferenceResolver.Current` is `null` when serialization is not in progress (e.g., bare STJ usage in unit tests)
   - **Minimum RemoteFactory version**: v0.21.4

### General Documentation (Step 8B)

**Agent:** remotefactory-docs-writer
**Completed:** 2026-03-20

- [x] `docs/release-notes/v0.21.4.md` -- Created release notes with overview, breaking changes, bug fixes, migration guide (before/after converter code), and commit summary
- [x] `docs/release-notes/index.md` -- Added v0.21.4 to highlights table and all releases list
- [x] `docs/release-notes/v0.21.3.md` -- Adjusted nav_order from 1 to 2
- [x] `src/Directory.Build.props` -- Bumped `PackageVersion` from 0.21.3 to 0.21.4
- [x] Skill files -- Verified no updates needed (no references to `IsNeatooType`, `PlainOptions`, `NeatooReferenceHandler`, or `ReferenceHandler`)
- [x] `docs/serialization.md` -- Already updated by business-requirements-documenter (heading renamed, content reflects converter-level mechanism)

Step 8 documentation is complete.

---

## Architect Verification

**Verified:** 2026-03-20
**Verdict:** VERIFIED

### Independent Build Results

- `dotnet build src/Neatoo.RemoteFactory.sln` -- 0 errors, 3 warnings (pre-existing WASM warnings in OrderEntry.BlazorClient)

### Independent Test Results

- RemoteFactory.UnitTests: 490 passed, 0 failed (net9.0); 490 passed, 0 failed (net10.0)
- RemoteFactory.IntegrationTests: 487 passed, 3 skipped, 0 failed (net9.0); 487 passed, 3 skipped, 0 failed (net10.0)
- Design.Tests: 42 passed, 0 failed (net9.0); 42 passed, 0 failed (net10.0)
- **Total: 2,038 passed, 0 failed, 6 skipped** (3 pre-existing ShowcasePerformanceTests skipped per TFM)

### Design Match Verification

Each acceptance criterion verified by reading the actual source files:

| Criterion | Verified? | Evidence |
|-----------|-----------|----------|
| `NeatooReferenceResolver.Current` is `public static` with `internal` setter | Yes | Lines 19-23: `public static NeatooReferenceResolver? Current { get => _current.Value; internal set => _current.Value = value; }` |
| `NeatooJsonSerializer` has single `JsonSerializerOptions` (no `PlainOptions`) | Yes | Grep for `PlainOptions` returned zero matches in NeatooJsonSerializer.cs |
| `NeatooJsonSerializer` has no `IsNeatooType()` method | Yes | Grep for `IsNeatooType` returned zero matches in NeatooJsonSerializer.cs |
| `NeatooJsonSerializer` has no `ReferenceHandler` property | Yes | Grep for `ReferenceHandler` returned zero matches in NeatooJsonSerializer.cs |
| `Options.ReferenceHandler` is `null` (not set) | Yes | Options construction (lines 68-73) has no `ReferenceHandler =` assignment |
| `NeatooReferenceHandler.cs` is deleted | Yes | File read returned "File does not exist" |
| Dead code removed from `NeatooInterfaceJsonTypeConverter.Read()` | Yes | No `var id = string.Empty` or `AddReference` call; grep confirmed zero matches |
| Resolver is always created and cleaned up in `try/finally` | Yes | All four methods confirmed: `Serialize(object?)` lines 100-120, `Serialize(object?, Type)` lines 136-156, `Deserialize<T>` lines 169-189, `Deserialize(string?, Type)` lines 204-224 |
| All existing tests pass without modification | Yes | 2,038 tests passed, 0 failures |
| `serviceAssemblies` field retained | Yes | Field at line 31, used in `DeserializeRemoteDelegateRequest` (line 306) and `FromObjectJson` (line 350) |
| No `NeatooReferenceHandler` references remain | Yes | Grep for `NeatooReferenceHandler` across `src/RemoteFactory/` returned zero matches |
| Comment-only change in `InterfaceFactoryRecordSerializationTests.cs` | Yes | Line 133 updated to `"serialize via the serializer (no ReferenceHandler in options, so records get no $id/$ref)"` -- no test logic changed |

### Issues Found

None. Implementation matches the design precisely across all 5 files (4 modified + 1 deleted).

---

## Requirements Verification

**Reviewer:** business-requirements-reviewer
**Verified:** 2026-03-20
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| **Serialization round-trip for Class Factory types** (Design.Tests SerializationTests, 6 tests) | SATISFIED | 42 Design.Tests pass per TFM. `NeatooJsonSerializer` methods all use single `this.Options` with `try/finally` resolver lifecycle. Round-trip path unchanged: `ToObjectJson` -> `Serialize` -> resolver set -> converters run -> resolver cleared. |
| **Interface Factory record round-trip without `$id`/`$ref`** (v0.21.3 fix, `InterfaceFactoryTests.cs:96-117`) | SATISFIED | `Options.ReferenceHandler` is `null` (no assignment in constructor, lines 68-73). STJ does not inject `$id`/`$ref` when `ReferenceHandler` is null. Records bypass all custom converters (`NeatooOrdinalConverterFactory` checks `IOrdinalSerializable`, `NeatooInterfaceJsonConverterFactory` checks `IsInterface || IsAbstract` -- records match neither). Test `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` passes. |
| **No `$id`/`$ref` metadata in serialized JSON for plain records** (Anti-Pattern 9, `CLAUDE-DESIGN.md:378-419`) | SATISFIED | Verified via `InterfaceFactory_NonNeatooType_NoRefMetadata` test (6 tests in `InterfaceFactoryRecordSerializationTests`). With `Options.ReferenceHandler == null`, STJ's native path serializes records without reference metadata. Anti-Pattern 9 rule text remains accurate: "plain records/DTOs are serialized without reference handling." |
| **Records with parameterized constructors deserialize without error** (Quick Decisions Table, `CLAUDE-DESIGN.md:153`) | SATISFIED | No `$id` in JSON means no `ObjectWithParameterizedCtorRefMetadataNotSupported` error. 12 `RecordSerializationTests` plus 4 ClientServer tests pass. |
| **Interface-typed property serialization emits `$type`/`$value`** (`NeatooInterfaceJsonTypeConverter`) | SATISFIED | `Write()` method (lines 76-95) writes `$type` and `$value` explicitly, independent of `ReferenceHandler`. `Read()` method (lines 22-74) reads `$type`/`$value` and resolves concrete type. Dead code removed (lines 31-48 in old file), core logic unchanged. 11 `InterfaceCollectionSerializationTests` pass. |
| **`AsyncLocal` resolver accessor: public getter, internal setter, nullable** (Gap 1 from requirements review) | SATISFIED | `NeatooReferenceResolver.cs` lines 19-23: `public static NeatooReferenceResolver? Current { get => _current.Value; internal set => _current.Value = value; }`. Downstream converters can read; only `NeatooJsonSerializer` can set. Null when no operation in progress. |
| **Resolver lifecycle: created before, cleared after (including exceptions)** (Business Rule 7) | SATISFIED | All four Serialize/Deserialize methods follow identical pattern: `using var rr = new NeatooReferenceResolver(); NeatooReferenceResolver.Current = rr; try { ... } finally { NeatooReferenceResolver.Current = null; }`. The `finally` block guarantees cleanup on all exit paths. |
| **Single `JsonSerializerOptions` instance (no `PlainOptions`)** (Business Rule 2) | SATISFIED | Grep for `PlainOptions` in `src/RemoteFactory/` returns zero matches. Constructor (lines 68-84) creates one `Options` instance. |
| **No `IsNeatooType()` method** (Business Rule 3) | SATISFIED | Grep for `IsNeatooType` in `src/RemoteFactory/` returns zero matches. |
| **`NeatooReferenceHandler.cs` deleted** (Business Rule 16) | SATISFIED | File glob for `**/NeatooReferenceHandler.cs` returns no matches. Grep for `NeatooReferenceHandler` in `src/RemoteFactory/` returns zero matches. |
| **Properties need public setters** (Critical Rule 5, `CLAUDE-DESIGN.md:603-610`) | NOT AFFECTED | No property visibility changes in this implementation. |
| **`[Remote]` only for aggregate root entry points** (Critical Rule 1, `CLAUDE-DESIGN.md:425-442`) | NOT AFFECTED | No changes to factory patterns or `[Remote]` behavior. |
| **All existing tests pass without modification** (Business Rule 18) | SATISFIED | 2,038 tests passed, 0 failures, 6 skipped (pre-existing). No test assertions or logic modified; only one comment updated in `InterfaceFactoryRecordSerializationTests.cs:133`. |
| **`serviceAssemblies` field retained** (Plan Concern 3) | SATISFIED | Field at `NeatooJsonSerializer.cs:31`, assignment at line 63, used in `DeserializeRemoteDelegateRequest` (line 306) and `FromObjectJson` (line 350). |
| **Files NOT changed that should not be changed** | SATISFIED | Verified zero `ReferenceHandler` matches in: `NeatooOrdinalConverterFactory.cs`, `NeatooInterfaceJsonConverterFactory.cs`, `NeatooJsonTypeInfoResolver.cs`, `AddRemoteFactoryServices.cs`. No generator code references changed components. |

### Unintended Side Effects

**None found.** Specifically verified:

1. **Generated code patterns (Design projects):** No impact. The generator does not reference `NeatooReferenceHandler`, `PlainOptions`, or `IsNeatooType`. Generated `IOrdinalSerializable` implementations are unaffected. The `NeatooOrdinalConverterFactory` and `NeatooOrdinalConverter<T>` do not use `ReferenceHandler`.

2. **Serialization contracts:** No change to what survives the client/server round-trip. The `$type`/`$value` wrapper for interface-typed properties is emitted by `NeatooInterfaceJsonTypeConverter.Write()` independent of `ReferenceHandler`. The `$id`/`$ref` metadata for Neatoo types is emitted by downstream Neatoo converters (out of scope), not by RemoteFactory's converters. Plain records/DTOs continue to serialize without reference metadata.

3. **Design project tests:** All 42 tests pass per TFM. The Design projects demonstrate correct patterns and remain accurate as the source of truth.

4. **Published docs accuracy:** `docs/serialization.md` lines 120-124 ("Scope: Neatoo Types Only") describes the user-facing behavior correctly -- plain records/DTOs are serialized without reference handling. The internal mechanism description is slightly stale (refers to the old approach conceptually) but the plan correctly tracks this as a Step 9 documentation deliverable. No user-facing behavioral inaccuracy.

5. **Anti-Pattern 9 (`CLAUDE-DESIGN.md:378-419`):** The rule and its "Why it matters" explanation remain behaviorally accurate. The statement "the record is serialized without reference handling, but the embedded Neatoo type expects it" is still correct. The plan tracks the mechanism description update as a Step 9 deliverable.

6. **Breaking change for downstream Neatoo converters:** Acknowledged in plan Risks section (item 1 and 4). Neatoo's converters that call `options.ReferenceHandler.CreateResolver()` will get `NullReferenceException` until migrated to `NeatooReferenceResolver.Current`. This is intentional and documented. The migration path is documented in the plan's Design section (lines 247-264).

7. **Multi-targeting (net9.0/net10.0):** Tests pass on both frameworks. `AsyncLocal<T>` is available in both. No framework-specific concerns.

### Issues Found

None.
