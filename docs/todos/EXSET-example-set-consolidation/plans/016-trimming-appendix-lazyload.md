# Trimming, appendix, LazyLoad removal

**Plan #:** 016
**Date:** 2026-10-15
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-4, AC-5
**Status:** Draft
**Last Updated:** 2026-10-15
**Plan-review opt-in:** set at Step 2
**Code-review opt-in:** set at Step 2
**Branch:** cut from EXSET at Step 2
**PR:** —

---

## Scope

Split from EXSET-011 at its Step 2 (2026-10-15).

**Pages:** replace the hand-written C# in `trimming.md` and the appendix pages (`appendix/serialization.md`, `appendix/record-reference-handling.md`) with Design regions or WRONG blocks.

**Findings to fix:**
- C-26: "business rules" framing;
- I-01 and U-12: private setters and `[JsonInclude]` (R-M), the punchlist's "IFactorySaveMeta Preservation" row;
- I-10: the one required `TrimMode` (R-N);
- T-02: missing `partial`;
- T-07: `[Save]`;
- T-09: the `[Event]` remnant;
- T-26: CSLA vocabulary;
- T-27: Neatoo-side converter names;
- `trimming.md`'s `EmployeeDto`, found at the EXSET-015 gate.

**LazyLoad removal (R-K):**
- delete `docs/lazyload.md` and the skill's `references/lazyload.md`, with `LazyLoadSamples.cs`;
- remove LazyLoad from every other doc page, the README and `SKILL.md`;
- record C-22 and T-01.
