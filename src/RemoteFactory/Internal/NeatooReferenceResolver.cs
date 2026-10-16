using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading;

namespace Neatoo.RemoteFactory.Internal;

public sealed class NeatooReferenceResolver : ReferenceResolver, IDisposable
{
	private static readonly AsyncLocal<NeatooReferenceResolver?> _current = new();

	/// <summary>
	/// Per-async-flow accessor for the active resolver.
	/// Set by NeatooJsonSerializer before each serialize/deserialize call; cleared in finally.
	/// Downstream converters (including Neatoo) read this to access reference tracking.
	/// Returns null when no serialization operation is in progress.
	/// </summary>
	public static NeatooReferenceResolver? Current
	{
		get => _current.Value;
		internal set => _current.Value = value;
	}

	private uint _referenceCount;
	private Dictionary<string, object> _referenceIdToObjectMap = new Dictionary<string, object>();
	private Dictionary<object, string> _objectToReferenceIdMap = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);

	// Ordinal objects whose $id has been read but which are not built yet: their
	// properties are read before construction (init-only, required, or a record).
	private Dictionary<string, Type> _pendingReferenceIdToTypeMap = new Dictionary<string, Type>();

	public void Dispose()
	{
		this._referenceCount = 0;
		this._referenceIdToObjectMap.Clear();
		this._objectToReferenceIdMap.Clear();
		this._pendingReferenceIdToTypeMap.Clear();
	}

	public override void AddReference(string referenceId, object value)
	{
		if (!this._referenceIdToObjectMap.TryAdd(referenceId, value))
		{
			throw new JsonException();
		}
		this._pendingReferenceIdToTypeMap.Remove(referenceId);
	}

	internal void AddPendingReference(string referenceId, Type type)
	{
		this._pendingReferenceIdToTypeMap[referenceId] = type;
	}

	internal bool TryResolveReference(string referenceId, out object? value, out Type? pendingType)
	{
		pendingType = null;
		if (this._referenceIdToObjectMap.TryGetValue(referenceId, out value))
		{
			return true;
		}
		this._pendingReferenceIdToTypeMap.TryGetValue(referenceId, out pendingType);
		return false;
	}

	public bool AlreadyExists(object reference)
	{
		if (this._objectToReferenceIdMap.ContainsKey(reference))
		{
			return true;
		}
		return false;
	}

	public override string GetReference(object value, out bool alreadyExists)
	{
		ArgumentNullException.ThrowIfNull(value, nameof(value));

		if (this._objectToReferenceIdMap.TryGetValue(value, out var referenceId))
		{
			alreadyExists = true;
		}
		else
		{
			this._referenceCount++;
			referenceId = this._referenceCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
			this._objectToReferenceIdMap.Add(value, referenceId);
			alreadyExists = false;
		}

		return referenceId;
	}

	public override object ResolveReference(string referenceId)
	{
		if (!this._referenceIdToObjectMap.TryGetValue(referenceId, out var value))
		{
			throw new JsonException();
		}

		return value;
	}
}
