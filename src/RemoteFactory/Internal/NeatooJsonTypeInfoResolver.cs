using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

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

		if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && jsonTypeInfo.CreateObject is null && type is not null)
		{
			if (this.ServiceProviderIsService.IsService(type))
			{
				jsonTypeInfo.CreateObject = () =>
				{
					return this.ServiceProvider.GetRequiredService(type);
				};
			}
			else if (DtoConstructorRegistry.TryCreate(type, out var factory))
			{
				// Plain DTOs: use generator-emitted constructor lambdas that survive
				// IL trimming. The generator discovers DTO return types at compile time
				// and emits DtoConstructorRegistry.Register<Dto>(() => new Dto()) calls.
				jsonTypeInfo.CreateObject = factory;
			}
		}
		return jsonTypeInfo;
	}

}
