using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.RemoteFactory.Internal;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Neatoo.RemoteFactory.Samples.Infrastructure;

/// <summary>
/// Holds reference to the server provider for client-to-server communication.
/// </summary>
public class ServerServiceProvider
{
    public IServiceProvider ServerProvider { get; set; } = null!;
}

/// <summary>
/// Implementation of IMakeRemoteDelegateRequest that serializes through JSON
/// to simulate the client-server boundary without HTTP.
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
/// Creates isolated client, server, and local containers for testing.
/// This simulates the full client-server round-trip with JSON serialization.
/// </summary>
public static class SampleTestContainers
{
    private static readonly object LockContainer = new();
    private static readonly ConcurrentDictionary<SerializationFormat, (IServiceProvider server, IServiceProvider client, IServiceProvider local)> FormatContainers = new();

    /// <summary>
    /// Creates scopes using the Ordinal serialization format (production default).
    /// </summary>
    public static (IServiceScope server, IServiceScope client, IServiceScope local) Scopes()
    {
        return Scopes(SerializationFormat.Ordinal);
    }

    /// <summary>
    /// Creates scopes using the specified serialization format.
    /// </summary>
    public static (IServiceScope server, IServiceScope client, IServiceScope local) Scopes(SerializationFormat format)
    {
        var containers = FormatContainers.GetOrAdd(format, CreateContainers);

        var serverScope = containers.server.CreateScope();
        var clientScope = containers.client.CreateScope();
        var localScope = containers.local.CreateScope();

        lock (LockContainer)
        {
            clientScope.GetRequiredService<ServerServiceProvider>().ServerProvider = serverScope.ServiceProvider;
        }

        return (serverScope, clientScope, localScope);
    }

    /// <summary>
    /// Creates scopes with custom service configuration.
    /// </summary>
    public static (IServiceScope client, IServiceScope server, IServiceScope local) Scopes(
        Action<IServiceCollection>? configureClient = null,
        Action<IServiceCollection>? configureServer = null)
    {
        var serializationOptions = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };

        var serverCollection = new ServiceCollection();
        var clientCollection = new ServiceCollection();
        var localCollection = new ServiceCollection();

        ConfigureContainer(serverCollection, NeatooFactory.Server, serializationOptions);
        ConfigureContainer(clientCollection, NeatooFactory.Remote, serializationOptions);
        ConfigureContainer(localCollection, NeatooFactory.Logical, serializationOptions);

        clientCollection.AddScoped<ServerServiceProvider>();
        clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>();

        configureServer?.Invoke(serverCollection);
        configureClient?.Invoke(clientCollection);

        var serverProvider = serverCollection.BuildServiceProvider();
        var clientProvider = clientCollection.BuildServiceProvider();
        var localProvider = localCollection.BuildServiceProvider();

        var serverScope = serverProvider.CreateScope();
        var clientScope = clientProvider.CreateScope();
        var localScope = localProvider.CreateScope();

        lock (LockContainer)
        {
            clientScope.GetRequiredService<ServerServiceProvider>().ServerProvider = serverScope.ServiceProvider;
        }

        return (clientScope, serverScope, localScope);
    }

    private static void ConfigureContainer(IServiceCollection services, NeatooFactory mode, NeatooSerializationOptions serializationOptions)
    {
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<IHostApplicationLifetime, TestHostApplicationLifetime>();

        RegisterFactoryTypes(services);
        RegisterMockServices(services, mode);

        services.AddNeatooRemoteFactory(mode, serializationOptions, Assembly.GetExecutingAssembly());
        services.RegisterMatchingName(Assembly.GetExecutingAssembly());
    }

    private static (IServiceProvider server, IServiceProvider client, IServiceProvider local) CreateContainers(SerializationFormat format)
    {
        var serializationOptions = new NeatooSerializationOptions { Format = format };

        var serverCollection = new ServiceCollection();
        var clientCollection = new ServiceCollection();
        var localCollection = new ServiceCollection();

        serverCollection.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        serverCollection.AddSingleton<IHostApplicationLifetime, TestHostApplicationLifetime>();
        clientCollection.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        clientCollection.AddSingleton<IHostApplicationLifetime, TestHostApplicationLifetime>();
        localCollection.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        localCollection.AddSingleton<IHostApplicationLifetime, TestHostApplicationLifetime>();

        RegisterFactoryTypes(serverCollection);
        RegisterFactoryTypes(clientCollection);
        RegisterFactoryTypes(localCollection);

        RegisterMockServices(serverCollection, NeatooFactory.Server);
        RegisterMockServices(localCollection, NeatooFactory.Logical);

        serverCollection.AddNeatooRemoteFactory(NeatooFactory.Server, serializationOptions, Assembly.GetExecutingAssembly());
        serverCollection.RegisterMatchingName(Assembly.GetExecutingAssembly());

        clientCollection.AddNeatooRemoteFactory(NeatooFactory.Remote, serializationOptions, Assembly.GetExecutingAssembly());
        clientCollection.AddScoped<ServerServiceProvider>();
        clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>();
        clientCollection.RegisterMatchingName(Assembly.GetExecutingAssembly());

        localCollection.AddNeatooRemoteFactory(NeatooFactory.Logical, serializationOptions, Assembly.GetExecutingAssembly());
        localCollection.RegisterMatchingName(Assembly.GetExecutingAssembly());

        return (
            serverCollection.BuildServiceProvider(),
            clientCollection.BuildServiceProvider(),
            localCollection.BuildServiceProvider()
        );
    }

    private static void RegisterFactoryTypes(IServiceCollection services)
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type.GenericTypeArguments.Length > 0 || type.IsAbstract)
                continue;

            if (type.GetCustomAttribute<FactoryAttribute>() == null)
                continue;

            if (type.GetMethod("<Clone>$") != null)
                continue;

            services.AddScoped(type);
        }
    }

    private static void RegisterMockServices(IServiceCollection services, NeatooFactory mode)
    {
        if (mode == NeatooFactory.Remote)
            return;

        services.AddSingleton<IPersonRepository, PersonRepository>();
        services.AddSingleton<IOrderRepository, OrderRepository>();
        services.AddSingleton<MockUserContext>();
        services.AddSingleton<IUserContext>(sp => sp.GetRequiredService<MockUserContext>());
        services.AddSingleton<MockEmailService>();
        services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<MockEmailService>());
        services.AddSingleton<MockAuditLogService>();
        services.AddSingleton<IAuditLogService>(sp => sp.GetRequiredService<MockAuditLogService>());
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
