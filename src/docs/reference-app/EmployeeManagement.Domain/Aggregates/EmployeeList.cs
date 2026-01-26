using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.ValueObjects;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Aggregates;

/// <summary>
/// Read-only list of employees for display purposes.
/// </summary>
[Factory]
public partial class EmployeeList
{
    public IReadOnlyList<EmployeeListItem> Items { get; private set; } = Array.Empty<EmployeeListItem>();

    [Create]
    public EmployeeList() { }

    /// <summary>
    /// Fetches all employees.
    /// </summary>
    [Remote, Fetch]
    public async Task FetchAll([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entities = await repository.GetAllAsync(ct);
        Items = entities.Select(MapToItem).ToList();
    }

    /// <summary>
    /// Fetches employees by department.
    /// </summary>
    [Remote, Fetch]
    public async Task FetchByDepartment(Guid departmentId, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entities = await repository.GetByDepartmentIdAsync(departmentId, ct);
        Items = entities.Select(MapToItem).ToList();
    }

    private static EmployeeListItem MapToItem(EmployeeEntity entity) => new()
    {
        Id = entity.Id,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        Email = entity.Email,
        Position = entity.Position,
        DepartmentId = entity.DepartmentId
    };
}

/// <summary>
/// Lightweight employee data for list display.
/// </summary>
[Factory]
public partial class EmployeeListItem
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Position { get; set; } = "";
    public Guid DepartmentId { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    [Create]
    public EmployeeListItem() { }
}
