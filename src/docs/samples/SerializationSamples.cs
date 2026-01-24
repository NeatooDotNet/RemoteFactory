using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.Samples.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neatoo.RemoteFactory.Samples;

/// <summary>
/// Code samples for docs/serialization.md documentation.
/// Tests and non-Factory types are nested in this class.
/// Factory types are at namespace level (below) for proper source generation.
/// </summary>
public partial class SerializationSamples
{
    #region serialization-config
    public static class SerializationConfiguration
    {
        public static void ConfigureOrdinalFormat(IServiceCollection services)
        {
            // Ordinal format: compact array-based JSON (default)
            // Properties serialized as: [value1, value2, value3]
            var options = new NeatooSerializationOptions
            {
                Format = SerializationFormat.Ordinal
            };

            services.AddNeatooAspNetCore(options, typeof(SerializationSamples).Assembly);
        }

        public static void ConfigureNamedFormat(IServiceCollection services)
        {
            // Named format: traditional object-based JSON
            // Properties serialized as: {"Property1": value1, "Property2": value2}
            var options = new NeatooSerializationOptions
            {
                Format = SerializationFormat.Named
            };

            services.AddNeatooAspNetCore(options, typeof(SerializationSamples).Assembly);
        }
    }
    #endregion

    #region serialization-custom-ordinal
    // Custom ordinal serialization for types NOT managed by [Factory].
    // For [Factory] types, the generator creates converters automatically.

    // Value object without [Factory] - requires custom converter
    public partial class Money
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
    }

    // Custom ordinal converter for Money value object
    public partial class MoneyOrdinalConverter : JsonConverter<Money>
    {
        public override Money? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected array for Money");

            reader.Read();
            var amount = reader.GetDecimal();
            reader.Read();
            var currency = reader.GetString() ?? "USD";
            reader.Read(); // End array

            return new Money { Amount = amount, Currency = currency };
        }

        public override void Write(
            Utf8JsonWriter writer,
            Money value,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.Amount);
            writer.WriteStringValue(value.Currency);
            writer.WriteEndArray();
        }
    }

    // For [Factory] types, IOrdinalConverterProvider<T> is generated automatically.
    // See MoneyWithFactory below for the pattern the generator produces.
    #endregion

    #region serialization-polymorphism
    public abstract class Shape
    {
        public Guid Id { get; set; }
        public string Color { get; set; } = "Black";
    }

    public partial class Circle : Shape
    {
        public double Radius { get; set; }
    }

    public partial class Rectangle : Shape
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }
    #endregion


    #region serialization-custom-converter
    public partial class PhoneNumber
    {
        public string CountryCode { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;

        public override string ToString() => $"+{CountryCode} {Number}";
    }

    public partial class PhoneNumberConverter : JsonConverter<PhoneNumber>
    {
        public override PhoneNumber? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return null;

            var parts = value.Split(' ', 2);
            return new PhoneNumber
            {
                CountryCode = parts[0].TrimStart('+'),
                Number = parts.Length > 1 ? parts[1] : string.Empty
            };
        }

        public override void Write(Utf8JsonWriter writer, PhoneNumber value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    // Register custom converter in serialization options
    public static class CustomConverterRegistration
    {
        public static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new PhoneNumberConverter());
            return options;
        }
    }
    #endregion

    #region serialization-logging
    // Enable serialization logging for debugging
    public static class SerializationLogging
    {
        public static void ConfigureWithLogging(IServiceCollection services)
        {
            // Add logging to see serialization details
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);

                // Filter to Neatoo categories for serialization logs
                builder.AddFilter("Neatoo.RemoteFactory", Microsoft.Extensions.Logging.LogLevel.Trace);
            });

            services.AddNeatooAspNetCore(typeof(SerializationSamples).Assembly);
        }
    }
    #endregion

    #region serialization-debug-named
    public static class DebugSerializationConfig
    {
        public static void ConfigureForDevelopment(IServiceCollection services, bool isDevelopment)
        {
            var options = new NeatooSerializationOptions
            {
                // Use Named format in development for readable JSON
                // Use Ordinal format in production for smaller payloads
                Format = isDevelopment
                    ? SerializationFormat.Named
                    : SerializationFormat.Ordinal
            };

            services.AddNeatooAspNetCore(options, typeof(SerializationSamples).Assembly);
        }
    }

    // Named format example (development):
    // {"Id":"550e8400-e29b-41d4-a716-446655440000","Name":"Test","Price":29.99}
    //
    // Ordinal format example (production):
    // ["550e8400-e29b-41d4-a716-446655440000","Test",29.99]
    #endregion

    #region serialization-json-options
    public static class CustomJsonOptions
    {
        public static void ConfigureCustomOptions(IServiceCollection services)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            // Add custom converters
            jsonOptions.Converters.Add(new PhoneNumberConverter());
            jsonOptions.Converters.Add(new JsonStringEnumConverter());

            // Note: RemoteFactory manages its own JsonSerializerOptions internally
            // Custom converters can be registered via IOrdinalConverterProvider
        }
    }
    #endregion

    // Tests
    [Fact]
    public void OrdinalExample_ToOrdinalArray_Works()
    {
        // OrdinalExample has [Factory] so ToOrdinalArray is generated
        var entity = new SerializationOrdinalExample
        {
            Alpha = "test",
            Beta = 42,
            Gamma = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // ToOrdinalArray is auto-generated by the source generator
        var array = entity.ToOrdinalArray();

        Assert.Equal(3, array.Length);
        Assert.Equal("test", array[0]);  // Alpha - alphabetically first
        Assert.Equal(42, array[1]);       // Beta
    }

    [Fact]
    public async Task CollectionExample_Serializes()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<ISerializationCollectionExampleFactory>();

        // Fetch returns a new instance (since Fetch has no parameters)
        var fetched = await factory.Fetch();

        Assert.NotNull(fetched);
        Assert.Equal(3, fetched.Tags.Count);
        Assert.Equal(2, fetched.Categories.Length);
        Assert.Equal(2, fetched.Counts.Count);
    }

    [Fact]
    public void ValidationExample_Works()
    {
        var entity = new SerializationValidatedEntity
        {
            Name = "A" // Too short - violates MinimumLength = 2
        };

        var isValid = ClientValidationExample.ValidateBeforeSave(entity);
        Assert.False(isValid);

        entity.Name = "Valid Name";
        isValid = ClientValidationExample.ValidateBeforeSave(entity);
        Assert.True(isValid);
    }

    [Fact]
    public void MoneyConverter_RoundTrips()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new MoneyOrdinalConverter());

        var money = new Money { Amount = 99.99m, Currency = "EUR" };

        // Serialize to ordinal format
        var json = JsonSerializer.Serialize(money, options);
        Assert.Equal("[99.99,\"EUR\"]", json);

        // Deserialize back
        var deserialized = JsonSerializer.Deserialize<Money>(json, options);
        Assert.NotNull(deserialized);
        Assert.Equal(99.99m, deserialized.Amount);
        Assert.Equal("EUR", deserialized.Currency);
    }
}

// ============================================================================
// Factory types at namespace level for proper source generation
// ============================================================================

#region serialization-ordinal-generated
// RemoteFactory automatically generates ordinal serialization for [Factory] types.
// Properties are serialized in alphabetical order.
[Factory]
public partial class SerializationOrdinalExample
{
    public string Alpha { get; set; } = string.Empty;    // Index 0 (alphabetically first)
    public int Beta { get; set; }                         // Index 1
    public DateTime Gamma { get; set; }                   // Index 2

    [Create]
    public SerializationOrdinalExample() { }

    // The generator automatically implements:
    // - IOrdinalSerializable.ToOrdinalArray()
    // - IOrdinalConverterProvider<SerializationOrdinalExample>.CreateOrdinalConverter()
    // - IOrdinalSerializationMetadata (PropertyNames, PropertyTypes, FromOrdinalArray)
}

// JSON output in Ordinal format: ["value", 42, "2024-01-15T10:30:00Z"]
// JSON output in Named format: {"Alpha":"value","Beta":42,"Gamma":"2024-01-15T10:30:00Z"}
#endregion

#region serialization-ordinal-versioning
// Versioning strategy for ordinal serialization:
// - Add new properties at the END alphabetically to maintain compatibility
// - Never remove or rename existing properties
// - Never change property types

[Factory]
public partial class SerializationVersionedEntity
{
    // Original properties (v1)
    public string Alpha { get; set; } = string.Empty;    // Index 0
    public int Beta { get; set; }                         // Index 1

    // Added in v2 - comes after Beta alphabetically
    public string? Gamma { get; set; }                    // Index 2

    // Added in v3 - comes after Gamma alphabetically
    public decimal? Zeta { get; set; }                    // Index 3

    [Create]
    public SerializationVersionedEntity() { }

    // Generator produces: [Alpha, Beta, Gamma, Zeta] in ordinal format
}
#endregion

// For [Factory] types, IOrdinalConverterProvider<T> is generated automatically.
// This example shows the pattern the generator produces:
[Factory]
public partial class MoneyWithFactory
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    [Create]
    public MoneyWithFactory() { }

    // Generator automatically implements:
    // static JsonConverter<MoneyWithFactory> CreateOrdinalConverter()
    // which returns a generated converter like MoneyWithFactoryOrdinalConverter
}

#region serialization-references
// Object references and circular reference handling

[Factory]
public partial class SerializationParentEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<SerializationChildEntity> Children { get; set; } = new();

    [Create]
    public SerializationParentEntity() { Id = Guid.NewGuid(); }
}

[Factory]
public partial class SerializationChildEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SerializationParentEntity? Parent { get; set; } // Circular reference

    [Create]
    public SerializationChildEntity() { Id = Guid.NewGuid(); }
}

// RemoteFactory uses NeatooReferenceHandler to:
// - Detect circular references
// - Preserve object identity across serialization
// - Avoid infinite loops in serialization
#endregion

#region serialization-interface
public interface ISerializationProduct
{
    Guid Id { get; }
    string Name { get; set; }
    decimal Price { get; set; }
}

[Factory]
public partial class SerializationConcreteProduct : ISerializationProduct
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Sku { get; set; } = string.Empty; // Additional property

    [Create]
    public SerializationConcreteProduct() { Id = Guid.NewGuid(); }
}

// When serializing ISerializationProduct, RemoteFactory includes type information
// to deserialize back to SerializationConcreteProduct
#endregion

#region serialization-collections
[Factory]
public partial class SerializationCollectionExample
{
    public Guid Id { get; private set; }
    public List<string> Tags { get; set; } = new();
    public string[] Categories { get; set; } = [];
    public Dictionary<string, int> Counts { get; set; } = new();

    [Create]
    public SerializationCollectionExample() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public Task Fetch()
    {
        Tags = ["tag1", "tag2", "tag3"];
        Categories = ["cat1", "cat2"];
        Counts = new() { ["a"] = 1, ["b"] = 2 };
        return Task.CompletedTask;
    }
}
#endregion

// Polymorphism example - Drawing uses Shape types from SerializationSamples
[Factory]
public partial class SerializationDrawing
{
    public Guid Id { get; private set; }
    public List<SerializationSamples.Shape> Shapes { get; set; } = new();

    [Create]
    public SerializationDrawing() { Id = Guid.NewGuid(); }

    [Create]
    public void Initialize()
    {
        Shapes.Add(new SerializationSamples.Circle { Id = Guid.NewGuid(), Color = "Red", Radius = 5.0 });
        Shapes.Add(new SerializationSamples.Rectangle { Id = Guid.NewGuid(), Color = "Blue", Width = 10.0, Height = 5.0 });
    }
}

// RemoteFactory serializes polymorphic types with type discriminators
// to correctly deserialize derived types

#region serialization-validation
[Factory]
public partial class SerializationValidatedEntity
{
    public Guid Id { get; private set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2-100 characters")]
    public string Name { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [Range(0, 1000, ErrorMessage = "Quantity must be 0-1000")]
    public int Quantity { get; set; }

    [Create]
    public SerializationValidatedEntity() { Id = Guid.NewGuid(); }
}

public partial class ClientValidationExample
{
    public static bool ValidateBeforeSave(SerializationValidatedEntity entity)
    {
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            entity,
            new ValidationContext(entity),
            validationResults,
            validateAllProperties: true);

        if (!isValid)
        {
            foreach (var result in validationResults)
            {
                Console.WriteLine($"Validation error: {result.ErrorMessage}");
            }
        }

        return isValid;
    }
}
#endregion

#region serialization-validation-server
[Factory]
public partial class SerializationServerValidatedEntity : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public SerializationServerValidatedEntity() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert([Service] IPersonRepository repository)
    {
        // Server-side validation before persisting
        if (string.IsNullOrWhiteSpace(Name))
            throw new ValidationException("Name is required");

        if (Name.Length > 100)
            throw new ValidationException("Name cannot exceed 100 characters");

        IsNew = false;
        return Task.CompletedTask;
    }
}
#endregion
