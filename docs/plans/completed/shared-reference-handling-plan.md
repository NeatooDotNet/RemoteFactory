# Shared Reference Handling for Non-Custom Types

**Date:** 2026-03-20
**Related Todo:** [Shared Reference Handling for Non-Custom Types](../todos/completed/shared-reference-handling-non-custom-types.md)
**Status:** Complete
**Last Updated:** 2026-03-21

---

## Overview

RemoteFactory v0.22.0 moved reference handling (`$id`/`$ref`) to a converter-level concern, removing `ReferenceHandler` from `JsonSerializerOptions`. This means only types with custom converters (Neatoo entities/lists) participate in reference tracking. Non-custom types (Dictionary, List, plain classes) serialized by STJ's built-in converters lose shared-reference identity after round-trip.

This plan addresses the gap in two phases:
1. **Phase 1 (Reproduction):** Create RemoteFactory-internal tests that reproduce all three problem scenarios -- shared Dictionary identity, records with parameterized constructors, and circular references in non-custom types -- using ONLY RemoteFactory's serialization (no Neatoo). **Phase 1 is complete.** It confirmed that custom `ReferenceHandler` works for mutable types, and that STJ's parameterized constructor limitation is permanent and extends to ANY `$id`/`$ref` metadata on reference-type constructor parameters.
2. **Phase 2 (Fix):** Implement a two-component solution: `NeatooPreserveReferenceHandler` (already built in Phase 1) wired into `NeatooJsonSerializer`'s options for mutable reference types, plus a new `RecordBypassConverterFactory` that claims types with parameterized constructors and delegates to inner options without `ReferenceHandler`. Records serialize without `$id`/`$ref` metadata entirely -- this is semantically correct because records are DDD value objects whose identity is defined by their values, not by reference.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/shared-reference-handling-non-custom-types.md#requirements-review)

### Published Documentation Promises

- **`docs/serialization.md:10`:** "Shared instance identity -- When the same object is referenced by two properties (e.g., a parent-child bidirectional reference), System.Text.Json duplicates it. RemoteFactory tracks object identity and serializes shared references as `$ref` pointers, preserving the graph structure." This claim is NOT qualified as "Neatoo types only." The current implementation under-delivers on this published promise.

- **`docs/appendix/serialization.md:53-55`:** "When two properties reference the same object instance (common in aggregate root / child entity relationships), STJ duplicates it -- creating two independent copies. RemoteFactory preserves identity using `$id` / `$ref` pointers, maintaining the object graph structure across the wire." Again, no qualification.

- **`docs/client-server-architecture.md:3`:** "RemoteFactory lets you write your domain model as if it runs in a single process." Losing shared-identity for non-custom types violates this abstraction. User clarification A1 reinforces: "RemoteFactory is billing itself as abstracting away the client/server physical layer. We should try as hard as we can to do that."

### Current Architecture (v0.22.0)

- **`docs/serialization.md:120-124`:** v0.22.0 documents "Scope: Converter-Level, Not Serializer-Level." Plain records and DTOs have no custom converter, so they serialize without reference metadata. This is the documented current-state limitation that this todo proposes changing.

- **`docs/release-notes/v0.22.0.md:16-17, 24`:** Confirms no `ReferenceHandler` on options is the current design. This was a deliberate breaking change from v0.21.3.

- **Anti-Pattern 9 (`src/Design/CLAUDE-DESIGN.md:378-419`):** Technical explanation references the converter-level mechanism. If this todo restores `ReferenceHandler` on options, Anti-Pattern 9's explanation needs updating. The user-facing rule (do not mix Neatoo types with records) may or may not change.

### STJ Limitation

- **`ObjectWithParameterizedCtorRefMetadataNotSupported`:** STJ cannot deserialize types with parameterized constructors when `$ref` metadata appears in the payload. Microsoft docs confirm: "This feature can't be used to preserve value types or immutable types. On deserialization, the instance of an immutable type is created after the entire payload is read. So it would be impossible to deserialize the same instance if a reference to it appears within the JSON payload." Records with primary constructors fall into this category.

### Infrastructure

- **`NeatooReferenceResolver`** (`src/RemoteFactory/Internal/NeatooReferenceResolver.cs`): Already provides the full reference tracking API -- `GetReference`, `AddReference`, `ResolveReference` with `ReferenceEqualityComparer.Instance`. It extends STJ's `ReferenceResolver` base class and is accessible via `AsyncLocal<NeatooReferenceResolver?>`.

- **`NeatooInterfaceJsonTypeConverter`** (`src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs`): Does NOT call `NeatooReferenceResolver` at all (dead code was removed in v0.22.0). Only handles `$type`/`$value` wrapping.

### Existing Tests

- **`InterfaceFactory_NonNeatooType_NoRefMetadata`** (`src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs:121-140`): Asserts `Assert.DoesNotContain("$id", json)` and `Assert.DoesNotContain("$ref", json)` for a serialized record. This test guards against the v0.21.3 record bug. If the implementation adds `ReferenceHandler` globally, this test will fail. **Intent analysis:** The test's purpose is to verify records are not corrupted by `$id`/`$ref` -- not to assert that reference tracking is universally absent. If the new design can handle records without corruption, the test's core intent (records work) is preserved even if the assertion changes.

- **Design project `SerializationTests.cs`** (`src/Design/Design.Tests/FactoryTests/SerializationTests.cs`): Seven tests verify round-trip for Create, Fetch, ValueObject, Collection, Nullable, Modified, SaveMeta. None test shared object identity.

- **No RemoteFactory test infrastructure for shared-reference scenarios** exists. The only test is in the Neatoo repository (currently [Ignore]d).

### Gaps

1. No documented contract for what "shared instance identity" means for non-Neatoo types -- which types participate, cross-entity vs. within-entity scope.
2. No documented approach for handling the record/parameterized-constructor conflict alongside `ReferenceHandler`.
3. No RemoteFactory test infrastructure for shared-reference scenarios.
4. No documented requirement for circular reference handling in non-custom types.
5. No analysis of ordinal format interaction with reference metadata.

### Contradictions

No hard contradictions with the Design Debt table or documented anti-patterns. The key **tension** is with the v0.22.0 "converter-level, not serializer-level" principle. Setting `options.ReferenceHandler` would partially reverse v0.22.0's direction. The architect addresses this tension in the Approach section.

---

## Business Rules (Testable Assertions)

### Phase 1: Reproduction Rules (exploration -- these establish the baseline)

1. WHEN a `Dictionary<string, string>` is assigned to two properties of a plain class and serialized/deserialized through `NeatooJsonSerializer` (no `ReferenceHandler` on options), THEN the two properties after deserialization are NOT the same object instance (`ReferenceEquals` returns `false`). -- Source: Current behavior per v0.22.0 design (Finding 4). This rule documents the CURRENT broken state that Phase 1 must reproduce.

2. WHEN `NeatooJsonSerializer` serializes a record with a parameterized constructor (current v0.22.0 -- no `ReferenceHandler`), THEN deserialization succeeds without error. -- Source: v0.22.0 release notes (Finding 13), test `InterfaceFactory_SimpleRecord_RoundTrip`.

3. WHEN `options.ReferenceHandler = ReferenceHandler.Preserve` is set AND a record with a parameterized constructor is serialized/deserialized, THEN STJ throws `NotSupportedException` containing `ObjectWithParameterizedCtorRefMetadataNotSupported` on deserialization. -- Source: Original v0.21.3 bug (Finding 7). Phase 1 must reproduce this to confirm it is an inherent STJ limitation.

4. WHEN a plain class has a circular reference (A.Child = B, B.Parent = A) and is serialized through `NeatooJsonSerializer` without `ReferenceHandler`, THEN serialization throws `JsonException` (stack depth exceeded) or produces invalid output. -- Source: `src/Design/Design.Tests/FactoryTests/SerializationTests.cs:38` ("Circular references without proper handling" listed as NO). Phase 1 must reproduce this.

5. WHEN a `Dictionary<string, string>` is assigned to two properties of a plain class AND `options.ReferenceHandler = ReferenceHandler.Preserve` is set, THEN after deserialization the two properties ARE the same object instance (`ReferenceEquals` returns `true`). -- Source: STJ `ReferenceHandler.Preserve` documented behavior. Phase 1 must confirm this works as the "happy path" when `ReferenceHandler` is present.

6. WHEN `options.ReferenceHandler` uses a custom `ReferenceHandler` subclass that delegates to `NeatooReferenceResolver.Current`, THEN STJ's built-in converters for mutable reference types (Dictionary, List, plain classes) emit `$id`/`$ref` metadata AND shared identity is preserved on deserialization. -- Source: NEW. Phase 1 must confirm this approach works.

### Phase 2: Solution Rules

7. WHEN a mutable reference type (Dictionary, List, plain class with default/settable constructor) is assigned to two properties of any object in the same serialization graph AND serialized/deserialized through `NeatooJsonSerializer`, THEN after deserialization the two properties reference the same object instance (`ReferenceEquals` returns `true`). -- Source: Published docs promise (Finding 1, 2). NEW implementation requirement.

8. WHEN a type with a parameterized constructor (records, classes with constructor parameters) is serialized/deserialized through `NeatooJsonSerializer`, THEN deserialization succeeds without error and all property values are preserved. The `RecordBypassConverterFactory` claims the type and delegates to inner options without `ReferenceHandler`, so no `$id`/`$ref` metadata appears in the JSON for these types. -- Source: v0.22.0 behavior preservation, existing test `InterfaceFactory_SimpleRecord_RoundTrip`. DDD justification: records are value objects; identity is defined by values, not reference.

9. WHEN a mutable reference type has a circular reference (A.Child = B, B.Parent = A) AND is serialized/deserialized through `NeatooJsonSerializer`, THEN the circular reference is preserved after deserialization (`A.Child.Parent` is the same instance as `A`). -- Source: User clarification A5 ("Both [shared identity and circular references]"). NEW.

10. WHEN a Neatoo type with a custom converter is serialized alongside non-custom types in the same object graph, THEN both Neatoo's converter-level `$id`/`$ref` AND STJ's built-in `$id`/`$ref` use the SAME `NeatooReferenceResolver` instance, so cross-type shared references are tracked correctly. -- Source: User clarification A2 ("Yes, at least start with that scope"). NEW.

11. WHEN a type with a parameterized constructor appears in a graph with shared mutable references, THEN the `RecordBypassConverterFactory` claims the parameterized-constructor type and serializes it WITHOUT `$id`/`$ref` metadata. Mutable references elsewhere in the graph (outside the parameterized-constructor type's subtree) still participate in `ReferenceHandler`-based tracking. A mutable type (e.g., Dictionary) nested inside a record's constructor parameters is serialized as an independent copy -- this is correct DDD behavior because records are value objects and their internal state is logically independent. -- Source: User decision (DDD value object semantics). NEW.

12. WHEN serialization uses Ordinal format (`SerializationFormat.Ordinal`), THEN types with `IOrdinalSerializable` are serialized as arrays by `NeatooOrdinalConverter<T>` and reference handling for those types continues to be managed by Neatoo's custom converters (not by `ReferenceHandler` on options). Non-ordinal types in the same graph still participate in `ReferenceHandler`-based reference tracking. -- Source: Ordinal converter design. NEW. Ordinal and `ReferenceHandler` coexist because the ordinal converter claims only `IOrdinalSerializable` types.

13. WHEN all existing tests in `RemoteFactory.IntegrationTests` and `Design.Tests` are run, THEN zero tests fail. The `InterfaceFactory_NonNeatooType_NoRefMetadata` test continues to pass unchanged because `RecordBypassConverterFactory` claims the record type before `ReferenceHandler` can add metadata -- records still produce JSON without `$id`/`$ref`. -- Source: Sacred tests rule -- intent preserved; bypass converter ensures records never get reference metadata.

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Shared Dictionary -- current behavior (no fix) | Plain class with `PropA` and `PropB` both set to same `Dictionary<string,string>` instance; serialize/deserialize via `NeatooJsonSerializer` (v0.22.0 options) | Rule 1 | `ReferenceEquals(result.PropA, result.PropB)` is `false` -- shared identity lost |
| 2 | Record round-trip -- current behavior | `InterfaceRecordWithCollection("Test", items)` serialize/deserialize via `NeatooJsonSerializer` | Rule 2 | Deserialization succeeds, `Name == "Test"`, `Items.Count == 3` |
| 3 | Record with ReferenceHandler.Preserve | Same record, but `options.ReferenceHandler = ReferenceHandler.Preserve` | Rule 3 | `NotSupportedException` thrown on deserialization |
| 4 | Circular reference -- no handler | Plain class `Node { string Name; Node? Next; }` with `a.Next = b; b.Next = a`; serialize via `NeatooJsonSerializer` (no handler) | Rule 4 | `JsonException` thrown (max depth exceeded) |
| 5 | Shared Dictionary with ReferenceHandler.Preserve | Same as Scenario 1 but with `ReferenceHandler.Preserve` on a bare `JsonSerializerOptions` | Rule 5 | `ReferenceEquals(result.PropA, result.PropB)` is `true` |
| 6 | Custom ReferenceHandler delegating to NeatooReferenceResolver | Same as Scenario 5 but using custom `ReferenceHandler` subclass that returns `NeatooReferenceResolver.Current` from `CreateResolver()` | Rule 6 | `ReferenceEquals(result.PropA, result.PropB)` is `true` -- confirms custom handler works |
| 7 | Shared Dictionary -- after fix | Same as Scenario 1 but with `NeatooPreserveReferenceHandler` on options and `RecordBypassConverterFactory` in converters | Rule 7 | `ReferenceEquals(result.PropA, result.PropB)` is `true` |
| 8 | Record round-trip -- after fix | Record with parameterized constructor containing reference-type params (e.g., `IReadOnlyList<T>`); serialize/deserialize with both components active | Rule 8 | Deserialization succeeds, all properties intact, JSON contains no `$id`/`$ref` for the record |
| 9 | Circular reference -- after fix | Same as Scenario 4 but with both components active | Rule 9 | `result.Next.Next` is same instance as `result` (`ReferenceEquals` is `true`) |
| 10 | Cross-type shared reference (Neatoo + non-Neatoo) | Neatoo entity with a `Dictionary<string,string>` property; same Dictionary also referenced from a sibling property | Rule 10 | Both properties reference the same Dictionary instance after round-trip |
| 11 | Record with nested mutable type in mixed graph | Graph containing a record `Foo(string Name, Dictionary<string,string> Data)` AND a plain class property referencing the SAME Dictionary instance; both components active | Rule 11 | Record deserializes correctly with its own independent copy of `Data`; the plain class property's Dictionary participates in `$id`/`$ref` tracking; no error. `ReferenceEquals` between the record's Data and the plain class's Data is `false` -- correct DDD behavior |
| 12 | Ordinal format -- existing behavior preserved | Existing ordinal serialization tests pass without change | Rule 12 | All ordinal tests pass |
| 13 | Existing test suite -- no regressions | Run all `RemoteFactory.IntegrationTests` and `Design.Tests` | Rule 13 | Zero failures; `InterfaceFactory_NonNeatooType_NoRefMetadata` passes unchanged because bypass converter prevents `$id`/`$ref` on records |

---

## Approach

### The Core Insight

The v0.22.0 decision to make reference handling a "converter-level concern" was correct for Neatoo's custom converters -- they have full control over their serialization format. But it left a gap: non-custom types handled by STJ's built-in converters have no converter to add `$id`/`$ref`. The only way to get STJ's built-in converters to participate in reference tracking is through `options.ReferenceHandler`.

This is not reverting v0.22.0. It is completing the picture:

- **v0.22.0 established:** Neatoo converters access `NeatooReferenceResolver.Current` directly (converter-level).
- **This work adds:** STJ's built-in converters access the SAME resolver through `options.ReferenceHandler` (serializer-level for non-custom types).
- **`RecordBypassConverterFactory` prevents STJ's parameterized-constructor limitation** from affecting records.
- **Both reference tracking paths use the same resolver instance**, so cross-type reference identity is maintained.

### The Record Problem and DDD Resolution

Phase 1 confirmed that STJ's parameterized-constructor limitation is permanent and comprehensive: STJ throws `NotSupportedException` for ANY reference metadata (`$id` or `$ref`) on reference-type constructor parameters -- not just `$ref` on the record itself. This limitation is closed as NOT_PLANNED in dotnet/runtime#73302.

Phase 2 attempted the "optimistic path" (hoping STJ would skip records as immutable) and discovered it does not. STJ adds `$id` to ALL reference types when `ReferenceHandler` is set, regardless of constructor type.

**The resolution is architectural, not a workaround -- it follows DDD value object semantics:**

Records are value objects. Value objects are defined by their values, not their identity. Two value objects with the same state are interchangeable. Therefore:

1. **Reference tracking is semantically wrong for records.** Tracking identity implies the object's reference matters. For value objects, it does not. Duplicating a record's internal state (including nested Lists/Dictionaries) on round-trip is the correct DDD behavior.

2. **Nested reference types within records are part of the value object's state.** A Dictionary inside a record constructor parameter is part of the value object's definition. If that same Dictionary instance is also referenced from an entity property elsewhere in the graph, the entity's reference participates in tracking (via the outer options with `ReferenceHandler`), while the record's copy is logically independent. This is not a limitation -- it is correct. Entities have identity; value objects do not.

3. **The bypass converter enforces this semantic boundary.** `RecordBypassConverterFactory` claims types with parameterized constructors and serializes them without reference metadata. This is not "skipping" reference handling -- it is applying the correct serialization behavior for value objects.

### Two-Component Architecture

**Component 1: `NeatooPreserveReferenceHandler`** (already built in Phase 1)
- Custom `ReferenceHandler` subclass wired into `NeatooJsonSerializer`'s `JsonSerializerOptions`
- Delegates `CreateResolver()` to `NeatooReferenceResolver.Current`
- Gives STJ's built-in converters `$id`/`$ref` for mutable reference types (Dictionary, List, plain classes with default constructors)

**Component 2: `RecordBypassConverterFactory`** (new, ~50-80 lines)
- `JsonConverterFactory` that claims types with parameterized constructors
- Detection rule: type has a constructor with parameters AND is NOT already claimed by a Neatoo converter
- Delegates serialization to a cached inner `JsonSerializerOptions` identical to the outer options except `ReferenceHandler = null` and no `RecordBypassConverterFactory` (prevents recursion)
- Records and their entire subtree serialize without `$id`/`$ref`

**Why "all types with parameterized constructors" is the correct detection rule:**
- Records with primary constructors are the primary target
- Non-record classes with parameterized constructors are rare in the RemoteFactory ecosystem
- Even if such classes exist, not having reference tracking on them is low-risk -- the simpler rule avoids fragile heuristics that attempt to distinguish `record class Foo(int X)` from `class Foo(int X)` (STJ cannot distinguish them)
- The rule is easy to explain and document

### Two-Phase Strategy

**Phase 1: Reproduction and exploration** (COMPLETE) -- Created `SharedReferenceExplorationTests` in `RemoteFactory.IntegrationTests` with targeted tests. Created `NeatooPreserveReferenceHandler`. Confirmed custom `ReferenceHandler` works for mutable types. Confirmed STJ's parameterized-constructor limitation is permanent and comprehensive.

**Phase 2: Implementation** -- Build on Phase 1 artifacts:
1. Create `RecordBypassConverterFactory` (~50-80 lines) that claims parameterized-constructor types and delegates to inner options without `ReferenceHandler`.
2. Wire both `NeatooPreserveReferenceHandler` (already built) and `RecordBypassConverterFactory` (new) into `NeatooJsonSerializer`'s options.
3. Create Phase 2 acceptance tests (Scenarios 7-11).
4. Verify all existing tests pass unchanged -- `InterfaceFactory_NonNeatooType_NoRefMetadata` should pass without modification because the bypass converter claims the record before `ReferenceHandler` can add metadata.
5. Update documentation.

---

## Domain Model Behavioral Design

Not applicable. This work is in the serialization infrastructure layer, not in domain model behavior. No computed properties, reactive rules, or validation rules are involved.

---

## Design

### Component 1: NeatooPreserveReferenceHandler (already built)

```
NeatooPreserveReferenceHandler : ReferenceHandler
    CreateResolver() => NeatooReferenceResolver.Current
                        ?? throw InvalidOperationException("No active serialization")
```

This bridges STJ's `ReferenceHandler` API to the existing `NeatooReferenceResolver.Current` `AsyncLocal`. The resolver is created and managed by `NeatooJsonSerializer` (unchanged from v0.22.0). Already built and validated in Phase 1.

### Component 2: RecordBypassConverterFactory (new)

```
RecordBypassConverterFactory : JsonConverterFactory

    CanConvert(Type typeToConvert):
        - Return false if typeToConvert is already claimed by a Neatoo converter
          (implements a Neatoo marker interface or is handled by NeatooInterfaceJsonTypeConverter, etc.)
        - Return true if typeToConvert has a constructor with parameters
          (STJ's own heuristic: [JsonConstructor] or single public ctor with params)
        - Return false otherwise (mutable types go through STJ built-in + ReferenceHandler)

    CreateConverter(Type typeToConvert, JsonSerializerOptions options):
        - Return new RecordBypassConverter<T>(innerOptions)
        - innerOptions is lazily created and cached (one per outer options instance)

RecordBypassConverter<T> : JsonConverter<T>

    Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options):
        - return JsonSerializer.Deserialize<T>(ref reader, _innerOptions)

    Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options):
        - JsonSerializer.Serialize(writer, value, _innerOptions)
```

### Inner Options Construction

The inner `JsonSerializerOptions` is the critical piece. It must be identical to the outer options except:

1. **`ReferenceHandler = null`** -- no `$id`/`$ref` for the record's subtree
2. **`Converters` list excludes `RecordBypassConverterFactory`** -- prevents infinite recursion (record converter claims record, delegates to inner options, inner options have record converter, which claims record again...)
3. **All other settings preserved** -- `TypeInfoResolver`, `WriteIndented`, `IncludeFields`, other Neatoo converters

Construction approach using the .NET 9+ copy constructor:

```
var innerOptions = new JsonSerializerOptions(outerOptions);
innerOptions.ReferenceHandler = null;
// Remove RecordBypassConverterFactory from Converters
// (iterate and copy all except self)
```

The inner options are created once per outer options instance and cached. Thread safety is handled by the `AsyncLocal` scope of `NeatooReferenceResolver.Current` (each serialization operation is isolated).

### Modified NeatooJsonSerializer Constructor

```
Options = new JsonSerializerOptions
{
    TypeInfoResolver = neatooDefaultJsonTypeInfoResolver,
    ReferenceHandler = new NeatooPreserveReferenceHandler(),  // Component 1
    WriteIndented = ...,
    IncludeFields = true,
    Converters = {
        new RecordBypassConverterFactory(),  // Component 2 -- BEFORE other converters
        // ... existing Neatoo converters
    }
};
```

Converter ordering matters: `RecordBypassConverterFactory` must be added before Neatoo converters so it is checked first. However, its `CanConvert` returns `false` for Neatoo types (they have their own converters), so there is no conflict.

### Reference Flow After Change

```
NeatooJsonSerializer.Serialize()
    |
    +-- Creates NeatooReferenceResolver, sets .Current via AsyncLocal
    |
    +-- JsonSerializer.Serialize(target, OuterOptions)
            |
            +-- For types with Neatoo custom converters (entities/lists):
            |       Neatoo converter reads NeatooReferenceResolver.Current directly
            |       Emits $id/$ref under its own control
            |       (unchanged from v0.22.0)
            |
            +-- For types with parameterized constructors (records, etc.):
            |       RecordBypassConverterFactory.CanConvert() returns true
            |       RecordBypassConverter delegates to InnerOptions (no ReferenceHandler)
            |       STJ serializes the record and its entire subtree normally
            |       NO $id/$ref metadata anywhere in the record's subtree
            |       (DDD correct: value objects have no identity to track)
            |
            +-- For mutable types without custom converters (Dictionary, List, plain classes):
            |       STJ built-in converter handles serialization
            |       ReferenceHandler is active (OuterOptions has NeatooPreserveReferenceHandler)
            |       STJ calls CreateResolver() -> NeatooReferenceResolver.Current
            |       STJ emits $id/$ref using that resolver
            |
            +-- Neatoo converters and STJ built-in converters share the SAME resolver
            |       Cross-type references share the same ID space
    |
    +-- Clears .Current in finally block (unchanged)
```

### Why InterfaceFactory_NonNeatooType_NoRefMetadata Passes Unchanged

The existing test serializes a record with a parameterized constructor and asserts `DoesNotContain("$id")` and `DoesNotContain("$ref")`. With the bypass converter:

1. STJ encounters the record type
2. `RecordBypassConverterFactory.CanConvert()` returns `true` (parameterized constructor)
3. `RecordBypassConverter` serializes using inner options (no `ReferenceHandler`)
4. No `$id`/`$ref` metadata appears in the JSON
5. Test assertions pass unchanged

The test's original intent (records are not corrupted by reference metadata) is preserved by design, not by coincidence.

### File Changes (Phase 2)

| File | Change |
|------|--------|
| `src/RemoteFactory/Internal/NeatooPreserveReferenceHandler.cs` | Already exists from Phase 1 -- no change needed |
| `src/RemoteFactory/Internal/RecordBypassConverterFactory.cs` | NEW -- `JsonConverterFactory` + `JsonConverter<T>` that bypasses reference handling for parameterized-constructor types (~50-80 lines) |
| `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` | Add `ReferenceHandler = new NeatooPreserveReferenceHandler()` and `new RecordBypassConverterFactory()` to Options constructor. NOTE: `NeatooPreserveReferenceHandler` may already be wired from Phase 1 -- verify and add `RecordBypassConverterFactory` |
| `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/SharedReferenceTests.cs` | NEW -- Phase 2 acceptance tests (Scenarios 7-11) |
| `src/Tests/RemoteFactory.IntegrationTests/TestTargets/TypeSerialization/SharedReferenceTargets.cs` | May need additional target types for Phase 2 scenarios (e.g., record with nested Dictionary for Scenario 11) |
| `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs` | NO change expected -- bypass converter preserves existing behavior |
| `docs/serialization.md` | Update "Scope" section to reflect expanded reference handling (Step 9 documentation deliverable) |
| `docs/appendix/serialization.md` | No change needed (already promises universal shared identity) |
| `docs/appendix/record-reference-handling.md` | NEW -- Appendix doc explaining DDD rationale for bypass converter (Step 9 documentation deliverable) |
| `src/Design/CLAUDE-DESIGN.md` | Update Anti-Pattern 9 technical explanation (Step 9 documentation deliverable) |

---

## Implementation Steps

### Phase 1: Reproduction and Exploration

1. Create `SharedReferenceTargets.cs` with test target types:
   - `SharedDictionaryHolder` -- plain class with two `Dictionary<string,string>` properties
   - `CircularNode` -- plain class with `Name` and `Next` properties for circular reference testing
   - Reuse existing `InterfaceRecordWithCollection` for record testing

2. Create `SharedReferenceExplorationTests.cs` with tests for Scenarios 1-6:
   - Tests that document current behavior (expected failures that prove the problem exists)
   - Tests that explore `ReferenceHandler.Preserve` with records (confirm STJ limitation)
   - Tests that explore custom `ReferenceHandler` delegating to `NeatooReferenceResolver.Current`
   - Each test clearly documents what it proves and whether the result was expected

3. Run Phase 1 tests, analyze results, and document findings in developer memory file.

### Phase 2: Implementation (building on Phase 1 artifacts)

Phase 1 is complete. `NeatooPreserveReferenceHandler` is already built. Phase 2 adds the bypass converter and wires both components into `NeatooJsonSerializer`.

4. Create `RecordBypassConverterFactory.cs` in `src/RemoteFactory/Internal/`:
   - `RecordBypassConverterFactory : JsonConverterFactory` -- `CanConvert` returns `true` for types with parameterized constructors, `false` for types already claimed by Neatoo converters
   - `RecordBypassConverter<T> : JsonConverter<T>` -- delegates Read/Write to `JsonSerializer.Serialize/Deserialize` using cached inner `JsonSerializerOptions` with `ReferenceHandler = null` and no `RecordBypassConverterFactory`
   - Inner options created lazily, cached per outer options instance
   - Estimated ~50-80 lines total

5. Wire both components into `NeatooJsonSerializer`'s `JsonSerializerOptions`:
   - Verify `NeatooPreserveReferenceHandler` is already on `Options.ReferenceHandler` (from Phase 1). If not, add it.
   - Add `new RecordBypassConverterFactory()` to `Options.Converters` list, BEFORE existing Neatoo converters.

6. Create Phase 2 acceptance tests in `SharedReferenceTests.cs`:
   - Scenario 7: Shared Dictionary identity preserved after round-trip
   - Scenario 8: Record with reference-type constructor params round-trips with both components active
   - Scenario 9: Circular reference in plain class preserved after round-trip
   - Scenario 10: Cross-type shared reference (Neatoo entity + Dictionary)
   - Scenario 11: Record with nested mutable type in mixed graph -- record gets independent copy, mutable type outside record is tracked

7. Run full test suite:
   - All existing tests must pass with zero failures
   - `InterfaceFactory_NonNeatooType_NoRefMetadata` should pass unchanged (bypass converter prevents `$id`/`$ref` on records)
   - If this test unexpectedly fails, investigate -- the bypass converter should prevent this

8. Documentation updates are Step 9 deliverables (see Acceptance Criteria).

---

## Acceptance Criteria

### Phase 1 (COMPLETE)
- [x] Phase 1 exploration tests exist and document current behavior for all three scenarios
- [x] Phase 1 findings are documented (inherent STJ limitation confirmed -- permanent, comprehensive)
- [x] `NeatooPreserveReferenceHandler` built and validated

### Phase 2 (Implementation)
- [ ] `RecordBypassConverterFactory` created (~50-80 lines) claiming all types with parameterized constructors
- [ ] Both components wired into `NeatooJsonSerializer`'s `JsonSerializerOptions`
- [ ] Shared Dictionary identity is preserved after round-trip through `NeatooJsonSerializer` (Scenario 7)
- [ ] Records with parameterized constructors (including reference-type params) continue to deserialize without error (Scenario 8)
- [ ] Records produce JSON without `$id`/`$ref` metadata (Scenario 8)
- [ ] Circular references in plain classes are handled (no infinite loop, identity preserved) (Scenario 9)
- [ ] Cross-type shared references (Neatoo entity + plain Dictionary) work (Scenario 10)
- [ ] Record with nested mutable type in mixed graph: record gets independent copy, mutable type outside record is tracked (Scenario 11)
- [ ] Ordinal format serialization is unaffected (Scenario 12)
- [ ] All existing tests pass (zero failures) (Scenario 13)
- [ ] `InterfaceFactory_NonNeatooType_NoRefMetadata` test passes unchanged -- bypass converter prevents `$id`/`$ref` on records

**Documentation deliverables** (for Step 9):
- [x] NEW: `docs/appendix/record-reference-handling.md` -- Appendix-style document explaining the DDD rationale for why records bypass reference handling. Covers: value object semantics, three-path serialization architecture, nested reference types within records, the STJ parameterized-constructor limitation (dotnet/runtime#73302), detection rule, practical type category table, and reference flow diagram.
- [x] Update published docs (`docs/serialization.md`) -- Requirements documenter updated the "Scope" section to "Reference Handling by Type Category" with the three-path table and appendix cross-reference. Docs writer fixed duplicate sentence in the anti-pattern guidance.
- [x] Update `docs/appendix/serialization.md` -- Requirements documenter updated section 3 ("Shared Object Identity Is Lost") with expanded scope and cross-reference to the new appendix.
- [x] Update `CLAUDE-DESIGN.md` Anti-Pattern 9 technical explanation -- updated by requirements documenter: Anti-Pattern 9 explanation, Quick Decisions Table record row, and Common Mistakes #10 summary
- [ ] Release notes for the version bump -- separate deliverable

---

## Requirements Documentation (Step 9)

### Files Directly Updated

1. **`src/Design/CLAUDE-DESIGN.md`** -- Three updates:
   - Anti-Pattern 9 "Why it matters" explanation: replaced stale "no ReferenceHandler set" text with two-path strategy description (`NeatooPreserveReferenceHandler` for mutable types, `RecordBypassConverterFactory` for records)
   - Quick Decisions Table: updated record row rationale from "serialized without `$id`/`$ref`" to "bypass reference handling (`RecordBypassConverterFactory`)"
   - Common Mistakes #10: updated description to reference bypass converter behavior

2. **`docs/serialization.md`** -- Replaced "Scope: Converter-Level, Not Serializer-Level" section (lines 120-124) with "Reference Handling by Type Category" section containing: three-path type table, shared resolver explanation, DDD value object rationale for nested mutable types in records, and cross-link to the new appendix

3. **`docs/appendix/serialization.md`** -- Updated section 3 ("Shared Object Identity Is Lost") with expanded scope clarifying which types participate in reference tracking and cross-reference to the new record-reference-handling appendix

4. **`docs/appendix/record-reference-handling.md`** -- Already existed (created by prior agent). Verified content matches implementation. No changes needed.

### Developer Deliverables (Source Code)

1. **`src/Design/Design.Tests/FactoryTests/SerializationTests.cs:38`** -- Update the header comment. Line 38 currently says "Circular references without proper handling" under the "NO" list. After this implementation, circular references in mutable types ARE handled (via `NeatooPreserveReferenceHandler`). Change to: "Circular references in types with parameterized constructors (records)" or remove the line and add a note under "PARTIAL" explaining that circular references work for mutable types but not records.

2. **(Optional) Design.Tests shared-reference test** -- Consider adding a test to `SerializationTests.cs` that exercises shared object identity for a non-Neatoo mutable type (e.g., same Dictionary assigned to two properties on a `[Factory]` class). This would close Gap 10 from the requirements review ("None test shared object identity"). Not urgent since `SharedReferenceTests.cs` in IntegrationTests covers this thoroughly.

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Reproduction Tests | developer | Yes | COMPLETE -- exploration tests built, findings documented | None |
| Phase 2: Implementation | developer | Yes | Clean context; implements bypass converter and wires both components. Phase 1 findings and architect revision are known. | Phase 1 (complete) |

**Parallelizable phases:** None -- Phase 2 depends on Phase 1 artifacts.

**Notes:**
- Phase 1 is complete. Key findings to relay to Phase 2 developer:
  - `NeatooPreserveReferenceHandler` is already built and validated
  - STJ's parameterized-constructor limitation is permanent and comprehensive (throws for `$id` on reference-type constructor params, not just `$ref`)
  - Custom `ReferenceHandler` works correctly for mutable types (Dictionary, List, plain classes)
  - The only viable path for records is a custom converter that bypasses reference handling entirely
- Phase 2 developer receives the plan (this file) and the architect's memory file context (relayed by orchestrator). The implementation is straightforward: ~50-80 lines for the bypass converter, wiring into serializer options, and Phase 2 acceptance tests.
- No architect revision is expected during Phase 2 -- the design is decided and complete.

---

## Dependencies

- **STJ `ReferenceHandler` API:** The custom subclass approach relies on STJ calling `CreateResolver()` for each serialization operation. This is well-documented public API.
- **`NeatooReferenceResolver.Current` lifecycle:** Must remain created/disposed by `NeatooJsonSerializer` per serialize/deserialize call. No changes to lifecycle management.
- **Neatoo converters:** Must continue to work with the shared resolver. Since Neatoo converters access `NeatooReferenceResolver.Current` directly (not through `options.ReferenceHandler`), they are unaffected by adding `ReferenceHandler` to options.

---

## Risks / Considerations

1. **Inner options caching and thread safety:** The bypass converter creates a cached inner `JsonSerializerOptions`. The cache key is the outer options instance. Since `NeatooJsonSerializer` creates `Options` once in the constructor and reuses it, there is exactly one inner options instance per serializer. Thread safety is handled by `AsyncLocal` scoping of `NeatooReferenceResolver.Current` -- each serialization operation is isolated. Risk is LOW but the developer should verify the caching strategy does not cause issues under concurrent serialization.

2. **Converter ordering:** `RecordBypassConverterFactory` must be checked BEFORE Neatoo converters in the `Converters` list. If Neatoo converters are checked first and one claims a type that also has a parameterized constructor, the bypass converter is never reached. This is CORRECT behavior (Neatoo types should use Neatoo's converter), but the ordering must be intentional. Risk is LOW if the developer adds the factory first in the list.

3. **Detection over-claiming:** The bypass converter claims ALL types with parameterized constructors, not just C# `record` types. This means a non-record class like `class Foo { public Foo(int x) { X = x; } public int X { get; set; } }` would also bypass reference handling. In the RemoteFactory ecosystem, such types are rare. The user explicitly accepted this tradeoff: simpler detection rule avoids fragile heuristics. Risk is LOW.

4. **Performance:** Two sources of overhead: (a) `ReferenceHandler` causes STJ to check every mutable reference type instance against the resolver's dictionary -- same cost as `ReferenceHandler.Preserve`, a standard STJ feature. (b) The bypass converter delegates to a second `JsonSerializer.Serialize/Deserialize` call with inner options. This is a pass-through, not double serialization -- STJ writes directly to the same `Utf8JsonWriter`. Risk is LOW.

5. **Neatoo converter interaction:** Neatoo converters and STJ built-in converters will both write to the same `NeatooReferenceResolver`. The `$id` numbering will be interleaved (Neatoo gets some IDs, STJ gets others). This is correct behavior -- the resolver tracks all objects in the graph regardless of which converter serialized them.

6. **v0.22.0 principle tension:** Adding `ReferenceHandler` to options partially reverses v0.22.0's "no `ReferenceHandler` on options" decision. The resolution is clean: v0.22.0 was correct that Neatoo converters should not depend on `options.ReferenceHandler` (they access the resolver directly). The `ReferenceHandler` is for STJ's built-in converters only. The bypass converter extends the converter-level principle to records. All three paths (Neatoo converters, bypass converter, STJ built-in) coexist without conflict.

7. **Multi-targeting:** Must verify on both net9.0 and net10.0. The `JsonSerializerOptions` copy constructor is available in both. The STJ `ReferenceHandler` API is available in both.

8. **Ordinal format:** `NeatooOrdinalConverterFactory` only claims types implementing `IOrdinalSerializable`. Non-ordinal types in the same graph fall through to STJ built-in handling and will participate in `ReferenceHandler`-based reference tracking. The ordinal converter serializes as arrays -- STJ's `ReferenceHandler` does not interfere with custom converters that fully control their write output.

9. **Nested reference types within records (the DDD resolution):** A Dictionary inside a record constructor parameter is serialized without `$id`/`$ref` (part of the record's bypass subtree). If the same Dictionary instance is also referenced from a plain class property elsewhere in the graph, the plain class's reference participates in tracking but the record's copy does not. This means `ReferenceEquals` between them is `false` after round-trip. This is intentional and correct per DDD value object semantics -- but it should be documented clearly (see appendix deliverable) to prevent future sessions from treating it as a bug.
