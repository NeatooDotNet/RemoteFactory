# Fix Factory Fetch NRE When Entity Not Found

**Status:** Complete
**Priority:** High
**Created:** 2026-03-21
**Last Updated:** 2026-03-21

**Plan:** [Fix Factory Fetch NRE Plan](../plans/fix-factory-fetch-nre-on-not-found.md)

---

## Problem

Generated factory `Fetch` methods throw `NullReferenceException` when the underlying `[Fetch]` method returns `false` (entity not found).

The generated code pattern is:

```csharp
// LocalFetch1 (line ~170-174 in generated factory):
var _succeeded = await target.Fetch(visitId, repository, areaListFactory);
if (!_succeeded)
{
    return default!;  // <-- returns null for Authorized<T> if it's a reference type
}

// Public Fetch wrapper (line ~128):
public virtual async Task<ISignsAssessment?> Fetch(long visitId, ...)
{
    return (await Fetch1Property(visitId, cancellationToken)).Result;  // <-- NRE: (null).Result
}
```

`LocalFetch1` returns `default!` for `Authorized<T>` when the entity isn't found. The public `Fetch` wrapper then calls `.Result` on the null `Authorized<T>`, causing NRE.

### Reproduction

Any factory Fetch call where the `[Fetch]` method returns `false`:

```csharp
// SignsAssessmentFactory.Fetch(nonExistentVisitId) → NRE
var signs = await signsFactory.Fetch(999999);  // throws NullReferenceException
```

### Impact

In zTreatment (Neatoo 0.23.0), this causes 12 database test failures on master and 27 on feature branches. Every factory that has a Fetch returning `bool` (not-found pattern) is affected.

Affected generated factories observed: `SignsAssessmentFactory`, `VisitFactory`, and any factory where Fetch can legitimately return "not found."

---

## Solution

The `LocalFetch` method should return `new Authorized<T>(default)` (or equivalent) instead of `default!` when `_succeeded` is false, so that the `.Result` access on the `Authorized<T>` wrapper returns null rather than throwing NRE.

Alternatively, the public `Fetch` wrapper could null-check before accessing `.Result`:

```csharp
public virtual async Task<ISignsAssessment?> Fetch(long visitId, ...)
{
    var authorized = await Fetch1Property(visitId, cancellationToken);
    return authorized?.Result;
}
```

---

## Plans

- [Fix Factory Fetch NRE Plan](../plans/fix-factory-fetch-nre-on-not-found.md)

---

## Tasks

- [ ] Fix generated code for not-found path
- [ ] Add test for Fetch-returns-false scenario

---

## Progress Log

### 2026-03-21
- Created todo
- Bug discovered in zTreatment database tests — 12 failures on master, 27 on weighted-dosing-engine branch
- All failures trace to same root: `NullReferenceException` in generated `SignsAssessmentFactory.Fetch` at the `(await Fetch1Property(...)).Result` line
- Confirmed `default!` on line 174 of generated factory is the cause

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-21
**Verdict:** APPROVED

### Relevant Requirements Found

1. **Bool-return Fetch contract (factory-operations.md:150-166):** Documented behavioral contract: "returning `bool` signals RemoteFactory: `true` means 'this instance is populated, return it to the caller,' `false` means 'the data wasn't found, discard this instance and return null.'" The todo's bug directly violates this contract when authorization is present.

2. **Auth failure behavior (AuthorizedOrder.cs:54-58, authorization.md:281):** Documented generator behavior: "Create/Fetch return null when authorization fails." This establishes that the public Fetch wrapper must handle null `Authorized<T>` results. The bug is that the not-found path (`_succeeded == false`) produces a null `Authorized<T>` but the public wrapper assumes a non-null `Authorized<T>`.

3. **Existing test contracts (RemoteFetchRoundTripTests.cs:53-109):** Three tests document the expected behavior for bool-false Fetch: `RemoteFetch_BoolFalse_ReturnsNull`, `RemoteFetch_BoolFalse_ServerAlsoReturnsNull`, `RemoteFetch_Remote_BoolFalse_ReturnsNull`. All assert `Assert.Null(result)`. These tests pass because their targets lack `[AuthorizeFactory]`, so no `Authorized<T>` wrapping occurs. The bug is in the untested intersection of bool-false + authorization.

4. **Generated code pattern for bool check (ClassFactoryRenderer.cs:450-470, 877-894):** Two locations in the renderer emit `return default!;` for the `_succeeded == false` path. Both the read-style local method and the write-style local method share this pattern. When `HasAuth` is true, the method's return type is `Authorized<T>`, so `default!` is `null` for this reference type.

5. **Public wrapper pattern (ClassFactoryRenderer.cs:313-316, 750-753):** When `HasAuth` is true, the public method emits `return (await MethodProperty(...)).Result;` which unconditionally dereferences the `Authorized<T>` return value. This is the NRE site.

6. **Authorized<T> constructor (Authorized.cs:91-102):** `new Authorized<T>(default)` sets `Result = default` (null for reference types) and `HasAccess = false`. This constructor is the correct way to represent "not found" within the `Authorized<T>` wrapper.

7. **Write-style bool check (ClassFactoryRenderer.cs:877-894):** The same `default!` pattern exists for write-style methods (Save with bool return). The fix must address both read-style and write-style paths consistently.

### Gaps

1. **No test coverage for bool-false + authorization intersection:** The existing `RemoteFetchTarget_BoolFalse` targets have no `[AuthorizeFactory]`. There are no test targets or tests for the combination of `Task<bool>` Fetch + `[AuthorizeFactory<T>]` or `[AspAuthorize]`. The architect should require a test target and test that covers this specific combination.

2. **No Design project example of bool-return Fetch:** The Design project's `Order.cs` Fetch method returns `void` (not `Task<bool>`). `AuthorizedOrder.cs` Fetch also returns `void`. The CLAUDE-DESIGN.md Quick Reference mentions `Task<bool> Fetch` in the Critical Rules section (line 466) but no actual Design.Domain example demonstrates it. The architect should consider whether a Design project example is warranted.

3. **Save with bool-return + authorization:** The write-style path has the same `default!` pattern. If any entity combines `[Insert]`/`[Update]`/`[Delete]` returning `bool` with `[AuthorizeFactory<T>]`, the same NRE would occur on Save. The fix should address both read and write paths.

### Contradictions

None. The todo proposes fixing a bug that violates the documented bool-return contract. Both proposed solutions (returning `new Authorized<T>(default)` or null-checking before `.Result`) would restore compliance with documented behavior.

### Recommendations for Architect

1. **Prefer the `new Authorized<T>(default)` approach over null-check in the public wrapper.** Returning a valid `Authorized<T>` with `HasAccess = false` and `Result = null` from the local method preserves the semantic meaning: the `Authorized<T>` wrapper exists but indicates no access/no result. This is consistent with how authorization failure already works (auth failure returns null via the wrapper, not a null wrapper). The null-check approach (`authorized?.Result`) would silently swallow a null that might indicate a different bug in the future.

2. **Fix both read-style (ClassFactoryRenderer.cs:450-470) and write-style (ClassFactoryRenderer.cs:877-894) bool-check paths.** Both emit `return default!;` and both need the same fix when `HasAuth` is true.

3. **Add an integration test target with `[AuthorizeFactory<T>]` + `Task<bool>` Fetch that returns false.** The existing `RemoteFetchTarget_BoolFalse` covers the non-auth case. A new target with authorization is needed to cover the exact bug scenario and prevent regression.

4. **Consider also testing `[AspAuthorize]` + `Task<bool>` Fetch returning false**, since `[AspAuthorize]` also triggers the `HasAuth` code path in the generator.

---

## Results / Conclusions

Fixed NullReferenceException in generated factory Fetch methods when `[Fetch]` returns `false` (entity not found) and the factory has `[AuthorizeFactory<T>]`. The bug was in two sites in `ClassFactoryRenderer.cs` where the bool-false path emitted `return default!;` — null for `Authorized<T>`. Changed to emit `new Authorized<T>()` (non-null wrapper with `HasAccess = false`, `Result = null`), so the public wrapper's `.Result` access returns `null` correctly instead of throwing NRE.

**Files changed:** `ClassFactoryRenderer.cs` (2 fix sites), `RoundTripTargets.cs` (3 new test targets + auth class), `RemoteFetchRoundTripTests.cs` (4 new tests). All 1,984 tests pass.

