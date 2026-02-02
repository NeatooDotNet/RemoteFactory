using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.ValueObjects;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.AspNetCore;

#region aspnetcore-correlation-id
/// <summary>
/// Employee with correlation ID support for distributed tracing.
/// </summary>
[Factory]
public partial class EmployeeWithCorrelation
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public EmailAddress Email { get; set; } = null!;

    [Create]
    public EmployeeWithCorrelation()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an Employee and logs the access with correlation ID for tracing.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] ICorrelationContext correlationContext,
        [Service] IEmployeeRepository repository,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // CorrelationId is auto-populated from X-Correlation-Id header by middleware
        var correlationId = correlationContext.CorrelationId;

        var entity = await repository.GetByIdAsync(id, ct);

        if (entity == null)
            return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = new EmailAddress(entity.Email);

        // Log with correlation ID for distributed tracing
        await auditLog.LogAsync(
            action: "Fetch",
            entityId: Id,
            entityType: nameof(EmployeeWithCorrelation),
            details: $"Fetched by correlation: {correlationId}",
            ct);

        return true;
    }
}
#endregion
