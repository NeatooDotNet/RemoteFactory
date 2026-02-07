using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Attributes;

// Full implementations for Execute - see MinimalAttributesSamples.cs for doc snippets

public record TransferResult(Guid EmployeeId, Guid NewDepartmentId, bool Success);

[Factory]
public static partial class TransferEmployeeCommand
{
    [Execute]
    private static Task<TransferResult> _TransferEmployee(
        Guid employeeId,
        Guid newDepartmentId,
        [Service] IEmployeeRepository repository)
    {
        return Task.FromResult(new TransferResult(employeeId, newDepartmentId, true));
    }

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
