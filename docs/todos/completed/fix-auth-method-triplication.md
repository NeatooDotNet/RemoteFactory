# Fix Auth Method Triplication in Generated Save Methods

**Status:** Complete
**Priority:** Medium
**Created:** 2026-04-06
**Last Updated:** 2026-04-13


---

## Problem

Generated `LocalCanSave` (and `LocalSave` auth checks) call each auth method **3 times** instead of once. For example, in the generated `ParamAuthOrderFactory`, `LocalCanSave()` calls `CanWriteRole()` three times in a row.

**Root cause:** `BuildSaveMethodFromGroup` in `FactoryModelBuilder.cs` (line ~648) merges authorization from Insert, Update, and Delete methods using `SelectMany().Distinct()`. But `AuthMethodCall` is a `record` with an `IReadOnlyList<ParameterModel>` property — record equality for collections uses **reference equality**, so `Distinct()` fails to deduplicate when each write method creates separate parameter list instances.

Each of Insert, Update, and Delete contributes the same auth method calls (from the same auth class), but since the `ParameterModel` list instances differ by reference, `Distinct()` treats them as unique → 3 copies.

**Impact:** Correctness is unaffected (auth methods are idempotent), but the generated code is wasteful and looks wrong to users who inspect it.

**Discovered during:** Developer code review of the CanSave target parameter feature (see `docs/todos/completed/cansave-target-parameter.md`).

## Solution

Fix `AuthMethodCall` equality so `Distinct()` works correctly. Options:
1. Implement `IEquatable<AuthMethodCall>` with proper value-based comparison of parameter lists
2. Use a custom comparer with `Distinct(comparer)`
3. Deduplicate by a semantic key (method name + class name + parameter types) instead of relying on record equality

---

## Requirements Review

**Verdict:** Pending
**Reviewed:**
**Summary:**

---

## Plans

- [Fix Auth Method Triplication Plan](../plans/fix-auth-method-triplication.md)

---

## Tasks

- [x] Requirements review (Step 2) — Skipped (straightforward bug fix)
- [x] Architect validation (Step 3) — Skipped (straightforward bug fix)
- [x] Implementation (Step 4)
- [x] Developer code review (Step 5) — see memory file
- [x] Verification (Step 6) — full build + test suite green (1318 tests)

---

## Progress Log

### 2026-04-06
- Created todo from pre-existing bug discovered during CanSave target parameter developer code review
- Bug confirmed present in both AuthorizedOrderFactory and ParamAuthOrderFactory generated output
- Root cause identified: `AuthMethodCall` record equality + `IReadOnlyList` reference equality

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass

**Verification results:**
- Build: 0 errors, 0 new warnings (Debug, both TFMs) — verified 2026-04-13
- Tests: 1,318 passed / 0 failed / 3 intentionally skipped (577 unit × 2 TFMs + 582 integration × 2 TFMs) — verified 2026-04-13

---

## Results / Conclusions

Fixed `AuthMethodCall` equality so `Distinct()` correctly deduplicates identical auth method calls in `BuildSaveMethodFromGroup`. Added regression tests `AuthMethodCallEqualityTests.cs` and `AuthMethodTriplicationTests.cs`. Generated `LocalCanSave` / `LocalSave` now emits each auth method exactly once.

