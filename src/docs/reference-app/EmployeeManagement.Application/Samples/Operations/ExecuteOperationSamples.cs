using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Operations;

// Note: Primary Execute operation snippet is in Domain/OperationsSamples.cs (operations-execute)
// This file contains supplementary Execute patterns that require Application layer dependencies

/// <summary>
/// Result of an employee transfer operation.
/// </summary>
public record TransferResult(Guid EmployeeId, Guid NewDepartmentId, DateTime TransferDate, bool Success);

/// <summary>
/// Command for transferring an employee to a new department.
/// </summary>
[SuppressFactory]
public static partial class EmployeeTransferCommand
{
    #region operations-execute-command
    // Command pattern with [Execute] - static class with private method, underscore prefix removed
    [Remote, Execute]
    private static async Task<TransferResult> _TransferEmployee(
        Guid employeeId, Guid newDepartmentId, DateTime effectiveDate,
        [Service] IEmployeeRepository employeeRepo, [Service] IDepartmentRepository departmentRepo)
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId);
        if (employee == null) throw new InvalidOperationException($"Employee {employeeId} not found.");
        employee.DepartmentId = newDepartmentId;
        await employeeRepo.UpdateAsync(employee);
        await employeeRepo.SaveChangesAsync();
        return new TransferResult(employeeId, newDepartmentId, effectiveDate, true);
    }
    #endregion
}

/// <summary>
/// Result of a batch assignment operation.
/// </summary>
public record BatchAssignmentResult(Guid[] AssignedEmployeeIds, List<string> AssignedDepartments);

/// <summary>
/// Command for batch assignment operations.
/// </summary>
[SuppressFactory]
public static partial class BatchAssignmentCommand
{
    #region operations-params-array-batch
    // Array and List parameters are serialized for batch operations
    [Remote, Execute]
    private static Task<BatchAssignmentResult> _AssignToDepartments(Guid[] employeeIds, List<string> departmentNames)
    {
        return Task.FromResult(new BatchAssignmentResult(employeeIds, departmentNames));
    }
    #endregion
}
