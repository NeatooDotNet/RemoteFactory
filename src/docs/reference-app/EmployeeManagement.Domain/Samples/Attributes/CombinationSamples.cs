using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-multiple-operations
/// <summary>
/// Department aggregate with combined [Insert, Update] on single method.
/// </summary>
[Factory]
public partial class DepartmentWithCombinedOps : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public decimal Budget { get; set; }
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Create]
    public DepartmentWithCombinedOps()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    /// <summary>
    /// [Insert, Update] combined - upsert pattern.
    /// Called by Save for both new and existing entities.
    /// </summary>
    [Remote, Insert, Update]
    public async Task Save(
        [Service] IDepartmentRepository repository,
        CancellationToken ct)
    {
        var entity = new DepartmentEntity
        {
            Id = Id,
            Name = Name,
            Budget = Budget
        };

        if (IsNew)
        {
            await repository.AddAsync(entity, ct);
            IsNew = false;
        }
        else
        {
            await repository.UpdateAsync(entity, ct);
        }

        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete(
        [Service] IDepartmentRepository repository,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
#endregion

#region attributes-remote-operation
/// <summary>
/// Employee aggregate with [Remote, Fetch] combination.
/// </summary>
[Factory]
public partial class EmployeeRemoteFetch
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeRemoteFetch()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// [Remote] + operation = server-side execution with serialization.
    /// </summary>
    [Remote, Fetch]
    public async Task FetchFromDatabase(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
        }
    }
}

/// <summary>
/// Promote command with [Remote, Execute].
/// </summary>
[Factory]
public static partial class PromoteEmployeeCommand
{
    /// <summary>
    /// [Remote, Execute] - server-side command execution.
    /// </summary>
    [Remote, Execute]
    private static async Task<bool> _Promote(
        Guid employeeId,
        string newTitle,
        decimal salaryIncrease,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var employee = await repository.GetByIdAsync(employeeId, ct);
        if (employee == null) return false;

        employee.Position = newTitle;
        employee.SalaryAmount += salaryIncrease;

        await repository.UpdateAsync(employee, ct);
        await repository.SaveChangesAsync(ct);

        return true;
    }
}
#endregion
