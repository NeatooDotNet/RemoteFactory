using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Operations;

#region operations-remote
/// <summary>
/// Employee aggregate demonstrating the [Remote] attribute for server-side execution.
/// </summary>
[Factory]
public partial class EmployeeRemoteDemo
{
    public string Result { get; private set; } = "";

    [Create]
    public EmployeeRemoteDemo()
    {
    }

    /// <summary>
    /// Fetches data from the server.
    /// </summary>
    /// <remarks>
    /// This code runs on the server - the [Remote] attribute marks it as a client entry point.
    /// </remarks>
    [Remote, Fetch]
    public Task FetchFromServer(string query, [Service] IEmployeeRepository repository)
    {
        // This code executes on the server
        Result = $"Server executed query: {query}";
        return Task.CompletedTask;
    }
}
#endregion
