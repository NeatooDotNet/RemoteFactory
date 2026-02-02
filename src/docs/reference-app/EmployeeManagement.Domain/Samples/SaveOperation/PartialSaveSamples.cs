using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.SaveOperation;

// ============================================================================
// Partial Save Methods - Insert Only
// ============================================================================

#region save-partial-methods
// Insert-only entity: no Update/Delete means records are immutable after creation
[Factory]
public partial class AuditLog : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Action { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create] public AuditLog() { Id = Guid.NewGuid(); }
    [Remote, Insert] public Task Insert(CancellationToken ct) { IsNew = false; return Task.CompletedTask; }
    // No [Update] or [Delete] = immutable after insert
}
#endregion

// ============================================================================
// Save Without Delete
// ============================================================================

#region save-no-delete
// Insert + Update only: setting IsDeleted=true and calling Save throws NotImplementedException
[Factory]
public partial class EmployeeNoDelete : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create] public EmployeeNoDelete() { Id = Guid.NewGuid(); }
    [Remote, Insert] public Task Insert(CancellationToken ct) { IsNew = false; return Task.CompletedTask; }
    [Remote, Update] public Task Update(CancellationToken ct) { return Task.CompletedTask; }
    // No [Delete] = cannot delete, use for soft-delete patterns
}
#endregion
