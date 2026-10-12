# Factory operations page

**Plan #:** 007
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

Migrate the `operations-*` and `collection-factory-*` regions into Design and re-point `factory-operations.md`, deleting the reference-app sources they replace. Fix that page's findings: the static `[Create]` create-or-fetch paragraph, commands that write another aggregate's row, `bool`-returning `[Create]`, the cascade and delete wording against R-D, explicit Insert/Update calls and `TryFetch`, parameter ordering, "seven operations", and hand-written generated interfaces.
