# Events, trimming, hierarchy, advanced references

**Plan #:** 004
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-5, AC-9
**Status:** Draft
**Last Updated:** 2026-10-16
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** —
**PR:** —

---

## Scope

Rebuild `factory-events.md` (24 hand-written blocks, including a `[Remote, Create]` that persists), `trimming.md` (the `[DynamicDependency]`/`[JsonInclude]` recipe), `polymorphic-hierarchy.md` (a `PlanRoutingService` that does not compile; Key Rule 4 backwards, #149) and `advanced-patterns.md` so that every C# block is a `src/Design` region or a WRONG block, and the pages say what the generator does: `[Factory]` and `[Remote]` are not inherited by a derived `[Factory]` class the way Rule 4 claims, per `attributes-reference.md#attributes-and-inheritance`. Where a page needs a region Design lacks, add it with its test. Does NOT touch the six core pages (003) or `SKILL.md` (002).
