# Skill-only embeds

**Plan #:** 015
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

Split from EXSET-010 at its Step 2 (2026-10-15). Re-point the skill's remaining reference-app embeds to `src/Design`: `skill-class-factory-complete`, `skill-lifecycle-hooks`, `skill-class-execute` (`class-factory.md`); `skill-interface-factory-complete` and the three `skill-interface-factory-auth-*` (`interface-factory.md`); `skill-entity-duality`, `skill-value-object-factory`, `skill-complex-aggregate` and `clientserver-container-usage` (`advanced-patterns.md`); `skill-blazor-usage` (`setup.md`); `skill-static-execute-commands` (`static-factory.md`). Reuse Design's existing regions where one shows the same shape. Fix the findings those embeds carry: C-11, C-14, C-15, C-19, I-05 and U-02's skill shares. Delete the reference-app `Samples/Skill` sources whose every region retires. The skill's prose around the embeds is Todo 2's.
