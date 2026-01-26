using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Aggregates;

/// <summary>
/// Department aggregate root with CRUD operations.
/// </summary>
[Factory]
public partial class Department : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public Guid? ManagerId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new Department with a generated ID.
    /// </summary>
    [Create]
    public Department()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an existing Department by ID.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IDepartmentRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        MapFromEntity(entity);
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Inserts a new Department into the repository.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        var entity = MapToEntity();
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    /// <summary>
    /// Updates an existing Department in the repository.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        var entity = MapToEntity();
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Deletes the Department from the repository.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }

    private void MapFromEntity(DepartmentEntity entity)
    {
        Id = entity.Id;
        Name = entity.Name;
        Code = entity.Code;
        ManagerId = entity.ManagerId;
    }

    private DepartmentEntity MapToEntity() => new()
    {
        Id = Id,
        Name = Name,
        Code = Code,
        ManagerId = ManagerId
    };
}
