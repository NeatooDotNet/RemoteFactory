using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-notifications
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
#endregion
