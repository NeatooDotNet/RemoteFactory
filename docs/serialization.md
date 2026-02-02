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
// Serialization format configuration during DI registration.
//
// Ordinal format (default) - compact array-based serialization:
// services.AddNeatooRemoteFactory(
//     NeatooFactory.Logical,
//     new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
//     domainAssembly);
//
// Named format - human-readable JSON with property names:
// services.AddNeatooRemoteFactory(
//     NeatooFactory.Logical,
//     new NeatooSerializationOptions { Format = SerializationFormat.Named },
//     domainAssembly);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L412-L426' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-config' title='Start of snippet'>anchor</a></sup>
<a id='snippet-serialization-config-1'></a>
```cs
/// <summary>
/// Demonstrates configuring RemoteFactory serialization formats during server startup.
/// </summary>
public static class SerializationConfiguration
{
    /// <summary>
    /// Configures RemoteFactory with Ordinal format (default).
    /// Produces compact JSON arrays without property names.
    /// </summary>
    public static void ConfigureOrdinalFormat(IServiceCollection services)
    {
        // Ordinal format: Compact arrays, 40-50% smaller payloads
        // Example: ["Engineering", "john@example.com", "2024-01-15", "John Doe"]
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };
        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);
    }

    /// <summary>
    /// Configures RemoteFactory with Named format.
    /// Produces traditional JSON objects with property names.
    /// </summary>
    public static void ConfigureNamedFormat(IServiceCollection services)
    {
        // Named format: Traditional JSON, easier to debug
        // Example: {"Department":"Engineering","Email":"john@example.com","HireDate":"2024-01-15","Name":"John Doe"}
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };
        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Serialization/SerializationConfigurationSamples.cs#L9-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-config-1' title='Start of snippet'>anchor</a></sup>
<a id='snippet-serialization-config-2'></a>
```cs
/// <summary>
/// Serialization format configuration during registration.
/// </summary>
public class SerializationConfigSample
{
    [Fact]
    public void ConfigureOrdinalFormat()
    {
        // Ordinal format (default) - compact array-based
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };

        // Use in registration:
        // services.AddNeatooRemoteFactory(NeatooFactory.Logical, options, assembly);
        // services.AddNeatooAspNetCore(options, assembly);

        Assert.Equal(SerializationFormat.Ordinal, options.Format);
    }

    [Fact]
    public void ConfigureNamedFormat()
    {
        // Named format - human-readable with property names
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };

        Assert.Equal(SerializationFormat.Named, options.Format);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L626-L660' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-config-2' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates ordinal serialization versioning considerations.
/// Properties are serialized in alphabetical order.
/// </summary>
[Factory]
public partial class EmployeeWithVersioning
{
    // Properties serialized in alphabetical order: Active, Age, Email, FirstName, HireDate, LastName
    // Adding a new property (e.g., "Department") inserts at position 0 (alphabetically before "Email")
    // This shifts existing positions - requires rebuilding both client and server

    public bool Active { get; set; } = true;      // [0]
    public int Age { get; set; }                  // [1]
    // Adding Department here would be [2], shifting Email, FirstName, HireDate, LastName
    public string Email { get; set; } = "";       // [2]
    public string FirstName { get; set; } = "";   // [3]
    public DateTime HireDate { get; set; }        // [4]
    public string LastName { get; set; } = "";    // [5]

    // Best practice: When adding properties, rebuild both client and server
    // to ensure ordinal positions match.

    [Create]
    public EmployeeWithVersioning() { }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L8-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-ordinal-versioning' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Removing or renaming properties breaks compatibility. Use semantic versioning and coordinate client/server updates.

## IOrdinalSerializable Interface

> **Recommendation:** Custom serialization should be avoided. Use the `[Factory]` attribute on your types instead - the source generator automatically implements all required serialization interfaces. Custom serialization is only necessary for edge cases like third-party types you cannot modify.

Implement `IOrdinalSerializable` to customize ordinal serialization:

<!-- snippet: serialization-custom-ordinal -->
<a id='snippet-serialization-custom-ordinal'></a>
```cs
/// <summary>
/// Money value object implementing IOrdinalSerializable.
/// Use when you need custom ordinal serialization for non-factory types.
/// </summary>
public class MoneyOrdinal : IOrdinalSerializable
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyOrdinal(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Returns properties in alphabetical order for ordinal serialization.
    /// Order: Amount, Currency
    /// </summary>
    public object?[] ToOrdinalArray()
    {
        // Alphabetical order: Amount, Currency
        return [Amount, Currency];
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L36-L62' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-custom-ordinal' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates circular reference handling.
/// Parent-child bidirectional references are preserved.
/// </summary>
[Factory]
public partial class TeamWithMembers
{
    public Guid Id { get; private set; }
    public string TeamName { get; set; } = "";
    public List<TeamMember> Members { get; set; } = [];

    [Create]
    public TeamWithMembers()
    {
        Id = Guid.NewGuid();
    }

    public void AddMember(string name)
    {
        var member = new TeamMember(name, this);
        Members.Add(member);
    }
}

[Factory]
public partial class TeamMember
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    /// <summary>
    /// Bidirectional reference to parent Team.
    /// RemoteFactory preserves object identity via $ref pointers.
    /// </summary>
    public TeamWithMembers Team { get; set; } = null!;

    [Create]
    public TeamMember(string name, TeamWithMembers team)
    {
        Id = Guid.NewGuid();
        Name = name;
        Team = team;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L64-L109' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-references' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Interface properties serialize as their concrete type with $type discriminator.
/// </summary>
[Factory]
public partial class EmployeeWithContact
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    /// <summary>
    /// Interface property holds concrete EmailContact or PhoneContact.
    /// Serialized with $type discriminator for correct deserialization.
    /// </summary>
    public IContactInfo? PrimaryContact { get; set; }

    [Create]
    public EmployeeWithContact()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Contact information interface.
/// </summary>
public interface IContactInfo
{
    string Type { get; }
    string Value { get; }
}

/// <summary>
/// Email contact implementation.
/// </summary>
[Factory]
public partial class EmailContact : IContactInfo
{
    public string Type => "Email";
    public string Value { get; }

    [Create]
    public EmailContact(string email)
    {
        Value = email;
    }
}

/// <summary>
/// Phone contact implementation.
/// </summary>
[Factory]
public partial class PhoneContact : IContactInfo
{
    public string Type => "Phone";
    public string Value { get; }
    public string Extension { get; }

    [Create]
    public PhoneContact(string phone, string extension = "")
    {
        Value = phone;
        Extension = extension;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L111-L176' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-interface' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates collection serialization patterns.
/// </summary>
[Factory]
public partial class OrganizationData
{
    public Guid Id { get; private set; }

    /// <summary>
    /// List collections serialized element-by-element.
    /// </summary>
    public List<string> EmployeeNames { get; set; } = [];

    /// <summary>
    /// Dictionary with Guid keys and string values.
    /// </summary>
    public Dictionary<Guid, string> DepartmentNames { get; set; } = [];

    /// <summary>
    /// Nested collections supported.
    /// </summary>
    public List<List<string>> TeamHierarchy { get; set; } = [];

    /// <summary>
    /// Array collections.
    /// </summary>
    public string[] ActiveProjects { get; set; } = [];

    [Create]
    public OrganizationData()
    {
        Id = Guid.NewGuid();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L178-L213' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-collections' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Base employee type for polymorphic serialization.
/// </summary>
[Factory]
public abstract partial class EmployeeTypeBase
{
    public Guid Id { get; protected set; }
    public string Name { get; set; } = "";
    public abstract string EmploymentType { get; }
}

/// <summary>
/// Full-time employee type.
/// </summary>
[Factory]
public partial class FullTimeEmployee : EmployeeTypeBase
{
    public override string EmploymentType => "FullTime";
    public decimal AnnualSalary { get; set; }
    public int VacationDays { get; set; }

    [Create]
    public FullTimeEmployee()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Contract employee type.
/// </summary>
[Factory]
public partial class ContractEmployee : EmployeeTypeBase
{
    public override string EmploymentType => "Contract";
    public decimal HourlyRate { get; set; }
    public DateTime ContractEndDate { get; set; }

    [Create]
    public ContractEmployee()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Container for polymorphic employee collection.
/// $type discriminator identifies concrete types during deserialization.
/// </summary>
[Factory]
public partial class Workforce
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Collection holds mixed FullTimeEmployee and ContractEmployee instances.
    /// Each serialized with $type discriminator.
    /// </summary>
    public List<EmployeeTypeBase> Employees { get; set; } = [];

    [Create]
    public Workforce()
    {
        Id = Guid.NewGuid();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L215-L282' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-polymorphism' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `$type` discriminator identifies the concrete type during deserialization.

## Validation Attributes

Validation attributes are not serialized but remain on the type for validation:

<!-- snippet: serialization-validation -->
<a id='snippet-serialization-validation'></a>
```cs
/// <summary>
/// Validation attributes on serializable types.
/// Attributes are preserved but not enforced during serialization.
/// </summary>
[Factory]
public partial class ValidatedEmployee
{
    public Guid Id { get; private set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = "";

    [Range(0, 10000000, ErrorMessage = "Salary must be between 0 and 10,000,000")]
    public decimal Salary { get; set; }

    [Create]
    public ValidatedEmployee()
    {
        Id = Guid.NewGuid();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L284-L315' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Validate on the server after deserialization:

<!-- snippet: serialization-validation-server -->
<a id='snippet-serialization-validation-server'></a>
```cs
/// <summary>
/// Server-side validation after deserialization using Validator.
/// </summary>
[Factory]
public partial class ServerValidatedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ServerValidatedEmployee() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Validate after deserialization using DataAnnotations.
    /// </summary>
    [Remote, Insert]
    public Task Insert(CancellationToken ct)
    {
        // Validate using DataAnnotations
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(this, context, results, validateAllProperties: true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ValidationException($"Validation failed: {errors}");
        }

        IsNew = false;
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L317-L364' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-validation-server' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Custom JsonConverter for types that cannot use [Factory].
/// Use for third-party types or special serialization logic.
/// </summary>
public class MoneyJsonConverter : JsonConverter<MoneyValue>
{
    public override MoneyValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Expect object format: { "amount": 100.00, "currency": "USD" }
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var amount = root.GetProperty("amount").GetDecimal();
        var currency = root.GetProperty("currency").GetString() ?? "USD";

        return new MoneyValue(amount, currency);
    }

    public override void Write(Utf8JsonWriter writer, MoneyValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.Currency);
        writer.WriteEndObject();
    }
}

/// <summary>
/// Value object with custom JSON converter.
/// Not a [Factory] type - uses custom converter instead.
/// </summary>
[JsonConverter(typeof(MoneyJsonConverter))]
public class MoneyValue
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyValue(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L366-L410' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-custom-converter' title='Start of snippet'>anchor</a></sup>
<a id='snippet-serialization-custom-converter-1'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/Serialization/CustomConverterSamples.cs#L6-L55' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-custom-converter-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Note:** RemoteFactory manages its own JsonSerializerOptions internally. For types that need custom serialization with Ordinal format, use `IOrdinalConverterProvider<T>` instead.

## Debugging Serialization

Enable verbose logging:

<!-- snippet: serialization-logging -->
<a id='snippet-serialization-logging'></a>
```cs
/// <summary>
/// Demonstrates enabling verbose logging for serialization debugging.
/// </summary>
public static class SerializationLoggingConfiguration
{
    public static void ConfigureWithLogging(IServiceCollection services)
    {
        // Enable verbose logging for serialization debugging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace);
        });

        services.AddNeatooAspNetCore(typeof(Employee).Assembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Serialization/SerializationConfigurationSamples.cs#L47-L66' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-logging' title='Start of snippet'>anchor</a></sup>
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
// Switching to Named format for debugging serialization issues.
//
// if (builder.Environment.IsDevelopment())
// {
//     // Named format for human-readable JSON in dev tools
//     builder.Services.AddNeatooAspNetCore(
//         new NeatooSerializationOptions { Format = SerializationFormat.Named },
//         domainAssembly);
// }
// else
// {
//     // Ordinal format for compact production payloads
//     builder.Services.AddNeatooAspNetCore(
//         new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
//         domainAssembly);
// }
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L428-L445' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-debug-named' title='Start of snippet'>anchor</a></sup>
<a id='snippet-serialization-debug-named-1'></a>
```cs
/// <summary>
/// Demonstrates switching serialization format based on environment.
/// </summary>
public static class EnvironmentBasedSerializationConfiguration
{
    public static void ConfigureByEnvironment(IServiceCollection services, bool isDevelopment)
    {
        // Use Named format in development for readable JSON debugging
        // Use Ordinal format in production for smaller payloads
        var format = isDevelopment
            ? SerializationFormat.Named
            : SerializationFormat.Ordinal;

        var options = new NeatooSerializationOptions { Format = format };
        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);
    }
}
// Development (Named):   {"Department":"Engineering","Email":"john@example.com","Name":"John Doe"}
// Production (Ordinal):  ["Engineering","john@example.com","John Doe"]
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Serialization/SerializationConfigurationSamples.cs#L68-L88' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-debug-named-1' title='Start of snippet'>anchor</a></sup>
<a id='snippet-serialization-debug-named-2'></a>
```cs
/// <summary>
/// Switching to Named format for debugging.
/// </summary>
public class SerializationDebugSample
{
    [Fact]
    public void DebugWithNamedFormat()
    {
        // For debugging, use Named format in development:
        // if (builder.Environment.IsDevelopment())
        // {
        //     services.AddNeatooAspNetCore(
        //         new NeatooSerializationOptions { Format = SerializationFormat.Named },
        //         assembly);
        // }

        // Named format produces human-readable JSON:
        // { "FirstName": "John", "LastName": "Doe", "Age": 30 }

        // Ordinal format produces compact arrays:
        // [30, "John", "Doe"]  // Age, FirstName, LastName (alphabetical)

        var namedOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };

        Assert.Equal(SerializationFormat.Named, namedOptions.Format);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L662-L693' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-debug-named-2' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inspect payloads with browser DevTools or Fiddler.

## Serialization Configuration

RemoteFactory manages its own JsonSerializerOptions internally. Configuration is done through `NeatooSerializationOptions`:

<!-- snippet: serialization-json-options -->
<a id='snippet-serialization-json-options'></a>
```cs
// NeatooSerializationOptions configuration.
// RemoteFactory manages JsonSerializerOptions internally.
//
// var options = new NeatooSerializationOptions
// {
//     // Format: Choose Ordinal (default, compact) or Named (readable)
//     Format = SerializationFormat.Ordinal
// };
//
// Note: RemoteFactory manages JsonSerializerOptions internally
// For custom type serialization, implement:
// - IOrdinalSerializable for [Factory] types
// - IOrdinalConverterProvider<T> for non-factory types
// - JsonConverter<T> for Named format only
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/SerializationSamples.cs#L447-L462' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-json-options' title='Start of snippet'>anchor</a></sup>
<a id='snippet-serialization-json-options-1'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/Serialization/CustomConverterSamples.cs#L57-L82' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-json-options-1' title='Start of snippet'>anchor</a></sup>
<a id='snippet-serialization-json-options-2'></a>
```cs
/// <summary>
/// NeatooSerializationOptions configuration.
/// </summary>
public class SerializationJsonOptionsSample
{
    [Fact]
    public void NeatooSerializationOptions_FormatProperty()
    {
        // NeatooSerializationOptions is the configuration object
        var options = new NeatooSerializationOptions
        {
            // Format: Choose Ordinal (default, compact) or Named (readable)
            Format = SerializationFormat.Ordinal
        };

        // Note: RemoteFactory manages JsonSerializerOptions internally
        // Use IOrdinalConverterProvider<T> for custom type serialization

        Assert.Equal(SerializationFormat.Ordinal, options.Format);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L695-L717' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-json-options-2' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Available options:**
- **Format**: Choose `SerializationFormat.Ordinal` (default) or `SerializationFormat.Named`

For custom type serialization, use `IOrdinalConverterProvider<T>` as shown in the [IOrdinalSerializable Interface](#iordinalserializable-interface) section.

## Next Steps

- [Factory Operations](factory-operations.md) - Operations that serialize data
- [Factory Modes](factory-modes.md) - When serialization occurs
- [ASP.NET Core Integration](aspnetcore-integration.md) - Server serialization setup
