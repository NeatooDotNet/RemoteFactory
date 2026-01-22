using Microsoft.Extensions.Hosting;

namespace RemoteFactory.IntegrationTests.Shared;

/// <summary>
/// Test service interface for verifying [Service] parameter injection.
/// </summary>
public interface IService
{
}

/// <summary>
/// Default implementation of IService for testing.
/// </summary>
public class Service : IService
{
}

/// <summary>
/// Second service interface for testing multiple service injection.
/// </summary>
public interface IService2
{
    int GetValue();
}

/// <summary>
/// Implementation of IService2.
/// </summary>
public class Service2 : IService2
{
    public int GetValue() => 42;
}

/// <summary>
/// Third service interface for testing multiple service injection.
/// </summary>
public interface IService3
{
    string GetName();
}

/// <summary>
/// Implementation of IService3.
/// </summary>
public class Service3 : IService3
{
    public string GetName() => "Service3";
}

/// <summary>
/// Server-only service interface - only available in Server/Logical modes.
/// </summary>
public interface IServerOnlyService
{
    string ServerOnlyValue { get; }
}

/// <summary>
/// Server-only service implementation.
/// Name intentionally doesn't match IServerOnlyService so RegisterMatchingName won't auto-register it on the client.
/// </summary>
public class ServerOnly : IServerOnlyService
{
    public string ServerOnlyValue => "ServerOnly";
}

/// <summary>
/// Test implementation of IHostApplicationLifetime for event testing.
/// </summary>
internal sealed class TestHostApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _startedSource = new();
    private readonly CancellationTokenSource _stoppingSource = new();
    private readonly CancellationTokenSource _stoppedSource = new();

    public CancellationToken ApplicationStarted => _startedSource.Token;
    public CancellationToken ApplicationStopping => _stoppingSource.Token;
    public CancellationToken ApplicationStopped => _stoppedSource.Token;

    public void StopApplication() => _stoppingSource.Cancel();
}
