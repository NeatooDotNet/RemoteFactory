namespace EmployeeManagement.Domain.Interfaces;

/// <summary>
/// Repository for Department persistence entities.
/// </summary>
public interface IDepartmentRepository
{
    Task<DepartmentEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<DepartmentEntity>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(DepartmentEntity entity, CancellationToken ct = default);
    Task UpdateAsync(DepartmentEntity entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Department persistence entity.
/// </summary>
public class DepartmentEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public Guid? ManagerId { get; set; }
    public decimal Budget { get; set; }
}
