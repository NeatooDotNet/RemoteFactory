using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Neatoo.RemoteFactory.Internal;

public class NeatooJsonConverterFactory : JsonConverterFactory
{
	private IServiceProvider scope;
	private readonly ILocalAssemblies localAssemblies;

	public NeatooJsonConverterFactory(IServiceProvider scope, ILocalAssemblies localAssemblies)
	{
		this.scope = scope;
		this.localAssemblies = localAssemblies;
	}

	public override bool CanConvert(Type typeToConvert)
	{
		ArgumentNullException.ThrowIfNull(typeToConvert, nameof(typeToConvert));

		if ((typeToConvert.IsInterface || typeToConvert.IsAbstract) && !typeToConvert.IsGenericType && this.localAssemblies.HasType(typeToConvert))
		{
			return true;
		}

		return false;
	}

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(typeToConvert, nameof(typeToConvert));

		if (typeToConvert.IsInterface || typeToConvert.IsAbstract)
		{
			return (JsonConverter)this.scope.GetRequiredService(typeof(NeatooInterfaceJsonTypeConverter<>).MakeGenericType(typeToConvert));
		}

		throw new JsonException($"CreateConverter not implemented for {typeToConvert}");
	}
}
