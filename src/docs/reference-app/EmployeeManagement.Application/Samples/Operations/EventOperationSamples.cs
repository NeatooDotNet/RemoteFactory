using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Operations;

#region operations-event
/// <summary>
/// Event handler for employee notifications.
/// </summary>
[SuppressFactory]
public partial class EmployeeEventHandler
{
    public Guid EmployeeId { get; private set; }

    [Create]
    public EmployeeEventHandler()
    {
    }

    /// <summary>
    /// Sends a welcome email to a new employee.
    /// Fire-and-forget event pattern for notifications.
    /// </summary>
    [Event]
    public async Task SendWelcomeEmail(
        Guid employeeId,
        string employeeEmail,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            employeeEmail,
            "Welcome to the Team",
            $"Welcome! Your employee ID is {employeeId}.",
            ct);
    }
}
#endregion

#region operations-event-tracker
// EventTracker usage pattern:
//
// Fire event (fire-and-forget):
// _ = sendWelcomeEmail(employeeId, employeeEmail);
//
// Wait for all pending events (useful for testing or shutdown):
// await eventTracker.WaitAllAsync();
//
// Check pending event count:
// var pending = eventTracker.PendingCount;
#endregion
