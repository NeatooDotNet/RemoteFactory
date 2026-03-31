# Plan: Recursive Nested DTO Constructor Discovery

**Status:** Complete
**Created:** 2026-03-30
**Last Updated:** 2026-03-31
**Todo:** [nested-dto-constructor-discovery](../todos/nested-dto-constructor-discovery.md)

---

## Overview

Extend the source generator's DTO discovery to recursively walk properties of discovered DTO types, finding nested DTOs that also need `DtoConstructorRegistry.Register<T>()` calls to survive IL trimming in Blazor WASM.

Currently, `DiscoverDtoReturnTypes` in `FactoryGenerator.Types.cs` only inspects the direct return type of factory methods. DTOs used as properties of returned DTOs are invisible to the generator, causing deserialization failures under IL trimming.

## Approach

Modify `DiscoverDtoReturnTypes` to recursively inspect properties of each discovered DTO candidate. After a type passes all eligibility checks (public parameterless ctor, not `[Factory]`, not System namespace, etc.), walk its public properties and apply the same unwrapping (collection, nullable) and eligibility logic to each property type. Use a visited set to prevent infinite recursion from circular references.

The change is contained entirely within `DiscoverDtoReturnTypes` — no changes to the aggregation pipeline, renderers, or registry are needed.

## Design

### Modified method: `DiscoverDtoReturnTypes`

**Current flow:**
1. Unwrap return type (Task, nullable, collection)
2. Check eligibility (not primitive, not System, not abstract, not [Factory], has public parameterless ctor)
3. Add qualifying types to `dtoTypes` list
4. Return

**New flow:**
1. Unwrap return type (Task, nullable, collection) — unchanged
2. Check eligibility — unchanged
3. Add qualifying types to `dtoTypes` list — unchanged
4. **NEW: For each qualifying type, walk its public instance properties:**
   a. Get `INamedTypeSymbol.GetMembers()` filtered to `IPropertySymbol` where `DeclaredAccessibility == Public` and not static
   b. For each property type, unwrap (nullable, collection) using the same logic
   c. Apply the same eligibility checks
   d. If eligible and not already in the visited set, add to `dtoTypes` and recurse into its properties
5. Return

### Refactoring approach

Extract the eligibility check and collection/nullable unwrapping into helper methods to avoid duplicating the logic between the return-type path and the property-walking path:

1. **`UnwrapType(ITypeSymbol type)`** — strips Task, nullable, and collection wrappers, returns the inner type(s)
2. **`IsDtoCandidate(INamedTypeSymbol type)`** — applies all eligibility checks, returns bool
3. **`DiscoverDtoReturnTypes(ITypeSymbol returnType)`** — calls UnwrapType + IsDtoCandidate for the return type, then recursively walks properties of discovered DTOs

For property walking, only nullable and collection unwrapping is needed (no Task unwrapping — properties aren't async).

### Cycle detection

Use a `HashSet<string>` of fully qualified type names (same format as `dtoTypes`). Before recursing into a type's properties, check if it's already visited. This handles:
- Direct cycles: A -> B -> A
- Indirect cycles: A -> B -> C -> A
- Self-references: A -> A

### What does NOT change

- `DtoConstructorRegistry` — already supports any number of registrations
- `NeatooJsonTypeInfoResolver` — already looks up from the registry
- Renderer code (ClassFactory, InterfaceFactory, StaticFactory) — they just iterate `DtoReturnTypes`
- The aggregation in `TypeInfo` constructor — it just deduplicates across methods
- The DTO eligibility criteria themselves — same rules, just applied recursively

## Implementation Steps

### Step 1: Refactor extraction helpers

Extract `UnwrapType` and `IsDtoCandidate` from the existing `DiscoverDtoReturnTypes` method body. Ensure the refactored method produces identical output (no behavior change).

### Step 2: Add recursive property walking

After a type is confirmed as a DTO candidate, iterate its public instance properties. For each property type, apply `UnwrapType` + `IsDtoCandidate`. If it qualifies and hasn't been visited, add it to the result set and recurse.

### Step 3: Add unit tests

Add tests covering:
- Nested DTO discovery (ParentDto with ChildDto property)
- Collection property unwrapping (`List<ChildDto>`, `IReadOnlyList<ChildDto>`, array)
- Nullable property unwrapping (`ChildDto?`)
- Cycle detection (A references B, B references A)
- Deep nesting (A -> B -> C)
- Mixed: some properties are DTOs, some are primitives/framework types
- Properties that are `[Factory]` types are excluded
- Properties that are abstract/interface are excluded

### Step 4: Integration validation

Build the full solution and run all 2142+ existing tests. Verify no regressions.

## Acceptance Criteria

1. All direct return type DTOs continue to be discovered (no regression)
2. Nested DTOs reachable through public properties are discovered and registered
3. Collection and nullable wrapping on properties is handled
4. Circular references don't cause infinite recursion
5. All existing tests pass
6. New unit tests cover the recursive walking scenarios

## Dependencies

- Completed: [generator-dto-constructor-emission](../plans/completed/generator-dto-constructor-plan.md)

## Risks

1. **Generator performance** — Recursive property walking adds compile-time cost. Mitigated by visited set (each type inspected at most once) and the existing filters (System namespace, primitives skip early).
2. **Over-discovery** — Could register types that aren't actually deserialized. This is harmless (unused registrations are just a small static cost) and is strictly better than under-discovery.
3. **Incremental generator caching** — The `EquatableArray<string>` for `DtoReturnTypes` already handles deduplication and equality comparison. Adding more strings to it doesn't change the caching behavior.

---

## Business Requirements Context

This plan resolves a documented design debt item in `src/Design/CLAUDE-DESIGN.md` (line 770, "Nested DTO discovery for trimming"). The reconsideration condition was met by a production failure in zTreatment: `AdminClinicAssignment` (a property of `AdminUserListItem`) failed deserialization under Blazor WASM IL trimming because it was not registered in `DtoConstructorRegistry`.

**Relevant requirements from the Requirements Review (APPROVED, zero contradictions):**

1. **DTO Constructor Registry for Trimming** (CLAUDE-DESIGN.md lines 578-598) -- The generator discovers plain DTO return types at compile time and emits `DtoConstructorRegistry.Register<T>(() => new T())` calls. The existing eligibility criteria (public parameterless ctor, not `[Factory]`, not record, not System namespace, not abstract/interface) must be reused identically for property-discovered DTOs.

2. **Known Limitation** (CLAUDE-DESIGN.md line 598; docs/trimming.md lines 260-263) -- Both documents explicitly note that nested DTOs through properties are not discovered. This plan directly resolves that documented limitation.

3. **Design Debt Entry** (CLAUDE-DESIGN.md line 770) -- "Only direct return types registered | Recursive property walking adds generator complexity | Reconsider When: If users frequently hit `DeserializeNoConstructor` for nested DTO properties." Condition met.

4. **DtoConstructorRegistry Idempotency** (DtoConstructorRegistry.cs line 24) -- Uses `ConcurrentDictionary.TryAdd`, so duplicate registrations from overlapping discoveries are harmless.

5. **netstandard2.0 Constraint** -- The generator targets netstandard2.0. The plan uses `INamedTypeSymbol.GetMembers()` and `IPropertySymbol`, which are standard Roslyn APIs available in netstandard2.0.

**Post-implementation documentation updates needed** (4 locations):
- `src/Design/CLAUDE-DESIGN.md` line 598: Remove/update "Known limitation" paragraph
- `src/Design/CLAUDE-DESIGN.md` line 770: Remove Design Debt table entry
- `src/Design/CLAUDE-DESIGN.md` line 157: Update Quick Decisions Table entry
- `docs/trimming.md` lines 260-263: Update "Nested DTOs are not automatically discovered" paragraph

## Business Rules (Testable Assertions)

**BR-NEST-001**: WHEN a factory method returns a type that passes DTO eligibility checks AND that type has a public instance property whose type also passes DTO eligibility checks, THEN the nested property type is included in `DtoReturnTypes`.
*Traces to: CLAUDE-DESIGN.md line 598 (resolves known limitation)*

**BR-NEST-002**: WHEN a factory method returns `ParentDto` which has a property `List<ChildDto> Items`, THEN both `ParentDto` and `ChildDto` appear in `DtoReturnTypes` (collection unwrapping applied to property types).
*Traces to: CLAUDE-DESIGN.md lines 594 (unwrapping behavior)*

**BR-NEST-003**: WHEN a factory method returns `ParentDto` which has a property `ChildDto? OptionalChild`, THEN both `ParentDto` and `ChildDto` appear in `DtoReturnTypes` (nullable unwrapping applied to property types).
*Traces to: CLAUDE-DESIGN.md lines 594 (unwrapping behavior)*

**BR-NEST-004**: WHEN a discovered DTO has a property of type `ChildDto` which itself has a property of type `GrandchildDto`, THEN all three types appear in `DtoReturnTypes` (recursion depth > 1).
*NEW -- extends existing single-level discovery to arbitrary depth*

**BR-NEST-005**: WHEN `DtoA` has a property of type `DtoB` and `DtoB` has a property of type `DtoA` (circular reference), THEN both types appear in `DtoReturnTypes` and the generator does not enter infinite recursion.
*NEW -- cycle detection*

**BR-NEST-006**: WHEN a discovered DTO has a property whose type is a `[Factory]`-annotated type, THEN that property type is excluded from `DtoReturnTypes` (same eligibility rule as direct return types).
*Traces to: CLAUDE-DESIGN.md line 589, existing `DiscoverDtoReturnTypes` lines 909-924*

**BR-NEST-007**: WHEN a discovered DTO has a property whose type is abstract, an interface, a record without a parameterless ctor, a primitive, or a System namespace type, THEN that property type is excluded from `DtoReturnTypes`.
*Traces to: CLAUDE-DESIGN.md lines 584-593 (eligibility criteria table)*

**BR-NEST-008**: WHEN the `UnwrapType` and `IsDtoCandidate` helpers are extracted from `DiscoverDtoReturnTypes` (Step 1 refactoring), THEN the method produces identical output to the pre-refactoring implementation -- zero behavior change.
*NEW -- refactoring safety*

**BR-NEST-009**: WHEN a discovered DTO inherits from a base class that has a DTO-eligible property, THEN the inherited property's type is also discovered.
*NEW -- inheritance traversal. Critical: `GetMembers()` returns only declared members, not inherited ones. The implementation must walk `GetBaseType()` or use an equivalent approach.*

**BR-NEST-010**: WHEN existing factory methods return simple DTOs (e.g., `ExampleDto` with only primitive properties), THEN their `DtoReturnTypes` output is unchanged from the current behavior.
*Regression guard -- traces to existing Design project tests*

## Test Scenarios

### TS-001: Single-level nested DTO (BR-NEST-001)
**Setup:** Factory returns `ParentDto` with property `ChildDto Child`. Both have public parameterless ctors, no `[Factory]`, not System types.
**Expected:** `DtoReturnTypes` contains both `ParentDto` and `ChildDto`.

### TS-002: Collection property unwrapping (BR-NEST-002)
**Setup:** Factory returns `ParentDto` with property `List<ChildDto> Items`.
**Expected:** `DtoReturnTypes` contains both `ParentDto` and `ChildDto`.

### TS-003: Array property unwrapping (BR-NEST-002)
**Setup:** Factory returns `ParentDto` with property `ChildDto[] Items`.
**Expected:** `DtoReturnTypes` contains both `ParentDto` and `ChildDto`.

### TS-004: IReadOnlyList property unwrapping (BR-NEST-002)
**Setup:** Factory returns `ParentDto` with property `IReadOnlyList<ChildDto> Items`.
**Expected:** `DtoReturnTypes` contains both `ParentDto` and `ChildDto`.

### TS-005: Nullable property unwrapping (BR-NEST-003)
**Setup:** Factory returns `ParentDto` with property `ChildDto? OptionalChild`.
**Expected:** `DtoReturnTypes` contains both `ParentDto` and `ChildDto`.

### TS-006: Deep nesting (BR-NEST-004)
**Setup:** Factory returns `A` with property `B Child`, `B` has property `C Grandchild`. All DTO-eligible.
**Expected:** `DtoReturnTypes` contains `A`, `B`, and `C`.

### TS-007: Circular reference (BR-NEST-005)
**Setup:** `DtoA` has property `DtoB Other`, `DtoB` has property `DtoA Back`.
**Expected:** `DtoReturnTypes` contains both `DtoA` and `DtoB`. No infinite loop or stack overflow.

### TS-008: Self-reference (BR-NEST-005)
**Setup:** `TreeNode` has property `TreeNode? Parent`.
**Expected:** `DtoReturnTypes` contains `TreeNode`. No infinite loop.

### TS-009: [Factory] property excluded (BR-NEST-006)
**Setup:** `ParentDto` has property `FactoryEntity Child` where `FactoryEntity` has `[Factory]`.
**Expected:** `DtoReturnTypes` contains `ParentDto` but not `FactoryEntity`.

### TS-010: Mixed property types (BR-NEST-007)
**Setup:** `ParentDto` has properties: `int Id` (primitive), `string Name` (System), `ChildDto Child` (eligible), `IService Svc` (interface).
**Expected:** `DtoReturnTypes` contains `ParentDto` and `ChildDto` only.

### TS-011: Abstract property type excluded (BR-NEST-007)
**Setup:** `ParentDto` has property `AbstractBase Item` where `AbstractBase` is abstract.
**Expected:** `DtoReturnTypes` contains `ParentDto` but not `AbstractBase`.

### TS-012: Refactoring preserves behavior (BR-NEST-008)
**Setup:** Run existing Design project and all test suite after Step 1 extraction (before adding recursion).
**Expected:** All tests pass. No change in generated output.

### TS-013: Inherited property discovery (BR-NEST-009)
**Setup:** `DerivedDto : BaseDto` where `BaseDto` has property `ChildDto Child`. Factory returns `DerivedDto`.
**Expected:** `DtoReturnTypes` contains `DerivedDto` and `ChildDto`.

### TS-014: No-regression for simple DTOs (BR-NEST-010)
**Setup:** Existing `ExampleDto` with only `int Id` and `string Name` properties.
**Expected:** `ExampleDto` in `DtoReturnTypes`, no new nested types discovered (primitives and strings filtered).

### TS-015: Collection of collections (edge case)
**Setup:** `ParentDto` has property `List<List<ChildDto>> Nested`.
**Expected:** The outer `List<List<ChildDto>>` implements `IEnumerable<List<ChildDto>>`. The inner `List<ChildDto>` is a System type that itself implements `IEnumerable<ChildDto>`. Whether `ChildDto` is discovered depends on whether the walker recurses into generic type arguments of System collections. Acceptable outcome: either `ChildDto` is discovered (over-discovery is fine) or it is not (the inner list is a System type and skipped). Document the chosen behavior.

## Domain Model Behavioral Design

This feature modifies the source generator's analysis pipeline, not runtime domain model behavior. No computed properties, visibility flags, reactive rules, or validation rules are involved.

**Generator pipeline changes:**

- **Modified method:** `DiscoverDtoReturnTypes` in `TypeFactoryMethodInfo` (FactoryGenerator.Types.cs, line 817)
- **New helper methods:** `UnwrapType(ITypeSymbol)` and `IsDtoCandidate(INamedTypeSymbol)` extracted from existing code
- **Data flow:** `DiscoverDtoReturnTypes` -> `TypeFactoryMethodInfo.DtoReturnTypes` (per-method) -> `TypeInfo.DtoReturnTypes` (aggregated, deduplicated via HashSet at line 237) -> renderers iterate to emit `Register<T>` calls
- **No changes to:** `DtoConstructorRegistry`, `NeatooJsonTypeInfoResolver`, any renderer, or the `TypeInfo` aggregation pipeline

**Critical implementation detail -- inherited properties:** Roslyn's `INamedTypeSymbol.GetMembers()` returns only **directly declared** members, not inherited members. The existing codebase confirms this pattern: `CollectPropertiesRecursive` (line 373-376) explicitly walks `GetBaseType()` for inheritance. The property walking in `DiscoverDtoReturnTypes` must do the same -- either by walking the base type chain for each discovered DTO's properties, or by using a helper that collects all public instance properties including inherited ones.

**Code cleanup opportunity:** Lines 927-934 of the current `DiscoverDtoReturnTypes` have a confused constructor check where the first assignment is dead code (immediately overwritten by a simplified version). The `IsDtoCandidate` extraction should use only the simplified check.

## Agent Phasing

This feature is small enough for a **single developer agent phase**. The total scope is:
- Extract 2 helper methods from existing code (pure refactoring)
- Add recursive property walking (~30-40 lines)
- Add unit tests (~150-200 lines)
- Run full test suite

**Phase 1 (single phase): Full implementation**
- Step 1: Extract `UnwrapType` and `IsDtoCandidate` helpers. Build and test (BR-NEST-008).
- Step 2: Add recursive property walking with cycle detection and inherited property traversal.
- Step 3: Add unit tests covering TS-001 through TS-015.
- Step 4: Full solution build and test run.

A fresh agent is not needed between steps because the context is small and self-contained within a single file (`FactoryGenerator.Types.cs`) plus new test files.

**Test strategy note:** Since `DiscoverDtoReturnTypes` is a `private static` method on `TypeFactoryMethodInfo`, direct unit testing requires either: (a) making it internal with `[InternalsVisibleTo]`, (b) testing indirectly through the generated output (checking that the generated `FactoryServiceRegistrar` contains the expected `Register<T>` calls), or (c) creating integration-style tests with real `[Factory]` types that return DTOs with nested properties and verifying the registration calls appear. Option (b) or (c) aligns best with existing test patterns in this project. The developer should choose the most practical approach.
