# CancellationToken Support Plan

**Status:** ✅ Completed
**Created:** 2026-01-04
**Completed:** 2026-01-04

## Overview

RemoteFactory needs comprehensive CancellationToken support across the entire client-server request pipeline. The main use cases are:
- Server shutdown during request processing
- Client disconnects (browser close, network failure)
- Request timeout scenarios
- Graceful application termination

Recovery from cancellation may not always be possible, but operations should terminate promptly without leaving resources in inconsistent states.

## Current State

### What Works

| Feature | Status | Notes |
|---------|--------|-------|
| Local factory methods with CancellationToken parameter | ✅ Complete | Generator recognizes and propagates tokens |
| Create/Fetch with CancellationToken | ✅ Complete | Fully tested |
| Insert/Update/Delete with CancellationToken | ✅ Complete | SaveAsync routes correctly |
| Mixed parameters (business + CancellationToken + [Service]) | ✅ Complete | Parameter ordering handled |
| Default CancellationToken values (`= default`) | ✅ Complete | Works as expected |

### What Now Works (After Implementation)

| Feature | Status | Notes |
|---------|--------|-------|
| Remote HTTP calls with cancellation | ✅ Complete | CancellationToken flows through HTTP layer, not serialized |
| Server request abort propagation | ✅ Complete | Uses linked token: `RequestAborted` + `ApplicationStopping` |
| CancellationToken serialization | ⛔ N/A | CancellationToken flows through HTTP layer, excluded from serialization |
| `IFactoryOnCancelled` hooks | ✅ Complete | Called when `OperationCanceledException` is thrown |

## Architecture Analysis

### Request Flow (Current)

```
CLIENT                                    NETWORK                               SERVER
┌────────────────────────────────────┐              ┌────────────────────────────────────────────────┐
│ Generated Factory Method           │              │ WebApplicationExtensions.UseNeatoo()           │
│   └─► LocalXxx() or RemoteXxx()   │              │   └─► Middleware: CorrelationId extraction     │
│        └─► IMakeRemoteDelegateRequest.ForDelegate()  ──HTTP POST──►   └─► MapPost("/api/neatoo")  │
│             └─► Serialize                │              │        └─► HandleRemoteDelegateRequest      │
│             └─► MakeRemoteDelegateRequestHttpCall  │   │             └─► Deserialize                   │
│                  └─► HttpClient.SendAsync()        │   │             └─► DynamicInvoke(delegate)       │
│                       ◄────────────────────────────┼───┼─────────────└─► Serialize response            │
│             └─► Deserialize response               │              └─────────────────────────────────────┘
└────────────────────────────────────┘
```

### Key Components Needing CancellationToken

#### Layer 1: HTTP Transport
| Component | File | Current Signature | Proposed Change |
|-----------|------|-------------------|-----------------|
| `MakeRemoteDelegateRequestHttpCall` delegate | `MakeRemoteDelegateRequestHttpCall.cs:6` | `Task<RemoteResponseDto> (RemoteRequestDto request)` | Add CancellationToken parameter |
| `HttpClient.SendAsync` call | `MakeRemoteDelegateRequestHttpCall.cs:30` | No token passed | Pass CancellationToken |
| `ReadFromJsonAsync` call | `MakeRemoteDelegateRequestHttpCall.cs:39` | No token passed | Pass CancellationToken |

#### Layer 2: Request Orchestration
| Component | File | Current Signature | Proposed Change |
|-----------|------|-------------------|-----------------|
| `IMakeRemoteDelegateRequest` interface | `MakeRemoteDelegateRequest.cs:11-15` | No CancellationToken | Add to both methods |
| `MakeRemoteDelegateRequest` class | `MakeRemoteDelegateRequest.cs:39,51` | No CancellationToken | Propagate to HTTP call |
| `MakeLocalSerializedDelegateRequest` | `MakeLocalSerializedDelegateRequest.cs:35,47` | No CancellationToken | Use for async operations |

#### Layer 3: Server Request Handler
| Component | File | Current Signature | Proposed Change |
|-----------|------|-------------------|-----------------|
| `HandleRemoteDelegateRequest` delegate | `HandleRemoteDelegateRequest.cs:10` | No CancellationToken | Add CancellationToken parameter |
| `LocalServer.HandlePortalRequest` | `HandleRemoteDelegateRequest.cs:22` | Returns delegate without token | Accept and use token |
| ASP.NET endpoint | `WebApplicationExtensions.cs:45` | Ignores `HttpContext.RequestAborted` | Wire to handler |

#### Layer 4: Factory Execution
| Component | File | Notes |
|-----------|------|-------|
| `IFactoryCore<T>` async methods | `FactoryCore.cs:15-20` | Could accept CancellationToken for lifecycle hooks |
| `IFactoryOnStartAsync` / `IFactoryOnCompleteAsync` | Various | Could accept CancellationToken |
| `IFactoryOnCancelled` / `IFactoryOnCancelledAsync` | New | Cancellation-specific cleanup |

#### Layer 5: Authorization (Optional Enhancement)
| Component | File | Notes |
|-----------|------|-------|
| `AspAuthorize.Authorize` | `AspAuthorize.cs:59` | Could accept CancellationToken |

---

## Tricky Cases - Decisions Made

### Case 1: CancellationToken Cannot Be Serialized ✅

**Decision:** Client aborts HTTP request via TCP connection closure.

CancellationToken doesn't need to be serialized. The client passes its CancellationToken to `HttpClient.SendAsync`. When cancelled, HttpClient closes the TCP connection, and the server detects this via `HttpContext.RequestAborted`.

```
Client CancellationToken → HttpClient.SendAsync → TCP close → HttpContext.RequestAborted
```

No complex generator changes needed - the token flows through the transport layer naturally.

---

### Case 2: Server Shutdown vs Client Disconnect ✅

**Decision:** Option B - Use linked token combining both sources.

```csharp
var cts = CancellationTokenSource.CreateLinkedTokenSource(
    httpContext.RequestAborted,
    hostApplicationLifetime.ApplicationStopping);
```

This handles:
- Client disconnect (browser close, network failure)
- Server graceful shutdown (SIGTERM, app pool recycle)

---

### Case 3: Mid-Execution Cancellation and Transaction Rollback ✅

**Decision:** Documentation only - not a framework concern.

- EF Core handles cancellation correctly (rolls back on cancellation)
- User code must cooperate by passing CancellationToken to async operations
- Multiple operations without transactions may leave partial state
- Documented in `docs/concepts/cancellation.md`

---

### Case 4: IFactoryOnComplete and Cancellation ✅

**Decision:** Option C - Add separate `IFactoryOnCancelled` interface.

```csharp
public interface IFactoryOnCancelled
{
    void FactoryCancelled(FactoryOperation operation);
}

public interface IFactoryOnCancelledAsync
{
    Task FactoryCancelledAsync(FactoryOperation operation);
}
```

- `IFactoryOnComplete` = success only (current behavior unchanged)
- `IFactoryOnCancelled` = cancellation-specific cleanup

---

### Case 5: Breaking Changes to Public Interfaces ✅

**Decision:** Minor version bump (no external users yet).

Just bump minor version and make changes directly. No overloads, no backwards compatibility shims needed. Internal migration guide added to Neatoo skill documentation.

---

### Case 6: Client-Side Timeout vs Server-Side Timeout ✅

**Decision:** Documentation only.

Our implementation handles this correctly:
- Client cancels → TCP closes → `RequestAborted` fires → server stops

Timeout best practices documented in `docs/concepts/cancellation.md`

---

### Case 7: Generator Changes for Remote Methods ✅

**Decision:** Generator excludes CancellationToken from serialized parameters, passes through HTTP layer.

```csharp
// Generated remote method
private Task<T> RemoteFetchAsync(int id, CancellationToken cancellationToken)
{
    return makeRemoteDelegateRequest.ForDelegate<T>(
        typeof(FetchDelegate),
        new object?[] { id },  // CancellationToken NOT in serialized params
        cancellationToken);    // Token passed separately to HTTP call
}
```

---

### Case 8: Partial Response on Cancellation ✅

**Decision:** No special handling needed.

ASP.NET handles this - response writing is cancelled, connection closed. Client receives exception. Documented in `docs/concepts/cancellation.md`

---

## Implementation Plan

### Phase 1: HTTP Layer Cancellation (Core) ✅

- [x] **1.1** Update `MakeRemoteDelegateRequestHttpCall` delegate to include CancellationToken
- [x] **1.2** Update `MakeRemoteDelegateRequestHttpCallImplementation.Create` to pass token to `SendAsync` and `ReadFromJsonAsync`
- [x] **1.3** Update `IMakeRemoteDelegateRequest` interface to include CancellationToken in both methods
- [x] **1.4** Update `MakeRemoteDelegateRequest` class to propagate token
- [x] **1.5** Update `MakeLocalSerializedDelegateRequest` to use token
- [x] **1.6** Update `HandleRemoteDelegateRequest` delegate to include CancellationToken
- [x] **1.7** Update `LocalServer.HandlePortalRequest` to accept and use token (including CancellationToken injection)
- [x] **1.8** Update `WebApplicationExtensions.UseNeatoo` to pass linked token to handler

### Phase 2: DI Registration Updates ✅

- [x] **2.1** Update `AddNeatooRemoteFactory` client-side registration (no changes needed - interface-based)
- [x] **2.2** Update `AddNeatooAspNetCore` server-side registration (no changes needed)
- [x] **2.3** Add `IHostApplicationLifetime` integration for graceful shutdown (linked token in WebApplicationExtensions)

### Phase 3: Generator Updates ✅

- [x] **3.1** Add `IsCancellationToken` property to `MethodParameterInfo`
- [x] **3.2** Modify generated Remote methods to exclude CancellationToken from serialization
- [x] **3.3** Modify generated Remote methods to pass CancellationToken to HTTP call
- [x] **3.4** Update static factory method generation for CancellationToken exclusion

### Phase 4: Lifecycle Hooks ✅

- [x] **4.1** Add `IFactoryOnCancelled` interface
- [x] **4.2** Add `IFactoryOnCancelledAsync` interface
- [x] **4.3** Update `FactoryCore` to call cancellation hooks on `OperationCanceledException`
- [x] **4.4** Add cancellation logging methods

### Phase 5: Testing ✅

- [x] **5.1** Tests: Remote factory methods with CancellationToken
- [x] **5.2** Tests: CancellationToken properly excluded from serialization
- [x] **5.3** Tests: Pre-cancelled token throws OperationCanceledException
- [x] **5.4** Tests: IFactoryOnCancelled callback structure
- [x] **5.5** Two-container tests: Cancellation across serialization boundary

### Phase 6: Documentation ✅

- [x] **6.1** Add concepts page for cancellation patterns (`docs/concepts/cancellation.md`)
- [x] **6.2** Migration guide in Neatoo skill (`~/.claude/skills/neatoo/migration.md`)
- [x] **6.3** Update client-server skill docs (`~/.claude/skills/neatoo/client-server.md`)
- [ ] **6.4** Document breaking changes in release notes (pending release)

---

## References

- Current tests: `src/Tests/FactoryGeneratorTests/Factory/CancellationTokenTests.cs`
- HTTP client code: `src/RemoteFactory/Internal/MakeRemoteDelegateRequestHttpCall.cs`
- Server handler: `src/RemoteFactory/HandleRemoteDelegateRequest.cs`
- ASP.NET integration: `src/RemoteFactory.AspNetCore/WebApplicationExtensions.cs`
- Concepts documentation: `docs/concepts/cancellation.md`
