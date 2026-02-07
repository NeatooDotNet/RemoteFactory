using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-requirements
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
#endregion

#region events-cancellation
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
#endregion

#region events-notifications
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
#endregion

#region events-scope-isolation
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
#endregion

// Additional classes needed for compilation but not shown in docs
[Factory]
public partial class IsolatedEventHandlers
{
    [Event]
    public async Task OnEmployeeCreated(Guid employeeId, string employeeName,
        [Service] IEmailService emailService, CancellationToken ct)
    {
        await emailService.SendAsync("hr@company.com", "New Employee", $"{employeeName} ({employeeId})", ct);
    }
}

#region events-error-handling
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
#endregion

// Delegate generation sample for tests
[Factory]
public partial class EventCallerHandlers
{
    [Event]
    public async Task OnInsert(Guid employeeId, string employeeName,
        [Service] IAuditLogService auditLog, CancellationToken ct)
    {
        await auditLog.LogAsync("Insert", employeeId, "Employee", $"Created {employeeName}", ct);
    }
}

#region events-integration
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
#endregion

#region events-authorization
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
#endregion

#region events-graceful-shutdown
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
#endregion

#region events-eventtracker-access
// IEventTracker registered by AddNeatooRemoteFactory/AddNeatooAspNetCore
public static class EventTrackerAccessSample
{
    public static void AccessEventTracker(IServiceProvider sp)
    {
        var tracker = sp.GetRequiredService<IEventTracker>();
        Console.WriteLine($"Pending: {tracker.PendingCount}");
    }
}
#endregion

#region events-eventtracker-wait
// WaitAllAsync for tests - ensures events complete before assertions
public static class EventTrackerWaitSample
{
    public static async Task WaitForEventsInTest(IServiceProvider sp)
    {
        var tracker = sp.GetRequiredService<IEventTracker>();
        await tracker.WaitAllAsync(); // With timeout: await tracker.WaitAllAsync(cts.Token);
    }
}
#endregion

#region events-eventtracker-count
// PendingCount for health checks and monitoring
public static class EventTrackerCountSample
{
    public static void MonitorPendingEvents(IEventTracker tracker, ILogger logger)
    {
        if (tracker.PendingCount > 100) logger.LogWarning("High pending: {Count}", tracker.PendingCount);
    }
}
#endregion
