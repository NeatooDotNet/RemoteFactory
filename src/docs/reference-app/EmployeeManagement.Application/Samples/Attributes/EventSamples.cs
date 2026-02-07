using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Attributes;

// Full implementations for Event - see MinimalAttributesSamples.cs for doc snippets

[Factory]
public partial class EmployeeEventHandlers
{
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
