using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Attributes;

#region attributes-execute
/// <summary>
/// Result of a transfer operation.
/// </summary>
public record TransferResult(Guid EmployeeId, Guid NewDepartmentId, bool Success);

/// <summary>
/// Command for transferring an employee to a new department.
/// [Execute] must be used in a static partial class.
/// </summary>
[Factory]
public static partial class TransferEmployeeCommand
{
    /// <summary>
    /// Local execute - runs on the calling machine.
    /// </summary>
    [Execute]
    private static Task<TransferResult> _TransferEmployee(
        Guid employeeId,
        Guid newDepartmentId,
        [Service] IEmployeeRepository repository)
    {
        return Task.FromResult(new TransferResult(employeeId, newDepartmentId, true));
    }

    /// <summary>
    /// Remote execute - serializes to server and executes there.
    /// </summary>
    [Remote, Execute]
    private static async Task<TransferResult> _TransferEmployeeRemote(
        Guid employeeId,
        Guid newDepartmentId,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var employee = await repository.GetByIdAsync(employeeId, ct);
        if (employee == null)
        {
            return new TransferResult(employeeId, newDepartmentId, false);
        }

        employee.DepartmentId = newDepartmentId;
        await repository.UpdateAsync(employee, ct);
        await repository.SaveChangesAsync(ct);

        return new TransferResult(employeeId, newDepartmentId, true);
    }
}
#endregion
