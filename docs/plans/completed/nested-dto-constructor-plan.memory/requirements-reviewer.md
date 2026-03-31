# Requirements Reviewer -- Nested DTO Constructor Discovery

Last updated: 2026-03-30
Current step: Post-implementation verification (Step 7B) -- complete

## Key Context

This plan resolves a documented Design Debt item (CLAUDE-DESIGN.md line 770): "Nested DTO discovery for trimming." The reconsideration condition was met by a production failure in zTreatment. The implementation extends the generator's `DiscoverDtoReturnTypes` to recursively walk public instance properties of discovered DTO types, finding nested types that also need `DtoConstructorRegistry.Register<T>()` calls.

The change is entirely contained within `FactoryGenerator.Types.cs` -- one method refactored into three, plus recursive property walking added. No renderers, models, registry, or pipeline changes.

## Mistakes to Avoid

None encountered on this run.

## User Corrections

None.

## Requirements Verification

**Verdict:** REQUIREMENTS SATISFIED
**Date:** 2026-03-30

### Compliance Table

| # | Requirement | Source | Status | Notes |
|---|------------|--------|--------|-------|
| 1 | DTO discovery criteria reused identically for nested DTOs (public parameterless ctor, not [Factory], not System, not abstract/interface) | CLAUDE-DESIGN.md lines 584-593 | Satisfied | `IsDtoCandidate` extracted into a shared helper; same checks applied to both return types and property types. Verified at FactoryGenerator.Types.cs lines 962-1010. |
| 2 | Generator unwraps Task<T>, nullable, collection wrappers | CLAUDE-DESIGN.md line 594 | Satisfied | `UnwrapType` extracted as shared helper with `unwrapTask` parameter. Return types get full unwrapping; property types skip Task unwrapping (correct -- properties are not async). Verified at lines 888-955. |
| 3 | Duplicate registrations are idempotent (ConcurrentDictionary.TryAdd) | CLAUDE-DESIGN.md line 596; DtoConstructorRegistry.cs line 24 | Satisfied | `DtoConstructorRegistry.cs` is untouched. `TryAdd` continues to handle duplicates. Recursive discovery may produce overlapping registrations from multiple factory methods -- this is safe. |
| 4 | Design Debt entry condition met | CLAUDE-DESIGN.md line 770 | Satisfied | The plan documents the production failure in zTreatment that met the "Reconsider When" condition. The implementation resolves the debt. Documentation update is Step 8 (not in scope for Step 7B). |
| 5 | Known limitation resolved | CLAUDE-DESIGN.md line 598; docs/trimming.md lines 260-263 | Satisfied | The implementation adds recursive property walking. Documentation still references the old limitation -- this is expected; doc updates are Step 8. |
| 6 | netstandard2.0 compatibility | Generator project constraint | Satisfied | Uses only standard Roslyn APIs (`INamedTypeSymbol`, `IPropertySymbol`, `GetMembers()`, `BaseType`), `HashSet<string>`, `List<string>`, and C# 7+ pattern matching. All available in netstandard2.0. |
| 7 | Renderers untouched -- just iterate DtoReturnTypes | Plan "What does NOT change" | Satisfied | Verified via Grep: `ClassFactoryRenderer`, `InterfaceFactoryRenderer`, `StaticFactoryRenderer` all reference `DtoReturnTypes` in iteration loops only. No changes to renderer files. |
| 8 | Aggregation pipeline untouched (TypeInfo deduplication) | Plan "What does NOT change"; FactoryGenerator.Types.cs lines 236-245 | Satisfied | The `HashSet<string>` aggregation at TypeInfo level is unchanged. It continues to deduplicate across factory methods. |
| 9 | Inherited properties discovered via GetBaseType() chain | Plan BR-NEST-009; existing pattern at CollectPropertiesRecursive lines 373-376 | Satisfied | Implementation uses `while (currentTypeForProperties != null && SpecialType != System_Object)` loop with `currentTypeForProperties = currentTypeForProperties.BaseType` at lines 860-881. Follows the established `CollectPropertiesRecursive` pattern. |
| 10 | Cycle detection prevents infinite recursion | Plan BR-NEST-005 | Satisfied | `HashSet<string>` of fully qualified type names at line 820. `visited.Add()` returns false for duplicates, causing early return at lines 852-854. Handles self-references, direct cycles, and indirect cycles. |
| 11 | [Remote] is only for aggregate root entry points | CLAUDE-DESIGN.md Key Rule 1 | Not affected | This change modifies DTO discovery in the generator, not [Remote] behavior. No impact. |
| 12 | Serialization contracts unchanged | CLAUDE-DESIGN.md serialization section | Satisfied | `NeatooJsonTypeInfoResolver` is untouched. `DtoConstructorRegistry` is untouched. The only change is that more types may be registered -- strictly additive, no behavioral change to existing types. |
| 13 | Incremental generator caching | Risk #3 in plan | Satisfied | `EquatableArray<string>` for `DtoReturnTypes` already handles equality comparison. More strings in the array is fine -- the caching mechanism compares arrays by content. |
| 14 | All existing tests pass (2082 passed, 0 failed) | Developer evidence | Satisfied | No regressions. |

### Test Coverage of Business Rules

| Business Rule | Test Scenario | Test Method | Status |
|---|---|---|---|
| BR-NEST-001 (single-level nested) | TS-001 | `SingleLevelNestedDto_BothDiscovered` | Covered |
| BR-NEST-002 (collection unwrapping) | TS-002, TS-003, TS-004 | `CollectionProperty_ListChildDto_BothDiscovered`, `CollectionProperty_ArrayChildDto_BothDiscovered`, `CollectionProperty_IReadOnlyListChildDto_BothDiscovered` | Covered |
| BR-NEST-003 (nullable unwrapping) | TS-005 | `NullableProperty_ChildDto_BothDiscovered` | Covered |
| BR-NEST-004 (deep nesting) | TS-006 | `DeepNesting_ThreeLevels_AllDiscovered` | Covered |
| BR-NEST-005 (circular reference) | TS-007, TS-008 | `CircularReference_AB_BothDiscovered`, `SelfReference_TreeNode_Discovered` | Covered |
| BR-NEST-006 ([Factory] excluded) | TS-009 | `FactoryProperty_Excluded` | Covered |
| BR-NEST-007 (ineligible types excluded) | TS-010, TS-011 | `MixedProperties_OnlyEligibleDiscovered`, `AbstractProperty_Excluded` | Covered |
| BR-NEST-008 (refactoring safety) | TS-012 | Verified by 2082 existing tests passing with 0 failures | Covered |
| BR-NEST-009 (inherited properties) | TS-013 | `InheritedProperty_ChildDiscovered` | Covered |
| BR-NEST-010 (regression guard) | TS-014 | `SimpleDto_NoNestedTypes_OnlyParentDiscovered` | Covered |
| Edge case (collection of collections) | TS-015 | `CollectionOfCollections_DocumentBehavior` | Covered (documented: inner ChildDto not discovered through nested collections) |

### Unintended Side Effects

**Over-discovery (benign):** The recursive property walking may register more types than strictly needed for deserialization. For example, if a DTO has a property of type `AddressDto` that is never actually populated in a particular factory method's response, `AddressDto` is still registered. This is explicitly acknowledged in the plan's Risk #2 as harmless -- unused registrations are a small static cost and are strictly better than under-discovery.

**Generator performance (minimal):** Each type is inspected at most once due to the visited set. The System namespace check is an early exit for the vast majority of properties (primitives, strings, DateTime, etc.). The additional compile-time cost is negligible.

**No unintended side effects detected.** The change is strictly additive: more types in `DtoReturnTypes`, same pipeline, same renderers, same registry. No existing behavior is altered.

### Issues Found

None.

### Documentation Debt (Expected -- Step 8)

Four documentation locations need updating in Step 8 (not a violation -- this is the planned sequence):
1. `src/Design/CLAUDE-DESIGN.md` line 598: "Known limitation" paragraph -- now resolved
2. `src/Design/CLAUDE-DESIGN.md` line 770: Design Debt table entry -- condition met, feature implemented
3. `src/Design/CLAUDE-DESIGN.md` line 157: Quick Decisions Table entry about nested DTOs
4. `docs/trimming.md` lines 260-263: "Nested DTOs are not automatically discovered" paragraph
