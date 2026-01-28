using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-scope-isolation
/// <summary>
/// Demonstrates scope isolation for event execution.
/// </summary>
[Factory]
public partial class EmployeeScopeIsolation
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public void Create(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <summary>
    /// Event runs in isolated DI scope with fresh service instances.
    /// </summary>
    [Event]
    public async Task ProcessInIsolatedScope(
        Guid employeeId,
        string action,
        // NEW scoped instance - independent of caller's scope
        [Service] IEmployeeRepository repository,
        // NEW scoped instance - separate from any other concurrent events
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // Repository is a fresh scoped instance
        var employee = await repository.GetByIdAsync(employeeId, ct);

        // Audit service is also a fresh scoped instance
        await auditLog.LogAsync(
            action,
            employeeId,
            "Employee",
            $"Processed {employee?.FirstName ?? "unknown"} in isolated scope",
            ct);
    }
}
#endregion

/// <summary>
/// Event handlers for transactional example.
/// </summary>
[Factory]
public partial class TransactionalEventHandlers
{
    [Create]
    public void Create()
    {
    }

    /// <summary>
    /// Logs employee hiring event. Runs in separate transaction.
    /// </summary>
    [Event]
    public async Task LogEmployeeHired(
        Guid employeeId,
        string employeeName,
        DateTime hireDate,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        await auditLog.LogAsync(
            "Hired",
            employeeId,
            "Employee",
            $"Employee {employeeName} hired on {hireDate:yyyy-MM-dd}",
            ct);
    }
}

#region events-scope-example
/// <summary>
/// Employee with transactional independence between operation and event.
/// </summary>
[Factory]
public partial class EmployeeTransactional : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime? HireDate { get; private set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public void Create(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        Status = "Pending";
    }

    /// <summary>
    /// Hires employee and saves to repository.
    /// </summary>
    [Remote, Insert]
    public async Task HireEmployee(
        [Service] IEmployeeRepository repository,
        [Service] TransactionalEventHandlers.LogEmployeeHiredEvent logHired,
        CancellationToken ct)
    {
        Status = "Active";
        HireDate = DateTime.UtcNow;

        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = Name,
            LastName = "",
            Email = $"{Name.ToLowerInvariant()}@company.com",
            Position = "New Hire",
            HireDate = HireDate.Value
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;

        // Fire event in separate transaction (fire-and-forget)
        // If event fails, HireEmployee still succeeds
        _ = logHired(Id, Name, HireDate.Value);
    }
}
#endregion
