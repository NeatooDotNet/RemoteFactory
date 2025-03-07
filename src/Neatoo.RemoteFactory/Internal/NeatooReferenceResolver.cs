using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Neatoo.RemoteFactory.Internal;

public sealed class NeatooReferenceResolver : ReferenceResolver, IDisposable
{
	private uint _referenceCount;
	private Dictionary<string, object> _referenceIdToObjectMap = new Dictionary<string, object>();
	private Dictionary<object, string> _objectToReferenceIdMap = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);

	public void Dispose()
	{
		this._referenceCount = 0;
		this._referenceIdToObjectMap.Clear();
		this._objectToReferenceIdMap.Clear();
	}

	public override void AddReference(string referenceId, object value)
	{
		if (!this._referenceIdToObjectMap.TryAdd(referenceId, value))
		{
			throw new JsonException();
		}
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
		var type = value.GetType();
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
		{
			alreadyExists = false;
			return string.Empty;
		}

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
