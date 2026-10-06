# Walker covers dictionary entries and public fields

**Plan #:** 002
**Date:** 2026-09-10
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-2, AC-6
**Status:** Draft
**Last Updated:** 2026-10-05
**Plan-review opt-in:** No — one generator seam, additive, no public API or documented-rule change.
**Code-review opt-in:** Yes — behavior-changing generator emission.
**Branch:** dict-002-dictionary-type-argument-walk — cut from the arc at Step 2
**PR:** —

---

## Scope

Brings the DTO walker up to the two shapes this todo covers. A `KeyValuePair<K,V>` element yields both its type arguments, so every pair-enumerating dictionary shape — `Dictionary`, `IDictionary`, `IReadOnlyDictionary`, sorted, concurrent, immutable, consumer-defined — contributes its key and value types, recursing through a value that is itself a collection or nullable so `Dictionary<string, List<Dto>>` reaches `Dto`. Public instance fields are walked alongside properties, because the serializer runs with `IncludeFields` on. Both shapes flow through the same constructor-shape bucket sort list elements get, through all three callers: factory signatures, `[Factory]` entity properties, and event-record graphs. Generator unit tests pin each shape per caller and per bucket, including dictionary keys, and the DICT-001 harness cases turn green. It does not walk tuple elements, emits no diagnostic, and does not change the runtime, `DtoConstructorRegistry`, or the harness beyond what DICT-001 added.

---

## Intent

(Filled at Step 2.)

---

## Framework & Architectural Alignment

(Filled at Step 2.)

---

## Constraints & Invariants

(Filled at Step 2.)

---

## Steps

(Filled at Step 2.)

---

## Acceptance

(Filled at Step 2.)

---

## Current State (Pre-Flight)

(Filled at Step 3.)

---

## Punchlist

- (none)

---

## Test Evidence

(Filled before the Step 5 gate.)

---

## Gate Record

(Filled at Step 5.)

---

## Plan Amendments

### 2026-09-11 — Widened from dictionaries to the shape set

- **Section affected:** Scope, Serves, title
- **Original said:** Dictionary keys and values, with nested wrappers recursing.
- **What changed:** Adds public fields and tuple elements.
- **Why:** User decision 2026-09-11: finish the walker once instead of returning to it per consumer failure.
- **Discovery Log:** 2026-09-11 / DICT

### 2026-10-05 — Narrowed to dictionary entries and public fields

- **Section affected:** Scope, Serves, title
- **Original said:** Dictionaries, recursive wrappers, public fields, and tuple elements.
- **What changed:** Tuple elements dropped. Recursion stays, scoped to reaching a dictionary value that is itself a collection or nullable.
- **Why:** User narrowed the todo 2026-10-05.
- **Discovery Log:** 2026-10-05 / DICT

---

## Notes

- The seam is `DtoTypeWalker.UnwrapType` plus the member walk, shared by all three callers, so one change covers signatures, entity properties, and event graphs; the unit tests exist to prove that claim per caller rather than assume it.
- Fields are in scope because `NeatooJsonSerializer` sets `IncludeFields = true`; if that option is ever turned off, the field walk becomes dead weight and should go with it.
- Side effect, per a read of `UnwrapType`: a nested list such as `List<List<Dto>>` is missed today for the same reason, because the inner list is rejected as a System type. Recursive unwrapping fixes it too. Pin it with one unit test; it is not a criterion.
