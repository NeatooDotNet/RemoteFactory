# Requirements Reviewer Memory — Shared Reference Handling Plan

## Post-Implementation Verification (Step 7B)

**Date:** 2026-03-21
**Verdict:** REQUIREMENTS SATISFIED (with documentation gap noted)

---

### Compliance Table

| # | Requirement | Source | Status | Evidence |
|---|------------|--------|--------|----------|
| 1 | Published docs promise shared-instance identity for all types | `docs/serialization.md:10`, `docs/appendix/serialization.md:53-55` | SATISFIED | `NeatooPreserveReferenceHandler` wired into `NeatooJsonSerializer` options (`NeatooJsonSerializer.cs:71`). STJ built-in converters now emit `$id`/`$ref` for mutable reference types (Dictionary, List, plain classes). Scenario 7 test (`SharedReferenceTests.cs:31-67`) verifies shared Dictionary identity. Scenario 9 test (`SharedReferenceTests.cs:128-151`) verifies circular references. |
| 2 | Client-server architecture promises "single process" abstraction | `docs/client-server-architecture.md:3` | SATISFIED | Mutable reference types now preserve identity across serialization, closing the gap. Records (value objects) are excluded by design -- DDD semantics justify this. |
| 3 | v0.22.0 "converter-level, not serializer-level" principle | `docs/serialization.md:120-124`, `docs/release-notes/v0.22.0.md:16-17,24` | SATISFIED (with architectural evolution) | The implementation does set `options.ReferenceHandler`, partially reversing v0.22.0. However, this is deliberate and justified: Neatoo converters still access `NeatooReferenceResolver.Current` directly (converter-level), while STJ built-in converters use `options.ReferenceHandler` (serializer-level). Both paths share the same resolver instance. `RecordBypassConverterFactory` extends the converter-level principle to records by claiming them via a custom converter that delegates to inner options without `ReferenceHandler`. The three-path architecture (Neatoo converters, bypass converter, STJ built-in) coexists correctly. |
| 4 | Anti-Pattern 9 explanation references converter-level mechanism | `src/Design/CLAUDE-DESIGN.md:378-419` | STALE DOC (not a violation) | Anti-Pattern 9's "Why it matters" text at line 419 says "RemoteFactory's `JsonSerializerOptions` has no `ReferenceHandler` set." This is now factually incorrect -- options DO have `NeatooPreserveReferenceHandler`. However, the user-facing rule (do not mix Neatoo types with records) remains valid because the underlying STJ limitation is unchanged. The plan explicitly marks this as a Step 9 documentation deliverable. See Documentation Gap below. |
| 5 | `InterfaceFactory_NonNeatooType_NoRefMetadata` test guards against record corruption | `InterfaceFactoryRecordSerializationTests.cs:121-140` | SATISFIED | Test passes unchanged per developer evidence (zero failures). The `RecordBypassConverterFactory` claims the record type (`InterfaceRecordWithCollection` has parameterized constructor, no parameterless constructor), delegates to inner options without `ReferenceHandler`, so no `$id`/`$ref` metadata appears. The test's `Assert.DoesNotContain("$id", json)` and `Assert.DoesNotContain("$ref", json)` continue to pass. Test intent (records are not corrupted) is preserved. |
| 6 | STJ parameterized-constructor limitation | Microsoft docs, dotnet/runtime#73302 | SATISFIED | `RecordBypassConverterFactory.CanConvert` (`RecordBypassConverterFactory.cs:36-58`) correctly detects types with parameterized constructors (no public parameterless constructor + at least one public constructor with parameters). These types are serialized via inner options with `ReferenceHandler = null`, preventing STJ's `NotSupportedException`. Phase 1 Scenario 3 test (`SharedReferenceExplorationTests.cs:130-169`) confirms the STJ limitation exists. Phase 2 Scenario 8 test (`SharedReferenceTests.cs:83-113`) confirms records work after the fix. |
| 7 | Design project serialization tests | `src/Design/Design.Tests/FactoryTests/SerializationTests.cs` | SATISFIED | Developer reports 42 design tests pass per framework. The seven serialization tests in `SerializationTests.cs` do not test shared object identity (confirmed by review of the file). They test Create, Fetch, ValueObject, Collection, Nullable, Modified, and SaveMeta round-trips. These are unaffected because: (a) in Ordinal format, the ordinal converter claims `[Factory]` types before either the bypass converter or `ReferenceHandler` intervene; (b) in Named format, the bypass converter claims records without reference metadata; (c) mutable types get `$id`/`$ref` which is additive, not breaking. |
| 8 | Design Debt table has no entry for this feature | `src/Design/CLAUDE-DESIGN.md:732-738` | SATISFIED | Verified the Design Debt table. It contains entries for: private setter support, OR logic for AspAuthorize, automatic Remote detection, collection factory injection, and IEnumerable serialization. None relate to shared reference handling for non-custom types. No design debt boundary was crossed. |

---

### Implementation Details Verified

**Component 1: NeatooPreserveReferenceHandler** (`src/RemoteFactory/Internal/NeatooPreserveReferenceHandler.cs`)
- Correctly extends `ReferenceHandler` and delegates `CreateResolver()` to `NeatooReferenceResolver.Current`
- Throws `InvalidOperationException` with clear message if `Current` is null
- Internal sealed class -- appropriate visibility

**Component 2: RecordBypassConverterFactory** (`src/RemoteFactory/Internal/RecordBypassConverterFactory.cs`)
- `CanConvert` detection rule: no public parameterless constructor AND at least one public constructor with parameters -- matches plan's specification and STJ's own heuristic
- Inner options constructed via copy constructor with `ReferenceHandler = null` and self removed from converters list (prevents recursion)
- Double-checked locking for thread-safe inner options caching
- `RecordBypassConverter<T>` handles null correctly in both Read and Write

**NeatooJsonSerializer wiring** (`src/RemoteFactory/Internal/NeatooJsonSerializer.cs:68-94`)
- `ReferenceHandler = new NeatooPreserveReferenceHandler()` set on options (line 71)
- Converter ordering: ordinal converter first (if ordinal format), then Neatoo converters, then `RecordBypassConverterFactory` LAST
- NOTE: The plan's Design section originally said bypass converter should be "BEFORE other converters" but the implementation places it AFTER, with a comment explaining: "Neatoo converters get first priority -- they claim interfaces, abstract types, and IOrdinalSerializable types that have purpose-built converters." This is correct behavior: Neatoo types must be claimed by their own converters first; the bypass converter only picks up remaining types with parameterized constructors. The code comment at line 89-92 documents the rationale.

**Resolver lifecycle** -- Unchanged from v0.22.0. Each Serialize/Deserialize method creates a `NeatooReferenceResolver`, sets `Current`, calls STJ, and clears `Current` in a finally block. The `NeatooPreserveReferenceHandler.CreateResolver()` returns this same resolver when STJ's built-in converters need it.

---

### Unintended Side Effects Check

1. **Generated code patterns in Design projects** -- Not affected. No changes to generator output. The bypass converter operates at runtime serialization, not at code generation.

2. **Serialization contracts** -- Changed as intended. Mutable reference types now get `$id`/`$ref` metadata in their JSON. This is additive. Records do NOT get `$id`/`$ref`, preserving existing behavior.

3. **Design project tests** -- All 42 pass per framework (developer evidence). Verified by tracing: the seven `SerializationTests.cs` tests exercise Create/Fetch/ValueObject/Collection/Nullable/Modified/SaveMeta round-trips, which are unaffected by the reference handler addition.

4. **Published docs accuracy** -- See Documentation Gap below.

5. **Reflection usage** -- `RecordBypassConverterFactory` uses `GetConstructors`, `GetParameters`, `MakeGenericType`, and `Activator.CreateInstance`. This follows the established pattern used by `NeatooInterfaceJsonConverterFactory` (line 37) and `NeatooOrdinalConverterFactory` (lines 109-110). In the `JsonConverterFactory` context, reflection is unavoidable and consistent with existing code.

6. **Multi-targeting** -- `JsonSerializerOptions` copy constructor and `ReferenceHandler` API are available in both net9.0 and net10.0. Developer reports zero failures across both frameworks (490 unit + 498 integration tests per framework).

---

### Documentation Gap (Step 9 Deliverable -- NOT a Violation)

The plan explicitly identifies documentation updates as Step 9 deliverables. The following docs are now stale but are scheduled to be updated:

1. **`docs/serialization.md:120-124`** -- "Scope: Converter-Level, Not Serializer-Level" section says "RemoteFactory's `JsonSerializerOptions` has no `ReferenceHandler` set." This is now factually incorrect. Options have `NeatooPreserveReferenceHandler`.

2. **`src/Design/CLAUDE-DESIGN.md:419`** -- Anti-Pattern 9 "Why it matters" says the same. Now stale.

3. **`docs/appendix/record-reference-handling.md`** -- Does not exist yet. Plan specifies this as a new doc explaining DDD rationale for bypass converter.

4. **`src/Design/Design.Tests/FactoryTests/SerializationTests.cs:38`** -- Comment says "Circular references without proper handling" are in the "NO" category. This is now potentially misleading since mutable types DO handle circular references via `ReferenceHandler`. However, the qualification "without proper handling" is still technically accurate.

These are not requirements violations because the plan defers documentation to a dedicated step after implementation verification.
