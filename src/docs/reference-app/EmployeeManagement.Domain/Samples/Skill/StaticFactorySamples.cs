using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;
using System.Globalization;

namespace EmployeeManagement.Domain.Samples.Skill;

#region skill-static-execute-commands
[Factory]
public static partial class SkillEmployeeCommands
{
    [Remote, Execute]
    private static async Task<bool> _SendNotification(
        string recipient,
        string message,
        [Service] IEmailService service)
    {
        await service.SendAsync(recipient, "Notification", message);
        return true;
    }

    [Remote, Execute]
    private static async Task<SkillEmployeeSummary> _GetEmployeeSummary(
        Guid employeeId,
        [Service] IEmployeeRepository repo)
    {
        var employee = await repo.GetByIdAsync(employeeId);
        if (employee == null)
            return new SkillEmployeeSummary { Id = employeeId, Found = false };

        return new SkillEmployeeSummary
        {
            Id = employeeId,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Position = employee.Position,
            Found = true
        };
    }
}

public class SkillEmployeeSummary
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool Found { get; set; }
}
#endregion

#region skill-static-event-handlers
[Factory]
public static partial class SkillEmployeeEvents
{
    [Remote, Event]
    private static async Task _OnEmployeeCreated(
        Guid employeeId,
        string employeeName,
        [Service] IEmailService emailService,
        CancellationToken cancellationToken)
    {
        await emailService.SendAsync(
            "hr@company.com",
            "New Employee",
            $"Welcome {employeeName}!",
            cancellationToken);
    }

    [Remote, Event]
    private static async Task _OnPaymentReceived(
        Guid employeeId,
        decimal amount,
        [Service] IEmailService email,
        [Service] IAuditLogService audit,
        CancellationToken cancellationToken)
    {
        var message = string.Format(
            CultureInfo.InvariantCulture,
            "Payment of {0:C} received for employee {1}",
            amount,
            employeeId);
        await email.SendAsync(
            "payroll@company.com",
            "Payment Received",
            message,
            cancellationToken);
        await audit.LogAsync(
            "PaymentReceived",
            employeeId,
            "Employee",
            string.Format(CultureInfo.InvariantCulture, "Payment received: {0:C}", amount),
            cancellationToken);
    }
}
#endregion
