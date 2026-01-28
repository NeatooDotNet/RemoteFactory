using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.SaveOperation;

// ============================================================================
// Optimistic Concurrency with Row Versioning
// ============================================================================

#region save-optimistic-concurrency
/// <summary>
/// Employee aggregate with optimistic concurrency control using row versioning.
/// </summary>
[Factory]
public partial class ConcurrentEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    /// <summary>
    /// Concurrency token for optimistic locking.
    /// Updated by database on each save.
    /// </summary>
    public byte[]? RowVersion { get; private set; }

    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ConcurrentEmployee()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches employee including RowVersion for concurrency checking.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        // RowVersion would be loaded from the entity in a real implementation
        // RowVersion = entity.RowVersion;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Update with concurrency check.
    /// Throws if another user modified the record.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var current = await repository.GetByIdAsync(Id, ct)
            ?? throw new InvalidOperationException($"Employee {Id} not found");

        // In a real EF Core implementation, the RowVersion would be compared:
        // if (current.RowVersion != null && RowVersion != null &&
        //     !current.RowVersion.SequenceEqual(RowVersion))
        // {
        //     throw new InvalidOperationException(
        //         "The record has been modified by another user. Please refresh and try again.");
        // }

        current.FirstName = FirstName;
        current.LastName = LastName;
        // RowVersion is updated automatically by database

        await repository.UpdateAsync(current, ct);
        await repository.SaveChangesAsync(ct);
    }
}
#endregion
