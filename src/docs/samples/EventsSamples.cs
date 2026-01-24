using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.Internal;
using Neatoo.RemoteFactory.Samples.Infrastructure;

namespace Neatoo.RemoteFactory.Samples;

#region events-basic
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
#endregion

#region events-requirements
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
#endregion

#region events-scope-isolation
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
#endregion

#region events-scope-example
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
#endregion

#region events-cancellation
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
#endregion

#region events-domain-events
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
#endregion

#region events-notifications
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
#endregion

#region events-audit
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
#endregion

#region events-error-handling
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
#endregion

#region events-integration
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
#endregion

#region events-authorization
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
#endregion

#region events-correlation
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
#endregion

/// <summary>
/// Code samples for docs/events.md documentation.
/// </summary>
public partial class EventsSamples
{
    #region events-caller
    public async Task FireEvent(
        OrderWithEvents.SendOrderConfirmationEvent sendConfirmation,
        IEventTracker eventTracker)
    {
        var orderId = Guid.NewGuid();
        var email = "customer@example.com";

        // Fire the event - returns Task but doesn't block
        _ = sendConfirmation(orderId, email);
        // Code continues immediately - event runs in background

        // In tests, wait for events to complete
        await eventTracker.WaitAllAsync();
    }
    #endregion

    #region events-tracker-generated
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
    #endregion

    #region events-graceful-shutdown
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
    #endregion

    #region events-eventtracker-access
    public async Task AccessEventTracker(
        IEventTracker eventTracker,
        OrderWithEvents.SendOrderConfirmationEvent sendConfirmation)
    {
        // IEventTracker is registered as singleton
        // Fire some events
        _ = sendConfirmation(Guid.NewGuid(), "test@example.com");
        _ = sendConfirmation(Guid.NewGuid(), "test2@example.com");

        // Check pending count (may be 0, 1, or 2 depending on timing)
        var pendingCount = eventTracker.PendingCount;

        // Wait for all events to complete
        await eventTracker.WaitAllAsync();
        var afterCount = eventTracker.PendingCount; // 0 - all completed
    }
    #endregion

    #region events-eventtracker-wait
    public async Task WaitForPendingEvents(
        IEventTracker eventTracker,
        OrderWithEvents.SendOrderConfirmationEvent sendConfirmation,
        MockEmailService emailService)
    {
        var orderId = Guid.NewGuid();

        // Fire event
        _ = sendConfirmation(orderId, "customer@example.com");

        // Wait for completion
        await eventTracker.WaitAllAsync();

        // Verify side effects
        var emailSent = emailService.SentEmails.Any(e => e.To == "customer@example.com");
        // emailSent is true
    }
    #endregion

    #region events-eventtracker-count
    public async Task CheckPendingCount(
        IEventTracker eventTracker,
        CancellableEvent.LongRunningEventEvent longRunningEvent)
    {
        var initialCount = eventTracker.PendingCount; // 0 - no pending events

        // Fire multiple events
        _ = longRunningEvent(Guid.NewGuid());
        _ = longRunningEvent(Guid.NewGuid());

        // Count may be > 0 while events are running
        var duringCount = eventTracker.PendingCount; // 0, 1, or 2

        // Wait for all to complete
        await eventTracker.WaitAllAsync();
        var afterCount = eventTracker.PendingCount; // 0 - all completed
    }
    #endregion

    #region events-aspnetcore
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
    #endregion

    #region events-testing
    public async Task TestEventSideEffects(
        IEventTracker eventTracker,
        OrderWithEvents.SendOrderConfirmationEvent sendConfirmation,
        MockEmailService emailService)
    {
        var orderId = Guid.NewGuid();
        var customerEmail = "test@example.com";

        // Fire event
        _ = sendConfirmation(orderId, customerEmail);

        // Wait for completion before asserting
        await eventTracker.WaitAllAsync();

        // Verify side effects occurred
        var confirmationSent = emailService.SentEmails.Any(
            e => e.To == customerEmail && e.Subject == "Order Confirmation");
        // confirmationSent is true
    }
    #endregion

    #region events-testing-latch
    public async Task TestMultipleEvents(
        IEventTracker eventTracker,
        OrderWithEvents.SendOrderConfirmationEvent sendConfirmation,
        MockEmailService emailService)
    {
        // Fire multiple events
        _ = sendConfirmation(Guid.NewGuid(), "user1@example.com");
        _ = sendConfirmation(Guid.NewGuid(), "user2@example.com");

        // Wait for all events to complete
        await eventTracker.WaitAllAsync();

        // Verify all events completed
        var emailCount = emailService.SentEmails.Count; // 2
    }
    #endregion

    // [Fact]
    public async Task BasicEvent_FiresAndCompletes()
    {
        var scopes = SampleTestContainers.Scopes();
        var sendConfirmation = scopes.local.GetRequiredService<OrderWithEvents.SendOrderConfirmationEvent>();

        _ = sendConfirmation(Guid.NewGuid(), "test@example.com");

        var eventTracker = scopes.local.GetRequiredService<IEventTracker>();
        await eventTracker.WaitAllAsync();

        var emailService = scopes.local.GetRequiredService<MockEmailService>();
        Assert.Single(emailService.SentEmails);
    }

    // [Fact]
    public async Task AuditEvent_LogsAction()
    {
        var scopes = SampleTestContainers.Scopes();
        var logAudit = scopes.local.GetRequiredService<AuditEvents.LogAuditTrailEvent>();

        var entityId = Guid.NewGuid();
        _ = logAudit("Created", entityId, "TestEntity", "Test details");

        var eventTracker = scopes.local.GetRequiredService<IEventTracker>();
        await eventTracker.WaitAllAsync();

        var auditService = scopes.local.GetRequiredService<MockAuditLogService>();
        Assert.Contains(auditService.Logs, l => l.EntityId == entityId && l.Action == "Created");
    }
}
