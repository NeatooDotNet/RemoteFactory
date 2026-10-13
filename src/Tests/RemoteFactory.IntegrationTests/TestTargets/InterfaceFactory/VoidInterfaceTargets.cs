using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestTargets.Execute;

namespace RemoteFactory.IntegrationTests.TestTargets.InterfaceFactory;

/// <summary>
/// Interface-factory methods returning non-generic <c>Task</c> (#122).
/// </summary>
/// <remarks>
/// Before #122 a <c>Task</c>-returning interface-factory method generated <c>Task&lt;Task&gt;</c>
/// and failed CS0738/CS0266 in generated code, so this interface could not exist.
/// The implementation is registered by <c>RegisterMatchingName</c> (VoidTaskService).
/// </remarks>
[Factory]
public interface IVoidTaskService
{
    Task Record(string message);

    Task RecordAsync(string message, CancellationToken cancellationToken);

    Task Fail(string message);

    Task<int> RecordedCount();
}

public class VoidTaskService : IVoidTaskService
{
    private readonly IVoidExecuteRecorder _recorder;

    public VoidTaskService(IVoidExecuteRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task Record(string message)
    {
        _recorder.Record(message);
        return Task.CompletedTask;
    }

    public async Task RecordAsync(string message, CancellationToken cancellationToken)
    {
        await Task.Delay(20, cancellationToken);
        _recorder.Record(message);
    }

    public async Task Fail(string message)
    {
        await Task.Yield();
        throw new InvalidOperationException(message);
    }

    public Task<int> RecordedCount() => Task.FromResult(_recorder.Recorded.Count);
}

/// <summary>
/// Authorization for <see cref="IAuthorizedVoidTaskService"/>. An async Execute-scope check
/// with a parameter matched by type, so the generated local method is async.
/// </summary>
public interface IVoidTaskAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    Task<bool> CanClose(Guid id);
}

public class VoidTaskAuth : IVoidTaskAuth
{
    public static readonly Guid DeniedId = Guid.Parse("00000000-0000-0000-0000-000000000122");

    public Task<bool> CanClose(Guid id) => Task.FromResult(id != DeniedId);
}

[Factory]
[AuthorizeFactory<IVoidTaskAuth>]
public interface IAuthorizedVoidTaskService
{
    Task Close(Guid id);
}

public class AuthorizedVoidTaskService : IAuthorizedVoidTaskService
{
    private readonly IVoidExecuteRecorder _recorder;

    public AuthorizedVoidTaskService(IVoidExecuteRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task Close(Guid id)
    {
        _recorder.Record($"closed:{id}");
        return Task.CompletedTask;
    }
}
