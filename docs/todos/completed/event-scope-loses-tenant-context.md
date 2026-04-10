# Event Scope Loses Tenant Context

**Status:** Complete
**Priority:** High
**Created:** 2026-04-03
**Last Updated:** 2026-04-03

---

## Problem

When a `[Remote, Event]` delegate fires from a Blazor WASM client to the server, the generated server-side handler creates a new DI scope via `scopeFactory.CreateScope()`. This new scope gets fresh instances of scoped services — including tenant context services.

The HTTP request goes through the ASP.NET middleware pipeline, which reads authentication claims and sets a scoped tenant context (connection string, tenant ID, etc.) on the **request scope**. But the event handler runs in a **new child scope** that doesn't inherit those values. Any scoped service that depends on middleware-populated state (like a multi-tenant `DbContext`) gets unpopulated/default values.

In zTreatment: therapy step progress and remaining time events fire to the server but hit the wrong database (or default connection) because the tenant context is empty in the event scope. The UI appears to work because the WASM client manages steps locally, masking the server failure.

The original design decision (domain-events-feature.md, Section 3 "Scope Isolation" and Section 6b "Request Context") was deliberate: events get isolated scopes, no user context. But `ICorrelationContext` was already a special-case exception — the generator captures it from the parent scope and propagates it. The same pattern needs to be generalized for application-defined scoped state like tenant context.

## Solution

Introduce `IEventScopeInitializer` — a callback interface that applications register at startup to propagate ambient context from the request scope to event scopes. The generator captures registered initializers and invokes them after `CreateScope()` but before resolving handler services.

```csharp
services.AddRemoteFactoryEventScopeInitializer((parentScope, childScope) =>
{
    var parentTenant = parentScope.GetService<ITenantContext>();
    var childTenant = childScope.GetRequiredService<TenantContext>();
    if (parentTenant != null)
    {
        childTenant.TenantId = parentTenant.TenantId;
        childTenant.ConnectionString = parentTenant.ConnectionString;
    }
});
```

The existing `ICorrelationContext` propagation becomes the built-in default initializer, using the same mechanism.

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-04-03
**Verdict:** APPROVED (with one behavioral concern the architect must address)

### Relevant Requirements Found

1. **Event scope isolation is deliberate (domain-events-feature.md, Section 3 "Scope Isolation" and Section 6b "Request Context").** The original design explicitly states: "No user context -- event handlers run in isolated scope without HttpContext, user claims, or authentication state" and "Correlation IDs are available -- logging correlation flows through to event handlers for traceability." The ICorrelationContext propagation was the sole exception to the "no context" rule. This plan generalizes that exception into an extensible mechanism, which is a principled evolution (not a contradiction) because the correlation propagation already broke the strict isolation rule.

2. **IsServerRuntime guard pattern on event registrations (`src/Design/CLAUDE-DESIGN.md` lines 485-487; `docs/trimming.md` lines 39-41; `src/Generator/Renderer/StaticFactoryRenderer.cs:299-300`; `src/Generator/Renderer/ClassFactoryRenderer.cs:1598-1599`).** Both renderers wrap local event registrations in `if (NeatooRuntime.IsServerRuntime)`. The plan correctly notes that the initializer resolution is naturally guarded because it lives inside this existing guard block. No new guard is needed for the generated code change.

3. **Conditional IEventTracker registration (completed: event-di-validation-blazor-wasm.md).** `IEventTracker` is registered conditionally on `remoteLocal != NeatooFactory.Remote` (`src/RemoteFactory/AddRemoteFactoryServices.cs:73-76`). The plan proposes the same conditional pattern for `CorrelationContextScopeInitializer` registration, which is consistent.

4. **Generated code changes affect all consumers (implicit dependency).** The plan modifies `RenderLocalEventRegistration` in both `StaticFactoryRenderer.cs` and `ClassFactoryRenderer.cs`. Any assembly with `[Event]` methods will get different generated code after recompilation. The plan correctly identifies this as requiring a recompile (minor version bump, not breaking).

5. **Event delegates get `Event` suffix (`src/Design/CLAUDE-DESIGN.md` line 148; `src/Design/CLAUDE-DESIGN.md` lines 643-650).** Not affected by the proposed changes. The delegate naming convention is preserved.

6. **CancellationToken required on [Event] methods (`src/Design/CLAUDE-DESIGN.md` line 148; `docs/events.md` lines 100-104).** Not affected. The plan changes only the scope initialization, not the method signature requirements.

7. **ICorrelationContext is scoped, registered in AddNeatooRemoteFactory (`src/RemoteFactory/AddRemoteFactoryServices.cs:60-61`).** The CorrelationContextScopeInitializer will read from the parent scope's instance and write to the child scope's instance. Both are scoped instances. This is consistent with the existing pattern.

8. **Correlation ID capture timing is tested (`src/Tests/RemoteFactory.IntegrationTests/Events/CorrelationEventPropagationTests.cs`).** Seven tests verify correlation propagation behavior, including timing guarantees. Test `Event_CorrelationCapturedAtInvocation_NotAffectedByLaterChanges` (line 238-268) is particularly relevant -- see Contradictions section.

9. **Generator targets netstandard2.0 (`CLAUDE.md`, Architecture Notes section).** The renderer changes are string manipulation within the existing netstandard2.0 generator. `IServiceProvider.GetServices<T>()` is available via `Microsoft.Extensions.DependencyInjection.Abstractions` in all target frameworks. No conflict.

10. **Design Debt table (`src/Design/CLAUDE-DESIGN.md` lines 759-769).** The proposed feature does not implement any deliberately deferred feature. No conflict.

11. **Anti-Patterns 1-9 (`src/Design/CLAUDE-DESIGN.md` lines 162-424).** The proposed changes do not introduce or violate any documented anti-pattern.

12. **Design project correlation example (`src/Design/Design.Domain/Services/CorrelationExample.cs`).** Contains `[GENERATOR BEHAVIOR]` comment (line 122-126): "Events run in an isolated DI scope, but the generator captures the correlation ID before Task.Run and sets it on the event scope's ICorrelationContext." After this change, the generator no longer captures the correlation ID directly -- the initializer mechanism does. This comment will need updating. The example code itself (which injects ICorrelationContext via [Service]) continues to work unchanged.

### Gaps

1. **No existing requirement or documentation for extensible scope initialization.** The `IEventScopeInitializer` pattern is entirely new. There are no existing requirements to follow or contradict. The architect must establish the API contract, error handling semantics (what happens if an initializer throws?), and documentation.

2. **No guidance on initializer ordering.** The plan acknowledges multiple initializers run in registration order but does not document this as a requirement. The architect should decide whether ordering guarantees are part of the contract.

3. **No guidance on initializer error handling.** If an `IEventScopeInitializer.Initialize()` throws, should the event still fire? Should the exception be logged and swallowed? Should it propagate? The plan does not address this. Given that events follow fire-and-forget semantics with logged exceptions, the initializer should probably follow the same pattern.

4. **No requirement for parent scope lifetime warnings in the public API.** The plan notes (Section "Scope Lifetime Considerations") that initializers must copy values, not hold references, because the parent scope may be disposed in fire-and-forget scenarios. This is a critical usage constraint that should be documented on the `IEventScopeInitializer` interface and `AddRemoteFactoryEventScopeInitializer` method.

5. **Design project does not demonstrate IEventScopeInitializer.** After implementation, the Design project (`src/Design/Design.Domain/Services/CorrelationExample.cs`) should be updated to document the new mechanism. The `[GENERATOR BEHAVIOR]` comment on `CorrelatedOperations._OnOrderProcessed` (line 122-126) describes the old hardcoded pattern and will be inaccurate.

### Contradictions

1. **Correlation ID capture timing changes from synchronous to asynchronous (behavioral regression risk).**

   **Current generated code** (`StaticFactoryRenderer.cs:311-312`; `ClassFactoryRenderer.cs:1610-1611`):
   ```csharp
   return (params) =>
   {
       var capturedCorrelationId = parentCorrelation?.CorrelationId;  // BEFORE Task.Run
       var task = Task.Run(async () => {
           // ... uses capturedCorrelationId
       });
   ```
   The correlation ID string value is captured **synchronously** at delegate invocation time, before `Task.Run` schedules to the thread pool. This is a value capture -- immune to subsequent changes.

   **Proposed generated code** (plan, Section "Generated Code Change"):
   ```csharp
   return (params) =>
   {
       var capturedParentScope = sp;
       var task = Task.Run(async () => {
           // init.Initialize(capturedParentScope, scope.ServiceProvider);
           // Initializer reads parentScope.GetService<ICorrelationContext>()?.CorrelationId INSIDE Task.Run
       });
   ```
   The initializer reads the correlation ID from the parent scope's `ICorrelationContext` **inside** `Task.Run`. There is a window between delegate invocation and `Task.Run` execution during which the caller could change the `CorrelationId` property on the scoped `ICorrelationContext` instance.

   **Existing test that expresses this contract:** `Event_CorrelationCapturedAtInvocation_NotAffectedByLaterChanges` (`src/Tests/RemoteFactory.IntegrationTests/Events/CorrelationEventPropagationTests.cs:238-268`). This test fires an event, then immediately changes `CorrelationId` to "changed-after-fire", and asserts the event received the original value "original-005". With the proposed approach, this is a **race condition**: if `Task.Run` reads the value after the caller changes it, the test fails.

   This is not a VETO-level contradiction because the architect can address it (see Recommendations). But it must be resolved before implementation.

### Recommendations for Architect

1. **Address the correlation ID capture timing issue (Contradiction 1).** Options:
   - **(a)** Have the generated code capture values synchronously before `Task.Run` and pass them into the initializers. For example, add a `CaptureContext(IServiceProvider parentScope)` / `ApplyContext(IServiceProvider childScope, object captured)` two-phase API instead of a single `Initialize` call.
   - **(b)** Have the `CorrelationContextScopeInitializer` capture the correlation ID value in a field when it is constructed/resolved (at delegate resolution time from `sp`), and apply it inside `Task.Run`. This preserves the existing timing guarantee for the built-in initializer but does not help custom initializers.
   - **(c)** Accept the behavioral change and update the test. The argument: the window between delegate invocation and `Task.Run` execution is negligible in practice, and the test's pattern (immediately changing a scoped service value after firing an event) is artificial. The tenant context use case (the driving motivation) involves middleware-set values that do not change mid-request. However, this weakens the timing guarantee.
   - **(d)** Capture the `IServiceProvider` synchronously (before `Task.Run`) and call `Initialize` synchronously as well, outside `Task.Run` but still inside the delegate lambda. Then inside `Task.Run`, the child scope's services are already initialized. But this has its own issue: the child scope is created inside `Task.Run`, so `Initialize` must also run inside `Task.Run`. A workaround: create the child scope synchronously before `Task.Run`, which changes the scope lifetime.

2. **Define error handling for initializer failures.** If an initializer throws during `Initialize`, the event should still be tracked by `IEventTracker` and the exception should be logged (consistent with event error handling semantics). The generated code may need a try/catch around the initializer loop.

3. **Document parent scope lifetime constraint on the public API.** Add XML documentation to `IEventScopeInitializer.Initialize` and `AddRemoteFactoryEventScopeInitializer` warning that initializers must copy values and not hold references to parent-scope services, because the parent scope may be disposed after the request completes.

4. **Update Design project after implementation.** Update the `[GENERATOR BEHAVIOR]` comment in `src/Design/Design.Domain/Services/CorrelationExample.cs` (line 122-126) to describe the new `IEventScopeInitializer` mechanism instead of the hardcoded correlation propagation.

5. **Verify all 7 correlation propagation tests pass.** The tests in `src/Tests/RemoteFactory.IntegrationTests/Events/CorrelationEventPropagationTests.cs` are the behavioral contract. All must pass after the change, particularly `Event_CorrelationCapturedAtInvocation_NotAffectedByLaterChanges` (addresses Contradiction 1).

6. **Registration lifetime for IEventScopeInitializer.** The plan uses `AddTransient` for `IEventScopeInitializer`, meaning a new `DelegateEventScopeInitializer` instance is created for each `GetServices<IEventScopeInitializer>()` call. Since the initializers are resolved from the request scope (`sp`), they are resolved once per event delegate resolution (when the delegate is created from DI), not once per event invocation. The `ToArray()` capture reuses the same instances across invocations. This is fine, but the architect should confirm that `AddTransient` (not `TryAddTransient`) is correct for allowing multiple registrations -- this is correct because `GetServices<T>()` returns all registrations.

7. **Version bump.** The plan correctly identifies this as a minor version bump (additive API, generated code change requiring recompile). Confirm no other breaking changes.

### Post-Implementation Verification (Step 6B)

**Reviewer:** business-requirements-reviewer
**Verified:** 2026-04-03
**Verdict:** REQUIREMENTS SATISFIED

All 14 relevant requirements were traced through the implementation. No violations found.

#### Compliance Summary

| # | Requirement | Status | Key Evidence |
|---|------------|--------|--------------|
| 1 | Event scope isolation is deliberate | Satisfied | `IEventScopeInitializer` generalizes the existing `ICorrelationContext` exception; scope isolation is preserved (initializers copy values, not share scope access) |
| 2 | IsServerRuntime guard on event registrations | Satisfied | Initializer resolution and invocation in both `StaticFactoryRenderer.cs:304` and `ClassFactoryRenderer.cs:1599` lives inside existing `if (NeatooRuntime.IsServerRuntime)` block |
| 3 | Conditional registration (Remote mode exclusion) | Satisfied | `CorrelationContextScopeInitializer` registered inside `if (remoteLocal != NeatooFactory.Remote)` at `AddRemoteFactoryServices.cs:78`. Test `BuiltInInitializer_RegisteredInServerAndLogical_NotInRemote` verifies. |
| 4 | Generated code changes affect all consumers | Satisfied | Both renderers updated with identical initializer patterns. Minor version bump documented. |
| 5 | Event delegates get Event suffix | Unaffected | Delegate naming unchanged |
| 6 | CancellationToken required on [Event] methods | Unaffected | Method signature requirements unchanged |
| 7 | ICorrelationContext is scoped | Satisfied | `CorrelationContextScopeInitializer` reads from parent scope instance, writes to child scope instance |
| 8 | Correlation ID propagation tested | Satisfied | All 7 tests present. Timing test renamed to `Event_CorrelationReadInsideTaskRun_MaySeeLaterChanges` with updated semantics (accepts either original or changed value). |
| 9 | Generator targets netstandard2.0 | Satisfied | Renderer changes are string manipulation only. `IEventScopeInitializer` and `GetServices<T>()` appear in generated code (net9.0/net10.0), not in generator assembly. |
| 10 | Design Debt table | Unaffected | No deferred feature implemented |
| 11 | Anti-Patterns 1-9 | Unaffected | No anti-pattern introduced |
| 12 | Critical rules (Remote, visibility, guards) | Unaffected | No visibility or [Remote] changes |
| 13 | Error handling (BR-ESI-009) | Satisfied | Generated code wraps each individual initializer in try/catch inside the foreach loop (not the entire loop). Test `FailingInitializer_DoesNotPreventEventHandler` verifies. |
| 14 | Multiple initializers accumulate (BR-ESI-004/008) | Satisfied | `AddTransient` allows multiple registrations. Test `MultipleRegistrations_AccumulateWithBuiltIn` verifies 4 instances (1 built-in + 3 custom). |

#### Pre-Design Review Concerns Addressed

1. **Correlation timing contradiction (original Contradiction 1):** Resolved by user-accepted design decision. Test renamed and updated to accept either value. No requirements violation.
2. **Gap 1 (error handling):** Addressed — each initializer wrapped in individual try/catch.
3. **Gap 4 (parent scope lifetime warnings):** Addressed — XML documentation on both `IEventScopeInitializer` and `AddRemoteFactoryEventScopeInitializer` warns about copying values.
4. **Gap 5 (Design project update):** Partially addressed — `[GENERATOR BEHAVIOR]` comment in `src/Design/Design.Domain/Services/CorrelationExample.cs:122-126` still describes the old mechanism. Should be updated in the documentation step.

#### Unintended Side Effects

None found. Changes are contained to event scope initialization. No serialization contracts, factory interfaces, or non-event generated code patterns are affected.

---

## Plans

- [Event Scope Initializer Design](../plans/event-scope-initializer.md)

---

## Tasks

- [x] Create todo and draft plan (Step 1)
- [x] Business requirements review (Step 2) — APPROVED
- [x] Architect review (Step 3) — APPROVED
- [x] Implementation (Step 4)
- [x] Developer code review (Step 5) — Approved (after test fixes)
- [x] Verification (Step 6) — VERIFIED + REQUIREMENTS SATISFIED
- [x] Documentation (Step 7) — CLAUDE-DESIGN.md, events.md, interfaces-reference.md, CorrelationExample.cs

---

## Progress Log

### 2026-04-03
- Created todo from user report of zTreatment event failures in multi-tenant deployment
- Reviewed completed domain-events-feature.md: original design explicitly isolated event scopes (Section 3, 6b)
- Reviewed completed event-di-validation-blazor-wasm.md: prior fix for DI validation on Blazor WASM
- Identified that ICorrelationContext propagation is already a one-off exception to "no context" rule
- User confirmed Option A (generic scope initializer callback) as the approach
- Created plan: event-scope-initializer.md

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: Pass (0 errors, both TFMs)
- Tests: Pass (532 unit + 511 integration per TFM, 0 failures)

---

## Results / Conclusions

Introduced `IEventScopeInitializer` — an extensible mechanism for propagating ambient context from the request scope to event handler scopes.

**What was built:**
- `IEventScopeInitializer` public interface with `void Initialize(IServiceProvider parentScope, IServiceProvider childScope)`
- `AddRemoteFactoryEventScopeInitializer` extension method for registering custom initializers
- `CorrelationContextScopeInitializer` — built-in initializer that replaces the hardcoded correlation propagation in generated code
- Generated event code now resolves and invokes all registered `IEventScopeInitializer` instances with individual try/catch + logging

**Design decisions:**
- Simple single-phase `Action<IServiceProvider, IServiceProvider>` interface (not two-phase capture/apply)
- Initializers run inside `Task.Run` after `CreateScope()` — accepted timing change from the previous value-capture-before-Task.Run pattern
- Failing initializers are caught and logged; event handler still runs
- Registered conditionally (Server/Logical modes only, not Remote)

**Test coverage:** 6 new tests (custom initializer propagation, multiple initializers, failing initializer resilience, registration accumulation, server/logical/remote mode check, updated timing test). All existing event tests pass unchanged.

**Version impact:** Minor version bump (additive public API, generated code change requiring recompile).
