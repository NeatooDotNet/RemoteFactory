using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Operations;

#region operations-cancellation
/// <summary>
/// Employee aggregate demonstrating CancellationToken usage.
/// </summary>
[Factory]
public partial class EmployeeCancellation
{
    public Guid Id { get; private set; }
    public bool Completed { get; private set; }

    [Create]
    public EmployeeCancellation()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an employee with proper CancellationToken handling.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken cancellationToken)
    {
        // Check cancellation before starting
        cancellationToken.ThrowIfCancellationRequested();

        // Pass token to async repository call
        var entity = await repository.GetByIdAsync(id, cancellationToken);

        // Check cancellation during processing
        if (cancellationToken.IsCancellationRequested)
            return;

        if (entity != null)
        {
            Id = entity.Id;
            Completed = true;
        }
    }
}
#endregion

#region operations-params-value
/// <summary>
/// Employee aggregate demonstrating value parameter types.
/// </summary>
[Factory]
public partial class EmployeeParamsValue
{
    public int YearsOfService { get; private set; }
    public string Department { get; private set; } = "";
    public DateTime ReviewDate { get; private set; }
    public decimal BonusAmount { get; private set; }

    [Create]
    public EmployeeParamsValue()
    {
    }

    /// <summary>
    /// Fetches employee data using various serializable value parameter types.
    /// </summary>
    [Remote, Fetch]
    public Task Fetch(int yearsOfService, string department, DateTime reviewDate, decimal bonusAmount)
    {
        YearsOfService = yearsOfService;
        Department = department;
        ReviewDate = reviewDate;
        BonusAmount = bonusAmount;
        return Task.CompletedTask;
    }
}
#endregion

#region operations-params-service
/// <summary>
/// Employee aggregate demonstrating service parameter injection.
/// </summary>
[Factory]
public partial class EmployeeParamsService
{
    public bool ServicesInjected { get; private set; }

    [Create]
    public EmployeeParamsService()
    {
    }

    /// <summary>
    /// Fetches data with multiple injected services.
    /// Services are resolved from DI container on server.
    /// </summary>
    [Remote, Fetch]
    public Task Fetch(
        Guid id,
        [Service] IEmployeeRepository employeeRepo,
        [Service] IDepartmentRepository departmentRepo,
        [Service] IUserContext userContext)
    {
        // Services are resolved from DI container on server
        ServicesInjected = employeeRepo != null && departmentRepo != null && userContext != null;
        return Task.CompletedTask;
    }
}
#endregion

#region operations-params-cancellation
/// <summary>
/// Employee aggregate demonstrating optional CancellationToken parameter.
/// </summary>
[Factory]
public partial class EmployeeParamsCancellation
{
    public bool Completed { get; private set; }

    [Create]
    public EmployeeParamsCancellation()
    {
    }

    /// <summary>
    /// Fetches data with optional CancellationToken.
    /// CancellationToken is optional - receives default if not provided by caller.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken cancellationToken = default)
    {
        await repository.GetByIdAsync(id, cancellationToken);
        Completed = true;
    }
}
#endregion
