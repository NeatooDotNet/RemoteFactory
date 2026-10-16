using System;
using System.Text.Json;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Reference identity for the generated ordinal converters, on the same
/// <see cref="NeatooReferenceResolver.Current"/> that STJ's collections and
/// Neatoo's entity converters use, so one graph shares one id sequence.
/// </summary>
/// <remarks>
/// <para>
/// An ordinal object's first appearance in a graph is written as
/// <c>{"$id":"1","$values":[...]}</c>; each later appearance is
/// <c>{"$ref":"1"}</c>. The shape matches STJ's preserved collections.
/// </para>
/// <para>
/// A bare array is still read, with no id: it is what a converter writes when no
/// resolver is active, and what 1.10.6 and earlier wrote.
/// </para>
/// <para>
/// Called by generated code; not intended for direct use.
/// </para>
/// </remarks>
public static class NeatooOrdinalReferences
{
	/// <summary>
	/// Starts writing <paramref name="value"/>. Returns <c>true</c> when the value was
	/// already written in this graph: a <c>$ref</c> was written and the caller is done.
	/// Otherwise the caller writes the ordinal array, then ends the wrapper with
	/// <see cref="WriteEnd"/>.
	/// </summary>
	/// <param name="wrapped"><c>true</c> when a <c>$id</c> wrapper was opened.</param>
	public static bool WriteReferenceOrStart(Utf8JsonWriter writer, object value, out bool wrapped)
	{
		ArgumentNullException.ThrowIfNull(writer);
		ArgumentNullException.ThrowIfNull(value);

		wrapped = false;
		var resolver = NeatooReferenceResolver.Current;
		if (resolver == null)
		{
			return false;
		}

		var id = resolver.GetReference(value, out var alreadyExists);
		writer.WriteStartObject();
		if (alreadyExists)
		{
			writer.WriteString("$ref", id);
			writer.WriteEndObject();
			return true;
		}

		writer.WriteString("$id", id);
		writer.WritePropertyName("$values");
		wrapped = true;
		return false;
	}

	/// <summary>
	/// Closes the <c>$id</c> wrapper opened by <see cref="WriteReferenceOrStart"/>.
	/// </summary>
	public static void WriteEnd(Utf8JsonWriter writer, bool wrapped)
	{
		ArgumentNullException.ThrowIfNull(writer);

		if (wrapped)
		{
			writer.WriteEndObject();
		}
	}

	/// <summary>
	/// Starts reading a <typeparamref name="T"/> from a non-null token. Returns
	/// <c>true</c> for a <c>$ref</c>: <paramref name="existing"/> is the object it
	/// names and the reader is on the reference's <c>EndObject</c>. Otherwise the
	/// reader is on the ordinal array's <c>StartArray</c> and <paramref name="id"/> is
	/// its <c>$id</c>, or <c>null</c> for a bare array.
	/// </summary>
	/// <param name="buildsFirst">
	/// <c>true</c> when the caller registers the object with <see cref="AddReference"/>
	/// before reading its properties. <c>false</c> when it is built from them, so a
	/// back reference read in the meantime cannot resolve and gets an error naming the type.
	/// </param>
	public static bool ReadReferenceOrStart<T>(ref Utf8JsonReader reader, bool buildsFirst, out T? existing, out string? id)
		where T : class
	{
		existing = null;
		id = null;

		if (reader.TokenType == JsonTokenType.StartArray)
		{
			return false;
		}

		if (reader.TokenType != JsonTokenType.StartObject)
		{
			throw new JsonException($"Expected an ordinal array, or a $id or $ref object, for {typeof(T).Name}; got {reader.TokenType}.");
		}

		reader.Read();
		if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("$ref"))
		{
			reader.Read();
			var referenceId = reader.GetString()
				?? throw new JsonException($"$ref for {typeof(T).Name} has no id.");
			existing = Resolve<T>(referenceId);
			reader.Read();
			if (reader.TokenType != JsonTokenType.EndObject)
			{
				throw new JsonException($"Expected the end of the $ref object for {typeof(T).Name}; got {reader.TokenType}.");
			}
			return true;
		}

		if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("$id"))
		{
			throw new JsonException($"Expected $id or $ref as the first property of the ordinal object for {typeof(T).Name}.");
		}

		reader.Read();
		id = reader.GetString()
			?? throw new JsonException($"$id for {typeof(T).Name} has no id.");

		reader.Read();
		if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("$values"))
		{
			throw new JsonException($"Expected $values after $id in the ordinal object for {typeof(T).Name}.");
		}

		reader.Read();
		if (reader.TokenType != JsonTokenType.StartArray)
		{
			throw new JsonException($"Expected StartArray for {typeof(T).Name} ordinal format, got {reader.TokenType}");
		}

		if (!buildsFirst)
		{
			NeatooReferenceResolver.Current?.AddPendingReference(id, typeof(T));
		}

		return false;
	}

	/// <summary>
	/// Registers <paramref name="value"/> under the <paramref name="id"/> read by
	/// <see cref="ReadReferenceOrStart{T}"/>. Does nothing for a bare array.
	/// </summary>
	public static void AddReference(string? id, object value)
	{
		ArgumentNullException.ThrowIfNull(value);

		if (id != null)
		{
			NeatooReferenceResolver.Current?.AddReference(id, value);
		}
	}

	/// <summary>
	/// Reads past the end of the <c>$id</c> wrapper, after the ordinal array's
	/// <c>EndArray</c>. Does nothing for a bare array.
	/// </summary>
	public static void ReadEnd(ref Utf8JsonReader reader, string? id)
	{
		if (id == null)
		{
			return;
		}

		reader.Read();
		if (reader.TokenType != JsonTokenType.EndObject)
		{
			throw new JsonException($"Expected the end of the ordinal object after $values; got {reader.TokenType}.");
		}
	}

	private static T Resolve<T>(string referenceId)
		where T : class
	{
		var resolver = NeatooReferenceResolver.Current
			?? throw new JsonException($"Cannot resolve $ref '{referenceId}' for {typeof(T).Name}: no NeatooReferenceResolver is active. Use NeatooJsonSerializer for deserialization.");

		if (resolver.TryResolveReference(referenceId, out var value, out var pendingType))
		{
			return value as T
				?? throw new JsonException($"$ref '{referenceId}' names a {value?.GetType().Name}, not a {typeof(T).Name}.");
		}

		if (pendingType != null)
		{
			throw new JsonException(
				$"$ref '{referenceId}' is a back reference to {pendingType.Name}, which is still being read. " +
				$"{pendingType.Name} is built from its properties (it has an init-only or required property), " +
				"so it does not exist until they are all read. Give its ordinal properties plain setters, or remove the back reference.");
		}

		throw new JsonException($"$ref '{referenceId}' for {typeof(T).Name} names no object read earlier in this graph.");
	}
}
