# Person example drops the private-setter recipe

**Plan #:** 005
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-10
**Status:** Done
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (an example application; no library, no public API)
**Code-review opt-in:** Yes (it changes how the example's model serializes, and the example has no tests)
**Branch:** RFSK-005-person-example-recipe
**PR:** —

---

## Scope

`src/Examples/Person/Person.DomainModel/PersonModel.cs` drops the private-setter, `[JsonInclude]`, `[DynamicDependency]` and `[DynamicallyAccessedMembers]` recipe and its "visibility narrowing" comments, so the example application matches what `trimming.md` now prescribes: public `get; set;` save flags (R-M) and a `partial` class that gets the generated ordinal serializer. The Person example stays an application, not a snippet source. Does NOT touch the skill, `src/Design`, or the example's other projects.

---

## Intent

- A reader who follows `docs/trimming.md`'s link to the Person example finds the same shape the docs and the skill prescribe, not the recipe they retired (#150).
- The example's save keeps routing: `IsNew` and `IsDeleted` still cross the wire, now through the generated serializer.

---

## Framework & Architectural Alignment

- R-M (`IsNew`/`IsDeleted` public `get; set;`), `trimming.md` "`IFactorySaveMeta` on a trimmed client" (RFSK-004), `anti-patterns.md` #12 and #13 (RFSK-003).
- `IFactorySaveMeta` declares both flags getter-only, so callers through `IPersonModel` still cannot set them; `MarkDeleted` stays the domain's way to delete.

---

## Constraints & Invariants

- The model's operations, authorization and events are unchanged.
- The main solution, which holds the Person projects, builds with no new warning, and both solutions' tests stay green.

---

## Steps

1. Make the save flags public `get; set;` and drop the serializer and trimmer annotations and the comments that justify them.
2. Mark the class `partial`, so it gets the ordinal serializer.
3. Remove the usings that only the annotations needed; keep `MarkDeleted` and its interface comment, worded for the getter-only interface.
4. Build and test both solutions.

---

## Acceptance

- [ ] `PersonModel` has no `[JsonInclude]`, `[DynamicDependency]`, `[DynamicallyAccessedMembers]` or private setter, and is `partial` `[explicit-skip: grep; the example has no test project]`
- [ ] The generator emits an ordinal serializer for `PersonModel` that reads and writes `IsNew` and `IsDeleted` `[explicit-skip: inspect the generated file; no test project]`
- [ ] Both solutions build with no new warning and their tests stay green `[explicit-skip: meta-bullet]`

---

## Current State (Pre-Flight)

Walked 2026-10-16 on the arc at `6d503ad`.

- `PersonModel.cs` (154 lines, tab-indented): `internal class PersonModel` (not `partial`) with `[DynamicallyAccessedMembers(PublicProperties | PublicParameterlessConstructor)]` on the class (37), `[DynamicDependency(nameof(IsNew))]` and `(nameof(IsDeleted))` on the `[Create]` constructor (46-47), `[JsonInclude] public bool IsDeleted { get; private set; }` (68-69) and `IsNew` (71-72), and comments at 32-36, 40-45 and 65-67 explaining the recipe. `MarkDeleted()` sets `IsDeleted`; `Fetch` sets `IsNew = false`.
- `IFactorySaveMeta` declares `bool IsDeleted { get; }` and `bool IsNew { get; }` (`src/RemoteFactory/IFactorySaveMeta.cs:22, 27`).
- Callers: `Person.Client/Pages/Home.razor:194` calls `MarkDeleted()`. No test project covers Person; it builds in `src/Neatoo.RemoteFactory.sln`.
- Not `partial` today, so the class has no ordinal converter and its registrar calls `DtoConstructorRegistry.PreserveProperties<T>()` (EXSET-016); with `partial` and a parameterless constructor it gets the converter.

---

## Punchlist

- 

---

## Test Evidence

The Person example has no test project; the plan's bullets are checks on the source and on the generator's output, not on behaviour a test runs.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| No recipe left; `partial` | Optional | `[explicit-skip]` | grep of `PersonModel.cs`: no `DynamicallyAccessed`, `DynamicDependency`, `JsonInclude`, `private set`; `internal partial class PersonModel`; the two usings only the annotations needed are removed | ✓ |
| Ordinal serializer carries both flags | Optional | `[explicit-skip]` | `Person.DomainModel/Generated/.../Person.DomainModel.PersonModel.Ordinal.g.cs` (gitignored): reads `IsDeleted`/`IsNew` at positions 3 and 4 and writes both | ✓ |
| Both solutions green, no new warning | Optional | `[explicit-skip]` | `reviews/005-*.log`: main build's only warnings are the two WASM `NativeFileReference` notices; unit 871, integration 664 (5 skipped), Design 180 (1 skipped), both TFMs | ✓ |

---

## Gate Record

- Round 1 (2026-10-16): test review APPROVED, no findings — `reviews/005-test-review.md`. Code review APPROVED, two theoretical items taken (an unused using removed; the comment names the ordinal format) — `reviews/005-code-review.md`
- No round 2: no veto — Done

---

## Plan Amendments

---

## Notes

- Loaded: `trimming.md` and `anti-patterns.md` as rebuilt, VisionAlignment R-M.
