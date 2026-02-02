using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Skill;

#region skill-interface-factory-complete
[Factory]
public interface IEmployeeQueryService
{
    Task<IReadOnlyList<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<int> CountAsync();
}

// Server implementation (no [Factory] attribute)
public class EmployeeQueryService : IEmployeeQueryService
{
    private readonly List<EmployeeDto> _employees = new()
    {
        new EmployeeDto { Id = 1, Name = "John Doe", Department = "Engineering" },
        new EmployeeDto { Id = 2, Name = "Jane Smith", Department = "Marketing" }
    };

    public Task<IReadOnlyList<EmployeeDto>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<EmployeeDto>>(_employees);
    }

    public Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var employee = _employees.FirstOrDefault(e => e.Id == id);
        return Task.FromResult(employee);
    }

    public Task<int> CountAsync()
    {
        return Task.FromResult(_employees.Count);
    }
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}
#endregion
