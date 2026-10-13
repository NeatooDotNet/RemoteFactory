using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Execute;

/// <summary>
/// Static-factory [Execute] commands under [AspAuthorize] (#91): the policy is checked on the
/// server, before the method runs. Each method records that it ran, so a refusal is visible as
/// the absence of the record.
/// </summary>
[Factory]
public static partial class StaticAspAuthorizeCommands
{
    [Remote, Execute]
    [AspAuthorize("CommandPolicy")]
    private static Task<string> _RunWithResult(string input, [Service] IServerOnlyService service, [Service] IVoidExecuteRecorder recorder)
    {
        recorder.Record($"result:{input}");
        return Task.FromResult($"{service.ServerOnlyValue}: {input}");
    }

    [Remote, Execute]
    [AspAuthorize("CommandPolicy")]
    private static Task _RunNoResult(string input, [Service] IVoidExecuteRecorder recorder)
    {
        recorder.Record($"no-result:{input}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// No [Remote]: [AspAuthorize] forces the call to the server, where the check runs.
    /// </summary>
    [Execute]
    [AspAuthorize("CommandPolicy")]
    [AspAuthorize(Roles = "Admin")]
    private static Task<string> _RunBare(string input, [Service] IVoidExecuteRecorder recorder)
    {
        recorder.Record($"bare:{input}");
        return Task.FromResult($"bare: {input}");
    }
}
