using System.Text.Json;
using System.Text.Json.Serialization;

namespace EmployeeManagement.Infrastructure.Samples.Serialization;

#region serialization-custom-converter
/// <summary>
/// Phone number value object for demonstrating custom Named format serialization.
/// </summary>
public class PhoneNumberValue
{
    public string CountryCode { get; }
    public string Number { get; }

    public PhoneNumberValue(string countryCode, string number)
    {
        CountryCode = countryCode ?? throw new ArgumentNullException(nameof(countryCode));
        Number = number ?? throw new ArgumentNullException(nameof(number));
    }

    public override string ToString() => $"+{CountryCode} {Number}";
}

/// <summary>
/// Custom JsonConverter for Named format serialization.
/// Serializes PhoneNumber as a single string: "+1 5551234567"
/// </summary>
public class PhoneNumberConverter : JsonConverter<PhoneNumberValue>
{
    public override PhoneNumberValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            throw new JsonException("PhoneNumber value cannot be null or empty");

        // Parse "+1 5551234567" format
        var trimmed = value.TrimStart('+');
        var spaceIndex = trimmed.IndexOf(' ');

        if (spaceIndex < 0)
            throw new JsonException("Invalid phone number format. Expected '+CountryCode Number'");

        var countryCode = trimmed[..spaceIndex];
        var number = trimmed[(spaceIndex + 1)..];

        return new PhoneNumberValue(countryCode, number);
    }

    public override void Write(Utf8JsonWriter writer, PhoneNumberValue value, JsonSerializerOptions options)
    {
        // Write as single string: "+1 5551234567"
        writer.WriteStringValue(value.ToString());
    }
}
#endregion

#region serialization-json-options
/// <summary>
/// Demonstrates creating custom JsonSerializerOptions (for reference/comparison).
/// Note: RemoteFactory manages its own JsonSerializerOptions internally.
/// </summary>
public static class JsonOptionsFactory
{
    public static JsonSerializerOptions CreateCustomOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Add custom converters
        options.Converters.Add(new PhoneNumberConverter());
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}
// RemoteFactory manages its own JsonSerializerOptions internally.
// For custom type serialization with Ordinal format, use IOrdinalConverterProvider<T> instead.
#endregion
