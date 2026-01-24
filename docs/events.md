# Events

RemoteFactory supports fire-and-forget domain events with scope isolation via the `[Event]` attribute.

## Event Operation Basics

Events are asynchronous operations that run independently of the caller:

<!-- snippet: events-basic -->
<a id='snippet-events-basic'></a>
```cs
[Factory]
public partial class OrderWithEvents
{
    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;

    [Create]
    public OrderWithEvents()
    {
        Id = Guid.NewGuid();
        OrderNumber = $"ORD-{Guid.NewGuid().ToString()[..8]}";
    }

    // Event handler - runs in isolated scope, fire-and-forget semantics
    [Event]
    public async Task SendOrderConfirmation(
        Guid orderId,
        string customerEmail,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            customerEmail,
            "Order Confirmation",
            $"Thank you for your order {orderId}!",
            ct);
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L9-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Key characteristics:
- **Fire-and-forget**: Caller doesn't wait for completion
- **Scope isolation**: New DI scope per event
- **Transactional independence**: Events run in separate transactions
- **Graceful shutdown**: EventTracker waits for pending events

## How Events Work

### 1. Caller Invokes Event

<!-- snippet: events-caller -->
<a id='snippet-events-caller'></a>
```cs
public partial class EventCallerExample
{
    // [Fact]
    public async Task FireEvent()
    {
        var scopes = SampleTestContainers.Scopes();

        // Get the generated event delegate
        var sendConfirmation = scopes.local.GetRequiredService<OrderWithEvents.SendOrderConfirmationEvent>();

        var orderId = Guid.NewGuid();
        var email = "customer@example.com";

        // Fire the event - returns Task but doesn't block
        _ = sendConfirmation(orderId, email);

        // Code continues immediately without waiting for event to complete
        // Event executes in background with isolated scope

        // For testing, wait for events to complete
        var eventTracker = scopes.local.GetRequiredService<IEventTracker>();
        await eventTracker.WaitAllAsync();
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L397-L422' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-caller' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The factory immediately returns without waiting.

### 2. Event Executes in Background

Event runs in a new DI scope:

```csharp
// Pseudo-code showing what happens
Task.Run(async () =>
{
    using var scope = serviceProvider.CreateScope();
    var entity = scope.ServiceProvider.GetRequiredService<OrderWithEvents>();
    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
    var ct = hostApplicationLifetime.ApplicationStopping;
    await entity.SendOrderConfirmation(orderId, customerEmail, emailService, ct);
});
```

### 3. EventTracker Monitors Completion

The generated factory tracks the event Task:

<!-- snippet: events-tracker-generated -->
<a id='snippet-events-tracker-generated'></a>
```cs
// Generated event delegate uses IEventTracker for isolated execution:
//
// public delegate Task SendOrderConfirmationEvent(Guid orderId, string customerEmail);
//
// Generated implementation (simplified):
// public Task SendOrderConfirmationDelegate(Guid orderId, string customerEmail)
// {
//     var task = Task.Run(async () =>
//     {
//         using var scope = _serviceProvider.CreateScope();
//         var entity = scope.ServiceProvider.GetRequiredService<OrderWithEvents>();
//         var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
//         var ct = _lifetime.ApplicationStopping;
//
//         await entity.SendOrderConfirmation(orderId, customerEmail, emailService, ct);
//     });
//
//     _eventTracker.Track(task);
//     return task; // Returns tracked task for optional awaiting
// }
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L424-L445' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-tracker-generated' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Event Method Requirements

Event methods must:

1. Have `[Event]` attribute
2. Accept `CancellationToken` as the last parameter
3. Return `Task` or `void` (void methods are converted to Task delegates)

<!-- snippet: events-requirements -->
<a id='snippet-events-requirements'></a>
```cs
[Factory]
public partial class EventRequirements
{
    [Create]
    public EventRequirements() { }

    // Event method MUST have CancellationToken as final parameter
    [Event]
    public Task ValidEvent(Guid id, [Service] IEmailService service, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    // Can return void - generated delegate still returns Task
    [Event]
    public void VoidEvent(string message, CancellationToken ct)
    {
        // Fire-and-forget
    }

    // Can be async
    [Event]
    public async Task AsyncEvent(Guid id, CancellationToken ct)
    {
        await Task.Delay(100, ct);
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L40-L68' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-requirements' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The generator converts void methods to Task automatically.

### Generated Event Delegates

For each `[Event]` method, the generator creates a delegate type named `{MethodName}Event`:

```csharp
// [Event] method
public async Task SendOrderConfirmation(Guid orderId, string customerEmail, ...) { }

// Generated delegate
public delegate Task SendOrderConfirmationEvent(Guid orderId, string customerEmail);
```

Register and invoke the delegate via DI:

```csharp
var sendConfirmation = serviceProvider.GetRequiredService<OrderWithEvents.SendOrderConfirmationEvent>();
_ = sendConfirmation(orderId, email); // Fire-and-forget
```

## Scope Isolation

Each event gets a new DI scope:

<!-- snippet: events-scope-isolation -->
<a id='snippet-events-scope-isolation'></a>
```cs
[Factory]
public partial class ScopeIsolatedEvent
{
    [Create]
    public ScopeIsolatedEvent() { }

    // Event runs in NEW IServiceScope
    // Scoped services (DbContext, etc.) are independent from the calling scope
    [Event]
    public async Task ProcessInIsolatedScope(
        Guid entityId,
        [Service] IPersonRepository repository, // New scoped instance
        [Service] IAuditLogService auditLog,    // New scoped instance
        CancellationToken ct)
    {
        // This repository instance is separate from the caller's scope
        var entity = await repository.GetByIdAsync(entityId, ct);

        // Audit log in separate transaction
        await auditLog.LogAsync("EventProcessed", entityId, "Entity", "Processed in isolated scope", ct);
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L70-L93' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-scope-isolation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Benefits:
- **Separate transactions**: Event failures don't roll back triggering operation
- **Independent lifetime**: Scoped services don't leak across events
- **Parallel execution**: Multiple events run concurrently without conflicts

### Example: Order Processing

<!-- snippet: events-scope-example -->
<a id='snippet-events-scope-example'></a>
```cs
[Factory]
public partial class OrderProcessing
{
    public Guid Id { get; private set; }
    public string Status { get; set; } = "Pending";

    [Create]
    public OrderProcessing() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> CreateOrder([Service] IOrderRepository repository)
    {
        // Main transaction - save order
        await repository.AddAsync(new OrderEntity
        {
            Id = Id,
            OrderNumber = $"ORD-{Id.ToString()[..8]}",
            Status = Status,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        });
        await repository.SaveChangesAsync();
        return true;
    }

    // Event runs in separate scope - separate transaction
    [Event]
    public async Task LogOrderCreated(
        Guid orderId,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // This is a separate transaction from Insert
        // If this fails, Insert still succeeds
        await auditLog.LogAsync("OrderCreated", orderId, "Order", "New order created", ct);
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L95-L133' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-scope-example' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If the event fails:
- Order is still saved (separate transaction)
- Event failure is logged by the application
- Client call succeeds

## CancellationToken Handling

Events receive the application shutdown token:

<!-- snippet: events-cancellation -->
<a id='snippet-events-cancellation'></a>
```cs
[Factory]
public partial class CancellableEvent
{
    [Create]
    public CancellableEvent() { }

    [Event]
    public async Task LongRunningEvent(
        Guid id,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        // Check cancellation before each operation
        ct.ThrowIfCancellationRequested();

        await emailService.SendAsync("admin@example.com", "Processing", $"Processing {id}", ct);

        // Respect cancellation during loops
        for (int i = 0; i < 10; i++)
        {
            if (ct.IsCancellationRequested)
                break;

            await Task.Delay(100, ct);
        }
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L135-L163' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The CancellationToken:
- Fires on `IHostApplicationLifetime.ApplicationStopping`
- Allows graceful cleanup
- Is required for all event methods

### Graceful Shutdown Example:

<!-- snippet: events-graceful-shutdown -->
<a id='snippet-events-graceful-shutdown'></a>
```cs
public static class GracefulShutdownConfiguration
{
    public static void Configure(IServiceCollection services)
    {
        // EventTrackerHostedService is automatically registered by AddNeatooAspNetCore
        // It handles graceful shutdown:
        //
        // 1. ApplicationStopping triggers cancellation of running events
        // 2. Waits for pending events to complete (with timeout)
        // 3. Logs any events that didn't complete

        services.AddNeatooAspNetCore(typeof(EventsSamples).Assembly);
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L447-L462' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-graceful-shutdown' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## EventTracker

The `IEventTracker` service monitors pending events.

### Accessing EventTracker

<!-- snippet: events-eventtracker-access -->
<a id='snippet-events-eventtracker-access'></a>
```cs
public partial class EventTrackerAccess
{
    // [Fact]
    public async Task AccessEventTracker()
    {
        var scopes = SampleTestContainers.Scopes();

        // IEventTracker is registered as singleton
        var eventTracker = scopes.local.GetRequiredService<IEventTracker>();

        // Fire some events
        var fireEvent = scopes.local.GetRequiredService<OrderWithEvents.SendOrderConfirmationEvent>();
        _ = fireEvent(Guid.NewGuid(), "test@example.com");
        _ = fireEvent(Guid.NewGuid(), "test2@example.com");

        // Check pending count
        Assert.True(eventTracker.PendingCount >= 0);

        // Wait for all events
        await eventTracker.WaitAllAsync();
        Assert.Equal(0, eventTracker.PendingCount);
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L464-L488' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-access' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Waiting for Events (Testing)

<!-- snippet: events-eventtracker-wait -->
<a id='snippet-events-eventtracker-wait'></a>
```cs
public partial class EventTrackerWaitExample
{
    // [Fact]
    public async Task WaitForPendingEvents()
    {
        var scopes = SampleTestContainers.Scopes();
        var eventTracker = scopes.local.GetRequiredService<IEventTracker>();

        var fireEvent = scopes.local.GetRequiredService<OrderWithEvents.SendOrderConfirmationEvent>();
        var orderId = Guid.NewGuid();

        // Fire event
        _ = fireEvent(orderId, "customer@example.com");

        // Wait for completion in tests
        await eventTracker.WaitAllAsync();

        // Assert side effects
        var emailService = scopes.local.GetRequiredService<MockEmailService>();
        Assert.Contains(emailService.SentEmails, e => e.To == "customer@example.com");
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L490-L513' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-wait' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Monitoring Pending Events

<!-- snippet: events-eventtracker-count -->
<a id='snippet-events-eventtracker-count'></a>
```cs
public partial class EventTrackerCountExample
{
    // [Fact]
    public async Task CheckPendingCount()
    {
        var scopes = SampleTestContainers.Scopes();
        var eventTracker = scopes.local.GetRequiredService<IEventTracker>();

        // Initially no pending events
        Assert.Equal(0, eventTracker.PendingCount);

        var fireEvent = scopes.local.GetRequiredService<CancellableEvent.LongRunningEventEvent>();

        // Fire multiple events
        _ = fireEvent(Guid.NewGuid());
        _ = fireEvent(Guid.NewGuid());

        // Some events may be pending
        // Note: count may already be 0 if events completed quickly
        var initialCount = eventTracker.PendingCount;

        // Wait and verify all complete
        await eventTracker.WaitAllAsync();
        Assert.Equal(0, eventTracker.PendingCount);
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L515-L542' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-count' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Error Handling

Event exceptions should be handled within the event method. Unhandled exceptions are caught by the generated wrapper, logged, and suppressed to preserve fire-and-forget semantics:

<!-- snippet: events-error-handling -->
<a id='snippet-events-error-handling'></a>
```cs
[Factory]
public partial class ErrorHandlingEvent
{
    [Create]
    public ErrorHandlingEvent() { }

    [Event]
    public async Task EventWithErrorHandling(
        Guid id,
        [Service] IEmailService emailService,
        [Service] Microsoft.Extensions.Logging.ILogger<ErrorHandlingEvent> logger,
        CancellationToken ct)
    {
        try
        {
            await emailService.SendAsync("customer@example.com", "Subject", "Body", ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log error but don't rethrow - fire-and-forget semantics
            logger.LogError(ex, "Failed to send email for entity {EntityId}", id);
        }
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L263-L288' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-error-handling' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The generated event delegate wraps execution:

```csharp
// Generated delegate (simplified)
public Task SendOrderConfirmationDelegate(Guid orderId, string customerEmail)
{
    var task = Task.Run(async () =>
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var entity = scope.ServiceProvider.GetRequiredService<OrderWithEvents>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            await entity.SendOrderConfirmation(orderId, customerEmail, emailService, _ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Event SendOrderConfirmation failed");
        }
    });
    _eventTracker.Track(task);
    return task; // Returns tracked task for optional awaiting
}
```

**Exception Behavior:**
- Unhandled exceptions are caught by the wrapper
- Logged with event name and correlation ID
- Suppressed (don't crash the application)
- Don't affect the triggering operation
- `OperationCanceledException` is not logged (expected during shutdown)

## ASP.NET Core Integration

Events integrate with ASP.NET Core hosting:

<!-- snippet: events-aspnetcore -->
<a id='snippet-events-aspnetcore'></a>
```cs
// ASP.NET Core automatically configures event handling:
//
// services.AddNeatooAspNetCore(...) registers:
// - IEventTracker (singleton)
// - EventTrackerHostedService (handles graceful shutdown)
//
// On application shutdown:
// 1. ApplicationStopping token is triggered
// 2. EventTrackerHostedService waits for pending events
// 3. Events receive cancellation token signal
// 4. Application exits after events complete or timeout
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L544-L556' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-aspnetcore' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`EventTrackerHostedService` waits for pending events during shutdown:

```csharp
public class EventTrackerHostedService : IHostedService
{
    public async Task StopAsync(CancellationToken ct)
    {
        await eventTracker.WaitAllAsync(ct);
    }
}
```

This ensures events complete before the application stops.

## Use Cases

### Domain Events

<!-- snippet: events-domain-events -->
<a id='snippet-events-domain-events'></a>
```cs
[Factory]
public partial class OrderAggregate
{
    public Guid Id { get; private set; }
    public string Status { get; private set; } = "Pending";

    [Create]
    public OrderAggregate() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id)
    {
        Id = id;
        Status = "Loaded";
        return Task.FromResult(true);
    }

    [Remote, Fetch]
    public Task<bool> Approve(Guid id)
    {
        Id = id;
        Status = "Approved";
        return Task.FromResult(true);
    }

    // Domain event - update read model
    [Event]
    public async Task OnOrderApproved(
        Guid orderId,
        [Service] IOrderRepository repository,
        CancellationToken ct)
    {
        // Update read model or projection
        var order = await repository.GetByIdAsync(orderId, ct);
        if (order != null)
        {
            order.Status = "Approved";
            await repository.UpdateAsync(order, ct);
            await repository.SaveChangesAsync(ct);
        }
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L165-L208' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-domain-events' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Notifications

<!-- snippet: events-notifications -->
<a id='snippet-events-notifications'></a>
```cs
[Factory]
public partial class NotificationEvents
{
    [Create]
    public NotificationEvents() { }

    [Event]
    public async Task SendEmailNotification(
        string to,
        string subject,
        string body,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(to, subject, body, ct);
    }

    // Push notification - background processing
    [Event]
    public Task SendPushNotification(
        Guid userId,
        string message,
        CancellationToken ct)
    {
        // Push notification logic here
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L210-L239' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-notifications' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Audit Logging

<!-- snippet: events-audit -->
<a id='snippet-events-audit'></a>
```cs
[Factory]
public partial class AuditEvents
{
    [Create]
    public AuditEvents() { }

    [Event]
    public async Task LogAuditTrail(
        string action,
        Guid entityId,
        string entityType,
        string details,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // Fire-and-forget audit logging
        await auditLog.LogAsync(action, entityId, entityType, details, ct);
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L241-L261' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-audit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Integration Events

<!-- snippet: events-integration -->
<a id='snippet-events-integration'></a>
```cs
public interface IExternalApiClient
{
    Task NotifyAsync(Guid entityId, string eventType, CancellationToken ct);
}

public partial class MockExternalApiClient : IExternalApiClient
{
    public List<(Guid EntityId, string EventType)> Notifications { get; } = new();
    public Task NotifyAsync(Guid entityId, string eventType, CancellationToken ct)
    {
        Notifications.Add((entityId, eventType));
        return Task.CompletedTask;
    }
}

[Factory]
public partial class IntegrationEvents
{
    [Create]
    public IntegrationEvents() { }

    [Event]
    public async Task NotifyExternalSystem(
        Guid entityId,
        string eventType,
        [Service] IExternalApiClient apiClient,
        CancellationToken ct)
    {
        // Call external API - fire-and-forget
        await apiClient.NotifyAsync(entityId, eventType, ct);
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L290-L323' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-integration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Events vs Background Jobs

| Feature | Events ([Event]) | Background Jobs (Hangfire, etc.) |
|---------|------------------|----------------------------------|
| **Execution** | Immediate | Scheduled or queued |
| **Durability** | In-memory | Persistent |
| **Retry** | Manual | Automatic |
| **Monitoring** | EventTracker | Job dashboard |
| **Overhead** | Low | Moderate |
| **Use Case** | Fast fire-and-forget | Long-running, durable work |

Use Events when:
- Operation is fast (<1 second)
- Loss on crash is acceptable
- You want minimal infrastructure

Use Background Jobs when:
- Operation is long-running
- Must survive restarts
- Needs retry logic

## Authorization and Events

**Events always bypass authorization checks.** They are internal operations triggered by application code, not user requests.

<!-- snippet: events-authorization -->
<a id='snippet-events-authorization'></a>
```cs
// Events bypass authorization - they are internal operations
// triggered by application code, not user requests

public interface IProtectedEntityAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();
}

public partial class ProtectedEntityAuth : IProtectedEntityAuth
{
    private readonly IUserContext _userContext;
    public ProtectedEntityAuth(IUserContext userContext) { _userContext = userContext; }
    public bool CanCreate() => _userContext.IsAuthenticated;
}

[Factory]
[AuthorizeFactory<IProtectedEntityAuth>]
public partial class ProtectedEntityWithEvent
{
    public Guid Id { get; private set; }

    // Requires authorization
    [Create]
    public ProtectedEntityWithEvent() { Id = Guid.NewGuid(); }

    // Events BYPASS authorization - always execute
    [Event]
    public Task NotifyInternal(
        string message,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        // This runs regardless of user permissions
        return emailService.SendAsync("internal@example.com", "Internal", message, ct);
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L325-L363' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-authorization' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Events execute in a separate scope with no user context. If you need authorization for event-triggered logic, implement it manually within the event method.

## Events in Different Modes

**Server (Full) mode:**
- Events execute in background on server

**RemoteOnly mode:**
- Event call serialized, sent to server
- Executes in background on server
- Client returns immediately

**Logical mode:**
- Events execute in background locally
- Still uses EventTracker and scope isolation

## Testing Events

Wait for events in tests:

<!-- snippet: events-testing -->
<a id='snippet-events-testing'></a>
```cs
public partial class EventTestingPatterns
{
    // [Fact]
    public async Task TestEventSideEffects()
    {
        var scopes = SampleTestContainers.Scopes();
        var eventTracker = scopes.local.GetRequiredService<IEventTracker>();
        var emailService = scopes.local.GetRequiredService<MockEmailService>();

        // Fire event
        var sendEmail = scopes.local.GetRequiredService<OrderWithEvents.SendOrderConfirmationEvent>();
        var orderId = Guid.NewGuid();
        var customerEmail = "test@example.com";

        _ = sendEmail(orderId, customerEmail);

        // Wait for event completion
        await eventTracker.WaitAllAsync();

        // Assert side effects
        Assert.Contains(emailService.SentEmails,
            e => e.To == customerEmail && e.Subject == "Order Confirmation");
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L558-L583' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Testing multiple events:

<!-- snippet: events-testing-latch -->
<a id='snippet-events-testing-latch'></a>
```cs
public partial class EventTestingWithLatch
{
    // [Fact]
    public async Task TestMultipleEvents()
    {
        var scopes = SampleTestContainers.Scopes();

        var emailService = scopes.local.GetRequiredService<MockEmailService>();
        var sendEmail = scopes.local.GetRequiredService<OrderWithEvents.SendOrderConfirmationEvent>();

        // Fire multiple events
        _ = sendEmail(Guid.NewGuid(), "user1@example.com");
        _ = sendEmail(Guid.NewGuid(), "user2@example.com");

        // Wait using IEventTracker for all events to complete
        var eventTracker = scopes.local.GetRequiredService<IEventTracker>();
        await eventTracker.WaitAllAsync();

        // Assert all events completed
        Assert.Equal(2, emailService.SentEmails.Count);
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L585-L608' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-testing-latch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Correlation ID Tracking

Events inherit the correlation ID from the triggering operation:

<!-- snippet: events-correlation -->
<a id='snippet-events-correlation'></a>
```cs
[Factory]
public partial class CorrelatedEvent
{
    [Create]
    public CorrelatedEvent() { }

    [Event]
    public Task EventWithCorrelation(
        Guid entityId,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // CorrelationContext.CorrelationId is available in event handlers
        // Propagated from the original request
        var correlationId = CorrelationContext.CorrelationId ?? "no-correlation";

        return auditLog.LogAsync(
            "EventProcessed",
            entityId,
            "Entity",
            $"Correlation: {correlationId}",
            ct);
    }
}
```
<sup><a href='/src/docs/samples/EventsSamples.cs#L365-L390' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-correlation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Logs include:
```
CorrelationId: 12345 - OrderPlaced event started
CorrelationId: 12345 - OrderPlaced event completed
```

This ties event execution to the triggering request.

## Performance Considerations

Events are lightweight:
- **Memory**: One Task object per pending event
- **Thread pool**: One thread from Task.Run pool
- **Scope**: One DI scope per event (disposed after completion)

For thousands of concurrent events, consider:
- Rate limiting event generation
- Using a proper message queue (RabbitMQ, Azure Service Bus)
- Batching events

## Next Steps

- [Factory Operations](factory-operations.md) - All operation types
- [Service Injection](service-injection.md) - Inject services into events
- [ASP.NET Core Integration](aspnetcore-integration.md) - EventTrackerHostedService
