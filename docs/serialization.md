# Serialization

RemoteFactory uses System.Text.Json with custom converters to serialize domain models across the client-server boundary.

## Serialization Formats

RemoteFactory supports two formats:

### Ordinal Format (Default)

Compact array-based serialization. Properties serialized in alphabetical order without property names.

**Example object:**
```csharp
new Person { FirstName = "John", LastName = "Doe", Age = 42, Active = true }
```

**Ordinal JSON:**
```json
[true, 42, "John", "Doe"]
```

Properties: Active, Age, FirstName, LastName (alphabetical)

**Advantages:**
- 40-50% smaller payloads
- Faster serialization
- Lower bandwidth costs

**Disadvantages:**
- Harder to debug (no property names)
- Breaks if property order changes (regenerate with rebuild)

### Named Format

Standard JSON with property names.

**Named JSON:**
```json
{
  "Active": true,
  "Age": 42,
  "FirstName": "John",
  "LastName": "Doe"
}
```

**Advantages:**
- Human-readable
- Easier debugging
- Backwards compatible with property additions

**Disadvantages:**
- Larger payloads
- Slower serialization

## Configuration

Configure format during DI registration:

<!-- snippet: serialization-config -->
<a id='snippet-serialization-config'></a>
```cs
public static void ConfigureOrdinalFormat(IServiceCollection services)
{
    // Ordinal format (default): compact array-based JSON
    // Properties serialized as: [value1, value2, value3]
    services.AddNeatooAspNetCore(
        new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
        typeof(SerializationSamples).Assembly);
}

public static void ConfigureNamedFormat(IServiceCollection services)
{
    // Named format: traditional object-based JSON
    // Properties serialized as: {"Property1": value1, "Property2": value2}
    services.AddNeatooAspNetCore(
        new NeatooSerializationOptions { Format = SerializationFormat.Named },
        typeof(SerializationSamples).Assembly);
}
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L18-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Both client and server must use the same format.

### Format Negotiation

RemoteFactory adds the `X-Neatoo-Format` header to responses:

```http
X-Neatoo-Format: ordinal
```

Clients can detect format mismatches and log warnings.

## How Ordinal Serialization Works

### Generated Code

For each factory-enabled type, RemoteFactory generates serialization methods:

<!-- snippet: serialization-ordinal-generated -->
<a id='snippet-serialization-ordinal-generated'></a>
```cs
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
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L259-L280' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-ordinal-generated' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Property Ordering

Properties are sorted alphabetically by name:

```csharp
class Person
{
    public int Age { get; set; }           // [0] - alphabetically first
    public string FirstName { get; set; }  // [1]
    public string LastName { get; set; }   // [2]
}
```

Array indices: `[Age, FirstName, LastName]` → `[0, 1, 2]`

### Versioning Considerations

Adding properties is safe (new elements appended):

<!-- snippet: serialization-ordinal-versioning -->
<a id='snippet-serialization-ordinal-versioning'></a>
```cs
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
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L282-L306' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-ordinal-versioning' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Removing or renaming properties breaks compatibility. Use semantic versioning and coordinate client/server updates.

## IOrdinalSerializable Interface

Implement `IOrdinalSerializable` to customize ordinal serialization:

<!-- snippet: serialization-custom-ordinal -->
<a id='snippet-serialization-custom-ordinal'></a>
```cs
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
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L38-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-custom-ordinal' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This overrides the generated serialization methods.

## Type Support

RemoteFactory serializes:

**Primitives:**
- `int`, `long`, `decimal`, `double`, `float`, `bool`, `string`, `DateTime`, `Guid`, `byte[]`

**Nullables:**
- `int?`, `DateTime?`, etc.

**Collections:**
- `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`, `T[]`
- `Dictionary<TKey, TValue>`

**Enums:**
- All enum types

**Factory-enabled types:**
- Any type with `[Factory]` attribute

**Interfaces:**
- Interface properties serialized as implementing type

**Records:**
- Full support including positional parameters and init properties

**Circular references:**
- Handled automatically via reference tracking

### Unsupported Types

These types are not serializable:
- Delegates and function pointers
- Unmanaged pointers
- Types without parameterless constructors (unless using records)

## Reference Handling

RemoteFactory preserves object identity:

<!-- snippet: serialization-references -->
<a id='snippet-serialization-references'></a>
```cs
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
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L324-L353' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-references' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The same instance is shared, not duplicated.

### How It Works

RemoteFactory uses `NeatooReferenceHandler` for reference preservation:

```json
{
  "$id": "1",
  "Name": "Parent",
  "Children": [
    {
      "$id": "2",
      "Name": "Child",
      "Parent": { "$ref": "1" }
    }
  ]
}
```

Circular references are encoded as `$ref` pointers.

## Interface Serialization

Interfaces serialize as their concrete implementation:

<!-- snippet: serialization-interface -->
<a id='snippet-serialization-interface'></a>
```cs
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
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L355-L377' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Serialized as:
```json
{
  "$type": "ConcreteProduct",
  "Id": "550e8400-e29b-41d4-a716-446655440000",
  "Name": "Widget",
  "Price": 29.99,
  "Sku": "WDG-001"
}
```

`$type` discriminator ensures correct deserialization.

## Collection Serialization

Collections serialize element-by-element:

<!-- snippet: serialization-collections -->
<a id='snippet-serialization-collections'></a>
```cs
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
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L379-L400' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-collections' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Supports:
- `List<T>`, `IList<T>`, `T[]`
- `Dictionary<TKey, TValue>`
- Nested collections (`List<List<T>>`)
- Interface collections (`IList<IPerson>`)

## Polymorphism

Polymorphic types are supported:

<!-- snippet: serialization-polymorphism -->
<a id='snippet-serialization-polymorphism'></a>
```cs
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
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L85-L102' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-polymorphism' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `$type` discriminator identifies the concrete type during deserialization.

## Validation Attributes

Validation attributes are not serialized but remain on the type for validation:

<!-- snippet: serialization-validation -->
<a id='snippet-serialization-validation'></a>
```cs
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

// Validation on client before sending to server
public partial class ClientValidationExample
{
    public static bool ValidateBeforeSave(SerializationValidatedEntity entity)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(entity, new ValidationContext(entity), results, true);
    }
}
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L423-L452' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Validate on the server after deserialization:

<!-- snippet: serialization-validation-server -->
<a id='snippet-serialization-validation-server'></a>
```cs
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
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L454-L480' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-validation-server' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Performance Characteristics

**Ordinal Format:**
- Serialization: ~30% faster than Named
- Deserialization: ~25% faster than Named
- Payload size: 40-50% smaller

**Named Format:**
- Serialization: Standard System.Text.Json performance
- Deserialization: Standard System.Text.Json performance
- Payload size: Standard JSON

Performance improvements come from eliminating property names in JSON arrays and reducing parsing complexity.

## Custom Converters

For Ordinal format, implement `IOrdinalConverterProvider<T>` as shown in the [IOrdinalSerializable Interface](#iordinalserializable-interface) section.

For Named format or types not using ordinal serialization, custom converters follow standard System.Text.Json patterns:

<!-- snippet: serialization-custom-converter -->
<a id='snippet-serialization-custom-converter'></a>
```cs
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
        if (string.IsNullOrEmpty(value)) return null;

        var parts = value.Split(' ', 2);
        return new PhoneNumber
        {
            CountryCode = parts[0].TrimStart('+'),
            Number = parts.Length > 1 ? parts[1] : string.Empty
        };
    }

    public override void Write(Utf8JsonWriter writer, PhoneNumber value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString()); // Writes "+1 5551234567"
    }
}
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L105-L133' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-custom-converter' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Note:** RemoteFactory manages its own JsonSerializerOptions internally. For types that need custom serialization with Ordinal format, use `IOrdinalConverterProvider<T>` instead.

## Debugging Serialization

Enable verbose logging:

<!-- snippet: serialization-logging -->
<a id='snippet-serialization-logging'></a>
```cs
public static void ConfigureWithLogging(IServiceCollection services)
{
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Debug);
        builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace);
    });

    services.AddNeatooAspNetCore(typeof(SerializationSamples).Assembly);
}
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L135-L147' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-logging' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Logs include:
- Serialization format used
- Payload size
- Type information
- Errors and warnings

Switch to Named format for debugging:

<!-- snippet: serialization-debug-named -->
<a id='snippet-serialization-debug-named'></a>
```cs
public static void ConfigureByEnvironment(IServiceCollection services, bool isDevelopment)
{
    // Named format in development for readable JSON
    // Ordinal format in production for smaller payloads
    var format = isDevelopment ? SerializationFormat.Named : SerializationFormat.Ordinal;

    services.AddNeatooAspNetCore(
        new NeatooSerializationOptions { Format = format },
        typeof(SerializationSamples).Assembly);
}

// Named:   {"Id":"550e8400...","Name":"Test","Price":29.99}
// Ordinal: ["550e8400...",29.99,"Test"]
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L149-L163' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-debug-named' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inspect payloads with browser DevTools or Fiddler.

## Serialization Configuration

RemoteFactory manages its own JsonSerializerOptions internally. Configuration is done through `NeatooSerializationOptions`:

<!-- snippet: serialization-json-options -->
<a id='snippet-serialization-json-options'></a>
```cs
public static JsonSerializerOptions CreateCustomOptions()
{
    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    options.Converters.Add(new PhoneNumberConverter());
    options.Converters.Add(new JsonStringEnumConverter());
    return options;

    // Note: RemoteFactory manages its own JsonSerializerOptions internally
    // Custom converters should use IOrdinalConverterProvider<T> instead
}
```
<sup><a href='/src/docs/samples/SerializationSamples.cs#L165-L182' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-json-options' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Available options:**
- **Format**: Choose `SerializationFormat.Ordinal` (default) or `SerializationFormat.Named`

For custom type serialization, use `IOrdinalConverterProvider<T>` as shown in the [IOrdinalSerializable Interface](#iordinalserializable-interface) section.

## Next Steps

- [Factory Operations](factory-operations.md) - Operations that serialize data
- [Factory Modes](factory-modes.md) - When serialization occurs
- [ASP.NET Core Integration](aspnetcore-integration.md) - Server serialization setup
