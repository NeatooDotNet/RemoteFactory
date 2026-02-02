using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

// Event handler for domain events - region removed (using events-domain-events in EmployeeEventHandlers.cs)
[Factory]
public partial class DomainEventHandlers
{
    [Create]
    public void Create() { }

    [Event]
    public async Task OnEmployeeActivated(Guid employeeId, string employeeName,
        [Service] IEmployeeRepository repository, [Service] IAuditLogService auditLog, CancellationToken ct)
    {
        var employee = await repository.GetByIdAsync(employeeId, ct);
        if (employee != null)
            await auditLog.LogAsync("Activated", employeeId, "Employee", $"{employeeName} active", ct);
    }
}

// Aggregate with domain event pattern - full class needed for compilation/tests
[Factory]
public partial class EmployeeDomainEvent : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public void Create(string name) { Id = Guid.NewGuid(); Name = name; Status = "Pending"; }

    [Remote, Update]
    public async Task Activate([Service] IEmployeeRepository repository,
        [Service] DomainEventHandlers.EmployeeActivatedEvent onActivated, CancellationToken ct)
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
        _ = onActivated(Id, Name); // Fire domain event
    }
}
