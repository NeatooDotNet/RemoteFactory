using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.GettingStarted;

#region getting-started-employee-model
public interface IEmployeeModel : IFactorySaveMeta
{
    Guid Id { get; }
    [Required] string FirstName { get; set; }
    [Required] string LastName { get; set; }
    [EmailAddress] string? Email { get; set; }
    Guid? DepartmentId { get; set; }
    DateTime Created { get; }
    DateTime Modified { get; }
    new bool IsDeleted { get; set; }
}

[Factory]
public partial class EmployeeModel : IEmployeeModel
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public Guid? DepartmentId { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeModel()
    {
        Id = Guid.NewGuid();
        Created = DateTime.UtcNow;
        Modified = DateTime.UtcNow;
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        DepartmentId = entity.DepartmentId;
        Created = entity.HireDate;
        Modified = DateTime.UtcNow;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email ?? "",
            DepartmentId = DepartmentId ?? Guid.Empty,
            Position = "",
            SalaryAmount = 0,
            SalaryCurrency = "USD",
            HireDate = Created
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(Id, ct);
        if (entity != null)
        {
            entity.FirstName = FirstName;
            entity.LastName = LastName;
            entity.Email = Email ?? "";
            entity.DepartmentId = DepartmentId ?? Guid.Empty;
            await repository.UpdateAsync(entity, ct);
            await repository.SaveChangesAsync(ct);
        }
        Modified = DateTime.UtcNow;
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
#endregion
