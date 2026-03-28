# Fix Event DI Validation Failure on Blazor WASM Client

**Date:** 2026-03-27
**Related Todo:** [Event DI Validation Failure on Blazor WASM Client](../todos/event-di-validation-blazor-wasm.md)
**Status:** Complete
**Last Updated:** 2026-03-27

---

## Overview

Two bugs cause DI validation failures when a Blazor WASM client references a domain assembly containing `[Event]` factory methods:

1. **Bug 1 (IEventTracker Registration):** `IEventTracker` is unconditionally registered in `AddRemoteFactoryServices.cs:68` as `TryAddSingleton<IEventTracker, EventTracker>`. `EventTracker` requires `ILogger<EventTracker>` in its constructor. On Blazor WASM clients that use `ValidateOnBuild=true` without logging configured, the DI container walks the dependency graph, cannot construct `EventTracker`, and throws `AggregateException`. This cascades to report unrelated services as unresolvable.

2. **Bug 2 (Missing IsServerRuntime Guard):** `ClassFactoryRenderer.RenderLocalEventRegistration` generates event delegate registrations that resolve `IEventTracker`, `IHostApplicationLifetime`, and `IServiceScopeFactory` without wrapping them in `if (NeatooRuntime.IsServerRuntime)`. The equivalent method in `StaticFactoryRenderer` already has this guard (lines 299-335). Without the guard, the IL trimmer cannot eliminate these server-only registrations from client assemblies.

The fix applies belt-and-suspenders: conditionally register `IEventTracker` (Bug 1) AND add the `IsServerRuntime` guard to class factory event registration (Bug 2).

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/event-di-validation-blazor-wasm.md#requirements-review)

### Relevant Existing Requirements

#### Business Rules

- **IsServerRuntime guard pattern** (`src/Design/CLAUDE-DESIGN.md` lines 444-479; `docs/trimming.md` lines 15-38): Internal and `[Remote]` methods get `if (NeatooRuntime.IsServerRuntime)` guards so the IL trimmer eliminates server-only code. Static factory event registrations already follow this pattern. Class factory event registrations do not.

- **Event infrastructure is server-only** (`docs/events.md` lines 499-509; `src/Design/CLAUDE-DESIGN.md` lines 40, 60-61): Events in Remote mode serialize to the server. The local event registration (scope isolation, `Task.Run`, `IHostApplicationLifetime`, `IEventTracker`) only runs in Server/Logical mode. Generated code already branches on `remoteLocal` for remote vs local.

- **IEventTracker is a public interface** (`src/RemoteFactory/IEventTracker.cs`; `docs/events.md` lines 229-278): `IEventTracker` is documented as available for monitoring (PendingCount, WaitAllAsync). Callers inject it via constructor injection. The documentation shows `GetRequiredService<IEventTracker>()` usage in test and monitoring samples.

- **EventTrackerHostedService is ASP.NET Core only** (`src/RemoteFactory.AspNetCore/ServiceCollectionExtensions.cs:42-48`): The hosted service resolves `IEventTracker` via `GetRequiredService`. On the server (which always has logging), this works. On Remote-mode clients, `IEventTracker` is not needed by generated remote event stubs (they serialize via `IMakeRemoteDelegateRequest.ForDelegateEvent`).

#### Existing Tests

- **Integration event tests** (`src/Tests/RemoteFactory.IntegrationTests/Events/RemoteEventIntegrationTests.cs`): 5 tests verify remote, local, and fire-and-forget event semantics. All use `ClientServerContainers.Scopes()` which always registers `IHostApplicationLifetime` and logging for all containers, masking the bug.

- **Correlation event tests** (`src/Tests/RemoteFactory.IntegrationTests/Events/CorrelationEventPropagationTests.cs`): Verify correlation ID propagation. Also use full-featured containers.

- **TrimmingTests project** (`src/Tests/RemoteFactory.TrimmingTests/`): Console app with `PublishTrimmed=true`, `IsServerRuntime=false`, `ValidateOnBuild=true`. Already has a static `[Remote, Event]` method (`TrimTestCommands._OnWorkCompleted`). Currently passes because it registers logging (`services.AddLogging()`) which satisfies `EventTracker`'s `ILogger<EventTracker>` dependency.

### Gaps

1. **No documented requirement for conditional IEventTracker registration.** After the fix, Remote-mode clients will not have `IEventTracker` registered. If user code on the client resolves `IEventTracker` directly, it will fail. Decision: register a `NullEventTracker` on Remote-mode clients (see Approach).

2. **No existing test validates Remote-mode DI without IHostApplicationLifetime and logging.** The test containers all register these services. A new test must validate a Remote-mode container builds successfully without them.

3. **No explicit requirement documenting parity between static and class factory event rendering.** The asymmetry is undocumented. After the fix, parity becomes an invariant.

### Contradictions

None. Both fixes align with documented patterns.

### Recommendations for Architect

- Register a no-op `IEventTracker` on Remote-mode clients rather than removing the registration entirely, since the interface is public and documented.
- Mirror the exact `StaticFactoryRenderer.RenderLocalEventRegistration` guard pattern for the class factory renderer.
- Add a test that creates a Remote-mode container WITHOUT `IHostApplicationLifetime` and WITHOUT logging, validates the container builds, and verifies event delegates resolve as remote stubs.
- Consider whether test containers should stop registering `IHostApplicationLifetime` for client mode to prevent masking this class of bug in the future.

---

## Business Rules (Testable Assertions)

1. WHEN `AddNeatooRemoteFactory` is called with `NeatooFactory.Remote`, THEN `IEventTracker` resolves to a no-op implementation (not `EventTracker`). Expected: `NullEventTracker` instance. -- Source: NEW (Gap 1; reviewer recommendation for public interface preservation)

2. WHEN `AddNeatooRemoteFactory` is called with `NeatooFactory.Server`, THEN `IEventTracker` resolves to `EventTracker`. Expected: `EventTracker` instance. -- Source: Existing behavior (`AddRemoteFactoryServices.cs:68`; `docs/events.md` line 349)

3. WHEN `AddNeatooRemoteFactory` is called with `NeatooFactory.Logical`, THEN `IEventTracker` resolves to `EventTracker`. Expected: `EventTracker` instance. -- Source: Existing behavior (`docs/events.md` line 509: "Still uses EventTracker and scope isolation")

4. WHEN a Remote-mode DI container is built with `ValidateOnBuild=true`, WITHOUT `IHostApplicationLifetime` registered, and WITHOUT logging configured, THEN the container builds successfully (no `AggregateException`). -- Source: NEW (Gap 2; the bug being fixed)

5. WHEN a Remote-mode DI container has `[Event]` factory methods, THEN event delegates resolve as remote stubs (using `IMakeRemoteDelegateRequest.ForDelegateEvent`), NOT as local event registrations. Expected: delegate invocation serializes to server. -- Source: Existing behavior (`docs/events.md` lines 502-505)

6. WHEN a class factory has `[Event]` methods AND `remoteLocal == NeatooFactory.Logical || remoteLocal == NeatooFactory.Server`, THEN the generated local event registration is wrapped in `if (NeatooRuntime.IsServerRuntime)`. -- Source: Trimming pattern (`docs/trimming.md` lines 34-35; `src/Design/CLAUDE-DESIGN.md` lines 444-479; Gap 3 parity with `StaticFactoryRenderer`)

7. WHEN a static factory has `[Event]` methods AND `remoteLocal == NeatooFactory.Logical || remoteLocal == NeatooFactory.Server`, THEN the generated local event registration is wrapped in `if (NeatooRuntime.IsServerRuntime)`. -- Source: Already implemented (`StaticFactoryRenderer.cs:299-335`; regression guard)

8. WHEN `NeatooRuntime.IsServerRuntime` is `false` (trimming scenario), THEN generated class factory local event registrations are eliminated by the IL trimmer, identical to static factory behavior. -- Source: `docs/trimming.md` line 35; Gap 3

9. WHEN `NullEventTracker.Track(task)` is called, THEN the task is not tracked and no exception is thrown. Expected: no-op. -- Source: NEW (implementation detail of Gap 1)

10. WHEN `NullEventTracker.WaitAllAsync(ct)` is called, THEN it returns `Task.CompletedTask` immediately. Expected: immediate completion. -- Source: NEW (implementation detail of Gap 1)

11. WHEN `NullEventTracker.PendingCount` is read, THEN it returns `0`. Expected: `0`. -- Source: NEW (implementation detail of Gap 1)

12. WHEN the TrimmingTests project runs with `ValidateOnBuild=true` and `AddLogging()` is removed, THEN the service provider still builds successfully. -- Source: NEW (validates Bug 1 fix end-to-end in trimming scenario)

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Remote-mode resolves NullEventTracker | `AddNeatooRemoteFactory(Remote, assembly)`, resolve `IEventTracker` | Rule 1 | Instance is `NullEventTracker`, not `EventTracker` |
| 2 | Server-mode resolves EventTracker | `AddNeatooRemoteFactory(Server, assembly)`, resolve `IEventTracker` | Rule 2 | Instance is `EventTracker` (requires logging) |
| 3 | Logical-mode resolves EventTracker | `AddNeatooRemoteFactory(Logical, assembly)`, resolve `IEventTracker` | Rule 3 | Instance is `EventTracker` (requires logging) |
| 4 | Remote-mode container builds without logging/hosting | `AddNeatooRemoteFactory(Remote, assembly)` with NO `AddLogging()` and NO `IHostApplicationLifetime`, `ValidateOnBuild=true` | Rule 4 | `BuildServiceProvider()` succeeds, no exception |
| 5 | Remote event delegates resolve as stubs | Remote-mode container, resolve `OrderEventTarget.SendOrderConfirmationEvent`, invoke | Rule 5 | Delegate calls `IMakeRemoteDelegateRequest.ForDelegateEvent` |
| 6 | Generated class factory event has IsServerRuntime guard | Build project with class `[Factory]` having `[Event]` method, inspect generated registrar | Rule 6 | `if (NeatooRuntime.IsServerRuntime)` wraps `services.AddScoped<...Event>` for local event |
| 7 | Generated static factory event retains IsServerRuntime guard | Build project with static `[Factory]` having `[Event]` method, inspect generated registrar | Rule 7 | `if (NeatooRuntime.IsServerRuntime)` wraps `services.AddScoped<...Event>` for local event (regression) |
| 8 | NullEventTracker Track is no-op | Call `Track(someTask)` on `NullEventTracker` | Rule 9 | No exception, task not tracked |
| 9 | NullEventTracker WaitAllAsync returns immediately | Call `WaitAllAsync()` on `NullEventTracker` | Rule 10 | Returns completed task |
| 10 | NullEventTracker PendingCount is zero | Read `PendingCount` on `NullEventTracker` | Rule 11 | Returns 0 |
| 11 | TrimmingTests builds without AddLogging | TrimmingTests with `AddLogging()` removed, `ValidateOnBuild=true` | Rule 12 | Program completes successfully, outputs "ServiceProvider built successfully" |
| 12 | Existing remote event tests still pass | Run all tests in `RemoteEventIntegrationTests` and `CorrelationEventPropagationTests` | Rules 5,6,7 | All existing tests pass (regression) |

---

## Approach

### Fix Strategy

**Bug 1 -- Conditional IEventTracker Registration:**

Replace the unconditional `TryAddSingleton<IEventTracker, EventTracker>()` in `AddRemoteFactoryServices.cs:68` with a conditional registration:
- `NeatooFactory.Remote` --> Register `NullEventTracker` (new class, implements `IEventTracker`, no-op)
- `NeatooFactory.Server` or `NeatooFactory.Logical` --> Register `EventTracker` (existing, requires `ILogger<EventTracker>`)

This preserves the public API contract: `IEventTracker` is always resolvable regardless of mode. Remote-mode clients get a no-op that has no constructor dependencies (no `ILogger` required). Server/Logical modes continue to get the full `EventTracker` with logging.

**Why NullEventTracker instead of removing registration:** `IEventTracker` is a public interface documented in `docs/events.md` with `GetRequiredService<IEventTracker>()` samples. User code that injects `IEventTracker` on the client side (e.g., in a shared caller class) would break with a missing-registration exception. A no-op preserves the contract while eliminating the logging dependency on the client.

**Bug 2 -- Add IsServerRuntime Guard to Class Factory Event Registration:**

Wrap the body of `ClassFactoryRenderer.RenderLocalEventRegistration` (lines 1598-1631) in `if (NeatooRuntime.IsServerRuntime) { ... }`, mirroring the pattern already used in `StaticFactoryRenderer.RenderLocalEventRegistration` (lines 299-335). This allows the IL trimmer to eliminate class factory local event registrations on client assemblies.

### NullEventTracker Design

```
internal sealed class NullEventTracker : IEventTracker
{
    public int PendingCount => 0;
    public void Track(Task eventTask) { }
    public Task WaitAllAsync(CancellationToken ct = default) => Task.CompletedTask;
}
```

Location: `src/RemoteFactory/Internal/NullEventTracker.cs` (alongside `EventTracker.cs`)

### Registration Change in AddRemoteFactoryServices.cs

Replace line 68:
```csharp
services.TryAddSingleton<IEventTracker, EventTracker>();
```

With:
```csharp
if (remoteLocal == NeatooFactory.Remote)
{
    services.TryAddSingleton<IEventTracker, NullEventTracker>();
}
else
{
    services.TryAddSingleton<IEventTracker, EventTracker>();
}
```

### Generator Change in ClassFactoryRenderer.cs

In `RenderLocalEventRegistration`, add the `IsServerRuntime` guard. The method currently emits `services.AddScoped<...>` directly. After the fix, it emits:

```
if (NeatooRuntime.IsServerRuntime)
{
    services.AddScoped<...>(sp =>
    {
        // ... existing body ...
    });
}
```

This matches the pattern already used in `StaticFactoryRenderer.RenderLocalEventRegistration` line 300.

---

## Design

### Files to Create

| File | Purpose |
|------|---------|
| `src/RemoteFactory/Internal/NullEventTracker.cs` | No-op `IEventTracker` implementation for Remote-mode clients |

### Files to Modify

| File | Change |
|------|--------|
| `src/RemoteFactory/AddRemoteFactoryServices.cs` (line 68) | Conditional `IEventTracker` registration based on `remoteLocal` |
| `src/Generator/Renderer/ClassFactoryRenderer.cs` (`RenderLocalEventRegistration`) | Add `if (NeatooRuntime.IsServerRuntime)` guard around entire registration |

### Files to Add Tests

| File | What to Test |
|------|-------------|
| `src/Tests/RemoteFactory.UnitTests/` (new file) | `NullEventTracker` unit tests (Scenarios 8-10) |
| `src/Tests/RemoteFactory.UnitTests/` (new file or extend existing) | DI container validation tests for Remote/Server/Logical modes (Scenarios 1-4) |
| `src/Tests/RemoteFactory.UnitTests/` (existing generator tests) | Verify generated code for class factory events includes `IsServerRuntime` guard (Scenario 6) |
| `src/Tests/RemoteFactory.TrimmingTests/Program.cs` | Remove `AddLogging()`, verify still builds (Scenario 11) |

### Existing Tests (Regression)

All existing tests in `RemoteEventIntegrationTests` and `CorrelationEventPropagationTests` must continue to pass. These test containers register logging and `IHostApplicationLifetime`, so they exercise the `EventTracker` path (Server/Logical modes). The new conditional registration does not affect them.

### Impact on TrimmingTests

The TrimmingTests project currently calls `services.AddLogging()` (line 14) specifically because `EventTracker` requires `ILogger<EventTracker>`. After the fix, Remote-mode clients get `NullEventTracker` which has no logging dependency. The `AddLogging()` call can be removed from TrimmingTests to validate the fix end-to-end.

Note: the TrimmingTests project already has the `[Remote, Event]` method (`TrimTestCommands._OnWorkCompleted`) and verifies event delegate resolution. After the fix, this test validates both:
- Bug 1: Container builds without logging (NullEventTracker has no dependencies)
- Static factory events: Already guarded by `IsServerRuntime` (existing behavior)

For class factory events, the generated code for `TrimTestEntity` does not have `[Event]` methods. However, the class factory `IsServerRuntime` guard fix (Bug 2) is validated by the generator unit tests (Scenario 6) and integration tests (Scenario 12), not the TrimmingTests.

---

## Implementation Steps

### Phase 1: Core Library Fix (Bug 1)

1. Create `src/RemoteFactory/Internal/NullEventTracker.cs` -- implements `IEventTracker` with no-op behavior, `internal sealed`, no constructor dependencies.

2. Modify `src/RemoteFactory/AddRemoteFactoryServices.cs` line 68 -- replace unconditional `TryAddSingleton<IEventTracker, EventTracker>()` with conditional registration: `NullEventTracker` for Remote, `EventTracker` for Server/Logical.

### Phase 2: Generator Fix (Bug 2)

3. Modify `src/Generator/Renderer/ClassFactoryRenderer.cs` method `RenderLocalEventRegistration` -- wrap the entire `services.AddScoped<...>` registration in `if (NeatooRuntime.IsServerRuntime) { ... }`, mirroring `StaticFactoryRenderer.RenderLocalEventRegistration` lines 299-335.

### Phase 3: Tests

4. Create unit tests for `NullEventTracker` (Scenarios 8-10): Track, WaitAllAsync, PendingCount.

5. Create DI container validation unit tests (Scenarios 1-4): Build Remote/Server/Logical containers, verify correct `IEventTracker` implementation resolves, verify Remote-mode builds without logging/hosting.

6. Create or verify generator output tests (Scenarios 6-7): Verify generated class factory event registrar includes `IsServerRuntime` guard. Verify static factory event registrar still has guard (regression).

7. Update TrimmingTests (Scenario 11): Remove `services.AddLogging()` from `Program.cs`. Verify the program still outputs "ServiceProvider built successfully" when run.

8. Run all existing event integration tests (Scenario 12) to confirm no regressions.

---

## Acceptance Criteria

- [ ] `NullEventTracker` class exists in `src/RemoteFactory/Internal/`, implements `IEventTracker`, has no constructor dependencies
- [ ] `AddRemoteFactoryServices.cs` registers `NullEventTracker` for Remote mode, `EventTracker` for Server/Logical
- [ ] `ClassFactoryRenderer.RenderLocalEventRegistration` wraps registration in `if (NeatooRuntime.IsServerRuntime)`
- [ ] Generated class factory event code includes `IsServerRuntime` guard (verified by inspection or test)
- [ ] Generated static factory event code still includes `IsServerRuntime` guard (regression)
- [ ] All new unit tests pass (NullEventTracker, DI validation, generator output)
- [ ] TrimmingTests builds and runs without `AddLogging()`
- [ ] All existing `RemoteEventIntegrationTests` pass
- [ ] All existing `CorrelationEventPropagationTests` pass
- [ ] Full solution builds: `dotnet build src/Neatoo.RemoteFactory.sln`
- [ ] Full test suite passes: `dotnet test src/Neatoo.RemoteFactory.sln`
- [ ] Documentation deliverables identified: `docs/events.md` should note `NullEventTracker` behavior in Remote mode (Step 9)

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1-2: Core Library + Generator Fix | developer | Yes | Small, focused changes across 3 files. Single agent can handle both bugs and create all new files. | None |
| Phase 3: Tests | developer | No (continue) | Tests validate the changes from Phase 1-2. Same context needed. | Phase 1-2 |

**Parallelizable phases:** None -- Phase 3 depends on Phase 1-2 changes being in place.

**Notes:** All phases are small enough for a single developer agent invocation. The total change touches 3 existing files (AddRemoteFactoryServices.cs, ClassFactoryRenderer.cs, TrimmingTests/Program.cs), creates 1 new production file (NullEventTracker.cs), and creates 2-3 new test files. A single agent with the full plan can complete this.

---

## Dependencies

- No external dependencies or new packages required.
- `NullEventTracker` uses only `System.Threading.Tasks.Task` (available in all target frameworks).
- Generator changes are within the existing netstandard2.0 constraint (string manipulation only).

---

## Risks / Considerations

1. **User code that injects IEventTracker on the client.** Mitigated by `NullEventTracker` -- the interface is always resolvable. User code gets a no-op on Remote-mode clients, which is correct since events are serialized to the server in Remote mode.

2. **TryAddSingleton semantics.** The current code uses `TryAddSingleton` which means the first registration wins. After the fix, the conditional registration happens before any user override. If a user has already registered a custom `IEventTracker` before calling `AddNeatooRemoteFactory`, `TryAddSingleton` will respect their registration. This behavior is preserved.

3. **Existing test containers always register logging and IHostApplicationLifetime.** This masks the bug class. Consider adding a future todo to make the Remote-mode test containers more realistic (no hosting services). Not in scope for this fix, but noted as a separate concern.

4. **Documentation deliverables (Step 9).** The `docs/events.md` page documents `IEventTracker` as always available. After this fix, Remote-mode clients get `NullEventTracker`. The documentation should note this distinction. This is deferred to the documentation step (Step 9).
