using System.Text.Json.Serialization;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Implemented by [Factory] types to provide a pre-compiled ordinal JSON converter.
/// Eliminates reflection-based converter creation for AOT compatibility.
/// </summary>
/// <typeparam name="TSelf">The implementing type (self-referencing generic).</typeparam>
public interface IOrdinalConverterProvider<TSelf> where TSelf : class
{
	/// <summary>
	/// Creates a strongly-typed ordinal converter for this type.
	/// </summary>
	static abstract JsonConverter<TSelf> CreateOrdinalConverter();
}
