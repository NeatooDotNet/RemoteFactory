using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Execute;

/// <summary>
/// What the auth class below decides from, set per test container.
/// </summary>
public sealed class StaticCommandAuthSwitch
{
    public bool Allow { get; set; } = true;
}

/// <summary>
/// Authorization for <see cref="StaticAuthorizeFactoryCommands"/>: a parameterless check and a
/// type-matched one that refuses with a reason.
/// </summary>
public interface IStaticCommandAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool CanRun();

    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    string? CheckInput(string input);
}

public sealed class StaticCommandAuth(StaticCommandAuthSwitch authSwitch) : IStaticCommandAuth
{
    public bool CanRun() => authSwitch.Allow;

    public string? CheckInput(string input) => input == "blocked" ? "Input is blocked" : null;
}

/// <summary>
/// Static-factory [Execute] commands under [AuthorizeFactory&lt;T&gt;]: the auth class's methods are
/// checked where the command runs, before the method. Each method records that it ran, so a
/// refusal is visible as the absence of the record.
/// </summary>
[Factory]
[AuthorizeFactory<IStaticCommandAuth>]
public static partial class StaticAuthorizeFactoryCommands
{
    [Remote, Execute]
    private static Task<string> _RunWithResult(string input, [Service] IVoidExecuteRecorder recorder)
    {
        recorder.Record($"result:{input}");
        return Task.FromResult($"ran: {input}");
    }

    [Remote, Execute]
    private static Task _RunNoResult(string input, [Service] IVoidExecuteRecorder recorder)
    {
        recorder.Record($"no-result:{input}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// No [Remote], and public auth methods: the command and its checks run on the tier that
    /// resolves it.
    /// </summary>
    [Execute]
    private static Task<string> _RunLocal(string input, [Service] IVoidExecuteRecorder recorder)
    {
        recorder.Record($"local:{input}");
        return Task.FromResult($"local: {input}");
    }
}
