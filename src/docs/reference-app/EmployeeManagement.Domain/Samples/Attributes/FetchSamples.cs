using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-fetch
/// <summary>
/// Employee aggregate with multiple [Fetch] methods.
/// </summary>
[Factory]
public partial class EmployeeWithFetch
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; private set; } = "";

    [Create]
    public EmployeeWithFetch()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches employee by primary key.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
            Email = entity.Email;
        }
    }

    /// <summary>
    /// Fetches employee by unique email address.
    /// </summary>
    [Remote, Fetch]
    public async Task FetchByEmail(
        string email,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var employees = await repository.GetAllAsync(ct);
        var entity = employees.FirstOrDefault(e =>
            e.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

        if (entity != null)
        {
            Id = entity.Id;
            FirstName = entity.FirstName;
            LastName = entity.LastName;
            Email = entity.Email;
        }
    }
}
#endregion
