using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Neatoo.RemoteFactory.Internal;

public class NeatooJsonTypeInfoResolver : DefaultJsonTypeInfoResolver
{

	public NeatooJsonTypeInfoResolver(IServiceProvider serviceProvider, IServiceProviderIsService serviceProviderIsService)
	{
		this.ServiceProvider = serviceProvider;
		this.ServiceProviderIsService = serviceProviderIsService;
	}

	private IServiceProvider ServiceProvider { get; }
	private IServiceProviderIsService ServiceProviderIsService { get; }

	public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		var jsonTypeInfo = base.GetTypeInfo(type, options);

		if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && jsonTypeInfo.CreateObject is null)
		{
			if (this.ServiceProviderIsService.IsService(type))
			{
				jsonTypeInfo.CreateObject = () =>
				{
					return this.ServiceProvider.GetRequiredService(type);
				};
			}
		}
		return jsonTypeInfo;
	}

}
