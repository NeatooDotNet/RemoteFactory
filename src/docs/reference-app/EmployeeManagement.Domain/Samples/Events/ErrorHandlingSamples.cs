using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-error-handling
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
#endregion
