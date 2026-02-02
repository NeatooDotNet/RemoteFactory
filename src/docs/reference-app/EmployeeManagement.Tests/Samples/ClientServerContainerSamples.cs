using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using System.Text.Json;

namespace EmployeeManagement.Tests.Samples;

#region clientserver-container-setup
/// <summary>
/// Creates isolated client, server, and local containers for integration testing.
/// </summary>
public static class ClientServerContainers
{
    /// <summary>
    /// Creates scopes for testing client-server communication.
    /// </summary>
    /// <remarks>
    /// - Client (NeatooFactory.Remote): Remote stubs, calls server via serialization
    /// - Server (NeatooFactory.Server): Full implementations, handles remote requests
    /// - Local (NeatooFactory.Logical): Full implementation, no remote calls
    /// </remarks>
    public static (IServiceScope server, IServiceScope client, IServiceScope local) Scopes()
    {
        var serializationOptions = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };

        var serverCollection = new ServiceCollection();
        var clientCollection = new ServiceCollection();
        var localCollection = new ServiceCollection();

        // Configure containers with appropriate mode
        serverCollection.AddNeatooRemoteFactory(NeatooFactory.Server, serializationOptions, typeof(Employee).Assembly);
        clientCollection.AddNeatooRemoteFactory(NeatooFactory.Remote, serializationOptions, typeof(Employee).Assembly);
        localCollection.AddNeatooRemoteFactory(NeatooFactory.Logical, serializationOptions, typeof(Employee).Assembly);

        // Server-only services (repositories, etc.)
        serverCollection.AddInfrastructureServices();
        localCollection.AddInfrastructureServices();

        // Both need IHostApplicationLifetime for event tracking
        serverCollection.AddSingleton<IHostApplicationLifetime, TestHostLifetime>();
        clientCollection.AddSingleton<IHostApplicationLifetime, TestHostLifetime>();
        localCollection.AddSingleton<IHostApplicationLifetime, TestHostLifetime>();

        // Client needs server reference for remote calls
        clientCollection.AddScoped<ServerServiceProvider>();
        clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>();

        var serverProvider = serverCollection.BuildServiceProvider();
        var clientProvider = clientCollection.BuildServiceProvider();
        var localProvider = localCollection.BuildServiceProvider();

        var serverScope = serverProvider.CreateScope();
        var clientScope = clientProvider.CreateScope();
        var localScope = localProvider.CreateScope();

        // Link client to server
        var serverRef = clientScope.ServiceProvider.GetRequiredService<ServerServiceProvider>();
        serverRef.ServerProvider = serverScope.ServiceProvider;

        return (serverScope, clientScope, localScope);
    }
}
#endregion

#region clientserver-container-usage
/// <summary>
/// Example tests using the ClientServerContainers pattern.
/// </summary>
public class ClientServerContainerTests
{
    [Fact]
    public void Local_Create_WorksWithoutSerialization()
    {
        var (server, client, local) = ClientServerContainers.Scopes();

        // Get factory from local container - no serialization
        var factory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create runs entirely locally (Logical mode)
        var employee = factory.Create();

        Assert.NotNull(employee);
        Assert.NotEqual(Guid.Empty, employee.Id);
        Assert.True(employee.IsNew);

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }
}
#endregion

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
