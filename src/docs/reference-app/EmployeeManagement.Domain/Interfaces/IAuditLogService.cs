namespace EmployeeManagement.Domain.Interfaces;

/// <summary>
/// Service for recording audit log entries.
/// </summary>
public interface IAuditLogService
{
    Task LogAsync(string action, Guid entityId, string entityType, string details, CancellationToken ct = default);
}
