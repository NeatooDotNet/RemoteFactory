using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

/// <summary>
/// Authorization interface for department operations (used by this sample).
/// </summary>
public interface IDepartmentAuthorizationSample
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch(Guid departmentId);
}

#region attributes-authorizefactory-method
/// <summary>
/// Department aggregate with class-level and method-level authorization.
/// </summary>
[Factory]
[AuthorizeFactory<IDepartmentAuthorizationSample>]
public partial class DepartmentWithMethodAuth
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public decimal Budget { get; set; }

    [Create]
    public DepartmentWithMethodAuth()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Uses class-level authorization only.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IDepartmentRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            Name = entity.Name;
        }
    }

    /// <summary>
    /// Method-level [AspAuthorize] adds additional authorization check.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize(Roles = "HRManager")]
    public async Task FetchWithSalaryData(
        Guid id,
        [Service] IDepartmentRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            Name = entity.Name;
            Budget = entity.Budget;
        }
    }
}
#endregion
