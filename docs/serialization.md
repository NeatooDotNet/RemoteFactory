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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Serialization/SerializationConfigurationSamples.cs#L9-L45' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-config' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Simple [Factory] entity demonstrating alphabetical property ordering for ordinal serialization.
/// The generator produces IOrdinalSerializable, IOrdinalConverterProvider, and IOrdinalSerializationMetadata
/// implementations automatically.
/// </summary>
[Factory]
public partial class EmployeeRecord
{
    // Properties are serialized in alphabetical order:
    // Index 0: Department
    // Index 1: Email
    // Index 2: HireDate
    // Index 3: Name

    public string Department { get; set; } = "";  // Ordinal index 0
    public string Email { get; set; } = "";       // Ordinal index 1
    public DateTime HireDate { get; set; }        // Ordinal index 2
    public string Name { get; set; } = "";        // Ordinal index 3

    [Create]
    public EmployeeRecord() { }
}
// Ordinal JSON: ["Engineering", "john@example.com", "2024-01-15T00:00:00Z", "John Doe"]
// Named JSON:   {"Department":"Engineering","Email":"john@example.com",
//                "HireDate":"2024-01-15T00:00:00Z","Name":"John Doe"}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/OrdinalSerializationSamples.cs#L5-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-ordinal-generated' title='Start of snippet'>anchor</a></sup>
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
// Versioning rules for ordinal serialization:
// 1. ADD new properties - they will be appended based on alphabetical position
// 2. NEVER remove existing properties - breaks deserialization
// 3. NEVER rename properties - changes ordinal indices
// 4. NEVER change property types - causes type mismatch errors

/// <summary>
/// Demonstrates safe versioning with ordinal serialization.
/// New properties are added alphabetically; existing indices remain stable.
/// </summary>
[Factory]
public partial class VersionedEmployee
{
    // Version 1 properties (stable indices)
    public string Department { get; set; } = "";  // Index 0 - original property
    public string Name { get; set; } = "";        // Index 1 - original property

    // Version 2 property (inserted alphabetically between Department and Name)
    public string? Email { get; set; }            // Index 1 - shifts Name to index 2

    // Version 3 property (appended after Name alphabetically)
    public string? Title { get; set; }            // Index 3 - comes after Name

    [Create]
    public VersionedEmployee() { }
}
// After all versions:
// Index 0: Department
// Index 1: Email (added in v2)
// Index 2: Name (shifted from index 1)
// Index 3: Title (added in v3)
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/OrdinalSerializationSamples.cs#L33-L65' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-ordinal-versioning' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Removing or renaming properties breaks compatibility. Use semantic versioning and coordinate client/server updates.

## IOrdinalSerializable Interface

> **Recommendation:** Custom serialization should be avoided. Use the `[Factory]` attribute on your types instead - the source generator automatically implements all required serialization interfaces. Custom serialization is only necessary for edge cases like third-party types you cannot modify.

Implement `IOrdinalSerializable` to customize ordinal serialization:

<!-- snippet: serialization-custom-ordinal -->
<a id='snippet-serialization-custom-ordinal'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/CustomOrdinalSamples.cs#L6-L58' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-custom-ordinal' title='Start of snippet'>anchor</a></sup>
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
// RemoteFactory preserves object identity and handles circular references automatically.
// The NeatooReferenceHandler tracks objects during serialization/deserialization.

/// <summary>
/// Department aggregate with a list of employees (parent side of circular reference).
/// </summary>
[Factory]
public partial class DepartmentWithEmployees
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public List<EmployeeInDepartment> Employees { get; set; } = [];

    [Create]
    public DepartmentWithEmployees()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Employee with reference back to department (child side of circular reference).
/// </summary>
[Factory]
public partial class EmployeeInDepartment
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public DepartmentWithEmployees? Department { get; set; }  // Circular reference

    [Create]
    public EmployeeInDepartment()
    {
        Id = Guid.NewGuid();
    }
}
// NeatooReferenceHandler capabilities:
// - Detects circular references during serialization
// - Preserves object identity (same instance shared, not duplicated)
// - Avoids infinite loops with $ref pointers
// - Reconstructs object graph correctly during deserialization
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/ReferenceSamples.cs#L5-L47' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-references' title='Start of snippet'>anchor</a></sup>
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
/// Interface defining the public contract for an employee.
/// </summary>
public interface IEmployeeContract
{
    Guid Id { get; }
    string Name { get; }
    string Department { get; }
}

/// <summary>
/// Concrete [Factory] implementation of IEmployeeContract.
/// RemoteFactory serializes the full concrete type, not just interface members.
/// </summary>
[Factory]
public partial class ContractEmployee : IEmployeeContract
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";

    // Additional properties beyond the interface
    public string Email { get; set; } = "";
    public DateTime HireDate { get; set; }

    [Create]
    public ContractEmployee()
    {
        Id = Guid.NewGuid();
    }
}
// RemoteFactory includes $type discriminator for interface deserialization:
// {"$type":"ContractEmployee","Department":"Engineering",
//  "Email":"john@example.com","HireDate":"2024-01-15T00:00:00Z",
//  "Id":"...","Name":"John Doe"}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/ReferenceSamples.cs#L49-L85' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-interface' title='Start of snippet'>anchor</a></sup>
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
/// Demonstrates collection serialization with various collection types.
/// </summary>
[Factory]
public partial class EmployeeWithSkills
{
    public Guid Id { get; private set; }
    public List<string> Skills { get; set; } = [];
    public string[] Certifications { get; set; } = [];
    public Dictionary<string, int> ProjectHours { get; set; } = [];

    [Create]
    public EmployeeWithSkills()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public void FetchSampleData()
    {
        Skills = ["C#", "TypeScript", "SQL"];
        Certifications = ["Azure Developer", "Scrum Master"];
        ProjectHours = new Dictionary<string, int>
        {
            ["Project Alpha"] = 120,
            ["Project Beta"] = 80,
            ["Project Gamma"] = 45
        };
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/CollectionSamples.cs#L5-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-collections' title='Start of snippet'>anchor</a></sup>
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
/// Abstract base class for employee compensation demonstrating polymorphic serialization.
/// </summary>
public abstract class Compensation
{
    public Guid Id { get; set; }
    public DateTime EffectiveDate { get; set; }
}

/// <summary>
/// Salary-based compensation (annual amount).
/// </summary>
public class SalaryCompensation : Compensation
{
    public decimal AnnualAmount { get; set; }
}

/// <summary>
/// Hourly-based compensation (rate and hours per week).
/// </summary>
public class HourlyCompensation : Compensation
{
    public decimal HourlyRate { get; set; }
    public int HoursPerWeek { get; set; }
}
// The $type discriminator identifies concrete type during deserialization:
// {"$type":"SalaryCompensation","AnnualAmount":85000,"EffectiveDate":"2024-01-01T00:00:00Z","Id":"..."}
// {"$type":"HourlyCompensation","EffectiveDate":"2024-01-01T00:00:00Z","HourlyRate":45.00,"HoursPerWeek":40,"Id":"..."}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/CollectionSamples.cs#L38-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-polymorphism' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `$type` discriminator identifies the concrete type during deserialization.

## Validation Attributes

Validation attributes are not serialized but remain on the type for validation:

<!-- snippet: serialization-validation -->
<a id='snippet-serialization-validation'></a>
```cs
/// <summary>
/// Demonstrates validation attributes that persist across serialization.
/// Attributes are not serialized but remain on the type for validation.
/// </summary>
[Factory]
public partial class ValidatedEmployee
{
    public Guid Id { get; private set; }

    [Required(ErrorMessage = "Employee name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = "";

    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string? Email { get; set; }

    [Range(30000, 500000, ErrorMessage = "Salary must be between $30,000 and $500,000")]
    public decimal Salary { get; set; }

    [Create]
    public ValidatedEmployee()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Helper class demonstrating client-side validation using DataAnnotations.
/// </summary>
public static class EmployeeValidator
{
    public static bool TryValidate(ValidatedEmployee employee, out List<ValidationResult> results)
    {
        results = [];
        var context = new ValidationContext(employee);
        return Validator.TryValidateObject(employee, context, results, validateAllProperties: true);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/ValidationSamples.cs#L7-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-validation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Validate on the server after deserialization:

<!-- snippet: serialization-validation-server -->
<a id='snippet-serialization-validation-server'></a>
```cs
/// <summary>
/// Demonstrates server-side validation in [Remote, Insert] method.
/// Implements IFactorySaveMeta for tracking new/deleted state.
/// </summary>
[Factory]
public partial class ServerValidatedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ServerValidatedEmployee()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        // Server-side validation before persistence
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ValidationException("Employee name cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            throw new ValidationException("Invalid email address format");
        }

        // Map to entity and persist
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = Name,
            LastName = "",
            Email = Email
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);

        // Mark as no longer new after successful insert
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Serialization/ValidationSamples.cs#L48-L98' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-validation-server' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/Serialization/CustomConverterSamples.cs#L6-L55' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-custom-converter' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Serialization/SerializationConfigurationSamples.cs#L68-L88' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-debug-named' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inspect payloads with browser DevTools or Fiddler.

## Serialization Configuration

RemoteFactory manages its own JsonSerializerOptions internally. Configuration is done through `NeatooSerializationOptions`:

<!-- snippet: serialization-json-options -->
<a id='snippet-serialization-json-options'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/Serialization/CustomConverterSamples.cs#L57-L82' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-json-options' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Available options:**
- **Format**: Choose `SerializationFormat.Ordinal` (default) or `SerializationFormat.Named`

For custom type serialization, use `IOrdinalConverterProvider<T>` as shown in the [IOrdinalSerializable Interface](#iordinalserializable-interface) section.

## Next Steps

- [Factory Operations](factory-operations.md) - Operations that serialize data
- [Factory Modes](factory-modes.md) - When serialization occurs
- [ASP.NET Core Integration](aspnetcore-integration.md) - Server serialization setup
