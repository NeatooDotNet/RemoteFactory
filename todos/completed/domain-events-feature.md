# Domain Events Feature Plan

## Overview

Add `[Event]` method attribute to RemoteFactory for fire-and-forget delegate generation with scope isolation. This enables decoupled, transactionally-independent operations on aggregate roots.

## Confirmed Design Decisions

### 1. Events vs Execute

| Aspect | `[Execute]` | `[Event]` |
|--------|-------------|-----------|
| **Semantics** | Request-response | Fire-and-forget capable |
| **Awaiting** | Caller always waits | Caller can await or discard |
| **Scope** | Shares caller's scope | Always NEW scope (isolated) |
| **Transaction** | Part of caller's transaction | Own transaction (isolated) |
| **Target** | Application/infrastructure services | Aggregate root methods |
| **Orphaning** | Never orphaned | Meant to be orphaned |
| **Mapping** | 1:1 (method → delegate) | 1:1 (method → delegate) |

### 2. Always Asynchronous

- **Generated delegates always return `Task`** regardless of whether the provider method is `void` or `Task`
- Caller receives `Task` and decides: `await` or discard (`_ =`)
- Void methods are wrapped internally to return `Task`
- **`CancellationToken` is required as the final parameter** - enforces cancellation support for fire-and-forget operations

```csharp
// Provider methods - MUST have CancellationToken as final parameter
[Event]
public void ProcessSync(OrderData data, [Service] IRepo repo, CancellationToken ct) { }

[Event]
public async Task ProcessAsync(OrderData data, [Service] IRepo repo, CancellationToken ct) { }

// Generated delegates - ALWAYS return Task, CancellationToken excluded (injected internally)
// Named with "Event" suffix to differentiate from [Execute] delegates
public delegate Task ProcessSyncEvent(OrderData data);
public delegate Task ProcessAsyncEvent(OrderData data);

// Consumer - uniform API
Task t1 = processSyncEvent(data);    // Wrapped internally
Task t2 = processAsyncEvent(data);

_ = t1;        // Fire-and-forget
await t2;      // Wait for completion

// Internally, ApplicationStopping token is passed to handler
```

### 3. Scope Isolation

- **Event handlers always run in a NEW `IServiceScope`**
- Handler gets its own `DbContext` instance
- Original transaction is unaffected by handler failures
- Each handler is its own unit of work

```csharp
// Factory operation commits → done, final
// Event handlers get fresh scope with own DbContext
// Handler failure doesn't affect original operation
```

### 4. Handler Ordering

- **No explicit ordering** - handlers should be independent
- If ordering is needed, handlers likely belong together or need explicit choreography

### 5. Error Handling

| Scenario | Behavior |
|----------|----------|
| **Fire-and-forget failure** | Log exception (ILogger), swallow - don't crash app |
| **Awaited failure** | Exception propagates normally to caller |

### 6. Retry/Resilience

- **Deferred to v2** - keep v1 simple
- Future: Hangfire integration for durable, retriable background processing

### 6a. Thread Pool Considerations

- **Each event uses `Task.Run`** - queues work to the thread pool
- **High-frequency events may cause thread pool pressure** under heavy load
- For high-volume scenarios, recommend Hangfire integration (v2) which uses dedicated workers
- v1 design is optimized for correctness and simplicity over maximum throughput

### 6b. Request Context

- **No user context** - event handlers run in isolated scope without `HttpContext`, user claims, or authentication state
- **Correlation IDs are available** - logging correlation flows through to event handlers for traceability
- If user identity is needed, pass it explicitly as a parameter (e.g., `Guid userId`)

### 7. Remote Events (v1 Scope)

- **Client-to-server only** - client raises, server handles
- **Client awaits HTTP acknowledgment** - confirms event reached server
- Server handlers run in isolated scope (fire-and-forget semantics at handler level)
- **Handler failures are invisible to client** - HTTP 200 means "received", not "succeeded"

```
Client (Blazor WASM)              Server (ASP.NET Core)
       │                                  │
       │  event delegate invocation       │
       │ ──────────────────────────────► │
       │    (serialize → HTTP POST)       │
       │                                  │ Handler in new scope
       │                                  │ Own transaction
       │  ◄─────────────────────────────  │
       │    (HTTP 200 acknowledgment)     │
       │                                  │ Handler may fail here
       │                                  │ (client unaware)
```

**Guidance:** Use `[Event]` when the client doesn't need to know the outcome (notifications, audit logs, analytics). Use `[Execute]` when the client must know success/failure.

### 8. Discovery & Registration

- **`[Event]` only valid within `[Factory]` classes** (performance optimization)
- Generator uses `ForAttributeWithMetadataName` on `[Factory]` first
- Within `[Factory]` classes, scans methods for `[Event]`
- Uses existing `FactoryServiceRegistrar` pattern for DI registration

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Consumer Code                             │
│                                                                  │
│   var sendConfirmation = sp.GetRequiredService<SendConfirmationEvent>();│
│   _ = sendConfirmation(orderId);  // Fire-and-forget            │
│   // OR                                                          │
│   await sendConfirmation(orderId);  // Wait for completion      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Generated Event Delegate                       │
│                                                                  │
│   public delegate Task SendConfirmationEvent(Guid orderId);     │
│                                                                  │
│   // Registered in DI with scope isolation + cancellation:      │
│   services.AddScoped<SendConfirmationEvent>(sp =>                    │
│   {                                                             │
│       var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();│
│       var tracker = sp.GetRequiredService<IEventTracker>();     │
│       var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();│
│       return (orderId) =>                                       │
│       {                                                         │
│           var task = Task.Run(async () =>                       │
│           {                                                     │
│               using var scope = scopeFactory.CreateScope();     │
│               var ct = lifetime.ApplicationStopping;            │
│               var handler = scope.ServiceProvider               │
│                   .GetRequiredService<OrderHandler>();          │
│               var email = scope.ServiceProvider                 │
│                   .GetRequiredService<IEmailService>();         │
│               await handler.SendConfirmation(orderId, email, ct);│
│           });                                                   │
│           tracker.Track(task);                                  │
│           return task;                                          │
│       };                                                        │
│   });                                                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Handler Class (Provider)                     │
│                                                                  │
│   [Factory]                                                     │
│   public class OrderHandler                                     │
│   {                                                             │
│       [Event]                                                   │
│       public async Task SendConfirmation(                       │
│           Guid orderId,                                         │
│           [Service] IEmailService emailService,                 │
│           CancellationToken ct)  // Required final parameter    │
│       {                                                         │
│           await emailService.SendOrderConfirmation(orderId, ct);│
│       }                                                         │
│   }                                                             │
└─────────────────────────────────────────────────────────────────┘
```

## Task List

### Phase 1: Core Infrastructure

- [ ] **Create `[Event]` attribute** in `src/RemoteFactory/FactoryAttributes.cs`
  - Similar structure to `[Execute]`
  - Target: `AttributeTargets.Method`
  - Properties: None for v1

- [ ] **Create `IEventTracker` interface** for pending async events
  - `void Track(Task eventTask)` - tracks a fire-and-forget task
  - `Task WaitAllAsync(CancellationToken ct = default)` - waits for all pending
  - `int PendingCount { get; }` - diagnostics

- [ ] **Create `EventTracker` implementation**
  - Constructor: inject `ILogger<EventTracker>` for exception logging
  - Thread-safe collection of pending tasks (e.g., `ConcurrentBag<Task>`)
  - Remove completed tasks on each add (or periodic cleanup)
  - Log exceptions from failed tasks via `ILogger.LogError` (don't throw)
  - `WaitAllAsync` implementation for shutdown

### Phase 2: Source Generator

- [ ] **Update `FactoryGenerator.cs` to detect `[Event]` methods**
  - Scan `[Factory]` classes (existing efficient pattern)
  - Within factory classes, find methods with `[Event]` attribute
  - Validate: return must be `void` or `Task`

- [ ] **Generate event delegates**
  - Naming: `{MethodName}Event` suffix (e.g., `SendConfirmation` → `SendConfirmationEvent`)
  - Differentiates from `[Execute]` delegates and prevents collisions
  - **Always returns `Task`** (even for void methods)
  - Exclude `[Service]` parameters from delegate signature
  - Exclude `CancellationToken` from delegate signature

- [ ] **Generate DI registration with scope isolation**
  - Use `IServiceScopeFactory` (singleton) to create isolated scopes safely
  - Use `IHostApplicationLifetime` to get `ApplicationStopping` token
  - Wrap in `Task.Run` with `CreateScope()`
  - Resolve handler and services from new scope
  - Pass `ApplicationStopping` token to handler's `CancellationToken` parameter
  - Track Task with `IEventTracker` for fire-and-forget monitoring
  - Pattern:
    ```csharp
    services.AddScoped<DelegateName>(sp =>
    {
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var tracker = sp.GetRequiredService<IEventTracker>();
        var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
        return (params) =>
        {
            var task = Task.Run(async () =>
            {
                using var scope = scopeFactory.CreateScope();
                var ct = lifetime.ApplicationStopping;
                // resolve and invoke from scope.ServiceProvider, passing ct
            });
            tracker.Track(task);
            return task;
        };
    });
    ```

- [ ] **Handle exception aggregation**
  - If caller awaits: exceptions propagate normally
  - If fire-and-forget: log exceptions, don't crash

### Phase 3: Remote Events Support

- [ ] **Extend remote delegate infrastructure for events**
  - Client serializes event delegate invocation
  - Server deserializes, creates new scope, invokes handler
  - Client awaits HTTP acknowledgment (not handler completion)

- [ ] **Generate remote event dispatch code**
  - Similar to existing `[Execute]` remote pattern
  - Serialize parameters → HTTP POST → deserialize → invoke in scope

### Phase 4: ASP.NET Core Integration

- [ ] **Create `EventTrackerHostedService` implementing `IHostedService`**
  - `StartAsync`: no-op
  - `StopAsync`: call `IEventTracker.WaitAllAsync()` with timeout
  - Ensures graceful shutdown waits for pending events

- [ ] **Update `AddRemoteFactoryServices` for event infrastructure**
  - Register `IEventTracker` as singleton
  - Register `IHostApplicationLifetime` (provided by host)
  - Register `EventTrackerHostedService` via `AddHostedService<T>()`
  - Ensure tracker is available across all scopes

### Phase 5: Diagnostics and Validation

- [ ] **Add Roslyn diagnostic: RF_EVENT_001**
  - Error: `[Event]` method must return `void` or `Task` (not `ValueTask`)
  - Rationale: `Task.Run` overhead makes `ValueTask` optimization meaningless for events

- [ ] **Add Roslyn diagnostic: RF_EVENT_002**
  - Error: `[Event]` method must be in a class with `[Factory]` attribute

- [ ] **Add Roslyn diagnostic: RF_EVENT_003**
  - Warning: `[Event]` method with no non-service parameters (unusual)

- [ ] **Add Roslyn diagnostic: RF_EVENT_004**
  - Error: `[Event]` method must have `CancellationToken` as final parameter

### Phase 6: Testing

- [ ] **Unit tests for `EventTracker`**
  - Track/wait semantics
  - Thread safety
  - Cleanup of completed tasks
  - Exception logging (not throwing)

- [ ] **Generator tests for `[Event]` attribute**
  - Delegate generation (always `Task` return)
  - Service parameter exclusion
  - Scope isolation in generated code
  - DI registration

- [ ] **Integration tests with two DI containers**
  - Remote event dispatch (client → server)
  - Serialization round-trip
  - Scope isolation verification

- [ ] **Fire-and-forget behavior tests**
  - Verify caller can continue without awaiting
  - Verify handler failures don't affect caller
  - Verify `WaitAllAsync` waits for pending tasks

### Phase 7: Documentation

- [ ] **Create `docs/concepts/events.md`**
  - Overview of `[Event]` vs `[Execute]`
  - Scope isolation explanation
  - Fire-and-forget semantics

- [ ] **Create `docs/getting-started/events-quick-start.md`**
  - Basic event handler definition
  - Consumer invocation patterns
  - Await vs fire-and-forget

- [ ] **Update `docs/reference/attributes.md`**
  - Document `[Event]` attribute

- [ ] **Create example in `src/Examples/`**
  - Order confirmation email (fire-and-forget)
  - Audit logging (await for compliance)

## Resolved Questions

| Question | Decision |
|----------|----------|
| Handler ordering | No ordering - handlers should be independent |
| Handler lifetime | Scoped, but in NEW scope (isolated from caller) |
| Transaction boundaries | Always new scope - handlers can't roll back caller |
| Error handling (sync) | Continue all handlers, aggregate exceptions |
| Error handling (async) | Log and swallow - don't crash app |
| Retry logic | Defer to v2 (Hangfire integration later) |
| Remote events scope | Client-to-server only for v1 |
| Remote acknowledgment | Client awaits HTTP 200 (delivery confirmation) |
| Remote handler failure | Invisible to client - use `[Execute]` if result needed |
| Event type discovery | Inferred from [Event] method parameters |
| Delegate return type | Always `Task` (void methods wrapped); `ValueTask` not supported |
| CancellationToken | Required as final parameter; `ApplicationStopping` injected |
| Discovery performance | Find [Factory] classes first, then [Event] methods |
| Registration pattern | Use existing FactoryServiceRegistrar |
| Delegate naming | `{MethodName}Event` suffix to differentiate from `[Execute]` |
| Scope creation | Use `IServiceScopeFactory` (singleton) - safe for fire-and-forget |
| Thread pool pressure | Documented limitation; Hangfire recommended for high-volume (v2) |
| Request context | No user claims; correlation IDs available; pass userId explicitly if needed |

## Future Considerations (Not in v1)

- Hangfire integration for durable, retriable events
- Server-to-client events (SignalR push)
- Event sourcing integration
- Saga/process manager support
- Distributed events (across services)

## References

- Existing `[Execute]` implementation: `src/Generator/FactoryGenerator.cs:374-444`
- Service parameter handling: `src/Generator/FactoryGenerator.Types.cs:719-739`
- DI registration pattern: `src/RemoteFactory/AddRemoteFactoryServices.cs:122-134`
- Incremental generator pattern: `ForAttributeWithMetadataName`
