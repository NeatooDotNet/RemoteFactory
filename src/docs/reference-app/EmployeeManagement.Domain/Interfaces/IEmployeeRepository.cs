namespace EmployeeManagement.Domain.Interfaces;

/// <summary>
/// Repository for Employee persistence entities.
/// </summary>
public interface IEmployeeRepository
{
    Task<EmployeeEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<EmployeeEntity>> GetAllAsync(CancellationToken ct = default);
    Task<List<EmployeeEntity>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken ct = default);
    Task AddAsync(EmployeeEntity entity, CancellationToken ct = default);
    Task UpdateAsync(EmployeeEntity entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Employee persistence entity.
/// </summary>
public class EmployeeEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public Guid DepartmentId { get; set; }
    public string Position { get; set; } = "";
    public decimal SalaryAmount { get; set; }
    public string SalaryCurrency { get; set; } = "USD";
    public DateTime HireDate { get; set; }
}
