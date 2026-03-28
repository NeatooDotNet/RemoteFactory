# Developer -- Event DI Validation Fix

Last updated: 2026-03-27
Current step: Step 7 -- Implementation (completed, awaiting verification)

## Key Context

- **Bug 1**: `AddRemoteFactoryServices.cs:68` unconditionally registered `TryAddSingleton<IEventTracker, EventTracker>()`. `EventTracker` constructor requires `ILogger<EventTracker>`. On Blazor WASM clients without logging, DI validation fails. **Fixed**: Conditional registration -- NullEventTracker for Remote, EventTracker for Server/Logical.
- **Bug 2**: `ClassFactoryRenderer.RenderLocalEventRegistration` emitted `services.AddScoped<...>` directly without `IsServerRuntime` guard. **Fixed**: Wrapped entire body in `if (NeatooRuntime.IsServerRuntime) { ... }`, mirroring StaticFactoryRenderer.
- **NullEventTracker**: Created at `src/RemoteFactory/Internal/NullEventTracker.cs`. Internal sealed, no constructor deps. PendingCount=0, Track=no-op, WaitAllAsync=Task.CompletedTask.
- **TrimmingTests**: Removed `services.AddLogging()` from Program.cs. Program runs successfully with `ValidateOnBuild=true` and no logging configured.

## Mistakes to Avoid

(none encountered during implementation)

## User Corrections

(none)

## Developer Review

**Status:** Approved
**Date:** 2026-03-27

### Summary

The plan fixes two bugs that cause DI validation failures on Blazor WASM clients with `[Event]` factory methods: (1) unconditional `IEventTracker` registration requiring logging, and (2) missing `IsServerRuntime` guard on class factory event registrations. The approach is belt-and-suspenders: fix the runtime registration AND the generator output.

### Assertion Trace Verification

| # | Business Rule | Implementation Path | Expected Result | Verified? |
|---|--------------|---------------------|-----------------|-----------|
| 1 | WHEN `AddNeatooRemoteFactory(Remote)`, THEN `IEventTracker` resolves to `NullEventTracker` | `AddRemoteFactoryServices.AddNeatooRemoteFactory` -- `if (remoteLocal == NeatooFactory.Remote)` branch calls `services.TryAddSingleton<IEventTracker, NullEventTracker>()` | `NullEventTracker` instance | Yes |
| 2 | WHEN `AddNeatooRemoteFactory(Server)`, THEN `IEventTracker` resolves to `EventTracker` | `AddRemoteFactoryServices.AddNeatooRemoteFactory` -- `else` branch calls `services.TryAddSingleton<IEventTracker, EventTracker>()` | `EventTracker` instance | Yes |
| 3 | WHEN `AddNeatooRemoteFactory(Logical)`, THEN `IEventTracker` resolves to `EventTracker` | Same `else` branch as Rule 2 | `EventTracker` instance | Yes |
| 4 | WHEN Remote container built with ValidateOnBuild, no logging, no hosting, THEN builds successfully | `NullEventTracker` has zero constructor dependencies | No exception | Yes |
| 5 | WHEN Remote container, event delegates resolve as remote stubs | Generated registrar `if (remoteLocal == NeatooFactory.Remote)` block | Serializes to server | Yes |
| 6 | WHEN class factory with [Event], Logical/Server mode, THEN local event registration wrapped in `if (NeatooRuntime.IsServerRuntime)` | `ClassFactoryRenderer.RenderLocalEventRegistration` now emits `if (NeatooRuntime.IsServerRuntime)` guard | Guard present | Yes -- verified in generated output for EventTarget_Simple |
| 7 | WHEN static factory with [Event], THEN local event registration wrapped in `if (NeatooRuntime.IsServerRuntime)` | `StaticFactoryRenderer.RenderLocalEventRegistration` line 300 | Guard present | Yes -- unchanged, regression guard |
| 8 | WHEN `IsServerRuntime=false`, THEN class factory local event registrations eliminated by IL trimmer | Generated code wraps in `if (NeatooRuntime.IsServerRuntime)` with `[FeatureSwitchDefinition]` | Dead code removed | Yes |
| 9 | WHEN `NullEventTracker.Track(task)` called, THEN no-op | Empty method body `{ }` | No exception | Yes |
| 10 | WHEN `NullEventTracker.WaitAllAsync(ct)` called, THEN returns `Task.CompletedTask` | `=> Task.CompletedTask` | Immediate completion | Yes |
| 11 | WHEN `NullEventTracker.PendingCount` read, THEN returns 0 | `=> 0` | 0 | Yes |
| 12 | WHEN TrimmingTests runs without AddLogging, THEN service provider builds | NullEventTracker for Remote mode has no dependencies | Success | Yes |

---

## Implementation Contract

### Scope

**Create:**
- `src/RemoteFactory/Internal/NullEventTracker.cs` -- No-op IEventTracker implementation: PendingCount=0, Track=no-op, WaitAllAsync=Task.CompletedTask. Internal sealed, no constructor deps.
- `src/Tests/RemoteFactory.UnitTests/Internal/NullEventTrackerTests.cs` -- Unit tests for NullEventTracker (Scenarios 8-10).
- `src/Tests/RemoteFactory.UnitTests/Internal/EventTrackerRegistrationTests.cs` -- DI container validation tests for Remote/Server/Logical modes (Scenarios 1-4).

**Modify:**
- `src/RemoteFactory/AddRemoteFactoryServices.cs` line 68 -- Replace unconditional `TryAddSingleton<IEventTracker, EventTracker>()` with conditional: NullEventTracker for Remote, EventTracker for Server/Logical.
- `src/Generator/Renderer/ClassFactoryRenderer.cs` method `RenderLocalEventRegistration` -- Wrap entire body in `if (NeatooRuntime.IsServerRuntime) { ... }` guard, mirroring StaticFactoryRenderer.
- `src/Tests/RemoteFactory.TrimmingTests/Program.cs` -- Remove `services.AddLogging()` call.

### Out of Scope

- `src/Tests/RemoteFactory.IntegrationTests/Events/RemoteEventIntegrationTests.cs` -- Must not be modified; regression guard.
- `src/Tests/RemoteFactory.IntegrationTests/Events/CorrelationEventPropagationTests.cs` -- Must not be modified; regression guard.
- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Events/EventGenerationTests.cs` -- Must not be modified.
- `src/Tests/RemoteFactory.UnitTests/Internal/EventTrackerTests.cs` -- Must not be modified.
- `docs/events.md` -- Documentation update deferred to Step 9.

### Tests to Add

- `NullEventTrackerTests`: Track no-op, WaitAllAsync immediate, PendingCount zero (Scenarios 8-10)
- `EventTrackerRegistrationTests`: Remote resolves NullEventTracker, Server resolves EventTracker, Logical resolves EventTracker, Remote builds without logging/hosting with ValidateOnBuild=true (Scenarios 1-4)

### Test Scenario Mapping

| # | Plan Scenario | Test Method | File |
|---|--------------|-------------|------|
| 1 | Remote-mode resolves NullEventTracker | `RemoteMode_Resolves_NullEventTracker` | EventTrackerRegistrationTests.cs |
| 2 | Server-mode resolves EventTracker | `ServerMode_Resolves_EventTracker` | EventTrackerRegistrationTests.cs |
| 3 | Logical-mode resolves EventTracker | `LogicalMode_Resolves_EventTracker` | EventTrackerRegistrationTests.cs |
| 4 | Remote container builds without logging/hosting | `RemoteMode_BuildsWithValidateOnBuild_WithoutLoggingAndHosting` | EventTrackerRegistrationTests.cs |
| 5 | Remote event delegates resolve as stubs | Covered by existing `RemoteEventIntegrationTests` | (existing) |
| 6 | Class factory event has IsServerRuntime guard | Verified by inspecting generated `EventTarget_SimpleFactory.g.cs` line 95 | Generated output inspection |
| 7 | Static factory event retains guard | Regression -- verified by TrimmingTests build | (existing) |
| 8 | NullEventTracker Track is no-op | `Track_IsNoOp_DoesNotThrow` | NullEventTrackerTests.cs |
| 9 | NullEventTracker WaitAllAsync returns immediately | `WaitAllAsync_ReturnsCompletedTask` | NullEventTrackerTests.cs |
| 10 | NullEventTracker PendingCount is zero | `PendingCount_ReturnsZero` | NullEventTrackerTests.cs |
| 11 | TrimmingTests without AddLogging | Removed AddLogging from Program.cs, ran `dotnet run` | TrimmingTests/Program.cs |
| 12 | Existing event tests pass | Full test suite green | (existing) |

### Verification Gates

1. After Phase 1 (NullEventTracker + AddRemoteFactoryServices change): `dotnet build` -- PASSED
2. After Phase 2 (ClassFactoryRenderer guard): `dotnet build` + inspected generated code -- PASSED
3. After Phase 3 (tests): 10 new tests pass on both TFMs -- PASSED
4. After TrimmingTests update: `dotnet run` outputs "ServiceProvider built successfully" -- PASSED
5. Final: `dotnet test src/Neatoo.RemoteFactory.sln` -- full suite green -- PASSED

### Stop Conditions

None triggered. All existing event tests passed without modification.

---

## Implementation Progress

### Phase 1: Core Library Fix (Bug 1) -- COMPLETE
- Created `src/RemoteFactory/Internal/NullEventTracker.cs` -- internal sealed, implements IEventTracker with no-op behavior, zero constructor dependencies.
- Modified `src/RemoteFactory/AddRemoteFactoryServices.cs` -- replaced unconditional `TryAddSingleton<IEventTracker, EventTracker>()` with conditional: NullEventTracker for Remote, EventTracker for else (Server/Logical).
- Build succeeded: 0 errors.

### Phase 2: Generator Fix (Bug 2) -- COMPLETE
- Modified `src/Generator/Renderer/ClassFactoryRenderer.cs` method `RenderLocalEventRegistration` -- wrapped entire body in `if (NeatooRuntime.IsServerRuntime) { ... }` guard with 4-space indent increase (16->20 spaces for inner content), mirroring `StaticFactoryRenderer.RenderLocalEventRegistration`.
- Also updated `serviceAssignments` join separator from `\n                            ` to `\n                                ` to match new indentation level.
- Build succeeded: 0 errors.
- Verified generated output: `EventTarget_SimpleFactory.g.cs` line 95 shows `if (NeatooRuntime.IsServerRuntime)` guard wrapping the `services.AddScoped<EventTarget_Simple.FireSimpleEvent>` registration.

### Phase 3: Tests -- COMPLETE
- Created `src/Tests/RemoteFactory.UnitTests/Internal/NullEventTrackerTests.cs` with 6 tests:
  - `PendingCount_ReturnsZero`, `Track_IsNoOp_DoesNotThrow`, `WaitAllAsync_ReturnsCompletedTask`, `WaitAllAsync_WithCancellationToken_ReturnsCompletedTask`, `Track_CompletedTask_DoesNotThrow`, `Track_FaultedTask_DoesNotThrow`
- Created `src/Tests/RemoteFactory.UnitTests/Internal/EventTrackerRegistrationTests.cs` with 4 tests:
  - `RemoteMode_Resolves_NullEventTracker`, `ServerMode_Resolves_EventTracker`, `LogicalMode_Resolves_EventTracker`, `RemoteMode_BuildsWithValidateOnBuild_WithoutLoggingAndHosting`
- All 10 new tests pass on both net9.0 and net10.0.

### Phase 4: TrimmingTests + Full Suite -- COMPLETE
- Removed `services.AddLogging()` and unused `using Microsoft.Extensions.Logging` from `src/Tests/RemoteFactory.TrimmingTests/Program.cs`.
- TrimmingTests runs successfully: "ServiceProvider built successfully with ValidateOnBuild=true." and "Event delegate resolved: True".
- Full test suite: 500 UnitTests passed + 502 IntegrationTests passed (both TFMs), 0 failures.

---

## Completion Evidence

### Test Results

**Unit Tests (net9.0):** 500 passed, 0 failed
**Unit Tests (net10.0):** 500 passed, 0 failed
**Integration Tests (net9.0):** 502 passed, 0 failed, 3 skipped (performance tests, pre-existing skip)
**Integration Tests (net10.0):** 502 passed, 0 failed, 3 skipped (performance tests, pre-existing skip)

### New Tests: 10 (all passing on both TFMs = 20 total executions)

**NullEventTrackerTests (6 tests):**
- `PendingCount_ReturnsZero` -- verifies constant 0 return
- `Track_IsNoOp_DoesNotThrow` -- verifies running task is not tracked, PendingCount stays 0
- `WaitAllAsync_ReturnsCompletedTask` -- verifies immediate completion
- `WaitAllAsync_WithCancellationToken_ReturnsCompletedTask` -- verifies immediate completion with CT
- `Track_CompletedTask_DoesNotThrow` -- verifies no exception on completed task
- `Track_FaultedTask_DoesNotThrow` -- verifies no exception on faulted task

**EventTrackerRegistrationTests (4 tests):**
- `RemoteMode_Resolves_NullEventTracker` -- Remote -> NullEventTracker
- `ServerMode_Resolves_EventTracker` -- Server -> EventTracker (with logging)
- `LogicalMode_Resolves_EventTracker` -- Logical -> EventTracker (with logging)
- `RemoteMode_BuildsWithValidateOnBuild_WithoutLoggingAndHosting` -- ValidateOnBuild=true, no logging, no hosting -> succeeds

### TrimmingTests Output (without AddLogging)
```
ServiceProvider built successfully with ValidateOnBuild=true.
Class factory resolution FAILED: InvalidOperationException: Cannot resolve scoped service from root provider. (pre-existing, unrelated)
Client mode - server-only code trimmed.
IsServerRuntime: False
Class factory resolved: False
Static factory delegate resolved: True
Event delegate resolved: True
Trimming verification app completed.
```

### Generated Code Verification
`EventTarget_SimpleFactory.g.cs` line 95: `if (NeatooRuntime.IsServerRuntime)` guard present around `services.AddScoped<EventTarget_Simple.FireSimpleEvent>` registration.

### Contract Status: All items complete
- [x] NullEventTracker.cs created
- [x] AddRemoteFactoryServices.cs conditionally registers NullEventTracker/EventTracker
- [x] ClassFactoryRenderer.RenderLocalEventRegistration wrapped in IsServerRuntime guard
- [x] NullEventTrackerTests.cs created with 6 tests
- [x] EventTrackerRegistrationTests.cs created with 4 tests
- [x] TrimmingTests Program.cs updated (AddLogging removed)
- [x] All new tests pass
- [x] All existing tests pass (zero regressions)
- [x] No out-of-scope tests modified

### Files Changed
**Created (3):**
- `src/RemoteFactory/Internal/NullEventTracker.cs`
- `src/Tests/RemoteFactory.UnitTests/Internal/NullEventTrackerTests.cs`
- `src/Tests/RemoteFactory.UnitTests/Internal/EventTrackerRegistrationTests.cs`

**Modified (3):**
- `src/RemoteFactory/AddRemoteFactoryServices.cs` (conditional IEventTracker registration)
- `src/Generator/Renderer/ClassFactoryRenderer.cs` (IsServerRuntime guard in RenderLocalEventRegistration)
- `src/Tests/RemoteFactory.TrimmingTests/Program.cs` (removed AddLogging)
