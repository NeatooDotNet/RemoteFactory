namespace Neatoo.RemoteFactory;

/// <summary>
/// Configuration options for Neatoo RemoteFactory serialization.
/// </summary>
public class NeatooSerializationOptions
{
	/// <summary>
	/// The serialization format to use for remote factory communication.
	/// Default is <see cref="SerializationFormat.Ordinal"/> (compact array format).
	/// </summary>
	public SerializationFormat Format { get; set; } = SerializationFormat.Ordinal;

	/// <summary>
	/// HTTP header name used to communicate the serialization format between client and server.
	/// </summary>
	public const string FormatHeaderName = "X-Neatoo-Format";

	/// <summary>
	/// Gets the header value for the current format setting.
	/// </summary>
	public string FormatHeaderValue => Format == SerializationFormat.Ordinal ? "ordinal" : "named";

	/// <summary>
	/// Parses a header value string into a SerializationFormat.
	/// </summary>
	/// <param name="headerValue">The header value to parse.</param>
	/// <returns>The parsed format, defaulting to Ordinal if unrecognized.</returns>
	public static SerializationFormat ParseHeaderValue(string? headerValue)
	{
		return headerValue?.ToUpperInvariant() switch
		{
			"NAMED" => SerializationFormat.Named,
			"ORDINAL" => SerializationFormat.Ordinal,
			_ => SerializationFormat.Ordinal
		};
	}
}
