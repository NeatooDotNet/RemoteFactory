# Person example drops the private-setter recipe

**Plan #:** 005
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-10
**Status:** Draft
**Last Updated:** 2026-10-16
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** —
**PR:** —

---

## Scope

`src/Examples/Person/Person.DomainModel/PersonModel.cs` drops the private-setter, `[JsonInclude]` and `[DynamicDependency]` recipe and its "visibility narrowing" comments, so the example application matches what `trimming.md` prescribes after 004: public setters and the generated ordinal path (#150). The Person example stays an application, not a snippet source. Does NOT touch the skill or `src/Design`.
