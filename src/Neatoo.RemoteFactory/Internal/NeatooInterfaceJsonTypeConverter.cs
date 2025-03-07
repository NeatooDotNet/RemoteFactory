using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Neatoo.RemoteFactory.Internal;

public class NeatooInterfaceJsonTypeConverter<T> : JsonConverter<T>
{
	private readonly IServiceProvider scope;
	private readonly ILocalAssemblies localAssemblies;

	public NeatooInterfaceJsonTypeConverter(IServiceProvider scope, ILocalAssemblies localAssemblies)
	{
		this.scope = scope;
		this.localAssemblies = localAssemblies;
	}

	public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(options, nameof(options));

		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException();
		}

		var editBaseType = typeToConvert;


		T? result = default;
		var id = string.Empty;
		Type? concreteType = default;

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				if(result == null)
				{
					throw new JsonException($"Unexpected end object");
				}

				if (!string.IsNullOrEmpty(id))
				{
					options.ReferenceHandler!.CreateResolver()
									 .AddReference(id, result);
				}

				if (result is IJsonOnDeserialized jsonOnDeserialized)
				{
					jsonOnDeserialized.OnDeserialized();
				}

				return result;
			}

			// Get the key.
			if (reader.TokenType != JsonTokenType.PropertyName) { throw new JsonException("Expecting PropertyName JsonTokenType"); }

			var propertyName = reader.GetString();
			reader.Read();

			if (propertyName == "$type")
			{
				var typeName = reader.GetString() ?? throw new JsonException("Expecting string value for $type");
				concreteType = this.localAssemblies.FindType(typeName) ?? throw new JsonException($"Could not load {typeName}");
			}
			else if (propertyName == "$value")
			{
				if (concreteType == null)
				{
					throw new JsonException("Expecting $type before $value");
				}
				result = (T?)JsonSerializer.Deserialize(ref reader, concreteType, options);
			}
		}

		throw new JsonException();
	}
	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(writer, nameof(writer));
		ArgumentNullException.ThrowIfNull(options, nameof(options));

		if(value == null) { return; }

		writer.WriteStartObject();

		writer.WritePropertyName("$type");
		var type = value.GetType().FullName;
		writer.WriteStringValue(type);


		writer.WritePropertyName("$value");
		JsonSerializer.Serialize(writer, value, value.GetType(), options);

		writer.WriteEndObject();
	}
}
