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
        // Reaches ServerOnlyHelper so the transitive-removal property is genuinely exercised
        // rather than merely asserted — see the remarks on ServerOnlyHelper.
        return "ServerOnlyRepository_MARKER: " + new ServerOnlyHelper().ProcessData(input);
    }
}

/// <summary>
/// Another server-only type, reached only transitively — <see cref="ServerOnlyRepository"/>
/// is its sole caller. Verifies that removing a server-only body also removes the types that
/// body's callees drag in.
/// </summary>
/// <remarks>
/// Its doc comment used to claim exactly this while nothing referenced it at all, so ILLink
/// dropped it unconditionally and its absence proved nothing — it could not have gone red for
/// any defect. It was nevertheless carried in the CI gate under a header asserting every marker
/// there was measured present-before/absent-after. Wired up for real by TRIM-008's test review;
/// the transitive property it names is now actually under test.
/// </remarks>
public class ServerOnlyHelper
{
    public static string HelperMarker => "ServerOnlyHelper_MARKER";

    public string ProcessData(string data)
    {
        return HelperMarker + ": " + data;
    }
}
