using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-integration
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
#endregion
