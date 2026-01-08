using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory.Internal;

namespace Neatoo.RemoteFactory.AspNetCore;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds Neatoo ASP.NET Core services with default serialization options (Ordinal format).
	/// </summary>
	public static IServiceCollection AddNeatooAspNetCore(this IServiceCollection services, params Assembly[] entityLibraries)
	{
		return AddNeatooAspNetCore(services, new NeatooSerializationOptions(), entityLibraries);
	}

	/// <summary>
	/// Adds Neatoo ASP.NET Core services with custom serialization options.
	/// </summary>
	public static IServiceCollection AddNeatooAspNetCore(this IServiceCollection services, NeatooSerializationOptions serializationOptions, params Assembly[] entityLibraries)
	{
		services.AddNeatooRemoteFactory(NeatooFactory.Server, serializationOptions, entityLibraries);
		services.TryAddScoped<IAspAuthorize, AspAuthorize>();

		// Register the HandleRemoteDelegateRequest with logger injection
		services.AddScoped<HandleRemoteDelegateRequest>(sp =>
		{
			var loggerFactory = sp.GetService<ILoggerFactory>();
			var logger = loggerFactory?.CreateLogger(NeatooLoggerCategories.Server);
			return LocalServer.HandlePortalRequest(sp, logger);
		});

		// Register EventTrackerHostedService for graceful shutdown of pending events
		services.AddSingleton<IHostedService>(sp =>
		{
			var eventTracker = sp.GetRequiredService<IEventTracker>();
			var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
			var logger = sp.GetRequiredService<ILogger<EventTrackerHostedService>>();
			return new EventTrackerHostedService(eventTracker, lifetime, logger);
		});

		return services;
	}
}
