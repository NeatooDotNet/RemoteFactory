# AuthorizeFactory Design Project Coverage

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-03-08
**Last Updated:** 2026-03-08

---

## Problem

`[AuthorizeFactory]` is not covered in the Design projects. `SecureOrder.cs` only comments about the attribute but doesn't actually use it. There are no Design.Tests for `CanXxx` authorization methods. The Design projects are supposed to be the source of truth, but they don't demonstrate `[AuthorizeFactory]` with `[Remote]` methods — which means the `CanXxx` promotion behavior (especially `[Remote] internal` → `CanXxx` promoted to `public` on the factory interface) has no source-of-truth demonstration.

**Discovered references:**
- `src/Design/Design.Domain/Aggregates/SecureOrder.cs` — documents `[AuthorizeFactory]` in comments only, does not use the attribute
- `src/Design/Design.Tests/` — no `CanXxx` tests exist
- `src/Examples/Person/Person.DomainModel/PersonModel.cs` — uses `[AuthorizeFactory<IPersonModelAuth>]` with `[Remote]` methods (working example, not source-of-truth)
- Related work: [remote-requires-internal](remote-requires-internal.md) — changed `[Remote]` to require `internal`, which affects `CanXxx` visibility promotion

## Solution

Add actual `[AuthorizeFactory]` usage to the Design project (likely `SecureOrder`), including demonstrating `[Remote] internal` methods with `CanXxx` promotion to `public` on the factory interface. Add Design.Tests covering authorization behavior. Ensure thorough testing of `[AuthorizeFactory]` in the test projects (unit and integration).

---

## Clarifications

---

## Requirements Review

**Reviewer:** [pending]
**Reviewed:** [pending]
**Verdict:** Pending

### Relevant Requirements Found

### Gaps

### Contradictions

### Recommendations for Architect

---

## Plans

---

## Tasks

- [ ] Architect comprehension check (Step 2)
- [ ] Business requirements review (Step 3)
- [ ] Architect plan creation & design (Step 4)
- [ ] Developer review (Step 5)
- [ ] Implementation (Step 7)
- [ ] Verification (Step 8)
- [ ] Documentation (Step 9)

---

## Progress Log

### 2026-03-08
- Created todo from gap discovered during remote-requires-internal implementation
- SecureOrder.cs only comments about [AuthorizeFactory], doesn't use it
- No Design.Tests for CanXxx methods exist

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass

**Verification results:**
- Build: [Pending]
- Tests: [Pending]

---

## Results / Conclusions

