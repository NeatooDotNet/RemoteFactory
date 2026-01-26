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
<!--
SNIPPET REQUIREMENTS:
- Show two static methods in a Server startup/configuration class
- First method: ConfigureOrdinalFormat - registers RemoteFactory with Ordinal format (default, compact arrays)
- Second method: ConfigureNamedFormat - registers RemoteFactory with Named format (traditional JSON objects)
- Use AddNeatooAspNetCore with NeatooSerializationOptions
- Context: Server layer (Program.cs or Startup configuration)
- Domain: Employee Management - reference the assembly containing Employee types
- Include brief comments explaining the difference between formats
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show a simple [Factory] entity with 3-4 properties demonstrating alphabetical ordering
- Use Employee domain: properties like Department (string), Email (string), HireDate (DateTime), Name (string)
- Include [Create] constructor
- Add comments showing which property gets which ordinal index (alphabetical order)
- Add comments explaining what interfaces/methods the generator produces (IOrdinalSerializable, IOrdinalConverterProvider, IOrdinalSerializationMetadata)
- Include trailing comment showing example JSON output in both Ordinal and Named formats
- Context: Domain layer
- Domain: Employee Management
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show a [Factory] entity demonstrating safe versioning with ordinal serialization
- Use Employee domain: start with core properties (Department, Name), add optional properties in later versions
- Version 1 properties: Department (string), Name (string) - indices 0, 1
- Version 2 property: Email (string?) - index 2 (comes after Department alphabetically)
- Version 3 property: Title (string?) - index 3 (comes after Name alphabetically)
- Include comments explaining the versioning strategy and ordinal indices
- Add leading comment block explaining versioning rules: add at END alphabetically, never remove/rename, never change types
- Context: Domain layer
- Domain: Employee Management
-->
<!-- endSnippet -->

Removing or renaming properties breaks compatibility. Use semantic versioning and coordinate client/server updates.

## IOrdinalSerializable Interface

> **Recommendation:** Custom serialization should be avoided. Use the `[Factory]` attribute on your types instead - the source generator automatically implements all required serialization interfaces. Custom serialization is only necessary for edge cases like third-party types you cannot modify.

Implement `IOrdinalSerializable` to customize ordinal serialization:

<!-- snippet: serialization-custom-ordinal -->
<!--
SNIPPET REQUIREMENTS:
- Show a value object that does NOT use [Factory] and requires custom ordinal serialization
- Use Employee domain: Salary value object with Amount (decimal) and Currency (string)
- Implement a custom JsonConverter<Salary> for ordinal format
- Read method: expect array, read Amount then Currency, return new Salary
- Write method: write array with Amount and Currency values
- Include leading comment explaining this is for types NOT managed by [Factory]
- Add trailing comment noting that [Factory] types get converters automatically
- Context: Domain layer (value object) + Infrastructure layer (converter)
- Domain: Employee Management
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show two [Factory] entities with circular reference relationship
- Use Employee domain: Department with list of Employees, Employee with reference back to Department
- Department: Guid Id, string Name, List<Employee> Employees
- Employee: Guid Id, string Name, Department? Department (circular reference)
- Both have [Create] constructors that initialize Id with Guid.NewGuid()
- Add leading comment explaining object references and circular reference handling
- Add trailing comment explaining NeatooReferenceHandler capabilities: detect circular refs, preserve identity, avoid infinite loops
- Context: Domain layer
- Domain: Employee Management
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show an interface and concrete [Factory] implementation demonstrating interface serialization
- Use Employee domain: IEmployee interface with Id, Name, Department properties
- Concrete implementation: Employee class with [Factory], implements IEmployee
- Employee has additional properties beyond interface (e.g., Email, HireDate)
- Include [Create] constructor
- Add trailing comment explaining that RemoteFactory includes $type discriminator for interface deserialization
- Context: Domain layer
- Domain: Employee Management
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show a [Factory] entity with various collection types demonstrating collection serialization
- Use Employee domain: Employee with skills, certifications, project hours
- Properties: Guid Id, List<string> Skills, string[] Certifications, Dictionary<string, int> ProjectHours
- Include [Create] constructor that initializes Id
- Include [Remote, Fetch] method that populates collections with sample data
- Context: Domain layer
- Domain: Employee Management
-->
<!-- endSnippet -->

Supports:
- `List<T>`, `IList<T>`, `T[]`
- `Dictionary<TKey, TValue>`
- Nested collections (`List<List<T>>`)
- Interface collections (`IList<IPerson>`)

## Polymorphism

Polymorphic types are supported:

<!-- snippet: serialization-polymorphism -->
<!--
SNIPPET REQUIREMENTS:
- Show abstract base class and concrete derived classes demonstrating polymorphic serialization
- Use Employee domain: abstract Compensation base class with derived types
- Base class Compensation: Guid Id, DateTime EffectiveDate
- Derived SalaryCompensation: decimal AnnualAmount
- Derived HourlyCompensation: decimal HourlyRate, int HoursPerWeek
- No [Factory] attributes needed - this demonstrates type hierarchy serialization
- Context: Domain layer
- Domain: Employee Management
-->
<!-- endSnippet -->

The `$type` discriminator identifies the concrete type during deserialization.

## Validation Attributes

Validation attributes are not serialized but remain on the type for validation:

<!-- snippet: serialization-validation -->
<!--
SNIPPET REQUIREMENTS:
- Show a [Factory] entity with validation attributes that persist across serialization
- Use Employee domain: Employee with validated properties
- Properties: Guid Id, [Required] string Name with [StringLength], [EmailAddress] string? Email, [Range] decimal Salary
- Include [Create] constructor
- Add a separate static helper class showing client-side validation using Validator.TryValidateObject
- Include appropriate error messages on validation attributes
- Context: Domain layer (entity) + Client layer (validation helper)
- Domain: Employee Management
-->
<!-- endSnippet -->

Validate on the server after deserialization:

<!-- snippet: serialization-validation-server -->
<!--
SNIPPET REQUIREMENTS:
- Show a [Factory] entity implementing IFactorySaveMeta with server-side validation in [Remote, Insert] method
- Use Employee domain: Employee with server-validated properties
- Properties: Guid Id, string Name, string Email, bool IsNew, bool IsDeleted
- Implement IFactorySaveMeta interface
- Include [Create] constructor
- Include [Remote, Insert] method with [Service] IEmployeeRepository parameter
- Server-side validation: check Name is not empty, Email format is valid, throw ValidationException on failure
- Set IsNew = false after successful insert
- Context: Domain layer with server-side operation
- Domain: Employee Management
-->
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
<!--
SNIPPET REQUIREMENTS:
- Show a value object with custom JsonConverter for Named format serialization
- Use Employee domain: PhoneNumber value object with CountryCode and Number properties
- PhoneNumber class: string CountryCode, string Number, override ToString() returning "+{CountryCode} {Number}"
- Custom JsonConverter<PhoneNumber>: serialize as single string "+1 5551234567", deserialize by parsing
- Read method: get string, split on space, extract country code and number
- Write method: write the ToString() value as string
- Context: Domain layer (value object) + Infrastructure layer (converter)
- Domain: Employee Management
-->
<!-- endSnippet -->

**Note:** RemoteFactory manages its own JsonSerializerOptions internally. For types that need custom serialization with Ordinal format, use `IOrdinalConverterProvider<T>` instead.

## Debugging Serialization

Enable verbose logging:

<!-- snippet: serialization-logging -->
<!--
SNIPPET REQUIREMENTS:
- Show server configuration method enabling verbose logging for serialization debugging
- Static method ConfigureWithLogging(IServiceCollection services)
- Configure logging: AddConsole, SetMinimumLevel to Debug, AddFilter for "Neatoo.RemoteFactory" at Trace level
- Call AddNeatooAspNetCore with the assembly containing Employee types
- Context: Server layer (Program.cs or Startup configuration)
- Domain: Employee Management - reference the assembly containing Employee types
-->
<!-- endSnippet -->

Logs include:
- Serialization format used
- Payload size
- Type information
- Errors and warnings

Switch to Named format for debugging:

<!-- snippet: serialization-debug-named -->
<!--
SNIPPET REQUIREMENTS:
- Show server configuration that switches serialization format based on environment
- Static method ConfigureByEnvironment(IServiceCollection services, bool isDevelopment)
- Use Named format in development (readable JSON), Ordinal format in production (smaller payloads)
- Conditional: var format = isDevelopment ? SerializationFormat.Named : SerializationFormat.Ordinal
- Call AddNeatooAspNetCore with NeatooSerializationOptions using the selected format
- Add trailing comment showing example output in both formats for an Employee
- Context: Server layer (Program.cs or Startup configuration)
- Domain: Employee Management - reference the assembly containing Employee types
-->
<!-- endSnippet -->

Inspect payloads with browser DevTools or Fiddler.

## Serialization Configuration

RemoteFactory manages its own JsonSerializerOptions internally. Configuration is done through `NeatooSerializationOptions`:

<!-- snippet: serialization-json-options -->
<!--
SNIPPET REQUIREMENTS:
- Show creating custom JsonSerializerOptions (for reference/comparison, NOT used by RemoteFactory)
- Static method CreateCustomOptions() returning JsonSerializerOptions
- Configure: CamelCase naming policy, WriteIndented = false, DefaultIgnoreCondition = WhenWritingNull
- Add converters: PhoneNumberConverter (from serialization-custom-converter), JsonStringEnumConverter
- Include trailing comment: RemoteFactory manages its own options internally, use IOrdinalConverterProvider<T> for custom serialization
- Context: Infrastructure layer (configuration reference)
- Domain: Employee Management
-->
<!-- endSnippet -->

**Available options:**
- **Format**: Choose `SerializationFormat.Ordinal` (default) or `SerializationFormat.Named`

For custom type serialization, use `IOrdinalConverterProvider<T>` as shown in the [IOrdinalSerializable Interface](#iordinalserializable-interface) section.

## Next Steps

- [Factory Operations](factory-operations.md) - Operations that serialize data
- [Factory Modes](factory-modes.md) - When serialization occurs
- [ASP.NET Core Integration](aspnetcore-integration.md) - Server serialization setup
