# Trimming gate: one red-first case per added shape

**Plan #:** 001
**Date:** 2026-09-10
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-6
**Status:** Draft
**Last Updated:** 2026-10-05
**Plan-review opt-in:** No — harness-only; no generator, runtime, or documented-rule change.
**Code-review opt-in:** No — test-only.
**Branch:** dict-001-gate-dictionary-carried-dto — cut from the arc at Step 2
**PR:** —

---

## Scope

Adds to the publish-trimmed harness one consumer-shaped case per shape this todo teaches the walk: a `[Factory]` entity carrying a `Dictionary<string, Dto>` property whose value type has an implicit parameterless constructor and whose only construction site is inside the entity's async `[Remote]` operation body — the zTreatment `BodyAssessmentInfo.Locations` shape — and a DTO reachable only as a public field of a walked type. Each case's DTO is otherwise unconstructed in client-reachable code. The harness deserializes each from a JSON literal, and the CI gate names each new type as a positive control so a case that stops generating cannot pass by absence. Both cases are run against the current generator first and their red results recorded in `reviews/001-evidence/` before any fix exists; a case that cannot be observed red does not count. Dictionary keys get no harness case, because System.Text.Json cannot deserialize a DTO-typed key without a converter; DICT-002 pins them with a generator test. It does not change the generator, the runtime, or the registry.

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

### 2026-09-11 — Widened to every added shape

- **Section affected:** Scope, Serves, title
- **Original said:** One dictionary-valued case plus a signature dictionary.
- **What changed:** One case per shape the walk gains — dictionary value, nested wrapper, public field, tuple element — each red-first.
- **Why:** The todo was widened to the serializer's whole shape set.
- **Discovery Log:** 2026-09-11 / DICT

### 2026-10-05 — Narrowed to dictionaries and public fields

- **Section affected:** Scope, Serves
- **Original said:** One case each for dictionary value, nested wrapper, public field, and tuple element, plus a signature dictionary.
- **What changed:** Two cases: the consumer-shaped dictionary value and a public field. Tuples are out of scope; nested wrappers and the other two callers are pinned by DICT-002's generator tests.
- **Why:** User narrowed the todo 2026-10-05.
- **Discovery Log:** 2026-10-05 / DICT

---

## Notes

- Why the gate cases come before the fix: the v1.7.0 gate measured `[Remote]` body absence but had no consumer-shaped case that depended on a leaked body, so removing the leak surfaced this only in a consumer. This plan's red observations are the missing evidence, and DICT-002 turns them green.
- The dictionary case checks two things at once. It is red today because the walk misses the value type, and it would be falsely green if a `[Remote]` body leak ever came back and re-rooted the constructor.
