using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
		return services;
	}
}
