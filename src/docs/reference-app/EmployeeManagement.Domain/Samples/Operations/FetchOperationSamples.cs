using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Operations;

#region operations-fetch-instance
/// <summary>
/// Employee aggregate with instance Fetch method.
/// </summary>
[Factory]
public partial class EmployeeFetchInstance
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public decimal Salary { get; private set; }
    public bool IsNew { get; private set; } = true;

    [Create]
    public EmployeeFetchInstance()
    {
    }

    /// <summary>
    /// Fetches an employee by ID from the repository.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null)
            throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        Id = entity.Id;
        EmployeeNumber = $"EMP-{entity.Id.ToString()[..8].ToUpperInvariant()}";
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Salary = entity.SalaryAmount;
        IsNew = false;
    }
}
#endregion

#region operations-fetch-bool-return
/// <summary>
/// Employee aggregate with bool-returning Fetch for nullable factory return.
/// </summary>
[Factory]
public partial class EmployeeFetchBoolReturn
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";

    [Create]
    public EmployeeFetchBoolReturn()
    {
    }

    /// <summary>
    /// Attempts to fetch an employee by ID.
    /// Returns false if not found (factory returns null).
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> TryFetch(Guid employeeId, [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null)
            return false;

        Id = entity.Id;
        EmployeeNumber = $"EMP-{entity.Id.ToString()[..8].ToUpperInvariant()}";
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        return true;
    }
}
#endregion
