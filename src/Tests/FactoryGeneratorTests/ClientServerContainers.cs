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

internal sealed class MakeRemoteDelegateRequest : IMakeRemoteDelegateRequest
{
	private readonly INeatooJsonSerializer NeatooJsonSerializer;
	private readonly IServiceProvider serviceProvider;

	public MakeRemoteDelegateRequest(INeatooJsonSerializer neatooJsonSerializer, IServiceProvider serviceProvider)
	{
		this.NeatooJsonSerializer = neatooJsonSerializer;
		this.serviceProvider = serviceProvider;
	}

	public async Task<T?> ForDelegate<T>(Type delegateType, object?[]? parameters)
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

	public static (IServiceScope server, IServiceScope client) Scopes()
	{
		lock (lockContainer)
		{
			if (serverContainer == null)
			{
				var serverCollection = new ServiceCollection();
				var clientCollection = new ServiceCollection();

				RegisterIfAttribute(serverCollection);
				RegisterIfAttribute(clientCollection);

				serverCollection.AddNeatooRemoteFactory(NeatooFactory.Local, Assembly.GetExecutingAssembly());
				serverCollection.AddSingleton<IServerOnlyService, ServerOnly>();
				serverCollection.AddSingleton<IAuthRemote, AuthServerOnly>();
				serverCollection.RegisterMatchingName(Assembly.GetExecutingAssembly());

				clientCollection.AddNeatooRemoteFactory(NeatooFactory.Remote, Assembly.GetExecutingAssembly());
				clientCollection.AddScoped<ServerServiceProvider>();
				clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeRemoteDelegateRequest>();
				clientCollection.AddScoped<IFactoryCore<FactoryCoreTarget>, FactoryCoreForTarget>(); // Test that DI does what I expect and injects this override of IFactoryCore
				clientCollection.RegisterMatchingName(Assembly.GetExecutingAssembly());

				serverContainer = serverCollection.BuildServiceProvider();
				clientContainer = clientCollection.BuildServiceProvider();
			}
		}

		var serverScope = serverContainer.CreateScope();
		var clientScope = clientContainer.CreateScope();

		clientScope.GetRequiredService<ServerServiceProvider>().serverProvider = serverScope.ServiceProvider;

		return (serverScope, clientScope);
	}

	private static void RegisterIfAttribute(this IServiceCollection services)
	{

		foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
		{
			if (!t.GenericTypeArguments.Any() && !t.IsAbstract && t.GetCustomAttribute<FactoryAttribute>() != null)
			{
				services.AddScoped(t);
			}
			
			if (t.GetCustomAttribute(typeof(AuthorizeAttribute<>)) != null)
			{

				var attr = t.GetCustomAttribute(typeof(AuthorizeAttribute<>))!;
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
