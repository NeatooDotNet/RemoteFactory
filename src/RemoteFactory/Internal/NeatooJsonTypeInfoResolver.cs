using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

		if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && jsonTypeInfo.CreateObject is null && type is not null)
		{
			if (this.ServiceProviderIsService.IsService(type))
			{
				jsonTypeInfo.CreateObject = () =>
				{
					return this.ServiceProvider.GetRequiredService(type);
				};
			}
			else if (type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) is not null)
			{
				// Plain DTOs: the IL trimmer strips constructor metadata that STJ's
				// DefaultJsonTypeInfoResolver needs. Activator.CreateInstance uses a
				// different code path. The DTO type's metadata survives because it's
				// referenced as a property type on preserved (DI-registered) types.
				// Guard: only for types with a public parameterless constructor.
				// Records/classes with only parameterized constructors are handled by
				// RecordBypassConverterFactory and must not have CreateObject set here.
				jsonTypeInfo.CreateObject = () => Activator.CreateInstance(type)!;
			}
		}
		return jsonTypeInfo;
	}

}
