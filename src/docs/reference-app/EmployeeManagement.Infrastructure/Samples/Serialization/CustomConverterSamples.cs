using System.Text.Json;
using System.Text.Json.Serialization;

namespace EmployeeManagement.Infrastructure.Samples.Serialization;

// Custom JsonConverter example - for types that cannot use [Factory].
// Note: The primary serialization-custom-converter snippet is in Domain/Samples/Serialization.

public class PhoneNumberValue(string countryCode, string number)
{
    public string CountryCode { get; } = countryCode ?? throw new ArgumentNullException(nameof(countryCode));
    public string Number { get; } = number ?? throw new ArgumentNullException(nameof(number));
    public override string ToString() => $"+{CountryCode} {Number}";
}

public class PhoneNumberConverter : JsonConverter<PhoneNumberValue>
{
    public override PhoneNumberValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString() ?? throw new JsonException("PhoneNumber cannot be null");
        var trimmed = value.TrimStart('+');
        var spaceIndex = trimmed.IndexOf(' ');
        if (spaceIndex < 0) throw new JsonException("Invalid phone format. Expected '+CountryCode Number'");
        return new PhoneNumberValue(trimmed[..spaceIndex], trimmed[(spaceIndex + 1)..]);
    }

    public override void Write(Utf8JsonWriter writer, PhoneNumberValue value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}

#region serialization-json-options
// RemoteFactory manages JsonSerializerOptions internally.
// Use IOrdinalConverterProvider<T> for custom Ordinal serialization.
// Use JsonConverter<T> (as above) for Named format only.
#endregion
