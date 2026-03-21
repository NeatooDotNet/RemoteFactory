# Requirements Reviewer -- Fix Factory Fetch NRE on Not Found

Last updated: 2026-03-21
Current step: Post-implementation verification complete

## Key Context

This is a bug fix in the source generator's `ClassFactoryRenderer.cs`. The bug: when a `[Fetch]` method returns `false` (entity not found) and the factory has `[AuthorizeFactory<T>]`, the generated local method returned `default!` which is `null` for `Authorized<T>`. The public wrapper then dereferences `.Result` on null, causing NRE. The fix emits `new Authorized<T>()` instead of `default!` when `HasAuth` is true, at two sites (read-style line 462, write-style line 904).

The developer deviated from the plan's `new Authorized<T>(default)` to `new Authorized<T>()` due to CS0121 constructor ambiguity between `Authorized(Authorized)` and `Authorized(T?)`. The parameterless constructor produces identical runtime state (`HasAccess = false`, `Result = default(T)`) and is already the established pattern in the save routing default at `RenderSaveLocalMethod` line 1087.

## Mistakes to Avoid

None encountered.

## User Corrections

None.

## Requirements Verification

**Verdict:** REQUIREMENTS SATISFIED
**Date:** 2026-03-21

### Compliance Table

| # | Requirement | Source | Status | Notes |
|---|------------|--------|--------|-------|
| 1 | Bool-return Fetch contract: `false` means not found, factory returns `null` | `docs/attributes-reference.md:109`, `skills/RemoteFactory/references/factory-operations.md:150-166` | Satisfied | Fix ensures local method returns non-null `Authorized<T>` wrapper with null `Result`, so public wrapper's `.Result` returns `null` instead of throwing NRE |
| 2 | Auth failure behavior: Create/Fetch return null when auth fails | `src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs:54-58` | Satisfied | The not-found path now produces the same consumer-visible result (null) as the auth-denied path |
| 3 | `Authorized<T>` constructor semantics | `src/RemoteFactory/Authorized.cs:80-82` (parameterless), base class lines 19-22 | Satisfied | `new Authorized<T>()` sets `HasAccess = false`, `Result = null` (default for reference types). Public wrapper `.Result` returns null correctly |
| 4 | `[Remote]` only for aggregate root entry points | `src/Design/CLAUDE-DESIGN.md` Critical Rule 1 | Satisfied | New test targets use `[Remote]` correctly on their Fetch methods (they are aggregate root entry points) |
| 5 | `[AuthorizeFactory<T>]` usage pattern | `src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs:84` | Satisfied | `FetchAuthAllow` uses `[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]` matching the Design project pattern |
| 6 | Properties need public setters | `src/Design/CLAUDE-DESIGN.md` Critical Rule 5 | Satisfied | All test target properties use public setters |
| 7 | `partial` keyword required | `src/Design/CLAUDE-DESIGN.md` Critical Rule 6 | Satisfied | All three new test targets are `partial` classes |
| 8 | Existing non-auth bool-false tests unaffected | `RemoteFetchRoundTripTests.cs:53-109` | Satisfied | The `else` branches (lines 477-485 and 919-927) preserve the original `default!` behavior for non-auth cases |
| 9 | Write-style path also fixed | `ClassFactoryRenderer.cs:894-928` | Satisfied | Identical `method.HasAuth` conditional applied at write-style site |
| 10 | No Design Debt violation | `src/Design/CLAUDE-DESIGN.md` Design Debt table | Satisfied | This fix does not implement any deliberately deferred feature |

### Unintended Side Effects

1. **Write-style bool-false + auth path**: The fix causes `new Authorized<T>()` with `HasAccess = false` to flow through the save public wrapper, which throws `NotAuthorizedException` (line 1043-1045). This is a change from NRE to `NotAuthorizedException`. The plan documents this as a known semantic difference (Risks section) and explicitly scopes it as acceptable -- the save-with-bool-return + auth combination is untested and likely unused. The previous behavior (NRE crash) was strictly worse.

2. **No effect on existing authorization tests**: All existing `[AuthorizeFactory<T>]` test targets use void-returning Fetch methods, not bool-returning. The fix only changes behavior when both `IsBool` and `HasAuth` are true, so existing auth tests are unaffected.

3. **No effect on serialization contracts**: The `Authorized<T>` wrapper is created and consumed entirely within the generated local method and public wrapper on the server side. For Fetch operations, the public wrapper extracts `.Result` (null) before any serialization occurs. The null value serializes correctly through the existing transport.

4. **No effect on Design project tests**: The Design projects use void-returning Fetch methods and are not affected by this change.

5. **No effect on published documentation accuracy**: The documentation at `docs/attributes-reference.md:109` states "Returns `bool` or `Task<bool>` -- `false` means not found (factory returns `null`)." The fix restores compliance with this documented contract. No doc updates needed.

### Issues Found

None. The implementation correctly addresses both bug sites, follows the established `new Authorized<T>()` pattern already used in the save routing default, and adds comprehensive test coverage for the previously untested bool-false + authorization intersection.
