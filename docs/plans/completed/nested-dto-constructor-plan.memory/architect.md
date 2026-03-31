# Architect -- Nested DTO Constructor Discovery

Last updated: 2026-03-30
Current step: Step 7A complete -- Post-Implementation Verification VERIFIED

## Key Context

### Codebase Dive Findings

1. **`DiscoverDtoReturnTypes` location:** `src/Generator/FactoryGenerator.Types.cs`, lines 817-945. Private static method on `TypeFactoryMethodInfo`. Returns `EquatableArray<string>` of fully-qualified type names.

2. **Aggregation pipeline:** Per-method `DtoReturnTypes` -> `TypeInfo` constructor (lines 236-245) aggregates via `HashSet<string>` -> `TypeInfo.DtoReturnTypes` -> renderers iterate to emit `Register<T>()` calls. No changes needed outside `DiscoverDtoReturnTypes`.

3. **Renderer pattern is identical across all three factory types:**
   - `ClassFactoryRenderer.cs` line 1527-1535
   - `InterfaceFactoryRenderer.cs` line 479-488
   - `StaticFactoryRenderer.cs` line 147-156
   All iterate `model.DtoReturnTypes` and emit the same `DtoConstructorRegistry.Register<{dtoType}>(() => new {dtoType}());` line. No renderer changes needed.

4. **`GetMembers()` does NOT include inherited members.** Confirmed via Roslyn documentation and by examining the codebase pattern: `CollectPropertiesRecursive` (line 373-376) explicitly walks `GetBaseType()`. The plan's property walker must do the same for BR-NEST-009.

5. **Dead code in current constructor check:** Lines 927-934 have a confused first assignment that is immediately overwritten by a simplified version. The extraction into `IsDtoCandidate` should clean this up.

6. **No existing tests for DTO discovery.** Zero unit or integration tests for `DiscoverDtoReturnTypes` or `DtoConstructorRegistry` registration. The Requirements Review correctly identified this gap. The plan adds comprehensive tests.

7. **Existing `NestedDto` test targets** exist at `src/Tests/RemoteFactory.IntegrationTests/TestTargets/Parameters/RemoteComplexParameterTargets.cs` and `src/Tests/RemoteFactory.UnitTests/TestTargets/Parameters/ComplexParameterTargets.cs` -- but these DTOs are used as **parameters**, not **return types**. New test targets will be needed.

8. **Design project `ExampleDto`** (`AllPatterns.cs:493-497`) has only primitive properties (`int Id`, `string Name`). It exercises the existing discovery path but does not exercise nested discovery. After implementation, a nested DTO example should be added to the Design project.

9. **DtoConstructorRegistry is idempotent** (`ConcurrentDictionary.TryAdd` at line 24). Duplicate registrations from overlapping discoveries are harmless.

10. **`NeatooJsonTypeInfoResolver`** (lines 33-38) falls through to the DTO registry only if the type is not in DI (`IsService` check first). No changes needed.

### Design Validation

- The plan correctly identifies that changes are contained entirely within `DiscoverDtoReturnTypes`
- The approach of extracting helpers before adding recursion is sound and follows the reviewer's recommendation
- The cycle detection approach (HashSet of fully qualified names) is correct and matches the existing deduplication pattern
- Collection/nullable unwrapping reuse is correct -- properties don't need Task unwrapping
- Over-discovery is explicitly acceptable per both the plan and the reviewer

### Scope Verification

| Component | Change needed? | Verified |
|-----------|---------------|----------|
| `DiscoverDtoReturnTypes` | Yes -- extract helpers, add recursion | Yes |
| `TypeInfo` constructor aggregation | No | Yes -- HashSet dedup handles additional types |
| `ClassFactoryRenderer` | No | Yes -- just iterates DtoReturnTypes |
| `InterfaceFactoryRenderer` | No | Yes -- same pattern |
| `StaticFactoryRenderer` | No | Yes -- same pattern |
| `DtoConstructorRegistry` | No | Yes -- idempotent registration |
| `NeatooJsonTypeInfoResolver` | No | Yes -- lookup only |
| `EquatableArray<string>` | No | Yes -- value equality unaffected by additional items |

### Critical Implementation Notes for Developer

1. **Inherited properties:** `GetMembers()` returns only declared members. Must walk base type chain for inherited property discovery (BR-NEST-009). Pattern exists at line 373-376 of same file.

2. **Constructor check cleanup:** Lines 927-934 have dead code. Extract the simplified version (line 933-934) into `IsDtoCandidate`.

3. **Test strategy:** `DiscoverDtoReturnTypes` is `private static`. Testing options: (a) `[InternalsVisibleTo]`, (b) test via generated output, (c) integration tests with real `[Factory]` types. Option (b) or (c) preferred.

4. **Property accessibility filter:** Only walk public instance properties with getters (matching the property walking for serialization). No need to walk private, protected, or static properties -- the serializer wouldn't see them anyway.

## Mistakes to Avoid

None -- no corrections were needed during this plan's lifecycle.

## User Corrections

None.

## Architectural Verification (Pre-Handoff)

**Verdict: APPROVED**

The plan is sound, well-scoped, and resolves a documented design debt item with production evidence. Key validations:

- [x] Generator Constraints: All APIs used are available in netstandard2.0
- [x] Equatability: No new pipeline types -- uses existing `EquatableArray<string>`
- [x] Serialization: No serialization changes needed
- [x] Testing: New unit/integration tests cover the recursive walking scenarios
- [x] Multi-Target: Generator is netstandard2.0, generated code uses no framework-specific APIs
- [x] Backward Compatibility: Existing DTO return types continue to be discovered identically
- [x] API Ergonomics: No consumer-facing API changes -- purely generator-internal improvement
- [x] Documentation: Four documentation locations identified for post-implementation updates

**Breaking changes:** None. This is purely additive -- types that were not discovered before will now be discovered. Over-discovery (registering types that are never deserialized) is harmless.

**One architectural note added to plan:** The `GetMembers()` / inherited property issue (BR-NEST-009) was not explicitly covered in the user's original design. I added it as a business rule and test scenario because omitting it would leave a gap for DTOs using inheritance (common pattern: `BaseDto` with shared properties).

## Architect Verification (Post-Implementation)

**Verdict: VERIFIED**

### Independent Build Results

- **Build:** 0 errors, 3 warnings (all pre-existing WASM sqlite warnings from `OrderEntry.BlazorClient`)
- **Tests:** 2082 passed, 0 failed, 6 skipped (pre-existing)
  - UnitTests net9.0: 532 passed
  - UnitTests net10.0: 532 passed
  - IntegrationTests net9.0: 509 passed, 3 skipped
  - IntegrationTests net10.0: 509 passed, 3 skipped

### Scope Compliance

- **Only 1 source file modified:** `src/Generator/FactoryGenerator.Types.cs` (131 insertions, 66 deletions)
- **2 new test files created:** `NestedDtoDiscoveryTests.cs` (14 test methods), `NestedDtoFailureTest.cs` (1 regression test)
- **No out-of-scope changes:** No renderer, registry, resolver, or existing test modifications

### Test Scenario Coverage: 15 of 15 verified with passing tests

| # | Plan Scenario | Test Method | Verified |
|---|---|---|---|
| TS-001 | Single-level nested DTO | `SingleLevelNestedDto_BothDiscovered` | PASS |
| TS-002 | List collection unwrapping | `CollectionProperty_ListChildDto_BothDiscovered` | PASS |
| TS-003 | Array unwrapping | `CollectionProperty_ArrayChildDto_BothDiscovered` | PASS |
| TS-004 | IReadOnlyList unwrapping | `CollectionProperty_IReadOnlyListChildDto_BothDiscovered` | PASS |
| TS-005 | Nullable unwrapping | `NullableProperty_ChildDto_BothDiscovered` | PASS |
| TS-006 | Deep nesting (3 levels) | `DeepNesting_ThreeLevels_AllDiscovered` | PASS |
| TS-007 | Circular reference | `CircularReference_AB_BothDiscovered` | PASS |
| TS-008 | Self-reference | `SelfReference_TreeNode_Discovered` | PASS |
| TS-009 | Factory property excluded | `FactoryProperty_Excluded` | PASS |
| TS-010 | Mixed properties | `MixedProperties_OnlyEligibleDiscovered` | PASS |
| TS-011 | Abstract property excluded | `AbstractProperty_Excluded` | PASS |
| TS-012 | Refactoring preserves behavior | Full suite (2082 pass) | PASS |
| TS-013 | Inherited property discovery | `InheritedProperty_ChildDiscovered` | PASS |
| TS-014 | No regression simple DTOs | `SimpleDto_NoNestedTypes_OnlyParentDiscovered` | PASS |
| TS-015 | Collection of collections | `CollectionOfCollections_DocumentBehavior` | PASS |

### Design Match Verification

1. **Three helper methods extracted as planned:** `DiscoverDtoTypesRecursive`, `UnwrapType`, `IsDtoCandidate` -- matches plan's refactoring approach
2. **Cycle detection:** `HashSet<string>` with `visited.Add()` returning false to short-circuit -- matches plan
3. **Inherited property traversal:** `GetBaseType()` chain with `while` loop stopping at `System.Object` -- addresses BR-NEST-009
4. **Property filtering:** Public, non-static, non-indexer, has getter -- correct for serialization visibility
5. **Task unwrapping parameterized:** `unwrapTask` bool parameter on `UnwrapType` -- properties skip Task unwrapping
6. **Dead code cleanup:** Confused constructor check removed, replaced by clean single check in `IsDtoCandidate`
7. **Same eligibility criteria** reused for both return types and property types -- no new rules introduced

### Regression Test

`NestedDtoFailureTest.NestedDto_ShouldBeRegistered_ButIsNot` -- developer reports (and full suite confirms) this test fails without the changes and passes with them, proving the gap was real and is now closed.

### Additional Notes

- Test approach used option (b) from the plan -- testing via generated output using `DiagnosticTestHelper.RunGenerator()` with regex matching of `DtoConstructorRegistry.Register<T>` calls. This is practical and validates the full pipeline from attribute detection through code generation.
- The `TS-015` (collection of collections) test documents the deliberate behavior: nested `List<List<ChildDto>>` does NOT discover `ChildDto` because the inner `List<ChildDto>` is a System type. This is acceptable (over-discovery preferred, but this edge case is documented).
