using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// JsonConverterFactory that creates ordinal converters for types implementing IOrdinalSerializable.
/// Ordinal converters serialize objects as JSON arrays instead of objects, eliminating property names.
/// </summary>
public class NeatooOrdinalConverterFactory : JsonConverterFactory
{
	private readonly NeatooSerializationOptions options;

	/// <summary>
	/// Static cache for registered converters (AOT path).
	/// Converters registered here bypass reflection-based creation.
	/// </summary>
	private static readonly ConcurrentDictionary<Type, JsonConverter> _registeredConverters = new();

	/// <summary>
	/// Static logger for logging converter factory operations.
	/// </summary>
	private static ILogger Logger => NeatooLogging.GetLogger(NeatooLoggerCategory.Serialization);

	/// <summary>
	/// Registers a pre-compiled ordinal converter for a type.
	/// This method is called at startup by generated factory code.
	/// </summary>
	/// <typeparam name="T">The type the converter handles.</typeparam>
	/// <param name="converter">The strongly-typed converter instance.</param>
	public static void RegisterConverter<T>(JsonConverter<T> converter) where T : class
	{
		if (_registeredConverters.TryAdd(typeof(T), converter))
		{
			Logger.ConverterRegistered(typeof(T).Name);
		}
	}

	/// <summary>
	/// Clears all registered converters. Used for testing purposes.
	/// </summary>
	internal static void ClearRegistrations()
	{
		_registeredConverters.Clear();
	}

	/// <summary>
	/// Gets the number of registered converters. Used for testing purposes.
	/// </summary>
	internal static int RegisteredConverterCount => _registeredConverters.Count;

	public NeatooOrdinalConverterFactory(NeatooSerializationOptions options)
	{
		this.options = options;
	}

	public override bool CanConvert(Type typeToConvert)
	{
		// Only use ordinal conversion when format is Ordinal
		if (this.options.Format != SerializationFormat.Ordinal)
		{
			return false;
		}

		// Check if type implements IOrdinalSerializable
		return typeof(IOrdinalSerializable).IsAssignableFrom(typeToConvert);
	}

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(typeToConvert);

		var typeName = typeToConvert.Name;

		// AOT path: Try registered converters first (fastest path)
		if (_registeredConverters.TryGetValue(typeToConvert, out var registered))
		{
			Logger.ConverterCacheHit(typeName);
			return registered;
		}

		// Middle path: Check if type implements IOrdinalConverterProvider<T>
		// and call its static CreateOrdinalConverter method (one-time reflection cost, then cached)
		var providerInterface = typeToConvert.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType &&
							i.GetGenericTypeDefinition() == typeof(IOrdinalConverterProvider<>));

		if (providerInterface != null)
		{
			var method = typeToConvert.GetMethod(
				nameof(IOrdinalConverterProvider<object>.CreateOrdinalConverter),
				BindingFlags.Public | BindingFlags.Static);

			if (method != null)
			{
				Logger.CreatingConverter(typeName, "IOrdinalConverterProvider");
				var converter = (JsonConverter)method.Invoke(null, null)!;
				_registeredConverters.TryAdd(typeToConvert, converter);
				return converter;
			}
		}

		// Reflection fallback path (legacy/non-generated types)
		Logger.ReflectionFallback(typeName);
		Logger.CreatingConverter(typeName, "Reflection");
		var converterType = typeof(NeatooOrdinalConverter<>).MakeGenericType(typeToConvert);
		var fallbackConverter = (JsonConverter)Activator.CreateInstance(converterType)!;
		// Cache fallback converters too to avoid repeated reflection
		_registeredConverters.TryAdd(typeToConvert, fallbackConverter);
		return fallbackConverter;
	}
}

/// <summary>
/// JsonConverter that serializes IOrdinalSerializable types as JSON arrays.
/// Properties are written in ordinal order (alphabetical by name, base class first).
/// </summary>
#pragma warning disable CA1810 // Initialize reference type static fields inline - complex initialization required
public class NeatooOrdinalConverter<T> : JsonConverter<T> where T : IOrdinalSerializable
{
	private static readonly string[]? _propertyNames;
	private static readonly Type[]? _propertyTypes;
	private static readonly Func<object?[], object>? _fromOrdinalArray;

	/// <summary>
	/// Cache for fallback JsonSerializerOptions (with ordinal converter removed).
	/// This avoids creating new options on every fallback deserialization.
	/// </summary>
	private static readonly ConcurrentDictionary<JsonSerializerOptions, JsonSerializerOptions> fallbackOptionsCache = new();

	static NeatooOrdinalConverter()
	{
		// Try to get metadata from the type if it implements IOrdinalSerializationMetadata
		var metadataInterface = typeof(T).GetInterfaces()
			.FirstOrDefault(i => !i.IsGenericType && i.Name == nameof(IOrdinalSerializationMetadata));

		if (metadataInterface != null || typeof(IOrdinalSerializationMetadata).IsAssignableFrom(typeof(T)))
		{
			// Get static properties via reflection on the type itself
			var propertyNamesProperty = typeof(T).GetProperty("PropertyNames", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			var propertyTypesProperty = typeof(T).GetProperty("PropertyTypes", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
			var fromOrdinalMethod = typeof(T).GetMethod("FromOrdinalArray", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

			if (propertyNamesProperty != null)
			{
				_propertyNames = (string[]?)propertyNamesProperty.GetValue(null);
			}

			if (propertyTypesProperty != null)
			{
				_propertyTypes = (Type[]?)propertyTypesProperty.GetValue(null);
			}

			if (fromOrdinalMethod != null)
			{
				_fromOrdinalArray = (values) => fromOrdinalMethod.Invoke(null, new object[] { values })!;
			}
		}
	}
#pragma warning restore CA1810

	public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// Check if we're reading an array (ordinal format) or object (named format)
		if (reader.TokenType == JsonTokenType.StartArray)
		{
			return ReadOrdinal(ref reader, options);
		}
		else if (reader.TokenType == JsonTokenType.StartObject)
		{
			// Fall back to standard deserialization for named format
			return ReadNamed(ref reader, typeToConvert, options);
		}
		else if (reader.TokenType == JsonTokenType.Null)
		{
			return default;
		}

		throw new JsonException($"Expected StartArray or StartObject, got {reader.TokenType}");
	}

	private T? ReadOrdinal(ref Utf8JsonReader reader, JsonSerializerOptions options)
	{
		if (_propertyTypes == null || _fromOrdinalArray == null)
		{
			throw new JsonException($"Type {typeof(T).Name} does not have ordinal serialization metadata. Ensure it implements IOrdinalSerializationMetadata with static members.");
		}

		var values = new object?[_propertyTypes.Length];
		int index = 0;

		reader.Read(); // Move past StartArray

		while (reader.TokenType != JsonTokenType.EndArray)
		{
			if (index >= _propertyTypes.Length)
			{
				var propertyList = _propertyNames != null ? string.Join(", ", _propertyNames) : "unknown";
				throw new JsonException($"Too many values in ordinal array for type {typeof(T).Name}. Expected {_propertyTypes.Length} values (properties: {propertyList}).");
			}

			values[index] = JsonSerializer.Deserialize(ref reader, _propertyTypes[index], options);
			index++;
			reader.Read();
		}

		if (index != _propertyTypes.Length)
		{
			var propertyList = _propertyNames != null ? string.Join(", ", _propertyNames) : "unknown";
			throw new JsonException($"Not enough values in ordinal array for type {typeof(T).Name}. Expected {_propertyTypes.Length} values (properties: {propertyList}), got {index}.");
		}

		return (T?)_fromOrdinalArray(values);
	}

	private static T? ReadNamed(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		// Read as JSON document and deserialize using the standard deserializer
		// The ordinal converter won't be invoked for object format since it starts with StartObject
		using var document = JsonDocument.ParseValue(ref reader);
		var json = document.RootElement.GetRawText();

		// Use cached fallback options to avoid creating new options on every deserialization
		var fallbackOptions = fallbackOptionsCache.GetOrAdd(options, static originalOptions =>
		{
			var newOptions = new JsonSerializerOptions(originalOptions);
			// Remove ordinal converters to avoid recursion
			for (int i = newOptions.Converters.Count - 1; i >= 0; i--)
			{
				if (newOptions.Converters[i] is NeatooOrdinalConverterFactory)
				{
					newOptions.Converters.RemoveAt(i);
				}
			}
			return newOptions;
		});

		return JsonSerializer.Deserialize<T>(json, fallbackOptions);
	}

	public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
	{
		ArgumentNullException.ThrowIfNull(writer);

		if (value == null)
		{
			writer.WriteNullValue();
			return;
		}

		var values = value.ToOrdinalArray();

		writer.WriteStartArray();

		if (_propertyTypes != null && _propertyTypes.Length == values.Length)
		{
			for (int i = 0; i < values.Length; i++)
			{
				JsonSerializer.Serialize(writer, values[i], _propertyTypes[i], options);
			}
		}
		else
		{
			// Fallback: serialize each value using its runtime type
			foreach (var val in values)
			{
				if (val == null)
				{
					writer.WriteNullValue();
				}
				else
				{
					JsonSerializer.Serialize(writer, val, val.GetType(), options);
				}
			}
		}

		writer.WriteEndArray();
	}
}
