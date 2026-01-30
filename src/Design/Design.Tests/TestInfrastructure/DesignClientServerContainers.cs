// =============================================================================
// DESIGN SOURCE OF TRUTH: Testing Infrastructure
// =============================================================================
//
// This file demonstrates the ClientServerContainers pattern for testing
// RemoteFactory applications without HTTP.
//
// DESIGN DECISION: Use container simulation instead of HTTP testing
//
// Reasons:
// 1. Faster - no network overhead
// 2. Deterministic - no timing issues
// 3. Validates serialization - JSON round-trip still happens
// 4. Isolated - no external dependencies
//
// The pattern: Create separate DI containers for client and server, wire
// them together with a custom IMakeRemoteDelegateRequest that serializes
// through JSON to simulate the actual boundary.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.FactoryPatterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using System.Reflection;
using System.Text.Json;

namespace Design.Tests.TestInfrastructure;

/// <summary>
/// Holds reference to the server provider for client-to-server communication.
/// </summary>
/// <remarks>
/// GENERATOR BEHAVIOR: The client's IMakeRemoteDelegateRequest implementation
/// uses this to find the server's DI container and resolve handlers.
/// </remarks>
public class ServerServiceProvider
{
    public IServiceProvider ServerProvider { get; set; } = null!;
}

/// <summary>
/// Simulates client-to-server remote calls with JSON serialization.
/// </summary>
/// <remarks>
/// DESIGN DECISION: This implements the same interface that the HTTP client uses.
/// Tests validate the full serialization round-trip without HTTP overhead.
///
/// DID NOT DO THIS: Mock the serialization
///
/// Reasons:
/// 1. Serialization bugs are common - we want to catch them
/// 2. The actual HTTP path uses the same serializer
/// 3. Minimal overhead for maximum confidence
///
/// The rule: Test the actual serialization, just skip the HTTP.
/// </remarks>
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
            throw new InvalidOperationException("The result of the remote delegate call was null, but a non-nullable type was expected.");
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
internal class TestHostApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _startedSource = new();
    private readonly CancellationTokenSource _stoppingSource = new();
    private readonly CancellationTokenSource _stoppedSource = new();

    public CancellationToken ApplicationStarted => _startedSource.Token;
    public CancellationToken ApplicationStopping => _stoppingSource.Token;
    public CancellationToken ApplicationStopped => _stoppedSource.Token;

    public void StopApplication()
    {
        _stoppingSource.Cancel();
        _stoppedSource.Cancel();
    }
}

/// <summary>
/// Creates isolated client, server, and local containers for integration testing.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Three containers - client, server, and local
///
/// - Client (NeatooFactory.Remote): Has remote stubs, calls server via serialization
/// - Server (NeatooFactory.Server): Has full implementations, handles remote requests
/// - Local (NeatooFactory.Logical): Full implementation, no remote calls (single-tier)
///
/// Tests should cover all three modes to ensure behavior is consistent.
/// </remarks>
public static class DesignClientServerContainers
{
    private static readonly object LockContainer = new();

    /// <summary>
    /// Creates scopes for testing client-server communication.
    /// </summary>
    /// <remarks>
    /// COMMON MISTAKE: Forgetting to link client and server scopes
    ///
    /// The client scope needs a reference to the server scope for remote calls.
    /// This is done via ServerServiceProvider.
    ///
    /// Usage:
    ///   var (server, client, local) = DesignClientServerContainers.Scopes();
    ///   var factory = client.GetRequiredService<IExampleClassFactory>();
    ///   var result = await factory.Create("test");  // Calls server through serialization
    /// </remarks>
    public static (IServiceScope server, IServiceScope client, IServiceScope local) Scopes(
        Action<IServiceCollection>? configureServer = null,
        Action<IServiceCollection>? configureClient = null)
    {
        var serializationOptions = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };

        var serverCollection = new ServiceCollection();
        var clientCollection = new ServiceCollection();
        var localCollection = new ServiceCollection();

        // Add base configuration
        ConfigureContainer(serverCollection, NeatooFactory.Server, serializationOptions);
        ConfigureContainer(clientCollection, NeatooFactory.Remote, serializationOptions);
        ConfigureContainer(localCollection, NeatooFactory.Logical, serializationOptions);

        // Server-only services
        serverCollection.AddScoped<IExampleService, ExampleService>();
        serverCollection.AddScoped<INotificationService, NotificationService>();
        serverCollection.AddScoped<IOrderRepository, InMemoryOrderRepository>();
        localCollection.AddScoped<IExampleService, ExampleService>();
        localCollection.AddScoped<INotificationService, NotificationService>();
        localCollection.AddScoped<IOrderRepository, InMemoryOrderRepository>();

        // Client needs server provider reference for remote calls
        clientCollection.AddScoped<ServerServiceProvider>();
        clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>();

        // Apply custom configuration
        configureServer?.Invoke(serverCollection);
        configureClient?.Invoke(clientCollection);

        var serverProvider = serverCollection.BuildServiceProvider();
        var clientProvider = clientCollection.BuildServiceProvider();
        var localProvider = localCollection.BuildServiceProvider();

        var serverScope = serverProvider.CreateScope();
        var clientScope = clientProvider.CreateScope();
        var localScope = localProvider.CreateScope();

        // Link client to server
        lock (LockContainer)
        {
            clientScope.ServiceProvider.GetRequiredService<ServerServiceProvider>().ServerProvider = serverScope.ServiceProvider;
        }

        return (serverScope, clientScope, localScope);
    }

    private static void ConfigureContainer(IServiceCollection services, NeatooFactory mode, NeatooSerializationOptions serializationOptions)
    {
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<IHostApplicationLifetime, TestHostApplicationLifetime>();

        // Register factory types
        RegisterFactoryTypes(services);

        // Register interface implementations via convention
        // DESIGN DECISION: RegisterMatchingName auto-maps IFoo → Foo
        // This convention reduces boilerplate while keeping explicit control.
        services.AddNeatooRemoteFactory(mode, serializationOptions, typeof(ExampleClassFactory).Assembly);
        services.RegisterMatchingName(typeof(ExampleClassFactory).Assembly);
    }

    /// <summary>
    /// Registers all types decorated with [Factory] attribute.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Use attribute scanning for registration
    ///
    /// This ensures the same types are registered that the generator sees.
    /// Manual registration could drift from generator output.
    /// </remarks>
    private static void RegisterFactoryTypes(IServiceCollection services)
    {
        foreach (var type in typeof(ExampleClassFactory).Assembly.GetTypes())
        {
            if (type.GenericTypeArguments.Length > 0 || type.IsAbstract || type.IsInterface)
                continue;

            if (type.GetCustomAttribute<FactoryAttribute>() == null)
                continue;

            // Skip static classes (they have abstract sealed)
            if (type.IsAbstract && type.IsSealed)
                continue;

            // Skip records (they have a compiler-generated <Clone>$ method)
            if (type.GetMethod("<Clone>$") != null)
                continue;

            services.AddScoped(type);
        }
    }
}

/// <summary>
/// Extension methods for IServiceScope.
/// </summary>
public static class ServiceScopeExtensions
{
    public static T GetRequiredService<T>(this IServiceScope scope) where T : class
    {
        return scope.ServiceProvider.GetRequiredService<T>();
    }
}
