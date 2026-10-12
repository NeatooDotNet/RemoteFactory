# Events, trimming, LazyLoad pages, appendix

**Plan #:** 011
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-5
**Status:** Draft
**Last Updated:** 2026-10-11
**Plan-review opt-in:** set at Step 2
**Code-review opt-in:** set at Step 2
**Branch:** cut from EXSET at Step 2
**PR:** —

---

## Scope

Replace the hand-written C# in `factory-events.md`, `events.md`, `trimming.md` and the appendix pages with Design regions or WRONG-labelled blocks, fixing their findings: events raised inside `[Create]` (C-06), instance handlers, stale registration names, "business rules" and the one required `TrimMode` (R-N), missing `partial`, CSLA vocabulary, and Neatoo-side converter names. Delete `docs/lazyload.md` and the skill's `references/lazyload.md`, and remove LazyLoad from every other doc page and the README (R-K).
