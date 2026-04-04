# Architect -- Event Scope Initializer

Last updated: 2026-04-03
Current step: Architect Verification (Step 6A) -- VERIFIED

## Key Context

### Design Validated Against Codebase

1. **Generated code patterns confirmed.** Both `StaticFactoryRenderer.RenderLocalEventRegistration` (lines 287-345) and `ClassFactoryRenderer.RenderLocalEventRegistration` (lines 1583-1641) have identical initializer logic: resolve `IEventScopeInitializer` services from `sp`, iterate inside `Task.Run` after `CreateScope()`, individual try/catch per initializer with logging, then resolve handler services and invoke handler.

2. **`GetServices<T>()` is available.** Already used in `AddRemoteFactoryServices.cs:84` (`sp.GetServices<NeatooJsonConverterFactory>()`). The generated code uses the same pattern.

3. **`IEventScopeInitializer` namespace availability confirmed.** The generated code references `IEventScopeInitializer` without qualification. It lives in `Neatoo.RemoteFactory` namespace, which is required by `[Factory]` attribute usage.

4. **`DelegateEventScopeInitializer` approach is sound.** `AddTransient` (not `TryAddTransient`) correctly allows multiple registrations. `GetServices<T>()` returns all registrations.

5. **IsServerRuntime guard is naturally correct.** The initializer resolution happens inside `if (NeatooRuntime.IsServerRuntime)` guard block (StaticFactoryRenderer line 304, ClassFactoryRenderer line 1599).

6. **Error handling implemented correctly.** Each individual initializer call is wrapped in its own try/catch (not just the loop). The catch resolves `ILoggerFactory` from the child scope and logs with `LogError`. This satisfies BR-ESI-009.

### Files Examined

- `src/RemoteFactory/IEventScopeInitializer.cs` -- Public interface with XML doc (copy values warning present)
- `src/RemoteFactory/Internal/DelegateEventScopeInitializer.cs` -- Primary constructor wrapper
- `src/RemoteFactory/Internal/CorrelationContextScopeInitializer.cs` -- Built-in initializer
- `src/RemoteFactory/AddRemoteFactoryServices.cs` -- Registration + extension method
- `src/Generator/Renderer/StaticFactoryRenderer.cs` (lines 287-345) -- Updated event registration
- `src/Generator/Renderer/ClassFactoryRenderer.cs` (lines 1583-1641) -- Updated event registration
- `src/Tests/RemoteFactory.IntegrationTests/Events/EventScopeInitializerTests.cs` -- 5 new tests
- `src/Tests/RemoteFactory.IntegrationTests/Events/CorrelationEventPropagationTests.cs` -- 7 tests (1 updated)
- `src/Tests/RemoteFactory.IntegrationTests/TestContainers/ClientServerContainers.cs` -- configureLocal added

## Mistakes to Avoid

- Do NOT use `.ToArray()` in generated code without ensuring `System.Linq` is in scope -- implementation correctly iterates `IEnumerable` directly
- Do NOT assume `configureLocal` exists on the `Scopes()` overload -- it was added as part of this implementation

## User Corrections

- Timing change (initializers run inside Task.Run) is an accepted behavioral change -- user explicitly decided this
- Simple single-phase `Action<IServiceProvider, IServiceProvider>` interface preferred over two-phase capture/apply -- user explicitly decided this
- Error handling: log and swallow, following fire-and-forget semantics -- user explicitly decided this

## Architectural Verification (Pre-Handoff)

### Scope Table

| Component | Files Affected | Change Type |
|-----------|---------------|-------------|
| Core Library | `IEventScopeInitializer.cs` (new) | New public interface |
| Core Library | `Internal/DelegateEventScopeInitializer.cs` (new) | New internal class |
| Core Library | `Internal/CorrelationContextScopeInitializer.cs` (new) | New internal class |
| Core Library | `AddRemoteFactoryServices.cs` | Add registration + extension method |
| Generator | `Renderer/StaticFactoryRenderer.cs` | Replace correlation hardcoding with initializer loop |
| Generator | `Renderer/ClassFactoryRenderer.cs` | Same as above |
| Tests | `TestContainers/ClientServerContainers.cs` | Add `configureLocal` parameter |
| Tests | `Events/EventScopeInitializerTests.cs` (new) | 5 custom initializer integration tests |
| Tests | `Events/CorrelationEventPropagationTests.cs` | Updated timing test |

### Breaking Changes

None. The changes are:
- **Additive API**: New `IEventScopeInitializer` interface and `AddRemoteFactoryEventScopeInitializer` extension method
- **Generated code change**: Requires recompile but behavior is equivalent for existing code (correlation still flows)
- **Minor version bump**: Additive public API surface

## Architect Verification (Post-Implementation)

### Build Results

- `dotnet build src/Neatoo.RemoteFactory.sln` -- **Build succeeded** (0 errors, 2 warnings -- both pre-existing Blazor WASM NativeFileReference warnings)

### Test Results

- `dotnet test src/Neatoo.RemoteFactory.sln` -- **All tests pass**
  - Run 1 (net9.0 UnitTests): 532 passed, 0 failed
  - Run 2 (net10.0 UnitTests): 532 passed, 0 failed
  - Run 3 (net9.0 IntegrationTests): 514 passed, 0 failed, 3 skipped (pre-existing)
  - Run 4 (net10.0 IntegrationTests): 514 passed, 0 failed, 3 skipped (pre-existing)

### Test Scenario Coverage

| Plan Scenario | Business Rule | Actual Test | Result |
|---|---|---|---|
| TS-ESI-001 | BR-ESI-005/012 | `Event_PropagatesCorrelationId_FromParentScope` | PASSED |
| TS-ESI-002 | BR-ESI-005/012 | `StaticEvent_PropagatesCorrelationId_FromParentScope` | PASSED |
| TS-ESI-003 | BR-ESI-006 | `Event_WithoutCorrelationId_StillExecutes` | PASSED |
| TS-ESI-004 | BR-ESI-014 | `Event_CorrelationReadInsideTaskRun_MaySeeLaterChanges` | PASSED |
| TS-ESI-005 | BR-ESI-007 | `CustomInitializer_PropagatesTenantContext_ToEventScope` | PASSED |
| TS-ESI-006 | BR-ESI-008 | `MultipleInitializers_AllRun_InRegistrationOrder` | PASSED |
| TS-ESI-007 | BR-ESI-009 | `FailingInitializer_DoesNotPreventEventHandler` | PASSED |
| TS-ESI-008 | BR-ESI-001/002/003 | `BuiltInInitializer_RegisteredInServerAndLogical_NotInRemote` | PASSED |
| TS-ESI-009 | BR-ESI-004 | `MultipleRegistrations_AccumulateWithBuiltIn` | PASSED |
| TS-ESI-010 | BR-ESI-010/011 | 5 existing `RemoteEventIntegrationTests` | ALL PASSED |

### Design Match Verification

1. **IEventScopeInitializer interface** -- Matches plan exactly. Public interface with `void Initialize(IServiceProvider parentScope, IServiceProvider childScope)`. XML doc includes fire-and-forget warning about copying values.

2. **DelegateEventScopeInitializer** -- Internal sealed class using primary constructor. Wraps `Action<IServiceProvider, IServiceProvider>`. Matches plan design.

3. **CorrelationContextScopeInitializer** -- Internal sealed class. Reads `ICorrelationContext` from parent, writes to child. Null-safe with `?.CorrelationId != null` check. Matches plan design.

4. **AddRemoteFactoryServices registration** -- `CorrelationContextScopeInitializer` registered as `AddTransient<IEventScopeInitializer>` inside the `if (remoteLocal != NeatooFactory.Remote)` guard (line 78). Matches plan design (server/logical only).

5. **AddRemoteFactoryEventScopeInitializer extension** -- Returns `IServiceCollection` for chaining. Uses `AddTransient` with `DelegateEventScopeInitializer` wrapper. Includes `ArgumentNullException.ThrowIfNull` guard. XML doc present. Matches plan design.

6. **Generated code (StaticFactoryRenderer)** -- Resolves `IEventScopeInitializer` from parent scope `sp`. Iterates inside `Task.Run` after `CreateScope()`. Individual try/catch per initializer with logging. No `.ToArray()` (addressed pre-handoff concern). Matches plan design.

7. **Generated code (ClassFactoryRenderer)** -- Identical initializer pattern to StaticFactoryRenderer. Both renderers emit the same structure. Matches plan design (BR-ESI-010/011).

8. **Timing semantics** -- Initializers run inside `Task.Run`, reading live parent scope. The updated test `Event_CorrelationReadInsideTaskRun_MaySeeLaterChanges` correctly asserts either original or changed value is valid. Matches plan's accepted behavioral change (BR-ESI-014).

9. **Error handling** -- Each initializer call has its own try/catch. Exception is logged via `ILoggerFactory` resolved from child scope. Handler continues regardless of initializer failure. Matches plan design (BR-ESI-009).

10. **ClientServerContainers** -- `configureLocal` parameter added as optional `Action<IServiceCollection>?`. Applied to local collection before `BuildServiceProvider()`. Matches plan design.

### Acceptance Criteria Checklist

- [x] `IEventScopeInitializer` public interface exists
- [x] `AddRemoteFactoryEventScopeInitializer` extension method exists and registers initializers
- [x] `CorrelationContextScopeInitializer` handles correlation propagation (replacing hardcoded generation)
- [x] Generated event code resolves and invokes all `IEventScopeInitializer` instances
- [x] Multiple initializers can be registered and all are invoked
- [x] Correlation timing test updated for new semantics (read inside Task.Run, not value capture before)
- [x] Existing correlation context propagation tests still pass (except the updated timing test)
- [x] New integration test demonstrates custom scope initializer (simulating tenant context)
- [x] Full solution builds: `dotnet build src/Neatoo.RemoteFactory.sln`
- [x] Full test suite passes: `dotnet test src/Neatoo.RemoteFactory.sln`

### Verdict: VERIFIED

The implementation matches the plan design in all respects. All 10 test scenarios map to passing tests. The solution builds cleanly and all 2,092 test executions pass (across 4 TFM runs). No test failures, no design drift, no missing coverage.
