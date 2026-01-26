using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.ValueObjects;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Aggregates;

#region factory-employee-aggregate
/// <summary>
/// Employee aggregate root with full CRUD operations.
/// </summary>
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

    /// <summary>
    /// Creates a new Employee with a generated ID.
    /// </summary>
    [Create]
    public Employee()
    {
        Id = Guid.NewGuid();
        Salary = new Money(0, "USD");
        HireDate = DateTime.UtcNow;
    }

    #region factory-fetch-operation
    /// <summary>
    /// Fetches an existing Employee by ID.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        MapFromEntity(entity);
        IsNew = false;
        return true;
    }
    #endregion

    #region factory-insert-operation
    /// <summary>
    /// Inserts a new Employee into the repository.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = MapToEntity();
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
    #endregion

    #region factory-update-operation
    /// <summary>
    /// Updates an existing Employee in the repository.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = MapToEntity();
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }
    #endregion

    #region factory-delete-operation
    /// <summary>
    /// Deletes the Employee from the repository.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
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
#endregion
