using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Operations;

#region operations-execute
/// <summary>
/// Result of an employee promotion operation.
/// </summary>
public record PromotionResult(Guid EmployeeId, bool IsApproved, string NewTitle, decimal NewSalary);

/// <summary>
/// Command for promoting an employee.
/// </summary>
[SuppressFactory]
public static partial class EmployeePromotionCommand
{
    /// <summary>
    /// Promotes an employee with a new title and salary increase.
    /// </summary>
    [Remote, Execute]
    private static async Task<PromotionResult> _PromoteEmployee(
        Guid employeeId,
        string newTitle,
        decimal salaryIncrease,
        [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null)
            throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        entity.Position = newTitle;
        entity.SalaryAmount += salaryIncrease;

        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();

        return new PromotionResult(employeeId, true, newTitle, entity.SalaryAmount);
    }
}
#endregion

#region operations-execute-command
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
    /// <summary>
    /// Transfers an employee to a new department.
    /// </summary>
    [Remote, Execute]
    private static async Task<TransferResult> _TransferEmployee(
        Guid employeeId,
        Guid newDepartmentId,
        DateTime effectiveDate,
        [Service] IEmployeeRepository employeeRepo,
        [Service] IDepartmentRepository departmentRepo)
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId);
        if (employee == null)
            throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        // Validate employee is in Active status (using Position as a status proxy)
        if (employee.Position == "Terminated")
            throw new InvalidOperationException("Cannot transfer a terminated employee.");

        employee.DepartmentId = newDepartmentId;

        await employeeRepo.UpdateAsync(employee);
        await employeeRepo.SaveChangesAsync();

        return new TransferResult(employeeId, newDepartmentId, effectiveDate, true);
    }
}
#endregion

#region operations-params-array
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
    /// <summary>
    /// Assigns multiple employees to departments.
    /// Demonstrates array and List parameter types.
    /// </summary>
    [Remote, Execute]
    private static Task<BatchAssignmentResult> _AssignToDepartments(
        Guid[] employeeIds,
        List<string> departmentNames)
    {
        return Task.FromResult(new BatchAssignmentResult(employeeIds, departmentNames));
    }
}
#endregion
