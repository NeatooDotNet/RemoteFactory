using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.SaveOperation;

// ============================================================================
// Complete Department Entity Example
// ============================================================================

#region save-complete-example
// Full CRUD: [Factory] + IFactorySaveMeta + Create/Fetch/Insert/Update/Delete
[Factory]
public partial class DepartmentCrud : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create] public DepartmentCrud() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IDepartmentRepository r, CancellationToken ct) { return true; }

    [Remote, Insert]
    public async Task Insert([Service] IDepartmentRepository r, CancellationToken ct) { IsNew = false; }

    [Remote, Update]
    public async Task Update([Service] IDepartmentRepository r, CancellationToken ct) { }

    [Remote, Delete]
    public async Task Delete([Service] IDepartmentRepository r, CancellationToken ct) { }
}
#endregion

// Full implementation for actual use
[Factory]
public partial class DepartmentCrudFull : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public Guid? ManagerId { get; set; }
    public decimal Budget { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public DepartmentCrudFull()
    {
        Id = Guid.NewGuid();
        IsActive = true;
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IDepartmentRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        Name = entity.Name;
        Code = entity.Code;
        ManagerId = entity.ManagerId;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        var entity = new DepartmentEntity { Id = Id, Name = Name, Code = Code, ManagerId = ManagerId };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(Id, ct) ?? throw new InvalidOperationException($"Department {Id} not found");
        entity.Name = Name;
        entity.Code = Code;
        entity.ManagerId = ManagerId;
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
