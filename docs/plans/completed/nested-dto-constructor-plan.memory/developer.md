# Developer -- Nested DTO Constructor Discovery

Last updated: 2026-03-30
Current step: Developer Review (Step 4) -- Complete, Approved

## Key Context

- The plan modifies only `DiscoverDtoReturnTypes` in `FactoryGenerator.Types.cs` (line 817-945)
- Three renderers (ClassFactory, InterfaceFactory, StaticFactory) just iterate `model.DtoReturnTypes` -- no changes needed
- `TypeInfo` aggregation at lines 236-245 uses `HashSet<string>` for deduplication -- no changes needed
- `DtoConstructorRegistry` uses `ConcurrentDictionary.TryAdd` -- idempotent, no changes needed
- Existing `CollectPropertiesRecursive` at line 366-377 demonstrates the inheritance walking pattern (`GetBaseType()` chain)
- Lines 927-934 have dead code: first assignment is immediately overwritten. The `IsDtoCandidate` extraction should use only lines 933-934
- `DiagnosticTestHelper.RunGenerator(source)` is the established pattern for verifying generated output -- returns `GeneratedTrees` that can be searched for `Register<T>` calls
- Two call sites for `DiscoverDtoReturnTypes`: line 761 (method return type) and line 791 (constructor containing type)
- Baseline: 2052 tests passing (517 unit x2 frameworks + 509 integration x2 frameworks)

## Mistakes to Avoid

(First run -- no prior mistakes)

## User Corrections

(First run -- no corrections yet)

## Developer Review

**Status:** Approved
**Date:** 2026-03-30

### Summary

The plan extends the source generator's DTO discovery to recursively walk properties of discovered DTO types, finding nested DTOs that need `DtoConstructorRegistry.Register<T>()` calls. The change is scoped entirely to `DiscoverDtoReturnTypes` in `FactoryGenerator.Types.cs` with new helper methods `UnwrapType` and `IsDtoCandidate`.

### Assertion Trace Verification

| # | Business Rule | Implementation Path | Expected Result | Verified? |
|---|--------------|---------------------|-----------------|-----------|
| BR-NEST-001 | WHEN factory returns ParentDto with DTO-eligible property ChildDto, THEN ChildDto in DtoReturnTypes | `DiscoverDtoReturnTypes` -- after confirming ParentDto passes `IsDtoCandidate`, iterate public instance properties via `GetMembers()` + base type walk. For each property type, apply `UnwrapType` (strip nullable/collection) + `IsDtoCandidate`. ChildDto passes all checks -> added to dtoTypes and recursed into. | Both ParentDto and ChildDto in result set | Yes |
| BR-NEST-002 | WHEN ParentDto has `List<ChildDto> Items`, THEN both discovered | `DiscoverDtoReturnTypes` property walking -> `List<ChildDto>` property type -> `UnwrapType` strips collection via `AllInterfaces` IEnumerable check -> extracts `ChildDto` -> `IsDtoCandidate` passes -> added | Both in result set | Yes |
| BR-NEST-003 | WHEN ParentDto has `ChildDto? OptionalChild`, THEN both discovered | Property type `ChildDto?` -> `UnwrapType` strips nullable annotation (NullableAnnotation.Annotated path or System_Nullable_T path for value types) -> `IsDtoCandidate` on unwrapped type passes -> added | Both in result set | Yes |
| BR-NEST-004 | WHEN A->B->C (deep nesting), THEN all three discovered | A passes IsDtoCandidate -> properties walked -> B found -> B passes IsDtoCandidate -> B's properties walked -> C found -> C passes IsDtoCandidate -> C's properties walked (no more nested DTOs). All three added to result set. | A, B, C all in result set | Yes |
| BR-NEST-005 | WHEN A<->B circular reference, THEN both discovered, no infinite recursion | `HashSet<string> visited` tracks fully-qualified type names. A discovered -> added to visited -> walk A's properties -> B found -> B not in visited -> added to visited -> walk B's properties -> A found -> A already in visited -> skip. No infinite loop. | Both in result set, terminates | Yes |
| BR-NEST-006 | WHEN ParentDto has property of `[Factory]` type, THEN excluded | Property type checked by `IsDtoCandidate` -> `GetAttributes()` check for FactoryAttribute + `AllInterfaces` check for [Factory] on interfaces -> fails IsDtoCandidate -> not added | ParentDto only in result set | Yes |
| BR-NEST-007 | WHEN ParentDto has abstract/interface/primitive/System properties, THEN excluded | Each property type checked by `IsDtoCandidate`: abstract -> `IsAbstract` check fails; interface -> `TypeKind == Interface` check fails; primitive -> `SpecialType != None` check fails; System namespace -> `ns.StartsWith("System")` check fails. None added. | ParentDto only in result set | Yes |
| BR-NEST-008 | WHEN UnwrapType and IsDtoCandidate extracted (refactoring), THEN identical output | Pure refactoring: extract existing conditional blocks into named methods without changing logic. Run full test suite after extraction, before adding recursion. Zero behavior change. | All 2052 tests pass after extraction | Yes |
| BR-NEST-009 | WHEN DerivedDto : BaseDto and BaseDto has DTO-eligible property, THEN inherited property discovered | Property walking must use `GetBaseType()` chain (same pattern as `CollectPropertiesRecursive` at line 373-376) because `GetMembers()` only returns declared members. Walk base types, collect public instance properties from each level. BaseDto's ChildDto property found and recursed into. | DerivedDto and ChildDto in result set | Yes |
| BR-NEST-010 | WHEN existing factory returns simple DTO (e.g., ExampleDto with int Id, string Name), THEN unchanged output | ExampleDto's properties are int (SpecialType.System_Int32 -> filtered) and string (SpecialType.System_String -> filtered). No nested DTOs found. DtoReturnTypes contains only ExampleDto as before. | No regression | Yes |

### Gaps and Questions

#### Critical (Must Answer Before Implementation)
None. The plan is thorough and all assertions trace cleanly.

#### Clarifying (Would Help But Not Blocking)

1. **TS-015 (Collection of collections):** The plan explicitly says the behavior is "acceptable either way" and asks the developer to document the chosen behavior. This is fine -- I will document whichever outcome the implementation produces.

2. **Property getter-only requirement:** The plan says "public instance properties with getters" (from architect finding #9). The current `CollectPropertiesRecursive` at line 382-388 also checks for setters, but for DTO property discovery we only need the type -- we don't need the property to be settable. The plan's guidance of "public instance properties with getters" is correct for discovery purposes (we just need to know the type exists as a property). This aligns with the plan.

3. **UnwrapType for properties vs return types:** The plan correctly notes that properties don't need Task unwrapping. The `UnwrapType` helper should be parameterized or split to handle both cases, or just include Task unwrapping harmlessly (properties will never be Task-typed, so the check is a no-op). Either approach works.

### Implementation Concerns

1. **Dead code cleanup (lines 927-934):** The plan and architect both call out the dead code. The `IsDtoCandidate` extraction will naturally clean this up by using only the simplified check (lines 933-934). No concern, just noting it.

2. **Constructor call site (line 791):** `DiscoverDtoReturnTypes` is also called with `constructorSymbol.ContainingType` (for record primary constructors). This call site passes a `[Factory]` type as the input, which will be filtered out by `IsDtoCandidate`. The constructor call site will return empty results as before (the containing type IS a Factory type). No regression concern.

3. **Test approach:** Using `DiagnosticTestHelper.RunGenerator(source)` + checking generated source for `Register<T>` calls is the established pattern (`AssemblyAttributeEmissionTests.cs`). This tests the full pipeline end-to-end without needing to make `DiscoverDtoReturnTypes` internal. This is approach (b) from the plan.

### Ready to Proceed?
[x] Yes, no blocking questions

## Implementation Contract

### Scope
- `src/Generator/FactoryGenerator.Types.cs` -- Extract `UnwrapType` and `IsDtoCandidate` helpers from `DiscoverDtoReturnTypes`, then add recursive property walking with cycle detection and inherited property traversal (modify)
- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/DtoDiscovery/NestedDtoDiscoveryTests.cs` -- New test file covering TS-001 through TS-015 using `DiagnosticTestHelper.RunGenerator` + generated output verification (create)

### Out of Scope (DO NOT MODIFY)
- All renderer files (`ClassFactoryRenderer.cs`, `InterfaceFactoryRenderer.cs`, `StaticFactoryRenderer.cs`)
- `DtoConstructorRegistry.cs`
- `NeatooJsonTypeInfoResolver.cs`
- `TypeInfo` aggregation code (lines 236-245)
- All existing test files
- Design project files (Step 8, documentation)

### Tests to Add
- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/DtoDiscovery/NestedDtoDiscoveryTests.cs`
  - TS-001: Single-level nested DTO
  - TS-002: Collection property unwrapping (List)
  - TS-003: Array property unwrapping
  - TS-004: IReadOnlyList property unwrapping
  - TS-005: Nullable property unwrapping
  - TS-006: Deep nesting (A -> B -> C)
  - TS-007: Circular reference (A <-> B)
  - TS-008: Self-reference (TreeNode)
  - TS-009: [Factory] property excluded
  - TS-010: Mixed property types
  - TS-011: Abstract property type excluded
  - TS-012: Refactoring preserves behavior (verified by full test suite pass after Step 1)
  - TS-013: Inherited property discovery
  - TS-014: No-regression for simple DTOs
  - TS-015: Collection of collections (edge case, document behavior)

### Test Scenario Mapping
| # | Plan Scenario | Test Method | File |
|---|--------------|-------------|------|
| 1 | TS-001 Single-level nested DTO | SingleLevelNestedDto_BothDiscovered | NestedDtoDiscoveryTests.cs |
| 2 | TS-002 List collection unwrapping | CollectionProperty_ListChildDto_BothDiscovered | NestedDtoDiscoveryTests.cs |
| 3 | TS-003 Array unwrapping | CollectionProperty_ArrayChildDto_BothDiscovered | NestedDtoDiscoveryTests.cs |
| 4 | TS-004 IReadOnlyList unwrapping | CollectionProperty_IReadOnlyListChildDto_BothDiscovered | NestedDtoDiscoveryTests.cs |
| 5 | TS-005 Nullable unwrapping | NullableProperty_ChildDto_BothDiscovered | NestedDtoDiscoveryTests.cs |
| 6 | TS-006 Deep nesting | DeepNesting_ThreeLevels_AllDiscovered | NestedDtoDiscoveryTests.cs |
| 7 | TS-007 Circular reference | CircularReference_AB_BothDiscovered | NestedDtoDiscoveryTests.cs |
| 8 | TS-008 Self-reference | SelfReference_TreeNode_Discovered | NestedDtoDiscoveryTests.cs |
| 9 | TS-009 Factory property excluded | FactoryProperty_Excluded | NestedDtoDiscoveryTests.cs |
| 10 | TS-010 Mixed properties | MixedProperties_OnlyEligibleDiscovered | NestedDtoDiscoveryTests.cs |
| 11 | TS-011 Abstract property excluded | AbstractProperty_Excluded | NestedDtoDiscoveryTests.cs |
| 12 | TS-012 Refactoring preserves behavior | (Verified by full test suite pass after Step 1) | N/A |
| 13 | TS-013 Inherited property | InheritedProperty_ChildDiscovered | NestedDtoDiscoveryTests.cs |
| 14 | TS-014 No regression simple DTOs | SimpleDto_NoNestedTypes_OnlyParentDiscovered | NestedDtoDiscoveryTests.cs |
| 15 | TS-015 Collection of collections | CollectionOfCollections_DocumentBehavior | NestedDtoDiscoveryTests.cs |

### Verification Gates
1. After Step 1 (helper extraction): Build solution + run full test suite (2052 tests). Must pass identically -- zero behavior change.
2. After Step 2 (recursive walking): Build solution + run full test suite. Must pass (existing DTOs with no nested types should be unchanged).
3. After Step 3 (new tests): Build solution + run full test suite including new tests. All must pass.

### Stop Conditions
- If extracting helpers changes generator output (Step 1 fails tests), STOP and report
- If any existing test fails after adding recursive walking, STOP and report
- If the `DiagnosticTestHelper.RunGenerator` approach cannot capture `Register<T>` calls (e.g., missing assembly references prevent compilation), STOP and investigate alternative test approaches
