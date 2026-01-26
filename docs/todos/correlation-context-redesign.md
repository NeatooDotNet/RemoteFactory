# CorrelationContext Redesign

**Status:** Pending
**Created:** 2025-01-26
**Updated:** 2025-01-26
**Priority:** Medium

## Overview

Re-evaluate the `CorrelationContext` implementation. Currently it is a static class in the `Internal` namespace, but it should be a proper public API with dependency injection support.

## Current Implementation

**Location:** `src/RemoteFactory/Internal/CorrelationContext.cs`

```csharp
namespace Neatoo.RemoteFactory.Internal;

public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public static string? CorrelationId { get; set; }
    public static string EnsureCorrelationId();
    public static IDisposable BeginScope(string? correlationId);
    public static void Clear();
}
```

## Problems

1. **Static class** - Cannot be mocked for testing, harder to reason about
2. **Internal namespace** - Not clearly part of public API, but used by consumers
3. **Ambient context pattern** - Magic globals are harder to trace and test
4. **No interface** - Cannot be substituted or extended

## Desired Goals

- [ ] Define clear public API for correlation ID access
- [ ] Support dependency injection pattern
- [ ] Maintain async flow across operations (client → server → events)
- [ ] Enable testing without static state
- [ ] Preserve backward compatibility if possible
- [ ] Clear documentation on how to access correlation IDs in:
  - Factory methods
  - Event handlers
  - Authorization checks
  - Lifecycle hooks

## Design Questions

1. Should there be an `ICorrelationContext` interface?
2. Should it be scoped per-request or ambient?
3. How does correlation flow from client to server to events?
4. Should the static API remain as a convenience alongside DI?
5. What is the migration path for existing code using the static class?

## Proposed Phases

### Phase 1: Design
- Define `ICorrelationContext` interface
- Determine scoping strategy (scoped vs singleton with AsyncLocal)
- Document correlation flow across boundaries

### Phase 2: Implementation
- Create interface and implementation
- Register in DI during `AddNeatooRemoteFactory` / `AddNeatooAspNetCore`
- Update internal usages to use injected interface
- Consider keeping static class as facade over DI for convenience

### Phase 3: Testing
- Add unit tests for correlation propagation
- Test client → server → event flow
- Verify async context flows correctly

### Phase 4: Documentation
- Update `events-correlation` snippet
- Update `aspnetcore-correlation-id` snippet
- Add correlation section to documentation if missing
- Document migration from static to DI pattern

## Acceptance Criteria

- [ ] `ICorrelationContext` interface defined and public
- [ ] Implementation registered in DI automatically
- [ ] Correlation IDs flow correctly: client → server → events
- [ ] Existing static usage continues to work (backward compatible)
- [ ] Unit tests verify correlation propagation
- [ ] Documentation updated with correct API usage

## Related Files

- `src/RemoteFactory/Internal/CorrelationContext.cs`
- `src/RemoteFactory.AspNetCore/WebApplicationExtensions.cs`
- `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs`
- `docs/aspnetcore-integration.md` (aspnetcore-correlation-id snippet)
- `docs/events.md` (events-correlation snippet)

## Notes

Discovered during documentation review - the current static implementation works but doesn't follow the DI patterns used elsewhere in RemoteFactory.
