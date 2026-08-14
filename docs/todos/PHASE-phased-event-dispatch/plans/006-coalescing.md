# Opt-In Coalescing of Queued Phase Dispatches

**Plan #:** 006
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-14
**Plan-review opt-in:** Yes (public API surface on the attribute; dedup semantics are contract)
**Code-review opt-in:** Yes (behavior-changing)

---

## Scope

Queued per user decision (2026-08-14): implement only if PHASE-001..005 land smoothly.
Add an opt-in flag to `[FactoryEventHandler<T>]` so identical queued `(handler, event)`
pairs (events are records — value equality) collapse to one dispatch when a phase queue
drains, addressing the multiple-recomputes-per-save observation from the motivating
proposal without consumer code. Same-event coalescing only — cross-event coalescing stays
out of scope per the parent todo.

---

*(Stub — Intent, Alignment, Constraints, Steps, Acceptance filled at Step 2.)*
