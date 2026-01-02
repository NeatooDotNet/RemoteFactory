namespace Neatoo.RemoteFactory;

/// <summary>
/// Specifies the JSON serialization format used for remote factory communication.
/// </summary>
public enum SerializationFormat
{
	/// <summary>
	/// Compact array-based format that eliminates property names.
	/// Properties are serialized in alphabetical order by name.
	/// Example: ["John", 42, true] instead of {"Name":"John","Age":42,"Active":true}
	/// This is the default format for reduced payload size.
	/// </summary>
	Ordinal = 0,

	/// <summary>
	/// Verbose object-based format with property names.
	/// Example: {"Name":"John","Age":42,"Active":true}
	/// Use for debugging or backwards compatibility.
	/// </summary>
	Named = 1
}
