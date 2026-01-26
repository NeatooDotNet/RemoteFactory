# Events

RemoteFactory supports fire-and-forget domain events with scope isolation via the `[Event]` attribute.

## Event Operation Basics

Events are asynchronous operations that run independently of the caller:

<!-- snippet: events-basic -->
<!--
SNIPPET REQUIREMENTS:
- Show an Employee aggregate with [Factory] attribute
- Include a [Create] method that initializes the employee
- Add an [Event] method called SendWelcomeEmail that:
  - Accepts employeeId (Guid), email (string) parameters
  - Injects IEmailService via [Service] attribute
  - Has CancellationToken as final parameter
  - Sends a welcome email asynchronously
- Context: Domain layer, production code
- Domain: Employee Management (Employee entity)
- Demonstrates: Basic event pattern with fire-and-forget semantics
-->
<!-- endSnippet -->

Key characteristics:
- **Fire-and-forget**: Caller doesn't wait for completion
- **Scope isolation**: New DI scope per event
- **Transactional independence**: Events run in separate transactions
- **Graceful shutdown**: EventTracker waits for pending events

## How Events Work

### 1. Caller Invokes Event

<!-- snippet: events-caller -->
<!--
SNIPPET REQUIREMENTS:
- Show Application layer service or controller code
- Resolve the generated event delegate from DI: Employee.SendWelcomeEmailEvent
- Fire the event with _ = sendWelcomeEmail(employeeId, email) pattern
- Add comment explaining code continues immediately without waiting
- Include IEventTracker.WaitAllAsync() call for testing scenarios
- Context: Application layer, production code with testing consideration
- Domain: Employee Management
- Demonstrates: How to invoke an event delegate fire-and-forget style
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show a comment block explaining the generated code pattern
- Include: delegate signature (SendWelcomeEmailEvent)
- Include: Task.Run wrapper with scope creation
- Include: _eventTracker.Track(task) call
- Include: return task for optional awaiting
- Context: Explanatory comment showing generated code pattern
- Domain: Employee Management
- Demonstrates: What the source generator produces for event methods
-->
<!-- endSnippet -->

## Event Method Requirements

Event methods must:

1. Have `[Event]` attribute
2. Accept `CancellationToken` as the last parameter
3. Return `Task` or `void` (void methods are converted to Task delegates)

<!-- snippet: events-requirements -->
<!--
SNIPPET REQUIREMENTS:
- Show a [Factory] class with multiple [Event] methods demonstrating valid signatures:
  - ValidEvent: returns Task, has [Service] injection, CancellationToken last
  - VoidEvent: returns void (generator converts to Task delegate)
  - AsyncEvent: async Task with await Task.Delay
- Include [Create] method
- Context: Domain layer, production code
- Domain: Employee Management (EmployeeEvents or similar)
- Demonstrates: All valid event method signatures
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show a [Factory] class with an [Event] method
- Inject multiple scoped services: IEmployeeRepository, IAuditLogService
- Add comments explaining each service is a NEW scoped instance
- Show repository access and audit logging in isolated scope
- Context: Domain layer, production code
- Domain: Employee Management
- Demonstrates: How scoped services are isolated per event execution
-->
<!-- endSnippet -->

Benefits:
- **Separate transactions**: Event failures don't roll back triggering operation
- **Independent lifetime**: Scoped services don't leak across events
- **Parallel execution**: Multiple events run concurrently without conflicts

### Example: Order Processing

<!-- snippet: events-scope-example -->
<!--
SNIPPET REQUIREMENTS:
- Show Employee aggregate with:
  - Properties: Id, Status, HireDate
  - [Create] method initializing the employee
  - [Remote, Fetch] method HireEmployee that saves to repository
  - [Event] method LogEmployeeHired for audit logging
- Show the event running in separate transaction from main operation
- Add comment: "If event fails, HireEmployee still succeeds"
- Context: Domain layer, production code
- Domain: Employee Management
- Demonstrates: Transactional independence between operation and event
-->
<!-- endSnippet -->

If the event fails:
- Employee is still saved (separate transaction)
- Event failure is logged by the application
- Client call succeeds

## CancellationToken Handling

Events receive the application shutdown token:

<!-- snippet: events-cancellation -->
<!--
SNIPPET REQUIREMENTS:
- Show [Event] method with proper cancellation handling
- Call ct.ThrowIfCancellationRequested() before operations
- Show loop with ct.IsCancellationRequested check
- Inject IEmailService and show cancellation-aware SendAsync call
- Context: Domain layer, production code
- Domain: Employee Management
- Demonstrates: Proper cancellation token usage in long-running events
-->
<!-- endSnippet -->

The CancellationToken:
- Fires on `IHostApplicationLifetime.ApplicationStopping`
- Allows graceful cleanup
- Is required for all event methods

### Graceful Shutdown Example:

<!-- snippet: events-graceful-shutdown -->
<!--
SNIPPET REQUIREMENTS:
- Show static configuration class/method
- Call services.AddNeatooAspNetCore(assembly)
- Add comments explaining what gets registered:
  - IEventTracker (singleton)
  - EventTrackerHostedService (handles graceful shutdown)
- Explain shutdown sequence in comments
- Context: Server startup configuration, production code
- Domain: N/A (infrastructure configuration)
- Demonstrates: ASP.NET Core event tracking registration
-->
<!-- endSnippet -->

## EventTracker

The `IEventTracker` service monitors pending events.

### Accessing EventTracker

<!-- snippet: events-eventtracker-access -->
<!--
SNIPPET REQUIREMENTS:
- Show resolving IEventTracker from DI (singleton)
- Fire multiple events using resolved delegate
- Check eventTracker.PendingCount property
- Call eventTracker.WaitAllAsync()
- Verify PendingCount is 0 after waiting
- Context: Application layer, testing/monitoring code
- Domain: Employee Management
- Demonstrates: IEventTracker singleton access and basic usage
-->
<!-- endSnippet -->

### Waiting for Events (Testing)

<!-- snippet: events-eventtracker-wait -->
<!--
SNIPPET REQUIREMENTS:
- Show test scenario for event side effects
- Fire an event (e.g., SendWelcomeEmailEvent)
- Call eventTracker.WaitAllAsync() to wait for completion
- Assert side effects via mock service (e.g., MockEmailService.SentEmails)
- Context: Test code demonstrating event verification pattern
- Domain: Employee Management
- Demonstrates: Testing pattern for verifying event side effects
-->
<!-- endSnippet -->

### Monitoring Pending Events

<!-- snippet: events-eventtracker-count -->
<!--
SNIPPET REQUIREMENTS:
- Assert initial PendingCount is 0
- Fire multiple events
- Show that PendingCount may already be 0 if events complete quickly
- Call WaitAllAsync and verify PendingCount is 0
- Context: Test code demonstrating event count monitoring
- Domain: Employee Management
- Demonstrates: PendingCount property usage and event completion verification
-->
<!-- endSnippet -->

## Error Handling

Event exceptions should be handled within the event method. Unhandled exceptions are caught by the generated wrapper, logged, and suppressed to preserve fire-and-forget semantics:

<!-- snippet: events-error-handling -->
<!--
SNIPPET REQUIREMENTS:
- Show [Event] method with try/catch error handling
- Inject IEmailService and ILogger<T>
- Wrap emailService.SendAsync in try/catch
- Catch Exception when not OperationCanceledException
- Log error with logger.LogError including entity ID
- Do NOT rethrow - demonstrate fire-and-forget semantics
- Context: Domain layer, production code
- Domain: Employee Management
- Demonstrates: Proper error handling in event methods
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show comment block explaining ASP.NET Core integration
- Explain services.AddNeatooAspNetCore registers:
  - IEventTracker (singleton)
  - EventTrackerHostedService (handles graceful shutdown)
- Explain shutdown sequence:
  1. ApplicationStopping token triggered
  2. EventTrackerHostedService waits for pending events
  3. Events receive cancellation signal
  4. Application exits after events complete or timeout
- Context: Explanatory comment block
- Domain: N/A (infrastructure pattern)
- Demonstrates: How events integrate with ASP.NET Core lifecycle
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show Employee aggregate with domain event pattern
- Properties: Id, Status (Pending, Active, etc.)
- [Create] method
- [Remote, Fetch] method Activate that changes status
- [Event] method OnEmployeeActivated that:
  - Updates read model or projection
  - Loads employee from repository
  - Updates status and saves
- Context: Domain layer, production code
- Domain: Employee Management
- Demonstrates: Domain event pattern for read model updates
-->
<!-- endSnippet -->

### Notifications

<!-- snippet: events-notifications -->
<!--
SNIPPET REQUIREMENTS:
- Show [Factory] class with notification events
- SendEmailNotification event: to, subject, body, IEmailService
- SendPushNotification event: userId, message (placeholder implementation)
- Both with CancellationToken as final parameter
- Context: Domain layer, production code
- Domain: Employee Management (notification infrastructure)
- Demonstrates: Multiple notification event patterns
-->
<!-- endSnippet -->

### Audit Logging

<!-- snippet: events-audit -->
<!--
SNIPPET REQUIREMENTS:
- Show [Factory] class with audit event
- LogAuditTrail event accepting:
  - action (string)
  - entityId (Guid)
  - entityType (string)
  - details (string)
  - IAuditLogService via [Service]
  - CancellationToken
- Call auditLog.LogAsync with all parameters
- Context: Domain layer, production code
- Domain: Employee Management (audit infrastructure)
- Demonstrates: Fire-and-forget audit logging pattern
-->
<!-- endSnippet -->

### Integration Events

<!-- snippet: events-integration -->
<!--
SNIPPET REQUIREMENTS:
- Define IExternalApiClient interface with NotifyAsync method
- Show MockExternalApiClient implementing interface with Notifications list
- Show [Factory] class with [Event] method NotifyExternalSystem
- Event accepts entityId, eventType, IExternalApiClient, CancellationToken
- Calls apiClient.NotifyAsync
- Context: Domain/Infrastructure layer, production code
- Domain: Employee Management (integration infrastructure)
- Demonstrates: External system notification via events
-->
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
<!--
SNIPPET REQUIREMENTS:
- Define IEmployeeAuth interface with [AuthorizeFactory(AuthorizeFactoryOperation.Create)] method
- Implement EmployeeAuth class checking IUserContext.IsAuthenticated
- Show [Factory][AuthorizeFactory<IEmployeeAuth>] class with:
  - [Create] method that requires authorization
  - [Event] method that BYPASSES authorization
- Add comments explaining events always execute regardless of user permissions
- Context: Domain layer with authorization, production code
- Domain: Employee Management
- Demonstrates: Events bypass authorization checks
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show test method for event side effects
- Set up DI container with IEventTracker and MockEmailService
- Fire SendWelcomeEmailEvent with test data
- Call eventTracker.WaitAllAsync()
- Assert MockEmailService.SentEmails contains expected email
- Context: Test code
- Domain: Employee Management
- Demonstrates: Standard pattern for testing event side effects
-->
<!-- endSnippet -->

Testing multiple events:

<!-- snippet: events-testing-latch -->
<!--
SNIPPET REQUIREMENTS:
- Show test method for multiple events
- Fire multiple events with different data
- Wait using IEventTracker.WaitAllAsync()
- Assert all events completed (e.g., SentEmails.Count == 2)
- Context: Test code
- Domain: Employee Management
- Demonstrates: Testing multiple concurrent events
-->
<!-- endSnippet -->

## Correlation ID Tracking

Events inherit the correlation ID from the triggering operation:

<!-- snippet: events-correlation -->
<!--
SNIPPET REQUIREMENTS:
- Show [Factory] class with [Event] method
- Access CorrelationContext.CorrelationId within event
- Use correlation ID in audit log call
- Add comment explaining correlation propagates from original request
- Context: Domain layer, production code
- Domain: Employee Management
- Demonstrates: Correlation ID propagation in events
-->
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
