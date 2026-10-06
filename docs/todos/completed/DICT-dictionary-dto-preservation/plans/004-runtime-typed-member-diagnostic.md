# Diagnostic for runtime-typed serialized members

**Plan #:** 004
**Date:** 2026-09-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-8
**Status:** Retired
**Last Updated:** 2026-10-05
**Plan-review opt-in:** Yes — a new diagnostic is public generator surface, and the repo builds with warnings as errors, so a false positive breaks consumers' builds.
**Code-review opt-in:** Yes — behavior-changing generator output.
**Branch:** dict-004-runtime-typed-member-diagnostic — cut from the arc at Step 2
**PR:** —

---

## Scope

Adds a generator warning for the one shape no static walk can resolve: a serialized member — property or public field — on a walked DTO or a `[Factory]` entity whose static type is an interface or abstract class not annotated with `[Factory]`, because RemoteFactory's `$type` interface converter chooses the concrete type at runtime and the trimmer cannot be told what to preserve. The diagnostic names the member and points at the explicit-preservation remedies. It follows the existing NF-code family and descriptor conventions, ships with descriptor and emission tests in the diagnostics suite, and is verified quiet against the Design projects, the reference app, the examples, and the test libraries, all of which build with warnings as errors. It does not fire for `[Factory]`-typed members, which DI preserves, does not fire for System types, and does not attempt to enumerate implementations.

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

(none)

---

## Notes

- Severity is a draft-time decision for Step 2: a warning is the proposal, because the member may be legitimately preserved by a consumer LinkerConfig entry the generator cannot see; an error would force a suppression for correct code.
- Pre-flight must count how many members in the repo's own projects would fire before the descriptor exists, since `TreatWarningsAsErrors` turns every hit into a red build.

---

## Abandonment / Retirement Reason

**Retired 2026-10-05** — removed by the Goal narrowing; the runtime-typed boundary is documented by DICT-003 instead of diagnosed. Discovery Log 2026-10-05 / DICT.
