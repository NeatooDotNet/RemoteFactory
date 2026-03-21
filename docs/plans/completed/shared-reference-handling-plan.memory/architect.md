# Architect -- Shared Reference Handling Plan

Last updated: 2026-03-21
Current step: Phase 2 design revised and finalized. Plan updated to "Under Review (Developer)" for developer review (Step 5).

## Key Context

### The Decided Approach: Simple Bypass Converter with DDD Justification

After Phase 1 confirmed that STJ's parameterized-constructor limitation is permanent and comprehensive, and after extensive discussion of viable approaches (custom converter factory, two-pass serialization, reduced scope, options stripping), the user decided on the simplest viable path:

**Two-component architecture:**

1. **`NeatooPreserveReferenceHandler`** (already built in Phase 1) -- wired into `NeatooJsonSerializer`'s `JsonSerializerOptions`. Gives STJ's built-in converters `$id`/`$ref` for mutable reference types.

2. **`RecordBypassConverterFactory`** (new, ~50-80 lines) -- claims ALL types with parameterized constructors. Delegates to cached inner `JsonSerializerOptions` identical to outer except `ReferenceHandler = null` and no `RecordBypassConverterFactory`. Records and their entire subtree serialize without `$id`/`$ref`.

### DDD Justification (Core Design Decision)

Records are value objects. Value objects are defined by their values, not their identity. This is not a compromise or limitation -- it is the correct semantic model:

- **Reference tracking is semantically wrong for records.** Tracking identity implies the object's reference matters. For value objects, it does not.
- **Nested reference types within records are part of the value object's state.** A Dictionary inside a record is part of the value object's definition. Duplicating it on round-trip is correct DDD behavior.
- **The entity/value object boundary is the serialization boundary.** Entities (mutable, identity-tracked via `$id`/`$ref`) and value objects (immutable, duplicated) serialize differently because they ARE different.

The user explicitly wants this documented in an appendix-style document (`docs/appendix/record-reference-handling.md`) to prevent future sessions from re-flagging this behavior as a bug.

### Detection Rule

Claim ALL types with parameterized constructors (simplest, safest). The user explicitly accepted this:
- Non-record classes with parameterized constructors are rare in the RemoteFactory ecosystem
- Even if such classes exist, not having reference tracking on them is low-risk
- The simpler rule avoids fragile heuristics that attempt to distinguish `record class` from `class` (STJ cannot distinguish them)

### v0.22.0 Principle Preserved

This does NOT revert v0.22.0. Three paths coexist:
- **Neatoo converters:** Access `NeatooReferenceResolver.Current` directly (unchanged from v0.22.0)
- **Bypass converter:** Serializes records without reference handling (new, extends converter-level principle)
- **STJ built-in:** Uses `ReferenceHandler` on options for mutable types only (new, fills the gap)

All three paths share the same `NeatooReferenceResolver` instance when applicable.

### Phase 1 Findings Summary (for relaying to Phase 2 developer)

1. `NeatooPreserveReferenceHandler` is built and validated -- works correctly for mutable types
2. STJ's parameterized-constructor limitation is permanent (dotnet/runtime#73302 closed NOT_PLANNED)
3. The limitation extends to ANY `$id`/`$ref` metadata on reference-type constructor parameters, not just `$ref` on the record itself
4. `ReferenceResolver.GetReference()` cannot suppress `$id` emission -- there is no per-type opt-out at the resolver level
5. The ONLY mechanism in STJ that bypasses built-in reference handling is custom converters (`JsonConverterFactory.CanConvert()` returning `true`)
6. The `JsonSerializerOptions` copy constructor (available in .NET 9+) makes creating inner options straightforward

### InterfaceFactory_NonNeatooType_NoRefMetadata Test

This test passes UNCHANGED with the bypass converter design. The bypass converter claims the record before `ReferenceHandler` can add metadata. The test's original intent (records are not corrupted by reference metadata) is preserved by design, not by coincidence.

## Mistakes to Avoid

- The original plan's "optimistic path" (hoping STJ would skip records as immutable) was WRONG. STJ adds `$id` to ALL reference types when `ReferenceHandler` is set, regardless of constructor type.
- The "filtering resolver" contingency also fails because `ReferenceResolver.GetReference()` cannot suppress `$id` emission.
- Do NOT attempt per-type `ReferenceHandler` control -- STJ does not support it. The only path is custom converters.
- Do NOT try to use `JsonTypeInfo.CreateObject` to change STJ's converter selection -- it does not work (converter is baked in at `JsonTypeInfo` creation time).

## User Corrections

- User reframe (critical): "Stop thinking about this as Neatoo vs non-Neatoo types. The issue is about standard .NET records. Records are value objects by definition -- identity doesn't matter, duplication is fine. The right model is: ReferenceHandler for mutable reference types (Dictionary, List, plain classes), skip it for records."
- User on detection rule: Claim ALL types with parameterized constructors. Simplest, safest. Non-record classes with parameterized constructors are rare.
- User on nested reference types: Acceptable. DDD justification resolves the concern.
- User on documentation: "Document this in its own appendix style/level of documentation." New file: `docs/appendix/record-reference-handling.md`.
- A1: "RemoteFactory is billing itself as abstracting away the client/server physical layer. We should try as hard as we can to do that."
- A2: Cross-entity sharing scope confirmed ("Yes, at least start with that scope").
- A3: RemoteFactory owns this, not Neatoo.
- A5: Both shared identity AND circular references.

## Post-Implementation Verification (Step 8A)

**Date:** 2026-03-21
**Verdict:** VERIFIED

### Build Verification

- **Solution build (Release):** PASSED -- zero errors, 3 pre-existing WASM warnings (unrelated SQLite interop)
- **All projects compiled successfully** including Generator (netstandard2.0), RemoteFactory (net9.0/net10.0), AspNetCore, all test projects, Design project, Examples

### Test Results -- All Projects, Both Frameworks

| Project | Framework | Total | Passed | Failed | Skipped | Result |
|---------|-----------|-------|--------|--------|---------|--------|
| RemoteFactory.UnitTests | net9.0 | 490 | 490 | 0 | 0 | PASS |
| RemoteFactory.UnitTests | net10.0 | 490 | 490 | 0 | 0 | PASS |
| RemoteFactory.IntegrationTests | net9.0 | 501 | 498 | 0 | 3 | PASS |
| RemoteFactory.IntegrationTests | net10.0 | 501 | 498 | 0 | 3 | PASS |
| Design.Tests | net9.0+net10.0 | 42 | 42 | 0 | 0 | PASS |

**Zero failures across all projects and both frameworks.**

The 3 skipped integration tests are `ShowcasePerformanceTests` (pre-existing, completely unrelated to this work).

### Test Scenario Cross-Check

For EACH numbered test scenario in the plan's Test Scenarios table, I verified a corresponding test method exists and passes:

| Scenario | Rule(s) | Test Method | Test File | Passes? |
|----------|---------|-------------|-----------|---------|
| 1 | Rule 1->7 | `Scenario1_SharedDictionary_IdentityPreserved` | SharedReferenceExplorationTests.cs | YES (both TFMs) |
| 2 | Rule 2 | `Scenario2_RecordRoundTrip_CurrentBehavior_Succeeds` | SharedReferenceExplorationTests.cs | YES (both TFMs) |
| 3 | Rule 3 | `Scenario3_RecordWithReferenceHandlerPreserve_ThrowsOnDeserialization` | SharedReferenceExplorationTests.cs | YES (both TFMs) |
| 4 | Rule 4->9 | `Scenario4_CircularReference_Handled` | SharedReferenceExplorationTests.cs | YES (both TFMs) |
| 5 | Rule 5 | `Scenario5_SharedDictionary_ReferenceHandlerPreserve_IdentityPreserved` | SharedReferenceExplorationTests.cs | YES (both TFMs) |
| 6 | Rule 6 | `Scenario6_CustomReferenceHandler_NeatooReferenceResolver_IdentityPreserved` | SharedReferenceExplorationTests.cs | YES (both TFMs) |
| 7 | Rule 7 | `Scenario7_SharedDictionary_AfterFix_IdentityPreserved` | SharedReferenceTests.cs | YES (both TFMs) |
| 8 | Rule 8 | `Scenario8_RecordRoundTrip_AfterFix_Succeeds` | SharedReferenceTests.cs | YES (both TFMs) |
| 9 | Rule 9 | `Scenario9_CircularReference_AfterFix_IdentityPreserved` | SharedReferenceTests.cs | YES (both TFMs) |
| 10 | Rule 10 | `Scenario10_CrossTypeSharedReference_DictionaryWithMixedProperties` | SharedReferenceTests.cs | YES (both TFMs) |
| 11 | Rule 11 | `Scenario11_RecordInGraphWithSharedMutableRefs` | SharedReferenceTests.cs | YES (both TFMs) |
| 12 | Rule 12 | Existing ordinal tests (no dedicated test) | OrdinalSerializationTests.cs, OrdinalMetadataTests.cs | YES -- all ordinal tests pass unchanged |
| 13 | Rule 13 | `InterfaceFactory_NonNeatooType_NoRefMetadata` + full suite | InterfaceFactoryRecordSerializationTests.cs | YES -- test passes UNCHANGED (confirmed via git diff) |

**Coverage: 13 of 13 test scenarios verified with passing tests.**

### Critical Test Verification

`InterfaceFactory_NonNeatooType_NoRefMetadata` passes UNCHANGED:
- **git diff confirms:** zero modifications to `InterfaceFactoryRecordSerializationTests.cs`
- **Test assertions unchanged:** `Assert.DoesNotContain("$id", json)` and `Assert.DoesNotContain("$ref", json)`
- **Passes on both net9.0 and net10.0**
- **Mechanism:** `RecordBypassConverterFactory` claims the record type (parameterized constructor) before `ReferenceHandler` can add metadata, so records still produce clean JSON

### Implementation Design Match

**New files created (matching plan's File Changes table):**
- `src/RemoteFactory/Internal/RecordBypassConverterFactory.cs` -- 134 lines, includes both `RecordBypassConverterFactory` and `RecordBypassConverter<T>`
- `src/RemoteFactory/Internal/NeatooPreserveReferenceHandler.cs` -- 41 lines (from Phase 1, unchanged)
- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/SharedReferenceTests.cs` -- Phase 2 acceptance tests (Scenarios 7-11)
- `src/Tests/RemoteFactory.IntegrationTests/TestTargets/TypeSerialization/SharedReferenceTargets.cs` -- target types

**Modified files:**
- `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` -- Added `ReferenceHandler = new NeatooPreserveReferenceHandler()` on options; added `new RecordBypassConverterFactory()` to converters list

**Unchanged (confirmed via git diff):**
- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs`

### Design Conformance

1. **`RecordBypassConverterFactory` detection rule:** `!hasParameterlessCtor && hasParameterizedCtor` -- matches plan specification exactly ("type has no public parameterless constructor AND has at least one public constructor with parameters")
2. **Inner options construction:** Uses `new JsonSerializerOptions(outerOptions)` copy constructor, sets `ReferenceHandler = null`, removes `RecordBypassConverterFactory` from converters to prevent recursion -- matches plan design
3. **Caching:** Double-checked locking with `_innerOptions` field -- correct thread-safe lazy initialization
4. **Null handling:** `RecordBypassConverter<T>` handles null values in both Read and Write -- good defensive coding not in the plan but correct

### Deliberate Deviation from Plan (Converter Ordering)

The plan specified `RecordBypassConverterFactory` BEFORE Neatoo converters (plan lines 257, 356). The implementation places it AFTER Neatoo converters (per user decision for defense-in-depth).

**This deviation is functionally correct** because:
- Neatoo converters claim their types first (interfaces, abstract types, IOrdinalSerializable)
- `RecordBypassConverterFactory.CanConvert()` does not explicitly check for Neatoo types -- it relies on ordering
- When AFTER Neatoo converters, Neatoo types are already claimed before the bypass factory is consulted
- The result is equivalent: Neatoo types use Neatoo converters, records use the bypass converter, mutable types use STJ built-in with ReferenceHandler
- All tests pass, confirming the ordering works correctly

### Exploration Tests (Phase 1) Updated

Scenarios 1 and 4 were updated from documenting broken behavior to asserting the fix works:
- Scenario 1: Changed from `Assert.False(ReferenceEquals(...))` (identity lost) to `Assert.True(ReferenceEquals(...))` (identity preserved)
- Scenario 4: Changed from `Assert.Throws<JsonException>` (max depth) to asserting circular reference round-trip works

These updates are correct -- the exploration tests now document the fixed behavior rather than the broken state. The original assertions were Phase 1 baselines that existed only to prove the problem; updating them to reflect the fix is appropriate.

### Summary

All acceptance criteria from the plan's Phase 2 section are satisfied:
- RecordBypassConverterFactory created (134 lines)
- Both components wired into NeatooJsonSerializer's JsonSerializerOptions
- Shared Dictionary identity preserved (Scenario 7)
- Records with parameterized constructors deserialize without error (Scenario 8)
- Records produce JSON without $id/$ref metadata (verified by unchanged InterfaceFactory_NonNeatooType_NoRefMetadata test)
- Circular references handled (Scenario 9)
- Cross-type shared references work (Scenario 10)
- Record with nested mutable type coexists correctly (Scenario 11)
- Ordinal format unaffected (all ordinal tests pass)
- All existing tests pass with zero failures
- InterfaceFactory_NonNeatooType_NoRefMetadata passes UNCHANGED
