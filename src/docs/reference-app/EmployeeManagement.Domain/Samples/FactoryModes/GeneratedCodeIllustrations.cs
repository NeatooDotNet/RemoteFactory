using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.FactoryModes;

#region modes-full-generated
// Conceptual illustration of what the generator produces in Full mode.
// This is a simplified representation - actual generated code is more complex.
//
// public interface IEmployeeFactory
// {
//     IEmployee Create();
//     Task<IEmployee?> Fetch(Guid id);
//     Task Save(IEmployee employee);
// }
//
// public class EmployeeFactory : IEmployeeFactory
// {
//     private readonly IServiceProvider ServiceProvider;
//     private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
//
//     public IEmployee Create() => new Employee();
//
//     public async Task<IEmployee?> Fetch(Guid id)
//     {
//         // Dual execution path based on runtime mode:
//         // - If IMakeRemoteDelegateRequest is registered (Remote mode):
//         //   serialize request, POST to /api/neatoo, deserialize response
//         // - Otherwise (Server/Logical mode):
//         //   execute directly using injected repository
//         if (MakeRemoteDelegateRequest != null)
//             return await callRemoteFetch(id);
//         return await localFetchDelegate(id);
//     }
//
//     public async Task Save(IEmployee employee)
//     {
//         // Same dual-path pattern for save operations
//         if (MakeRemoteDelegateRequest != null)
//             await callRemoteSave(employee);
//         else
//             await localSaveDelegate(employee);
//     }
//
//     // Static method for handling incoming HTTP requests (Server mode)
//     public static void RegisterRemoteDelegates(HandleRemoteDelegateRequest handler)
//     {
//         // Registers handlers for incoming serialized requests
//         handler.Register("Fetch", (payload) => ...);
//         handler.Register("Save", (payload) => ...);
//     }
// }
public static class FullModeGeneratedCodeIllustration
{
    // This class exists only to hold the region for documentation.
    // See the comments above for the conceptual generated code pattern.
}
#endregion

#region modes-remoteonly-generated
// Conceptual illustration of what the generator produces in RemoteOnly mode.
// No local implementation code - HTTP stubs only.
//
// public class EmployeeFactory : IEmployeeFactory
// {
//     private readonly IServiceProvider ServiceProvider;
//     private readonly IMakeRemoteDelegateRequest MakeRemoteDelegateRequest;
//
//     // Benefits of RemoteOnly mode:
//     // - Smaller assembly size (no entity method code)
//     // - No server dependencies in client bundle
//     // - Clear separation of client and server code
//     // - Faster client startup (less code to load)
//
//     public IEmployee Create() => callRemoteCreate();
//
//     public async Task<IEmployee?> Fetch(Guid id)
//     {
//         // ALL methods serialize and POST to server
//         // No local execution path available
//         return await callRemoteFetch(id);
//     }
//
//     public async Task Save(IEmployee employee)
//     {
//         // No local execution path available
//         await callRemoteSave(employee);
//     }
//
//     // No RegisterRemoteDelegates method
//     // RemoteOnly mode doesn't handle incoming HTTP requests
// }
public static class RemoteOnlyModeGeneratedCodeIllustration
{
    // This class exists only to hold the region for documentation.
    // See the comments above for the conceptual generated code pattern.
}
#endregion

/// <summary>
/// Interface for mode demonstration entity.
/// </summary>
public interface IEmployeeModeDemo
{
    Guid Id { get; }
    string FirstName { get; set; }
    string LastName { get; set; }
    string LocalComputedValue { get; }
    string? ServerLoadedData { get; }
}

#region modes-local-remote-methods
/// <summary>
/// Employee entity demonstrating mixed local and remote method execution.
/// </summary>
[Factory]
public partial class EmployeeModeDemo : IEmployeeModeDemo
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string LocalComputedValue { get; private set; } = "";
    public string? ServerLoadedData { get; private set; }

    [Create]
    public EmployeeModeDemo()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Local-only method - executes on client/server directly.
    /// No [Remote] attribute means this never goes over HTTP.
    /// </summary>
    [Fetch]
    public void FetchLocalComputed(string computedInput)
    {
        // This method runs locally regardless of mode
        // Use for client-side calculations or local data
        LocalComputedValue = $"Computed: {computedInput}";
    }

    /// <summary>
    /// Remote method - serializes and executes on server.
    /// The [Remote] attribute means this can be called over HTTP.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> FetchFromServer(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        // This method executes on server (or locally in Logical mode)
        // Server-only services are injected via [Service] attribute
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        ServerLoadedData = $"Loaded from server at {DateTime.UtcNow:O}";
        return true;
    }
}
#endregion
