# Save operation and interfaces reference

**Plan #:** 008
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** Draft
**Last Updated:** 2026-10-11
**Plan-review opt-in:** set at Step 2
**Code-review opt-in:** set at Step 2
**Branch:** cut from EXSET at Step 2
**PR:** —

---

## Scope

Migrate the `save-*` and `interfaces-*` regions into Design and re-point `save-operation.md` and `interfaces-reference.md`, deleting the reference-app sources they replace. Fix those pages' findings: validation in hooks and write methods (R-E), stored method-injected services, Save not reassigned, the `IsNew`/`IsDeleted` shape (R-M), the save table's authorized and missing-method outcomes, `IFactoryCore`, and LazyLoad mentions.
