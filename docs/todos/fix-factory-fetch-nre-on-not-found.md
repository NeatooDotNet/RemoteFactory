# Fix Factory Fetch NRE When Entity Not Found

**Status:** Open
**Priority:** High
**Created:** 2026-03-21
**Last Updated:** 2026-03-21

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

## Results / Conclusions

