using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-requirements
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
#endregion

#region events-cancellation
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
#endregion

#region events-notifications
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
#endregion

#region events-scope-isolation
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
#endregion

#region events-scope-example
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
#endregion

#region events-error-handling
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
#endregion

#region events-caller
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
#endregion

#region events-integration
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
#endregion

#region events-authorization
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
#endregion

#region events-graceful-shutdown
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
#endregion

#region events-eventtracker-access
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
#endregion

#region events-eventtracker-wait
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
#endregion

#region events-eventtracker-count
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
#endregion
