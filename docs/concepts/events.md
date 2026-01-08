---
layout: default
title: Events
parent: Concepts
nav_order: 7
---

# Domain Events

RemoteFactory supports fire-and-forget domain events through the `[Event]` attribute. Events run in isolated DI scopes and are tracked for graceful shutdown support.

## Event vs Execute

Both `[Event]` and `[Execute]` mark methods for remote execution, but they serve different purposes:

| Aspect | `[Execute]` | `[Event]` |
|--------|-------------|-----------|
| Execution | Synchronous (awaited) | Fire-and-forget |
| Return value | Can return data | Always returns `Task` |
| Exceptions | Thrown to caller | Logged, not thrown |
| DI Scope | Caller's scope | Isolated scope |
| Use case | Commands, queries | Notifications, side effects |

## How Events Work

When you invoke an event delegate, the framework:

1. Creates a new DI scope (isolated from the caller)
2. Resolves required services in that scope
3. Executes the handler in a background task
4. Tracks the task for graceful shutdown
5. Returns immediately to the caller

```
CALLER                                    FRAMEWORK                                HANDLER
   │                                          │                                       │
   │ eventDelegate(orderId)                   │                                       │
   │──────────────────────────────────────────►│                                       │
   │                                          │ 1. Create isolated scope              │
   │                                          │ 2. Track task                         │
   │ Task (returned immediately)              │ 3. Start Task.Run                     │
   │◄──────────────────────────────────────────│                                       │
   │                                          │                                       │
   │ (caller continues)                       │──────resolve services─────────────────►│
   │                                          │                                       │
   │                                          │                    (handler executes) │
   │                                          │◄──────────────────────────────────────│
   │                                          │ 4. Dispose scope                      │
   │                                          │ 5. Remove from tracker                │
```

## Defining Events

### Instance Class Event

```csharp
[Factory]
public partial class OrderModel
{
    [Event]
    public Task SendOrderConfirmation(
        Guid orderId,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        return emailService.SendConfirmationAsync(orderId, ct);
    }
}
```

### Static Class Event

```csharp
[Factory]
public static partial class NotificationHandler
{
    [Event]
    public static Task NotifyWarehouse(
        Guid orderId,
        string warehouseCode,
        [Service] IAuditService auditService,
        CancellationToken ct)
    {
        return auditService.LogEventAsync("WarehouseNotified", orderId, ct);
    }
}
```

## Generated Code

The source generator creates a delegate type and registers it with DI:

```csharp
// Generated nested delegate type
public partial class OrderModel
{
    public delegate Task SendOrderConfirmationEvent(Guid orderId);
}
```

The delegate is registered differently based on factory mode:

| Mode | Registration |
|------|--------------|
| Server/Logical | Scoped delegate that creates isolated scope and runs handler |
| Remote | Transient delegate that calls server via HTTP |

## Consuming Events

Inject the generated delegate and invoke it:

```csharp
public class OrderService
{
    private readonly OrderModel.SendOrderConfirmationEvent _sendConfirmation;

    public OrderService(OrderModel.SendOrderConfirmationEvent sendConfirmation)
    {
        _sendConfirmation = sendConfirmation;
    }

    public async Task PlaceOrderAsync(Order order)
    {
        // Save order to database...

        // Fire-and-forget: don't await
        _ = _sendConfirmation(order.Id);

        // Or await if you need to ensure completion before continuing
        await _sendConfirmation(order.Id);
    }
}
```

## Scope Isolation

Events run in isolated DI scopes to prevent scope-related issues:

```csharp
// Caller's scope
using var scope = serviceProvider.CreateScope();
var orderFactory = scope.ServiceProvider.GetRequiredService<IOrderFactory>();

// When event is invoked, it creates its OWN scope:
// - DbContext is fresh (no shared tracking)
// - Scoped services are fresh instances
// - Caller's scope can dispose without affecting event
_ = sendConfirmationEvent(orderId);
```

This isolation means:
- Events don't share DbContext with the caller
- Events can outlive the caller's scope
- Each event invocation gets fresh service instances

## Graceful Shutdown

The `IEventTracker` tracks all pending events. On server shutdown:

1. `IHostApplicationLifetime.ApplicationStopping` fires
2. `EventTrackerHostedService` calls `IEventTracker.WaitAllAsync()`
3. Server waits for all pending events to complete
4. Each event handler receives the cancellation token

```csharp
[Event]
public async Task ProcessOrderAsync(
    Guid orderId,
    [Service] IDbContext db,
    CancellationToken ct)  // This receives ApplicationStopping token
{
    // Check cancellation for long-running operations
    ct.ThrowIfCancellationRequested();

    var order = await db.Orders.FindAsync(new object[] { orderId }, ct);
    // ...
}
```

## Exception Handling

Event exceptions are logged but not thrown to the caller:

```csharp
[Event]
public Task ProcessNotification(
    Guid id,
    [Service] INotificationService notifications,
    CancellationToken ct)
{
    // If this throws, the exception is:
    // - Logged at Error level
    // - NOT propagated to the caller
    return notifications.ProcessAsync(id, ct);
}
```

Check server logs for event failures:

```
[ERR] Event handler failed with exception (EventId: 9003)
      System.InvalidOperationException: Notification service unavailable
         at NotificationHandler.ProcessNotification(...)
```

## CancellationToken Requirement

Events must have `CancellationToken` as the final parameter:

```csharp
// Correct: CancellationToken is last parameter
[Event]
public Task SendEmail(Guid orderId, [Service] IEmailService email, CancellationToken ct)

// Error NF0404: CancellationToken must be final parameter
[Event]
public Task SendEmail(Guid orderId, [Service] IEmailService email)
```

This ensures events can be cancelled during graceful shutdown.

## Remote Events

Events work across the client-server boundary:

```
CLIENT (Remote mode)                          SERVER
┌─────────────────────────┐                   ┌─────────────────────────┐
│ // Delegate calls server │                   │                         │
│ _ = sendConfirmation(id) │─── HTTP POST ────►│ Creates isolated scope  │
│                         │                   │ Executes handler        │
│ // Returns immediately  │◄──────────────────│ Returns Task            │
└─────────────────────────┘                   └─────────────────────────┘
```

The client doesn't wait for event completion on the server.

## Diagnostics

The generator reports these diagnostics for events:

| ID | Severity | Message |
|----|----------|---------|
| NF0401 | Error | Event method must return `void` or `Task`, not `Task<T>` |
| NF0402 | Error | Event method must be in a class with `[Factory]` attribute |
| NF0403 | Warning | Event method with no non-service parameters |
| NF0404 | Error | Event method must have `CancellationToken` as final parameter |

## Best Practices

1. **Use events for side effects that shouldn't block the caller**
   - Email notifications
   - Audit logging
   - Cache invalidation
   - Analytics tracking

2. **Don't rely on event completion for business logic**
   - Events may fail without caller notification
   - Use Execute for operations that must succeed

3. **Keep events independent**
   - Events run in isolated scopes
   - Don't assume shared state with caller

4. **Handle graceful shutdown**
   - Check `CancellationToken` in long-running handlers
   - Server waits for pending events before shutdown

5. **Log event parameters for debugging**
   - Event exceptions are logged automatically
   - Include entity IDs for correlation

## IEventTracker Interface

For advanced scenarios, inject `IEventTracker` directly:

```csharp
public interface IEventTracker
{
    void Track(Task eventTask);
    Task WaitAllAsync(CancellationToken ct = default);
    int PendingCount { get; }
}
```

This is primarily used by the framework, but can be useful for:
- Custom hosted services that need to wait for events
- Testing scenarios
- Monitoring pending event count
