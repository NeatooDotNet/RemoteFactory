using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-inheritance
/// <summary>
/// Base Employee class with [Factory].
/// </summary>
[Factory]
public partial class EmployeeBaseInherited
{
    public Guid Id { get; protected set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeBaseInherited()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// [Remote] is inherited by derived classes.
    /// </summary>
    [Remote, Fetch]
    public virtual async Task Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
        }
    }
}

/// <summary>
/// Manager inherits [Factory] from base.
/// </summary>
public partial class ManagerInherited : EmployeeBaseInherited
{
    public int DirectReportCount { get; set; }

    /// <summary>
    /// Override with additional data loading.
    /// [Remote] is inherited from base.
    /// </summary>
    [Fetch]
    public override async Task Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        await base.Fetch(id, repository, ct);
        // Load additional manager-specific data
        DirectReportCount = 5; // Would be loaded from repository
    }
}

/// <summary>
/// Contractor with [SuppressFactory] - no factory generated, use EmployeeBaseFactory.
/// </summary>
[SuppressFactory]
public partial class ContractorInherited : EmployeeBaseInherited
{
    public DateTime ContractEndDate { get; set; }
    public string AgencyName { get; set; } = "";
}
#endregion
