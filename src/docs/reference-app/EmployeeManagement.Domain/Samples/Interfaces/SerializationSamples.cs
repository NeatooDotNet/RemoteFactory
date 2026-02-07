using System.Text.Json;
using System.Text.Json.Serialization;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Interfaces;

// Additional serialization examples - compiled but no longer extracted as duplicate snippets
// Primary snippets are in InterfacesSamples.cs
public class EmployeeSnapshot : IOrdinalSerializable
{
    public string DepartmentCode { get; set; } = "";
    public int EmployeeCount { get; set; }
    public DateTime LastUpdated { get; set; }

    public object?[] ToOrdinalArray() => [DepartmentCode, EmployeeCount, LastUpdated];
}

public class Money : IOrdinalConverterProvider<Money>
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    public static JsonConverter<Money> CreateOrdinalConverter() => new MoneyOrdinalConverter();
}

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
        reader.Read();
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
