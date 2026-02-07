using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.SaveOperation;

// ============================================================================
// Optimistic Concurrency with Row Versioning
// ============================================================================

#region save-optimistic-concurrency
// Add RowVersion property; EF Core throws DbUpdateConcurrencyException on conflict -> 409 response
[Factory]
public partial class ConcurrentEmployee : IFactorySaveMeta
{
    public byte[]? RowVersion { get; private set; }  // Concurrency token, auto-updated by database
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }
    /* Fetch loads RowVersion, Update includes it for conflict detection */
}
#endregion

// Full implementation
public partial class ConcurrentEmployee
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public ConcurrentEmployee() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        // RowVersion = entity.RowVersion;  // Would load from entity
        IsNew = false;
        return true;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var current = await repository.GetByIdAsync(Id, ct) ?? throw new InvalidOperationException($"Employee {Id} not found");
        // EF Core checks RowVersion automatically; throws DbUpdateConcurrencyException on conflict
        current.FirstName = FirstName;
        current.LastName = LastName;
        await repository.UpdateAsync(current, ct);
        await repository.SaveChangesAsync(ct);
    }
}
