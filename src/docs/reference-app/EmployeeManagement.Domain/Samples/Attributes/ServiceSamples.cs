using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-service
/// <summary>
/// Employee aggregate demonstrating [Service] parameter injection.
/// </summary>
[Factory]
public partial class EmployeeWithService
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FetchedBy { get; private set; } = "";

    [Create]
    public EmployeeWithService()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Demonstrates both value and service parameters.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid employeeId,                        // Value parameter - passed by caller
        [Service] IEmployeeRepository repository, // Service - injected from DI
        [Service] IUserContext userContext,       // Service - injected from DI
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
        }

        FetchedBy = userContext.Username;
    }
}
#endregion
