# Design regions the trap rows embed

**Plan #:** 001
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2, AC-3
**Status:** Draft
**Last Updated:** 2026-10-16
**Plan-review opt-in:** —
**Code-review opt-in:** —
**Branch:** —
**PR:** —

---

## Scope

Give `src/Design` the regions five trap rows need and no region holds today: the context a screen binds to (`Contexts/OrderCheckout.cs`, T6, T14, T18), the read model as a whole class (`ReadModels/OrderSummary.cs`, T20), a class-level `[Execute]` with and without `[Remote]` (`FactoryPatterns/ClassFactoryWithExecute.cs`, T19), the root and child operations of one entity side by side (`Entities/OrderLine.cs`, T15), and the server-only `FetchDraft` behind `OpenDraft` (`Aggregates/Order.cs`, T3). Each region is tight around what its row shows, carries a unique prefixed name, and is pinned by a Design test on the real containers; existing regions keep their names. Does NOT embed anything (002–004), rename types, or change behaviour.
