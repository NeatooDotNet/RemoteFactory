using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

/// <summary>
/// Static factory used to test IL trimming of server-only dependencies.
/// Static factories use delegate types (not factory interfaces), which are
/// the original scenario where IL trimming broke factory registration.
/// </summary>
[Factory]
public static partial class TrimTestCommands
{
    [Remote]
    [Execute]
    private static Task<string> _DoWork(string input, [Service] IServerOnlyRepository repo)
    {
        return Task.FromResult(repo.DoServerWork(input));
    }

    [Remote, Event]
    private static async Task _OnWorkCompleted(
        string workId,
        [Service] IServerOnlyRepository repo,
        CancellationToken cancellationToken)
    {
        await Task.Run(() => repo.DoServerWork(workId), cancellationToken);
    }
}
