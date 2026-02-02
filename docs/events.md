# Events

RemoteFactory supports fire-and-forget domain events with scope isolation via the `[Event]` attribute.

## Event Operation Basics

Events are asynchronous operations that run independently of the caller:

<!-- snippet: events-basic -->
<a id='snippet-events-basic'></a>
```cs
// [Event] marks fire-and-forget methods that run in isolated DI scopes
[Event]
public async Task NotifyHROfNewEmployee(
    Guid employeeId, string employeeName,
    [Service] IEmailService emailService, CancellationToken ct)
{
    await emailService.SendAsync("hr@company.com", $"New Employee: {employeeName}", $"ID: {employeeId}", ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Events/EmployeeEventHandlers.cs#L9-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-basic' title='Start of snippet'>anchor</a></sup>
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
// Fire event via generated delegate - returns immediately
public class EmployeeEventCaller
{
    private readonly EmployeeBasicEvent.SendWelcomeEmailEvent _sendWelcomeEmail;
    private readonly IEventTracker _eventTracker;

    public EmployeeEventCaller(EmployeeBasicEvent.SendWelcomeEmailEvent sendWelcomeEmail, IEventTracker eventTracker)
    {
        _sendWelcomeEmail = sendWelcomeEmail;
        _eventTracker = eventTracker;
    }

    public void OnboardEmployee(Guid employeeId, string email)
    {
        _ = _sendWelcomeEmail(employeeId, email); // Fire-and-forget
    }

    public async Task WaitForEventsAsync(CancellationToken ct) => await _eventTracker.WaitAllAsync(ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Events/EventCallerSamples.cs#L6-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-caller' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The factory immediately returns without waiting.

### 2. Event Executes in Background

Event runs in a new DI scope:

```csharp
// Pseudo-code showing what happens
Task.Run(async () =>
{
    using var scope = serviceProvider.CreateScope();
    var entity = scope.ServiceProvider.GetRequiredService<Employee>();
    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
    var ct = hostApplicationLifetime.ApplicationStopping;
    await entity.SendWelcomeEmail(employeeId, email, emailService, ct);
});
```

### 3. EventTracker Monitors Completion

The generated factory tracks the event Task:

<!-- snippet: events-tracker-generated -->
<a id='snippet-events-tracker-generated'></a>
```cs
// Generated for [Event] method SendWelcomeEmail(Guid employeeId, string email, ...):
//   public delegate Task SendWelcomeEmailEvent(Guid employeeId, string email);
//
// Factory runs in Task.Run with new DI scope, tracks via IEventTracker:
//   var task = Task.Run(async () => {
//       using var scope = _sp.CreateScope();
//       var ct = _lifetime.ApplicationStopping;
//       await entity.SendWelcomeEmail(employeeId, email, emailService, ct);
//   });
//   _eventTracker.Track(task);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/GeneratedCodeSamples.cs#L3-L14' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-tracker-generated' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Event Method Requirements

Event methods must:

1. Have `[Event]` attribute
2. Accept `CancellationToken` as the last parameter
3. Return `Task` or `void` (void methods are converted to Task delegates)

<!-- snippet: events-requirements -->
<a id='snippet-events-requirements'></a>
```cs
// CancellationToken required as last parameter; return Task or void
[Factory]
public partial class EventRequirements
{
    [Event]
    public async Task NotifyOnChange(Guid entityId, string changeType,
        [Service] IEmailService emailService, CancellationToken ct)
    {
        await emailService.SendAsync("notify@company.com", "Changed", $"{entityId}: {changeType}", ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L8-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-requirements' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The generator converts void methods to Task automatically.

### Generated Event Delegates

For each `[Event]` method, the generator creates a delegate type named `{MethodName}Event`:

```csharp
// [Event] method
public async Task SendWelcomeEmail(Guid employeeId, string email, ...) { }

// Generated delegate
public delegate Task SendWelcomeEmailEvent(Guid employeeId, string email);
```

Register and invoke the delegate via DI:

```csharp
var sendWelcome = serviceProvider.GetRequiredService<Employee.SendWelcomeEmailEvent>();
_ = sendWelcome(employeeId, email); // Fire-and-forget
```

## Scope Isolation

Each event gets a new DI scope:

<!-- snippet: events-scope-isolation -->
<a id='snippet-events-scope-isolation'></a>
```cs
// Each event gets isolated DI scope - scoped services independent from caller
[Factory]
public partial class EventScopeIsolation
{
    [Event]
    public async Task ProcessInIsolatedScope(Guid entityId, [Service] IAuditLogService auditLog, CancellationToken ct)
    {
        await auditLog.LogAsync("Event", entityId, "Entity", "Processed in isolated scope", ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L51-L62' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-scope-isolation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Benefits:
- **Separate transactions**: Event failures don't roll back triggering operation
- **Independent lifetime**: Scoped services don't leak across events
- **Parallel execution**: Multiple events run concurrently without conflicts

If the event fails:
- Employee is still saved (separate transaction)
- Event failure is logged by the application
- Client call succeeds

## CancellationToken Handling

Events receive the application shutdown token:

<!-- snippet: events-cancellation -->
<a id='snippet-events-cancellation'></a>
```cs
// Check cancellation for long-running operations
[Factory]
public partial class EventWithCancellation
{
    [Event]
    public async Task ProcessLongRunningTask(Guid taskId, [Service] IAuditLogService auditLog, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await auditLog.LogAsync("Start", taskId, "Task", "Processing", ct);
        for (int i = 0; i < 10; i++) { ct.ThrowIfCancellationRequested(); await Task.Delay(100, ct); }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L22-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The CancellationToken:
- Fires on `IHostApplicationLifetime.ApplicationStopping`
- Allows graceful cleanup
- Is required for all event methods

### Graceful Shutdown Example:

<!-- snippet: events-graceful-shutdown -->
<a id='snippet-events-graceful-shutdown'></a>
```cs
// IEventTracker waits for pending events during shutdown
[Factory]
public partial class ShutdownAwareEvents
{
    [Event]
    public async Task ProcessBeforeShutdown(Guid entityId,
        [Service] IAuditLogService auditLog, [Service] ILogger<ShutdownAwareEvents> logger,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await auditLog.LogAsync("Processing", entityId, "Entity", "Started", ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L139-L153' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-graceful-shutdown' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## EventTracker

The `IEventTracker` service monitors pending events.

### Accessing EventTracker

<!-- snippet: events-eventtracker-access -->
<a id='snippet-events-eventtracker-access'></a>
```cs
// IEventTracker registered by AddNeatooRemoteFactory/AddNeatooAspNetCore
public static class EventTrackerAccessSample
{
    public static void AccessEventTracker(IServiceProvider sp)
    {
        var tracker = sp.GetRequiredService<IEventTracker>();
        Console.WriteLine($"Pending: {tracker.PendingCount}");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L155-L165' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-access' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Waiting for Events (Testing)

<!-- snippet: events-eventtracker-wait -->
<a id='snippet-events-eventtracker-wait'></a>
```cs
// WaitAllAsync for tests - ensures events complete before assertions
public static class EventTrackerWaitSample
{
    public static async Task WaitForEventsInTest(IServiceProvider sp)
    {
        var tracker = sp.GetRequiredService<IEventTracker>();
        await tracker.WaitAllAsync(); // With timeout: await tracker.WaitAllAsync(cts.Token);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L167-L177' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-wait' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Monitoring Pending Events

<!-- snippet: events-eventtracker-count -->
<a id='snippet-events-eventtracker-count'></a>
```cs
// PendingCount for health checks and monitoring
public static class EventTrackerCountSample
{
    public static void MonitorPendingEvents(IEventTracker tracker, ILogger logger)
    {
        if (tracker.PendingCount > 100) logger.LogWarning("High pending: {Count}", tracker.PendingCount);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L179-L188' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-count' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Error Handling

Event exceptions should be handled within the event method. Unhandled exceptions are caught by the generated wrapper, logged, and suppressed to preserve fire-and-forget semantics:

<!-- snippet: events-error-handling -->
<a id='snippet-events-error-handling'></a>
```cs
// Catch exceptions within event - parent operation already completed
[Factory]
public partial class EventErrorHandling
{
    [Event]
    public async Task RiskyNotification(Guid entityId,
        [Service] IEmailService emailService, [Service] ILogger<EventErrorHandling> logger, CancellationToken ct)
    {
        try { await emailService.SendAsync("invalid", "Notification", $"Event for {entityId}", ct); }
#pragma warning disable CA1031
        catch (Exception ex) { logger.LogError(ex, "Failed notification for {EntityId}", entityId); }
#pragma warning restore CA1031
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L76-L91' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-error-handling' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The generated event delegate wraps execution:

```csharp
// Generated delegate (simplified)
public Task SendWelcomeEmailDelegate(Guid employeeId, string email)
{
    var task = Task.Run(async () =>
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var entity = scope.ServiceProvider.GetRequiredService<Employee>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            await entity.SendWelcomeEmail(employeeId, email, emailService, _ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Event SendWelcomeEmail failed");
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
// AddNeatooAspNetCore registers:
// - IEventTracker (singleton): tracks pending event Tasks, provides PendingCount/WaitAllAsync
// - EventTrackerHostedService: calls WaitAllAsync on shutdown for graceful completion
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Events/AspNetCoreIntegrationSamples.cs#L3-L7' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-aspnetcore' title='Start of snippet'>anchor</a></sup>
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
// Domain events can inject repositories and services
[Event]
public async Task NotifyManagerOfPromotion(Guid employeeId, string name,
    string oldPos, string newPos, [Service] IEmailService email,
    [Service] IEmployeeRepository repo, CancellationToken ct)
{
    var emp = await repo.GetByIdAsync(employeeId, ct);
    await email.SendAsync("manager@company.com", "Promoted", $"{name}: {oldPos}->{newPos}", ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Events/EmployeeEventHandlers.cs#L20-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-domain-events' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Notifications

<!-- snippet: events-notifications -->
<a id='snippet-events-notifications'></a>
```cs
// Fire-and-forget notifications
[Factory]
public partial class NotificationEvents
{
    [Event]
    public async Task SendWelcomeEmail(string recipientEmail, string employeeName,
        [Service] IEmailService emailService, CancellationToken ct)
    {
        await emailService.SendAsync(recipientEmail, $"Welcome, {employeeName}!", "Added to system.", ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L37-L49' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-notifications' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Audit Logging

<!-- snippet: events-audit -->
<a id='snippet-events-audit'></a>
```cs
// Audit logging as fire-and-forget event
[Event]
public async Task LogEmployeeDeparture(
    Guid employeeId, string reason,
    [Service] IAuditLogService auditLog, CancellationToken ct)
{
    await auditLog.LogAsync("Departure", employeeId, "Employee", reason, ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Events/EmployeeEventHandlers.cs#L32-L41' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-audit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Integration Events

<!-- snippet: events-integration -->
<a id='snippet-events-integration'></a>
```cs
// External system integration as fire-and-forget
[Factory]
public partial class IntegrationEvents
{
    [Event]
    public async Task SyncToExternalHR(Guid employeeId, string firstName, string lastName,
        [Service] IExternalHRSync hrSync, [Service] IAuditLogService auditLog, CancellationToken ct)
    {
        var success = await hrSync.SyncEmployeeAsync(employeeId, firstName, lastName, ct);
        await auditLog.LogAsync(success ? "SyncSuccess" : "SyncFailed", employeeId, "ExternalHR", "", ct);
    }
}

public interface IExternalHRSync
{
    Task<bool> SyncEmployeeAsync(Guid employeeId, string firstName, string lastName, CancellationToken ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L105-L123' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-integration' title='Start of snippet'>anchor</a></sup>
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
// Events bypass authorization - they're internal operations, not user requests
[Factory]
public partial class EventWithoutAuthorization
{
    [Event]
    public async Task ProcessInternalOperation(Guid entityId, string operationType,
        [Service] IAuditLogService auditLog, CancellationToken ct)
    {
        await auditLog.LogAsync(operationType, entityId, "Internal", operationType, ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L125-L137' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-authorization' title='Start of snippet'>anchor</a></sup>
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
// Fire event delegate, wait via IEventTracker, assert side effects
public static class EventTestingPatternSample
{
    public static async Task TestWelcomeEmailEvent(IServiceProvider sp, IEventTracker tracker)
    {
        var sendEmail = sp.GetRequiredService<EmployeeBasicEvent.SendWelcomeEmailEvent>();
        InMemoryEmailService.Clear();
        _ = sendEmail(Guid.NewGuid(), "test@example.com");
        await tracker.WaitAllAsync();
        Assert.Single(InMemoryEmailService.GetSentEmails());
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/Events/EventTestingSamples.cs#L8-L21' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Testing multiple events:

<!-- snippet: events-testing-latch -->
<a id='snippet-events-testing-latch'></a>
```cs
// Multiple concurrent events - WaitAllAsync waits for all
public static class MultipleEventTestSample
{
    public static async Task TestMultipleConcurrentEvents(IServiceProvider sp, IEventTracker tracker)
    {
        var sendEmail = sp.GetRequiredService<EmployeeBasicEvent.SendWelcomeEmailEvent>();
        InMemoryEmailService.Clear();
        _ = sendEmail(Guid.NewGuid(), "emp1@example.com");
        _ = sendEmail(Guid.NewGuid(), "emp2@example.com");
        await tracker.WaitAllAsync();
        Assert.Equal(2, InMemoryEmailService.GetSentEmails().Count);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/Events/EventTestingSamples.cs#L23-L37' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-testing-latch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Correlation ID Tracking

Events inherit the correlation ID from the triggering operation:

<!-- snippet: events-correlation -->
<a id='snippet-events-correlation'></a>
```cs
// Correlation ID propagates from triggering request to event
[Factory]
public partial class EmployeeCorrelation
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public void Create(string name) { Id = Guid.NewGuid(); Name = name; }

    [Event]
    public async Task LogWithCorrelation(Guid employeeId, string action,
        [Service] ICorrelationContext ctx, [Service] IAuditLogService auditLog, CancellationToken ct)
    {
        await auditLog.LogAsync(action, employeeId, "Employee", $"CorrelationId: {ctx.CorrelationId}", ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/CorrelationSamples.cs#L6-L24' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-correlation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Logs include:
```
CorrelationId: 12345 - EmployeeActivated event started
CorrelationId: 12345 - EmployeeActivated event completed
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
