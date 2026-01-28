using System.Text.Json;
using System.Text.Json.Serialization;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Interfaces;

#region interfaces-ordinalserializable
/// <summary>
/// Value object implementing IOrdinalSerializable for compact JSON serialization.
/// </summary>
public class EmployeeSnapshot : IOrdinalSerializable
{
    public string DepartmentCode { get; set; } = "";  // Index 0 (alphabetically first)
    public int EmployeeCount { get; set; }            // Index 1
    public DateTime LastUpdated { get; set; }          // Index 2

    /// <summary>
    /// Converts the object to an array of property values in alphabetical order.
    /// </summary>
    public object?[] ToOrdinalArray()
    {
        // Properties in alphabetical order: DepartmentCode, EmployeeCount, LastUpdated
        return [DepartmentCode, EmployeeCount, LastUpdated];
    }
}

// JSON comparison:
// Array format:  ["HR", 42, "2024-01-15T10:30:00Z"]
// Object format: {"DepartmentCode":"HR","EmployeeCount":42,"LastUpdated":"2024-01-15T10:30:00Z"}
#endregion

#region interfaces-ordinalconverterprovider
// IOrdinalConverterProvider<TSelf> enables types to provide their own ordinal converter

/// <summary>
/// Money value object implementing IOrdinalConverterProvider for custom ordinal serialization.
/// </summary>
public class Money : IOrdinalConverterProvider<Money>
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Creates the ordinal converter for this type.
    /// </summary>
    public static JsonConverter<Money> CreateOrdinalConverter()
    {
        return new MoneyOrdinalConverter();
    }
}

/// <summary>
/// Custom ordinal converter for Money value object.
/// </summary>
public class MoneyOrdinalConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array");

        reader.Read();
        var amount = reader.GetDecimal();

        reader.Read();
        var currency = reader.GetString() ?? "USD";

        reader.Read(); // EndArray

        return new Money { Amount = amount, Currency = currency };
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Amount);
        writer.WriteStringValue(value.Currency);
        writer.WriteEndArray();
    }
}
#endregion
