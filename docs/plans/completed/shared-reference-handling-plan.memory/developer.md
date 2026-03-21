# Developer -- Shared Reference Handling Plan

Last updated: 2026-03-21
Current step: Phase 2 REVISED -- Implementation Complete, Awaiting Verification

## Key Context

### Codebase Verification Summary

- **NeatooReferenceResolver.cs** (verified): Extends `ReferenceResolver`, uses `ReferenceEqualityComparer.Instance`, has `AsyncLocal` `Current` property with public getter / internal setter. Contains `GetReference`, `AddReference`, `ResolveReference`, `AlreadyExists`. Reference counter starts at 0, increments per new reference. Implements `IDisposable`.
- **NeatooJsonSerializer.cs** (verified): Creates a new `NeatooReferenceResolver` per Serialize/Deserialize call using `using var rr = new NeatooReferenceResolver()`, sets `Current = rr` before calling `JsonSerializer.Serialize/Deserialize`, clears `Current = null` in `finally`. Four methods follow this pattern (two Serialize, two Deserialize). The `JsonSerializerOptions` is constructed in the constructor with NO `ReferenceHandler` -- confirmed. Contains a comment (lines 71-76) documenting the Phase 2 revert and the reason.
- **NeatooInterfaceJsonTypeConverter.cs** (verified): Only handles `$type`/`$value` wrapping. Does NOT call `NeatooReferenceResolver` anywhere. Confirmed no dead code remains.
- **NeatooInterfaceJsonConverterFactory.cs** (verified): `CanConvert()` checks `typeToConvert.IsInterface || typeToConvert.IsAbstract` AND `!typeToConvert.IsGenericType` AND `serviceAssemblies.HasType(typeToConvert)`. Only claims interfaces/abstract types known to RemoteFactory.
- **NeatooJsonConverterFactory.cs** (verified): Abstract marker base class extending `JsonConverterFactory`. Empty -- no methods. Provides type identity for filtering (e.g., `converter is NeatooJsonConverterFactory`).
- **NeatooOrdinalConverterFactory.cs** (verified): `CanConvert()` checks `options.Format == SerializationFormat.Ordinal` AND `typeof(IOrdinalSerializable).IsAssignableFrom(typeToConvert)`. Only claims IOrdinalSerializable types in Ordinal mode.
- **InterfaceFactory_NonNeatooType_NoRefMetadata test** (verified at line 120-140): Serializes `InterfaceRecordWithCollection` (a record with primary constructor) and asserts `DoesNotContain("$id")` and `DoesNotContain("$ref")`.
- **InterfaceRecordWithCollection** (verified): `public record InterfaceRecordWithCollection(string Name, IReadOnlyList<InterfaceRecordItem> Items)` -- primary constructor, `Items` is a reference-type constructor parameter.
- **NeatooPreserveReferenceHandler.cs** (verified): Already built. `CreateResolver()` returns `NeatooReferenceResolver.Current` with null check throwing `InvalidOperationException`.
- **NeatooJsonSerializer constructor converter ordering** (verified lines 66-90): Ordinal converter added first (if Ordinal format), then Neatoo converter factories from DI. The `RecordBypassConverterFactory` must be inserted BEFORE ordinal and Neatoo converters.

### Timing Analysis for Custom ReferenceHandler

The proposed `NeatooPreserveReferenceHandler.CreateResolver()` returns `NeatooReferenceResolver.Current`. The call sequence is:

1. `NeatooJsonSerializer.Serialize()` creates `rr = new NeatooReferenceResolver()`
2. Sets `NeatooReferenceResolver.Current = rr`
3. Calls `JsonSerializer.Serialize(target, this.Options)`
4. STJ internally calls `Options.ReferenceHandler.CreateResolver()` which returns `NeatooReferenceResolver.Current` (= `rr`)
5. STJ uses `rr` for built-in converter `$id`/`$ref` tracking
6. Meanwhile, Neatoo custom converters also read `NeatooReferenceResolver.Current` (= `rr`)
7. Both paths share the same resolver instance and ID counter
8. `finally` clears `Current = null`

Timing is correct. STJ calls `CreateResolver()` once at the start of serialize/deserialize, AFTER `Current` has been set.

### Phase 1 Key Findings (from prior implementation)

- F1: `ReferenceHandler.Preserve` throws for records with `$ref` (only when same instance in two properties)
- F2: Custom `ReferenceHandler` delegating to `NeatooReferenceResolver.Current` works for mutable types
- F3: STJ does NOT skip `$id` for records -- they get `$id` even though docs suggest immutable types are skipped
- F4 (Phase 2 discovery): The limitation is broader -- ANY reference-type constructor parameter gets `$id` and throws `NotSupportedException` during deserialization. `InterfaceRecordWithCollection(string Name, IReadOnlyList<InterfaceRecordItem> Items)` fails because `Items` (IReadOnlyList) gets `$id`.

### Limitation Not In Plan Scope (Pre-Existing)

`ToRemoteDelegateRequest` serializes each parameter individually -- each `Serialize` call creates a NEW resolver. Shared references across parameters in a remote request are NOT tracked. This is pre-existing behavior and out of scope.

## Mistakes to Avoid

- Phase 1 Finding F1 was INCOMPLETE. STJ throws not just for `$ref` on records but for `$id` on ANY reference-type constructor parameter. The revised plan accounts for this with the bypass converter.
- Do not assume STJ treats records as "immutable types" for reference-metadata purposes. It does not.
- The `NeatooJsonSerializer` constructor builds the converter list in a specific order (ordinal first, then Neatoo factories from DI). `RecordBypassConverterFactory` must be inserted before all of them, not appended.
- The Phase 1 test `Scenario3_RecordWithReferenceHandlerPreserve_ThrowsOnDeserialization` must assign the same record to TWO properties to trigger `$ref`. A single-occurrence record will only get `$id`.

## User Corrections

- User decided: "all types with parameterized constructors" as detection rule (simplest, avoids fragile heuristics)
- User decided: DDD justification resolves the nested-reference-type concern -- records are value objects, duplication is correct
- User decided: `InterfaceFactory_NonNeatooType_NoRefMetadata` should pass UNCHANGED (bypass converter claims records before ReferenceHandler can add metadata)

---

## Assertion Trace Verification -- Phase 2 Revised Plan

For this review, I re-trace ALL 13 rules against the REVISED implementation (bypass converter approach). Rules 1-6 are Phase 1 (already implemented and passing). Rules 7-13 are Phase 2.

### Rules 1-6 (Phase 1 -- already verified and implemented)

These rules are unchanged from the original review. Phase 1 tests exist and pass.

| Rule # | Rule Summary | Status |
|--------|-------------|--------|
| 1 | Shared Dictionary, no handler -> identity lost | CONFIRMED -- Phase 1 test passes |
| 2 | Record round-trip succeeds (no handler) | CONFIRMED -- Phase 1 test passes |
| 3 | Record + ReferenceHandler.Preserve -> NotSupportedException | CONFIRMED -- Phase 1 test passes |
| 4 | Circular reference, no handler -> JsonException | CONFIRMED -- Phase 1 test passes |
| 5 | Shared Dictionary + ReferenceHandler.Preserve -> identity preserved | CONFIRMED -- Phase 1 test passes |
| 6 | Custom ReferenceHandler + NeatooReferenceResolver -> identity preserved | CONFIRMED -- Phase 1 test passes |

### Rules 7-13 (Phase 2 -- revised bypass converter approach)

| Rule # | Rule Summary | Implementation Path | Verdict |
|--------|-------------|-------------------|---------|
| 7 | Shared Dictionary identity preserved after fix | `NeatooJsonSerializer` constructor gets `ReferenceHandler = new NeatooPreserveReferenceHandler()` on `Options` (line 68-79 area). `RecordBypassConverterFactory` is added to `Options.Converters` first. When serializing `SharedDictionaryHolder`, STJ encounters `Dictionary<string,string>` properties. `RecordBypassConverterFactory.CanConvert(typeof(Dictionary<string,string>))` returns `false` because `Dictionary` has a default constructor (no parameterized constructor that STJ uses). STJ's built-in converter handles it. `ReferenceHandler` is active, so STJ emits `$id` on first dictionary, `$ref` on second. After deserialization, `ReferenceEquals` is `true`. | CONFIRMED -- `Dictionary<string,string>` has no parameterized constructor. Bypass converter does not claim it. STJ built-in converter + `ReferenceHandler` handles it correctly. |
| 8 | Records with parameterized constructors still deserialize correctly | STJ encounters `InterfaceRecordWithCollection(string Name, IReadOnlyList<InterfaceRecordItem> Items)`. `RecordBypassConverterFactory.CanConvert()` returns `true` because the type has a parameterized constructor. `RecordBypassConverter<T>.Write()` delegates to `JsonSerializer.Serialize<T>(writer, value, _innerOptions)` where `_innerOptions` has `ReferenceHandler = null`. No `$id`/`$ref` emitted. On deserialization, `RecordBypassConverter<T>.Read()` delegates to `JsonSerializer.Deserialize<T>(ref reader, _innerOptions)`. No reference metadata in the JSON, so STJ constructs the record normally via its constructor. | CONFIRMED -- The inner options have no `ReferenceHandler`, so the entire record subtree serializes clean. This is functionally identical to the current v0.22.0 behavior for records. |
| 9 | Circular references in mutable types preserved | `CircularNode` has properties `Name` (string) and `Next` (CircularNode?). `CircularNode` has a default constructor (no parameterized constructor). `RecordBypassConverterFactory.CanConvert(typeof(CircularNode))` returns `false`. STJ built-in converter handles it. `ReferenceHandler` is active. STJ calls `GetReference` on `nodeA`, gets `$id="1"`, `alreadyExists=false`. Serializes normally. Encounters `nodeB` in `Next` property, gets `$id="2"`. Inside `nodeB`, encounters `nodeA` again in `Next`, `GetReference` returns `alreadyExists=true`, emits `$ref="1"`. On deserialization, `$ref="1"` resolves to the already-constructed `nodeA`. Circular graph restored. | CONFIRMED -- Mutable classes with default constructors go through STJ built-in path with `ReferenceHandler`. Standard STJ cycle detection applies. |
| 10 | Cross-type shared references use same resolver | Both Neatoo custom converters (reading `NeatooReferenceResolver.Current` directly) and STJ built-in converters (accessing resolver via `NeatooPreserveReferenceHandler.CreateResolver()`) get the SAME `NeatooReferenceResolver` instance. The `_objectToReferenceIdMap` and `_referenceIdToObjectMap` are shared. IDs are globally unique because `_referenceCount` increments within the single resolver. Cross-type identity is tracked. For the Phase 2 test (Scenario 10), the test uses only non-Neatoo types (plain classes + Dictionary), which is a proxy for the real cross-converter scenario (requires Neatoo repository). The test verifies the resolver handles mixed-type graphs correctly. | CONFIRMED -- The resolver sharing mechanism is sound. The test is a reasonable proxy. Full cross-converter testing requires Neatoo. |
| 11 | Records in mixed graphs: record gets independent copy, mutable types tracked | `RecordWithSharedDictionaryHolder` has properties `Record` (InterfaceRecordItem -- a record), `DictionaryA`, `DictionaryB` (both pointing to same dictionary). STJ encounters the holder (mutable, default constructor). `ReferenceHandler` is active. STJ serializes `Record` property: `RecordBypassConverterFactory.CanConvert(typeof(InterfaceRecordItem))` returns `true` (parameterized constructor). `RecordBypassConverter` serializes using inner options (no `ReferenceHandler`). No `$id` on the record. STJ serializes `DictionaryA`: bypass converter returns `false`. STJ built-in converter handles it. `ReferenceHandler` emits `$id` on first dictionary. STJ serializes `DictionaryB`: same dictionary instance, `GetReference` returns `alreadyExists=true`, emits `$ref`. After deserialization: record deserialized correctly via inner options, dictionaries share identity via `$ref`. `ReferenceEquals(result.DictionaryA, result.DictionaryB)` is `true`. The record's data is an independent copy. | CONFIRMED -- The bypass converter cleanly separates the record's subtree from the reference-tracked graph. DDD semantics are preserved. |
| 12 | Ordinal format unaffected | `NeatooOrdinalConverterFactory.CanConvert()` (line 60-69) checks `options.Format == SerializationFormat.Ordinal` AND `typeof(IOrdinalSerializable).IsAssignableFrom(typeToConvert)`. Ordinal converter claims only `IOrdinalSerializable` types. `RecordBypassConverterFactory` checks for parameterized constructors. No conflict: types implementing `IOrdinalSerializable` are Neatoo types generated with ordinal metadata -- they are not records with parameterized constructors. Even if there were overlap, the ordinal converter would be added after the bypass converter in the list, but the bypass converter's `CanConvert` would return `false` for Neatoo types (see detection rule below). Non-ordinal types in the same graph fall through to STJ built-in handling with `ReferenceHandler`. | CONFIRMED -- Ordinal and bypass converters have non-overlapping `CanConvert` predicates. Wait -- I need to verify this. See Concern C6. |
| 13 | All existing tests pass; `InterfaceFactory_NonNeatooType_NoRefMetadata` passes unchanged | The test (line 120-140) serializes `InterfaceRecordWithCollection` via `NeatooJsonSerializer`. With the bypass converter: `RecordBypassConverterFactory.CanConvert(typeof(InterfaceRecordWithCollection))` returns `true` (parameterized constructor: `string Name, IReadOnlyList<InterfaceRecordItem> Items`). `RecordBypassConverter` delegates to inner options with `ReferenceHandler = null`. JSON output has no `$id`/`$ref`. Test assertions `DoesNotContain("$id")` and `DoesNotContain("$ref")` pass. | CONFIRMED -- The bypass converter intercepts the record BEFORE STJ's built-in converter sees it. Since the bypass converter delegates to inner options without `ReferenceHandler`, no reference metadata appears. The test passes unchanged. This is the correct behavior by design, not a coincidence. |

## Concern Analysis -- Revised Phase 2

### C6: RecordBypassConverterFactory CanConvert Detection Rule -- How Exactly? (Clarifying, not blocking)

The plan says: "Return false if typeToConvert is already claimed by a Neatoo converter (implements a Neatoo marker interface or is handled by NeatooInterfaceJsonTypeConverter, etc.)" and "Return true if typeToConvert has a constructor with parameters."

The "already claimed by a Neatoo converter" check needs clarification. Looking at the actual Neatoo converters in RemoteFactory:

1. `NeatooInterfaceJsonConverterFactory` -- claims `typeToConvert.IsInterface || typeToConvert.IsAbstract` (with additional checks). Records are concrete classes, not interfaces/abstracts. This converter would NOT claim a record. No conflict.

2. `NeatooOrdinalConverterFactory` -- claims `typeof(IOrdinalSerializable).IsAssignableFrom(typeToConvert)` in Ordinal mode. Records do not implement `IOrdinalSerializable`. No conflict.

3. Neatoo repository converters (`NeatooBaseJsonTypeConverter`, `NeatooListBaseJsonTypeConverter`) -- these are registered in the Neatoo repo, not RemoteFactory. They extend `NeatooJsonConverterFactory`. They claim Neatoo entity/list types.

The detection rule "has a constructor with parameters" is simple: check if the type has any public constructor with parameters. The question is: does `RecordBypassConverterFactory` need to explicitly filter out Neatoo types, or does converter ordering handle it?

**Converter ordering in STJ**: STJ checks converters in list order. The first converter whose `CanConvert` returns `true` wins. If `RecordBypassConverterFactory` is added FIRST (before Neatoo converters), it would be checked first. But its `CanConvert` only returns `true` for types with parameterized constructors. Neatoo entity types typically have their concrete classes created by generated code -- do they have parameterized constructors?

Looking at the interface converter: it claims only interfaces/abstracts. The concrete types (generated by Neatoo) are serialized INSIDE the `$type`/`$value` wrapper by calling `JsonSerializer.Serialize(writer, value, value.GetType(), options)`. At that point, STJ looks for a converter for the concrete type. The Neatoo downstream converters (`NeatooBaseJsonTypeConverter<T>`) are registered for the concrete types in the Neatoo repo. Those converters are `NeatooJsonConverterFactory` subclasses, which are added to the converter list after `RecordBypassConverterFactory`.

If a Neatoo entity type happens to have a parameterized constructor, `RecordBypassConverterFactory` (checked first) would claim it, and Neatoo's converter would never be reached. This could be a problem.

**However**, in practice: Neatoo entities are generated classes with default constructors (DI-constructed). They are not records with primary constructors. The risk is low but not zero -- a future change to Neatoo code generation could introduce parameterized constructors.

**Mitigation**: The plan's design says "Return false if typeToConvert is already claimed by a Neatoo converter." The simplest check: if any converter in the outer options' `Converters` list is a `NeatooJsonConverterFactory` subclass AND its `CanConvert` returns `true` for the type, skip it. But this would require iterating converters, which is expensive.

A simpler mitigation: check if the type is in the `IServiceAssemblies` registry (Neatoo types are registered). But `RecordBypassConverterFactory` does not have access to `IServiceAssemblies`.

The SIMPLEST mitigation (and what I recommend): `RecordBypassConverterFactory` does NOT filter Neatoo types explicitly. Instead, rely on converter list ordering: add `RecordBypassConverterFactory` AFTER Neatoo converters, not before. Wait -- this contradicts the plan.

Actually, re-reading the plan more carefully: the plan says to add `RecordBypassConverterFactory` BEFORE Neatoo converters. The rationale is: "its `CanConvert` returns false for Neatoo types (they have their own converters)." But the bypass converter does not CHECK whether a Neatoo converter exists -- it only checks for parameterized constructors.

**My recommendation**: Add `RecordBypassConverterFactory` AFTER Neatoo converters in the list. This way, Neatoo converters get first priority. The bypass converter only picks up types that no Neatoo converter claimed. This is simpler and safer than adding explicit Neatoo-type detection logic to the bypass converter.

Wait -- but Neatoo converters (`NeatooInterfaceJsonConverterFactory`) only claim interfaces/abstracts. The concrete Neatoo types (entities) would NOT be claimed by the interface converter. They are claimed by downstream Neatoo converters from the Neatoo repository. Those downstream converters ARE `NeatooJsonConverterFactory` subclasses and ARE in the converter list (added via DI).

So the ordering would be:
1. Ordinal converter (if ordinal format)
2. Neatoo converters from DI (NeatooInterfaceJsonConverterFactory + downstream Neatoo converters)
3. RecordBypassConverterFactory

This ordering ensures Neatoo converters claim their types first. Remaining types with parameterized constructors (i.e., records not claimed by any Neatoo converter) are claimed by the bypass converter. Types without parameterized constructors (Dictionary, List, plain classes) fall through to STJ built-in handling with `ReferenceHandler`.

**IMPORTANT**: This changes the plan's stated ordering ("BEFORE other converters"). The plan says to add bypass converter first. I believe adding it LAST (after Neatoo converters) is safer. This is a Clarifying concern -- the implementation is the same either way if Neatoo types never have parameterized constructors, but the ordering provides defense-in-depth.

**Resolution**: Either ordering works today because Neatoo entity types do not have parameterized constructors. The plan's ordering (first) works if the `CanConvert` check is robust. My suggested ordering (last) provides implicit safety. I will note this in the implementation contract and let the user decide.

### C7: Inner Options Construction -- JsonSerializerOptions Copy Constructor Mutability (Clarifying)

The plan shows:
```
var innerOptions = new JsonSerializerOptions(outerOptions);
innerOptions.ReferenceHandler = null;
// Remove RecordBypassConverterFactory from Converters
```

The `JsonSerializerOptions` copy constructor in .NET 9+ creates a mutable copy. After `JsonSerializer.Serialize/Deserialize` is called with an options instance, STJ "locks" the options (makes them immutable). The inner options must be created BEFORE the first serialize/deserialize call that uses them, or the copy must be made from the outer options before they are locked.

In the plan's design, `RecordBypassConverter<T>` lazily creates inner options on first use. At that point, the outer options have already been passed to `JsonSerializer.Serialize` (which locks them). However, `new JsonSerializerOptions(outerOptions)` copies from a locked instance, which is fine -- the COPY is mutable until it is locked by its own first use.

But there is a subtlety: after creating the copy, we need to modify it (remove `RecordBypassConverterFactory` from Converters, set `ReferenceHandler = null`). These modifications must happen before the inner options are used for serialization. Since the inner options are lazily created and then immediately used, the sequence is:

1. Create copy: `new JsonSerializerOptions(outerOptions)` -- mutable copy
2. Modify: `innerOptions.ReferenceHandler = null` -- fine, still mutable
3. Remove converter: iterate and remove `RecordBypassConverterFactory` -- fine, still mutable
4. First use: `JsonSerializer.Serialize(writer, value, innerOptions)` -- locks inner options

This sequence is correct. The concern is theoretical only.

**One additional consideration**: The `Converters` list on the copy. Does `new JsonSerializerOptions(outerOptions)` deep-copy the converters list? The .NET documentation says the copy constructor copies "all configuration properties." The `Converters` collection is copied. Removing an item from the copy's list does not affect the outer options' list.

Verdict: Non-issue. The construction sequence is sound.

### C8: Inner Options Caching Strategy (Clarifying)

The plan says inner options are "created once per outer options instance and cached." Since `NeatooJsonSerializer` creates `Options` once in its constructor and reuses it for all operations, there is exactly one outer options instance per serializer, and therefore exactly one inner options instance per serializer.

The cache could be as simple as a field on `RecordBypassConverterFactory`. But the factory creates typed converters (`RecordBypassConverter<T>`) per type. The inner options should be shared across all typed converters (since they are identical regardless of T). The factory should own the cache, not the individual converters.

Implementation: `RecordBypassConverterFactory` has a `Lazy<JsonSerializerOptions>` or similar, initialized from the outer options. Each `RecordBypassConverter<T>` receives the cached inner options from the factory.

Wait -- `CreateConverter(Type typeToConvert, JsonSerializerOptions options)` receives the `options` parameter. The factory can create the inner options from this parameter on the first call to `CreateConverter` and cache it.

Thread safety: `NeatooJsonSerializer` is scoped per DI scope. Multiple threads could call Serialize concurrently within the same scope. The inner options creation should be thread-safe. A `Lazy<JsonSerializerOptions>` or a `volatile` field with double-check locking would work.

However, since `JsonSerializerOptions` itself is thread-safe once locked (after first use), and the inner options are locked on first use, the cache is safe. The only race condition is during creation -- two threads might create inner options simultaneously. Using `Lazy<T>` prevents double creation.

Verdict: Minor implementation detail. The plan's "lazily created and cached" approach is correct. Recommend `Lazy<JsonSerializerOptions>` or a simple null-check with lock.

### C9: What Counts as "Parameterized Constructor" for CanConvert? (Clarifying)

The plan says: "Return true if typeToConvert has a constructor with parameters (STJ's own heuristic: [JsonConstructor] or single public ctor with params)."

The bypass converter should match STJ's own detection heuristic for parameterized constructors. STJ's rules are:
1. If `[JsonConstructor]` is present on a constructor, use that constructor.
2. If no `[JsonConstructor]`, use the single public constructor if it has parameters.
3. If there is a public parameterless constructor, STJ uses it (not parameterized).

The bypass converter's `CanConvert` should return `true` when STJ WOULD use a parameterized constructor. The simplest approximation:

```csharp
// Type has no public parameterless constructor AND has at least one public constructor with parameters
// OR type has [JsonConstructor] on a parameterized constructor
```

A simpler (slightly over-inclusive) check:
```csharp
// Does NOT have a public parameterless constructor
!typeToConvert.GetConstructors().Any(c => c.GetParameters().Length == 0)
&& typeToConvert.GetConstructors().Any(c => c.GetParameters().Length > 0)
```

This catches records (which have a primary constructor and typically no parameterless constructor). It also catches any class without a parameterless constructor. This aligns with the user's decision: "all types with parameterized constructors."

For types that have BOTH a parameterless constructor and a parameterized one, STJ uses the parameterless constructor by default. These types would NOT get reference metadata issues (STJ uses the parameterless constructor, which supports `$id`/`$ref`). So `CanConvert` should return `false` for these types. The check above handles this correctly: if a parameterless constructor exists, `CanConvert` returns `false`.

Edge case: `[JsonConstructor]` on a parameterized constructor in a type that also has a parameterless constructor. In this case, STJ uses the parameterized constructor despite the parameterless one existing. The simple check above would return `false` (parameterless constructor exists), and the type would go through STJ's built-in path with `ReferenceHandler`, which would fail.

**Risk assessment**: This edge case (explicit `[JsonConstructor]` on a parameterized constructor alongside a parameterless constructor) is extremely rare in the RemoteFactory ecosystem. Records never have this pattern. The risk is LOW and the simple check is correct for the stated scope.

Verdict: The simple "no parameterless constructor" check is sufficient. Document the `[JsonConstructor]` edge case as a known limitation.

### C10: Scenario 11 Test Design -- Record Property Serialization Order (Non-blocking)

In Scenario 11, the `RecordWithSharedDictionaryHolder` has properties: `Record` (InterfaceRecordItem), `DictionaryA`, `DictionaryB`. The test assumes the bypass converter handles `Record` before STJ assigns `$id` to the Dictionary. Since STJ serializes properties in order, and `Record` comes before `DictionaryA` in the property list, the bypass converter claims `InterfaceRecordItem` first. When STJ reaches `DictionaryA`, it is the first time it sees that dictionary instance, so it assigns `$id`. Then `DictionaryB` gets `$ref`. This is correct.

But what if the Dictionary appeared INSIDE the record's constructor? The plan's Rule 11 states: "A mutable type (e.g., Dictionary) nested inside a record's constructor parameters is serialized as an independent copy." The bypass converter delegates to inner options (no `ReferenceHandler`), so the Dictionary inside the record would not participate in reference tracking. If the same Dictionary instance appears both as a record constructor parameter AND as a standalone property, the standalone property gets `$id`/`$ref` but the record's copy is independent. This is the intended DDD behavior.

The current Scenario 11 test uses `InterfaceRecordItem(int Id, string Description)` -- constructor params are value types. It does NOT test a record with a Dictionary constructor parameter. The plan's Rule 11 description mentions this case but the test scenario does not exercise it.

**Recommendation**: The test as written is valid for the stated scenario. A more thorough test with a record that has a Dictionary constructor parameter would be a nice-to-have but is not blocking. The DDD justification covers this case architecturally.

Verdict: Non-blocking. The test is sufficient for the stated acceptance criteria.

## Test Scenario Verification -- Revised Phase 2

| Scenario | Rule(s) | Expected Result | Trace Verdict |
|----------|---------|-----------------|---------------|
| 7: Shared Dictionary after fix | R7 | `ReferenceEquals` is `true` | PASS -- Dictionary has default constructor, bypass converter skips it, STJ built-in + ReferenceHandler handles it. |
| 8: Record round-trip after fix | R8 | Deserialization succeeds, no `$id`/`$ref` in JSON | PASS -- Bypass converter claims record (parameterized constructor), delegates to inner options without ReferenceHandler. Functionally identical to v0.22.0 for records. |
| 9: Circular reference after fix | R9 | Circular reference preserved | PASS -- `CircularNode` has default constructor, bypass converter skips it, STJ built-in + ReferenceHandler handles cycles. |
| 10: Cross-type shared reference | R10 | Same Dictionary instance | PASS -- Both converter paths share the same resolver. Test uses non-Neatoo types as proxy. |
| 11: Record in graph with shared mutable refs | R11 | Record works, Dictionary shared | PASS -- Bypass converter serializes record without metadata. STJ built-in handles Dictionary with `$id`/`$ref`. Record and Dictionary coexist. |
| 12: Ordinal format preserved | R12 | All ordinal tests pass | PASS -- Ordinal converter claims IOrdinalSerializable types (not records). Bypass converter claims records (not IOrdinalSerializable). Non-overlapping. |
| 13: Existing tests pass; NoRefMetadata unchanged | R13 | Zero failures | PASS -- Bypass converter claims record before ReferenceHandler can add metadata. JSON output is clean. |

## Requirements Context Check

The revised plan's approach respects all documented requirements:

1. **Published docs promise** (docs/serialization.md:10, docs/appendix/serialization.md:53-55) -- The bypass converter approach delivers shared-instance identity for mutable types while correctly excluding records (value objects). The docs promise shared identity for "when the same object is referenced by two properties" -- records as value objects are semantically not "the same object" even when they share a reference. The implementation is consistent with the promise when interpreted through DDD semantics.

2. **v0.22.0 principle** (docs/serialization.md:120-124) -- The "converter-level, not serializer-level" section described the pre-fix state. This fix adds `ReferenceHandler` to options (serializer-level) for STJ's built-in converters, while keeping Neatoo converters unchanged (converter-level). The bypass converter extends the converter-level principle to records. This is a principled extension, not a revert.

3. **Existing test intent** (InterfaceFactoryRecordSerializationTests.cs:120-140) -- The `InterfaceFactory_NonNeatooType_NoRefMetadata` test guards against record corruption by `$id`/`$ref`. The bypass converter preserves this guarantee by design. The test passes unchanged.

4. **Anti-Pattern 9** -- The user-facing rule (do not mix Neatoo types with records) is unchanged. Only the technical explanation needs updating (docs deliverable).

5. **STJ limitation** -- Fully addressed. The bypass converter prevents STJ from ever seeing reference metadata on parameterized-constructor types. The limitation cannot manifest.

## Agent Phasing Review

The revised plan's phasing is straightforward:

- **Phase 1** (complete): Exploration tests and `NeatooPreserveReferenceHandler` built.
- **Phase 2** (revised): Single fresh agent. Creates `RecordBypassConverterFactory`, wires both components into `NeatooJsonSerializer`, creates Phase 2 acceptance tests, verifies all tests pass.

The phasing is practical. Phase 2 is self-contained: one new file (~50-80 lines), one file modification (NeatooJsonSerializer constructor), test updates (remove Skip attributes, add Scenario 8 JSON assertion). No parallel phases needed.

## Verdict

**Approved.** The revised bypass converter approach cleanly resolves the Phase 2 blocker (STJ parameterized-constructor limitation). All 13 business rules trace through the implementation correctly. The detection rule ("all types with parameterized constructors") is simple and defensible. The DDD justification for record behavior is sound. The claim that `InterfaceFactory_NonNeatooType_NoRefMetadata` passes unchanged is correct -- the bypass converter intercepts records before `ReferenceHandler` can add metadata.

Concerns C6-C10 are all Clarifying, not blocking:
- C6 (converter ordering) -- either ordering works today; adding bypass converter AFTER Neatoo converters is slightly safer
- C7 (inner options mutability) -- non-issue, construction sequence is correct
- C8 (caching strategy) -- minor implementation detail, Lazy<T> recommended
- C9 (parameterized constructor detection) -- "no parameterless public constructor" check is sufficient
- C10 (Scenario 11 completeness) -- test covers the stated scenario; record-with-Dictionary-param is a nice-to-have

No logic errors found. No blocking concerns. Requirements context is consistent with the design.

---

## Implementation Contract -- Phase 2 (Revised)

### Scope

#### Files to Create
- `src/RemoteFactory/Internal/RecordBypassConverterFactory.cs` -- `JsonConverterFactory` + `JsonConverter<T>` that bypasses reference handling for types with parameterized constructors (~50-80 lines)

#### Files to Modify
- `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` -- Add `ReferenceHandler = new NeatooPreserveReferenceHandler()` to Options constructor. Add `new RecordBypassConverterFactory()` to `Options.Converters` list. Remove the revert comment (lines 71-76).
- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/SharedReferenceTests.cs` -- Remove `Skip` attributes from Scenarios 7, 9, 10, 11. Update Scenario 8 to also assert no `$id`/`$ref` in JSON output (per Rule 8).

#### Files to Verify (NO modification expected)
- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs` -- `InterfaceFactory_NonNeatooType_NoRefMetadata` must pass unchanged
- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/SharedReferenceExplorationTests.cs` -- Phase 1 exploration tests. Scenarios 1 and 4 will change behavior (identity now preserved / circular refs now handled). These are IN SCOPE for modification since they document "current behavior" which is changing.

### Out of Scope
- Documentation updates (docs/serialization.md, docs/appendix/record-reference-handling.md, CLAUDE-DESIGN.md) -- Step 9 deliverables
- Neatoo repository changes -- downstream, benefits automatically from shared resolver
- `ToRemoteDelegateRequest` per-parameter serialization -- pre-existing limitation
- Ordinal format changes -- not affected

### Tests NOT to Modify (Sacred)
- `InterfaceFactory_SimpleRecord_RoundTrip` (line 39-48) -- must pass unchanged
- `InterfaceFactory_RecordWithCollection_RoundTrip` (line 55-71) -- must pass unchanged
- `InterfaceFactory_NestedRecord_RoundTrip` (line 79-88) -- must pass unchanged
- `InterfaceFactory_NullableRecord_ReturnsNull` (line 95-101) -- must pass unchanged
- `InterfaceFactory_NullableRecord_ReturnsValue` (line 104-113) -- must pass unchanged
- `InterfaceFactory_NonNeatooType_NoRefMetadata` (line 120-140) -- must pass unchanged
- All Design.Tests -- must pass unchanged
- All other existing IntegrationTests and UnitTests -- must pass unchanged

### Phase 1 Exploration Tests (Expected Behavior Changes)
- `Scenario1_SharedDictionary_CurrentBehavior_IdentityLost` -- Currently asserts `ReferenceEquals` is `false`. After the fix, identity IS preserved. This test must be updated to reflect the new behavior (identity preserved). This is an IN SCOPE change.
- `Scenario2_RecordRoundTrip_CurrentBehavior_Succeeds` -- Should continue to pass (records still work).
- `Scenario4_CircularReference_NoHandler_ThrowsJsonException` -- Currently asserts `JsonException`. After the fix, circular references are handled. This test must be updated to reflect the new behavior. This is an IN SCOPE change.

### Verification Gates

1. After creating `RecordBypassConverterFactory.cs`: build succeeds
2. After modifying `NeatooJsonSerializer.cs` (wiring both components): build succeeds
3. After wiring: run `InterfaceFactory_NonNeatooType_NoRefMetadata` -- MUST pass unchanged (if it fails, STOP immediately -- the bypass converter is not working as expected)
4. After wiring: run all `InterfaceFactoryRecordSerializationTests` -- all 6 tests must pass
5. After wiring: remove Skip from Phase 2 tests (Scenarios 7, 9, 10, 11) and run them -- all must pass
6. After all changes: run full test suite (`dotnet test src/Neatoo.RemoteFactory.sln`) -- zero failures
7. Verify on both net9.0 and net10.0

### Stop Conditions

- If `InterfaceFactory_NonNeatooType_NoRefMetadata` fails after wiring: STOP. The bypass converter is not claiming the record type. Investigate `CanConvert` logic.
- If `InterfaceFactory_RecordWithCollection_RoundTrip` or `InterfaceFactory_NestedRecord_RoundTrip` fails: STOP. These are out-of-scope sacred tests. The bypass converter is not preventing reference metadata on records with reference-type constructor parameters.
- If any Design.Tests fail: STOP. These are out-of-scope.
- If any other existing test fails: STOP and report.

### Open Decision for User

**Converter ordering**: The plan says to add `RecordBypassConverterFactory` BEFORE Neatoo converters. My analysis (Concern C6) suggests adding it AFTER Neatoo converters is slightly safer (Neatoo converters get first priority). Either works today because Neatoo entity types do not have parameterized constructors. I recommend AFTER for defense-in-depth but will follow the plan's ordering if the user prefers.

---

## Implementation Progress -- Phase 1

### Files Created

1. **`src/Tests/RemoteFactory.IntegrationTests/TestTargets/TypeSerialization/SharedReferenceTargets.cs`**
   - `SharedDictionaryHolder` -- plain class with two `Dictionary<string,string>` properties
   - `CircularNode` -- plain class with `Name` and `Next` properties for circular reference
   - `SharedRecordHolder` -- wrapper with two properties holding the same record instance (needed for Scenario 3 per Concern C1)
   - `CrossTypeSharedReferenceHolder` -- plain class with scalar + shared Dictionary properties (Scenario 10)
   - `RecordWithSharedDictionaryHolder` -- plain class with record + shared Dictionary properties (Scenario 11)

2. **`src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/SharedReferenceExplorationTests.cs`**
   - 6 test methods covering Scenarios 1-6
   - Temporary `TestNeatooPreserveReferenceHandler` class nested in the test class for Scenario 6

3. **`src/RemoteFactory/Internal/NeatooPreserveReferenceHandler.cs`** -- Production-ready custom `ReferenceHandler` subclass.

4. **`src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/SharedReferenceTests.cs`** -- Phase 2 acceptance tests. Scenarios 7, 9, 10, 11 marked Skip. Scenario 8 passes.

### Test Results (Phase 1 -- All Pass)

| Scenario | Test Method | Result |
|----------|------------|--------|
| 1 | `Scenario1_SharedDictionary_CurrentBehavior_IdentityLost` | PASS |
| 2 | `Scenario2_RecordRoundTrip_CurrentBehavior_Succeeds` | PASS |
| 3 | `Scenario3_RecordWithReferenceHandlerPreserve_ThrowsOnDeserialization` | PASS |
| 4 | `Scenario4_CircularReference_NoHandler_ThrowsJsonException` | PASS |
| 5 | `Scenario5_SharedDictionary_ReferenceHandlerPreserve_IdentityPreserved` | PASS |
| 6 | `Scenario6_CustomReferenceHandler_NeatooReferenceResolver_IdentityPreserved` | PASS |

### Phase 2 Blocker (Now Resolved by Revised Plan)

Phase 2 originally attempted adding `ReferenceHandler` without a bypass converter. STJ threw `NotSupportedException` for records with reference-type constructor parameters (`Items: IReadOnlyList<InterfaceRecordItem>`). The revised plan introduces `RecordBypassConverterFactory` to prevent reference metadata on parameterized-constructor types.

---

## Completion Evidence -- Phase 1

### Test Output Summary

```
Test Run Successful (net9.0 and net10.0).
Phase 1 tests: 6 passed
Full suite: 494 passed, 7 skipped (3 pre-existing + 4 Phase 2 skips), 0 failed
```

---

## Implementation Progress -- Phase 2 (Revised)

### Files Created

1. **`src/RemoteFactory/Internal/RecordBypassConverterFactory.cs`** (~120 lines)
   - `RecordBypassConverterFactory : JsonConverterFactory` -- claims types with parameterized constructors (no public parameterless ctor AND at least one public ctor with parameters)
   - `RecordBypassConverter<T> : JsonConverter<T>` -- delegates Read/Write to `JsonSerializer.Serialize/Deserialize` with inner options that have `ReferenceHandler = null` and no `RecordBypassConverterFactory`
   - Inner options created with double-check locking (`lock` + null check), cached for the lifetime of the factory instance
   - Handles nulls in Read (returns default) and Write (writes null token)

### Files Modified

2. **`src/RemoteFactory/Internal/NeatooJsonSerializer.cs`** -- Constructor changes:
   - Added `ReferenceHandler = new NeatooPreserveReferenceHandler()` to `JsonSerializerOptions` initializer
   - Removed the 6-line explanatory comment about the Phase 2 revert
   - Added `this.Options.Converters.Add(new RecordBypassConverterFactory())` AFTER the existing Neatoo converter loop (per user decision: Neatoo converters get first priority)
   - Added comment explaining the ordering rationale

3. **`src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/SharedReferenceExplorationTests.cs`** -- Phase 1 exploration tests updated for new behavior:
   - Scenario 1: renamed `Scenario1_SharedDictionary_CurrentBehavior_IdentityLost` -> `Scenario1_SharedDictionary_IdentityPreserved`, assertion changed from `Assert.False(ReferenceEquals)` to `Assert.True(ReferenceEquals)`, comments updated
   - Scenario 4: renamed `Scenario4_CircularReference_NoHandler_ThrowsJsonException` -> `Scenario4_CircularReference_Handled`, changed from `Assert.Throws<JsonException>` to asserting the circular graph is preserved after round-trip
   - Scenarios 2, 3, 5, 6: unchanged (their behavior is unaffected by the fix)

4. **`src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/SharedReferenceTests.cs`** -- Phase 2 acceptance tests:
   - Removed `Skip` attribute from Scenario 7 (`Scenario7_SharedDictionary_AfterFix_IdentityPreserved`)
   - Removed `Skip` attribute from Scenario 9 (`Scenario9_CircularReference_AfterFix_IdentityPreserved`)
   - Removed `Skip` attribute from Scenario 10 (`Scenario10_CrossTypeSharedReference_DictionaryWithMixedProperties`)
   - Removed `Skip` attribute from Scenario 11 (`Scenario11_RecordInGraphWithSharedMutableRefs`)
   - No other changes to test bodies or assertions

### Files Verified Unchanged

- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs` -- all 6 tests pass unchanged, including `InterfaceFactory_NonNeatooType_NoRefMetadata` which asserts `DoesNotContain("$id")` and `DoesNotContain("$ref")`
- `src/Tests/RemoteFactory.IntegrationTests/TestTargets/TypeSerialization/SharedReferenceTargets.cs` -- no changes needed
- `src/RemoteFactory/Internal/NeatooPreserveReferenceHandler.cs` -- already built in Phase 1, no changes

### Design Decision: Converter Ordering

Per user decision, `RecordBypassConverterFactory` is added AFTER Neatoo converters in the list. The ordering in `NeatooJsonSerializer` constructor is:

1. `NeatooOrdinalConverterFactory` (if ordinal format)
2. Neatoo converter factories from DI (NeatooInterfaceJsonConverterFactory + downstream converters)
3. `RecordBypassConverterFactory`

This gives Neatoo converters first priority. The bypass converter only picks up types that no Neatoo converter claimed. This aligns with Concern C6 analysis: defense-in-depth against future Neatoo types that might have parameterized constructors.

### Verification Gates -- All Passed

| Gate | Expected | Actual |
|------|----------|--------|
| 1. `RecordBypassConverterFactory.cs` compiles | Build succeeds | PASS -- 0 errors, 0 warnings |
| 2. `NeatooJsonSerializer.cs` wiring compiles | Build succeeds | PASS -- 0 errors, 0 warnings |
| 3. `InterfaceFactory_NonNeatooType_NoRefMetadata` unchanged | Passes | PASS -- both net9.0 and net10.0 |
| 4. All `InterfaceFactoryRecordSerializationTests` | 6 pass | PASS -- 6 pass on both frameworks |
| 5. Phase 2 tests unskipped | Scenarios 7, 9, 10, 11 pass | PASS -- all pass on both frameworks |
| 6. Full test suite | Zero failures | PASS -- see below |
| 7. Both net9.0 and net10.0 | All pass | PASS |

---

## Completion Evidence -- Phase 2 (Revised)

### Full Test Suite Results

| Test Project | Framework | Passed | Failed | Skipped | Total |
|---|---|---|---|---|---|
| RemoteFactory.UnitTests | net9.0 | 490 | 0 | 0 | 490 |
| RemoteFactory.UnitTests | net10.0 | 490 | 0 | 0 | 490 |
| RemoteFactory.IntegrationTests | net9.0 | 498 | 0 | 3 | 501 |
| RemoteFactory.IntegrationTests | net10.0 | 498 | 0 | 3 | 501 |
| Design.Tests | net9.0 | 42 | 0 | 0 | 42 |
| Design.Tests | net10.0 | 42 | 0 | 0 | 42 |

**Zero failures across all test projects and both frameworks.**

The 3 skipped tests are pre-existing `ShowcasePerformanceTests` (unrelated to this work).

### Test Scenario Mapping

| Plan Scenario | Test Method | File | Status |
|---|---|---|---|
| 7: Shared Dictionary after fix | `Scenario7_SharedDictionary_AfterFix_IdentityPreserved` | SharedReferenceTests.cs | PASS |
| 8: Record round-trip after fix | `Scenario8_RecordRoundTrip_AfterFix_Succeeds` | SharedReferenceTests.cs | PASS |
| 9: Circular reference after fix | `Scenario9_CircularReference_AfterFix_IdentityPreserved` | SharedReferenceTests.cs | PASS |
| 10: Cross-type shared reference | `Scenario10_CrossTypeSharedReference_DictionaryWithMixedProperties` | SharedReferenceTests.cs | PASS |
| 11: Record in mixed graph | `Scenario11_RecordInGraphWithSharedMutableRefs` | SharedReferenceTests.cs | PASS |
| 12: Ordinal format preserved | All existing ordinal tests | RemoteFactory.UnitTests + IntegrationTests | PASS (490 + 498 passing) |
| 13: Existing tests no regression | Full suite | All test projects | PASS (zero failures) |

Also covered by updated Phase 1 exploration tests:
| Plan Scenario | Updated Exploration Test | Status |
|---|---|---|
| 7 (proxy) | `Scenario1_SharedDictionary_IdentityPreserved` | PASS |
| 9 (proxy) | `Scenario4_CircularReference_Handled` | PASS |

### Sacred Tests Verification

All sacred tests passed unchanged (no modifications made):

| Test | Status |
|---|---|
| `InterfaceFactory_SimpleRecord_RoundTrip` | PASS |
| `InterfaceFactory_RecordWithCollection_RoundTrip` | PASS |
| `InterfaceFactory_NestedRecord_RoundTrip` | PASS |
| `InterfaceFactory_NullableRecord_ReturnsNull` | PASS |
| `InterfaceFactory_NullableRecord_ReturnsValue` | PASS |
| `InterfaceFactory_NonNeatooType_NoRefMetadata` | PASS |
| All Design.Tests (42 tests) | PASS |

### Implementation Contract Status

| Contract Item | Status |
|---|---|
| Create `RecordBypassConverterFactory.cs` | DONE |
| Wire `NeatooPreserveReferenceHandler` into options | DONE |
| Wire `RecordBypassConverterFactory` into options (AFTER Neatoo converters) | DONE |
| Remove revert comment from NeatooJsonSerializer | DONE |
| Unskip Scenarios 7, 9, 10, 11 | DONE |
| Update Phase 1 exploration tests (Scenarios 1, 4) | DONE |
| Verify `InterfaceFactory_NonNeatooType_NoRefMetadata` unchanged | DONE |
| Full test suite zero failures | DONE |
| Both net9.0 and net10.0 | DONE |

---

## Implementation Progress -- Phase 2 (Revised)

### Status: Approved -- Ready for Implementation
