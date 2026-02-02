# CorrelationContext Redesign

**Status:** Completed
**Created:** 2026-01-26
**Updated:** 2026-02-01
**Completed:** 2026-02-01
**Priority:** Medium

## Overview

Re-evaluate the `CorrelationContext` implementation. Currently it is a static class in the `Internal` namespace, but it should be a proper public API with dependency injection support.

## Resolution

Implemented a **breaking change** redesign that replaces the static `CorrelationContext` class with a scoped `ICorrelationContext` interface.

### Design Decisions

| Decision | Choice |
|----------|--------|
| Namespace | Public (`Neatoo.RemoteFactory`) |
| Static class | Removed entirely |
| Lifetime | Scoped per-request (no AsyncLocal) |
| Internal code | Constructor injection |
| Interface | Minimal - just `CorrelationId { get; set; }` |
| Event correlation | Generator captures ID before Task.Run, restores in new scope |

### What Was Built

1. **New `ICorrelationContext` interface** - Public, minimal API with just `CorrelationId` property
2. **`CorrelationContextImpl`** - Internal scoped implementation
3. **Generator update** - Captures correlation ID before `Task.Run` and restores it in event scopes
4. **Middleware update** - Sets correlation from X-Correlation-Id header or generates new ID
5. **Internal code update** - All usages converted to constructor injection

### Files Created

- `src/RemoteFactory/ICorrelationContext.cs` - Public interface

### Files Modified

- `src/RemoteFactory/Internal/CorrelationContextImpl.cs` - New implementation (replaced static class)
- `src/RemoteFactory/AddRemoteFactoryServices.cs` - DI registration
- `src/RemoteFactory/Internal/FactoryCore.cs` - Constructor injection
- `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs` - Constructor injection
- `src/RemoteFactory/Internal/MakeRemoteDelegateRequestHttpCall.cs` - Constructor injection
- `src/RemoteFactory/HandleRemoteDelegateRequest.cs` - Scoped resolution
- `src/RemoteFactory.AspNetCore/WebApplicationExtensions.cs` - Middleware update
- `src/RemoteFactory.AspNetCore/AspAuthorize.cs` - Constructor injection
- `src/Generator/Renderer/ClassFactoryRenderer.cs` - Event correlation capture

### Files Deleted

- `src/RemoteFactory/Internal/CorrelationContext.cs` - Static class removed

### Sample Files Updated

- `src/Design/Design.Domain/Services/CorrelationExample.cs`
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/CorrelationSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/AspNetCore/CorrelationIdSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs`
- `src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs`

## Breaking Changes

This is a **breaking change** requiring a major version bump.

### Migration Guide

**Before (static access):**
```csharp
var correlationId = CorrelationContext.CorrelationId;
```

**After (DI injection):**
```csharp
[Remote, Fetch]
public async Task Fetch(
    Guid id,
    [Service] ICorrelationContext correlationContext,
    [Service] IRepository repository)
{
    var correlationId = correlationContext.CorrelationId;
}
```

### Removed APIs

- `CorrelationContext.EnsureCorrelationId()` - Middleware handles generation
- `CorrelationContext.BeginScope()` - Not needed with scoped lifetime
- `CorrelationContext.Clear()` - DI scope handles cleanup

## Acceptance Criteria

- [x] `ICorrelationContext` interface defined and public
- [x] Implementation registered in DI automatically
- [x] Correlation IDs flow correctly: client → server → events
- [x] ~~Existing static usage continues to work~~ (Breaking change - removed static class)
- [x] Existing tests pass (correlation propagation verified)
- [x] Documentation samples updated with correct API usage

## Notes

- All 1,400+ tests pass across all target frameworks (net8.0, net9.0, net10.0)
- Event correlation works via explicit capture in generated code, not AsyncLocal
- Scoped lifetime eliminates race conditions and improves testability
