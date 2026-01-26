using EmployeeManagement.Domain.Events;
using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

/// <summary>
/// Employee aggregate that raises domain events during operations.
/// Demonstrates event delegate invocation pattern.
/// </summary>
[Factory]
public partial class EmployeeWithEvents : IFactorySaveMeta
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

    public string FullName => $"{FirstName} {LastName}";

    [Create]
    public EmployeeWithEvents()
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
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        [Service] EmployeeEventHandlers.NotifyHROfNewEmployeeEvent notifyHR,
        CancellationToken ct)
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

        // Fire event for new employee notification (fire-and-forget)
        _ = notifyHR(Id, FullName);
    }

    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        [Service] EmployeeEventHandlers.NotifyManagerOfPromotionEvent notifyPromotion,
        CancellationToken ct)
    {
        var existingEntity = await repository.GetByIdAsync(Id, ct);
        var oldPosition = existingEntity?.Position ?? "";

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

        // If position changed, raise promotion event
        if (!string.Equals(oldPosition, Position, StringComparison.Ordinal))
        {
            _ = notifyPromotion(Id, FullName, oldPosition, Position);
        }
    }

    [Remote, Delete]
    public async Task Delete(
        [Service] IEmployeeRepository repository,
        [Service] EmployeeEventHandlers.LogEmployeeDepartureEvent logDeparture,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);

        // Fire event for employee departure
        _ = logDeparture(Id, "Deleted from system");
    }
}
