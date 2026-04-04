# Event Scope Initializer

**Date:** 2026-04-03
**Related Todo:** [Event Scope Loses Tenant Context](../todos/event-scope-loses-tenant-context.md)
**Status:** Complete
**Last Updated:** 2026-04-03

<!-- Valid status values (do not render in plan):
Draft | Under Review (Architect) | Concerns Raised (Architect) | Ready for Implementation |
In Progress | Awaiting Code Review | Code Review Concerns | Awaiting Verification | Sent Back |
Requirements Documented | Documentation Complete | Complete
-->

---

## Overview

Event handlers run in isolated DI scopes (by design), but this breaks multi-tenant applications where middleware populates scoped tenant context on the request scope. The generator already propagates `ICorrelationContext` as a one-off special case. This plan generalizes that pattern into an extensible `IEventScopeInitializer` mechanism so applications can propagate any ambient context from the request scope to event scopes.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/event-scope-loses-tenant-context.md#requirements-review)

### Relevant Existing Requirements

1. **Event scope isolation is deliberate** (domain-events-feature.md, Section 3 and 6b). Events get isolated scopes without user context. `ICorrelationContext` propagation was already a documented exception to this rule. This plan generalizes that exception into an extensible mechanism — a principled evolution, not a contradiction.

2. **IsServerRuntime guard pattern on event registrations** (CLAUDE-DESIGN.md lines 485-487). Both renderers wrap local event registrations in `if (NeatooRuntime.IsServerRuntime)`. The initializer resolution lives inside this existing guard. No new guard is needed.

3. **Conditional registration pattern** (event-di-validation-blazor-wasm.md). `IEventTracker` is registered conditionally on `remoteLocal != NeatooFactory.Remote` (`AddRemoteFactoryServices.cs:73-76`). The plan proposes the same pattern for `CorrelationContextScopeInitializer` — consistent.

4. **Generated code changes affect all consumers**. Modifying `RenderLocalEventRegistration` in both renderers means any assembly with `[Event]` methods gets different generated code. Requires recompile. Minor version bump.

5. **ICorrelationContext is scoped, registered in AddNeatooRemoteFactory** (`AddRemoteFactoryServices.cs:60-61`). The initializer reads from parent scope instance and writes to child scope instance. Both are scoped. Consistent with existing pattern.

6. **Correlation ID capture timing is tested** (`CorrelationEventPropagationTests.cs`). Seven tests verify correlation propagation behavior. One test (`Event_CorrelationCapturedAtInvocation_NotAffectedByLaterChanges`) asserts pre-Task.Run capture timing and will be updated per the accepted behavioral change.

7. **Generator targets netstandard2.0**. The renderer changes are string manipulation. No netstandard2.0 constraint violated.

### Gaps

1. **No existing requirement for extensible scope initialization.** `IEventScopeInitializer` is entirely new. The architect must establish the API contract, error handling semantics, and documentation. Addressed in Business Rules below.

2. **No guidance on initializer ordering.** Multiple initializers run in registration order. No explicit ordering mechanism. Addressed in Business Rules below.

3. **No guidance on initializer error handling.** Addressed in Business Rules — initializer exceptions are caught, logged, and do not prevent the event handler from executing.

4. **No requirement for parent scope lifetime warnings.** The constraint that initializers must copy values (not hold references) needs XML documentation on the interface.

5. **Design project needs update.** The `[GENERATOR BEHAVIOR]` comment in `CorrelationExample.cs` (line 122-126) describes the old hardcoded pattern and will be inaccurate. Documentation step deliverable.

### Contradictions

None (after addressing the timing change below). The requirements reviewer's Contradiction 1 (correlation ID capture timing) is resolved by the user's design decision to accept the behavioral change and update the affected test.

---

## Business Rules (Testable Assertions)

### Registration and Resolution

**BR-ESI-001 (NEW):** WHEN `AddNeatooRemoteFactory` is called with `NeatooFactory.Server`, THEN `IEventScopeInitializer` is resolvable via `GetServices<IEventScopeInitializer>()` AND the collection includes `CorrelationContextScopeInitializer`.

**BR-ESI-002 (NEW):** WHEN `AddNeatooRemoteFactory` is called with `NeatooFactory.Logical`, THEN `IEventScopeInitializer` is resolvable via `GetServices<IEventScopeInitializer>()` AND the collection includes `CorrelationContextScopeInitializer`.

**BR-ESI-003 (NEW):** WHEN `AddNeatooRemoteFactory` is called with `NeatooFactory.Remote`, THEN `CorrelationContextScopeInitializer` is NOT registered (no unnecessary registrations on client).

**BR-ESI-004 (NEW):** WHEN `AddRemoteFactoryEventScopeInitializer` is called multiple times, THEN `GetServices<IEventScopeInitializer>()` returns all registered initializers (including the built-in `CorrelationContextScopeInitializer`).

### CorrelationContextScopeInitializer Behavior

**BR-ESI-005 (NEW):** WHEN `CorrelationContextScopeInitializer.Initialize` is called with a parent scope whose `ICorrelationContext.CorrelationId` is "abc-123" AND a child scope with a fresh `ICorrelationContext`, THEN the child scope's `ICorrelationContext.CorrelationId` RETURNS "abc-123".

**BR-ESI-006 (NEW):** WHEN `CorrelationContextScopeInitializer.Initialize` is called with a parent scope whose `ICorrelationContext.CorrelationId` is null, THEN the child scope's `ICorrelationContext.CorrelationId` is NOT modified (remains null).

### Custom Initializer Behavior

**BR-ESI-007 (NEW):** WHEN a custom `IEventScopeInitializer` is registered via `AddRemoteFactoryEventScopeInitializer` AND an event fires in Logical/Server mode, THEN the custom initializer's `Initialize` method is called with the parent scope and child scope.

**BR-ESI-008 (NEW):** WHEN multiple initializers are registered (built-in + custom), THEN all initializers run for each event scope creation AND they run in registration order (built-in first, then custom in the order registered).

### Error Handling

**BR-ESI-009 (NEW):** WHEN an `IEventScopeInitializer` throws during `Initialize`, THEN the exception is logged AND the event handler still executes (initializer failure does not prevent event execution). The try/catch wraps each individual initializer call, not the entire loop.

### Generated Code Pattern

**BR-ESI-010 (NEW):** WHEN a class with `[Event]` methods is compiled with the generator, THEN the generated `RenderLocalEventRegistration` code resolves `IEventScopeInitializer` instances from the parent scope (`sp`), captures them, and invokes them inside `Task.Run` after `CreateScope()` but before resolving handler services.

**BR-ESI-011 (NEW):** WHEN a static class with `[Event]` methods is compiled with the generator, THEN the same generated pattern as BR-ESI-010 applies (both renderers emit identical initializer logic).

**BR-ESI-012:** WHEN a local event fires in a scope where `ICorrelationContext.CorrelationId` is set, THEN the event handler's scope receives the correlation ID. (Existing behavior, preserved through the new mechanism.)

### Remote Mode Behavior

**BR-ESI-013:** WHEN the factory mode is `NeatooFactory.Remote`, THEN event delegates serialize to the server and no local scope is created — initializers do not run on the client. (Existing behavior, unchanged.)

### Timing Semantics

**BR-ESI-014 (NEW — behavioral change from pre-existing):** WHEN an event fires, THEN initializers read from the parent scope **inside** `Task.Run` (not before it). If the caller mutates scoped state between delegate invocation and `Task.Run` execution, the initializer may see the mutated state. This is an accepted behavioral change from the previous value-capture-before-Task.Run pattern.

### Test Scenarios

**TS-ESI-001 (for BR-ESI-005, BR-ESI-012):** Register `CorrelationContextScopeInitializer` via `AddNeatooRemoteFactory(Logical)`. Set `ICorrelationContext.CorrelationId = "test-corr-001"` on the local scope. Resolve event delegate, fire event. Assert the event handler received correlation ID "test-corr-001". *(Covered by existing test `Event_PropagatesCorrelationId_FromParentScope` — should pass unchanged.)*

**TS-ESI-002 (for BR-ESI-005, BR-ESI-012):** Same as TS-ESI-001 but for a static class event. *(Covered by existing test `StaticEvent_PropagatesCorrelationId_FromParentScope`.)*

**TS-ESI-003 (for BR-ESI-006):** Do not set a correlation ID. Fire event. Assert the event still executes (correlation ID null in handler). *(Covered by existing test `Event_WithoutCorrelationId_StillExecutes`.)*

**TS-ESI-004 (for BR-ESI-014):** Set correlation ID to "original". Fire event. Immediately change correlation ID to "changed". Assert: the event handler may receive either value (race condition is accepted). *(Existing test `Event_CorrelationCapturedAtInvocation_NotAffectedByLaterChanges` must be updated to reflect the new timing — see Implementation Step 10.)*

**TS-ESI-005 (for BR-ESI-007):** Register a custom `IEventScopeInitializer` via `AddRemoteFactoryEventScopeInitializer` that propagates a custom scoped service value (simulating tenant context). Fire event. Assert the event handler's scope received the propagated value. *(New test.)*

**TS-ESI-006 (for BR-ESI-008):** Register two custom initializers (in addition to the built-in). Fire event. Assert all three initializers ran (verify via side effects on the child scope). *(New test.)*

**TS-ESI-007 (for BR-ESI-009):** Register a custom initializer that throws `InvalidOperationException`. Fire event. Assert the event handler still executed (exception was swallowed). *(New test.)*

**TS-ESI-008 (for BR-ESI-001, BR-ESI-002, BR-ESI-003):** Verify registration: in Server/Logical mode, `GetServices<IEventScopeInitializer>()` returns at least one instance (the built-in). In Remote mode, the collection is empty. *(New unit test.)*

**TS-ESI-009 (for BR-ESI-004):** Call `AddRemoteFactoryEventScopeInitializer` three times. Verify `GetServices<IEventScopeInitializer>()` returns 4 instances (1 built-in + 3 custom). *(New unit test.)*

**TS-ESI-010 (for BR-ESI-010, BR-ESI-011):** Existing `RemoteEventIntegrationTests` (5 tests) pass unchanged, confirming the generated code pattern works for both class and static factory events.

---

## Approach

### The Problem

The generated event registration code creates a new scope and only propagates `ICorrelationContext.CorrelationId`:

```csharp
// Current generated code (both StaticFactoryRenderer and ClassFactoryRenderer)
var parentCorrelation = sp.GetService<ICorrelationContext>();
return (params) =>
{
    var capturedCorrelationId = parentCorrelation?.CorrelationId;
    var task = Task.Run(async () =>
    {
        using var scope = scopeFactory.CreateScope();
        var eventCorrelation = scope.ServiceProvider.GetService<ICorrelationContext>();
        if (eventCorrelation != null && capturedCorrelationId != null)
        {
            eventCorrelation.CorrelationId = capturedCorrelationId;
        }
        // ... resolve services and invoke handler
    });
};
```

Tenant context, user identity, and any other middleware-populated scoped state is lost.

### The Solution: IEventScopeInitializer

Introduce an interface and registration extension that lets applications define what context flows from the request scope to event scopes:

```csharp
// New public interface in Neatoo.RemoteFactory
public interface IEventScopeInitializer
{
    void Initialize(IServiceProvider parentScope, IServiceProvider childScope);
}

// Extension method for registration
public static IServiceCollection AddRemoteFactoryEventScopeInitializer(
    this IServiceCollection services,
    Action<IServiceProvider, IServiceProvider> initializer);
```

### Built-in Default: Correlation Context

The existing `ICorrelationContext` propagation becomes a built-in `IEventScopeInitializer` registered by `AddNeatooRemoteFactory`. This removes the hardcoded correlation propagation from generated code and replaces it with the general mechanism.

### Application Usage

```csharp
// In Startup / Program.cs — AFTER AddNeatooRemoteFactory
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

### Generated Code Change

The generator replaces the hardcoded correlation propagation with a loop over all registered `IEventScopeInitializer` instances:

```csharp
// New generated code pattern
var initializers = sp.GetServices<IEventScopeInitializer>();
return (params) =>
{
    // Capture parent scope's service provider before Task.Run
    var capturedInitializers = initializers.ToArray();
    var capturedParentScope = sp;  // the request scope
    var task = Task.Run(async () =>
    {
        using var scope = scopeFactory.CreateScope();
        foreach (var init in capturedInitializers)
        {
            init.Initialize(capturedParentScope, scope.ServiceProvider);
        }
        // ... resolve services and invoke handler
    });
};
```

The initializers run **inside** `Task.Run` after `CreateScope()`. The parent `IServiceProvider` is captured by reference, not by value — initializers read the live parent scope services at `Task.Run` execution time, not at delegate invocation time.

**Timing nuance**: The current hardcoded correlation propagation captures the correlation ID string **before** `Task.Run` (value capture). The new mechanism reads from the parent scope **inside** `Task.Run`. This means there's a brief window where the caller could mutate scoped state between delegate invocation and initializer execution. This is an accepted behavioral change — initializers should read stable state that isn't mutated after event firing. The existing test `Event_CorrelationCapturedAtInvocation_NotAffectedByLaterChanges` must be updated to reflect the new timing semantics.

### Correlation Context as Built-in Initializer

```csharp
// Internal class in Neatoo.RemoteFactory
internal sealed class CorrelationContextScopeInitializer : IEventScopeInitializer
{
    public void Initialize(IServiceProvider parentScope, IServiceProvider childScope)
    {
        var parentCorrelation = parentScope.GetService<ICorrelationContext>();
        var childCorrelation = childScope.GetService<ICorrelationContext>();
        if (parentCorrelation?.CorrelationId != null && childCorrelation != null)
        {
            childCorrelation.CorrelationId = parentCorrelation.CorrelationId;
        }
    }
}
```

Registered automatically in `AddNeatooRemoteFactory` (Server/Logical modes only — Remote mode doesn't run event handlers locally).

---

## Domain Model Behavioral Design

Not applicable — this is infrastructure/framework code, not a domain model. No computed properties, visibility flags, reactive rules, or validation rules.

---

## Design

### New Files

| File | Purpose |
|------|---------|
| `src/RemoteFactory/IEventScopeInitializer.cs` | Public interface |
| `src/RemoteFactory/Internal/CorrelationContextScopeInitializer.cs` | Built-in initializer for correlation context |

### Modified Files

| File | Change |
|------|--------|
| `src/RemoteFactory/AddRemoteFactoryServices.cs` | Register `CorrelationContextScopeInitializer`, add `AddRemoteFactoryEventScopeInitializer` extension |
| `src/Generator/Renderer/StaticFactoryRenderer.cs` | Replace hardcoded correlation propagation with `IEventScopeInitializer` loop in `RenderLocalEventRegistration` |
| `src/Generator/Renderer/ClassFactoryRenderer.cs` | Same change in `RenderLocalEventRegistration` |

### Registration Design

`IEventScopeInitializer` is registered using `AddTransient` (not `TryAdd`), allowing multiple registrations. Each call to `AddRemoteFactoryEventScopeInitializer` adds another instance. The built-in `CorrelationContextScopeInitializer` is registered first by `AddNeatooRemoteFactory`.

The extension method wraps the lambda in a concrete class:

```csharp
public static IServiceCollection AddRemoteFactoryEventScopeInitializer(
    this IServiceCollection services,
    Action<IServiceProvider, IServiceProvider> initializer)
{
    services.AddTransient<IEventScopeInitializer>(
        _ => new DelegateEventScopeInitializer(initializer));
    return services;
}

internal sealed class DelegateEventScopeInitializer(
    Action<IServiceProvider, IServiceProvider> initializer) : IEventScopeInitializer
{
    public void Initialize(IServiceProvider parentScope, IServiceProvider childScope)
        => initializer(parentScope, childScope);
}
```

### Server-Only Guard

Event scope initializers only matter on the server (Server/Logical modes). In Remote mode, events serialize to the server — no local scope is created. The generated code for local events is already inside `if (NeatooRuntime.IsServerRuntime)`, so the initializer resolution is naturally guarded.

The `CorrelationContextScopeInitializer` registration should still be conditional on `remoteLocal != NeatooFactory.Remote` to avoid unnecessary registrations on the client.

### Scope Lifetime Considerations

The `IEventScopeInitializer` instances are resolved from the request scope (`sp.GetServices<IEventScopeInitializer>()`). They're captured as an array before `Task.Run`. Inside `Task.Run`, they receive both the parent `IServiceProvider` (request scope) and child `IServiceProvider` (event scope). The parent scope is still alive at this point because:

1. The event delegate is invoked during the request
2. `Task.Run` starts immediately
3. The initializer runs before any async work in the handler

For fire-and-forget scenarios, the initializer must **copy values**, not hold references to parent-scope services, because the parent scope may be disposed after the request completes.

---

## Implementation Steps

1. **Create `IEventScopeInitializer` interface** — `src/RemoteFactory/IEventScopeInitializer.cs`. Public interface with `void Initialize(IServiceProvider parentScope, IServiceProvider childScope)`.

2. **Create `DelegateEventScopeInitializer`** — Internal class in `src/RemoteFactory/Internal/DelegateEventScopeInitializer.cs`. Wraps `Action<IServiceProvider, IServiceProvider>` as `IEventScopeInitializer`.

3. **Create `CorrelationContextScopeInitializer`** — `src/RemoteFactory/Internal/CorrelationContextScopeInitializer.cs`. Moves existing correlation propagation logic from generated code into a reusable initializer.

4. **Add `AddRemoteFactoryEventScopeInitializer` extension method** — In `AddRemoteFactoryServices.cs`. Registers `DelegateEventScopeInitializer` as transient `IEventScopeInitializer`.

5. **Register `CorrelationContextScopeInitializer` in `AddNeatooRemoteFactory`** — Conditional on `remoteLocal != NeatooFactory.Remote`.

6. **Update `StaticFactoryRenderer.RenderLocalEventRegistration`** — Replace hardcoded correlation context capture/propagation with: resolve `IEventScopeInitializer` collection from `sp`, capture array, loop inside `Task.Run` after `CreateScope()`.

7. **Update `ClassFactoryRenderer.RenderLocalEventRegistration`** — Same change as Step 6.

8. **Add unit tests** — `IEventScopeInitializer` mechanism: verify multiple initializers run, verify correlation context initializer works, verify custom initializers propagate state.

9. **Add integration tests** — Using `ClientServerContainers`: register a custom scope initializer, fire an event, verify the child scope received the propagated state.

10. **Update correlation timing test** — `Event_CorrelationCapturedAtInvocation_NotAffectedByLaterChanges` in `CorrelationEventPropagationTests.cs` must be updated to reflect the new timing: correlation is read from the parent scope inside `Task.Run`, not captured as a value before it.

11. **Verify existing event tests pass** — All `RemoteEventIntegrationTests` and remaining `CorrelationEventPropagationTests` must still pass (correlation context now flows through the initializer mechanism instead of hardcoded generation).

---

## Acceptance Criteria

- [ ] `IEventScopeInitializer` public interface exists
- [ ] `AddRemoteFactoryEventScopeInitializer` extension method exists and registers initializers
- [ ] `CorrelationContextScopeInitializer` handles correlation propagation (replacing hardcoded generation)
- [ ] Generated event code resolves and invokes all `IEventScopeInitializer` instances
- [ ] Multiple initializers can be registered and all are invoked
- [ ] Correlation timing test updated for new semantics (read inside Task.Run, not value capture before)
- [ ] Existing correlation context propagation tests still pass (except the updated timing test)
- [ ] New integration test demonstrates custom scope initializer (simulating tenant context)
- [ ] Full solution builds: `dotnet build src/Neatoo.RemoteFactory.sln`
- [ ] Full test suite passes: `dotnet test src/Neatoo.RemoteFactory.sln`

---

## Dependencies

- No new external packages required
- Generator changes are string manipulation within existing netstandard2.0 constraint
- `IServiceProvider.GetServices<T>()` is available in all target frameworks via `Microsoft.Extensions.DependencyInjection.Abstractions`

---

## Risks / Considerations

1. **Parent scope lifetime in fire-and-forget** — Initializers run inside `Task.Run` but before the handler does async work. For fire-and-forget events, the request scope may be disposed after the initializer runs. Initializers must copy values, not hold references. Document this clearly on the interface.

2. **Ordering of initializers** — Multiple `IEventScopeInitializer` registrations run in registration order. No explicit ordering mechanism. If ordering matters, register in the correct order. This matches the current pattern (correlation context is first).

3. **Performance** — `GetServices<IEventScopeInitializer>()` is called once per scope creation (when the event delegate is resolved from DI). For most applications this is negligible. The array is captured once and reused for all invocations of that delegate instance.

4. **Breaking change assessment** — The generated code changes, so any assembly using events must be recompiled. The runtime API is additive (new interface, new extension method). This is a minor version bump.

5. **Correlation capture timing change** — Accepted behavioral change. Previously, correlation ID was captured as a string value before `Task.Run`. Now it's read from the parent scope inside `Task.Run`. In practice, callers should not mutate scoped state immediately after firing an event. The existing test `Event_CorrelationCapturedAtInvocation_NotAffectedByLaterChanges` will be updated to reflect this.
