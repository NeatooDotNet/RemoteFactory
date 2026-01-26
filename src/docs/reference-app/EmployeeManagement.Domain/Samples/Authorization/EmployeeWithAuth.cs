using EmployeeManagement.Domain.Authorization;
using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Authorization;

/// <summary>
/// Employee aggregate with authorization applied.
/// Demonstrates the AuthorizeFactory attribute pattern.
/// </summary>
[Factory]
[AuthorizeFactory<EmployeeAuthorization>]
public partial class EmployeeWithAuth : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public string Position { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithAuth()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        DepartmentId = entity.DepartmentId;
        Position = entity.Position;
        Salary = entity.SalaryAmount;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            DepartmentId = DepartmentId,
            Position = Position,
            SalaryAmount = Salary,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            DepartmentId = DepartmentId,
            Position = Position,
            SalaryAmount = Salary,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
