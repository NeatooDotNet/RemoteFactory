using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Attributes;

// Full implementations for patterns - see MinimalAttributesSamples.cs for doc snippets

public record TerminationResult(
    Guid EmployeeId,
    bool Success,
    DateTime EffectiveDate,
    string Message);

[Factory]
public static partial class TerminateEmployeeCommand
{
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

[Factory]
public partial class EmployeeLifecycleEvents
{
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
