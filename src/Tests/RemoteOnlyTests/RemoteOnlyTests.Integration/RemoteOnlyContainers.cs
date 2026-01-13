extern alias ClientAssembly;
extern alias ServerAssembly;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using System.Text.Json;

// Type aliases for clarity
using ClientFactory = ClientAssembly::RemoteOnlyTests.Domain.TestAggregateFactory;
using ServerFactory = ServerAssembly::RemoteOnlyTests.Domain.TestAggregateFactory;
using ServerDataStore = ServerAssembly::RemoteOnlyTests.Domain.ITestDataStore;
using ServerDataStoreImpl = ServerAssembly::RemoteOnlyTests.Server.TestDataStore;

namespace RemoteOnlyTests.Integration;

/// <summary>
/// Holds a reference to the server's service provider.
/// Used to route "remote" calls from client to server.
/// </summary>
public class ServerServiceProvider
{
    public IServiceProvider ServerProvider { get; set; } = null!;
}

/// <summary>
/// Test implementation of IHostApplicationLifetime.
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

/// <summary>
/// Simulates remote HTTP calls by serializing through JSON and executing on the server container.
/// </summary>
internal sealed class MakeSerializedServerRequest : IMakeRemoteDelegateRequest
{
    private readonly INeatooJsonSerializer _serializer;
    private readonly IServiceProvider _serviceProvider;

    public MakeSerializedServerRequest(INeatooJsonSerializer serializer, IServiceProvider serviceProvider)
    {
        _serializer = serializer;
        _serviceProvider = serviceProvider;
    }

    public async Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
    {
        var result = await ForDelegateNullable<T>(delegateType, parameters, cancellationToken);
        if (result == null)
        {
            throw new InvalidOperationException("Remote call returned null but non-nullable type expected.");
        }
        return result;
    }

    public async Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Serialize the request (client side)
        var remoteRequest = _serializer.ToRemoteDelegateRequest(delegateType, parameters);
        var json = JsonSerializer.Serialize(remoteRequest);
        var requestOnServer = JsonSerializer.Deserialize<RemoteRequestDto>(json)!;

        // Execute on server container
        var serverProvider = _serviceProvider.GetRequiredService<ServerServiceProvider>().ServerProvider;
        var handler = serverProvider.GetRequiredService<HandleRemoteDelegateRequest>();
        var responseOnServer = await handler(requestOnServer, cancellationToken);

        // Serialize response back (server to client)
        json = JsonSerializer.Serialize(responseOnServer);
        var response = JsonSerializer.Deserialize<RemoteResponseDto>(json)!;

        return _serializer.DeserializeRemoteResponse<T>(response);
    }

    public async Task ForDelegateEvent(Type delegateType, object?[]? parameters)
    {
        var remoteRequest = _serializer.ToRemoteDelegateRequest(delegateType, parameters);
        var json = JsonSerializer.Serialize(remoteRequest);
        var requestOnServer = JsonSerializer.Deserialize<RemoteRequestDto>(json)!;

        var serverProvider = _serviceProvider.GetRequiredService<ServerServiceProvider>().ServerProvider;
        var handler = serverProvider.GetRequiredService<HandleRemoteDelegateRequest>();
        await handler(requestOnServer, default);
    }
}

/// <summary>
/// Sets up two DI containers for testing RemoteOnly mode:
/// - Client: Uses RemoteOnlyTests.Client assembly (FactoryMode.RemoteOnly)
/// - Server: Uses RemoteOnlyTests.Server assembly (FactoryMode.Full)
///
/// This validates that a RemoteOnly-compiled client can call a Full-compiled server.
/// </summary>
public static class RemoteOnlyContainers
{
    /// <summary>
    /// Creates client and server scopes using different assemblies.
    /// </summary>
    public static (IServiceScope Client, IServiceScope Server) Scopes()
    {
        var serializationOptions = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };

        // === CLIENT CONTAINER (RemoteOnly mode) ===
        var clientServices = new ServiceCollection();
        clientServices.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        clientServices.AddSingleton<IHostApplicationLifetime, TestHostApplicationLifetime>();

        // Register from CLIENT assembly - these factories have RemoteOnly mode
        clientServices.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            serializationOptions,
            typeof(ClientFactory).Assembly);

        // Wire up remote call handling
        clientServices.AddScoped<ServerServiceProvider>();
        clientServices.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerRequest>();

        // === SERVER CONTAINER (Full mode) ===
        var serverServices = new ServiceCollection();
        serverServices.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        serverServices.AddSingleton<IHostApplicationLifetime, TestHostApplicationLifetime>();

        // Register from SERVER assembly - these factories have Full mode
        serverServices.AddNeatooRemoteFactory(
            NeatooFactory.Server,
            serializationOptions,
            typeof(ServerFactory).Assembly);

        // Server-only services
        serverServices.AddSingleton<ServerDataStore, ServerDataStoreImpl>();

        // Build providers
        var clientProvider = clientServices.BuildServiceProvider();
        var serverProvider = serverServices.BuildServiceProvider();

        // Create scopes
        var clientScope = clientProvider.CreateScope();
        var serverScope = serverProvider.CreateScope();

        // Connect client to server
        clientScope.ServiceProvider.GetRequiredService<ServerServiceProvider>().ServerProvider = serverScope.ServiceProvider;

        return (clientScope, serverScope);
    }
}
