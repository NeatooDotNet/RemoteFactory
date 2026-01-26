using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Commands;

#region factory-execute-command
/// <summary>
/// Command to promote an employee with a new position and salary increase.
/// </summary>
[Factory]
public static partial class PromoteEmployeeCommand
{
    /// <summary>
    /// Executes the promotion on the server.
    /// </summary>
    [Remote, Execute]
    private static async Task<bool> _Execute(
        Guid employeeId,
        string newPosition,
        decimal salaryIncrease,
        [Service] IEmployeeRepository repository,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        var oldPosition = entity.Position;
        entity.Position = newPosition;
        entity.SalaryAmount += salaryIncrease;

        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);

        await auditLog.LogAsync(
            "Promote",
            employeeId,
            "Employee",
            $"Promoted from {oldPosition} to {newPosition} with ${salaryIncrease:N2} raise",
            ct);

        return true;
    }
}
#endregion
