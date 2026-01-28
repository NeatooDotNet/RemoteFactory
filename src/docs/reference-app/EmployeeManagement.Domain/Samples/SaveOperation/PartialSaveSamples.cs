using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.SaveOperation;

// ============================================================================
// Partial Save Methods - Insert Only
// ============================================================================

#region save-partial-methods
/// <summary>
/// AuditLog entity that only supports Insert - records are immutable after creation.
/// Use this pattern for audit logs, event sourcing, or compliance records.
/// </summary>
[Factory]
public partial class AuditLog : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public DateTime Timestamp { get; private set; }
    public Guid UserId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new audit log entry with auto-generated Id and timestamp.
    /// </summary>
    [Create]
    public AuditLog()
    {
        Id = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Inserts the audit log record.
    /// This is the ONLY write operation - audit logs are immutable.
    /// </summary>
    [Remote, Insert]
    public Task Insert(CancellationToken ct)
    {
        // Persist to audit store
        IsNew = false;
        return Task.CompletedTask;
    }

    // No Update method = entity becomes read-only after creation
    // No Delete method = audit records cannot be deleted

    // Save behavior:
    // - If IsNew: routes to Insert
    // - If not IsNew: no-op (no Update defined)
    // - If IsDeleted: throws NotImplementedException (no Delete defined)
}
#endregion

// ============================================================================
// Save Without Delete
// ============================================================================

#region save-no-delete
/// <summary>
/// Employee entity that supports Insert and Update but not Delete.
/// Use for soft-delete patterns where actual deletion is not allowed.
/// </summary>
[Factory]
public partial class EmployeeNoDelete : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeNoDelete()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Insert]
    public Task Insert(CancellationToken ct)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Update]
    public Task Update(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    // NO Delete method defined
    // Setting IsDeleted = true and calling Save throws NotImplementedException
    // Use case: soft-delete pattern where records are deactivated, not removed
}
#endregion
