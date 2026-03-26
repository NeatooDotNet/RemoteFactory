# Fix Missing global:: Namespace Qualifier in Generated Code

**Status:** Complete
**Priority:** High
**Created:** 2026-03-26
**Last Updated:** 2026-03-26

---

## Problem

RemoteFactory 0.24.0 generator bug: the generator emits namespace-qualified type references (e.g., `Person.Ef.PersonEntity`) without the `global::` prefix. When a class name matches a namespace segment (e.g., class `PersonModel` in namespace `Person.DomainModel`, referencing type `Person.Ef.PersonEntity`), C# resolves `Person` as the class rather than the namespace.

The fix is to use `global::` prefixed type references in generated code (e.g., `global::Person.Ef.PersonEntity`).

This only affects the Person example project currently, but could affect any consumer where a class name collides with a namespace segment.

## Solution

Update the Roslyn source generator to emit `global::` prefixed fully-qualified type names wherever it emits namespace-qualified type references in generated code. This is a standard best practice for source generators.

---

## Clarifications

---

## Plans

- [Fix Missing global:: Namespace Qualifier Plan](../../plans/completed/fix-global-namespace-qualifier-plan.md)

---

## Tasks

- [x] Architect questions (Step 2)
- [x] Architect plan (Step 3)
- [x] Developer review (Step 4)
- [x] Implementation (Step 5)
- [x] Architect verification (Step 6)
- [x] Documentation (Step 7) -- No docs needed (internal generator fix)

---

## Progress Log

### 2026-03-26
- Created todo
- Architect created plan: `docs/plans/fix-global-namespace-qualifier-plan.md`
- Developer reviewed and approved plan
- Implementation complete: 8 changes across 6 files (7 planned + 1 custom SymbolDisplayFormat)
- Architect verified: all builds and tests pass (490 unit, 502 integration, 42 design -- both TFMs)
- No documentation needed (internal generator fix)
- Todo complete

---

## Results / Conclusions

Fixed the generator to emit `global::` prefixed fully-qualified type references in all generated code. 7 locations across 5 generator files were updated, plus 3 unit test assertions. One additional refinement: a custom `SymbolDisplayFormat` (`FullyQualifiedFormatWithNullable`) was created to combine `FullyQualifiedFormat` with `IncludeNullableReferenceTypeModifier`, preserving inner nullable annotations in generic types like `List<string?>`.
