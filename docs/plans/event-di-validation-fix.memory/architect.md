# Architect -- Event DI Validation Fix

Last updated: 2026-03-27
Current step: Post-implementation verification complete (Step 8A). Verdict: VERIFIED.

## Key Context

### Bug 1: IEventTracker Registration
- `AddRemoteFactoryServices.cs:68` registered `TryAddSingleton<IEventTracker, EventTracker>()` unconditionally
- `EventTracker` constructor requires `ILogger<EventTracker>` -- failed DI validation on Blazor WASM without logging
- Fix: conditional registration -- `NullEventTracker` for Remote, `EventTracker` for Server/Logical
- `IEventTracker` is public and documented (`docs/events.md`), so it must be resolvable in all modes

### Bug 2: Missing IsServerRuntime Guard
- `ClassFactoryRenderer.RenderLocalEventRegistration` lacked `if (NeatooRuntime.IsServerRuntime)` guard
- `StaticFactoryRenderer.RenderLocalEventRegistration` already had the guard
- Fix: added identical guard wrapping to class factory renderer

### Design Decisions
- **NullEventTracker over removal**: IEventTracker is public, documented with `GetRequiredService` samples in `docs/events.md`. Removing registration breaks user code that injects IEventTracker on client side.
- **Belt-and-suspenders**: User confirmed (Clarifications A1): fix both bugs.
- **TryAddSingleton preserved**: Conditional registration still uses `TryAddSingleton` so user overrides before `AddNeatooRemoteFactory` are respected.

## Mistakes to Avoid

(none)

## User Corrections

- User confirmed belt-and-suspenders approach: fix both bugs (Clarifications A1 in todo)

## Architectural Verification (Pre-Handoff)

### Scope Table

| Component | Files | Change Type |
|-----------|-------|-------------|
| NullEventTracker | `src/RemoteFactory/Internal/NullEventTracker.cs` | NEW |
| AddRemoteFactoryServices | `src/RemoteFactory/AddRemoteFactoryServices.cs:68` | MODIFY (conditional registration) |
| ClassFactoryRenderer | `src/Generator/Renderer/ClassFactoryRenderer.cs:1583-1636` | MODIFY (add IsServerRuntime guard) |
| TrimmingTests | `src/Tests/RemoteFactory.TrimmingTests/Program.cs` | MODIFY (remove AddLogging) |
| Unit Tests (NullEventTracker) | `src/Tests/RemoteFactory.UnitTests/Internal/NullEventTrackerTests.cs` | NEW |
| Unit Tests (Registration) | `src/Tests/RemoteFactory.UnitTests/Internal/EventTrackerRegistrationTests.cs` | NEW |

### Breaking Changes

None. The public API contract is preserved.

## Architect Verification (Post-Implementation)

### Verdict: VERIFIED

All builds pass, all tests pass, all 12 test scenarios have corresponding verification. Implementation matches the plan design.

### Independent Build Verification

- **`dotnet build src/Neatoo.RemoteFactory.sln`**: Build succeeded. 0 errors, 2 warnings (both are pre-existing Blazor WASM NativeFileReference warnings, unrelated to this change).

### Independent Test Verification

- **UnitTests net9.0**: 500 passed, 0 failed, 0 skipped
- **UnitTests net10.0**: 500 passed, 0 failed, 0 skipped
- **IntegrationTests net9.0**: 502 passed, 0 failed, 3 skipped (pre-existing ShowcasePerformanceTests)
- **IntegrationTests net10.0**: 502 passed, 0 failed, 3 skipped (pre-existing ShowcasePerformanceTests)
- **Total: 2004 passed, 0 failed, 6 skipped** (all skipped are pre-existing)

### TrimmingTests Verification

- **`dotnet run --project src/Tests/RemoteFactory.TrimmingTests/RemoteFactory.TrimmingTests.csproj`**: Completed successfully
- Output: "ServiceProvider built successfully with ValidateOnBuild=true."
- `AddLogging()` is NOT present in `Program.cs` (confirmed by reading the file)
- All resolution checks pass: static factory delegate resolved, event delegate resolved
- Class factory resolution failure is pre-existing (scoped-from-root, unrelated to this fix)

### Implementation Design Match

1. **NullEventTracker** (`src/RemoteFactory/Internal/NullEventTracker.cs`):
   - `internal sealed class NullEventTracker : IEventTracker` -- matches plan exactly
   - No constructor dependencies -- verified (no constructor defined, default parameterless)
   - `PendingCount => 0`, `Track(Task)` is no-op, `WaitAllAsync()` returns `Task.CompletedTask` -- all match plan

2. **AddRemoteFactoryServices.cs** (lines 71-78):
   - `if (remoteLocal == NeatooFactory.Remote)` registers `NullEventTracker`, else registers `EventTracker` -- matches plan exactly
   - Uses `TryAddSingleton` for both paths -- preserves existing override semantics

3. **ClassFactoryRenderer.cs** `RenderLocalEventRegistration` (lines 1598-1635):
   - `sb.AppendLine("                if (NeatooRuntime.IsServerRuntime)");` at line 1599
   - Entire `services.AddScoped<...>` registration wrapped inside the guard
   - Closing `}` at line 1635
   - Pattern matches `StaticFactoryRenderer.RenderLocalEventRegistration` (line 300)

4. **StaticFactoryRenderer.cs** (line 300):
   - `IsServerRuntime` guard still present -- regression check passed

### Test Scenario Coverage: 12 of 12 verified

| # | Scenario | Verification Method | Result |
|---|----------|-------------------|--------|
| 1 | Remote-mode resolves NullEventTracker | `EventTrackerRegistrationTests.RemoteMode_Resolves_NullEventTracker()` -- test exists, asserts `IsType<NullEventTracker>` | PASS (500 unit tests pass) |
| 2 | Server-mode resolves EventTracker | `EventTrackerRegistrationTests.ServerMode_Resolves_EventTracker()` -- test exists, asserts `IsType<EventTracker>` | PASS |
| 3 | Logical-mode resolves EventTracker | `EventTrackerRegistrationTests.LogicalMode_Resolves_EventTracker()` -- test exists, asserts `IsType<EventTracker>` | PASS |
| 4 | Remote-mode container builds without logging/hosting | `EventTrackerRegistrationTests.RemoteMode_BuildsWithValidateOnBuild_WithoutLoggingAndHosting()` -- test exists, uses `ValidateOnBuild=true` with no `AddLogging()` and no `IHostApplicationLifetime` | PASS |
| 5 | Remote event delegates resolve as stubs | Existing integration tests (`RemoteEventIntegrationTests`, `CorrelationEventPropagationTests`) -- 12 tests per TFM, all pass | PASS |
| 6 | Generated class factory event has IsServerRuntime guard | Code inspection: `ClassFactoryRenderer.cs` line 1599 emits `if (NeatooRuntime.IsServerRuntime)` wrapping entire `AddScoped` block | VERIFIED (code inspection) |
| 7 | Generated static factory event retains IsServerRuntime guard | Code inspection: `StaticFactoryRenderer.cs` line 300 still has `if (NeatooRuntime.IsServerRuntime)` | VERIFIED (regression guard intact) |
| 8 | NullEventTracker Track is no-op | `NullEventTrackerTests.Track_IsNoOp_DoesNotThrow()` + `Track_CompletedTask_DoesNotThrow()` + `Track_FaultedTask_DoesNotThrow()` | PASS |
| 9 | NullEventTracker WaitAllAsync returns immediately | `NullEventTrackerTests.WaitAllAsync_ReturnsCompletedTask()` + `WaitAllAsync_WithCancellationToken_ReturnsCompletedTask()` | PASS |
| 10 | NullEventTracker PendingCount is zero | `NullEventTrackerTests.PendingCount_ReturnsZero()` | PASS |
| 11 | TrimmingTests builds without AddLogging | `dotnet run` output: "ServiceProvider built successfully with ValidateOnBuild=true." -- `AddLogging()` removed from Program.cs | PASS |
| 12 | Existing remote event tests still pass | Filtered test run: 12 passed per TFM (24 total), 0 failed | PASS |

### Notes

- Scenarios 6-7 are verified by code inspection of the renderer source, not by dedicated generator output tests. This is consistent with the plan which stated "Generator output inspection" as the verification method.
- The developer created 6 `NullEventTracker` tests (exceeding the 3 scenarios in the plan) including edge cases for completed tasks, faulted tasks, and cancellation tokens. These are good additions.
- No out-of-scope tests were modified. The only test modification was removing `AddLogging()` from `TrimmingTests/Program.cs`, which is in-scope.
