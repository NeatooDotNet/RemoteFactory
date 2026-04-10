# Requirements Reviewer -- Event DI Validation Fix

Last updated: 2026-03-27
Current step: Post-implementation requirements verification (Step 8B) complete

## Key Context

This plan fixes two bugs: (1) unconditional `EventTracker` registration causes DI validation failures on Blazor WASM clients without logging, and (2) missing `IsServerRuntime` guard on class factory event registrations breaks IL trimming parity with static factory event registrations.

The fix introduces `NullEventTracker` for Remote mode and adds the `IsServerRuntime` guard to `ClassFactoryRenderer.RenderLocalEventRegistration`.

## Mistakes to Avoid

None encountered during this review.

## User Corrections

None.

## Requirements Verification

**Verdict:** REQUIREMENTS SATISFIED
**Date:** 2026-03-27

### Compliance Table

| # | Requirement | Source | Status | Notes |
|---|------------|--------|--------|-------|
| 1 | `[Remote]` is only for aggregate root entry points | `CLAUDE-DESIGN.md` Critical Rule 1 | Not Affected | No `[Remote]` annotations were added or changed |
| 2 | Internal methods get IsServerRuntime guards | `CLAUDE-DESIGN.md` lines 444-479; `docs/trimming.md` lines 17-36 | Satisfied | `ClassFactoryRenderer.RenderLocalEventRegistration` now emits `if (NeatooRuntime.IsServerRuntime)` guard (lines 1599-1635), matching `StaticFactoryRenderer` (lines 299-335) |
| 3 | Event infrastructure is server-only in Remote mode | `docs/events.md` lines 499-509; `CLAUDE-DESIGN.md` lines 40, 60-61 | Satisfied | Remote-mode clients get `NullEventTracker` (no-op); local event registrations guarded by `IsServerRuntime`; remote event stubs use `ForDelegateEvent` (unchanged) |
| 4 | IEventTracker is a public interface, always resolvable | `src/RemoteFactory/IEventTracker.cs`; `docs/events.md` lines 229-278 | Satisfied | `NullEventTracker` registered for Remote mode preserves the public API contract. `GetRequiredService<IEventTracker>()` succeeds in all modes |
| 5 | EventTrackerHostedService is ASP.NET Core only | `src/RemoteFactory.AspNetCore/ServiceCollectionExtensions.cs` lines 42-48 | Not Affected | `AddNeatooAspNetCore` passes `NeatooFactory.Server`, so `EventTracker` (not `NullEventTracker`) is registered; hosted service resolves `IEventTracker` which gets the full implementation |
| 6 | Static factory event registrations have IsServerRuntime guard | `StaticFactoryRenderer.cs` lines 299-335 | Satisfied (Regression) | Existing guard preserved unchanged |
| 7 | Class factory event registrations have IsServerRuntime guard | Plan Rule 6; Gap 3 parity requirement | Satisfied | New guard added at `ClassFactoryRenderer.cs` lines 1599-1635, structurally identical to static factory pattern |
| 8 | `TryAddSingleton` semantics preserved | `AddRemoteFactoryServices.cs` line 73/77 | Satisfied | Conditional registration still uses `TryAddSingleton`, respecting user overrides |
| 9 | NullEventTracker: no constructor dependencies | Plan Rule 9 | Satisfied | `NullEventTracker` has no constructor, no fields, no dependencies (`src/RemoteFactory/Internal/NullEventTracker.cs`) |
| 10 | NullEventTracker: Track is no-op | Plan Rule 9 | Satisfied | `Track(Task)` has empty body; verified by `NullEventTrackerTests.Track_IsNoOp_DoesNotThrow` |
| 11 | NullEventTracker: WaitAllAsync returns immediately | Plan Rule 10 | Satisfied | Returns `Task.CompletedTask`; verified by `NullEventTrackerTests.WaitAllAsync_ReturnsCompletedTask` |
| 12 | NullEventTracker: PendingCount returns 0 | Plan Rule 11 | Satisfied | Property returns `0`; verified by `NullEventTrackerTests.PendingCount_ReturnsZero` |
| 13 | TrimmingTests works without AddLogging | Plan Rule 12 | Satisfied | `AddLogging()` removed from `TrimmingTests/Program.cs`; developer reports successful run |
| 14 | No Design Debt features implemented | `CLAUDE-DESIGN.md` Design Debt table (lines 732-738) | Not Affected | No overlap between bug fixes and deferred features |
| 15 | Generator targets netstandard2.0 | `CLAUDE.md` Architecture Notes | Satisfied | `ClassFactoryRenderer.cs` changes are string manipulation only, no API changes |
| 16 | Existing tests not modified | CLAUDE.md "Existing Tests Are Sacred" | Satisfied | Developer confirms zero out-of-scope test modifications; 500 unit + 502 integration tests pass per TFM |
| 17 | `partial` keyword requirement | `CLAUDE-DESIGN.md` Quick Decisions | Not Affected | No class definitions changed |

### Unintended Side Effects

**None identified.**

1. **ASP.NET Core path unaffected.** `AddNeatooAspNetCore` calls `AddNeatooRemoteFactory(NeatooFactory.Server, ...)`, which registers the full `EventTracker`. The `EventTrackerHostedService` continues to resolve `IEventTracker` as `EventTracker` with logging support.

2. **Serialization contracts unaffected.** No changes to serializable types, `IOrdinalSerializable`, or `NeatooJsonSerializer`. The `NullEventTracker` is a DI registration change only; it does not cross the client/server boundary.

3. **Generated factory interface contracts unaffected.** The `IsServerRuntime` guard wraps internal event registration code in `FactoryServiceRegistrar`, not the factory interface itself. No breaking changes to generated interfaces.

4. **Documentation accuracy.** `docs/events.md` shows `GetRequiredService<IEventTracker>()` patterns that still work in all modes. Remote-mode clients get `NullEventTracker` where `PendingCount` is always 0 and `WaitAllAsync` returns immediately -- this is correct behavior since events serialize to the server in Remote mode. The plan notes a documentation deliverable for Step 9 to document this distinction.

5. **Published docs (`docs/trimming.md`) technically understated.** Line 35 says "Static factories -- Delegate and event registrations are guarded" but does not explicitly mention class factory event registrations. After this fix, class factory event registrations are also guarded. This is an improvement that makes the docs slightly incomplete (docs say static; reality is now static AND class). Not a contradiction -- the fix makes the implementation more consistent. Documentation update deferred to Step 9 per plan.

### Issues Found

None. All implementation choices align with documented requirements, patterns, and behavioral contracts.
