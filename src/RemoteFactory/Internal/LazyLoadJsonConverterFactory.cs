using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Claims <see cref="LazyLoad{T}"/> types and serializes them in named format
/// as <c>{"value": ..., "isLoaded": bool}</c>. Standalone converter factory
/// (not a <see cref="NeatooJsonConverterFactory"/> subclass) because it does
/// not require DI services.
/// </summary>
internal sealed class LazyLoadJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() == typeof(LazyLoad<>);
    }

    public override JsonConverter CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var innerType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(LazyLoadJsonConverter<>).MakeGenericType(innerType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// Converter for <see cref="LazyLoad{T}"/> that serializes as
/// <c>{"value": V, "isLoaded": true}</c> or <c>{"value": null, "isLoaded": false}</c>.
/// On deserialization, constructs <c>new LazyLoad&lt;T&gt;(value)</c> if loaded,
/// or <c>new LazyLoad&lt;T&gt;()</c> if not.
/// </summary>
internal sealed class LazyLoadJsonConverter<T> : JsonConverter<LazyLoad<T>> where T : class?
{
    public override LazyLoad<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"Expected StartObject token for LazyLoad<{typeof(T).Name}>, got {reader.TokenType}.");
        }

        T? value = default;
        bool isLoaded = false;
        bool hasValue = false;
        bool hasIsLoaded = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException($"Expected PropertyName token, got {reader.TokenType}.");
            }

            var propertyName = reader.GetString();
            reader.Read(); // Move to the value

            switch (propertyName)
            {
                case "value":
                    if (reader.TokenType == JsonTokenType.Null)
                    {
                        value = default;
                    }
                    else
                    {
                        value = JsonSerializer.Deserialize<T>(ref reader, options);
                    }
                    hasValue = true;
                    break;

                case "isLoaded":
                    isLoaded = reader.GetBoolean();
                    hasIsLoaded = true;
                    break;

                default:
                    reader.Skip();
                    break;
            }
        }

        if (!hasValue || !hasIsLoaded)
        {
            throw new JsonException($"LazyLoad<{typeof(T).Name}> JSON must contain both 'value' and 'isLoaded' properties.");
        }

        if (isLoaded)
        {
            return new LazyLoad<T>(value);
        }

        return new LazyLoad<T>();
    }

    public override void Write(Utf8JsonWriter writer, LazyLoad<T> value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();

        writer.WritePropertyName("value");
        if (value.Value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            JsonSerializer.Serialize(writer, value.Value, options);
        }

        writer.WriteBoolean("isLoaded", value.IsLoaded);

        writer.WriteEndObject();
    }
}
