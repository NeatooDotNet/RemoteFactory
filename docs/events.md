# Events

RemoteFactory supports fire-and-forget domain events with scope isolation via the `[Event]` attribute.

## Event Operation Basics

Events are asynchronous operations that run independently of the caller:

<!-- snippet: events-basic -->
<a id='snippet-events-basic'></a>
```cs
/// <summary>
/// Employee aggregate demonstrating basic event pattern.
/// </summary>
[Factory]
public partial class EmployeeBasicEvent
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";

    [Create]
    public void Create(string firstName, string lastName, string email)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }

    /// <summary>
    /// Sends a welcome email asynchronously.
    /// Event executes in a new DI scope with fire-and-forget semantics.
    /// </summary>
    [Event]
    public async Task SendWelcomeEmail(
        Guid employeeId,
        string email,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            email,
            "Welcome to the Company!",
            $"Welcome! Your employee ID is {employeeId}.",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/BasicEventSamples.cs#L6-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-basic' title='Start of snippet'>anchor</a></sup>
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
/// Demonstrates how to invoke events from application code.
/// </summary>
public class EmployeeEventCaller
{
    private readonly EmployeeBasicEvent.SendWelcomeEmailEvent _sendWelcomeEmail;
    private readonly IEventTracker _eventTracker;

    public EmployeeEventCaller(
        EmployeeBasicEvent.SendWelcomeEmailEvent sendWelcomeEmail,
        IEventTracker eventTracker)
    {
        _sendWelcomeEmail = sendWelcomeEmail;
        _eventTracker = eventTracker;
    }

    /// <summary>
    /// Creates employee and fires welcome email event.
    /// </summary>
    public async Task OnboardEmployeeAsync(Guid employeeId, string email)
    {
        // Fire event - returns immediately without waiting
        // Code continues executing while event runs in background
        _ = _sendWelcomeEmail(employeeId, email);

        // Execution continues immediately - email sends asynchronously
        Console.WriteLine("Employee onboarded - welcome email queued");
    }

    /// <summary>
    /// For testing: wait for all pending events to complete.
    /// </summary>
    public async Task WaitForEventsAsync(CancellationToken ct)
    {
        // Use IEventTracker.WaitAllAsync() in tests to verify event side effects
        await _eventTracker.WaitAllAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Events/EventCallerSamples.cs#L6-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-caller' title='Start of snippet'>anchor</a></sup>
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
// Generated Event Delegate Pattern
//
// For an [Event] method like:
//   public async Task SendWelcomeEmail(Guid employeeId, string email,
//       [Service] IEmailService emailService, CancellationToken ct)
//
// The source generator produces:
//
// 1. Delegate type:
//    public delegate Task SendWelcomeEmailEvent(Guid employeeId, string email);
//
// 2. Factory implementation (simplified):
//    public Task SendWelcomeEmailDelegate(Guid employeeId, string email)
//    {
//        var task = Task.Run(async () =>
//        {
//            // Create new DI scope for isolation
//            using var scope = _serviceProvider.CreateScope();
//
//            // Resolve entity and services from new scope
//            var entity = scope.ServiceProvider.GetRequiredService<Employee>();
//            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
//
//            // Get cancellation token from host lifetime
//            var ct = _hostApplicationLifetime.ApplicationStopping;
//
//            // Execute the event method
//            await entity.SendWelcomeEmail(employeeId, email, emailService, ct);
//        });
//
//        // Track the task for graceful shutdown
//        _eventTracker.Track(task);
//
//        // Return task for optional awaiting (fire-and-forget callers ignore it)
//        return task;
//    }
//
// Key points:
// - Each event gets its own DI scope
// - CancellationToken comes from ApplicationStopping
// - EventTracker monitors the task
// - Caller receives the task but typically ignores it (_ = ...)
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/GeneratedCodeSamples.cs#L3-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-tracker-generated' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Event Method Requirements

Event methods must:

1. Have `[Event]` attribute
2. Accept `CancellationToken` as the last parameter
3. Return `Task` or `void` (void methods are converted to Task delegates)

<!-- snippet: events-requirements -->
<a id='snippet-events-requirements'></a>
```cs
/// <summary>
/// Demonstrates valid event method signatures.
/// </summary>
[Factory]
public partial class EmployeeEventSignatures
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public void Create(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <summary>
    /// Returns Task with service injection - standard pattern.
    /// </summary>
    [Event]
    public async Task ValidEvent(
        Guid employeeId,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            "notifications@company.com",
            "Event Notification",
            $"Event triggered for {employeeId}",
            ct);
    }

    /// <summary>
    /// Returns void - generator converts to Task delegate.
    /// </summary>
    [Event]
    public void VoidEvent(
        Guid employeeId,
        string message,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Sync operation completed
        Console.WriteLine($"Void event for {employeeId}: {message}");
    }

    /// <summary>
    /// Async Task with explicit await.
    /// </summary>
    [Event]
    public async Task AsyncEvent(
        Guid employeeId,
        int delayMs,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        await Task.Delay(delayMs, ct);
        await auditLog.LogAsync(
            "AsyncEvent",
            employeeId,
            "Employee",
            $"Completed after {delayMs}ms delay",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/EventRequirementsSamples.cs#L6-L72' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-requirements' title='Start of snippet'>anchor</a></sup>
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
/// Demonstrates scope isolation for event execution.
/// </summary>
[Factory]
public partial class EmployeeScopeIsolation
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public void Create(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <summary>
    /// Event runs in isolated DI scope with fresh service instances.
    /// </summary>
    [Event]
    public async Task ProcessInIsolatedScope(
        Guid employeeId,
        string action,
        // NEW scoped instance - independent of caller's scope
        [Service] IEmployeeRepository repository,
        // NEW scoped instance - separate from any other concurrent events
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // Repository is a fresh scoped instance
        var employee = await repository.GetByIdAsync(employeeId, ct);

        // Audit service is also a fresh scoped instance
        await auditLog.LogAsync(
            action,
            employeeId,
            "Employee",
            $"Processed {employee?.FirstName ?? "unknown"} in isolated scope",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/ScopeIsolationSamples.cs#L6-L48' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-scope-isolation' title='Start of snippet'>anchor</a></sup>
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
/// Employee with transactional independence between operation and event.
/// </summary>
[Factory]
public partial class EmployeeTransactional : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime? HireDate { get; private set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public void Create(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        Status = "Pending";
    }

    /// <summary>
    /// Hires employee and saves to repository.
    /// </summary>
    [Remote, Insert]
    public async Task HireEmployee(
        [Service] IEmployeeRepository repository,
        [Service] TransactionalEventHandlers.LogEmployeeHiredEvent logHired,
        CancellationToken ct)
    {
        Status = "Active";
        HireDate = DateTime.UtcNow;

        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = Name,
            LastName = "",
            Email = $"{Name.ToLowerInvariant()}@company.com",
            Position = "New Hire",
            HireDate = HireDate.Value
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;

        // Fire event in separate transaction (fire-and-forget)
        // If event fails, HireEmployee still succeeds
        _ = logHired(Id, Name, HireDate.Value);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/ScopeIsolationSamples.cs#L81-L134' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-scope-example' title='Start of snippet'>anchor</a></sup>
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
/// Demonstrates proper cancellation token handling in events.
/// </summary>
[Factory]
public partial class EmployeeCancellation
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public void Create(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <summary>
    /// Event with proper cancellation handling for graceful shutdown.
    /// </summary>
    [Event]
    public async Task SendBatchNotifications(
        Guid employeeId,
        string[] recipients,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        // Check cancellation before starting work
        ct.ThrowIfCancellationRequested();

        foreach (var recipient in recipients)
        {
            // Check cancellation in long-running loops
            if (ct.IsCancellationRequested)
            {
                Console.WriteLine($"Cancellation requested, stopping batch for {employeeId}");
                break;
            }

            // Pass token to async operations for cancellation-aware execution
            await emailService.SendAsync(
                recipient,
                $"Notification for Employee {employeeId}",
                "This is an automated notification.",
                ct);
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/CancellationSamples.cs#L6-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-cancellation' title='Start of snippet'>anchor</a></sup>
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
/// Demonstrates ASP.NET Core event tracking configuration.
/// </summary>
public static class EventGracefulShutdownConfig
{
    public static void ConfigureEventTracking(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // AddNeatooAspNetCore registers:
        // - IEventTracker (singleton): Monitors pending fire-and-forget events
        // - EventTrackerHostedService (IHostedService): Handles graceful shutdown
        services.AddNeatooAspNetCore(domainAssembly);

        // Shutdown sequence:
        // 1. ApplicationStopping token is triggered
        // 2. EventTrackerHostedService.StopAsync is called
        // 3. EventTrackerHostedService waits for pending events via WaitAllAsync
        // 4. Running events receive cancellation signal
        // 5. Application exits after events complete (or timeout)
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Events/GracefulShutdownSamples.cs#L7-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-graceful-shutdown' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## EventTracker

The `IEventTracker` service monitors pending events.

### Accessing EventTracker

<!-- snippet: events-eventtracker-access -->
<a id='snippet-events-eventtracker-access'></a>
```cs
/// <summary>
/// Demonstrates IEventTracker singleton access and basic usage.
/// </summary>
public class EventTrackerAccessDemo
{
    private readonly IEventTracker _eventTracker;
    private readonly EmployeeBasicEvent.SendWelcomeEmailEvent _sendWelcomeEmail;

    public EventTrackerAccessDemo(
        IEventTracker eventTracker,
        EmployeeBasicEvent.SendWelcomeEmailEvent sendWelcomeEmail)
    {
        // IEventTracker is a singleton registered by AddNeatooAspNetCore
        _eventTracker = eventTracker;
        _sendWelcomeEmail = sendWelcomeEmail;
    }

    public async Task DemonstrateEventTrackerAsync()
    {
        // Fire multiple events
        _ = _sendWelcomeEmail(Guid.NewGuid(), "employee1@company.com");
        _ = _sendWelcomeEmail(Guid.NewGuid(), "employee2@company.com");
        _ = _sendWelcomeEmail(Guid.NewGuid(), "employee3@company.com");

        // Check pending count (may be 0 if events complete quickly)
        var pendingCount = _eventTracker.PendingCount;
        Console.WriteLine($"Pending events: {pendingCount}");

        // Wait for all events to complete
        await _eventTracker.WaitAllAsync();

        // Verify all events completed
        if (_eventTracker.PendingCount != 0)
        {
            throw new InvalidOperationException("Expected no pending events");
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Events/EventTrackerSamples.cs#L6-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-access' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Waiting for Events (Testing)

<!-- snippet: events-eventtracker-wait -->
<a id='snippet-events-eventtracker-wait'></a>
```cs
/// <summary>
/// Test demonstrating event side effect verification.
/// </summary>
public static class EventWaitTestSample
{
    public static async Task VerifyEventSideEffects(
        IEventTracker eventTracker,
        EmployeeBasicEvent.SendWelcomeEmailEvent sendWelcomeEmail)
    {
        // Clear any previous test data
        InMemoryEmailService.Clear();

        // Fire the event
        var employeeId = Guid.NewGuid();
        var email = "test@company.com";
        _ = sendWelcomeEmail(employeeId, email);

        // Wait for event to complete
        await eventTracker.WaitAllAsync();

        // Assert side effects via mock service
        var sentEmails = InMemoryEmailService.GetSentEmails();
        Assert.Single(sentEmails);
        Assert.Equal(email, sentEmails[0].Recipient);
        Assert.Contains("Welcome", sentEmails[0].Subject, StringComparison.Ordinal);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/Events/EventTestingSamples.cs#L8-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-wait' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Monitoring Pending Events

<!-- snippet: events-eventtracker-count -->
<a id='snippet-events-eventtracker-count'></a>
```cs
/// <summary>
/// Test demonstrating PendingCount monitoring.
/// </summary>
public static class EventCountTestSample
{
    public static async Task VerifyEventCounting(
        IEventTracker eventTracker,
        EmployeeBasicEvent.SendWelcomeEmailEvent sendWelcomeEmail)
    {
        // Assert initial state - no pending events
        Assert.Equal(0, eventTracker.PendingCount);

        // Fire multiple events
        _ = sendWelcomeEmail(Guid.NewGuid(), "emp1@company.com");
        _ = sendWelcomeEmail(Guid.NewGuid(), "emp2@company.com");

        // PendingCount may already be 0 if events complete quickly
        // This is expected for fast operations
        var pendingAfterFire = eventTracker.PendingCount;
        Console.WriteLine($"Pending after fire: {pendingAfterFire}");

        // Wait for completion
        await eventTracker.WaitAllAsync();

        // Verify all events completed
        Assert.Equal(0, eventTracker.PendingCount);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/Events/EventTestingSamples.cs#L38-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-eventtracker-count' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Error Handling

Event exceptions should be handled within the event method. Unhandled exceptions are caught by the generated wrapper, logged, and suppressed to preserve fire-and-forget semantics:

<!-- snippet: events-error-handling -->
<a id='snippet-events-error-handling'></a>
```cs
/// <summary>
/// Demonstrates proper error handling in event methods.
/// </summary>
[Factory]
public partial class EmployeeErrorHandling
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public void Create(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <summary>
    /// Event with internal error handling to preserve fire-and-forget semantics.
    /// </summary>
    [Event]
    public async Task SendNotificationWithRetry(
        Guid employeeId,
        string recipientEmail,
        [Service] IEmailService emailService,
        [Service] ILogger<EmployeeErrorHandling> logger,
        CancellationToken ct)
    {
        try
        {
            await emailService.SendAsync(
                recipientEmail,
                $"Notification for {employeeId}",
                "Important notification content.",
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log error but do NOT rethrow - fire-and-forget semantics
            logger.LogError(ex,
                "Failed to send notification for employee {EmployeeId} to {Recipient}",
                employeeId,
                recipientEmail);
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/ErrorHandlingSamples.cs#L7-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-error-handling' title='Start of snippet'>anchor</a></sup>
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
// ASP.NET Core Integration for Events
//
// When you call services.AddNeatooAspNetCore(assembly), it registers:
//
// 1. IEventTracker (singleton)
//    - Tracks all pending fire-and-forget event Tasks
//    - Provides PendingCount property and WaitAllAsync() method
//    - Used by EventTrackerHostedService for graceful shutdown
//
// 2. EventTrackerHostedService (IHostedService)
//    - Implements graceful shutdown for events
//    - StopAsync waits for pending events to complete
//
// Shutdown sequence:
// 1. ApplicationStopping token is triggered (SIGTERM, app.StopAsync, etc.)
// 2. EventTrackerHostedService.StopAsync is called by the host
// 3. EventTrackerHostedService calls eventTracker.WaitAllAsync(ct)
// 4. Running events receive the cancellation signal
// 5. Events that check ct.IsCancellationRequested can exit early
// 6. Application waits for events to complete or shutdown timeout
// 7. Application exits cleanly
//
// This ensures events complete before the application stops,
// preventing data loss or incomplete operations.
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Events/AspNetCoreIntegrationSamples.cs#L3-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-aspnetcore' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate with domain event pattern for read model updates.
/// </summary>
[Factory]
public partial class EmployeeDomainEvent : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public void Create(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        Status = "Pending";
    }

    /// <summary>
    /// Activates the employee and triggers domain event.
    /// </summary>
    [Remote, Update]
    public async Task Activate(
        [Service] IEmployeeRepository repository,
        [Service] DomainEventHandlers.EmployeeActivatedEvent onActivated,
        CancellationToken ct)
    {
        Status = "Active";

        var entity = await repository.GetByIdAsync(Id, ct);
        if (entity != null)
        {
            entity.Position = "Active Employee";
            await repository.UpdateAsync(entity, ct);
            await repository.SaveChangesAsync(ct);
        }
        IsNew = false;

        // Fire domain event for read model update
        _ = onActivated(Id, Name);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/DomainEventSamples.cs#L43-L88' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-domain-events' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Notifications

<!-- snippet: events-notifications -->
<a id='snippet-events-notifications'></a>
```cs
/// <summary>
/// Notification event handlers for various notification channels.
/// </summary>
[Factory]
public partial class NotificationEvents
{
    [Create]
    public void Create()
    {
    }

    /// <summary>
    /// Sends email notification.
    /// </summary>
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

    /// <summary>
    /// Sends push notification (placeholder implementation).
    /// </summary>
    [Event]
    public Task SendPushNotification(
        Guid userId,
        string message,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Placeholder: would integrate with push notification service
        Console.WriteLine($"Push notification to {userId}: {message}");
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/NotificationSamples.cs#L6-L47' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-notifications' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Audit Logging

<!-- snippet: events-audit -->
<a id='snippet-events-audit'></a>
```cs
/// <summary>
/// Audit event handler for fire-and-forget audit logging.
/// </summary>
[Factory]
public partial class AuditEvents
{
    [Create]
    public void Create()
    {
    }

    /// <summary>
    /// Logs audit trail entry asynchronously.
    /// </summary>
    [Event]
    public async Task LogAuditTrail(
        string action,
        Guid entityId,
        string entityType,
        string details,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        await auditLog.LogAsync(action, entityId, entityType, details, ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/AuditSamples.cs#L6-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-audit' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Integration Events

<!-- snippet: events-integration -->
<a id='snippet-events-integration'></a>
```cs
/// <summary>
/// External API client interface for integration events.
/// </summary>
public interface IExternalApiClient
{
    Task NotifyAsync(Guid entityId, string eventType, CancellationToken ct = default);
}

/// <summary>
/// Mock implementation for testing integration events.
/// </summary>
public class MockExternalApiClient : IExternalApiClient
{
    public List<NotificationRecord> Notifications { get; } = new();

    public Task NotifyAsync(Guid entityId, string eventType, CancellationToken ct = default)
    {
        Notifications.Add(new NotificationRecord(entityId, eventType, DateTime.UtcNow));
        return Task.CompletedTask;
    }

    public record NotificationRecord(Guid EntityId, string EventType, DateTime SentAt);
}

/// <summary>
/// Integration event handler for external system notifications.
/// </summary>
[Factory]
public partial class IntegrationEvents
{
    [Create]
    public void Create()
    {
    }

    /// <summary>
    /// Notifies external system of entity changes.
    /// </summary>
    [Event]
    public async Task NotifyExternalSystem(
        Guid entityId,
        string eventType,
        [Service] IExternalApiClient apiClient,
        CancellationToken ct)
    {
        await apiClient.NotifyAsync(entityId, eventType, ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/IntegrationSamples.cs#L5-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-integration' title='Start of snippet'>anchor</a></sup>
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
/// Authorization interface for employee operations.
/// </summary>
public interface IEmployeeEventAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    Task<bool> CanCreateAsync();
}

/// <summary>
/// Authorization implementation checking user context.
/// </summary>
public class EmployeeEventAuth : IEmployeeEventAuth
{
    private readonly IUserContext _userContext;

    public EmployeeEventAuth(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public Task<bool> CanCreateAsync()
    {
        return Task.FromResult(_userContext.IsAuthenticated);
    }
}

/// <summary>
/// Employee with authorization on operations but events bypass authorization.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeEventAuth>]
public partial class EmployeeWithAuthEvents
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    /// <summary>
    /// Create requires authorization - IEmployeeEventAuth.CanCreateAsync is called.
    /// </summary>
    [Create]
    public void Create(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <summary>
    /// Event BYPASSES authorization - always executes regardless of user permissions.
    /// Events are internal operations triggered by application code, not user requests.
    /// </summary>
    [Event]
    public async Task NotifySystemAdmin(
        Guid employeeId,
        string message,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        // This event executes without authorization checks
        // It runs in a separate scope with no user context
        await emailService.SendAsync(
            "admin@company.com",
            "System Notification",
            $"Employee {employeeId}: {message}",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/AuthorizationSamples.cs#L6-L74' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-authorization' title='Start of snippet'>anchor</a></sup>
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
/// Standard pattern for testing event side effects.
/// </summary>
public static class EventTestingPatternSample
{
    public static async Task TestWelcomeEmailEvent(
        IServiceProvider serviceProvider,
        IEventTracker eventTracker)
    {
        // Arrange - get event delegate from DI
        var sendWelcomeEmail = serviceProvider
            .GetRequiredService<EmployeeBasicEvent.SendWelcomeEmailEvent>();

        // Clear test data
        InMemoryEmailService.Clear();

        // Act - fire the event
        var employeeId = Guid.NewGuid();
        var testEmail = "newemployee@company.com";
        _ = sendWelcomeEmail(employeeId, testEmail);

        // Wait for event completion
        await eventTracker.WaitAllAsync();

        // Assert - verify email was sent
        var sentEmails = InMemoryEmailService.GetSentEmails();
        Assert.Single(sentEmails);
        Assert.Equal(testEmail, sentEmails[0].Recipient);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/Events/EventTestingSamples.cs#L69-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Testing multiple events:

<!-- snippet: events-testing-latch -->
<a id='snippet-events-testing-latch'></a>
```cs
/// <summary>
/// Testing multiple concurrent events.
/// </summary>
public static class MultipleEventTestSample
{
    public static async Task TestMultipleConcurrentEvents(
        IServiceProvider serviceProvider,
        IEventTracker eventTracker)
    {
        // Arrange
        var sendWelcomeEmail = serviceProvider
            .GetRequiredService<EmployeeBasicEvent.SendWelcomeEmailEvent>();

        InMemoryEmailService.Clear();

        // Act - fire multiple events
        _ = sendWelcomeEmail(Guid.NewGuid(), "emp1@company.com");
        _ = sendWelcomeEmail(Guid.NewGuid(), "emp2@company.com");

        // Wait for all events using IEventTracker
        await eventTracker.WaitAllAsync();

        // Assert - verify all events completed
        var sentEmails = InMemoryEmailService.GetSentEmails();
        Assert.Equal(2, sentEmails.Count);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/Events/EventTestingSamples.cs#L102-L130' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-testing-latch' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Correlation ID Tracking

Events inherit the correlation ID from the triggering operation:

<!-- snippet: events-correlation -->
<a id='snippet-events-correlation'></a>
```cs
/// <summary>
/// Demonstrates correlation ID propagation in events.
/// </summary>
[Factory]
public partial class EmployeeCorrelation
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public void Create(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <summary>
    /// Event that uses correlation ID for tracing.
    /// Correlation ID propagates from the original request.
    /// </summary>
    [Event]
    public async Task LogWithCorrelation(
        Guid employeeId,
        string action,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // CorrelationContext.CorrelationId contains the ID from the triggering request
        var correlationId = CorrelationContext.CorrelationId;

        // Include correlation ID in audit log for tracing
        await auditLog.LogAsync(
            action,
            employeeId,
            "Employee",
            $"CorrelationId: {correlationId} - Action: {action}",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Events/CorrelationSamples.cs#L7-L47' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-correlation' title='Start of snippet'>anchor</a></sup>
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
