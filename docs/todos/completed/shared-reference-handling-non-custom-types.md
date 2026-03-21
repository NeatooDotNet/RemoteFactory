# Shared Reference Handling for Non-Custom Types

**Status:** Complete
**Priority:** High
**Created:** 2026-03-20
**Last Updated:** 2026-03-21

---

## Problem

RemoteFactory v0.22.0 removed `ReferenceHandler` from `JsonSerializerOptions` as part of the `NeatooReferenceResolver.Current` migration. This means STJ's built-in converters for non-Neatoo types (e.g., `Dictionary<string, string>`, `List<string>`, plain objects) no longer participate in `$id`/`$ref` reference tracking.

Neatoo's custom converters (`NeatooBaseJsonTypeConverter`, `NeatooListBaseJsonTypeConverter`) handle `$id`/`$ref` for Neatoo entities and lists directly via `NeatooReferenceResolver.Current`. But when a Neatoo entity has properties of plain .NET types that share the same object instance, STJ serializes them as two separate copies — shared identity is lost on round-trip.

It breaks things having ReferenceHandler and it breaks things not having it:

- **With `ReferenceHandler`:** STJ injects `$id`/`$ref` on ALL types, including records with parameterized constructors. STJ can't deserialize its own `$ref` metadata on those records — throws `ObjectWithParameterizedCtorRefMetadataNotSupported`.
- **Without `ReferenceHandler`:** Non-custom types lose shared-reference identity after deserialization.

## Design Question

RemoteFactory v0.22.0 provides `NeatooReferenceResolver` as a tool that Neatoo converters use explicitly. The question is whether `NeatooJsonSerializer` should also wire a `ReferenceHandler` into `JsonSerializerOptions` so that STJ's built-in converters participate in reference tracking too.

**Option A: Restore ReferenceHandler on options** — `NeatooJsonSerializer` sets `options.ReferenceHandler` to a handler that delegates `CreateResolver()` to `NeatooReferenceResolver.Current`. All types (Neatoo and non-Neatoo) participate in `$id`/`$ref`. Risk: could introduce unexpected `$id`/`$ref` tokens in output for types that don't need it, or cause errors with types that don't support reference preservation.

**Option B: Keep current design (tool, not always-on)** — Only Neatoo's custom converters do `$id`/`$ref`. Shared references for plain .NET types are not preserved. This is simpler and more predictable but loses shared-reference identity for non-Neatoo property types.

**Option C: Neatoo adds this feature** — Neatoo (the DDD framework) is the one that cares about reference identity. Neatoo could handle shared-reference tracking for non-Neatoo property values within its own converters, rather than pushing this into RemoteFactory.

## Context

Discovered during Neatoo's migration to RemoteFactory v0.22.0. The failing Neatoo test:

- **`FatClientValidate_Deserialize_SharedDictionaryReference`** — Assigns the same `Dictionary<string, string>` instance to two properties (`Data` and `Data2`) on a Neatoo entity. After serialize/deserialize round-trip, asserts `Assert.AreSame(newTarget.Data, newTarget.Data2)`. This fails because the dictionary is serialized twice without `$id`/`$ref`.

## Related Work

- Completed: [Serializer Responsibility Redesign](completed/serializer-responsibility-redesign.md) — v0.22.0 redesign that removed `ReferenceHandler` from options
- Completed (Neatoo): `Neatoo/docs/todos/completed/remotefactory-serializer-migration.md` — Neatoo converter migration to v0.22.0, [Ignore]d the `SharedDictionaryReference` test
- Completed: [Record Deserialization Ref Metadata](completed/record-deserialization-ref-metadata.md) — the original v0.21.3 bug

## Key Files

- `src/RemoteFactory/Internal/NeatooReferenceResolver.cs` — the `AsyncLocal` resolver
- `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` — serializer that manages resolver lifecycle
- Neatoo: `src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientValidateTests.cs` — `FatClientValidate_Deserialize_SharedDictionaryReference` test (currently [Ignore]d)

## Tests to Analyze

These Neatoo tests exercise shared-reference behavior for non-Neatoo types and are affected by this decision:

| Test | File | Line | What It Tests |
|------|------|------|---------------|
| `FatClientValidate_Deserialize_SharedDictionaryReference` | `Neatoo/src/Neatoo.UnitTest/Integration/Concepts/Serialization/FatClientValidateTests.cs` | 209 | Same `Dictionary<string, string>` assigned to two properties — asserts `AreSame` after round-trip |

These Neatoo tests exercise shared-reference behavior for **Neatoo types** and are NOT affected (Neatoo converters handle `$id`/`$ref` directly):

| Test | File | Line | What It Tests |
|------|------|------|---------------|
| `FatClientEntity_Deserialize_Child_ParentRef` | `FatClientEntityTests.cs` | 86 | Child entity's Parent ref resolves to parent object after round-trip |
| `FatClientValidate_Deserialize_Child_ParentRef` | `FatClientValidateTests.cs` | 157 | ValidateBase child's Parent ref resolves after round-trip |
| `FatClientEntityList_Deserialize_Child_ParentRef` | `FatClientEntityListTests.cs` | 69 | Entity list child's Parent ref resolves after round-trip |

---

## Clarifications

**Q1 (Architect):** How common is shared non-Neatoo instances in practice? Beyond the test case, do real domain models typically share the same Dictionary/List/plain-object instance across multiple properties?

**A1:** Edge case, but when it happens it's really hard to spot. RemoteFactory is billing itself as abstracting away the client/server physical layer. We should try as hard as we can to do that.

**Q2 (Architect):** Cross-entity sharing? Could `EntityA.Data` and `EntityB.Data` (parent-child) both point to the same dictionary? That would require reference tracking across the entire graph, not just within one entity.

**A2:** Yes, at least start with that scope.

**Q3 (Architect):** Is Option C (Neatoo owns it) your leaning? If so, should this todo result in a documented design decision with no RemoteFactory code change?

**A3:** Leaning is RemoteFactory owns it. That's why we're back here.

**Q4 (Architect):** If Neatoo owns it, what RemoteFactory support is expected?

**A4:** N/A — RemoteFactory owns it (see A3).

**Q5 (Architect):** Circular references in non-Neatoo types — shared identity only, or also circular references (infinite serialization loops)?

**A5:** Both. Can we re-create the issues with records and with dictionaries and duplicate refs and see if we can get all working? First, I want to see if the main difficulty was our own creation of having Neatoo and RemoteFactory sharing responsibilities.

Architect confirmed **Ready**.

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-20
**Verdict:** APPROVED (with critical tension to resolve)

### Relevant Requirements Found

1. **Published docs explicitly promise shared-instance identity for all types (`docs/serialization.md:10`).** The serialization overview lists three things RemoteFactory handles beyond STJ. The second bullet reads: "Shared instance identity -- When the same object is referenced by two properties (e.g., a parent-child bidirectional reference), System.Text.Json duplicates it. RemoteFactory tracks object identity and serializes shared references as `$ref` pointers, preserving the graph structure." This claim is not qualified as "Neatoo types only." The todo's goal of extending reference handling to non-custom types would bring the implementation in line with this published promise.

2. **Serialization appendix reinforces the general promise (`docs/appendix/serialization.md:53-55`).** Section "3. Shared Object Identity Is Lost" states: "When two properties reference the same object instance (common in aggregate root / child entity relationships), STJ duplicates it -- creating two independent copies. RemoteFactory preserves identity using `$id` / `$ref` pointers, maintaining the object graph structure across the wire." Again, no qualification that this applies only to Neatoo types.

3. **Client-server architecture doc establishes the "single process" abstraction goal (`docs/client-server-architecture.md:3`).** "RemoteFactory lets you write your domain model as if it runs in a single process." The user's clarification A1 reinforces this: "RemoteFactory is billing itself as abstracting away the client/server physical layer. We should try as hard as we can to do that." Losing shared-identity for non-custom types violates this abstraction -- in a single process, two properties pointing at the same Dictionary are the same object; after round-trip, they become two copies.

4. **v0.22.0 explicitly documents "Scope: Converter-Level, Not Serializer-Level" (`docs/serialization.md:120-124`).** This section states: "Plain records and DTOs have no custom converter, so they are serialized by System.Text.Json without reference metadata. This means plain records/DTOs do not support circular references, but they do support parameterized constructors (primary constructors) without issue." This is the documented current-state limitation. The todo proposes changing this behavior.

5. **Anti-Pattern 9 ("Mixing Neatoo types with records") rationale (`src/Design/CLAUDE-DESIGN.md:378-419`).** The "Why it matters" explanation references the converter-level mechanism: "RemoteFactory's `JsonSerializerOptions` has no `ReferenceHandler` set." If this todo restores `ReferenceHandler` on options (Option A), Anti-Pattern 9's technical explanation would need to change. The user-facing rule (do not mix Neatoo types with records) may or may not change depending on implementation approach.

6. **Existing test explicitly asserts records have NO `$id`/`$ref` (`src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs:121-140`).** The test `InterfaceFactory_NonNeatooType_NoRefMetadata` serializes a record and asserts `Assert.DoesNotContain("$id", json)` and `Assert.DoesNotContain("$ref", json)`. If the implementation adds `ReferenceHandler` to options, this test will fail. This is a critical tension point -- the test was written to verify the v0.21.3/v0.22.0 fix. The todo's scope would intentionally reverse this behavior.

7. **The STJ parameterized-constructor limitation remains (`docs/todos/completed/record-deserialization-ref-metadata.md:13-17`).** STJ cannot deserialize types with parameterized constructors when `$ref` metadata appears in the payload (`ObjectWithParameterizedCtorRefMetadataNotSupported`). This was the original v0.21.3 bug. If `ReferenceHandler` is restored globally (Option A), this bug would resurface for records. The architect must find a way to handle both: shared identity for non-custom types AND records with parameterized constructors.

8. **`NeatooReferenceResolver` already provides the infrastructure (`src/RemoteFactory/Internal/NeatooReferenceResolver.cs`).** The resolver is created per-operation in `NeatooJsonSerializer`, tracks reference identity via `ReferenceEqualityComparer.Instance`, and supports `GetReference`/`AddReference`/`ResolveReference` operations. It is public and accessible via `AsyncLocal`. The mechanism exists; the question is how to wire non-custom types into it.

9. **`NeatooInterfaceJsonTypeConverter` no longer handles `$id`/`$ref` (`src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs`).** The v0.22.0 release notes confirm dead reference handler code was removed from this converter's `Read()` method. The converter only handles `$type`/`$value` wrapping for interface-typed properties. It does not call `NeatooReferenceResolver` at all. If the todo needs interface-typed properties to participate in reference tracking, this converter will need changes.

10. **SerializationTests in Design project test round-trip but not shared identity (`src/Design/Design.Tests/FactoryTests/SerializationTests.cs`).** The seven serialization tests verify property values survive round-trip (Create, Fetch, ValueObject, Collection, Nullable, Modified, SaveMeta). None test shared object identity (same instance referenced from multiple properties). There is no Design project test that exercises the scenario described in this todo.

11. **"Circular references without proper handling" is listed as a NO in the round-trip guide (`src/Design/Design.Tests/FactoryTests/SerializationTests.cs:38`).** Line 38 of the test file's header comment lists "Circular references without proper handling" under "NO -- These will NOT serialize correctly." This documents the current limitation but does not prohibit fixing it.

12. **Design Debt table has no entry for shared-reference handling of non-custom types (`src/Design/CLAUDE-DESIGN.md:728-739`).** This feature was not deliberately deferred. It is not listed in the Design Debt table. No "Reconsider When" condition applies. The todo does not contradict a deliberate non-implementation.

13. **v0.22.0 release notes confirm no `ReferenceHandler` on options is the current design (`docs/release-notes/v0.22.0.md:16-17, 24`).** "Returns to a single `JsonSerializerOptions` instance with no `ReferenceHandler` set." And as a documented breaking change: "`options.ReferenceHandler` is no longer set on `JsonSerializerOptions`."

### Gaps

1. **No documented contract for what "shared instance identity" means for non-Neatoo types.** The published docs promise shared identity (Finding 1, 2) but the implementation only delivers it for Neatoo types with custom converters. There is no documented design decision about whether this gap is intentional or an oversight. The architect must establish: (a) Which non-custom types should participate (all? only reference types? only types without parameterized constructors?), (b) Whether cross-entity sharing is supported (EntityA.Data === EntityB.Data per clarification A2), (c) How this interacts with ordinal format.

2. **No documented approach for handling the record/parameterized-constructor conflict alongside `ReferenceHandler`.** The todo identifies the core tension (with `ReferenceHandler`: records break; without it: shared identity lost) but does not propose a resolution strategy. This is the central design challenge and needs architectural analysis. Possible approaches include: selective `ReferenceHandler` that skips types with parameterized constructors, a custom `ReferenceHandler` subclass, or per-type converter logic.

3. **No test infrastructure for shared-reference scenarios in RemoteFactory.** The only test is in the Neatoo repository (`FatClientValidate_Deserialize_SharedDictionaryReference`, currently [Ignore]d). RemoteFactory needs its own reproduction tests. The user explicitly asked for this in clarification A5: "Can we re-create the issues with records and with dictionaries and duplicate refs and see if we can get all working?"

4. **No documented requirement for circular reference handling in non-custom types.** The user requested both shared identity AND circular references for non-custom types (clarification A5). Circular references in non-custom types are a harder problem than shared identity -- they can cause infinite serialization loops without a mechanism to break the cycle. The completed `record-deserialization-ref-metadata.md` plan (`docs/plans/completed/record-deserialization-ref-metadata.md:144`) explicitly assessed circular reference support for plain records/DTOs as "NOT trivial."

5. **No analysis of ordinal format interaction.** The ordinal converter (`NeatooOrdinalConverter<T>`) serializes as JSON arrays. Reference handling metadata (`$id`/`$ref`) is typically emitted as object properties. The architect must clarify whether shared-reference tracking is needed for ordinal format, and if so, how it interacts with array serialization.

### Contradictions

No hard contradictions with the Design Debt table or documented anti-patterns. However, there is a significant **tension** with the v0.22.0 design decision:

1. **Tension with the "converter-level, not serializer-level" principle.** The v0.22.0 redesign (completed `serializer-responsibility-redesign.md`) established that reference handling is a converter-level concern. The serializer provides the resolver via `AsyncLocal`, but `JsonSerializerOptions` has no `ReferenceHandler`. Option A in this todo (restoring `ReferenceHandler` on options) would partially reverse this decision. This is not a contradiction per se -- the user has the authority to change direction -- but the architect should explicitly address why the v0.22.0 principle does not fully apply here, or propose an approach that extends the converter-level principle to non-custom types rather than reverting to a serializer-level mechanism.

2. **Test `InterfaceFactory_NonNeatooType_NoRefMetadata` would break under Option A.** This is an existing passing test that asserts the current behavior. Per the sacred tests rule, this test's intent must be understood before modifying it. The test's intent was to verify the v0.21.3 record fix -- ensuring records are not corrupted by unwanted `$id`/`$ref`. If the implementation adds `ReferenceHandler` globally, this test's assertion is no longer correct. The architect must determine: (a) Is the test's intent still valid under the new design? (b) If so, how is the record problem solved alongside reference tracking?

### Recommendations for Architect

1. **Start with reproduction tests as the user requested (clarification A5).** Before designing a solution, create RemoteFactory-internal tests that reproduce: (a) shared Dictionary assigned to two properties -- assert identity after round-trip, (b) record with parameterized constructor -- assert it deserializes without error, (c) circular reference in a non-custom type. This will clarify the exact failure modes and constrain the solution space.

2. **Evaluate a custom `ReferenceHandler` that is selective about which types get `$id`/`$ref`.** A custom `ReferenceHandler` subclass could create a resolver that skips types with parameterized constructors (records with primary constructors) while tracking reference types like Dictionary and List. This would thread the needle between the two conflicting requirements.

3. **Consider extending the converter-level approach rather than reverting.** Instead of restoring `ReferenceHandler` on options (which reverses v0.22.0's principle), consider whether RemoteFactory could provide a converter (or set of converters) for common non-custom types (Dictionary, List, plain classes) that use `NeatooReferenceResolver.Current` directly -- similar to how Neatoo's converters do it. This would preserve the "converter-level, not serializer-level" principle while extending reference tracking. Evaluate whether this is feasible or too broad.

4. **Address the `InterfaceFactory_NonNeatooType_NoRefMetadata` test explicitly.** This test asserts records have no `$id`/`$ref`. Any solution must either: (a) keep this assertion true (records still have no metadata, but other non-custom types do), or (b) update the test with a documented rationale for why the behavior changed. The test was created to guard against the `ObjectWithParameterizedCtorRefMetadataNotSupported` bug -- if the bug is no longer possible under the new design, the test's intent changes.

5. **Update published docs to match the actual scope.** Regardless of the implementation approach, the published docs (`docs/serialization.md:10`, `docs/appendix/serialization.md:53-55`) currently promise universal shared-instance identity. After implementation, verify the docs accurately describe which types participate in reference tracking and which do not. If any types are excluded (e.g., records with parameterized constructors), document the limitation.

6. **Multi-targeting.** Verify the solution works on both net9.0 and net10.0. The STJ parameterized-constructor limitation exists in both.

---

## Plans

- [Shared Reference Handling Plan](../../plans/completed/shared-reference-handling-plan.md)

---

## Tasks

- [x] Step 2: Architect comprehension check
- [x] Step 3: Business requirements review — APPROVED (with critical tension to resolve)
- [x] Step 4: Architect plan creation & design
- [x] Step 5: Developer review — Approved, implementation contract created
- [x] Step 7: Implementation — Phase 1 + Phase 2 complete, zero failures
- [x] Step 8: Verification — Architect VERIFIED, Requirements SATISFIED
- [x] Step 9: Documentation — requirements docs, appendix, serialization docs updated
- [x] Step 10: Completion

---

## Progress Log

### 2026-03-20
- Todo created from Neatoo's RemoteFactory v0.22.0 serializer migration work
- The Neatoo migration [Ignore]d `SharedDictionaryReference` test and flagged this for RemoteFactory architectural decision
- The Neatoo migration is otherwise complete (2111 passed, 1 [Ignore]d)
- Decision pending on whether RemoteFactory or Neatoo owns this feature
- Promoted to full project-todos workflow for architectural analysis
- Architect comprehension check: Ready -- 5 questions answered
- Requirements review: APPROVED -- 13 relevant requirements, 5 gaps, no hard contradictions, critical tension with v0.22.0 converter-level principle
- Architect plan created: two-phase approach (reproduction then fix), 13 business rules, custom ReferenceHandler subclass design
- Developer review: Approved -- all 13 assertions traced, no logic errors, implementation contract created
- Phase 1 implementation: exploration tests created, NeatooPreserveReferenceHandler built
- Phase 2 blocked: STJ parameterized-constructor limitation is permanent and comprehensive -- throws for ANY $id/$ref on reference-type constructor params. Plan sent back for architect revision.

### 2026-03-21
- Architect revision complete for Phase 2 after extensive analysis and user discussion
- Decided approach: **Simple Bypass Converter with DDD Justification**
  - `NeatooPreserveReferenceHandler` (already built) wired into serializer options for mutable types
  - New `RecordBypassConverterFactory` (~50-80 lines) claims ALL types with parameterized constructors, delegates to inner options without ReferenceHandler
  - Records serialize without $id/$ref -- correct DDD behavior (value objects have no identity to track)
- Detection rule: all types with parameterized constructors (simplest, safest per user decision)
- DDD justification resolves the nested-reference-type concern: a Dictionary inside a record is part of the value object's state; duplication is semantically correct
- New documentation deliverable: `docs/appendix/record-reference-handling.md` -- appendix explaining the DDD rationale (user request: "document this in its own appendix style/level")
- `InterfaceFactory_NonNeatooType_NoRefMetadata` test passes unchanged -- bypass converter claims records before ReferenceHandler can add metadata
- Plan updated to "Under Review (Developer)" -- ready for developer review (Step 5)
- Developer review of revised Phase 2: Approved
- Phase 2 implementation complete: RecordBypassConverterFactory + NeatooPreserveReferenceHandler wired in. Zero failures.
- Verification: Architect VERIFIED (13/13 scenarios), Requirements SATISFIED (all 8 requirements traced)
- Documentation: CLAUDE-DESIGN.md, serialization.md, appendix/serialization.md updated. New appendix `record-reference-handling.md` created.
- Stale comment in SerializationTests.cs fixed (moved circular references to PARTIAL section)
- **Complete**

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors, all projects build (net9.0 + net10.0)
- Tests: 490 unit + 498 integration + 42 design per framework, 0 failures

---

## Results / Conclusions

Shared reference handling for non-custom types implemented with a two-component approach justified by DDD semantics:

1. **`NeatooPreserveReferenceHandler`** — bridges STJ's built-in converters to `NeatooReferenceResolver.Current`. Mutable reference types (Dictionary, List, plain classes with default constructors) now participate in `$id`/`$ref` reference tracking.

2. **`RecordBypassConverterFactory`** — claims types with parameterized constructors (records) and delegates to inner options without `ReferenceHandler`. Records serialize without reference metadata.

**DDD rationale:** Records are value objects — defined by their values, not their identity. Duplicating a record's internal state (including nested collections) on round-trip is semantically correct. Reference identity only matters for entities. This is not a limitation or compromise; it's correct domain model behavior.

**STJ limitation:** `dotnet/runtime#73302` (closed NOT_PLANNED) — STJ cannot handle `$id`/`$ref` metadata on types with parameterized constructors. The bypass converter is the designed extensibility mechanism, not a workaround.

New appendix documentation at `docs/appendix/record-reference-handling.md` explains the full rationale to prevent future sessions from re-flagging this as a bug.
