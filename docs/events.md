# Events

RemoteFactory supports fire-and-forget domain events with scope isolation via the `[Event]` attribute.

## Event Operation Basics

Events are asynchronous operations that run independently of the caller:

<!-- snippet: events-basic -->
<a id='snippet-events-basic'></a>
```cs
/// <summary>
/// Notifies HR when a new employee is created.
/// CancellationToken is required as the last parameter.
/// </summary>
[Event]
public async Task NotifyHROfNewEmployee(
    Guid employeeId,
    string employeeName,
    [Service] IEmailService emailService,
    CancellationToken ct)
{
    await emailService.SendAsync(
        "hr@company.com",
        $"New Employee: {employeeName}",
        $"Employee {employeeName} (ID: {employeeId}) has been added to the system.",
        ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Events/EmployeeEventHandlers.cs#L13-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-basic' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Event handler class demonstrating event delegate generation.
/// Events are invoked as delegates injected via [Service].
/// Naming convention: {HandlerClass}.{MethodName}Event
/// </summary>
[Factory]
public partial class EventCallerHandlers
{
    /// <summary>
    /// This generates a delegate: EventCallerHandlers.OnInsertEvent
    /// The delegate can be injected via [Service] in other factory methods.
    /// Usage: _ = onInsert(Id, FirstName); // Fire-and-forget
    /// </summary>
    [Event]
    public async Task OnInsert(
        Guid employeeId,
        string employeeName,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        await auditLog.LogAsync("Insert", employeeId, "Employee", $"Created {employeeName}", ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L200-L224' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-caller' title='Start of snippet'>anchor</a></sup>
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

The generated event delegate tracks tasks with IEventTracker:

```csharp
// Source: Generated event delegate pattern from Generated/Neatoo.Generator/...
public Task SendWelcomeEmailDelegate(Guid employeeId, string email)
{
    var task = Task.Run(async () =>
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<EmployeeEventHandler>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var ct = _hostLifetime.ApplicationStopping;  // Framework provides CancellationToken
            await handler.SendWelcomeEmail(employeeId, email, emailService, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Event SendWelcomeEmail failed");
        }
    });
    _eventTracker.Track(task);  // Track for graceful shutdown
    return task;
}
```

*Source: Pattern from `Generated/Neatoo.Generator/Neatoo.Factory/` for `[Event]` methods*

## Event Method Requirements

Event methods must:

1. Have `[Event]` attribute
2. Accept `CancellationToken` as the last parameter
3. Return `Task` or `void` (void methods are converted to Task delegates)

<!-- snippet: events-requirements -->
<a id='snippet-events-requirements'></a>
```cs
/// <summary>
/// Event methods must have CancellationToken as the last parameter.
/// </summary>
[Factory]
public partial class EventRequirements
{
    /// <summary>
    /// Required: CancellationToken as last parameter.
    /// Events return void or Task.
    /// </summary>
    [Event]
    public async Task NotifyOnChange(
        Guid entityId,
        string changeType,
        [Service] IEmailService emailService,
        CancellationToken ct)  // Required last parameter
    {
        await emailService.SendAsync(
            "notify@company.com",
            "Entity Changed",
            $"Entity {entityId} changed: {changeType}",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L8-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-requirements' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Events run in isolated DI scopes.
/// </summary>
[Factory]
public partial class EventScopeIsolation
{
    /// <summary>
    /// Each event handler gets its own DI scope.
    /// Scoped services are independent from the calling scope.
    /// </summary>
    [Event]
    public async Task ProcessInIsolatedScope(
        Guid entityId,
        [Service] IAuditLogService auditLog, // Scoped - isolated instance
        CancellationToken ct)
    {
        // This auditLog instance is separate from the caller's instance
        // Changes do not affect the original operation's scope
        await auditLog.LogAsync("Event", entityId, "Entity", "Processed in isolated scope", ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L112-L134' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-scope-isolation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Benefits:
- **Separate transactions**: Event failures don't roll back triggering operation
- **Independent lifetime**: Scoped services don't leak across events
- **Parallel execution**: Multiple events run concurrently without conflicts

### Example: Order Processing

<!-- snippet: events-scope-example -->
<a id='snippet-events-scope-example'></a>
```cs
/// <summary>
/// Complete example of scope isolation with parent-child pattern.
/// Event handlers run in isolated DI scopes.
/// </summary>
[Factory]
public partial class IsolatedEventHandlers
{
    /// <summary>
    /// Event handler runs in isolated scope.
    /// Each event handler gets its own DI scope.
    /// </summary>
    [Event]
    public async Task OnEmployeeCreated(
        Guid employeeId,
        string employeeName,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            "hr@company.com",
            "New Employee Created",
            $"Employee {employeeName} (ID: {employeeId}) created.",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L136-L162' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-scope-example' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

If the event fails:
- Employee is still saved (separate transaction)
- Event failure is logged by the application
- Client call succeeds

## CancellationToken Handling

Events receive the application shutdown token:

<!-- snippet: events-cancellation -->
<a id='snippet-events-cancellation'></a>
```cs
/// <summary>
/// Events respect cancellation during graceful shutdown.
/// </summary>
[Factory]
public partial class EventWithCancellation
{
    /// <summary>
    /// Event handlers should check cancellation for long-running operations.
    /// </summary>
    [Event]
    public async Task ProcessLongRunningTask(
        Guid taskId,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // Check before starting
        ct.ThrowIfCancellationRequested();

        await auditLog.LogAsync("Start", taskId, "Task", "Starting long process", ct);

        // Simulate work - check periodically
        for (int i = 0; i < 10; i++)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(100, ct);
        }

        await auditLog.LogAsync("Complete", taskId, "Task", "Finished long process", ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L35-L66' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The CancellationToken:
- Fires on `IHostApplicationLifetime.ApplicationStopping`
- Allows graceful cleanup
- Is required for all event methods

### Graceful Shutdown Example:

<!-- snippet: events-graceful-shutdown -->
<a id='snippet-events-graceful-shutdown'></a>
```cs
/// <summary>
/// Events support graceful shutdown via IEventTracker.
/// </summary>
[Factory]
public partial class ShutdownAwareEvents
{
    /// <summary>
    /// Event respects server shutdown cancellation.
    /// IEventTracker waits for pending events during shutdown.
    /// </summary>
    [Event]
    public async Task ProcessBeforeShutdown(
        Guid entityId,
        [Service] IAuditLogService auditLog,
        [Service] ILogger<ShutdownAwareEvents> logger,
        CancellationToken ct)
    {
        try
        {
            // Long operation - check cancellation
            ct.ThrowIfCancellationRequested();

            await auditLog.LogAsync("Processing", entityId, "Entity", "Started", ct);

            // Simulate work
            await Task.Delay(500, ct);

            await auditLog.LogAsync("Complete", entityId, "Entity", "Finished", ct);
        }
        catch (OperationCanceledException)
        {
            // Server is shutting down - log and exit gracefully
            logger.LogWarning("Event processing cancelled due to shutdown: {EntityId}", entityId);
            throw; // Re-throw to signal cancellation
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L301-L339' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-graceful-shutdown' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## EventTracker

The `IEventTracker` service monitors pending events.

### Accessing EventTracker

<!-- snippet: events-eventtracker-access -->
<a id='snippet-events-eventtracker-access'></a>
```cs
/// <summary>
/// Demonstrates injecting and using IEventTracker.
/// </summary>
public static class EventTrackerAccessSample
{
    /// <summary>
    /// IEventTracker is registered automatically by AddNeatooRemoteFactory/AddNeatooAspNetCore.
    /// </summary>
    public static void AccessEventTracker(IServiceProvider serviceProvider)
    {
        // Resolve IEventTracker from DI
        var eventTracker = serviceProvider.GetRequiredService<IEventTracker>();

        // Check pending event count
        var pendingCount = eventTracker.PendingCount;
        Console.WriteLine($"Pending events: {pendingCount}");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L341-L360' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-access' title='Start of snippet'>anchor</a></sup>
<a id='snippet-events-eventtracker-access-1'></a>
```cs
/// <summary>
/// Accessing IEventTracker for pending event monitoring.
/// </summary>
public class EventTrackerAccessTests
{
    [Fact]
    public void EventTracker_ResolvedFromDI()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();

        // Act - IEventTracker is registered by AddNeatooRemoteFactory
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();

        // Assert
        Assert.NotNull(eventTracker);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L560-L579' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-access-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Waiting for Events (Testing)

<!-- snippet: events-eventtracker-wait -->
<a id='snippet-events-eventtracker-wait'></a>
```cs
/// <summary>
/// Demonstrates waiting for all events in tests.
/// </summary>
public static class EventTrackerWaitSample
{
    /// <summary>
    /// Use WaitAllAsync to ensure all events complete before assertions.
    /// Essential for testing event-based functionality.
    /// </summary>
    public static async Task WaitForEventsInTest(IServiceProvider serviceProvider)
    {
        var eventTracker = serviceProvider.GetRequiredService<IEventTracker>();

        // Wait for all pending events to complete
        // Optionally pass CancellationToken for timeout
        await eventTracker.WaitAllAsync();

        // With timeout:
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await eventTracker.WaitAllAsync(cts.Token);

        // Now safe to assert event side effects
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L362-L387' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-wait' title='Start of snippet'>anchor</a></sup>
<a id='snippet-events-eventtracker-wait-1'></a>
```cs
/// <summary>
/// Waiting for all events to complete.
/// </summary>
public class EventTrackerWaitTests
{
    [Fact]
    public async Task WaitAllAsync_CompletesWhenAllEventsFinish()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();

        // Act - Wait for all pending events
        var waitTask = eventTracker.WaitAllAsync();

        // Assert - WaitAllAsync completes
        await waitTask;
        Assert.Equal(0, eventTracker.PendingCount);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L603-L624' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-wait-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Monitoring Pending Events

<!-- snippet: events-eventtracker-count -->
<a id='snippet-events-eventtracker-count'></a>
```cs
/// <summary>
/// Demonstrates monitoring pending events with PendingCount.
/// </summary>
public static class EventTrackerCountSample
{
    /// <summary>
    /// PendingCount useful for health checks and monitoring.
    /// </summary>
    public static void MonitorPendingEvents(
        IEventTracker eventTracker,
        ILogger logger)
    {
        // Check pending event count
        var pendingCount = eventTracker.PendingCount;

        // Log if too many events are queued
        if (pendingCount > 100)
        {
            logger.LogWarning("High pending event count: {Count}", pendingCount);
        }

        // Use in health check endpoints
        // return pendingCount < threshold ? HealthCheckResult.Healthy() : HealthCheckResult.Degraded();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L389-L415' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-count' title='Start of snippet'>anchor</a></sup>
<a id='snippet-events-eventtracker-count-1'></a>
```cs
/// <summary>
/// Monitoring pending event count.
/// </summary>
public class EventTrackerCountTests
{
    [Fact]
    public async Task PendingCount_AfterWaitAll_IsZero()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();

        // Act - Wait for any pending events
        await eventTracker.WaitAllAsync();

        // Assert - No pending events after wait
        Assert.Equal(0, eventTracker.PendingCount);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L581-L601' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-count-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Error Handling

Event exceptions should be handled within the event method. Unhandled exceptions are caught by the generated wrapper, logged, and suppressed to preserve fire-and-forget semantics:

<!-- snippet: events-error-handling -->
<a id='snippet-events-error-handling'></a>
```cs
/// <summary>
/// Event errors are logged but do not fail the parent operation.
/// </summary>
[Factory]
public partial class EventErrorHandling
{
    /// <summary>
    /// Exceptions in event handlers are logged but suppressed.
    /// The calling operation continues successfully.
    /// </summary>
    [Event]
    public async Task RiskyNotification(
        Guid entityId,
        [Service] IEmailService emailService,
        [Service] ILogger<EventErrorHandling> logger,
        CancellationToken ct)
    {
        try
        {
            await emailService.SendAsync(
                "invalid-email", // May fail
                "Notification",
                $"Event for {entityId}",
                ct);
        }
        catch (Exception ex)
        {
            // Log error but do not re-throw
            // The parent operation already completed
            logger.LogError(ex, "Failed to send notification for {EntityId}", entityId);
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L164-L198' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-error-handling' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Events with ASP.NET Core integration.
/// </summary>
[Factory]
public partial class AspNetCoreEventHandlers
{
    /// <summary>
    /// Event handler running in ASP.NET Core context.
    /// Events run in isolated scopes with their own DI resolution.
    /// </summary>
    [Event]
    public async Task OnEmployeeCreated(
        Guid employeeId,
        string employeeName,
        [Service] IEmailService emailService,
        [Service] Microsoft.Extensions.Logging.ILogger<AspNetCoreEventHandlers> logger,
        CancellationToken ct)
    {
        var correlationId = CorrelationContext.CorrelationId;

        logger.LogInformation(
            "Processing employee created event. EmployeeId: {EmployeeId}, CorrelationId: {CorrelationId}",
            employeeId, correlationId);

        await emailService.SendAsync(
            "hr@company.com",
            $"New Employee: {employeeName}",
            $"Employee {employeeName} (ID: {employeeId}) created.",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L331-L363' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-aspnetcore' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Domain event for employee promotion.
/// </summary>
[Event]
public async Task NotifyManagerOfPromotion(
    Guid employeeId,
    string employeeName,
    string oldPosition,
    string newPosition,
    [Service] IEmailService emailService,
    [Service] IEmployeeRepository repository,
    CancellationToken ct)
{
    var employee = await repository.GetByIdAsync(employeeId, ct);
    var departmentId = employee?.DepartmentId ?? Guid.Empty;

    await emailService.SendAsync(
        "manager@company.com",
        $"Employee Promotion: {employeeName}",
        $"{employeeName} has been promoted from {oldPosition} to {newPosition}. Department: {departmentId}",
        ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Events/EmployeeEventHandlers.cs#L33-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-domain-events' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Notifications

<!-- snippet: events-notifications -->
<a id='snippet-events-notifications'></a>
```cs
/// <summary>
/// Events are ideal for notification dispatch.
/// </summary>
[Factory]
public partial class NotificationEvents
{
    /// <summary>
    /// Fire-and-forget email notification.
    /// Does not block the calling operation.
    /// </summary>
    [Event]
    public async Task SendWelcomeEmail(
        string recipientEmail,
        string employeeName,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            recipientEmail,
            $"Welcome, {employeeName}!",
            "You have been added to our employee management system.",
            ct);
    }

    /// <summary>
    /// Notify manager of team changes.
    /// </summary>
    [Event]
    public async Task NotifyManagerOfTeamChange(
        string managerEmail,
        string changeDescription,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            managerEmail,
            "Team Update",
            changeDescription,
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L68-L110' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-notifications' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Audit Logging

<!-- snippet: events-audit -->
<a id='snippet-events-audit'></a>
```cs
/// <summary>
/// Audit logging event for employee departure.
/// </summary>
[Event]
public async Task LogEmployeeDeparture(
    Guid employeeId,
    string reason,
    [Service] IAuditLogService auditLog,
    CancellationToken ct)
{
    await auditLog.LogAsync(
        "Departure",
        employeeId,
        "Employee",
        $"Employee departed. Reason: {reason}",
        ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Events/EmployeeEventHandlers.cs#L58-L76' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-audit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Integration Events

<!-- snippet: events-integration -->
<a id='snippet-events-integration'></a>
```cs
/// <summary>
/// Events for external system integration.
/// </summary>
[Factory]
public partial class IntegrationEvents
{
    /// <summary>
    /// Sync employee to external HR system.
    /// Fire-and-forget prevents blocking main operation.
    /// </summary>
    [Event]
    public async Task SyncToExternalHR(
        Guid employeeId,
        string firstName,
        string lastName,
        [Service] IExternalHRSync hrSync,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        var success = await hrSync.SyncEmployeeAsync(employeeId, firstName, lastName, ct);

        await auditLog.LogAsync(
            success ? "SyncSuccess" : "SyncFailed",
            employeeId,
            "ExternalHR",
            $"External HR sync: {(success ? "completed" : "failed")}",
            ct);
    }
}

/// <summary>
/// External HR system integration service.
/// </summary>
public interface IExternalHRSync
{
    Task<bool> SyncEmployeeAsync(Guid employeeId, string firstName, string lastName, CancellationToken ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L226-L264' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-integration' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Events bypass authorization - they are internal operations, not user requests.
/// </summary>
[Factory]
public partial class EventWithoutAuthorization
{
    /// <summary>
    /// No [AuthorizeFactory] attribute - events don't check authorization.
    /// Events run in isolated scope without user context.
    /// </summary>
    [Event]
    public async Task ProcessInternalOperation(
        Guid entityId,
        string operationType,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // Events are triggered by application code, not user requests.
        // Authorization is not applicable - there's no user context.
        //
        // If you need to restrict who can trigger the event,
        // implement that check in the code that calls the event delegate,
        // NOT in the event handler itself.

        await auditLog.LogAsync(
            operationType,
            entityId,
            "Internal",
            $"Internal operation: {operationType}",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventsSamples.cs#L266-L299' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-authorization' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Testing event handlers via delegate injection.
/// Events are fired via generated delegates, not factory methods.
/// </summary>
public class EventsTests
{
    [Fact]
    public async Task EventDelegate_FiresAsynchronously()
    {
        // Arrange - Clear any previous data
        InMemoryEmailService.Clear();

        var scopes = TestClientServerContainers.CreateScopes();

        // Events are invoked via delegates resolved from DI
        // The delegate is: EmployeeEventHandlers.NotifyHROfNewEmployeeEvent
        var notifyDelegate = scopes.local.ServiceProvider
            .GetRequiredService<EmployeeManagement.Domain.Events.EmployeeEventHandlers.NotifyHROfNewEmployeeEvent>();

        // Act - Invoke event delegate (fire-and-forget)
        await notifyDelegate(
            Guid.NewGuid(),
            "John Doe");

        // Assert - Wait for event to complete
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();
        await eventTracker.WaitAllAsync();

        // Verify email was sent
        var emails = InMemoryEmailService.GetSentEmails();
        Assert.Contains(emails, e =>
            e.Recipient == "hr@company.com" &&
            e.Subject.Contains("John Doe", StringComparison.Ordinal));
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L371-L407' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Testing multiple events:

<!-- snippet: events-testing-latch -->
<a id='snippet-events-testing-latch'></a>
```cs
/// <summary>
/// Testing events with completion latch.
/// </summary>
public class EventLatchTests
{
    [Fact]
    public async Task WaitForEventCompletion()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();

        // Act - Fire event (would be done via delegate injection in real code)
        // Wait for all pending events
        await eventTracker.WaitAllAsync();

        // Assert - All events completed
        Assert.Equal(0, eventTracker.PendingCount);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L409-L430' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-testing-latch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Correlation ID Tracking

Events inherit the correlation ID from the triggering operation:

<!-- snippet: events-correlation -->
<a id='snippet-events-correlation'></a>
```cs
/// <summary>
/// Events with correlation ID propagation.
/// </summary>
[Factory]
public partial class CorrelatedEventHandlers
{
    /// <summary>
    /// Access correlation ID in event handlers for distributed tracing.
    /// </summary>
    [Event]
    public async Task LogWithCorrelation(
        Guid entityId,
        string action,
        [Service] IAuditLogService auditLog,
        [Service] Microsoft.Extensions.Logging.ILogger<CorrelatedEventHandlers> logger,
        CancellationToken ct)
    {
        var correlationId = CorrelationContext.CorrelationId;

        logger.LogInformation(
            "Event processing with correlation {CorrelationId}",
            correlationId);

        await auditLog.LogAsync(
            action,
            entityId,
            "Event",
            $"Processed with correlation {correlationId}",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L365-L397' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-correlation' title='Start of snippet'>anchor</a></sup>
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
