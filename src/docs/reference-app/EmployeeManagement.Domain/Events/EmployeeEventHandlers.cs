using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Events;

[Factory]
public partial class EmployeeEventHandlers
{
    #region events-basic
    // [Event] marks fire-and-forget methods that run in isolated DI scopes
    [Event]
    public async Task NotifyHROfNewEmployee(
        Guid employeeId, string employeeName,
        [Service] IEmailService emailService, CancellationToken ct)
    {
        await emailService.SendAsync("hr@company.com", $"New Employee: {employeeName}", $"ID: {employeeId}", ct);
    }
    #endregion

    #region events-domain-events
    // Domain events can inject repositories and services
    [Event]
    public async Task NotifyManagerOfPromotion(Guid employeeId, string name,
        string oldPos, string newPos, [Service] IEmailService email,
        [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var emp = await repo.GetByIdAsync(employeeId, ct);
        await email.SendAsync("manager@company.com", "Promoted", $"{name}: {oldPos}->{newPos}", ct);
    }
    #endregion

    #region events-audit
    // Audit logging as fire-and-forget event
    [Event]
    public async Task LogEmployeeDeparture(
        Guid employeeId, string reason,
        [Service] IAuditLogService auditLog, CancellationToken ct)
    {
        await auditLog.LogAsync("Departure", employeeId, "Employee", reason, ct);
    }
    #endregion
}
