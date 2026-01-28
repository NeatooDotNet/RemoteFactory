using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Attributes;

#region attributes-pattern-command
/// <summary>
/// Result of a termination operation.
/// </summary>
public record TerminationResult(
    Guid EmployeeId,
    bool Success,
    DateTime EffectiveDate,
    string Message);

/// <summary>
/// Command pattern for employee termination.
/// </summary>
[Factory]
public static partial class TerminateEmployeeCommand
{
    /// <summary>
    /// Executes the termination process on the server.
    /// </summary>
    [Remote, Execute]
    private static async Task<TerminationResult> _Execute(
        Guid employeeId,
        DateTime terminationDate,
        string reason,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var employee = await repository.GetByIdAsync(employeeId, ct);
        if (employee == null)
        {
            return new TerminationResult(
                employeeId,
                false,
                terminationDate,
                "Employee not found");
        }

        await repository.DeleteAsync(employeeId, ct);
        await repository.SaveChangesAsync(ct);

        return new TerminationResult(
            employeeId,
            true,
            terminationDate,
            $"Terminated for: {reason}");
    }
}
#endregion

#region attributes-pattern-event
/// <summary>
/// Domain event handlers for employee lifecycle events.
/// Event handlers are fire-and-forget - caller does not wait for completion.
/// </summary>
[Factory]
public partial class EmployeeLifecycleEvents
{
    /// <summary>
    /// Sends welcome email when employee is hired.
    /// CancellationToken must be the final parameter for [Event] methods.
    /// </summary>
    [Event]
    public async Task OnEmployeeHired(
        Guid employeeId,
        string email,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            email,
            "Welcome to the Team!",
            $"Your employee ID is {employeeId}. Welcome aboard!",
            ct);
    }

    /// <summary>
    /// Sends congratulations email when employee is promoted.
    /// </summary>
    [Event]
    public async Task OnEmployeePromoted(
        Guid employeeId,
        string newTitle,
        decimal newSalary,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            "hr@company.com",
            "Employee Promotion",
            $"Employee {employeeId} promoted to {newTitle} with salary ${newSalary:N2}",
            ct);
    }
}
#endregion
