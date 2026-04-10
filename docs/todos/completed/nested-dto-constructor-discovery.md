# Recursive Nested DTO Constructor Discovery for IL Trimming

**Status:** Complete
**Priority:** High
**Created:** 2026-03-30
**Last Updated:** 2026-03-31

---

## Problem

The generator-emitted DTO constructor registration (completed in [generator-dto-constructor-emission](completed/generator-dto-constructor-emission.md)) only discovers DTOs that are direct return types from factory method signatures. DTOs used as properties of other DTOs are not discovered, so they don't get registered in `DtoConstructorRegistry` and fail to deserialize under IL trimming.

### Production evidence

Discovered in zTreatment production deployment (2026-03-31):
- `AdminUserListItem` is returned from a factory method -> registered correctly
- `AdminClinicAssignment` is a property (`List<AdminClinicAssignment> Clinics`) on `AdminUserListItem` -> **not registered**
- Deserialization of `AdminClinicAssignment` fails under Blazor WASM IL trimming

### Current workaround

Users must either:
1. Return the nested DTO type from a factory method somewhere (triggers automatic discovery)
2. Manually register it: `DtoConstructorRegistry.Register<NestedDto>(() => new NestedDto())`

Both workarounds are fragile and non-obvious.

### Prior work

- [Completed todo: generator-dto-constructor-emission](completed/generator-dto-constructor-emission.md)
- [Completed plan: generator-dto-constructor-plan](../plans/completed/generator-dto-constructor-plan.md)
- CLAUDE-DESIGN.md Design Debt table entry: "Nested DTO discovery for trimming"
- docs/trimming.md: nested DTO limitation note

### Key files

- `src/Generator/FactoryGenerator.Types.cs` — `DiscoverDtoReturnTypes` method (line ~817) needs recursive property walking
- `src/RemoteFactory/Internal/DtoConstructorRegistry.cs` — no changes expected (already supports arbitrary registrations)
- `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs` — no changes expected

## Solution

Extend `DiscoverDtoReturnTypes` in `FactoryGenerator.Types.cs` to recursively walk the properties of discovered DTO types. For each property whose type passes the existing DTO eligibility checks (public parameterless constructor, not `[Factory]`, not record, not System namespace, etc.), add it to the discovered set and recurse into its properties.

### Key considerations

1. **Cycle detection** — DTOs may reference each other (A has property of type B, B has property of type A). Must track visited types to avoid infinite recursion.
2. **Collection unwrapping** — Properties may be `List<NestedDto>`, `IReadOnlyList<NestedDto>`, arrays, etc. Reuse existing unwrapping logic.
3. **Nullable unwrapping** — Properties may be `NestedDto?`. Reuse existing nullable stripping.
4. **Generator performance** — Recursive walking adds cost. Should be bounded by the visited set.
5. **Existing tests** — All 2142 existing tests must continue to pass.

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-30
**Verdict:** APPROVED

### Relevant Requirements Found

1. **DTO Constructor Registry for Trimming** (`src/Design/CLAUDE-DESIGN.md`, lines 579-598, "DTO Constructor Registry for Trimming" section). This section documents the exact mechanism the todo extends: the generator discovers plain DTO return types at compile time and emits `DtoConstructorRegistry.Register<T>(() => new T())` calls. The proposed change applies the same discovery criteria (public parameterless constructor, not `[Factory]`, not record, not System namespace, not abstract/interface) to properties of discovered DTOs. No new eligibility rules are introduced — the same criteria are reused recursively.

2. **Known Limitation — Nested DTO Discovery** (`src/Design/CLAUDE-DESIGN.md`, line 598; `docs/trimming.md`, lines 260-263). Both CLAUDE-DESIGN.md and trimming.md explicitly document this limitation: "DTO discovery only covers direct return types and their generic type arguments. Nested DTOs — types that appear as properties of a discovered DTO but are not themselves returned by any factory method — are not automatically registered." The todo directly resolves this documented limitation.

3. **Design Debt Entry: "Nested DTO discovery for trimming"** (`src/Design/CLAUDE-DESIGN.md`, line 770, Design Debt table). The table entry states: "Only direct return types registered | Recursive property walking adds generator complexity; workaround: return nested DTOs from a factory method | **Reconsider When:** If users frequently hit `DeserializeNoConstructor` for nested DTO properties." The todo documents that this condition has been met — the failure was discovered in zTreatment production deployment. The user explicitly created this follow-up todo to resolve the debt. This is NOT a contradiction; it is deliberate resolution of a design debt item whose reconsideration condition was triggered.

4. **Quick Decisions Table: Nested DTO Guidance** (`src/Design/CLAUDE-DESIGN.md`, line 157). The current guidance says: "What if my nested DTO fails to deserialize under trimming? Return it from a factory method so the generator discovers it, or register it manually." After implementation, this guidance should be updated to reflect that nested DTOs are now automatically discovered.

5. **RecordBypassConverterFactory Exclusion** (`src/Design/CLAUDE-DESIGN.md`, Anti-Pattern 9, lines 382-423). The existing DTO discovery already excludes records (types with no public parameterless constructor and parameterized constructors). The recursive property walking must apply the same exclusion. The plan correctly states the same eligibility checks are reused — records encountered as property types will be excluded by the `hasPublicParameterlessCtor` check.

6. **[Factory]-Annotated Type Exclusion** (existing `DiscoverDtoReturnTypes` at `src/Generator/FactoryGenerator.Types.cs`, lines 909-924). The method already checks for `[Factory]` on the type and its interfaces. Property types that are `[Factory]`-annotated entities (e.g., an aggregate root referenced as a property) must also be excluded — they are DI-registered and go through ordinal serialization, not the DTO path. The plan's reuse of `IsDtoCandidate` helper ensures this.

7. **Collection/Nullable Unwrapping Consistency** (existing `DiscoverDtoReturnTypes` at `src/Generator/FactoryGenerator.Types.cs`, lines 830-868). The method currently unwraps `Task<T>`, nullable `T?`, and generic collections (`IEnumerable<T>`, arrays). The plan correctly identifies that property walking only needs nullable and collection unwrapping (no `Task<T>` unwrapping — properties are not async). The plan proposes extracting `UnwrapType` and `IsDtoCandidate` helpers to avoid duplication.

8. **Incremental Generator Caching** (`EquatableArray<string>` on `TypeInfo.DtoReturnTypes` at `src/Generator/FactoryGenerator.Types.cs`, line 245). The `EquatableArray<string>` already supports value equality for incremental generator caching. Adding more strings (from nested discovery) to the array does not change the caching mechanism — it just means more types per method's `DtoReturnTypes`. The deduplication in the `TypeInfo` constructor (lines 237-244, `HashSet<string>`) handles overlapping discoveries from multiple methods.

9. **DtoConstructorRegistry Idempotency** (`src/RemoteFactory/Internal/DtoConstructorRegistry.cs`, line 24). The registry uses `ConcurrentDictionary.TryAdd`, making duplicate registrations harmless. If the same nested DTO is discovered through property walking from multiple parent DTOs (e.g., both `ParentA.Child` and `ParentB.Child`), the second registration is silently ignored. No risk of conflict.

10. **Generator Must Target netstandard2.0** (`src/Generator/Generator.csproj`, line 4). The generator targets netstandard2.0 (Roslyn requirement). The plan uses `INamedTypeSymbol.GetMembers()` filtered to `IPropertySymbol` — standard Roslyn API available in netstandard2.0. No compatibility issue.

11. **Existing DTO — ExampleDto Has No Nested Properties** (`src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs`, lines 493-497). The current Design project's `ExampleDto` has only `int Id` and `string Name` — no nested DTO properties. The recursive walking will execute on `ExampleDto` but discover no nested DTOs (primitives and strings are filtered by `SpecialType` and `System` namespace checks). This confirms no regression in existing behavior.

### Gaps

1. **No Existing Test for DTO Property Walking.** There are no existing unit tests for the `DiscoverDtoReturnTypes` method itself — not even for the current direct-return-type discovery. The plan proposes adding tests for the recursive walking. The architect should also consider adding baseline tests for the current behavior (before recursion) to guard against regressions in the extraction refactoring (Step 1 of the plan).

2. **Design Project Does Not Demonstrate Nested DTOs.** The Design source of truth (`ExampleDto` in `AllPatterns.cs`) does not have a nested DTO property. After implementation, the architect should consider whether the Design project needs a `ParentDto` with a `List<ChildDto>` property to serve as the source of truth for this pattern. The Design project is supposed to demonstrate all supported patterns.

3. **Cross-Assembly Nested DTO Types.** If a DTO's property references a type defined in a different assembly, the generator can still see it via Roslyn's `ITypeSymbol` semantic model. However, the emitted `() => new Dto()` lambda requires the type to be accessible (public constructor visible from the generated code's assembly). The plan does not explicitly discuss cross-assembly access. This was already true for direct return types and is not a new concern, but worth noting.

4. **Documentation Updates Needed.** After implementation, the following documentation must be updated:
   - `src/Design/CLAUDE-DESIGN.md` line 598: Remove or update the "Known limitation" paragraph
   - `src/Design/CLAUDE-DESIGN.md` line 770: Remove the Design Debt table entry (debt resolved)
   - `src/Design/CLAUDE-DESIGN.md` line 157: Update the Quick Decisions Table entry
   - `docs/trimming.md` lines 260-263: Update the "Nested DTOs are not automatically discovered" paragraph

### Contradictions

None. The todo resolves a documented design debt item whose reconsideration condition (users hitting `DeserializeNoConstructor` for nested DTO properties) has been met per the production evidence from zTreatment.

### Recommendations for Architect

1. **Reuse eligibility criteria exactly** — do not introduce any new rules for property-discovered DTOs. The same `IsDtoCandidate` check that applies to return types must apply to property types. This maintains a single source of truth for what qualifies as a registerable DTO.

2. **Extract helpers before adding recursion** — the plan's Step 1 (refactoring `UnwrapType` and `IsDtoCandidate`) should produce identical output as a verifiable intermediate step. Run the full test suite after extraction and before adding the recursive walking.

3. **Property walking should use `GetMembers()` on the type hierarchy** — if a base class defines a DTO property, the derived type's property walking should discover it. Verify whether `GetMembers()` includes inherited members or if `GetBaseType()` traversal is needed.

4. **Consider adding a Design project nested DTO example** — add a `ParentDto` with `List<ChildDto>` property to `AllPatterns.cs` and a corresponding test in `InterfaceFactoryTests.cs` to serve as the source of truth for this pattern.

5. **Update documentation after implementation** — resolve the four documentation items listed in Gaps #4 above. The Design Debt table entry, Known Limitation paragraphs, and Quick Decisions Table guidance all need updating to reflect the resolved limitation.

6. **Over-discovery is acceptable** — as the plan notes, registering DTOs that are never deserialized is harmless (small static cost). Under-discovery causes runtime failures. Err on the side of discovery.

---

## Plan

[nested-dto-constructor-plan](../plans/nested-dto-constructor-plan.md)

## Tasks

- [x] Create todo
- [x] Create plan (Step 1B)
- [x] Requirements review (Step 2)
- [x] Architect review (Step 3)
- [x] Developer review (Step 4)
- [x] Implementation (Step 6)
- [x] Verification (Step 7)
- [x] Documentation (Step 8)
- [x] Completion (Step 9)

---

## Progress Log

| Date | Update |
|------|--------|
| 2026-03-30 | Created todo from gap identified in generator-dto-constructor-emission completion |
| 2026-03-30 | Plan drafted, requirements review APPROVED, architect review APPROVED, developer review APPROVED |
| 2026-03-31 | Implementation complete. Stash/pop regression proof confirmed. Architect VERIFIED (15/15 scenarios). Requirements SATISFIED (14/14 items). Documentation updated in 4 locations. |

## Results

Extended `DiscoverDtoReturnTypes` in `src/Generator/FactoryGenerator.Types.cs` to recursively walk public instance properties of discovered DTOs. Extracted `UnwrapType`, `IsDtoCandidate`, and `DiscoverDtoTypesRecursive` helper methods. Handles collection/nullable unwrapping, inherited properties via `GetBaseType()` chain, and circular references via visited set. 15 new test scenarios (14 test methods + full suite regression proof). All 2082 tests pass. Resolves Design Debt item "Nested DTO discovery for trimming" — documentation updated in CLAUDE-DESIGN.md and trimming.md.
