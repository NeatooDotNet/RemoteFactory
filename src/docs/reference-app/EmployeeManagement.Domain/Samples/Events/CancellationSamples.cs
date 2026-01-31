using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-cancellation
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
#endregion
