using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Events;

/// <summary>
/// Event handlers for Department domain events.
/// </summary>
[Factory]
public partial class DepartmentEventHandlers
{
    /// <summary>
    /// Notifies when a new department is created.
    /// </summary>
    [Event]
    public async Task NotifyNewDepartment(
        Guid departmentId,
        string departmentName,
        string departmentCode,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            "admin@company.com",
            $"New Department Created: {departmentName}",
            $"Department {departmentName} ({departmentCode}) with ID {departmentId} has been created.",
            ct);
    }

    /// <summary>
    /// Notifies when a department manager is assigned.
    /// </summary>
    [Event]
    public async Task NotifyManagerAssignment(
        Guid departmentId,
        Guid managerId,
        [Service] IEmailService emailService,
        [Service] IEmployeeRepository employeeRepo,
        CancellationToken ct)
    {
        var manager = await employeeRepo.GetByIdAsync(managerId, ct);
        var managerName = manager != null ? $"{manager.FirstName} {manager.LastName}" : "Unknown";

        await emailService.SendAsync(
            "admin@company.com",
            "Department Manager Assigned",
            $"{managerName} has been assigned as manager of department {departmentId}.",
            ct);
    }
}
