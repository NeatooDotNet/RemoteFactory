using EmployeeManagement.Domain.Interfaces;
using System.Collections.Concurrent;

namespace EmployeeManagement.Infrastructure.Services;

/// <summary>
/// In-memory implementation of IAuditLogService for demonstration.
/// </summary>
public class InMemoryAuditLogService : IAuditLogService
{
    private static readonly ConcurrentBag<AuditLogEntry> AuditEntries = new();

    public Task LogAsync(string action, Guid entityId, string entityType, string details, CancellationToken ct = default)
    {
        AuditEntries.Add(new AuditLogEntry
        {
            Action = action,
            EntityId = entityId,
            EntityType = entityType,
            Details = details,
            Timestamp = DateTime.UtcNow
        });
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets all audit entries. Useful for testing.
    /// </summary>
    public static IReadOnlyList<AuditLogEntry> GetEntries()
    {
        return AuditEntries.ToList();
    }

    /// <summary>
    /// Clears all audit entries. Useful for testing.
    /// </summary>
    public static void Clear()
    {
        AuditEntries.Clear();
    }
}

/// <summary>
/// Audit log entry.
/// </summary>
public class AuditLogEntry
{
    public string Action { get; set; } = "";
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
