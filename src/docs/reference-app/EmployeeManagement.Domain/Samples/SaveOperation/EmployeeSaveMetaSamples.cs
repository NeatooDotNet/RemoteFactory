using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.SaveOperation;

// ============================================================================
// IFactorySaveMeta Implementation
// ============================================================================

/// <summary>
/// Employee aggregate implementing IFactorySaveMeta for automatic Save routing.
/// </summary>
[Factory]
public partial class EmployeeForSave : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }

    #region save-ifactorysavemeta
    // IFactorySaveMeta requires: IsNew (true for new entities) and IsDeleted (true for deletion)
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }
    #endregion

    [Create]
    public EmployeeForSave()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        IsNew = false; // Fetched entities are not new
        return true;
    }
}

// ============================================================================
// Full CRUD Operations
// ============================================================================

/// <summary>
/// Employee aggregate with full Insert, Update, Delete operations.
/// </summary>
[Factory]
public partial class EmployeeCrud : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeCrud()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        DepartmentId = entity.DepartmentId;
        IsNew = false;
        return true;
    }

    #region save-write-operations
    // Save routes to Insert/Update/Delete based on IsNew and IsDeleted flags
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.AddAsync(new EmployeeEntity { Id = Id, FirstName = FirstName }, ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var e = await repo.GetByIdAsync(Id, ct);
        if (e != null) { e.FirstName = FirstName; await repo.UpdateAsync(e, ct); }
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
    }
    #endregion
}

// ============================================================================
// Generated Save Routing Logic (Conceptual)
// ============================================================================

#region save-generated
// Save routing: IsNew=true -> Insert, IsNew=false -> Update, IsDeleted=true -> Delete
// | IsNew | IsDeleted | Operation |
// |-------|-----------|-----------|
// | true  | false     | Insert    |
// | false | false     | Update    |
// | false | true      | Delete    |
// | true  | true      | no-op     |
#endregion
