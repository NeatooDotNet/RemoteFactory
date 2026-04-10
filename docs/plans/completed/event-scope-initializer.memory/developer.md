# Developer -- Event Scope Initializer

Last updated: 2026-04-03
Current step: Developer Code Review (Step 5)

## Key Context

The Event Scope Initializer plan introduces `IEventScopeInitializer` to generalize the hardcoded `ICorrelationContext` propagation in generated event code. The implementation replaces per-event hardcoded correlation capture with a foreach loop over all registered `IEventScopeInitializer` instances.

Key design decisions from the task context:
- Simple single-phase `Action<IServiceProvider, IServiceProvider>` (not two-phase capture/apply)
- Initializers run inside `Task.Run` (accepted timing change from pre-existing value-capture pattern)
- Failing initializers are caught and logged per-initializer; handler still runs
- No `.ToArray()` in generated code -- iterate IEnumerable directly with foreach
- `configureLocal` parameter added to `ClientServerContainers.Scopes()` custom overload

## Developer Review

**Status:** Concerns
**Date:** 2026-04-03

### Summary

The implementation correctly introduces `IEventScopeInitializer`, a `DelegateEventScopeInitializer` wrapper, and a `CorrelationContextScopeInitializer` built-in. Registration in `AddRemoteFactoryServices.cs` is conditional on non-Remote mode. Both renderers (Static and Class) emit identical initializer resolution and foreach-with-try/catch patterns. The updated timing test correctly reflects the accepted behavioral change. The `configureLocal` parameter enables isolated test container configuration.

### Code Review Trace

| # | Business Rule | Implementation Path | Verified? |
|---|--------------|---------------------|-----------|
| BR-ESI-001 | Server mode registers CorrelationContextScopeInitializer | `AddRemoteFactoryServices.cs:73-78` -- `if (remoteLocal != NeatooFactory.Remote)` block registers `AddTransient<IEventScopeInitializer, CorrelationContextScopeInitializer>()` | Yes |
| BR-ESI-002 | Logical mode registers CorrelationContextScopeInitializer | `AddRemoteFactoryServices.cs:73-78` -- same `!=Remote` guard covers Logical | Yes |
| BR-ESI-003 | Remote mode does NOT register CorrelationContextScopeInitializer | `AddRemoteFactoryServices.cs:73` -- `if (remoteLocal != NeatooFactory.Remote)` excludes Remote | Yes |
| BR-ESI-004 | Multiple AddRemoteFactoryEventScopeInitializer calls accumulate | `AddRemoteFactoryServices.cs:227` -- `services.AddTransient<IEventScopeInitializer>(...)` (AddTransient, not TryAdd) allows multiple registrations | Yes |
| BR-ESI-005 | CorrelationContextScopeInitializer copies correlation ID from parent to child | `CorrelationContextScopeInitializer.cs:13-18` -- reads parent's `CorrelationId`, writes to child's `CorrelationId` | Yes |
| BR-ESI-006 | Null correlation ID not copied | `CorrelationContextScopeInitializer.cs:15` -- `if (parentCorrelation?.CorrelationId != null && childCorrelation != null)` guards null | Yes |
| BR-ESI-007 | Custom initializer's Initialize called with parent and child scope | `StaticFactoryRenderer.cs:323` / `ClassFactoryRenderer.cs:1618` -- `initializer.Initialize(sp, scope.ServiceProvider)` inside foreach; `DelegateEventScopeInitializer.cs:9` delegates to the Action | Yes |
| BR-ESI-008 | Multiple initializers all run in registration order | `StaticFactoryRenderer.cs:319-330` / `ClassFactoryRenderer.cs:1614-1625` -- foreach over `eventScopeInitializers` (resolved from DI, which preserves registration order). Per-initializer try/catch means one failure doesn't skip subsequent ones. | Yes |
| BR-ESI-009 | Failing initializer caught, logged, handler still runs | `StaticFactoryRenderer.cs:321-329` / `ClassFactoryRenderer.cs:1616-1625` -- try/catch per initializer, logs via `ILoggerFactory`, execution continues to handler invocation at line 338/1634 | Yes |
| BR-ESI-010 | Class factory generated code resolves IEventScopeInitializer from sp, invokes inside Task.Run after CreateScope | `ClassFactoryRenderer.cs:1607` resolves from `sp`; lines 1612-1625 inside Task.Run after CreateScope; `initializer.Initialize(sp, scope.ServiceProvider)` | Yes |
| BR-ESI-011 | Static factory generated code has identical pattern | `StaticFactoryRenderer.cs:312` resolves from `sp`; lines 317-330 inside Task.Run after CreateScope; `initializer.Initialize(sp, scope.ServiceProvider)` | Yes |
| BR-ESI-012 | Correlation ID flows through new mechanism | Existing tests pass; `CorrelationContextScopeInitializer` replaces hardcoded correlation capture. Verified by test `Event_PropagatesCorrelationId_FromParentScope` | Yes |
| BR-ESI-013 | Remote mode: events serialize, no local scope, no initializers | `StaticFactoryRenderer.cs:271-285` / `ClassFactoryRenderer.cs:1562-1570` -- Remote mode uses `ForDelegateEvent` (serialization), no scope creation. Registration guard at `AddRemoteFactoryServices.cs:73` prevents initializer registration in Remote mode. | Yes |
| BR-ESI-014 | Timing: initializers read parent scope inside Task.Run | `StaticFactoryRenderer.cs:315-323` -- initializer loop is inside `Task.Run(async () => { ... })` after `CreateScope()`. `sp` is captured by reference, not value-captured before Task.Run. Test `Event_CorrelationReadInsideTaskRun_MaySeeLaterChanges` accepts either value. | Yes |

### Test Scenario Mapping

| # | Plan Scenario | Test Method | File | Covered? |
|---|--------------|-------------|------|----------|
| TS-ESI-001 | Correlation propagation in Logical mode (class event) | `Event_PropagatesCorrelationId_FromParentScope` | `CorrelationEventPropagationTests.cs:34` | Yes |
| TS-ESI-002 | Correlation propagation (static class event) | `StaticEvent_PropagatesCorrelationId_FromParentScope` | `CorrelationEventPropagationTests.cs:69` | Yes |
| TS-ESI-003 | Event executes without correlation ID | `Event_WithoutCorrelationId_StillExecutes` | `CorrelationEventPropagationTests.cs:209` | Yes |
| TS-ESI-004 | Correlation timing with mutation after fire | `Event_CorrelationReadInsideTaskRun_MaySeeLaterChanges` | `CorrelationEventPropagationTests.cs:240` | Yes |
| TS-ESI-005 | Custom initializer propagates tenant context | `CustomInitializer_PropagatesTenantContext_ToEventScope` | `EventScopeInitializerTests.cs:37` | **Partial -- see Concern 1** |
| TS-ESI-006 | Multiple initializers all run | `MultipleInitializers_AllRun_InRegistrationOrder` | `EventScopeInitializerTests.cs:84` | **Partial -- see Concern 2** |
| TS-ESI-007 | Failing initializer does not prevent handler | `FailingInitializer_DoesNotPreventEventHandler` | `EventScopeInitializerTests.cs:140` | Yes |
| TS-ESI-008 | Built-in registered in Server/Logical, not Remote | `BuiltInInitializer_RegisteredInServerAndLogical_NotInRemote` | `EventScopeInitializerTests.cs:176` | Yes |
| TS-ESI-009 | 3 custom + 1 built-in = 4 instances | No dedicated test | -- | **Missing -- see Concern 3** |
| TS-ESI-010 | Existing event integration tests pass | `RemoteEventIntegrationTests` (5 tests) | `RemoteEventIntegrationTests.cs` | Yes (all pass) |

### Concerns

#### Concern 1: `CustomInitializer_PropagatesTenantContext_ToEventScope` does not verify custom initializer ran

**Severity: Medium**

The test registers a custom `IEventScopeInitializer` that propagates `ITenantContext`, but the event handler (`CorrelationEventTarget.ProcessWithCorrelation`) only reads `ICorrelationContext`, not `ITenantContext`. The assertion on line 76 checks `CorrelationId == "corr-with-tenant"`, which proves the built-in correlation initializer ran, but provides no evidence that the custom tenant initializer ran.

The plan's TS-ESI-005 says: "Register a custom `IEventScopeInitializer` ... that propagates a custom scoped service value (simulating tenant context). Fire event. **Assert the event handler's scope received the propagated value.**"

To properly verify this, the test needs either:
- A new event handler that reads `ITenantContext` from DI and records its values, OR
- A different mechanism to verify the custom initializer was invoked (e.g., a flag set by the initializer that can be checked after the event completes)

As written, if the custom initializer were removed from the test, the assertion would still pass (it only checks correlation, which the built-in handles).

#### Concern 2: `MultipleInitializers_AllRun_InRegistrationOrder` does not verify custom initializers ran

**Severity: Medium**

Same issue as Concern 1. The test registers two custom initializers that set `ITenantContext.TenantId` and `ITenantContext.ConnectionString`, but the event handler doesn't read `ITenantContext`. The assertion (line 131-133) only verifies the event executed by checking event name and entity ID.

This test would pass identically if both custom initializers were removed -- it only verifies that the event fired, not that any custom initializer ran. To test "all initializers run in registration order," the test needs to verify the side effects of the custom initializers within the event handler's scope.

#### Concern 3: TS-ESI-009 has no test

**Severity: Low**

The plan specifies TS-ESI-009: "Call `AddRemoteFactoryEventScopeInitializer` three times. Verify `GetServices<IEventScopeInitializer>()` returns 4 instances (1 built-in + 3 custom)." No test implements this scenario. While BR-ESI-004 (multiple registrations accumulate) is trivially correct from the `AddTransient` call, explicit count verification is cheap and provides a regression safety net.

#### Concern 4: Plan's Approach section shows `.ToArray()` but implementation omits it

**Severity: Info (not a concern)**

The plan's Approach section (line 202) shows `var capturedInitializers = initializers.ToArray()` but the actual implementation iterates the `IEnumerable` from `GetServices` directly. This is explicitly called out as a key design decision in the task description ("No `.ToArray()` in generated code -- iterate IEnumerable directly with foreach"). This is safe because the initializers are resolved once at DI resolution time and the same collection object is reused for all invocations. The plan's Approach section is aspirational; the actual design decision supersedes it.

### Design Drift Assessment

The implementation closely matches the plan. Notable deviations:

1. **No `.ToArray()`** -- Addressed above, explicitly accepted.
2. **`DelegateEventScopeInitializer` in separate file** -- The plan showed it inline in `AddRemoteFactoryServices.cs` but the implementation correctly puts it in its own file at `src/RemoteFactory/Internal/DelegateEventScopeInitializer.cs`. This is cleaner.
3. **Logger category string** -- The generated code uses `"Neatoo.RemoteFactory.EventScopeInitializer"` as the logger category. The plan didn't specify a category. This is a reasonable choice.

No fundamental design drift detected.

### Logic Review

1. **Null safety**: `CorrelationContextScopeInitializer.Initialize` guards both `parentCorrelation?.CorrelationId != null` and `childCorrelation != null`. Correct.
2. **ArgumentNullException guard**: `AddRemoteFactoryEventScopeInitializer` validates `initializer != null` (line 226). Correct.
3. **Per-initializer try/catch**: Each initializer in the foreach gets its own try/catch. One failure doesn't skip the rest. Correct per BR-ESI-009.
4. **Scope lifetime**: Initializers receive `sp` (parent scope's `IServiceProvider`) and `scope.ServiceProvider` (child scope). Parent scope is captured by closure reference, not value. Inside Task.Run, the parent scope should still be alive since the delegate is invoked during the request. Correct.
5. **Both renderers identical**: StaticFactoryRenderer lines 306-345 and ClassFactoryRenderer lines 1598-1641 have identical initializer logic (differing only in `{typeName}` vs `{model.ImplementationTypeName}` for the target type and handler resolution). Correct.

### Ready to Proceed?

[x] Concerns raised -- three test coverage gaps need addressing before approval

**Recommended actions (in priority order):**

1. **(Concern 1 + 2)** Either create a new event target that reads `ITenantContext` from DI and records its values, or use a simpler verification mechanism (e.g., a shared flag/counter). The custom initializer tests need to verify that the custom initializer's `Initialize` method actually ran and affected the child scope.

2. **(Concern 3)** Add a simple test that registers 3 custom initializers, resolves `GetServices<IEventScopeInitializer>()`, and asserts count == 4.

These are fixable without design changes -- they're test coverage gaps, not implementation bugs.
