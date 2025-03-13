using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
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

	public static IServiceCollection AddNeatooRemoteFactory(this IServiceCollection services, NeatooFactory portalServer, params Assembly[] assemblies)
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

		if (portalServer == NeatooFactory.Remote)
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

		foreach (var assembly in assemblies)
		{
			services.AutoRegisterAssemblyTypes(assembly);
		}

		return services;
	}

	private static void AutoRegisterAssemblyTypes(this IServiceCollection services, Assembly assembly)
	{
		var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && !string.IsNullOrEmpty(t.FullName)).ToList();
		var interfaces = assembly.GetTypes().Where(t => t.IsInterface && t.Name.StartsWith('I') && !string.IsNullOrEmpty(t.FullName))
														.ToDictionary(t => GetConcreteName().Replace(t.FullName!, $"$1$2{t.Name.Substring(1)}"));

		foreach (var t in types)
		{
			if (t.GetCustomAttribute<FactoryAttribute>() != null)
			{
				// Let the factory handle the registration
				continue;
			}

			var registrationMethod = t.GetMethod("FactoryServiceRegistrar", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

			if (registrationMethod != null)
			{
				registrationMethod.Invoke(null, [services]);
			}
			else if (interfaces.TryGetValue(t.FullName!, out var i))
			{
				services.AddTransient(i, t);
				services.AddTransient(t);
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
