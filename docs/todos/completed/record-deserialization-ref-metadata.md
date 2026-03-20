# Record Deserialization Fails with $ref Metadata

**Status:** Complete
**Priority:** High
**Created:** 2026-03-20
**Last Updated:** 2026-03-20


---

## Problem

`NeatooJsonSerializer` uses `ReferenceHandler.Preserve`, which adds `$id`/`$ref` metadata to JSON. System.Text.Json has a known limitation: it cannot deserialize types with parameterized constructors (like C# record types with primary constructors) when `$ref` metadata appears in the payload.

**Error:** `ObjectWithParameterizedCtorRefMetadataNotSupported`

This surfaces when an Interface Factory method returns a record type — the serializer adds reference metadata, and client-side deserialization fails.

**Minimal repro structure:**

```csharp
// A record with a primary constructor — the problem type
public record MyResult(
    string Name,
    IReadOnlyList<MyItem> Items);

public record MyItem(int Id, string Value);

// Interface Factory that returns the record
[Factory]
public interface IMyService
{
    Task<MyResult?> GetDataAsync();
}
```

When `GetDataAsync` returns through RemoteFactory, the serializer adds `$id`/`$ref` metadata. On deserialization, System.Text.Json encounters `$ref` metadata on `Items` — it can't resolve the reference AND populate the constructor parameter at the same time, so it throws.

**Root cause:** `NeatooJsonSerializer` applies `ReferenceHandler.Preserve` globally via `NeatooReferenceHandler`. Neatoo types (`IValidateBase`, `IValidateListBase`) handle `$id`/`$ref` manually in their custom converters. Plain records/DTOs fail the `CanConvert` check in `NeatooBaseJsonConverterFactory` and fall back to STJ built-in handling, which has the parameterized constructor limitation.

## Solution

To be determined by architect. Possible directions:
- Modify NeatooJsonSerializer to handle non-Neatoo types (records/DTOs) differently — e.g., skip `ReferenceHandler.Preserve` for types that don't need reference tracking
- Strip `$id`/`$ref` metadata for non-Neatoo types during deserialization
- Document as a known limitation with guidance to avoid records as Interface Factory return types

---

## Clarifications

**Q1 (Architect):** Is this a user-reported issue, or a proactive discovery? Do you have a specific repro or error trace?

**A1:** User-reported. They had to work around it by switching from records to classes.

**Q2 (Architect):** What types need reference preservation? Only Neatoo domain types, or could users have circular references in their DTOs/records?

**A2:** OK with not handling circular references for DTOs/records. Draw the line at Neatoo types only. However, the architect should assess difficulty of supporting circular refs in DTOs — if trivial, worth considering; if not, skip.

**Q3 (Architect):** Can Interface Factory return types mix records with Neatoo domain types (e.g., a record containing an `IValidateBase` property)?

**A3:** No. Label that an anti-pattern. They are either using Neatoo incorrectly (not DDD, not an aggregate) or can just use RemoteFactory directly.

**Q4 (Architect):** Would it be acceptable to change the JSON wire format for non-Neatoo types (removing `$id`/`$ref`)?

**A4:** Fully breaking change tolerant. Still v0.

**Q5 (Architect):** Does ordinal format also have this problem?

**A5:** Not applicable. Records/DTOs don't implement `IOrdinalSerializable`, so they never use ordinal format. The problem is only in the default STJ fallback path.

Architect confirmed **Ready** after these clarifications.

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-20
**Verdict:** APPROVED

### Relevant Requirements Found

1. **Serialization Architecture (NeatooJsonSerializer):** The serializer applies `ReferenceHandler.Preserve` globally via `NeatooReferenceHandler` (`src/RemoteFactory/Internal/NeatooJsonSerializer.cs:70`). This is the root cause -- `$id`/`$ref` metadata is emitted for ALL types, including non-Neatoo records/DTOs.

2. **Custom Converter Chain:** Three converter factories are registered in order: `NeatooOrdinalConverterFactory` (for `IOrdinalSerializable` types), then the generated `NeatooJsonConverterFactory` subclasses (including `NeatooInterfaceJsonConverterFactory` for interface-typed properties). Types that do not match any custom converter fall through to STJ built-in handling, which is where records with parameterized constructors encounter the `$ref` incompatibility.

3. **NeatooJsonTypeInfoResolver (`src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs`):** Overrides `CreateObject` for DI-registered types. This is relevant because records with primary constructors use STJ's constructor-based deserialization, which conflicts with `ReferenceHandler.Preserve` at the STJ engine level -- the resolver cannot bypass that limitation.

4. **Interface Factory Return Types:** The Design source of truth (`src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:204-220`) shows Interface Factory methods returning plain DTOs (`ExampleDto`, a class with public setters) and `IReadOnlyList<ExampleDto>`. The `ExampleDto` class is NOT a Neatoo type -- it has no `[Factory]` attribute, no `IOrdinalSerializable`, and no custom converter. This establishes the pattern that Interface Factory return types can be arbitrary non-Neatoo types.

5. **Record Support (Completed Todo: `docs/todos/completed/record-support-plan.md`):** v10.1.0 added record support with `[Factory]` on records. Records with `[Factory]` get `IOrdinalSerializable` generated code and custom converters. This todo's problem is specifically about records WITHOUT `[Factory]` (plain DTOs returned from Interface Factories), which fall through to STJ built-in handling.

6. **Serialization Round-Trip Guide (`src/Design/Design.Tests/FactoryTests/SerializationTests.cs:22-43`):** Documents which types serialize correctly. Records are listed under "YES" (line 27). However, this refers to `[Factory]`-decorated records (like `Money`). Plain record DTOs returned from Interface Factories are not explicitly addressed.

7. **Reference Preservation Documented Behavior (`docs/serialization.md:89-118`):** Published docs state that reference preservation handles "shared instance identity" and "circular references." The docs describe this as handling Neatoo domain object graphs (parent-child bidirectional references), not arbitrary user DTOs.

8. **HandleRemoteDelegateRequest Response Serialization (`src/RemoteFactory/HandleRemoteDelegateRequest.cs:126-141`):** The server serializes the response using `serializer.Serialize(result, returnType)`, where `returnType` is extracted from the delegate's return type. This passes through `NeatooJsonSerializer.Serialize`, which always sets up the `NeatooReferenceHandler` -- meaning `$id`/`$ref` metadata is always emitted.

9. **Design Debt -- IEnumerable<T> serialization (`src/Design/CLAUDE-DESIGN.md:691`):** "Only concrete collections. Type preservation complexity. User demand for interface collections." This is tangentially related but not directly blocking -- the todo is about records in collections, not about interface-typed collections themselves.

10. **Breaking Change Tolerance (Clarification A4):** The user confirmed full breaking change tolerance since the project is still v0. This removes any wire-format backward-compatibility constraint.

### Gaps

1. **No documented requirement for non-Neatoo type serialization behavior:** The Design source of truth demonstrates Interface Factory methods returning `ExampleDto` (a class), but there is no test or documented contract for what happens when an Interface Factory returns a record with a parameterized constructor. The existing tests use `ExampleDto` which is a class with a default constructor and public setters -- this sidesteps the `$ref` issue entirely.

2. **No documented anti-pattern for mixing Neatoo types with records in return types:** Clarification A3 establishes a new anti-pattern: records containing `IValidateBase` properties should not be used as Interface Factory return types. This anti-pattern does not currently exist in `CLAUDE-DESIGN.md` or the Design projects. The architect should document it.

3. **No requirement covering circular reference handling scope:** The published docs (`docs/serialization.md`) describe reference preservation for Neatoo domain objects but do not state whether it applies to all types or only Neatoo types. The architect needs to establish the boundary: reference preservation for Neatoo types only (per Clarification A2), with no circular reference support for plain DTOs/records.

4. **No Design project example of Interface Factory returning a record:** `AllPatterns.cs` uses `ExampleDto` (a class) as the return type. After this fix, the Design projects should include an Interface Factory method returning a record type to demonstrate the supported pattern.

### Contradictions

None found. This todo does not contradict any documented pattern, anti-pattern, or design debt decision.

- The Design Debt table entries are not in conflict. The `IEnumerable<T> serialization` debt item is about interface-typed collections, not about record deserialization.
- No anti-pattern is violated. The proposed change fixes an unintended limitation rather than implementing a deliberately deferred feature.
- The todo's direction (making non-Neatoo types skip `ReferenceHandler.Preserve` behavior) is consistent with the documented architecture: Neatoo types have custom converters that handle `$id`/`$ref` manually, while non-Neatoo types should fall through to standard STJ behavior without the reference metadata that breaks parameterized constructors.

### Recommendations for Architect

1. **Scope the fix to the serializer layer only.** The converter chain is well-structured: Neatoo types (IOrdinalSerializable, interface-typed properties) have custom converters that handle `$id`/`$ref` manually. The fix should ensure non-Neatoo types are serialized without `ReferenceHandler.Preserve` interference, or that `$ref` metadata is stripped/skipped for types that cannot handle it (parameterized constructors).

2. **Preserve reference handling for Neatoo types.** The `NeatooInterfaceJsonTypeConverter` (`src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs:44-48`) explicitly manages `$id`/`$ref` for interface-typed properties. The ordinal converter writes arrays, bypassing reference metadata entirely. Any solution must not break these existing converters.

3. **Add a Design project example.** After implementing the fix, add an Interface Factory method that returns a record type to `AllPatterns.cs` (or a new file), and add a corresponding test to `Design.Tests/FactoryTests/InterfaceFactoryTests.cs` demonstrating the record return type pattern.

4. **Document the new anti-pattern (Clarification A3).** Add to `CLAUDE-DESIGN.md` Anti-Patterns section: records containing Neatoo domain types (`IValidateBase`, etc.) as properties should not be used as Interface Factory return types. They should either be full Neatoo entities or pure DTOs/records.

5. **Test with the two DI container pattern.** The fix must be validated using `ClientServerContainers.Scopes()` to ensure the full client-to-server-to-client round-trip works for record return types. Test both simple records and records containing collections (the exact scenario from the repro).

6. **Consider the HandleRemoteDelegateRequest response path.** The server-side response serialization (`HandleRemoteDelegateRequest.cs:141`) calls `serializer.Serialize(result, returnType)`. If the solution involves type-specific serialization options, this is the key integration point where the return type is known and the serialization strategy can be varied.

7. **Multi-targeting.** Verify the fix works on both net9.0 and net10.0. The STJ `$ref` limitation with parameterized constructors exists in both versions.

---

## Plans

- [Fix Record Deserialization with $ref Metadata](../plans/record-deserialization-ref-metadata.md)

---

## Tasks

- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3)
- [x] Architect plan creation & design (Step 4)
- [x] Developer review (Step 5) — Approved
- [x] Implementation (Step 7)
- [x] Verification (Step 8) — Architect VERIFIED, Requirements SATISFIED
- [x] Documentation (Step 9) — Requirements docs, Design project examples, skill updates, release notes (v0.21.3)

---

## Progress Log

### 2026-03-20
- Todo created (fresh start). Problem: NeatooJsonSerializer's global ReferenceHandler.Preserve causes STJ to fail deserializing records with parameterized constructors when $ref metadata is present.
- Architect plan created: dual-options approach. NeatooJsonSerializer maintains two JsonSerializerOptions instances -- one with ReferenceHandler for Neatoo types, one without for plain records/DTOs. Type classification via IsNeatooType(). Plan linked: `docs/plans/record-deserialization-ref-metadata.md`

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors (net9.0 + net10.0)
- Tests: UnitTests 490x2, IntegrationTests 487x2 (3 skipped pre-existing), Design.Tests 42x2 — 0 failures

---

## Results / Conclusions

Fixed by introducing dual `JsonSerializerOptions` in `NeatooJsonSerializer`. Neatoo types (`IOrdinalSerializable`, interface/abstract types in registered assemblies) continue using `ReferenceHandler.Preserve`. Non-Neatoo types (records, DTOs, primitives) use clean options without `$id`/`$ref` metadata, eliminating the `ObjectWithParameterizedCtorRefMetadataNotSupported` error for records with parameterized constructors. New anti-pattern documented: mixing Neatoo domain types with records in Interface Factory return types. Version bumped to v0.21.3.
