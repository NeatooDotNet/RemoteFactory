using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-remote
/// <summary>
/// Employee aggregate demonstrating [Remote] vs local execution.
/// </summary>
[Factory]
public partial class EmployeeWithRemote
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";

    [Create]
    public EmployeeWithRemote()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// [Remote, Fetch] - Serialized HTTP call to server.
    /// Method executes on the server with access to server-side services.
    /// </summary>
    [Remote, Fetch]
    public async Task FetchFromDatabase(
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
            Email = entity.Email;
        }
    }

    /// <summary>
    /// [Fetch] without [Remote] - Local execution only.
    /// No serialization, no HTTP call. Uses only local data.
    /// </summary>
    [Fetch]
    public void FetchFromCache(Guid id)
    {
        // In a real scenario, this would read from a local cache
        Id = id;
        FirstName = "Cached";
        LastName = "Employee";
        Email = "cached@example.com";
    }
}
#endregion
