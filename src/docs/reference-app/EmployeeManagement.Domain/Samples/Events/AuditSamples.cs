using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-audit
/// <summary>
/// Audit event handler for fire-and-forget audit logging.
/// </summary>
[Factory]
public partial class AuditEvents
{
    [Create]
    public void Create()
    {
    }

    /// <summary>
    /// Logs audit trail entry asynchronously.
    /// </summary>
    [Event]
    public async Task LogAuditTrail(
        string action,
        Guid entityId,
        string entityType,
        string details,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        await auditLog.LogAsync(action, entityId, entityType, details, ct);
    }
}
#endregion
