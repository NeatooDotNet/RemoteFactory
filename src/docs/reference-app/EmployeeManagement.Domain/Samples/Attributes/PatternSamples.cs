using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-pattern-crud
/// <summary>
/// Complete Employee CRUD entity pattern.
/// </summary>
[Factory]
public partial class EmployeeCrud : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public DateTime HireDate { get; private set; }
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeCrud()
    {
        Id = Guid.NewGuid();
        HireDate = DateTime.UtcNow;
        IsNew = true;
    }

    [Remote, Fetch]
    public async Task Fetch(
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
            Email = entity.Email;
            DepartmentId = entity.DepartmentId;
            HireDate = entity.HireDate;
            IsNew = false;
        }
    }

    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = MapToEntity();
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = MapToEntity();
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }

    private EmployeeEntity MapToEntity() => new()
    {
        Id = Id,
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        DepartmentId = DepartmentId,
        HireDate = HireDate
    };
}
#endregion

#region attributes-pattern-readonly
/// <summary>
/// Read-only Employee snapshot - Create and Fetch only, no persistence.
/// </summary>
[Factory]
public partial class EmployeeSnapshot
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public string DepartmentName { get; private set; } = "";
    public DateTime SnapshotDate { get; private set; }

    /// <summary>
    /// Creates a new snapshot with current timestamp.
    /// </summary>
    [Create]
    public EmployeeSnapshot()
    {
        Id = Guid.NewGuid();
        SnapshotDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Loads snapshot data from repository.
    /// No Insert, Update, or Delete - read-only after creation.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository employeeRepo,
        [Service] IDepartmentRepository departmentRepo,
        CancellationToken ct)
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId, ct);
        if (employee != null)
        {
            Id = employee.Id;
            FirstName = employee.FirstName;
            LastName = employee.LastName;

            var department = await departmentRepo.GetByIdAsync(employee.DepartmentId, ct);
            DepartmentName = department?.Name ?? "Unknown";
        }
    }
}
#endregion
