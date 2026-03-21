using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Claims types with parameterized constructors (records, classes without a default
/// constructor) and serializes them without reference metadata (<c>$id</c>/<c>$ref</c>).
/// </summary>
/// <remarks>
/// <para>
/// STJ throws <c>NotSupportedException</c> when reference metadata appears on
/// types deserialized through a parameterized constructor. This converter prevents
/// that by delegating to inner <see cref="JsonSerializerOptions"/> with
/// <see cref="JsonSerializerOptions.ReferenceHandler"/> set to <c>null</c>.
/// </para>
/// <para>
/// Records are DDD value objects whose identity is defined by their values, not
/// by reference. Excluding them from reference tracking is semantically correct:
/// duplicating a value object's internal state on round-trip is the right behavior.
/// </para>
/// <para>
/// Detection rule: the type has no public parameterless constructor AND has at
/// least one public constructor with parameters. This matches STJ's own heuristic
/// for parameterized-constructor deserialization.
/// </para>
/// </remarks>
internal sealed class RecordBypassConverterFactory : JsonConverterFactory
{
	private JsonSerializerOptions? _innerOptions;
	private readonly object _lock = new();

	public override bool CanConvert(Type typeToConvert)
	{
		var constructors = typeToConvert.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

		bool hasParameterlessCtor = false;
		bool hasParameterizedCtor = false;

		for (int i = 0; i < constructors.Length; i++)
		{
			if (constructors[i].GetParameters().Length == 0)
			{
				hasParameterlessCtor = true;
			}
			else
			{
				hasParameterizedCtor = true;
			}
		}

		// Claim the type only when STJ would use a parameterized constructor:
		// no public parameterless constructor, but at least one public constructor with parameters.
		return !hasParameterlessCtor && hasParameterizedCtor;
	}

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var innerOpts = GetOrCreateInnerOptions(options);

		var converterType = typeof(RecordBypassConverter<>).MakeGenericType(typeToConvert);
		return (JsonConverter)Activator.CreateInstance(converterType, innerOpts)!;
	}

	private JsonSerializerOptions GetOrCreateInnerOptions(JsonSerializerOptions outerOptions)
	{
		if (_innerOptions is not null)
		{
			return _innerOptions;
		}

		lock (_lock)
		{
			if (_innerOptions is not null)
			{
				return _innerOptions;
			}

			var inner = new JsonSerializerOptions(outerOptions);
			inner.ReferenceHandler = null;

			// Remove this factory from the inner options to prevent infinite recursion.
			for (int i = inner.Converters.Count - 1; i >= 0; i--)
			{
				if (inner.Converters[i] is RecordBypassConverterFactory)
				{
					inner.Converters.RemoveAt(i);
				}
			}

			_innerOptions = inner;
			return inner;
		}
	}
}

/// <summary>
/// Converter that delegates serialization to inner options without
/// <see cref="ReferenceHandler"/>, preventing <c>$id</c>/<c>$ref</c> metadata
/// on types with parameterized constructors.
/// </summary>
internal sealed class RecordBypassConverter<T> : JsonConverter<T>
{
	private readonly JsonSerializerOptions _innerOptions;

	public RecordBypassConverter(JsonSerializerOptions innerOptions)
	{
		_innerOptions = innerOptions;
	}

	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null)
		{
			return default;
		}

		return JsonSerializer.Deserialize<T>(ref reader, _innerOptions);
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}

		JsonSerializer.Serialize(writer, value, _innerOptions);
	}
}
