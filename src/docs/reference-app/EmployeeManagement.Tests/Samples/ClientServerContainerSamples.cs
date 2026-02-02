using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using System.Text.Json;

namespace EmployeeManagement.Tests.Samples;

/// <summary>
/// Creates isolated client, server, and local containers for integration testing.
/// </summary>
public static class ClientServerContainers
{
    #region clientserver-container-setup
    // Three containers: client (Remote), server (Server), local (Logical)
    public static (IServiceScope server, IServiceScope client, IServiceScope local) Scopes()
    {
        var options = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };
        var serverCollection = new ServiceCollection();
        var clientCollection = new ServiceCollection();
        var localCollection = new ServiceCollection();

        // Configure each container with appropriate mode
        serverCollection.AddNeatooRemoteFactory(NeatooFactory.Server, options, typeof(Employee).Assembly);
        clientCollection.AddNeatooRemoteFactory(NeatooFactory.Remote, options, typeof(Employee).Assembly);
        localCollection.AddNeatooRemoteFactory(NeatooFactory.Logical, options, typeof(Employee).Assembly);

        // Server/local get infrastructure; client gets server reference
        serverCollection.AddInfrastructureServices();
        localCollection.AddInfrastructureServices();
        clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>();

        // ... build providers, create scopes, link client to server
        #endregion

        serverCollection.AddSingleton<IHostApplicationLifetime, TestHostLifetime>();
        clientCollection.AddSingleton<IHostApplicationLifetime, TestHostLifetime>();
        localCollection.AddSingleton<IHostApplicationLifetime, TestHostLifetime>();
        clientCollection.AddScoped<ServerServiceProvider>();

        var serverProvider = serverCollection.BuildServiceProvider();
        var clientProvider = clientCollection.BuildServiceProvider();
        var localProvider = localCollection.BuildServiceProvider();

        var serverScope = serverProvider.CreateScope();
        var clientScope = clientProvider.CreateScope();
        var localScope = localProvider.CreateScope();

        var serverRef = clientScope.ServiceProvider.GetRequiredService<ServerServiceProvider>();
        serverRef.ServerProvider = serverScope.ServiceProvider;

        return (serverScope, clientScope, localScope);
    }
}

/// <summary>
/// Example tests using the ClientServerContainers pattern.
/// </summary>
public class ClientServerContainerTests
{
    #region clientserver-container-usage
    [Fact]
    public void Local_Create_WorksWithoutSerialization()
    {
        var (server, client, local) = ClientServerContainers.Scopes();
        var factory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();
        var employee = factory.Create();  // Runs locally (Logical mode)
        Assert.NotNull(employee);
        server.Dispose(); client.Dispose(); local.Dispose();
    }
    #endregion
}

#region clientserver-infrastructure
/// <summary>
/// Holds reference to the server provider for client-to-server communication.
/// </summary>
public class ServerServiceProvider
{
    public IServiceProvider ServerProvider { get; set; } = null!;
}

/// <summary>
/// Simulates client-to-server remote calls with JSON serialization.
/// </summary>
internal sealed class MakeSerializedServerStandinDelegateRequest : IMakeRemoteDelegateRequest
{
    private readonly INeatooJsonSerializer _neatooJsonSerializer;
    private readonly IServiceProvider _serviceProvider;

    public MakeSerializedServerStandinDelegateRequest(
        INeatooJsonSerializer neatooJsonSerializer,
        IServiceProvider serviceProvider)
    {
        _neatooJsonSerializer = neatooJsonSerializer;
        _serviceProvider = serviceProvider;
    }

    public async Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
    {
        var result = await ForDelegateNullable<T>(delegateType, parameters, cancellationToken);
        if (result == null)
        {
            throw new InvalidOperationException("The result of the remote delegate call was null.");
        }
        return result;
    }

    public async Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Serialize the request (client side)
        var remoteRequest = _neatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);
        var json = JsonSerializer.Serialize(remoteRequest);
        var remoteRequestOnServer = JsonSerializer.Deserialize<RemoteRequestDto>(json)!;

        // Execute on server (simulated)
        var remoteResponseOnServer = await _serviceProvider
            .GetRequiredService<ServerServiceProvider>()
            .ServerProvider
            .GetRequiredService<HandleRemoteDelegateRequest>()(remoteRequestOnServer, cancellationToken);

        // Deserialize the response
        json = JsonSerializer.Serialize(remoteResponseOnServer);
        var result = JsonSerializer.Deserialize<RemoteResponseDto>(json);

        return _neatooJsonSerializer.DeserializeRemoteResponse<T>(result!);
    }

    public async Task ForDelegateEvent(Type delegateType, object?[]? parameters)
    {
        var remoteRequest = _neatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);
        var json = JsonSerializer.Serialize(remoteRequest);
        var remoteRequestOnServer = JsonSerializer.Deserialize<RemoteRequestDto>(json)!;

        await _serviceProvider
            .GetRequiredService<ServerServiceProvider>()
            .ServerProvider
            .GetRequiredService<HandleRemoteDelegateRequest>()(remoteRequestOnServer, default);
    }
}

/// <summary>
/// Simple IHostApplicationLifetime for testing.
/// </summary>
internal sealed class TestHostLifetime : IHostApplicationLifetime, IDisposable
{
    private readonly CancellationTokenSource _startedSource = new();
    private readonly CancellationTokenSource _stoppingSource = new();
    private readonly CancellationTokenSource _stoppedSource = new();

    public CancellationToken ApplicationStarted => _startedSource.Token;
    public CancellationToken ApplicationStopping => _stoppingSource.Token;
    public CancellationToken ApplicationStopped => _stoppedSource.Token;

    public void StopApplication() => _stoppingSource.Cancel();

    public void Dispose()
    {
        _startedSource.Dispose();
        _stoppingSource.Dispose();
        _stoppedSource.Dispose();
    }
}
#endregion
