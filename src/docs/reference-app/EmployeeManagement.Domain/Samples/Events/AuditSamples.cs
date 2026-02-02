using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

// Full audit event class - region removed (using events-audit in EmployeeEventHandlers.cs)
[Factory]
public partial class AuditEvents
{
    [Create]
    public void Create() { }

    [Event]
    public async Task LogAuditTrail(string action, Guid entityId, string entityType, string details,
        [Service] IAuditLogService auditLog, CancellationToken ct)
    {
        await auditLog.LogAsync(action, entityId, entityType, details, ct);
    }
}
