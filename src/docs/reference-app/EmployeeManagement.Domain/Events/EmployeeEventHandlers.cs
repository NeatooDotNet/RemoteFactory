using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Events;

#region attributes-pattern-event
/// <summary>
/// Event handlers for Employee domain events.
/// </summary>
[Factory]
public partial class EmployeeEventHandlers
{
    #region events-basic
    /// <summary>
    /// Notifies HR when a new employee is created.
    /// CancellationToken is required as the last parameter.
    /// </summary>
    [Event]
    public async Task NotifyHROfNewEmployee(
        Guid employeeId,
        string employeeName,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            "hr@company.com",
            $"New Employee: {employeeName}",
            $"Employee {employeeName} (ID: {employeeId}) has been added to the system.",
            ct);
    }
    #endregion

    #region events-domain-events
    /// <summary>
    /// Domain event for employee promotion.
    /// </summary>
    [Event]
    public async Task NotifyManagerOfPromotion(
        Guid employeeId,
        string employeeName,
        string oldPosition,
        string newPosition,
        [Service] IEmailService emailService,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var employee = await repository.GetByIdAsync(employeeId, ct);
        var departmentId = employee?.DepartmentId ?? Guid.Empty;

        await emailService.SendAsync(
            "manager@company.com",
            $"Employee Promotion: {employeeName}",
            $"{employeeName} has been promoted from {oldPosition} to {newPosition}. Department: {departmentId}",
            ct);
    }
    #endregion

    #region events-audit
    /// <summary>
    /// Audit logging event for employee departure.
    /// </summary>
    [Event]
    public async Task LogEmployeeDeparture(
        Guid employeeId,
        string reason,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        await auditLog.LogAsync(
            "Departure",
            employeeId,
            "Employee",
            $"Employee departed. Reason: {reason}",
            ct);
    }
    #endregion
}
#endregion
