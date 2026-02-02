using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.ValueObjects;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Aggregates;

// Employee aggregate root with factory methods
[Factory]
public partial class Employee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public EmailAddress Email { get; set; } = null!;
    public PhoneNumber? Phone { get; set; }
    public Guid DepartmentId { get; set; }
    public string Position { get; set; } = "";
    public Money Salary { get; set; } = null!;
    public DateTime HireDate { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    // Constructor initializes defaults (outside snippet for brevity)
    [Create]
    public Employee()
    {
        Id = Guid.NewGuid();
        Salary = new Money(0, "USD");
        HireDate = DateTime.UtcNow;
    }

    #region getting-started-employee-model
    // [Remote] executes on server; [Service] injects from DI (not serialized)
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        MapFromEntity(entity);
        IsNew = false;
        return true;
    }

    // Save() routes to Insert/Update/Delete based on IsNew and IsDeleted
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.AddAsync(MapToEntity(), ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.UpdateAsync(MapToEntity(), ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
    #endregion

    private void MapFromEntity(EmployeeEntity entity)
    {
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = new EmailAddress(entity.Email);
        Phone = entity.Phone != null ? ParsePhone(entity.Phone) : null;
        DepartmentId = entity.DepartmentId;
        Position = entity.Position;
        Salary = new Money(entity.SalaryAmount, entity.SalaryCurrency);
        HireDate = entity.HireDate;
    }

    private EmployeeEntity MapToEntity() => new()
    {
        Id = Id,
        FirstName = FirstName,
        LastName = LastName,
        Email = Email.Value,
        Phone = Phone?.ToString(),
        DepartmentId = DepartmentId,
        Position = Position,
        SalaryAmount = Salary.Amount,
        SalaryCurrency = Salary.Currency,
        HireDate = HireDate
    };

    private static PhoneNumber ParsePhone(string phone)
    {
        var parts = phone.Split(' ', 2);
        return new PhoneNumber(parts[0], parts.Length > 1 ? parts[1] : "");
    }
}
