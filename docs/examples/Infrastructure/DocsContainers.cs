using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Neatoo.RemoteFactory.DocsExamples.Infrastructure;

/// <summary>
/// Holds reference to the server's service provider for simulated remote calls.
/// </summary>
public class ServerServiceProvider
{
	public IServiceProvider serverProvider { get; set; } = null!;
}

/// <summary>
/// Simulates remote HTTP calls by serializing requests through JSON,
/// executing on the server container, and deserializing responses.
/// </summary>
internal sealed class MakeSerializedServerStandinDelegateRequest : IMakeRemoteDelegateRequest
{
	private readonly INeatooJsonSerializer NeatooJsonSerializer;
	private readonly IServiceProvider serviceProvider;

	public MakeSerializedServerStandinDelegateRequest(INeatooJsonSerializer neatooJsonSerializer, IServiceProvider serviceProvider)
	{
		this.NeatooJsonSerializer = neatooJsonSerializer;
		this.serviceProvider = serviceProvider;
	}

	public async Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
	{
		var result = await this.ForDelegateNullable<T>(delegateType, parameters, cancellationToken);
		if (result == null)
		{
			throw new InvalidOperationException($"The result of the remote delegate call was null, but a non-nullable type was expected.");
		}
		return result;
	}

	public async Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
	{
		// Check for cancellation before processing
		cancellationToken.ThrowIfCancellationRequested();

		// Mimic all the steps of a Remote call except the actual HTTP call
		var remoteRequest = this.NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);

		// Use standard ASP.NET Core JSON serialization (mimics real HTTP transport)
		var json = JsonSerializer.Serialize(remoteRequest);
		var remoteRequestOnServer = JsonSerializer.Deserialize<RemoteRequestDto>(json)!;

		// Execute on the Server's container - pass cancellation token to the handler
		var remoteResponseOnServer = await this.serviceProvider.GetRequiredService<ServerServiceProvider>()
			.serverProvider
			.GetRequiredService<HandleRemoteDelegateRequest>()(remoteRequestOnServer, cancellationToken);

		json = JsonSerializer.Serialize(remoteResponseOnServer);
		var result = JsonSerializer.Deserialize<RemoteResponseDto>(json);

		return this.NeatooJsonSerializer.DeserializeRemoteResponse<T>(result!);
	}
}

/// <summary>
/// Creates isolated client/server/local DI containers for testing remote operations.
/// This simulates a 3-tier architecture where:
/// - Server: Runs with NeatooFactory.Server mode
/// - Client: Runs with NeatooFactory.Remote mode (calls go through simulated HTTP)
/// - Local: Runs with NeatooFactory.Logical mode (everything in-process)
/// </summary>
internal static class DocsContainers
{
	private static readonly object lockContainer = new object();

	// Thread-safe cache for format-specific containers
	private static readonly ConcurrentDictionary<SerializationFormat, (IServiceProvider server, IServiceProvider client, IServiceProvider local)> formatContainers = new();

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
		var containers = formatContainers.GetOrAdd(format, CreateContainers);

		var serverScope = containers.server.CreateScope();
		var clientScope = containers.client.CreateScope();
		var localScope = containers.local.CreateScope();

		// Link client to server for simulated remote calls
		lock (lockContainer)
		{
			clientScope.ServiceProvider.GetRequiredService<ServerServiceProvider>().serverProvider = serverScope.ServiceProvider;
		}

		return (serverScope, clientScope, localScope);
	}

	private static (IServiceProvider server, IServiceProvider client, IServiceProvider local) CreateContainers(SerializationFormat format)
	{
		var serializationOptions = new NeatooSerializationOptions { Format = format };

		var serverCollection = new ServiceCollection();
		var clientCollection = new ServiceCollection();
		var localCollection = new ServiceCollection();

		// Register [Factory] attributed classes and authorization interfaces
		RegisterIfAttribute(serverCollection);
		RegisterIfAttribute(clientCollection);
		RegisterIfAttribute(localCollection);

		// Register mock services for documentation examples
		RegisterMockServices(serverCollection);
		RegisterMockServices(clientCollection);
		RegisterMockServices(localCollection);

		// Server container: Full server mode with all services
		serverCollection.AddNeatooRemoteFactory(NeatooFactory.Server, serializationOptions, Assembly.GetExecutingAssembly());
		serverCollection.RegisterMatchingName(Assembly.GetExecutingAssembly());

		// Client container: Remote mode with simulated HTTP transport
		clientCollection.AddNeatooRemoteFactory(NeatooFactory.Remote, serializationOptions, Assembly.GetExecutingAssembly());
		clientCollection.AddScoped<ServerServiceProvider>();
		clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>();
		clientCollection.RegisterMatchingName(Assembly.GetExecutingAssembly());

		// Local container: Logical mode for in-process testing
		localCollection.AddNeatooRemoteFactory(NeatooFactory.Logical, serializationOptions, Assembly.GetExecutingAssembly());
		localCollection.RegisterMatchingName(Assembly.GetExecutingAssembly());

		return (
			serverCollection.BuildServiceProvider(),
			clientCollection.BuildServiceProvider(),
			localCollection.BuildServiceProvider()
		);
	}

	/// <summary>
	/// Registers mock services used by documentation examples.
	/// </summary>
	private static void RegisterMockServices(IServiceCollection services)
	{
		// Register mock data contexts for examples
		services.AddScoped<IPersonContext, InMemoryPersonContext>();
		services.AddScoped<IService, Service>();

		// Register mock services for service injection examples
		services.AddScoped<Concepts.IEmailService, Concepts.MockEmailService>();
		services.AddScoped<Concepts.IAuditService, Concepts.MockAuditService>();
		services.AddScoped<Concepts.ICurrentUser, Concepts.MockCurrentUser>();
		services.AddScoped<Concepts.ICalculatorService, Concepts.CalculatorService>();

		// Register authorization services
		services.AddScoped<Authorization.ICurrentUserForAuth, Authorization.MockCurrentUserForAuth>();
		services.AddScoped<Authorization.IAuthorizedModelAuth, Authorization.AuthorizedModelAuth>();
		services.AddScoped<Authorization.IDeniedModelAuth, Authorization.DeniedModelAuth>();

		// Add logging (NullLogger for test purposes)
		services.AddLogging();
	}

	/// <summary>
	/// Auto-registers classes with [Factory] attribute and authorization interfaces.
	/// </summary>
	private static void RegisterIfAttribute(this IServiceCollection services)
	{
		foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
		{
			if (!t.GenericTypeArguments.Any() && !t.IsAbstract && t.GetCustomAttribute<FactoryAttribute>() != null)
			{
				// Don't register records as DI services - they are value objects
				if (t.GetMethod("<Clone>$") != null)
					continue;

				services.AddScoped(t);
			}

			if (t.GetCustomAttribute(typeof(AuthorizeFactoryAttribute<>)) != null)
			{
				var attr = t.GetCustomAttribute(typeof(AuthorizeFactoryAttribute<>))!;
				var authType = attr.GetType().GetGenericArguments()[0];

				if (!authType.IsInterface)
				{
					services.AddScoped(authType);
				}
			}
		}
	}
}

/// <summary>
/// Extension method for convenient service resolution from scope.
/// </summary>
internal static class ServiceScopeProviderExtension
{
	public static T GetRequiredService<T>(this IServiceScope scope) where T : class
	{
		return scope.ServiceProvider.GetRequiredService<T>();
	}
}
