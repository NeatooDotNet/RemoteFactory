# Reflection-Free Ordinal Serialization - Implementation Plan

## Overview

Eliminate reflection from ordinal JSON serialization by generating strongly-typed converters for each `[Factory]` type. This is Phase 1 of the STJ Source Generation initiative.

**Status:** PENDING

**Reference:** `docs/analysis/STJ-Source-Generation-Analysis.md`

---

## Goals

1. Eliminate `MakeGenericType` calls in `NeatooOrdinalConverterFactory`
2. Eliminate reflection-based metadata access in `NeatooOrdinalConverter<T>`
3. Eliminate `Activator.CreateInstance` for converter creation
4. Maintain full backward compatibility with existing wire format
5. Improve startup performance and AOT trimming support

---

## Non-Goals (Phase 1)

- Full AOT/NativeAOT support (requires Phase 2+)
- Eliminating reflection from interface polymorphism (`$type`/`$value`)
- Eliminating `DynamicInvoke` from delegate dispatch
- Changes to Named format serialization

---

## Current State

### Reflection Points to Eliminate

| File | Reflection | Purpose |
|------|------------|---------|
| `NeatooOrdinalConverterFactory.cs:34` | `MakeGenericType` | Create `NeatooOrdinalConverter<T>` |
| `NeatooOrdinalConverterFactory.cs:35` | `Activator.CreateInstance` | Instantiate converter |
| `NeatooOrdinalConverter.cs:59-60` | `GetInterfaces()` | Check for metadata interface |
| `NeatooOrdinalConverter.cs:65` | `GetProperty("PropertyNames")` | Get property names |
| `NeatooOrdinalConverter.cs:66` | `GetProperty("PropertyTypes")` | Get property types |
| `NeatooOrdinalConverter.cs:67` | `GetMethod("FromOrdinalArray")` | Get factory method |
| `NeatooOrdinalConverter.cs:71-81` | `GetValue()`, `Invoke()` | Read metadata values |

### Current Generated Code

```csharp
// Person.Ordinal.g.cs
partial class Person : IOrdinalSerializable, IOrdinalSerializationMetadata<Person>
{
    public static string[] PropertyNames => new[] { "Age", "Name" };

    public object?[] ToOrdinalArray()
    {
        return new object?[] { this.Age, this.Name };
    }

    public static Person FromOrdinalArray(object?[] values)
    {
        return new Person
        {
            Age = (int)values[0]!,
            Name = (string)values[1]!
        };
    }
}
```

---

## Implementation Tasks

### Task 1: Create New Interface

**File:** `src/RemoteFactory/IOrdinalConverterProvider.cs` (NEW)

Create interface for types that provide their own converter:

```csharp
using System.Text.Json.Serialization;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Implemented by [Factory] types to provide a pre-compiled ordinal JSON converter.
/// Eliminates reflection-based converter creation for AOT compatibility.
/// </summary>
/// <typeparam name="TSelf">The implementing type (self-referencing generic).</typeparam>
public interface IOrdinalConverterProvider<TSelf> where TSelf : class
{
    /// <summary>
    /// Creates a strongly-typed ordinal converter for this type.
    /// </summary>
    static abstract JsonConverter<TSelf> CreateOrdinalConverter();
}
```

- [x] Create `IOrdinalConverterProvider.cs`
- [x] Add XML documentation

---

### Task 2: Update NeatooOrdinalConverterFactory

**File:** `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs`

Add converter registration and cache:

```csharp
public class NeatooOrdinalConverterFactory : JsonConverterFactory
{
    private readonly NeatooSerializationOptions options;

    // NEW: Static cache for registered converters (AOT path)
    private static readonly ConcurrentDictionary<Type, JsonConverter> _registeredConverters = new();

    // NEW: Registration method called at startup
    public static void RegisterConverter<T>(JsonConverter<T> converter) where T : class
    {
        _registeredConverters.TryAdd(typeof(T), converter);
    }

    // NEW: Clear registrations (for testing)
    internal static void ClearRegistrations()
    {
        _registeredConverters.Clear();
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        // AOT path: Try registered converters first
        if (_registeredConverters.TryGetValue(typeToConvert, out var registered))
        {
            return registered;
        }

        // Reflection fallback path (existing code)
        // ... existing MakeGenericType logic ...
    }
}
```

- [x] Add `_registeredConverters` static dictionary
- [x] Add `RegisterConverter<T>()` method
- [x] Add `ClearRegistrations()` for testing
- [x] Modify `CreateConverter()` to check cache first
- [x] Keep existing reflection fallback for backward compatibility

---

### Task 3: Update Source Generator - Converter Generation

**File:** `src/Generator/FactoryGenerator.cs`

Add method to generate strongly-typed converter class:

```csharp
private void GenerateOrdinalConverter(StringBuilder sb, TypeInfo typeInfo)
{
    var typeName = typeInfo.Name;
    var fullTypeName = typeInfo.FullName;
    var properties = typeInfo.OrdinalProperties; // Alphabetically sorted

    sb.AppendLine($@"
/// <summary>
/// Strongly-typed ordinal converter for {typeName}. No reflection required.
/// </summary>
internal sealed class {typeName}OrdinalConverter : global::System.Text.Json.Serialization.JsonConverter<{fullTypeName}>
{{
    public override {fullTypeName}? Read(
        ref global::System.Text.Json.Utf8JsonReader reader,
        global::System.Type typeToConvert,
        global::System.Text.Json.JsonSerializerOptions options)
    {{
        if (reader.TokenType == global::System.Text.Json.JsonTokenType.Null)
            return null;

        if (reader.TokenType != global::System.Text.Json.JsonTokenType.StartArray)
            throw new global::System.Text.Json.JsonException(
                $""Expected StartArray for {typeName} ordinal format, got {{reader.TokenType}}"");

        reader.Read(); // Move past StartArray
");

    // Generate property reads
    for (int i = 0; i < properties.Count; i++)
    {
        var prop = properties[i];
        var propType = prop.TypeFullName;
        var isNullable = prop.IsNullable;

        sb.AppendLine($@"
        // {prop.Name} ({propType}) - position {i}
        var prop{i} = global::System.Text.Json.JsonSerializer.Deserialize<{propType}>(ref reader, options);
        reader.Read();");
    }

    sb.AppendLine($@"
        if (reader.TokenType != global::System.Text.Json.JsonTokenType.EndArray)
            throw new global::System.Text.Json.JsonException(
                $""Too many values in ordinal array for {typeName}. Expected {properties.Count}."");

        return new {fullTypeName}
        {{");

    // Generate property assignments
    for (int i = 0; i < properties.Count; i++)
    {
        var prop = properties[i];
        var nullForgiving = prop.IsNullable ? "" : "!";
        sb.AppendLine($"            {prop.Name} = prop{i}{nullForgiving},");
    }

    sb.AppendLine($@"        }};
    }}

    public override void Write(
        global::System.Text.Json.Utf8JsonWriter writer,
        {fullTypeName} value,
        global::System.Text.Json.JsonSerializerOptions options)
    {{
        if (value == null)
        {{
            writer.WriteNullValue();
            return;
        }}

        writer.WriteStartArray();");

    // Generate property writes
    for (int i = 0; i < properties.Count; i++)
    {
        var prop = properties[i];
        sb.AppendLine($"        global::System.Text.Json.JsonSerializer.Serialize(writer, value.{prop.Name}, options);");
    }

    sb.AppendLine($@"        writer.WriteEndArray();
    }}
}}");
}
```

- [x] Add `GenerateOrdinalConverter()` method
- [x] Handle all property types (primitives, objects, collections, nullables)
- [x] Handle inheritance (base class properties first)
- [x] Handle records with primary constructors
- [x] Generate fully-qualified type names to avoid using conflicts

---

### Task 4: Update Source Generator - Interface Implementation

**File:** `src/Generator/FactoryGenerator.cs`

Update `GenerateOrdinalSerialization()` to add the new interface:

```csharp
// Current: partial class Person : IOrdinalSerializable, IOrdinalSerializationMetadata<Person>
// New:     partial class Person : IOrdinalSerializable, IOrdinalSerializationMetadata<Person>, IOrdinalConverterProvider<Person>

// Add to interface list
sb.Append($", global::Neatoo.RemoteFactory.IOrdinalConverterProvider<{fullTypeName}>");

// Add CreateOrdinalConverter method
sb.AppendLine($@"
    /// <summary>
    /// Creates an AOT-compatible ordinal converter for this type.
    /// </summary>
    public static global::System.Text.Json.Serialization.JsonConverter<{fullTypeName}> CreateOrdinalConverter()
        => new {typeName}OrdinalConverter();
");
```

- [x] Add `IOrdinalConverterProvider<T>` to interface list
- [x] Generate `CreateOrdinalConverter()` static method
- [x] Ensure method returns the generated converter type

---

### Task 5: Update Source Generator - Registration

**File:** `src/Generator/FactoryGenerator.cs`

Update factory registration to register converters at startup:

```csharp
// In FactoryServiceRegistrar method
sb.AppendLine($@"
        // Register AOT-compatible ordinal converters
        global::Neatoo.RemoteFactory.Internal.NeatooOrdinalConverterFactory.RegisterConverter(
            {fullTypeName}.CreateOrdinalConverter());
");
```

- [x] Add converter registration to `FactoryServiceRegistrar`
- [x] Ensure registration happens before any serialization
- [x] Handle multiple types in same assembly

---

### Task 6: Handle Special Cases in Generator

**File:** `src/Generator/FactoryGenerator.cs`

Handle edge cases in converter generation:

#### 6.1 Nullable Value Types

```csharp
// Property: int? Age
var prop0 = global::System.Text.Json.JsonSerializer.Deserialize<int?>(ref reader, options);
```

#### 6.2 Nullable Reference Types

```csharp
// Property: string? MiddleName (nullable)
var prop1 = global::System.Text.Json.JsonSerializer.Deserialize<string?>(ref reader, options);

// Property: string Name (non-nullable)
var prop2 = global::System.Text.Json.JsonSerializer.Deserialize<string>(ref reader, options);
// Assignment uses null-forgiving: Name = prop2!
```

#### 6.3 Nested Factory Types

```csharp
// Property: Address HomeAddress (another [Factory] type)
// Uses the same serialization mechanism - JsonSerializer handles it
var prop3 = global::System.Text.Json.JsonSerializer.Deserialize<Address>(ref reader, options);
```

#### 6.4 Collections

```csharp
// Property: List<string> Tags
var prop4 = global::System.Text.Json.JsonSerializer.Deserialize<global::System.Collections.Generic.List<string>>(ref reader, options);
```

#### 6.5 Interface Properties

```csharp
// Property: IAddress BillingAddress (interface)
// This will use NeatooInterfaceJsonTypeConverter - no special handling needed
var prop5 = global::System.Text.Json.JsonSerializer.Deserialize<IAddress>(ref reader, options);
```

#### 6.6 Records with Primary Constructors

```csharp
// record Person(string Name, int Age)
// Must use constructor, not object initializer
return new Person(
    Name: prop1!,
    Age: prop0
);
```

- [x] Handle nullable value types
- [x] Handle nullable reference types
- [x] Handle nested Factory types
- [x] Handle collections (List, Array, etc.)
- [x] Handle interface-typed properties
- [x] Handle records with primary constructors
- [x] Handle inheritance (call base properties first)

---

### Task 7: Update Existing NeatooOrdinalConverter

**File:** `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs`

Keep the reflection-based converter as fallback but add deprecation path:

```csharp
public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
{
    // AOT path: registered converters
    if (_registeredConverters.TryGetValue(typeToConvert, out var registered))
    {
        return registered;
    }

    // Check if type implements IOrdinalConverterProvider<T>
    var providerInterface = typeToConvert.GetInterfaces()
        .FirstOrDefault(i => i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IOrdinalConverterProvider<>));

    if (providerInterface != null)
    {
        // Call static abstract CreateOrdinalConverter via reflection (one-time cost)
        var method = typeToConvert.GetMethod(
            nameof(IOrdinalConverterProvider<object>.CreateOrdinalConverter),
            BindingFlags.Public | BindingFlags.Static);

        if (method != null)
        {
            var converter = (JsonConverter)method.Invoke(null, null)!;
            _registeredConverters.TryAdd(typeToConvert, converter);
            return converter;
        }
    }

    // Ultimate fallback: reflection-based converter (legacy/non-generated types)
    var converterType = typeof(NeatooOrdinalConverter<>).MakeGenericType(typeToConvert);
    var fallbackConverter = (JsonConverter)Activator.CreateInstance(converterType)!;
    _cachedFallbackConverters.TryAdd(typeToConvert, fallbackConverter);
    return fallbackConverter;
}
```

- [x] Add IOrdinalConverterProvider check as middle path
- [x] Cache converters from provider interface
- [x] Keep MakeGenericType fallback for non-generated types
- [ ] Add logging/diagnostics for which path is used (deferred - not critical)

---

### Task 8: Add Tests

**File:** `src/Tests/FactoryGeneratorTests/Factory/ReflectionFreeSerializationTests.cs` (NEW)

```csharp
public class ReflectionFreeSerializationTests : FactoryTestBase<ISimpleRecordFactory>
{
    [Fact]
    public void GeneratedConverter_IsRegistered()
    {
        // Verify converter is in cache after DI setup
        var converterType = typeof(SimpleRecord).Assembly
            .GetType("Neatoo.RemoteFactory.FactoryGeneratorTests.Factory.SimpleRecordOrdinalConverter");

        Assert.NotNull(converterType);
    }

    [Fact]
    public void GeneratedConverter_SerializesCorrectly()
    {
        var record = new SimpleRecord("Test", 42);
        var json = JsonSerializer.Serialize(record, GetJsonOptions());

        Assert.Equal("[\"Test\",42]", json); // Ordinal format
    }

    [Fact]
    public void GeneratedConverter_DeserializesCorrectly()
    {
        var json = "[\"Test\",42]";
        var record = JsonSerializer.Deserialize<SimpleRecord>(json, GetJsonOptions());

        Assert.Equal("Test", record.Name);
        Assert.Equal(42, record.Value);
    }

    [Fact]
    public void GeneratedConverter_HandlesNull()
    {
        var json = "null";
        var record = JsonSerializer.Deserialize<SimpleRecord?>(json, GetJsonOptions());

        Assert.Null(record);
    }

    [Fact]
    public void GeneratedConverter_HandlesNullableProperties()
    {
        // Test type with nullable properties
    }

    [Fact]
    public void GeneratedConverter_HandlesNestedTypes()
    {
        // Test type containing another [Factory] type
    }

    [Fact]
    public void GeneratedConverter_HandlesCollections()
    {
        // Test type with List<T> property
    }

    [Fact]
    public void GeneratedConverter_HandlesInheritance()
    {
        // Test derived type serialization
    }

    [Fact]
    public void GeneratedConverter_RoundTripsViaClientServer()
    {
        // Full client/server round-trip test
    }
}
```

- [x] Create test file
- [x] Test converter registration
- [x] Test serialization correctness
- [x] Test deserialization correctness
- [x] Test null handling
- [x] Test nullable properties
- [x] Test nested types
- [x] Test collections
- [x] Test inheritance
- [x] Test client/server round-trip
- [x] Test record types
- [x] Test error cases (malformed JSON)

---

### Task 9: Update Documentation

**Files:**
- `docs/advanced/json-serialization.md`
- `docs/release-notes/v10.3.0.md` (or next version)

Add section on AOT-friendly serialization:

```markdown
## AOT-Compatible Serialization

RemoteFactory generates strongly-typed JSON converters for each `[Factory]` type,
eliminating runtime reflection for ordinal serialization.

### How It Works

The source generator creates:
1. A `PersonOrdinalConverter : JsonConverter<Person>` class
2. Implementation of `IOrdinalConverterProvider<Person>`
3. Registration code that runs at startup

### Benefits

- Faster startup (no reflection-based converter creation)
- Better AOT trimming support
- Reduced memory allocation
- Compile-time validation of serialization logic
```

- [ ] Update json-serialization.md with AOT section (deferred - documentation update)
- [ ] Create release notes for this feature (deferred - will be done with release)
- [ ] Document any limitations (deferred - documentation update)

---

## File Changes Summary

| File | Change Type | Description |
|------|-------------|-------------|
| `src/RemoteFactory/IOrdinalConverterProvider.cs` | NEW | New interface |
| `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs` | MODIFY | Add registration, caching |
| `src/Generator/FactoryGenerator.cs` | MODIFY | Generate converters |
| `src/Tests/.../ReflectionFreeSerializationTests.cs` | NEW | Tests |
| `docs/advanced/json-serialization.md` | MODIFY | Documentation |

---

## Testing Strategy

### Unit Tests
- Converter generation for all property types
- Serialization/deserialization correctness
- Null handling
- Error cases

### Integration Tests
- Client/server round-trip with generated converters
- Mixed scenarios (some types generated, some fallback)

### Performance Tests
- Startup time comparison (before/after)
- Serialization throughput comparison

### AOT Verification
- Run with `PublishTrimmed=true`
- Verify no trimming warnings for serialization

---

## Rollout Plan

1. **Implementation** - Generate converters alongside existing code
2. **Testing** - Comprehensive test coverage
3. **Parallel Run** - Both paths active, verify identical behavior
4. **Default** - Make generated converters the primary path
5. **Deprecation** - Mark reflection fallback as legacy

---

## Success Criteria

- [ ] All existing serialization tests pass
- [ ] No `MakeGenericType` calls for types with generated converters
- [ ] No `Activator.CreateInstance` for converter creation
- [ ] No `GetProperty`/`GetMethod`/`Invoke` for ordinal metadata
- [ ] Startup time improved (measure)
- [ ] Build with `PublishTrimmed=true` succeeds without warnings
- [ ] Documentation updated

---

## Estimated Effort

| Task | Effort |
|------|--------|
| Task 1: New interface | 0.5 hours |
| Task 2: Update converter factory | 1 hour |
| Task 3: Generator - converter class | 3 hours |
| Task 4: Generator - interface impl | 1 hour |
| Task 5: Generator - registration | 0.5 hours |
| Task 6: Special cases | 2 hours |
| Task 7: Fallback path | 1 hour |
| Task 8: Tests | 3 hours |
| Task 9: Documentation | 1 hour |
| **Total** | **~13 hours** |

---

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Generated code bloat | Measure assembly size, optimize if needed |
| Edge cases not covered | Comprehensive test suite |
| Breaking existing behavior | Keep fallback path, parallel testing |
| Complex property types | Use JsonSerializer.Deserialize for nested types |

---

## Future Phases (Out of Scope)

- **Phase 2**: Generate `JsonSerializerContext` for Named format
- **Phase 3**: Hybrid resolver combining all approaches
- **Phase 4**: Full AOT support for interface polymorphism (requires wire format changes)
