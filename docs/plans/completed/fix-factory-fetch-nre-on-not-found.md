# Fix Factory Fetch NRE When Entity Not Found

**Date:** 2026-03-21
**Related Todo:** [Fix Factory Fetch NRE When Entity Not Found](../todos/fix-factory-fetch-nre-on-not-found.md)
**Status:** Complete
**Last Updated:** 2026-03-21

---

## Overview

Generated factory methods throw `NullReferenceException` when a `[Fetch]` method returns `false` (entity not found) and the factory has authorization (`[AuthorizeFactory<T>]` or `[AspAuthorize]`). The bug is in two locations in `ClassFactoryRenderer.cs` where the bool-false path emits `return default!;`. When `HasAuth` is true, the local method's return type is `Authorized<T>`, so `default!` evaluates to `null`. The public wrapper then dereferences `.Result` (read-style) or `.HasAccess` (write-style) on the null reference, causing NRE.

The fix changes the `default!` return on the bool-false path to `new Authorized<T>(default)` when `HasAuth` is true, consistent with how the success path and save routing already construct `Authorized<T>` wrappers.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/fix-factory-fetch-nre-on-not-found.md#requirements-review)

### Relevant Existing Requirements

#### Behavioral Contracts

- **Bool-return Fetch contract** (`skills/RemoteFactory/references/factory-operations.md:150-166`): "Returning `bool` signals RemoteFactory: `true` means 'this instance is populated, return it to the caller,' `false` means 'the data wasn't found, discard this instance and return null.'" The bug violates this contract when authorization is present -- the factory throws NRE instead of returning null.

- **Auth failure behavior** (`src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs:54-58`, `skills/RemoteFactory/references/authorization.md:281`): "Create/Fetch return null when authorization fails." This establishes that the public Fetch wrapper must handle null `Authorized<T>.Result`. The bool-false path is a different scenario (not-found rather than auth-denied), but must produce the same consumer-visible result: null.

- **Authorized<T>(T?) constructor semantics** (`src/RemoteFactory/Authorized.cs:91-102`): `new Authorized<T>(default)` sets `Result = default` (null for reference types) and `HasAccess = false`. This is the correct representation for "not found" within the `Authorized<T>` wrapper -- the wrapper exists (non-null), but signals no result.

#### Existing Tests

- **`RemoteFetchRoundTripTests.cs:53-61`** (`RemoteFetch_BoolFalse_ReturnsNull`): Asserts `Assert.Null(result)` when Fetch returns false. Passes because the target lacks `[AuthorizeFactory]`. Not affected by this fix.

- **`RemoteFetchRoundTripTests.cs:84-92`** (`RemoteFetch_BoolFalse_ServerAlsoReturnsNull`): Same assertion from server container. Not affected.

- **`RemoteFetchRoundTripTests.cs:100-109`** (`RemoteFetch_Remote_BoolFalse_ReturnsNull`): Same assertion through remote transport with `[Service]` injection. Not affected.

- **`RemoteFetchRoundTripTests.cs:68-78`** (`RemoteFetch_BoolTrue_ReturnsObject`): Verifies happy path (bool true). Not affected.

### Gaps

1. **No test coverage for bool-false + authorization intersection.** The existing `RemoteFetchTarget_BoolFalse` targets lack `[AuthorizeFactory<T>]`. No tests exercise the combination of `Task<bool>` Fetch + authorization. This is the exact scenario that triggers the NRE.

2. **No Design project example of bool-return Fetch.** The Design project's `Order.cs` and `AuthorizedOrder.cs` both use void-returning Fetch. CLAUDE-DESIGN.md mentions `Task<bool> Fetch` in Critical Rules but no Design.Domain example demonstrates it. This gap is noted but out of scope for this bug fix.

3. **Save with bool-return + authorization.** The write-style path has the same `default!` pattern (`ClassFactoryRenderer.cs:877-894`). If any entity combines `[Insert]`/`[Update]`/`[Delete]` returning `bool` with `[AuthorizeFactory<T>]`, the same NRE would occur on Save. The fix must address both paths.

### Contradictions

None. The todo proposes fixing a bug that violates the documented bool-return contract.

### Recommendations for Architect

The reviewer recommends the `new Authorized<T>(default)` approach over null-checking in the public wrapper. This preserves semantic meaning: the `Authorized<T>` wrapper exists but indicates no result, consistent with how authorization failure already works. The null-check approach would silently swallow nulls that might indicate different bugs in the future.

---

## Business Rules (Testable Assertions)

1. WHEN a `[Fetch]` method returns `false` AND the factory has `[AuthorizeFactory<T>]`, THEN the public Fetch method RETURNS `null` (not NRE). -- Source: Bool-return Fetch contract + Auth failure behavior

2. WHEN a `[Fetch]` method returns `false` AND the factory has `[AuthorizeFactory<T>]`, THEN the local method RETURNS `new Authorized<T>(default)` (a non-null wrapper with null Result). -- Source: NEW (implementation detail ensuring Rule 1)

3. WHEN a `[Fetch]` method returns `true` AND the factory has `[AuthorizeFactory<T>]`, THEN the public Fetch method RETURNS the fetched entity (existing behavior, must not regress). -- Source: existing `RemoteFetch_BoolTrue_ReturnsObject` test

4. WHEN a `[Fetch]` method returns `false` AND the factory has NO authorization, THEN the public Fetch method RETURNS `null` (existing behavior, must not regress). -- Source: existing `RemoteFetch_BoolFalse_ReturnsNull` test

5. WHEN a write-style method (`[Insert]`/`[Update]`/`[Delete]`) returns `false` AND the factory has `[AuthorizeFactory<T>]`, THEN the local write method RETURNS `new Authorized<T>(default)` (not `default!`). -- Source: NEW (same pattern as Rule 2, applied to write-style path)

6. WHEN a `[Fetch]` method returns `false` AND the factory has `[AuthorizeFactory<T>]` AND the request crosses the client/server boundary, THEN the client receives `null` (serialization round-trip). -- Source: NEW (extends Rule 1 across the remote transport)

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Auth + bool-false Fetch (local) | `[AuthorizeFactory<T>]` target, Fetch returns `false`, server container | 1, 2 | `result` is `null` (no NRE) |
| 2 | Auth + bool-false Fetch (remote round-trip) | `[AuthorizeFactory<T>]` target, Fetch returns `false`, client container | 1, 2, 6 | `result` is `null` (no NRE), round-trip succeeds |
| 3 | Auth + bool-true Fetch (no regression) | `[AuthorizeFactory<T>]` target, Fetch returns `true`, client container | 3 | `result` is non-null with expected state |
| 4 | No-auth + bool-false Fetch (no regression) | No auth, Fetch returns `false`, client container | 4 | `result` is `null` (existing tests still pass) |
| 5 | Auth + bool-false Fetch with [Service] (remote) | `[AuthorizeFactory<T>]` target with `[Service]` param, Fetch returns `false`, client container | 1, 2, 6 | `result` is `null`, service was injected on server |

---

## Approach

Change the generated code in the bool-false early-return path to emit `new Authorized<T>(default)` instead of `default!` when `HasAuth` is true. This is a minimal, targeted fix at the two emission sites in `ClassFactoryRenderer.cs`:

1. **Read-style path** (line ~450-470): The `IsBool` check inside `RenderReadLocalMethod`
2. **Write-style path** (line ~877-894): The `IsBool` check inside `RenderWriteLocalMethod`

Both sites share the same pattern:

```
// Current (buggy):
return default!;              // null for Authorized<T>

// Fixed:
return new Authorized<T>(default);   // non-null wrapper with null Result
```

The `Task.FromResult` wrapping (non-async case) also needs the same treatment.

The public wrappers (`RenderPublicMethod` lines 295-320, `RenderWritePublicMethod` lines 734-757) do NOT need changes. Once the local method returns a non-null `Authorized<T>`, the `.Result` access works correctly and returns null (the `Authorized<T>.Result` is `default(T)` which is `null` for reference types).

The save public wrapper (`RenderSavePublicMethod` lines 983-1029) is also safe: it checks `authorized.HasAccess` before `.Result`, and `new Authorized<T>(default)` has `HasAccess = false`, so it will throw `NotAuthorizedException`. This is actually the correct behavior for save -- if a write operation's domain method returns `false`, it means the operation failed, and the save should not silently return null.

**Note on save semantics:** For save operations, the bool-false path has different semantics than for fetch. A fetch returning false means "not found, return null." A save/insert/update/delete returning false is less common and arguably means "operation declined." The `Authorized<T>(default)` wrapper with `HasAccess = false` will cause the save public wrapper to throw `NotAuthorizedException`, which may not be ideal. However, this is a pre-existing semantic question and fixing it is out of scope for this bug. The immediate fix prevents the NRE, which is the blocking issue.

---

## Design

### Bug Location

Two sites in `src/Generator/Renderer/ClassFactoryRenderer.cs`:

**Site 1: Read-style local method** (`RenderReadLocalMethod`, lines 450-470)

```csharp
// Current code (lines 460-468):
if (!needsAsync && method.IsTask)
{
    var nullableServiceType = method.IsNullable ? $"{model.ServiceTypeName}?" : model.ServiceTypeName;
    sb.AppendLine($"                    return Task.FromResult<{nullableServiceType}>(default)!;");
}
else
{
    sb.AppendLine("                    return default!;");
}
```

When `HasAuth` is true, the local method's return type is `Authorized<T>` (or `Task<Authorized<T>>`). The `default!` evaluates to `null` for this reference type.

**Site 2: Write-style local method** (`RenderWriteLocalMethod`, lines 877-894)

Identical pattern to Site 1.

### Fix Design

At both sites, add a conditional: when `method.HasAuth` is true, emit `new Authorized<{model.ServiceTypeName}>(default)` instead of `default!`. This mirrors the pattern already used in `RenderSaveLocalMethod` at line 1052-1054 for the save routing default return:

```csharp
var defaultReturn = method.HasAuth
    ? $"new Authorized<{model.ServiceTypeName}>()"
    : $"default({model.ServiceTypeName})";
```

The exact fix for both sites:

```csharp
if (method.HasAuth)
{
    var authDefault = $"new Authorized<{model.ServiceTypeName}>(default)";
    if (!needsAsync && method.IsTask)
    {
        sb.AppendLine($"                    return Task.FromResult({authDefault});");
    }
    else
    {
        sb.AppendLine($"                    return {authDefault};");
    }
}
else if (!needsAsync && method.IsTask)
{
    var nullableServiceType = method.IsNullable ? $"{model.ServiceTypeName}?" : model.ServiceTypeName;
    sb.AppendLine($"                    return Task.FromResult<{nullableServiceType}>(default)!;");
}
else
{
    sb.AppendLine("                    return default!;");
}
```

Note: We use `new Authorized<T>(default)` (with `default` argument), not `new Authorized<T>()` (parameterless). The parameterless constructor is the `[JsonConstructor]` that leaves `Result` unset. The `Authorized(T?)` constructor explicitly sets `Result = default`, `HasAccess = false` when result is null. Both produce the same runtime state for reference types, but `new Authorized<T>(default)` is more explicit about intent.

### Files Changed

| File | Change |
|------|--------|
| `src/Generator/Renderer/ClassFactoryRenderer.cs` | Fix both bool-false return sites (read-style ~line 460, write-style ~line 885) |
| `src/Tests/RemoteFactory.IntegrationTests/TestTargets/FactoryRoundTrip/RoundTripTargets.cs` | Add new test targets: `RemoteFetchTarget_AuthBoolFalse`, `RemoteFetchTarget_AuthBoolTrue`, `RemoteFetchTarget_RemoteAuthBoolFalse` |
| `src/Tests/RemoteFactory.IntegrationTests/FactoryRoundTrip/RemoteFetchRoundTripTests.cs` | Add new tests for scenarios 1-3 and 5 |

### Test Target Design

**Authorization class** (reuse or add alongside existing targets):

```csharp
public class FetchAuthAllow
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool CanFetch() => true;  // Always allows -- we're testing the not-found path, not auth denial
}
```

**Test target for auth + bool-false Fetch:**

```csharp
[Factory]
[AuthorizeFactory<FetchAuthAllow>]
public partial class RemoteFetchTarget_AuthBoolFalse
{
    public bool FetchCalled { get; set; }
    public int ReceivedId { get; set; }

    [Fetch]
    [Remote]
    internal Task<bool> Fetch(int id)
    {
        FetchCalled = true;
        ReceivedId = id;
        return Task.FromResult(false);
    }
}
```

**Test target for auth + bool-true Fetch** (regression guard):

```csharp
[Factory]
[AuthorizeFactory<FetchAuthAllow>]
public partial class RemoteFetchTarget_AuthBoolTrue
{
    public bool FetchCalled { get; set; }
    public int ReceivedId { get; set; }

    [Fetch]
    [Remote]
    internal Task<bool> Fetch(int id)
    {
        FetchCalled = true;
        ReceivedId = id;
        return Task.FromResult(true);
    }
}
```

**Test target for auth + bool-false Fetch with [Service]** (proves server execution):

```csharp
[Factory]
[AuthorizeFactory<FetchAuthAllow>]
public partial class RemoteFetchTarget_RemoteAuthBoolFalse
{
    public bool FetchCalled { get; set; }
    public int ReceivedId { get; set; }
    public bool ServiceWasInjected { get; set; }

    [Remote]
    [Fetch]
    internal Task<bool> Fetch(int id, [Service] IServerOnlyService service)
    {
        FetchCalled = true;
        ReceivedId = id;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }
}
```

---

## Implementation Steps

1. **Fix read-style bool-false return** in `ClassFactoryRenderer.cs` (`RenderReadLocalMethod`, lines ~450-470): Add `HasAuth` conditional before the existing `default!` emission to emit `new Authorized<T>(default)` instead.

2. **Fix write-style bool-false return** in `ClassFactoryRenderer.cs` (`RenderWriteLocalMethod`, lines ~877-894): Apply the identical fix as step 1.

3. **Add auth class and test targets** in `RoundTripTargets.cs`: Add `FetchAuthAllow` class and three new test targets (`RemoteFetchTarget_AuthBoolFalse`, `RemoteFetchTarget_AuthBoolTrue`, `RemoteFetchTarget_RemoteAuthBoolFalse`).

4. **Add integration tests** in `RemoteFetchRoundTripTests.cs`: Add tests for scenarios 1, 2, 3, and 5 from the Test Scenarios table.

5. **Build and run all tests**: Verify the new tests pass and all existing tests remain green.

---

## Acceptance Criteria

- [ ] `RemoteFetchTarget_AuthBoolFalse` factory Fetch returns `null` (not NRE) from both server and client containers
- [ ] `RemoteFetchTarget_AuthBoolTrue` factory Fetch returns the populated entity (no regression)
- [ ] `RemoteFetchTarget_RemoteAuthBoolFalse` factory Fetch returns `null` through remote transport with service injection
- [ ] All existing `RemoteFetchRoundTripTests` continue to pass (no regression in non-auth bool-false/bool-true paths)
- [ ] Full solution builds without errors: `dotnet build src/Neatoo.RemoteFactory.sln`
- [ ] All tests pass: `dotnet test src/Neatoo.RemoteFactory.sln`

---

## Dependencies

None. This is a self-contained bug fix in the generator renderer and test project.

---

## Risks / Considerations

1. **Save path semantics**: The write-style fix will cause `new Authorized<T>(default)` with `HasAccess = false` to flow through the save public wrapper, which throws `NotAuthorizedException`. This may not be the ideal behavior for "save operation returned false," but it prevents NRE and the save-with-bool-return + auth combination is untested and likely unused. A future todo can address save-specific bool-false semantics if needed.

2. **Generated code caching**: Since this changes the generator output, any project using RemoteFactory will need a rebuild to pick up the fix. The generated code in `obj/` directories is not checked in, so this is standard behavior.

3. **Authorized<T> constructor choice**: `new Authorized<T>(default)` vs `new Authorized<T>()` -- both produce `HasAccess = false, Result = null` for reference types. We use the `(T?)` constructor overload because it explicitly sets `Result`, which is clearer about intent. The `[JsonConstructor]` parameterless constructor could theoretically have different init behavior in the future.

---

## Architectural Verification

**Scope Table:**

| Pattern | Affected? | Current Behavior | After Fix |
|---------|-----------|-----------------|-----------|
| Read-style Fetch + auth + bool-false | YES (bug) | NRE on `.Result` | Returns `null` |
| Read-style Fetch + auth + bool-true | No | Returns entity | Unchanged |
| Read-style Fetch + no auth + bool-false | No | Returns `null` | Unchanged |
| Write-style Save + auth + bool-false | YES (latent bug) | NRE on `.HasAccess` | Throws `NotAuthorizedException` |
| Write-style Save + no auth + bool-false | No | Returns `default!` | Unchanged |

**Verification Evidence:**

- Read-style bug site: `src/Generator/Renderer/ClassFactoryRenderer.cs:460-468` -- confirmed `default!` emitted when `HasAuth` true
- Write-style bug site: `src/Generator/Renderer/ClassFactoryRenderer.cs:885-893` -- confirmed identical pattern
- Public wrapper NRE site: `src/Generator/Renderer/ClassFactoryRenderer.cs:315` -- `.Result` dereference on potentially null `Authorized<T>`
- Save wrapper NRE site: `src/Generator/Renderer/ClassFactoryRenderer.cs:1009` -- `.HasAccess` dereference on potentially null `Authorized<T>`
- `Authorized<T>(T?)` constructor: `src/RemoteFactory/Authorized.cs:91-102` -- `HasAccess = false` when `result == null`
- Existing non-auth tests pass: 3 tests in `RemoteFetchRoundTripTests.cs` cover bool-false without auth
- Save routing default already uses `new Authorized<T>()`: `ClassFactoryRenderer.cs:1052-1053`

**Breaking Changes:** No. The current behavior is an NRE crash. The fix replaces a crash with the contractually correct return value (`null`).

**Codebase Analysis:**

- `ClassFactoryRenderer.cs` is the single file containing the bug (two sites)
- The fix pattern (`new Authorized<T>(...)`) is already used elsewhere in the same file (line 515, 921, 1053, 1147)
- No other renderers emit bool-check code
- The `Authorized<T>` class is stable and the `(T?)` constructor is well-defined

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Fix + Tests | developer | Yes | Single focused phase: fix two lines in renderer, add test targets and tests, build and verify | None |

**Parallelizable phases:** None -- single phase.

**Notes:** This is a small, focused bug fix touching one production file and two test files. A single developer agent phase is sufficient.

---

## Documentation

**Agent:** developer (no documentation agent needed)
**Completed:** [pending]

### Expected Deliverables

- [ ] No skill/doc updates needed -- this is a bug fix restoring existing documented behavior
- [ ] Skill updates: N/A
- [ ] Sample updates: N/A

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-21

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `RenderReadLocalMethod` (line 342): `method.IsBool && !method.IsConstructor && !method.IsStaticFactory` at line 451 triggers bool check. When `!_succeeded` (line 453), fix adds `method.HasAuth` conditional to emit `new Authorized<T>(default)` instead of `default!`. `RenderPublicMethod` (line 295): `.Result` on the `Authorized<T>` at line 315 returns `null` (since `Result = default = null`). | Public Fetch returns `null` (no NRE) | Yes | `Authorized<T>(T?)` ctor (line 91-102) sets `Result = null`, `HasAccess = false`. `.Result` returns `null`. |
| 2 | Same path as Rule 1: `RenderReadLocalMethod` lines 451-468 with proposed fix -- when `method.HasAuth == true`, emits `new Authorized<{model.ServiceTypeName}>(default)` (async variant: `Task.FromResult(new Authorized<T>(default))`). | Local method returns `new Authorized<T>(default)` (non-null wrapper) | Yes | Directly matches proposed code in Design section. |
| 3 | `RenderReadLocalMethod` (line 342): When `_succeeded == true`, execution continues past bool check (lines 450-470). Success path at lines 513-515: `returnExpr = new Authorized<T>(resultVar)`. `RenderPublicMethod` line 315: `.Result` on non-null `Authorized<T>` returns entity. | Public Fetch returns populated entity | Yes | Fix only changes `!_succeeded` early-return path; success path untouched. |
| 4 | `RenderReadLocalMethod` lines 460-468: When `method.HasAuth == false`, fix falls through to existing `else if` / `else` branches emitting `default!` or `Task.FromResult<T?>(default)!`. `RenderPublicMethod` lines 309-311: no auth, returns method target directly. | Public Fetch returns `null` (existing behavior) | Yes | `HasAuth` conditional is new outer branch; existing non-auth code becomes `else` branches. |
| 5 | `RenderLocalMethod(WriteMethodModel)` (line 818): Same `method.IsBool` check at line 866, same `!_succeeded` at line 878, same proposed fix: when `method.HasAuth == true`, emit `new Authorized<T>(default)`. | Write local method returns `new Authorized<T>(default)` (not `default!`) | Yes | Identical pattern to read-style. Save wrapper (line 1009) checks `.HasAccess` which is `false`, throws `NotAuthorizedException`. |
| 6 | Rule 1 fix applied server-side. `MakeSerializedServerStandinDelegateRequest.ForDelegateNullable<T>` serializes server response. Server-side `RenderPublicMethod` line 315 extracts `.Result` (null) from `Authorized<T>`. Null serializes/deserializes correctly. Client receives `null`. | Client receives `null` through round-trip | Yes | `ForDelegateNullable` returns `null` as valid result. Auth unwrapping happens server-side before serialization. |

### Concerns

1. **Minor naming inaccuracy (non-blocking):** Plan references `RenderWriteLocalMethod` but actual method name is `RenderLocalMethod(StringBuilder sb, WriteMethodModel method, ClassFactoryModel model)` at line 818. Does not affect implementation -- line numbers and code patterns are correct.

2. **`RenderWritePublicMethod` reference (non-blocking):** Plan references "lines 734-757" as write-style public wrapper. The actual method at those lines is `RenderClassExecutePublicMethod` (for `[Execute]` operations), not write-style `[Insert]/[Update]/[Delete]`. Does not affect correctness -- the fix at the local method level prevents null from reaching any public wrapper.

3. **Save path semantics (acknowledged, out of scope):** Write-style bool-false + auth results in `NotAuthorizedException` via save wrapper (line 1009). Correctly scoped as out of scope.

---

## Implementation Contract

**Created:** 2026-03-21
**Approved by:** developer agent

### Verification Acceptance Criteria

- All new tests pass (scenarios 1-3, 5)
- All existing `RemoteFetchRoundTripTests` pass (no regression)
- Full solution builds: `dotnet build src/Neatoo.RemoteFactory.sln`
- All tests pass: `dotnet test src/Neatoo.RemoteFactory.sln`

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1 | `RemoteFetch_AuthBoolFalse_ServerReturnsNull` | Auth + bool-false, server container (local execution) |
| 2 | `RemoteFetch_AuthBoolFalse_ClientReturnsNull` | Auth + bool-false, client container (serialization round-trip) |
| 3 | `RemoteFetch_AuthBoolTrue_ReturnsObject` | Auth + bool-true, client container (regression guard) |
| 4 | (Covered by existing tests) | No-auth + bool-false already tested |
| 5 | `RemoteFetch_RemoteAuthBoolFalse_ReturnsNull` | Auth + bool-false + `[Service]`, client container (proves server execution) |

### In Scope

- [ ] `src/Generator/Renderer/ClassFactoryRenderer.cs`: Fix bool-false return at two sites
  - Site 1: `RenderReadLocalMethod` lines 460-468 -- add `HasAuth` conditional before existing branches
  - Site 2: `RenderLocalMethod(WriteMethodModel)` lines 885-893 -- add identical `HasAuth` conditional
- [ ] `src/Tests/RemoteFactory.IntegrationTests/TestTargets/FactoryRoundTrip/RoundTripTargets.cs`: Add `FetchAuthAllow` auth class and three test targets (`RemoteFetchTarget_AuthBoolFalse`, `RemoteFetchTarget_AuthBoolTrue`, `RemoteFetchTarget_RemoteAuthBoolFalse`)
- [ ] `src/Tests/RemoteFactory.IntegrationTests/FactoryRoundTrip/RemoteFetchRoundTripTests.cs`: Add tests for scenarios 1-3, 5
- [ ] Checkpoint: Build and run all tests after changes

### Out of Scope

- Save-specific bool-false semantics (future todo if needed)
- Design project examples for bool-return Fetch
- `[AspAuthorize]` + bool-false combination (separate test -- same code path as `[AuthorizeFactory<T>]`)
- All other test files

### Verification Gates

1. After renderer fix: `dotnet build src/Neatoo.RemoteFactory.sln` succeeds
2. After adding test targets: Build succeeds (generator produces correct code for new targets)
3. Final: `dotnet test src/Neatoo.RemoteFactory.sln` -- all tests pass, no regressions

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure
- Architectural contradiction discovered

---

## Implementation Progress

**Started:** 2026-03-21
**Developer:** developer agent

### Milestones

1. **Fix read-style bool-false return (Site 1)** -- DONE. Added `method.HasAuth` conditional in `RenderReadLocalMethod` (lines 460-471) to emit `new Authorized<T>()` instead of `default!`.

2. **Fix write-style bool-false return (Site 2)** -- DONE. Added identical `method.HasAuth` conditional in `RenderLocalMethod(WriteMethodModel)` (lines 900-911).

3. **Constructor ambiguity discovered and resolved** -- The plan specified `new Authorized<T>(default)` but this caused CS0121 ambiguity between `Authorized(Authorized)` and `Authorized(T?)` constructors. Resolved by using `new Authorized<T>()` (parameterless constructor), which is consistent with the existing save routing default pattern at `RenderSaveLocalMethod` line 1083. Both produce the same runtime state: `HasAccess = false`, `Result = default(T)`.

4. **Build verification after renderer fix** -- DONE. Full solution builds with 0 errors.

5. **Add FetchAuthAllow auth class and three test targets** -- DONE. Added to `RoundTripTargets.cs`: `FetchAuthAllow`, `RemoteFetchTarget_AuthBoolFalse`, `RemoteFetchTarget_AuthBoolTrue`, `RemoteFetchTarget_RemoteAuthBoolFalse`.

6. **Build verification after test targets** -- DONE. Generator produces correct factory code for all three targets. Verified generated `LocalFetch` method emits `new Authorized<RemoteFetchTarget_AuthBoolFalse>()` on the bool-false path.

7. **Add integration tests** -- DONE. Added 4 tests to `RemoteFetchRoundTripTests.cs` for scenarios 1, 2, 3, and 5.

8. **Full test suite** -- DONE. All 1984 tests pass (502+502 integration + 490+490 unit), 0 failures, 6 skipped (pre-existing performance tests).

---

## Completion Evidence

**Reported:** 2026-03-21

- **Tests Passing:** All 1984 tests pass across net9.0 and net10.0 (502+502 integration, 490+490 unit). Zero failures. 6 skipped (pre-existing ShowcasePerformanceTests).
- **Verification Resources Pass:** N/A (no design project changes needed for this bug fix)
- **All Contract Items:** Confirmed 100% complete

### Contract Item Status

| Contract Item | Status |
|--------------|--------|
| `ClassFactoryRenderer.cs` Site 1 (read-style) fix | DONE |
| `ClassFactoryRenderer.cs` Site 2 (write-style) fix | DONE |
| `RoundTripTargets.cs` FetchAuthAllow + 3 targets | DONE |
| `RemoteFetchRoundTripTests.cs` 4 new tests | DONE |
| Build succeeds | DONE |
| All tests pass | DONE |

### Test Scenario Results

| Scenario # | Test Method | Result |
|------------|-------------|--------|
| 1 | `RemoteFetch_AuthBoolFalse_ServerReturnsNull` | PASS (returns null, no NRE) |
| 2 | `RemoteFetch_AuthBoolFalse_ClientReturnsNull` | PASS (returns null through round-trip) |
| 3 | `RemoteFetch_AuthBoolTrue_ReturnsObject` | PASS (entity returned with state intact) |
| 4 | (Existing tests) | PASS (no regression) |
| 5 | `RemoteFetch_RemoteAuthBoolFalse_ReturnsNull` | PASS (returns null with service injection) |

### Deviation from Plan

The plan specified `new Authorized<T>(default)` but the implementation uses `new Authorized<T>()` (parameterless constructor). This change was necessary because `new Authorized<T>(default)` is ambiguous between `Authorized(Authorized)` and `Authorized(T?)` constructors (CS0121). The parameterless constructor produces identical runtime state (`HasAccess = false`, `Result = default(T)`) and is already the established pattern in the save routing default at `RenderSaveLocalMethod` line 1083. Both business rules 1-6 are satisfied identically.

---

## Architect Verification

**Verified:** 2026-03-21
**Verdict:** VERIFIED

**Independent test results:** All 1984 tests pass (502+502 integration, 490+490 unit). Zero failures. 6 skipped (pre-existing ShowcasePerformanceTests). Build succeeds with 0 errors.

**Design match:** Both bug sites fixed correctly. Test targets and tests cover all four planned scenarios (1, 2, 3, 5). Developer deviated from `new Authorized<T>(default)` to `new Authorized<T>()` due to CS0121 constructor ambiguity -- verified acceptable (identical runtime state, matches existing codebase pattern). Developer used `AuthorizeFactoryOperation.Fetch` instead of plan's `.Read` -- this is correct (Fetch is the proper enum value for Fetch operations).

**Issues found:** None.

---

## Requirements Verification

**Reviewer:** business-requirements-reviewer
**Verified:** 2026-03-21
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Source | Status | Evidence |
|-------------|--------|--------|----------|
| Bool-return Fetch contract: `false` = not found, factory returns `null` | `docs/attributes-reference.md:109` | Satisfied | Fix emits `new Authorized<T>()` (non-null wrapper, null Result) instead of `default!` (null wrapper). Public wrapper `.Result` returns `null` instead of NRE. |
| Auth failure behavior: Create/Fetch return null when auth fails | `src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs:54-58` | Satisfied | Not-found path now produces same consumer result (null) as auth-denied path |
| `Authorized<T>` parameterless constructor semantics | `src/RemoteFactory/Authorized.cs:80-82`, base class lines 19-22 | Satisfied | `new Authorized<T>()` sets `HasAccess = false`, `Result = null`. Matches save routing default pattern at line 1087. |
| `[AuthorizeFactory<T>]` usage pattern | `src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs:84` | Satisfied | `FetchAuthAllow` uses `AuthorizeFactoryOperation.Fetch` with `bool` return, matching Design project pattern |
| `[Remote]` only for aggregate root entry points | `src/Design/CLAUDE-DESIGN.md` Critical Rule 1 | Satisfied | New test targets use `[Remote]` on Fetch correctly |
| Properties need public setters | `src/Design/CLAUDE-DESIGN.md` Critical Rule 5 | Satisfied | All test target properties use public setters |
| `partial` keyword required | `src/Design/CLAUDE-DESIGN.md` Critical Rule 6 | Satisfied | All three new test targets are `partial` classes |
| Write-style path also fixed | `ClassFactoryRenderer.cs:894-928` | Satisfied | Identical `method.HasAuth` conditional at write-style site |
| No Design Debt violation | `src/Design/CLAUDE-DESIGN.md` Design Debt table | Satisfied | Fix does not implement any deliberately deferred feature |
| Existing non-auth bool-false tests unaffected | `RemoteFetchRoundTripTests.cs:53-109` | Satisfied | `else` branches preserve original `default!` behavior for non-auth cases |

### Unintended Side Effects

1. **Write-style bool-false + auth path**: Fix changes behavior from NRE to `NotAuthorizedException` via save public wrapper (line 1043-1045). Documented in plan Risks section as acceptable -- save-with-bool-return + auth combination is untested and likely unused. Previous behavior (NRE crash) was strictly worse.

2. **No effect on existing auth tests**: All existing `[AuthorizeFactory<T>]` test targets use void-returning Fetch, not bool-returning. The fix only triggers when both `IsBool` and `HasAuth` are true.

3. **No effect on serialization contracts**: `Authorized<T>` wrapper is created/consumed within generated local and public methods on the server side. For Fetch, the public wrapper extracts `.Result` (null) before serialization. Null serializes correctly.

4. **No effect on Design project tests or published docs accuracy**: Design projects use void-returning Fetch. Published docs already state the correct contract (`false` = not found, factory returns `null`).

### Issues Found

None.
