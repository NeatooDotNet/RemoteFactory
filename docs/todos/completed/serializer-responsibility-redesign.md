# Serializer Responsibility Redesign

**Status:** Complete
**Priority:** High
**Created:** 2026-03-20
**Last Updated:** 2026-03-20

---

## Problem

The v0.21.3 fix (dual `JsonSerializerOptions` with `IsNeatooType` classification) didn't work. Worse, the "neatoo vs non-neatoo" approach made wrong assumptions about Neatoo:

1. Neatoo entities can't implement `IOrdinalSerializable` — that interface is generated for `[Factory]`-decorated types, not for Neatoo's own base classes (`ValidateBase`, `EntityBase`, etc.)
2. The `PlainOptions` path (no `ReferenceHandler`) breaks downstream converters that need `ReferenceHandler` — 88 Neatoo serialization tests fail with `NullReferenceException`
3. RemoteFactory is making assumptions about which types need what serialization behavior, but it doesn't have enough knowledge to make those decisions

The fundamental question: RemoteFactory and Neatoo need to work such that **RemoteFactory makes no assumptions about the types being serialized** and **Neatoo takes over for what Neatoo objects need**.

## Solution

Analyze and redesign the serializer responsibilities:

1. What makes RemoteFactory need a custom serializer at all?
2. What do we want to keep?
3. What does Neatoo manage?
4. What was the original bug (the record deserialization issue that prompted v0.21.3)?

Design a clean separation where RemoteFactory handles transport/protocol concerns and Neatoo handles its own domain object serialization needs, without RemoteFactory needing to classify types.

---

## Related Work

- Prior fix (v0.21.3): [record-deserialization-ref-metadata](completed/record-deserialization-ref-metadata.md) — the fix being reconsidered
- Immediate failure: [isneatootype-missing-validatebase-check](isneatootype-missing-validatebase-check.md) — documents the 88 test failures

## Key Files

- `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` — the serializer with the broken dual-options approach
- `src/RemoteFactory/IOrdinalSerializable.cs` — interface the `IsNeatooType` check relies on
- `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs` — ordinal format converter
- `src/RemoteFactory/Internal/NeatooInterfaceJsonConverterFactory.cs` — interface type converter
- `src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs` — handles `$id`/`$ref` for interface types

---

## Clarifications

**Q1 (Architect):** When you say "Neatoo takes over for what Neatoo objects need," are you referring to Neatoo's custom converters registered via `NeatooJsonConverterFactory` DI extension point? Or a broader mechanism?

**A1:** Yes, through the converter extension point. User admits not fully understanding the reference handling tension.

**Q2 (Architect):** Is the original record bug still the priority, or is the redesign the priority?

**A2:** Redesign first, then the record bug. Don't forget the record bug — re-open or recreate a todo for it if needed.

**Q3 (Architect):** Do you have a target architecture in mind? (a) Remove dual-options, go back to single Options, fix record bug differently, or (b) Keep split but make extensible?

**A3:** Don't know. Let's dig deeper.

**Q4 (Architect):** Is reverting v0.21.3 on the table as a starting point?

**A4:** Yes, on the table.

**Q5 (Architect):** Should Neatoo's converters manage their own reference handling independently, or should RemoteFactory guarantee ReferenceHandler is always available?

**A5:** That's an option. But more broadly — what value should RemoteFactory provide to a consumer? Things like serializing interfaces, types within lists — those are RemoteFactory strengths, not necessarily Neatoo. Can reference handling be one of RemoteFactory's responsibilities, or is that too ambitious? What does this bug tell us? The user wants the architect to dig into this question before proposing a solution.

**Architect analysis (follow-up to A3/A5):** Reference handling is a converter-level concern, not a serializer-level concern. Proposed direction: remove `ReferenceHandler` from `JsonSerializerOptions` entirely. Provide `NeatooReferenceResolver` through a static `AsyncLocal` accessor. Converters access it directly. STJ never sees it — records serialize natively. `IsNeatooType()` and `PlainOptions` are deleted.

**Q6 (Architect):** Does the "always-available resolver, no global ReferenceHandler" direction align?

**A6:** Yes.

**Q7 (Architect):** Is the coordinated Neatoo converter change acceptable?

**A7:** Yes, it is.

**Q8 (Architect):** Ready to proceed to formal plan?

**A8:** Proceed.

Architect confirmed **Ready**.

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-20
**Verdict:** APPROVED

### Relevant Requirements Found

1. **`NeatooReferenceHandler` already uses `AsyncLocal` (`src/RemoteFactory/Internal/NeatooReferenceHandler.cs:8`).** The proposed direction (static `AsyncLocal` accessor for `NeatooReferenceResolver`) is consistent with the existing pattern. The current `NeatooReferenceHandler` wraps an `AsyncLocal<ReferenceResolver>` and surfaces it via the STJ `ReferenceHandler.CreateResolver()` override. The proposal replaces this indirect channel with direct `AsyncLocal` access by converters.

2. **Only one RemoteFactory converter accesses `options.ReferenceHandler` (`src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs:46`).** The `NeatooInterfaceJsonTypeConverter.Read()` method calls `options.ReferenceHandler!.CreateResolver().AddReference(id, result)` to register `$id` values during deserialization. This is the sole RemoteFactory call site that must be migrated to the new `AsyncLocal` accessor. The `NeatooOrdinalConverterFactory` and `NeatooOrdinalConverter<T>` do not reference `ReferenceHandler` at all.

3. **Downstream Neatoo converters are the primary consumer of `ReferenceHandler` (`docs/todos/isneatootype-missing-validatebase-check.md:32-33`).** The 88-test failure documented in the related todo shows that Neatoo's `NeatooBaseJsonTypeConverter.Write()` calls `options.ReferenceHandler.CreateResolver().GetReference(value, out var alreadyExists)`. The redesign's coordinated Neatoo update must migrate these call sites to the new `AsyncLocal` accessor.

4. **Anti-Pattern 9 documents the Neatoo/record serialization boundary (`src/Design/CLAUDE-DESIGN.md:378-419`).** Records are serialized without `$id`/`$ref`; Neatoo types require reference handling. This anti-pattern's rationale is based on the current `IsNeatooType()`/`PlainOptions` split. The redesign removes this split, so the anti-pattern's technical explanation will need updating, though the user-facing rule (do not mix) remains valid -- converters that use reference handling still cannot coexist with STJ's native record serialization in the same object graph.

5. **Quick Decisions Table entry for records (`src/Design/CLAUDE-DESIGN.md:153`).** States "Records are serialized without `$id`/`$ref`; do not mix Neatoo types into record properties." This describes the desired behavior. The redesign achieves the same outcome through a different mechanism (converters decide, not the serializer), so the rule remains valid.

6. **Published serialization docs: "Scope: Neatoo Types Only" section (`docs/serialization.md:120-124`).** Added in v0.21.3, this section documents that `$id`/`$ref` applies only to Neatoo types. The section references the type classification approach ("classes and records decorated with `[Factory]` that implement `IOrdinalSerializable`, and interface/abstract types registered in the factory assembly"). The redesign changes the mechanism (converter-level, not serializer-level), so the doc text will need updating to describe the new approach.

7. **`NeatooJsonSerializer` constructor builds two `JsonSerializerOptions` instances (`src/RemoteFactory/Internal/NeatooJsonSerializer.cs:76-113`).** The `Options` set has `ReferenceHandler`, `PlainOptions` does not. Both share the same converter factories and type info resolver. The redesign deletes `PlainOptions` and removes `ReferenceHandler` from `Options` entirely, returning to a single options set -- simpler than v0.21.0 (single options WITH `ReferenceHandler`) or v0.21.3 (dual options).

8. **`IsNeatooType()` relies on `IOrdinalSerializable` and `IServiceAssemblies.HasType()` (`src/RemoteFactory/Internal/NeatooJsonSerializer.cs:123-136`).** This is the type classification method the todo proposes deleting. Its two checks are: (a) `IOrdinalSerializable` assignability for `[Factory]`-decorated types, and (b) `IServiceAssemblies.HasType()` for interface/abstract types registered via factory assemblies.

9. **`NeatooJsonTypeInfoResolver` overrides `CreateObject` for DI-registered types (`src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs:24-39`).** This resolver is shared between `Options` and `PlainOptions` today. It remains relevant after the redesign because it is how deserialized Neatoo types get their constructor-injected services. The redesign does not affect this resolver's operation since it is independent of `ReferenceHandler`.

10. **`NeatooJsonConverterFactory` is the DI extension point for downstream converters (`src/RemoteFactory/Internal/NeatooJsonConverterFactory.cs`).** This abstract base class is registered as a service type (`AddRemoteFactoryServices.cs:65`). Neatoo registers its own converter factories through this extension. All registered converter factories are added to `Options.Converters` (and currently `PlainOptions.Converters`). After the redesign, they will be in the single options set -- but they must access the resolver via the new `AsyncLocal` accessor rather than `options.ReferenceHandler`.

11. **Design project serialization tests (`src/Design/Design.Tests/FactoryTests/SerializationTests.cs`).** Six tests verify round-trip serialization for Create, Fetch, value objects, collections, nullables, and Save. These tests must continue to pass after the redesign. They test the full DI-based serialization pipeline through `DesignClientServerContainers`.

12. **Interface Factory record round-trip test (`src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs:96-117`).** Tests `GetRecordByIdAsync()` returning an `ExampleRecordResult` record through client-server serialization. This test validates the scenario that prompted the original v0.21.3 fix. The redesign must keep this test passing -- records should serialize without `$id`/`$ref` because no converter injects reference metadata for them.

13. **Design Debt table (`src/Design/CLAUDE-DESIGN.md:728-739`).** No design debt item addresses serializer architecture or reference handling. The proposed redesign does not implement any deliberately deferred feature.

### Gaps

1. **No documented contract for `AsyncLocal` resolver accessibility by third-party converters.** The redesign introduces a new public/internal API surface: the static `AsyncLocal` accessor on `NeatooReferenceResolver` (or a new static class). There is no existing requirement specifying the visibility, thread safety guarantees, or lifecycle semantics of this accessor. The architect must define: (a) Is the accessor `public` (for Neatoo/downstream converters) or `internal`? (b) Who creates/disposes the `NeatooReferenceResolver` per serialization call? (c) What happens if a converter accesses the resolver when no serialization is in progress (null safety)?

2. **No documented contract for how the `NeatooInterfaceJsonTypeConverter` should access reference state after the redesign.** The current code at `NeatooInterfaceJsonTypeConverter.cs:46` uses `options.ReferenceHandler!.CreateResolver()`. After the redesign, this code must be changed to use the `AsyncLocal` accessor. The Design projects do not currently cover this converter's behavior in their tests -- the interface factory tests exercise the full pipeline but do not independently verify `$id`/`$ref` registration for interface-typed properties.

3. **No documented requirement for the breaking change to Neatoo's converter API.** The coordinated Neatoo update is acknowledged in the clarifications (A7) but there is no documented migration path. The architect should document: (a) which Neatoo call sites must change, (b) the before/after API for accessing the resolver, (c) minimum RemoteFactory version required.

4. **No test for reference preservation with interface-typed properties after the redesign.** The Design project `SerializationTests.cs` tests round-trip for value objects and collections, but no test explicitly verifies that `$id`/`$ref` is correctly written/read for interface-typed properties (the scenario handled by `NeatooInterfaceJsonTypeConverter`). After removing `ReferenceHandler` from options, this is a critical path to validate.

### Contradictions

None found.

- The redesign does not implement any feature in the Design Debt table (`src/Design/CLAUDE-DESIGN.md:728-739`).
- The redesign does not violate any of the 10 documented anti-patterns. Anti-Pattern 9 (mixing Neatoo types with records) remains valid under the new architecture -- the user-facing rule is unchanged even though the enforcement mechanism changes.
- The redesign is consistent with the clarifications from the original record-deserialization todo (A2: reference preservation for Neatoo types only; A3: mixing is an anti-pattern; A4: full breaking change tolerance).
- The `NeatooReferenceHandler` already uses `AsyncLocal` internally (`NeatooReferenceHandler.cs:8`), so the proposed direction is an evolution of the existing pattern, not a contradiction.

### Recommendations for Architect

1. **Define the `AsyncLocal` accessor API precisely.** The accessor must be: (a) accessible to `NeatooInterfaceJsonTypeConverter` within RemoteFactory, (b) accessible to Neatoo's converters (which are in a separate assembly), and (c) null-safe when no serialization is in progress. Consider making the accessor a static property on `NeatooReferenceResolver` itself (e.g., `NeatooReferenceResolver.Current`) since that class is already `public`.

2. **Preserve `NeatooReferenceResolver` lifecycle management in `NeatooJsonSerializer`.** Today, each `Serialize`/`Deserialize` call creates a scoped `NeatooReferenceResolver` via `using var rr = new NeatooReferenceResolver()` and assigns it to the `AsyncLocal` on `NeatooReferenceHandler` (`NeatooJsonSerializer.cs:156-158`). After the redesign, the serializer should continue to own the resolver lifecycle (create, assign to `AsyncLocal`, dispose after serialization). This keeps the scoping clean and prevents cross-request reference ID collisions.

3. **Update `NeatooInterfaceJsonTypeConverter.Read()` at line 46.** Change from `options.ReferenceHandler!.CreateResolver().AddReference(id, result)` to use the new `AsyncLocal` accessor. This is the only RemoteFactory code change needed beyond the serializer itself.

4. **Verify Anti-Pattern 9 remains valid.** The anti-pattern's "why it matters" explanation currently references the dual-options split ("the record is serialized without reference handling, but the embedded Neatoo type expects it"). After the redesign, the technical reason changes: converters for Neatoo types will add `$id`/`$ref` metadata using the resolver directly, but STJ's native record handling cannot process `$ref` in parameterized constructors. The user-facing rule is identical. Update the explanation text in `CLAUDE-DESIGN.md` and `docs/serialization.md`.

5. **Add a test for interface-typed property reference preservation.** The Design project lacks a dedicated test for `$id`/`$ref` behavior on interface-typed properties. Since `NeatooInterfaceJsonTypeConverter` is the only RemoteFactory converter that touches the resolver, this is an important regression test for the redesign.

6. **Document the Neatoo migration.** Since this is a coordinated breaking change, the plan should include a clear list of Neatoo call sites that access `options.ReferenceHandler.CreateResolver()` and must migrate to the `AsyncLocal` accessor.

7. **Keep the record round-trip bug visible.** Per clarification A2, the original record deserialization bug should not be lost. After the redesign, verify the `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` test still passes, and re-open/create a todo for any remaining record issues if the redesign does not fully resolve them.

---

## Plans

- [Serializer Responsibility Redesign](../plans/serializer-responsibility-redesign.md)

---

## Tasks

- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3)
- [x] Architect plan creation & design (Step 4)
- [x] Developer review (Step 5) -- Approved 2026-03-20, implementation contract created
- [x] Implementation (Step 7) — 2,038 tests passed, 0 failed
- [x] Verification (Step 8) — Architect VERIFIED, Requirements SATISFIED
- [x] Documentation (Step 9) — Requirements docs updated, release notes v0.21.4 created

---

## Progress Log

### 2026-03-20
- Created todo from user's assessment that v0.21.3 fix didn't work
- User identified fundamental flaw: RemoteFactory shouldn't classify "neatoo vs non-neatoo" types
- Existing todo `isneatootype-missing-validatebase-check.md` documents the immediate failure (88 Neatoo tests crash)
- This todo addresses the broader redesign question
- Developer review completed: all 18 business rule assertions traced and verified
- 5 concerns raised, all acknowledged by user
- Implementation contract created, plan status set to "Ready for Implementation"

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors, all projects build (net9.0 + net10.0)
- Tests: 2,038 passed, 0 failed, 6 skipped (pre-existing performance test skips)

---

## Results / Conclusions

Reference handling redesigned as a converter-level concern. `ReferenceHandler` removed from `JsonSerializerOptions`, `IsNeatooType()`/`PlainOptions` deleted, `NeatooReferenceHandler.cs` deleted. New `NeatooReferenceResolver.Current` static `AsyncLocal` accessor provides the resolver to converters directly. Records now serialize natively without `$id`/`$ref` interference. Coordinated Neatoo converter migration required (tracked separately). Version bumped to v0.21.4.

**Supersedes:** The `isneatootype-missing-validatebase-check.md` todo is resolved by this work — the `PlainOptions` split that caused the 88 Neatoo test failures no longer exists.
