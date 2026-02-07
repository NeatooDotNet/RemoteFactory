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
// Configure serialization format: Ordinal (compact) or Named (readable).
public static void Configure(IServiceCollection services)
{
    var options = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };
    services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Serialization/SerializationConfigurationSamples.cs#L11-L18' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-config' title='Start of snippet'>anchor</a></sup>
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
```
** Could not find snippet 'serialization-ordinal-generated' **
```
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
// Properties serialized alphabetically: Active[0], Age[1], Email[2], FirstName[3]...
// Adding "Department" inserts at [2], shifting everything after.
public bool Active { get; set; } = true;
public int Age { get; set; }
public string Email { get; set; } = "";
public string FirstName { get; set; } = "";
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L13-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-ordinal-versioning' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Removing or renaming properties breaks compatibility. Use semantic versioning and coordinate client/server updates.

## IOrdinalSerializable Interface

> **Recommendation:** Custom serialization should be avoided. Use the `[Factory]` attribute on your types instead - the source generator automatically implements all required serialization interfaces. Custom serialization is only necessary for edge cases like third-party types you cannot modify.

Implement `IOrdinalSerializable` to customize ordinal serialization:

<!-- snippet: serialization-custom-ordinal -->
<a id='snippet-serialization-custom-ordinal'></a>
```cs
// Implement IOrdinalSerializable: return properties in alphabetical order.
public object?[] ToOrdinalArray() => [Amount, Currency];
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L41-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-custom-ordinal' title='Start of snippet'>anchor</a></sup>
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
// Bidirectional reference - serializes as {"$ref": "1"} to preserve identity.
public void AddMember(string name) => Members.Add(new TeamMember(name, this));
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L58-L61' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-references' title='Start of snippet'>anchor</a></sup>
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
// Interface property serializes with $type: "EmailContact" or "PhoneContact"
public IContactInfo? PrimaryContact { get; set; }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L89-L92' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-interface' title='Start of snippet'>anchor</a></sup>
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
public List<string> EmployeeNames { get; set; } = [];              // List<T>
public Dictionary<Guid, string> DepartmentNames { get; set; } = []; // Dictionary
public List<List<string>> TeamHierarchy { get; set; } = [];        // Nested
public string[] ActiveProjects { get; set; } = [];                 // Array
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L121-L126' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-collections' title='Start of snippet'>anchor</a></sup>
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
// Mixed types: each serialized with $type discriminator for correct deserialization.
public List<EmployeeTypeBase> Employees { get; set; } = [];
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L162-L165' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-polymorphism' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `$type` discriminator identifies the concrete type during deserialization.

## Validation Attributes

Validation attributes are not serialized but remain on the type for validation:

<!-- snippet: serialization-validation -->
<a id='snippet-serialization-validation'></a>
```cs
// Validation attributes are serialized with the type, validated server-side.
[Required] [StringLength(100)] public string FirstName { get; set; } = "";
[Required] [EmailAddress] public string Email { get; set; } = "";
[Range(0, 10000000)] public decimal Salary { get; set; }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L177-L182' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Validate on the server after deserialization:

<!-- snippet: serialization-validation-server -->
<a id='snippet-serialization-validation-server'></a>
```cs
// Validate after deserialization using DataAnnotations.
[Remote, Insert]
public Task Insert(CancellationToken ct)
{
    var context = new ValidationContext(this);
    var results = new List<ValidationResult>();
    if (!Validator.TryValidateObject(this, context, results, validateAllProperties: true))
        throw new ValidationException($"Validation failed: {string.Join("; ", results.Select(r => r.ErrorMessage))}");
    IsNew = false;
    return Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L204-L216' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-validation-server' title='Start of snippet'>anchor</a></sup>
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
[JsonConverter(typeof(MoneyJsonConverter))]
public class MoneyValue(decimal amount, string currency)
{
    public decimal Amount { get; } = amount;
    public string Currency { get; } = currency;
}

public class MoneyJsonConverter : JsonConverter<MoneyValue>
{
    public override MoneyValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return new MoneyValue(doc.RootElement.GetProperty("amount").GetDecimal(),
                              doc.RootElement.GetProperty("currency").GetString() ?? "USD");
    }

    public override void Write(Utf8JsonWriter writer, MoneyValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.Currency);
        writer.WriteEndObject();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L220-L245' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-custom-converter' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Note:** RemoteFactory manages its own JsonSerializerOptions internally. For types that need custom serialization with Ordinal format, use `IOrdinalConverterProvider<T>` instead.

## Debugging Serialization

Enable verbose logging:

<!-- snippet: serialization-logging -->
<a id='snippet-serialization-logging'></a>
```cs
// Enable verbose logging for serialization debugging.
public static void ConfigureWithLogging(IServiceCollection services)
{
    services.AddLogging(b => b.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace));
    services.AddNeatooAspNetCore(typeof(Employee).Assembly);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Serialization/SerializationConfigurationSamples.cs#L23-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-logging' title='Start of snippet'>anchor</a></sup>
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
// Switch to Named format in development for readable JSON debugging.
public static void ConfigureByEnvironment(IServiceCollection services, bool isDevelopment)
{
    var format = isDevelopment ? SerializationFormat.Named : SerializationFormat.Ordinal;
    services.AddNeatooAspNetCore(new NeatooSerializationOptions { Format = format }, typeof(Employee).Assembly);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Serialization/SerializationConfigurationSamples.cs#L35-L42' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-debug-named' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inspect payloads with browser DevTools or Fiddler.

## Serialization Configuration

RemoteFactory manages its own JsonSerializerOptions internally. Configuration is done through `NeatooSerializationOptions`:

<!-- snippet: serialization-json-options -->
<a id='snippet-serialization-json-options'></a>
```cs
// RemoteFactory manages JsonSerializerOptions internally.
// Use IOrdinalConverterProvider<T> for custom Ordinal serialization.
// Use JsonConverter<T> (as above) for Named format only.
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/Serialization/CustomConverterSamples.cs#L31-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-json-options' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Available options:**
- **Format**: Choose `SerializationFormat.Ordinal` (default) or `SerializationFormat.Named`

For custom type serialization, use `IOrdinalConverterProvider<T>` as shown in the [IOrdinalSerializable Interface](#iordinalserializable-interface) section.

## Next Steps

- [Factory Operations](factory-operations.md) - Operations that serialize data
- [Factory Modes](factory-modes.md) - When serialization occurs
- [ASP.NET Core Integration](aspnetcore-integration.md) - Server serialization setup
