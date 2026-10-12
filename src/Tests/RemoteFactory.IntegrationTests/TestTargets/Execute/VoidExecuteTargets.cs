using System.Collections.Concurrent;
using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Execute;

/// <summary>
/// Records what a result-less command did, so a test can see the call reached the tier that
/// owns the service. Registered as a singleton on the server and local containers only.
/// </summary>
public interface IVoidExecuteRecorder
{
    void Record(string value);
    IReadOnlyCollection<string> Recorded { get; }
}

public sealed class VoidExecuteRecorder : IVoidExecuteRecorder
{
    private readonly ConcurrentQueue<string> _recorded = new();

    public void Record(string value) => _recorded.Enqueue(value);

    public IReadOnlyCollection<string> Recorded => _recorded.ToArray();
}

/// <summary>
/// Static [Execute] methods returning non-generic Task (#113): commands with no result.
/// </summary>
[Factory]
public static partial class VoidExecuteCommands
{
    [Remote, Execute]
    private static Task _RecordRemote(string value, [Service] IVoidExecuteRecorder recorder)
    {
        recorder.Record(value);
        return Task.CompletedTask;
    }

    [Remote, Execute]
    private static async Task _RecordRemoteAsync(string value, [Service] IVoidExecuteRecorder recorder, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        recorder.Record(value);
    }

    [Remote, Execute]
    private static async Task _FailRemote(string message)
    {
        await Task.Yield();
        throw new InvalidOperationException(message);
    }

    [Execute]
    private static Task _RecordLocal(string value, [Service] IVoidExecuteRecorder recorder)
    {
        recorder.Record(value);
        return Task.CompletedTask;
    }
}
