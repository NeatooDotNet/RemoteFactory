# Attributes reference

**Plan #:** 014
**Date:** 2026-10-15
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** Draft
**Last Updated:** 2026-10-15
**Plan-review opt-in:** set at Step 2
**Code-review opt-in:** set at Step 2
**Branch:** cut from EXSET at Step 2
**PR:** —

---

## Scope

Split from EXSET-009 (Keith, 2026-10-15). Re-point `docs/attributes-reference.md` and the skill embeds that share its regions to `src/Design`, and delete the reference-app sources:
- the page has 16 embeds and 4 hand-written fences;
- the skill embeds are `attributes-suppressfactory` (`class-factory.md`), `attributes-execute-local` (`static-factory.md`), and `attributes-factoryhintnamelength` and `attributes-inheritance` (`advanced-patterns.md`);
- the reference-app sources are `MinimalAttributesSamples.cs`, `AssemblyAttributes.cs` and `AssemblyAttributeSamples.cs`.

Fix that page's findings:
- C-16: `[SuppressFactory]` as "the shape to copy";
- I-03: the `[Execute]` return type;
- I-14: the duplicate `attributes-factoryhintnamelength` key;
- the page's shares of C-19, C-27, I-01, I-02 and T-22.
