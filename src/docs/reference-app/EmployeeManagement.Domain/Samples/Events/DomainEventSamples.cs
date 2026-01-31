using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

/// <summary>
/// Event handlers for domain events.
/// </summary>
[Factory]
public partial class DomainEventHandlers
{
    [Create]
    public void Create()
    {
    }

    /// <summary>
    /// Domain event handler that updates read model or projection.
    /// </summary>
    [Event]
    public async Task OnEmployeeActivated(
        Guid employeeId,
        string employeeName,
        [Service] IEmployeeRepository repository,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // Update read model/projection in isolated scope
        var employee = await repository.GetByIdAsync(employeeId, ct);
        if (employee != null)
        {
            // Update any projections or read models
            await auditLog.LogAsync(
                "Activated",
                employeeId,
                "Employee",
                $"Employee {employeeName} is now active",
                ct);
        }
    }
}

#region events-domain-events
/// <summary>
/// Employee aggregate with domain event pattern for read model updates.
/// </summary>
[Factory]
public partial class EmployeeDomainEvent : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "Pending";
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
    /// Activates the employee and triggers domain event.
    /// </summary>
    [Remote, Update]
    public async Task Activate(
        [Service] IEmployeeRepository repository,
        [Service] DomainEventHandlers.EmployeeActivatedEvent onActivated,
        CancellationToken ct)
    {
        Status = "Active";

        var entity = await repository.GetByIdAsync(Id, ct);
        if (entity != null)
        {
            entity.Position = "Active Employee";
            await repository.UpdateAsync(entity, ct);
            await repository.SaveChangesAsync(ct);
        }
        IsNew = false;

        // Fire domain event for read model update
        _ = onActivated(Id, Name);
    }
}
#endregion
