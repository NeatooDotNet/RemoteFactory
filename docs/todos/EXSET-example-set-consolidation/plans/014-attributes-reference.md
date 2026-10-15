# Attributes reference

**Plan #:** 014
**Date:** 2026-10-15
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** In Progress
**Last Updated:** 2026-10-15
**Plan-review opt-in:** No. Nearly every embed re-points to a region 002–009 built and reviewed. Design gains four regions and no new shape.
**Code-review opt-in:** Yes. The page is restated against the generator, and the inheritance claims change.
**Branch:** EXSET-014-attributes-reference
**PR:** —

---

## Scope

Split from EXSET-009 (Keith, 2026-10-15). Re-point every C# block on `docs/attributes-reference.md` to `src/Design` or turn it into prose. The page has 16 embeds and 4 hand-written fences. The skill embeds that share its regions are re-pointed too:
- `attributes-suppressfactory` in `class-factory.md`;
- `attributes-execute-local` in `static-factory.md`;
- `attributes-factoryhintnamelength` and `attributes-inheritance` in `advanced-patterns.md`.

**Reused regions:**
- `operations-create-method`, `-create-constructor`, `-fetch-instance`, `-insert`, `-insert-update` and `-execute`;
- `aspnetcore-cancellation`;
- `authorization-apply`, `-interface` and `-policy-multiple`.

**New Design regions:**
- `attributes-factory`, on CustomerPreferences' class;
- `attributes-factory-interface`, on ICustomerDirectory;
- `attributes-execute-local`, on ExampleCommands' bare `[Execute]`;
- `attributes-factoryeventhandler`, on a phased handler;
- `attributes-factoryevent`, on an event record.

**Becomes prose:**
- `[SuppressFactory]`;
- `[Remote]`'s contrast, which the reused regions already show;
- combined flags, shown by `authorization-interface`;
- `[assembly: FactoryHintNameLength]`;
- inheritance;
- `FactoryEventBase`'s declaration, which is library code.

The skill's three embeds whose regions retire become one prose line each. The rest of the skill's prose is Todo 2's.

**Findings to fix:**
- C-16: `[SuppressFactory]` as "the shape to copy";
- I-03: the `[Execute]` return type;
- I-14: the duplicate `attributes-factoryhintnamelength` key;
- this page's shares of C-19, C-27, I-01, I-02 and T-22.

**Found at recon (probe compile, 2026-10-15).** The generator matches only types that declare `[Factory]` themselves (`ForAttributeWithMetadataName`). So:
- a derived class gets no factory unless it carries `[Factory]` too, although `FactoryAttribute` is declared `Inherited = true`;
- `[SuppressFactory]` has an effect only beside a `[Factory]` on the same declaration.

The page's "Inherited: Yes; derived classes get their own factory" is false. The page will state the behaviour, and a techdebt issue will record the attribute's declaration that misleads.

Delete `MinimalAttributesSamples.cs` and `AssemblyAttributeSamples.cs`, and remove `AssemblyAttributes.cs`'s region markers. That closes I-14. The attribute itself stays: the reference app compiles with it.

Does NOT touch:
- `factory-events.md` (EXSET-011);
- the skill's prose beyond the three replaced embeds (Todo 2).

---

## Intent

The attributes reference states what each attribute does as the generator applies it. Each example is Design code already pinned by tests, and the inheritance rule is true.

---

## Framework & Architectural Alignment

- **Rulings:** D3, D11, D12, D14, R-E, R-K and R-M; I-12's prose precedent.
- **`.claude/rules/design-snippets.md`.**
- **The 007, 008 and 009 pages,** which this page links rather than restates.

---

## Constraints & Invariants

- **Embeds.** Every C# block on the page is an embed from `src/Design` or a short WRONG block.
- **Regions.** Each region name is defined once. A region retiring from the skill leaves no bare marker.
- **The whole still works:** both solutions green, mdsnippets swallows nothing, the reference app builds.

---

## Steps

1. The five new Design regions.
2. Rewrite the page: embeds, the quick-lookup table, inheritance, and the events attributes.
3. Replace the skill's three retiring embeds with prose.
4. Delete the reference-app sources, then build `EmployeeManagement.sln`.
5. File the techdebt issue on `[Factory]`'s `Inherited = true`.
6. Verify and record: mdsnippets, both solutions, the ledger.

---

## Acceptance

- [ ] Every C# block on the page is a Design embed or WRONG. No region is defined twice, and none retired is embedded, page or skill. The reference-app sources are gone, and the app builds. `[explicit-skip: greps; EmployeeManagement.sln build]` · Must
- [ ] The page's inheritance and `[SuppressFactory]` statements match the generator `[explicit-skip: probe compile and FactoryGenerator.cs:19-23, cited]` · Must
- [ ] The embedded regions are pinned by existing Design tests `[explicit-skip: each region's test named in Test Evidence]` · Must
- [ ] mdsnippets clean; both solutions green `[explicit-skip: meta]` · Must

---

## Current State (Pre-Flight)

- **Design has none of these:**
  - `[SuppressFactory]`;
  - `FactoryHintNameLength`;
  - an inheritance example.
- **Design does have the event shapes:** `FactoryEventHandlerPattern.cs` and `FactoryEventPhasesPattern.cs` hold the handlers and event records.
- **The probe compile:**
  - Only `ZzBase` (`[Factory]`) and `ZzDerivedOwn` (`[Factory]` on the derived class) got factories.
  - `ZzDerived` (inherits only) got none.
  - `ZzSuppressedDerived` got none.
  - `ZzBoth` (`[Factory][SuppressFactory]`) got none.

---

## Punchlist

---

## Test Evidence

---

## Gate Record

---

## Plan Amendments

---

## Notes

- **Run unattended** under Keith's standing instruction. The defaults follow 007–009: library and assembly-level code becomes prose, and a behaviour that contradicts the old page is stated as it is and filed.
