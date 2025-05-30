using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Neatoo.RemoteFactory.Internal;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Neatoo.RemoteFactory;

public enum NeatooFactory
{
	/// <summary>
	/// This is the server in a 3-Tier architecture
	/// </summary>
	Server,
	/// <summary>
	/// This is the client in a 3-Tier architecture
	/// </summary>
	Remote,
	/// <summary>
	/// A logical factory that does not make any remote calls
	/// Factory methods will be executed in the same process
	/// </summary>
	Logical
}

public delegate IEnumerable<Type> GetServiceImplementationTypes(Type type);

public static partial class RemoteFactoryServices
{
	public const string HttpClientKey = "NeatooHttpClient";

	public static IServiceCollection AddNeatooRemoteFactory(this IServiceCollection services, NeatooFactory remoteLocal, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services, nameof(services));
		ArgumentNullException.ThrowIfNull(assemblies, nameof(assemblies));

		if (assemblies.Length == 0)
		{
			assemblies = [Assembly.GetExecutingAssembly()!];
		}

		services.AddSingleton<IServiceAssemblies>(new ServiceAssemblies(assemblies));
		services.AddScopedSelf<INeatooJsonSerializer, NeatooJsonSerializer>();
		services.AddScoped<NeatooJsonTypeInfoResolver>();
		services.AddTransient<NeatooInterfaceJsonConverterFactory>();
		services.AddTransient<NeatooJsonConverterFactory, NeatooInterfaceJsonConverterFactory>();
		services.AddTransient(typeof(NeatooInterfaceJsonTypeConverter<>));
		services.AddSingleton(typeof(IFactoryCore<>), typeof(FactoryCore<>));

		if (remoteLocal == NeatooFactory.Remote)
		{
			// This being registered changes the behavior of every Factory
			services.AddScoped<IMakeRemoteDelegateRequest, MakeRemoteDelegateRequest>();

			services.AddTransient(sp =>
			{
				var httpClient = sp.GetRequiredKeyedService<HttpClient>(HttpClientKey);
				return MakeRemoteDelegateRequestHttpCallImplementation.Create(httpClient);
			});
		}
		else if (remoteLocal == NeatooFactory.Logical)
		{
			// Client Only
			// We still Serialize the objects
			// but we don't need to make a call to the server
			services.AddScoped<IMakeRemoteDelegateRequest, MakeLocalSerializedDelegateRequest>();
		}
		else
		{
			services.AddTransient<HandleRemoteDelegateRequest>(s => LocalServer.HandlePortalRequest(s));
		}

		services.AddSingleton<GetServiceImplementationTypes>(s =>
			{
				return (Type type) =>
				{
					// This is why these are delegates
					// Need access to the DI container
					return services
							  .Where(d => d.ServiceType == type && d.ImplementationType != null)
							  .Select(d => d.ImplementationType!)
							  .Distinct();
				};
			});

		services.RegisterFactories(remoteLocal, assemblies);

		return services;
	}

	private static void RegisterFactories(this IServiceCollection services, NeatooFactory remoteLocal, params Assembly[] assemblies)
	{
		foreach (var assembly in assemblies)
		{
			var methods = assembly.GetTypes().Select(t => t.GetMethod("FactoryServiceRegistrar", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
				.Where(m => m != null).ToList();

			foreach (var m in methods)
			{
				m?.Invoke(null, [services, remoteLocal]);
			}
		}
	}

	/// <summary>
	/// Register services where the service and implementation have the same name
	/// but the service is an interface whose name starts with an "I"
	/// and the implementation is the concrete type without the "I"
	/// Ex. IBusinessObject would be registered to BusinessObject
	/// </summary>
	/// <param name="services"></param>
	/// <param name="assembly"></param>
	public static void RegisterMatchingName(this IServiceCollection services, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services, nameof(services));
		ArgumentNullException.ThrowIfNull(assemblies, nameof(assemblies));

		foreach (var assembly in assemblies)
		{
			var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && !string.IsNullOrEmpty(t.FullName)).ToList();

			var interfaces = assembly.GetTypes().Where(t => t.IsInterface && t.Name.StartsWith('I') && !string.IsNullOrEmpty(t.FullName))
													.ToDictionary(t => GetConcreteName().Replace(t.FullName!, $"$1$2{t.Name.Substring(1)}"));
			foreach (var t in types)
			{
				if (interfaces.TryGetValue(t.FullName!, out var i))
				{
					services.TryAddTransient(i, t);
					services.TryAddTransient(t);
				}
			}
		}
	}

	public static IServiceCollection AddTransientSelf<TService, TImpl>(this IServiceCollection services) where TImpl : class, TService where TService : class
	{
		services.TryAddTransient<TService, TImpl>();
		services.TryAddTransient(provider => (TImpl)provider.GetRequiredService<TService>());
		return services;
	}

	public static IServiceCollection AddTransientSelf(this IServiceCollection services, Type tService, Type tImpl)
	{
		services.TryAddTransient(tService, tImpl);
		services.TryAddTransient(tImpl, provider => provider.GetRequiredService(tService));
		return services;
	}

	public static IServiceCollection AddScopedSelf<TService, TImpl>(this IServiceCollection services) where TImpl : class, TService where TService : class
	{
		services.AddScoped<TService, TImpl>();
		services.AddScoped(provider => (TImpl)provider.GetRequiredService<TService>());
		return services;
	}

	[GeneratedRegex(@"(.*)([\.|\+])\w+$")]
	public static partial Regex GetConcreteName();
}
