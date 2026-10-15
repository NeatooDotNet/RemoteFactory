using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
	/// A logical factory for single-tier applications and unit tests.
	/// Executes factory methods locally without serialization (same behavior as Server mode).
	/// Use this mode when no server/client separation is needed.
	/// </summary>
	Logical
}

public delegate IEnumerable<Type> GetServiceImplementationTypes(Type type);

public static partial class RemoteFactoryServices
{
	public const string HttpClientKey = "NeatooHttpClient";

	/// <summary>
	/// Adds Neatoo RemoteFactory services with default serialization options (Ordinal format).
	/// </summary>
	/// <remarks>
	/// Safe to call more than once (#117): see the overload that takes serialization options.
	/// This overload never replaces options an earlier call registered.
	/// </remarks>
	public static IServiceCollection AddNeatooRemoteFactory(this IServiceCollection services, NeatooFactory remoteLocal, params Assembly[] assemblies)
	{
		return AddNeatooRemoteFactoryCore(services, remoteLocal, null, assemblies);
	}

	/// <summary>
	/// Adds Neatoo RemoteFactory services with custom serialization options.
	/// </summary>
	/// <remarks>
	/// Safe to call more than once, as a host does when it reaches this method through two
	/// entry points (Neatoo's AddNeatooServices and AddNeatooAspNetCore, in either order).
	/// The first call registers everything. A later call with the same mode adds only what is
	/// new: its assemblies join the existing <see cref="IServiceAssemblies"/> and their factories
	/// are registered; every other registration, including a JSON converter factory the host
	/// replaced, is left alone. Options passed explicitly replace the registered ones. A later
	/// call with a different mode throws.
	/// </remarks>
	public static IServiceCollection AddNeatooRemoteFactory(this IServiceCollection services, NeatooFactory remoteLocal, NeatooSerializationOptions serializationOptions, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(serializationOptions, nameof(serializationOptions));
		return AddNeatooRemoteFactoryCore(services, remoteLocal, serializationOptions, assemblies);
	}

	private static IServiceCollection AddNeatooRemoteFactoryCore(IServiceCollection services, NeatooFactory remoteLocal, NeatooSerializationOptions? serializationOptions, Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services, nameof(services));
		ArgumentNullException.ThrowIfNull(assemblies, nameof(assemblies));

		if (assemblies.Length == 0)
		{
			assemblies = [Assembly.GetExecutingAssembly()!];
		}

		var existing = services
			.FirstOrDefault(d => d.ServiceType == typeof(RemoteFactoryRegistration))?
			.ImplementationInstance as RemoteFactoryRegistration;

		if (existing != null)
		{
			if (existing.Mode != remoteLocal)
			{
				throw new InvalidOperationException(
					$"AddNeatooRemoteFactory was already called with NeatooFactory.{existing.Mode}; it cannot also be NeatooFactory.{remoteLocal} in the same service collection.");
			}

			if (serializationOptions != null)
			{
				services.RemoveAll<NeatooSerializationOptions>();
				services.AddSingleton(serializationOptions);
			}

			var newAssemblies = assemblies.Distinct().Where(a => !existing.Assemblies.HasAssembly(a)).ToArray();
			if (newAssemblies.Length > 0)
			{
				existing.Assemblies.AddAssemblies(newAssemblies);
				services.RegisterFactories(remoteLocal, newAssemblies);
			}

			return services;
		}

		var serviceAssemblies = new ServiceAssemblies(assemblies);
		services.AddSingleton(new RemoteFactoryRegistration(remoteLocal, serviceAssemblies));

		// Register serialization options as singleton
		services.AddSingleton(serializationOptions ?? new NeatooSerializationOptions());

		// Register correlation context for distributed tracing (scoped per request)
		services.AddScoped<ICorrelationContext, CorrelationContextImpl>();

		services.AddSingleton<IServiceAssemblies>(serviceAssemblies);
		services.AddScoped<NeatooJsonTypeInfoResolver>();
		services.AddTransient<NeatooInterfaceJsonConverterFactory>();
		services.AddTransient<NeatooJsonConverterFactory, NeatooInterfaceJsonConverterFactory>();
		services.AddTransient(typeof(NeatooInterfaceJsonTypeConverter<>));
		services.AddSingleton<ILazyLoadFactory, LazyLoadFactory>();

		// Register IFactoryEvents — dispatches to handlers registered in FactoryEventHandlerRegistry.
		// In Remote mode, a RemoteFactoryEvents wrapper sends events to the server.
		// In Logical/Server mode, the dispatcher runs handlers locally.
		if (remoteLocal == NeatooFactory.Remote)
		{
			services.AddScoped<IFactoryEvents, RemoteFactoryEvents>();
		}
		else
		{
			services.TryAddScoped<IFactoryEvents, FactoryEventsDispatcher>();

			// Per-scope queue for handlers registered at a non-Immediate DispatchPhase.
			// Registered for Server AND Logical — unlike IFactoryEventCollector, which is
			// about relaying to a client and is therefore Server-only. Handler dispatch
			// happens in both modes, so both need somewhere to queue.
			services.TryAddScoped<IFactoryEventPhaseScheduler>(sp =>
				new FactoryEventPhaseScheduler(sp, sp.GetService<ILoggerFactory>()));

			// Consumer-facing drain trigger for the AfterFlush point. Resolves the
			// scope's EXISTING scheduler — a registration that constructed its own would
			// give the scope two schedulers, and the coordinator would drain an
			// always-empty twin while the dispatcher queues into the real one.
			services.TryAddScoped<IFactoryEventPhaseCoordinator>(sp =>
				new FactoryEventPhaseCoordinator(sp.GetRequiredService<IFactoryEventPhaseScheduler>(), sp.GetService<ILoggerFactory>()));

			// Register the delegate handler for remote IFactoryEvents.Raise requests.
			// When a Remote client sends a RaiseFactoryEventRemote request, the server
			// resolves this delegate, dispatches to local handlers in the request scope,
			// and keeps the HTTP response open until every handler has completed.
			services.AddScoped<RaiseFactoryEventRemote>(sp =>
			{
				var factoryEvents = sp.GetRequiredService<IFactoryEvents>();
				return (factoryEvent, options, cancellationToken) =>
					factoryEvents.RaiseUntyped(factoryEvent, (RaiseOptions)options, cancellationToken);
			});
		}

		// Register NeatooJsonSerializer with serialization options and logging
		services.AddScoped<INeatooJsonSerializer>(sp =>
		{
			var converterFactories = sp.GetServices<NeatooJsonConverterFactory>();
			var serviceAssemblies = sp.GetRequiredService<IServiceAssemblies>();
			var typeInfoResolver = sp.GetRequiredService<NeatooJsonTypeInfoResolver>();
			var options = sp.GetRequiredService<NeatooSerializationOptions>();
			var logger = sp.GetService<ILogger<NeatooJsonSerializer>>();
			var loggerFactory = sp.GetService<ILoggerFactory>();
			return new NeatooJsonSerializer(converterFactories, serviceAssemblies, typeInfoResolver, options, logger, loggerFactory);
		});
		services.AddScoped(sp => (NeatooJsonSerializer)sp.GetRequiredService<INeatooJsonSerializer>());

		if (remoteLocal == NeatooFactory.Remote)
		{
			// Default no-op relay. Consumers override by registering their own
			// IFactoryEventRelay before or after AddNeatooRemoteFactory — TryAdd
			// keeps consumer registrations winning when they come first, and a
			// later AddSingleton<IFactoryEventRelay, MyRelay> replaces this one
			// via standard DI last-writer-wins.
			//
			// NoOpFactoryEventRelay itself logs a one-time warning on its first
			// non-empty batch. That fires when events actually start flowing, which
			// is where the silent-drop footgun bites.
			services.TryAddSingleton<IFactoryEventRelay, NoOpFactoryEventRelay>();

			// This being registered changes the behavior of every Factory
			services.AddScoped<IMakeRemoteDelegateRequest, MakeRemoteDelegateRequest>();

			services.AddScoped(sp =>
			{
				var httpClient = sp.GetRequiredKeyedService<HttpClient>(HttpClientKey);
				var correlationContext = sp.GetRequiredService<ICorrelationContext>();
				return MakeRemoteDelegateRequestHttpCallImplementation.Create(httpClient, correlationContext);
			});
		}
		else if (remoteLocal == NeatooFactory.Server)
		{
			// Request-scoped collector captures events for relay back to client
			services.AddScoped<IFactoryEventCollector, FactoryEventCollector>();

			services.AddTransient<HandleRemoteDelegateRequest>(s =>
			{
				var logger = s.GetService<ILoggerFactory>()?.CreateLogger(NeatooLoggerCategories.Server);
				return LocalServer.HandlePortalRequest(s, logger);
			});
		}
		// Logical mode: No IMakeRemoteDelegateRequest registered.
		// Generated factories will use local constructor -> direct execution (same as Server mode).

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

		services.RegisterFactories(remoteLocal, assemblies.Distinct().ToArray());

		return services;
	}

	/// <summary>
	/// Marks a service collection AddNeatooRemoteFactory has run on: the mode it chose and the
	/// assembly list it registered, which a later call extends.
	/// </summary>
	private sealed class RemoteFactoryRegistration(NeatooFactory mode, ServiceAssemblies assemblies)
	{
		public NeatooFactory Mode { get; } = mode;
		public ServiceAssemblies Assemblies { get; } = assemblies;
	}

	private static void RegisterFactories(this IServiceCollection services, NeatooFactory remoteLocal, params Assembly[] assemblies)
	{
		foreach (var assembly in assemblies)
		{
			var attributes = assembly.GetCustomAttributes<NeatooFactoryRegistrarAttribute>();

			foreach (var attr in attributes)
			{
				var method = attr.Type.GetMethod("FactoryServiceRegistrar",
					BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
				method?.Invoke(null, [services, remoteLocal]);
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
