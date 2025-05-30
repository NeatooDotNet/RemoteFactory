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

	public static IServiceCollection AddNeatooAspNetCore(this IServiceCollection services, params Assembly[] entityLibraries)
	{
		services.AddNeatooRemoteFactory(NeatooFactory.Server, entityLibraries);
		services.TryAddScoped<IAspAuthorize, AspAuthorize>();
		return services;
	}

}
