using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Attributes;

#region attributes-event
/// <summary>
/// Event handlers for employee onboarding.
/// Events are fire-and-forget - caller does not wait for completion.
/// </summary>
[Factory]
public partial class EmployeeEventHandlers
{
    /// <summary>
    /// Sends welcome email when an employee is hired.
    /// CancellationToken must be the final parameter for [Event] methods.
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
            $"Your employee ID is {employeeId}. We're excited to have you!",
            ct);
    }
}
#endregion
