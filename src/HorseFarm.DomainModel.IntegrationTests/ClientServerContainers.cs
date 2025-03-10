using HorseFarm.Ef;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HorseFarm.DomainModel.IntegrationTests;

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
		var remoteRequestOnServer = JsonSerializer.Deserialize<RemoteDelegateRequestDto>(json)!; // this.NeatooJsonSerializer.Deserialize<RemoteRequestDto>(json);

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

				serverCollection.AddNeatooRemoteFactory(NeatooHost.Local, Assembly.GetExecutingAssembly(), typeof(IHorseFarm).Assembly);
				serverCollection.AddScoped<IHorseFarmContext, HorseFarmContext>();


				clientCollection.AddScoped<ServerServiceProvider>();
				clientCollection.AddNeatooRemoteFactory(NeatooHost.Remote, Assembly.GetExecutingAssembly(), typeof(IHorseFarm).Assembly);

				clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeRemoteDelegateRequest>();
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
			if (t.GetCustomAttribute<FactoryAttribute>() != null)
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
