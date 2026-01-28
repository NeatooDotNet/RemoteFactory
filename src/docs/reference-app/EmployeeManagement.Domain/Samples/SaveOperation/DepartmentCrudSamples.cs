using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.SaveOperation;

// ============================================================================
// Complete Department Entity Example
// ============================================================================

#region save-complete-example
/// <summary>
/// Complete Department aggregate with full CRUD operations via IFactorySaveMeta.
/// </summary>
[Factory]
public partial class DepartmentCrud : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public Guid? ManagerId { get; set; }
    public decimal Budget { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new department with generated Id and default active status.
    /// </summary>
    [Create]
    public DepartmentCrud()
    {
        Id = Guid.NewGuid();
        IsActive = true;
    }

    /// <summary>
    /// Loads an existing department from the repository.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IDepartmentRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        Name = entity.Name;
        Code = entity.Code;
        ManagerId = entity.ManagerId;
        // Budget and IsActive would be loaded from extended entity
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Inserts a new department into the database.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        var entity = new DepartmentEntity
        {
            Id = Id,
            Name = Name,
            Code = Code,
            ManagerId = ManagerId
            // Budget, IsActive, Created/Modified timestamps would be set in extended entity
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    /// <summary>
    /// Updates an existing department in the database.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(Id, ct)
            ?? throw new InvalidOperationException($"Department {Id} not found");

        entity.Name = Name;
        entity.Code = Code;
        entity.ManagerId = ManagerId;
        // Budget, IsActive, Modified timestamp would be updated in extended entity

        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Deletes the department from the database.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
#endregion
