# Architect -- Fix Factory Fetch NRE on Not Found

Last updated: 2026-03-21
Current step: Post-implementation verification (Step 8A) complete -- VERIFIED

## Key Context

- Bug is in `ClassFactoryRenderer.cs` at two sites: read-style (~line 460) and write-style (~line 900)
- Both emit `return default!;` on the bool-false path, which is null for `Authorized<T>`
- Public wrappers then dereference `.Result` (read-style) or `.HasAccess` (save-style) on null
- Fix: emit `new Authorized<T>()` instead of `default!` when `HasAuth` is true
- Developer deviated from plan (`new Authorized<T>(default)` to `new Authorized<T>()`) due to CS0121 constructor ambiguity -- verified acceptable

## Mistakes to Avoid

- Original plan said `new Authorized<T>(default)` but this causes CS0121 ambiguity between `Authorized(Authorized)` and `Authorized(T?)` constructors when `T` is unconstrained and `default` is `null`
- `new Authorized<T>()` is the correct choice -- it produces identical runtime state (`HasAccess = false`, `Result = null`) and matches existing save routing default pattern at `RenderSaveLocalMethod`
- Plan design section referenced `AuthorizeFactoryOperation.Read` but the correct enum value for Fetch operations is `AuthorizeFactoryOperation.Fetch` -- developer correctly used `.Fetch`

## User Corrections

None.

## Architectural Verification (Pre-Handoff)

Scope table and evidence documented in plan file. No breaking changes. The fix replaces a crash (NRE) with the contractually correct return value (null).

## Architect Verification (Post-Implementation)

**Date:** 2026-03-21
**Verdict:** VERIFIED

### Independent Build Results

- `dotnet build src/Neatoo.RemoteFactory.sln`: 0 errors, 2 warnings (pre-existing Blazor WASM NativeFileReference warnings)
- `dotnet test src/Neatoo.RemoteFactory.sln`: ALL PASS
  - Integration tests (net9.0): 502 passed, 3 skipped, 0 failed
  - Integration tests (net10.0): 502 passed, 3 skipped, 0 failed
  - Unit tests (net9.0): 490 passed, 0 skipped, 0 failed
  - Unit tests (net10.0): 490 passed, 0 skipped, 0 failed
  - Total: 1984 passed, 6 skipped (pre-existing ShowcasePerformanceTests), 0 failed

### Design Match

**Site 1 (read-style, ClassFactoryRenderer.cs lines 462-472):** Fix matches design. `method.HasAuth` conditional added before existing branches. When true, emits `new Authorized<{model.ServiceTypeName}>()`. Handles both async and Task.FromResult variants. Comments explain the rationale.

**Site 2 (write-style, ClassFactoryRenderer.cs lines 904-914):** Identical fix pattern to Site 1. Comments reference `.HasAccess` (write-style wrapper) rather than `.Result` (read-style wrapper), showing awareness of both NRE sites.

**Test targets (RoundTripTargets.cs lines 194-267):**
- `FetchAuthAllow`: Authorization class with `AuthorizeFactoryOperation.Fetch` (correct enum value) returning `true` (allows access -- testing not-found path, not auth denial)
- `RemoteFetchTarget_AuthBoolFalse`: Core bug scenario -- auth + bool-false Fetch
- `RemoteFetchTarget_AuthBoolTrue`: Regression guard -- auth + bool-true Fetch
- `RemoteFetchTarget_RemoteAuthBoolFalse`: Auth + bool-false + [Service] injection (proves server execution via remote transport)

**Tests (RemoteFetchRoundTripTests.cs lines 141-197):**
- Scenario 1: `RemoteFetch_AuthBoolFalse_ServerReturnsNull` -- server container, asserts null
- Scenario 2: `RemoteFetch_AuthBoolFalse_ClientReturnsNull` -- client container round-trip, asserts null
- Scenario 3: `RemoteFetch_AuthBoolTrue_ReturnsObject` -- client container, asserts non-null + state intact
- Scenario 5: `RemoteFetch_RemoteAuthBoolFalse_ReturnsNull` -- client container with [Service], asserts null

### Deviation Assessment

Developer used `new Authorized<T>()` instead of plan's `new Authorized<T>(default)`. This is **acceptable and correct**:
- `new Authorized<T>(default)` causes CS0121 ambiguity between `Authorized(Authorized)` and `Authorized(T?)` constructors
- `new Authorized<T>()` parameterless constructor calls base `Authorized()` which sets `HasAccess = false`; `Result` property defaults to `null` (default for reference types via `T? Result { get; init; }`)
- Runtime state is identical: `HasAccess = false`, `Result = null`
- This pattern is already established in the codebase at `RenderSaveLocalMethod` (line ~1083)

### Test Scenario Coverage

| Scenario # | Test Method | Covered? |
|------------|-------------|----------|
| 1 | `RemoteFetch_AuthBoolFalse_ServerReturnsNull` | YES |
| 2 | `RemoteFetch_AuthBoolFalse_ClientReturnsNull` | YES |
| 3 | `RemoteFetch_AuthBoolTrue_ReturnsObject` | YES |
| 4 | (Existing tests) | YES (no regression -- all 1984 tests pass) |
| 5 | `RemoteFetch_RemoteAuthBoolFalse_ReturnsNull` | YES |

### Issues Found

None.
