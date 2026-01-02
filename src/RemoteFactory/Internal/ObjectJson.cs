using System;
using System.Collections.Generic;
using System.Text;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Wrapper for serialized object data with type information.
/// The Json property contains the serialized content (array for ordinal format, object for named format).
/// </summary>
public class ObjectJson
{
	/// <summary>
	/// The serialized JSON content.
	/// For ordinal format: JSON array like ["value1", 42, true]
	/// For named format: JSON object like {"Name":"value1","Age":42,"Active":true}
	/// </summary>
	public string Json { get; }

	/// <summary>
	/// The full type name for deserialization.
	/// </summary>
	public string AssemblyType { get; }

	public ObjectJson(string json, string assemblyType)
	{
		this.Json = json;
		this.AssemblyType = assemblyType;
	}
}
