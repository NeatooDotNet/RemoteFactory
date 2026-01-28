using System.Text.Json;
using System.Text.Json.Serialization;

namespace EmployeeManagement.Domain.Samples.Serialization;

#region serialization-custom-ordinal
// Custom ordinal serialization is for types NOT managed by [Factory].
// Types with [Factory] get ordinal converters automatically.

/// <summary>
/// Value object for employee salary (not using [Factory] attribute).
/// Requires custom ordinal serialization.
/// </summary>
public class Salary
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Salary(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }
}

/// <summary>
/// Custom JsonConverter for Salary ordinal serialization.
/// Serializes as array: [amount, currency]
/// </summary>
public class SalaryOrdinalConverter : JsonConverter<Salary>
{
    public override Salary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for Salary");

        reader.Read();
        var amount = reader.GetDecimal();

        reader.Read();
        var currency = reader.GetString() ?? "USD";

        reader.Read(); // EndArray

        return new Salary(amount, currency);
    }

    public override void Write(Utf8JsonWriter writer, Salary value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Amount);
        writer.WriteStringValue(value.Currency);
        writer.WriteEndArray();
    }
}
// Types with [Factory] attribute get ordinal converters automatically via IOrdinalConverterProvider<T>.
// Only implement custom converters for third-party types or value objects without [Factory].
#endregion
