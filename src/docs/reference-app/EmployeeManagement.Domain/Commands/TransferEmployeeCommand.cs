using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Commands;

/// <summary>
/// Command to transfer an employee to a different department.
/// </summary>
[Factory]
public static partial class TransferEmployeeCommand
{
    /// <summary>
    /// Executes the transfer on the server.
    /// </summary>
    [Remote, Execute]
    private static async Task<bool> _Execute(
        Guid employeeId,
        Guid newDepartmentId,
        [Service] IEmployeeRepository employeeRepo,
        [Service] IDepartmentRepository departmentRepo,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId, ct);
        if (employee == null) return false;

        var newDepartment = await departmentRepo.GetByIdAsync(newDepartmentId, ct);
        if (newDepartment == null) return false;

        var oldDepartmentId = employee.DepartmentId;
        employee.DepartmentId = newDepartmentId;

        await employeeRepo.UpdateAsync(employee, ct);
        await employeeRepo.SaveChangesAsync(ct);

        await auditLog.LogAsync(
            "Transfer",
            employeeId,
            "Employee",
            $"Transferred from department {oldDepartmentId} to {newDepartment.Name} ({newDepartmentId})",
            ct);

        return true;
    }
}
