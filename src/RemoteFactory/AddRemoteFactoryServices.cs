using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Neatoo.RemoteFactory;

public enum NeatooFactory
{
	Local,
	Remote
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
		services.AddScoped(typeof(IFactoryCore<>), typeof(FactoryCore<>));

		if (remoteLocal == NeatooFactory.Remote)
		{
			services.AddScoped<IMakeRemoteDelegateRequest, MakeRemoteDelegateRequest>();

			services.AddTransient(sp =>
			{
				var httpClient = sp.GetRequiredKeyedService<HttpClient>(HttpClientKey);
				return MakeRemoteDelegateRequestHttpCallImplementation.Create(httpClient);
			});
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
					services.AddTransient(i, t);
					services.AddTransient(t);
				}
			}
		}
	}

	public static IServiceCollection AddTransientSelf<TService, TImpl>(this IServiceCollection services) where TImpl : class, TService where TService : class
	{
		services.AddTransient<TService, TImpl>();
		services.AddTransient(provider => (TImpl)provider.GetRequiredService<TService>());
		return services;
	}

	public static IServiceCollection AddTransientSelf(this IServiceCollection services, Type tService, Type tImpl)
	{
		services.AddTransient(tService, tImpl);
		services.AddTransient(tImpl, provider => provider.GetRequiredService(tService));
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
