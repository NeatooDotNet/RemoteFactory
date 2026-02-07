using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Operations;

// Note: Primary Event operation snippet is in Domain/OperationsSamples.cs (operations-event)
// This file contains supplementary event patterns

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
