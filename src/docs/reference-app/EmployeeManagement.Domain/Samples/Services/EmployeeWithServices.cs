using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Services;

/// <summary>
/// Employee aggregate demonstrating various service injection patterns.
/// </summary>
[Factory]
public partial class EmployeeWithServices : IFactorySaveMeta
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
    public EmployeeWithServices()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Demonstrates multiple service injection in a single operation.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
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

        // Log the fetch operation
        await auditLog.LogAsync("Fetch", id, "Employee", $"Employee {FirstName} {LastName} fetched", ct);

        return true;
    }

    /// <summary>
    /// Demonstrates service injection with email notification.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        [Service] IEmailService emailService,
        [Service] IAuditLogService auditLog,
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

        // Send welcome email
        await emailService.SendAsync(
            Email,
            $"Welcome to the team, {FirstName}!",
            $"You have been added as {Position}.",
            ct);

        // Log the insert operation
        await auditLog.LogAsync("Insert", Id, "Employee", $"Employee {FirstName} {LastName} created", ct);
    }

    /// <summary>
    /// Demonstrates service injection with IUserContext for authorization checks.
    /// </summary>
    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        [Service] IUserContext userContext,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // Business logic: only HR can modify salary
        var existingEntity = await repository.GetByIdAsync(Id, ct);
        if (existingEntity != null &&
            existingEntity.SalaryAmount != Salary &&
            !userContext.IsInRole("HR"))
        {
            throw new UnauthorizedAccessException("Only HR can modify salary");
        }

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

        // Log with user information
        await auditLog.LogAsync(
            "Update",
            Id,
            "Employee",
            $"Employee {FirstName} {LastName} updated by {userContext.Username}",
            ct);
    }

    [Remote, Delete]
    public async Task Delete(
        [Service] IEmployeeRepository repository,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);

        await auditLog.LogAsync("Delete", Id, "Employee", $"Employee deleted", ct);
    }
}
