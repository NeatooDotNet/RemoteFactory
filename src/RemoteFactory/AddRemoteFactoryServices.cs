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
	public static IServiceCollection AddNeatooRemoteFactory(this IServiceCollection services, NeatooFactory remoteLocal, params Assembly[] assemblies)
	{
		return AddNeatooRemoteFactory(services, remoteLocal, new NeatooSerializationOptions(), assemblies);
	}

	/// <summary>
	/// Adds Neatoo RemoteFactory services with custom serialization options.
	/// </summary>
	public static IServiceCollection AddNeatooRemoteFactory(this IServiceCollection services, NeatooFactory remoteLocal, NeatooSerializationOptions serializationOptions, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services, nameof(services));
		ArgumentNullException.ThrowIfNull(serializationOptions, nameof(serializationOptions));
		ArgumentNullException.ThrowIfNull(assemblies, nameof(assemblies));

		if (assemblies.Length == 0)
		{
			assemblies = [Assembly.GetExecutingAssembly()!];
		}

		// Register serialization options as singleton
		services.AddSingleton(serializationOptions);

		// Register correlation context for distributed tracing (scoped per request)
		services.AddScoped<ICorrelationContext, CorrelationContextImpl>();

		services.AddSingleton<IServiceAssemblies>(new ServiceAssemblies(assemblies));
		services.AddScoped<NeatooJsonTypeInfoResolver>();
		services.AddTransient<NeatooInterfaceJsonConverterFactory>();
		services.AddTransient<NeatooJsonConverterFactory, NeatooInterfaceJsonConverterFactory>();
		services.AddTransient(typeof(NeatooInterfaceJsonTypeConverter<>));
		services.AddSingleton<ILazyLoadFactory, LazyLoadFactory>();
		// Register EventTracker for fire-and-forget event handling.
		// Remote-mode clients do not register IEventTracker at all because
		// events serialize to the server -- there are no local background tasks
		// to track, and omitting the registration avoids DI validation failures
		// when logging is not configured (EventTracker requires ILogger).
		if (remoteLocal != NeatooFactory.Remote)
		{
			services.TryAddSingleton<IEventTracker, EventTracker>();

			// Built-in event scope initializer: propagates correlation ID to event scopes
			services.AddTransient<IEventScopeInitializer, CorrelationContextScopeInitializer>();
		}

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
			// Singleton relay for client-side event dispatch (holds handler registrations)
			services.AddSingleton<IFactoryEventRelay, FactoryEventRelayDispatcher>();

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

		services.RegisterFactories(remoteLocal, assemblies);

		return services;
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

	/// <summary>
	/// Registers a callback that propagates ambient context from the request scope to event handler scopes.
	/// Multiple initializers can be registered; they run in registration order.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Initializers run inside <c>Task.Run</c> after <c>CreateScope()</c> but before handler services
	/// are resolved. For fire-and-forget events, the parent scope may be disposed after the initializer
	/// runs — <b>copy values, do not hold references to parent-scope services</b>.
	/// </para>
	/// <para>
	/// A built-in initializer for <see cref="ICorrelationContext"/> is registered automatically
	/// by <see cref="AddNeatooRemoteFactory"/>. Call this method to add additional initializers
	/// (e.g., for tenant context).
	/// </para>
	/// </remarks>
	/// <param name="services">The service collection.</param>
	/// <param name="initializer">
	/// A callback that receives the parent (request) scope and the child (event) scope.
	/// Read from the parent, write to the child.
	/// </param>
	public static IServiceCollection AddRemoteFactoryEventScopeInitializer(
		this IServiceCollection services,
		Action<IServiceProvider, IServiceProvider> initializer)
	{
		ArgumentNullException.ThrowIfNull(initializer, nameof(initializer));
		services.AddTransient<IEventScopeInitializer>(_ => new DelegateEventScopeInitializer(initializer));
		return services;
	}

	[GeneratedRegex(@"(.*)([\.|\+])\w+$")]
	public static partial Regex GetConcreteName();
}
