# Developer -- Fix Factory Fetch NRE on Not Found

Last updated: 2026-03-21
Current step: Implementation complete, awaiting verification (Step 8)

## Key Context

- Bug is in `ClassFactoryRenderer.cs` at two sites: read-style line 460 and write-style line 900
- Both emitted `return default!;` when `_succeeded == false` and `method.HasAuth` is true
- `default!` for `Authorized<T>` (reference type) is `null`
- Public wrapper at line 315 does `(await ...).Result` -- NRE when `.Result` is called on null
- Save public wrapper at line 1009 does `.HasAccess` -- same NRE pattern
- Fix: emit `new Authorized<T>()` instead of `default!` when `HasAuth` is true
- Used parameterless constructor `new Authorized<T>()` NOT `new Authorized<T>(default)` due to CS0121 ambiguity
- The save routing default at line 1083 already uses `new Authorized<T>()` -- established precedent

## Mistakes to Avoid

- Do NOT use `new Authorized<T>(default)` -- causes CS0121 ambiguity between `Authorized(Authorized)` and `Authorized(T?)` constructors
- Use `new Authorized<T>()` (parameterless) instead -- unambiguous and matches existing pattern

## User Corrections

- None

## Developer Review

**Status:** Approved
**Date:** 2026-03-21

(See plan file for full assertion trace table)

## Implementation Contract

(See plan file for full contract)

## Implementation Progress

All milestones complete:
1. Fixed read-style bool-false return (Site 1)
2. Fixed write-style bool-false return (Site 2)
3. Resolved CS0121 constructor ambiguity
4. Added FetchAuthAllow auth class and 3 test targets
5. Added 4 integration tests (scenarios 1, 2, 3, 5)
6. All 1984 tests pass, 0 failures

## Completion Evidence

- All tests pass across net9.0 and net10.0
- All 4 new tests pass
- All existing tests pass (no regression)
- Plan status set to "Awaiting Verification"

### Files Modified

1. `src/Generator/Renderer/ClassFactoryRenderer.cs` -- Fixed two bug sites
2. `src/Tests/RemoteFactory.IntegrationTests/TestTargets/FactoryRoundTrip/RoundTripTargets.cs` -- Added FetchAuthAllow + 3 test targets
3. `src/Tests/RemoteFactory.IntegrationTests/FactoryRoundTrip/RemoteFetchRoundTripTests.cs` -- Added 4 integration tests
