using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.FactoryModes;

// Minimal generated code illustrations as comments

#region modes-full-generated
// Full mode: local methods + remote handlers (dual-path execution)
// public Task<IEmployee?> Fetch(Guid id) {
//     if (MakeRemoteDelegateRequest != null) return callRemoteFetch(id);
//     return localFetchDelegate(id);  // Server/Logical: execute locally
// }
// public static void RegisterRemoteDelegates(...)  // Server: handles incoming HTTP
#endregion

#region modes-remoteonly-generated
// RemoteOnly mode: HTTP stubs only (no local implementation)
// public Task<IEmployee?> Fetch(Guid id) => callRemoteFetch(id);  // Always HTTP
// No RegisterRemoteDelegates - client doesn't handle incoming requests
// Benefits: smaller assembly, no server dependencies in client bundle
#endregion

// Supporting interface for mode demonstration
public interface IEmployeeModeDemo
{
    Guid Id { get; }
    string FirstName { get; set; }
    string LastName { get; set; }
    string LocalComputedValue { get; }
    string? ServerLoadedData { get; }
}

// Supporting class for local/remote method demonstration (full class not shown in docs)
[Factory]
public partial class EmployeeModeDemo : IEmployeeModeDemo
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string LocalComputedValue { get; private set; } = "";
    public string? ServerLoadedData { get; private set; }

    [Create]
    public EmployeeModeDemo() => Id = Guid.NewGuid();

    [Fetch]
    public void FetchLocalComputed(string computedInput)
    {
        LocalComputedValue = $"Computed: {computedInput}";
    }

    [Remote, Fetch]
    public async Task<bool> FetchFromServer(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        ServerLoadedData = $"Loaded from server at {DateTime.UtcNow:O}";
        return true;
    }
}
