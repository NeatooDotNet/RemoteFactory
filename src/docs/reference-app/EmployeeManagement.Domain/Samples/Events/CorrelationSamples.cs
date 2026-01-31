using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-correlation
/// <summary>
/// Demonstrates correlation ID propagation in events.
/// </summary>
[Factory]
public partial class EmployeeCorrelation
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
    /// Event that uses correlation ID for tracing.
    /// Correlation ID propagates from the original request.
    /// </summary>
    [Event]
    public async Task LogWithCorrelation(
        Guid employeeId,
        string action,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // CorrelationContext.CorrelationId contains the ID from the triggering request
        var correlationId = CorrelationContext.CorrelationId;

        // Include correlation ID in audit log for tracing
        await auditLog.LogAsync(
            action,
            employeeId,
            "Employee",
            $"CorrelationId: {correlationId} - Action: {action}",
            ct);
    }
}
#endregion
