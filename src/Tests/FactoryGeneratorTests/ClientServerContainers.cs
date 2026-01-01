using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Showcase;
using Neatoo.RemoteFactory.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests;

public class ServerServiceProvider
{
	public IServiceProvider serverProvider { get; set; } = null!;
}

internal sealed class MakeSerializedServerStandinDelegateRequest : IMakeRemoteDelegateRequest
{
	private readonly INeatooJsonSerializer NeatooJsonSerializer;
	private readonly IServiceProvider serviceProvider;

	public MakeSerializedServerStandinDelegateRequest(INeatooJsonSerializer neatooJsonSerializer, IServiceProvider serviceProvider)
	{
		this.NeatooJsonSerializer = neatooJsonSerializer;
		this.serviceProvider = serviceProvider;
	}
	public async Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters)
	{
		var result = await this.ForDelegateNullable<T>(delegateType, parameters);
		if (result == null)
		{
			throw new InvalidOperationException($"The result of the remote delegate call was null, but a non-nullable type was expected.");
		}
		return result;
	}

	public async Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters)
	{
		// Mimic all the steps of a Remote call except the actual http call

		var remoteRequest = this.NeatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);

		// Mimic real life - use standard ASP.NET Core JSON serialization
		var json = JsonSerializer.Serialize(remoteRequest); //NeatooJsonSerializer.Serialize(remoteRequest);
		var remoteRequestOnServer = JsonSerializer.Deserialize<RemoteRequestDto>(json)!; // this.NeatooJsonSerializer.Deserialize<RemoteRequestDto>(json);

		// Use the Server's container
		var remoteResponseOnServer = await this.serviceProvider.GetRequiredService<ServerServiceProvider>()
																			 .serverProvider
																			 .GetRequiredService<HandleRemoteDelegateRequest>()(remoteRequestOnServer);

		json = JsonSerializer.Serialize(remoteResponseOnServer); // NeatooJsonSerializer.Serialize(remoteResponseOnServer);
		var result = JsonSerializer.Deserialize<RemoteResponseDto>(json); // NeatooJsonSerializer.Deserialize<RemoteResponseDto>(json);

		return this.NeatooJsonSerializer.DeserializeRemoteResponse<T>(result!);
	}
}

internal static class ClientServerContainers
{
	private static object lockContainer = new object();
	static IServiceProvider serverContainer = null!;
	static IServiceProvider clientContainer = null!;
	static IServiceProvider localContainer = null!;

	public static (IServiceScope server, IServiceScope client, IServiceScope local) Scopes()
	{
		lock (lockContainer)
		{
			if (serverContainer == null)
			{
				var serverCollection = new ServiceCollection();
				var clientCollection = new ServiceCollection();
				var localCollection = new ServiceCollection();

				RegisterIfAttribute(serverCollection);
				RegisterIfAttribute(clientCollection);
				RegisterIfAttribute(localCollection);

				serverCollection.AddNeatooRemoteFactory(NeatooFactory.Server, Assembly.GetExecutingAssembly());
				serverCollection.AddSingleton<IServerOnlyService, ServerOnly>();
				serverCollection.AddSingleton<IAuthRemote, AuthServerOnly>();
				serverCollection.RegisterMatchingName(Assembly.GetExecutingAssembly());

				clientCollection.AddNeatooRemoteFactory(NeatooFactory.Remote, Assembly.GetExecutingAssembly());
				clientCollection.AddScoped<ServerServiceProvider>();
				clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>();
				clientCollection.AddScoped<IFactoryCore<FactoryCoreTarget>, FactoryCoreForTarget>(); // Test that DI does what I expect and injects this override of IFactoryCore
				clientCollection.RegisterMatchingName(Assembly.GetExecutingAssembly());

				localCollection.AddNeatooRemoteFactory(NeatooFactory.Logical, Assembly.GetExecutingAssembly());
				localCollection.RegisterMatchingName(Assembly.GetExecutingAssembly());

				serverContainer = serverCollection.BuildServiceProvider();
				clientContainer = clientCollection.BuildServiceProvider();
				localContainer = localCollection.BuildServiceProvider();
			}
		}

		var serverScope = serverContainer.CreateScope();
		var clientScope = clientContainer.CreateScope();
		var localScope = localContainer.CreateScope();

		clientScope.GetRequiredService<ServerServiceProvider>().serverProvider = serverScope.ServiceProvider;

		return (serverScope, clientScope, localScope);
	}

	private static void RegisterIfAttribute(this IServiceCollection services)
	{

		foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
		{
			if (!t.GenericTypeArguments.Any() && !t.IsAbstract && t.GetCustomAttribute<FactoryAttribute>() != null)
			{
				// Don't register records as DI services - they are value objects
				// Records have a compiler-generated <Clone>$ method
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
				//services.AddTransient(attr.AttributeType.GenericTypeArguments[0]);
			}
		}
	}
}

internal static class ServiceScopeProviderExtension
{
	public static T GetRequiredService<T>(this IServiceScope service) where T : class
	{

		return service.ServiceProvider.GetRequiredService<T>();
	}
}
