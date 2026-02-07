using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-correlation
// Correlation ID propagates from triggering request to event
[Factory]
public partial class EmployeeCorrelation
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public void Create(string name) { Id = Guid.NewGuid(); Name = name; }

    [Event]
    public async Task LogWithCorrelation(Guid employeeId, string action,
        [Service] ICorrelationContext ctx, [Service] IAuditLogService auditLog, CancellationToken ct)
    {
        await auditLog.LogAsync(action, employeeId, "Employee", $"CorrelationId: {ctx.CorrelationId}", ct);
    }
}
#endregion
