using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.SaveOperation;

// ============================================================================
// IFactorySaveMeta Implementation
// ============================================================================

#region save-ifactorysavemeta
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

    /// <summary>
    /// True for new entities not yet persisted. Defaults to true.
    /// </summary>
    public bool IsNew { get; private set; } = true;

    /// <summary>
    /// True for entities marked for deletion.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new employee with a generated Id.
    /// IsNew defaults to true, so Save will route to Insert.
    /// </summary>
    [Create]
    public EmployeeForSave()
    {
        Id = Guid.NewGuid();
        // IsNew = true by default
    }

    /// <summary>
    /// Loads an existing employee from the repository.
    /// Sets IsNew = false so Save will route to Update.
    /// </summary>
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
#endregion

// ============================================================================
// Full CRUD Operations
// ============================================================================

#region save-write-operations
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

    /// <summary>
    /// Loads an existing employee from the repository.
    /// Sets IsNew = false so Save will route to Update.
    /// </summary>
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

    /// <summary>
    /// Inserts a new employee into the database.
    /// Called by Save when IsNew = true.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            DepartmentId = DepartmentId,
            HireDate = DateTime.UtcNow // Created timestamp
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false; // After insert, no longer new
    }

    /// <summary>
    /// Updates an existing employee in the database.
    /// Called by Save when IsNew = false and IsDeleted = false.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(Id, ct)
            ?? throw new InvalidOperationException($"Employee {Id} not found");

        entity.FirstName = FirstName;
        entity.LastName = LastName;
        entity.DepartmentId = DepartmentId;
        // Modified timestamp would be updated here

        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Deletes the employee from the database.
    /// Called by Save when IsDeleted = true.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
#endregion

// ============================================================================
// Generated Save Routing Logic (Conceptual)
// ============================================================================

#region save-generated
/// <summary>
/// Conceptual illustration of the generated Save method routing logic.
/// The actual implementation is source-generated by RemoteFactory.
/// </summary>
public static class SaveRoutingLogic
{
    // The generated Save method follows this decision tree:
    //
    // async Task<T?> LocalSave(T entity, CancellationToken ct)
    // {
    //     if (entity.IsDeleted)
    //     {
    //         if (entity.IsNew)
    //             return default;  // New entity deleted before save = no-op
    //         else
    //             return await LocalDelete(ct);  // Existing entity = delete
    //     }
    //
    //     if (entity.IsNew)
    //         return await LocalInsert(ct);  // New entity = insert
    //
    //     return await LocalUpdate(ct);  // Existing entity = update
    // }
    //
    // Routing summary:
    // | IsNew  | IsDeleted | Result      |
    // |--------|-----------|-------------|
    // | true   | false     | LocalInsert |
    // | false  | false     | LocalUpdate |
    // | false  | true      | LocalDelete |
    // | true   | true      | null (no-op)|
}
#endregion
