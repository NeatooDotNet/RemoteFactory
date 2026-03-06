namespace RemoteFactory.TrimmingTests;

/// <summary>
/// Server-only interface simulating a repository (like an EF Core DbContext or repository).
/// If trimming works correctly, this type should be absent from the published output
/// when NeatooRuntime.IsServerRuntime is set to false.
/// </summary>
public interface IServerOnlyRepository
{
    string DoServerWork(string input);
}

/// <summary>
/// Server-only implementation. Contains a distinctive string constant
/// ("ServerOnlyRepository_MARKER") that can be searched for in the trimmed output.
/// </summary>
public class ServerOnlyRepository : IServerOnlyRepository
{
    public string DoServerWork(string input)
    {
        return "ServerOnlyRepository_MARKER: " + input;
    }
}

/// <summary>
/// Another server-only type to verify transitive dependency removal.
/// This is used by ServerOnlyRepository to test whether transitive types are also trimmed.
/// </summary>
public class ServerOnlyHelper
{
    public static string HelperMarker => "ServerOnlyHelper_MARKER";

    public string ProcessData(string data)
    {
        return HelperMarker + ": " + data;
    }
}
