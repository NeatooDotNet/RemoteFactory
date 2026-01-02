namespace Neatoo.RemoteFactory;

/// <summary>
/// Interface for types that support ordinal (positional) JSON serialization.
/// Types implementing this interface can be serialized as JSON arrays instead of objects,
/// eliminating property names from the serialized output for reduced payload size.
///
/// The source generator automatically implements this interface for types with [Factory] attribute.
/// Properties are serialized in alphabetical order by name. For inherited types, base class
/// properties come first (alphabetically), followed by derived class properties (alphabetically).
/// </summary>
public interface IOrdinalSerializable
{
	/// <summary>
	/// Converts the object to an array of property values in ordinal order.
	/// The order is alphabetical by property name, with base class properties first.
	/// </summary>
	/// <returns>An array containing property values in the defined ordinal order.</returns>
	object?[] ToOrdinalArray();
}

/// <summary>
/// Provides metadata about ordinal serialization for a specific type.
/// This information is used by the serializer to reconstruct objects from ordinal arrays.
/// </summary>
public interface IOrdinalSerializationMetadata
{
	/// <summary>
	/// Gets the property names in ordinal order (alphabetical, base properties first).
	/// </summary>
#pragma warning disable CA1819 // Properties should not return arrays - required for source generator compatibility
	static abstract string[] PropertyNames { get; }
#pragma warning restore CA1819

	/// <summary>
	/// Gets the property types in ordinal order, matching PropertyNames.
	/// </summary>
#pragma warning disable CA1819 // Properties should not return arrays - required for source generator compatibility
	static abstract Type[] PropertyTypes { get; }
#pragma warning restore CA1819

	/// <summary>
	/// Creates an instance from an array of property values in ordinal order.
	/// </summary>
	/// <param name="values">The property values in ordinal order.</param>
	/// <returns>A new instance populated with the provided values.</returns>
	static abstract object FromOrdinalArray(object?[] values);
}
