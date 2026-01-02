# Compact JSON Serialization Plan

## Implementation Status

**Status:** COMPLETED - Implementation Notes

| Phase | Status | Notes |
|-------|--------|-------|
| Phase 1: Property Order Infrastructure | ✅ Completed | Alphabetical ordering, base class first |
| Phase 2: Custom JsonConverter | ✅ Completed | NeatooOrdinalConverterFactory |
| Phase 3: Source Generator Changes | ✅ Completed | IOrdinalSerializable, IOrdinalSerializationMetadata |
| Phase 4: Interface Serialization | ✅ Completed | Uses `$type`/`$value` (reads both) |
| Phase 5: Reference Handling | ✅ Completed | Works with `$id`/`$ref` |
| Phase 6: ObjectJson Updates | ⏸️ DEFERRED | Property names remain `Json`/`AssemblyType` |
| Phase 7: Backward Compatibility | ✅ Completed | Server detects format, X-Neatoo-Format header |

**Key Implementation Details:**
- Actual options class: `NeatooSerializationOptions` (not `NeatooServerOptions`)
- Actual feature flag: `SerializationFormat.Named` enum (not `UseVerboseSerialization`)
- Phase 4: Write uses `$type`/`$value`, Read accepts both `$type`/`$value` AND `$t`/`$v`
- Phase 6: Deferred - ObjectJson still uses full property names for stability

---

## Overview

Eliminate property names from serialized JSON by using ordinal (positional) arrays instead of named objects. Since both client and server have the compiled type definitions, property names are redundant - only type names are needed for interface polymorphism.

## Motivation

Current serialization includes property names that are redundant when type schema is known:

| Current Format | Compact Format | Savings |
|----------------|----------------|---------|
| `{"Name":"John","Age":42,"Active":true}` | `["John",42,true]` | ~45% |
| `{"FirstName":"Alice","LastName":"Smith","Email":"a@b.com"}` | `["Alice","Smith","a@b.com"]` | ~50% |

For typical DTOs with 5-10 properties, payload reduction is **40-50%**.

## Current Architecture

### Serialization Flow

```
┌─ RemoteRequestDto ─────────────────────────────────────────────┐
│  DelegateAssemblyType: "Namespace.Factory+FetchDelegate"       │
│  Parameters: ──────────────────────────────────────────────── │
│    ┌─ ObjectJson[] ────────────────────────────────────────┐  │
│    │  AssemblyType: "Namespace.Person"    ← KEEP (type)    │  │
│    │  Json: "{"Name":"John","Age":42}"    ← OPTIMIZE       │  │
│    └───────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────┘
```

### Key Files

| File | Purpose |
|------|---------|
| `src/RemoteFactory/Internal/NeatooJsonSerializer.cs` | Main serializer (224 lines) |
| `src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs` | Interface `$type`/`$value` wrapper |
| `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs` | Type resolution |
| `src/RemoteFactory/Internal/NeatooReferenceHandler.cs` | Circular reference handling |
| `src/RemoteFactory/Internal/ObjectJson.cs` | Object wrapper with type info |

### Interface Serialization (Current)

```json
{
  "$type": "ConcreteImplementation.FullName",
  "$value": { "Name": "John", "Age": 42 }
}
```

## Proposed Compact Format

### Standard Objects

**Current:**
```json
{
  "Json": "{\"Name\":\"John\",\"Age\":42,\"Active\":true}",
  "AssemblyType": "Namespace.Person"
}
```

**Compact:**
```json
{
  "J": "[\"John\",42,true]",
  "T": "Namespace.Person"
}
```

### Interface Types

**Current:**
```json
{
  "$type": "Namespace.ConcretePerson",
  "$value": { "Name": "John", "Age": 42 }
}
```

**Compact:**
```json
{
  "$t": "Namespace.ConcretePerson",
  "$v": ["John", 42]
}
```

### Nested Objects

**Current:**
```json
{
  "Name": "John",
  "Address": { "Street": "123 Main", "City": "Boston" }
}
```

**Compact:**
```json
["John", ["123 Main", "Boston"]]
```

### Null Handling

Since ordinal arrays require fixed positions, nulls must be explicit:

```json
["John", null, true]  // Middle property is null
```

### Collections

Collections serialize as arrays within the positional array:

```json
["John", ["item1", "item2", "item3"], 42]
```

## Implementation Plan

### Phase 1: Property Order Infrastructure

The source generator must establish deterministic property ordering.

#### 1.1 Define Property Order Strategy

**Option A: Alphabetical Order (Recommended)**
- Deterministic across compilations
- No dependency on declaration order
- Consistent with System.Text.Json default behavior

**Option B: Declaration Order**
- Matches developer mental model
- Requires stable reflection order (not guaranteed)

**Decision: Use alphabetical order by property name.**

#### 1.2 Generate Property Metadata

Add to generated factory code:

```csharp
// Generated in factory implementation
internal static class PersonSerializationMetadata
{
    public static readonly string[] PropertyOrder = new[] { "Active", "Age", "Name" };
    public static readonly Type[] PropertyTypes = new[] { typeof(bool), typeof(int), typeof(string) };
}
```

#### 1.3 Create IOrdinalSerializable Interface

```csharp
namespace Neatoo.RemoteFactory;

/// <summary>
/// Marker interface for types that support ordinal serialization.
/// Generated by the source generator for [Factory] types.
/// </summary>
public interface IOrdinalSerializable
{
    /// <summary>
    /// Returns property values in ordinal order for serialization.
    /// </summary>
    object?[] ToOrdinalArray();
}
```

### Phase 2: Custom JsonConverter Implementation

#### 2.1 Create NeatooOrdinalConverter<T>

```csharp
public class NeatooOrdinalConverter<T> : JsonConverter<T> where T : IOrdinalSerializable
{
    private readonly string[] _propertyOrder;
    private readonly Type[] _propertyTypes;
    private readonly Func<object?[], T> _factory;

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Read JSON array
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected array for ordinal serialization");

        var values = new object?[_propertyOrder.Length];
        int index = 0;

        reader.Read(); // Move past StartArray
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            values[index] = JsonSerializer.Deserialize(ref reader, _propertyTypes[index], options);
            index++;
            reader.Read();
        }

        return _factory(values);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var values = value.ToOrdinalArray();
        writer.WriteStartArray();
        for (int i = 0; i < values.Length; i++)
        {
            JsonSerializer.Serialize(writer, values[i], _propertyTypes[i], options);
        }
        writer.WriteEndArray();
    }
}
```

#### 2.2 Create NeatooOrdinalConverterFactory

```csharp
public class NeatooOrdinalConverterFactory : JsonConverterFactory
{
    private readonly IServiceAssemblies _serviceAssemblies;

    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(IOrdinalSerializable).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        // Get metadata and create converter
        var metadata = GetSerializationMetadata(typeToConvert);
        var converterType = typeof(NeatooOrdinalConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType, metadata);
    }
}
```

### Phase 3: Source Generator Changes

#### 3.1 Generate IOrdinalSerializable Implementation

For each `[Factory]` type, generate:

```csharp
// Generated code for Person class
partial class Person : IOrdinalSerializable
{
    private static readonly string[] __PropertyOrder = new[] { "Active", "Age", "Name" };

    public object?[] ToOrdinalArray()
    {
        return new object?[] { this.Active, this.Age, this.Name };
    }

    internal static Person FromOrdinalArray(object?[] values)
    {
        return new Person
        {
            Active = (bool)values[0],
            Age = (int)values[1],
            Name = (string)values[2]
        };
    }
}
```

#### 3.2 Generate Converter Registration

Add to generated factory:

```csharp
// In AddRemoteFactory extension method
services.AddSingleton<JsonConverter>(sp =>
    new NeatooOrdinalConverter<Person>(
        Person.__PropertyOrder,
        Person.FromOrdinalArray));
```

#### 3.3 Handle Record Types

For records with primary constructors:

```csharp
// Generated for record Person(string Name, int Age, bool Active)
partial record Person : IOrdinalSerializable
{
    public object?[] ToOrdinalArray()
    {
        return new object?[] { this.Active, this.Age, this.Name }; // Alphabetical
    }

    internal static Person FromOrdinalArray(object?[] values)
    {
        return new Person(
            Name: (string)values[2],
            Age: (int)values[1],
            Active: (bool)values[0]
        );
    }
}
```

### Phase 4: Interface Serialization Updates

> **Implementation Note:** The actual implementation uses full property names (`$type`/`$value`) for writing,
> but reads both full names AND shortened names (`$t`/`$v`) for backward compatibility.

#### 4.1 Update NeatooInterfaceJsonTypeConverter

Modify to use ordinal format for the `$value`:

```csharp
public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
{
    writer.WriteStartObject();
    writer.WritePropertyName("$type");  // Full name for stability
    writer.WriteStringValue(value.GetType().FullName);
    writer.WritePropertyName("$value");  // Full name for stability

    if (value is IOrdinalSerializable ordinal)
    {
        // Use ordinal array format
        var values = ordinal.ToOrdinalArray();
        writer.WriteStartArray();
        foreach (var v in values)
            JsonSerializer.Serialize(writer, v, options);
        writer.WriteEndArray();
    }
    else
    {
        // Fallback to standard serialization
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }

    writer.WriteEndObject();
}

// Reading accepts both formats for compatibility:
// - "$type"/"$value" (current)
// - "$t"/"$v" (future/compact)
```

### Phase 5: Reference Handling Updates

#### 5.1 Update NeatooReferenceResolver

Reference IDs continue to work with ordinal format:

```json
{
  "$id": "1",
  "$v": ["John", 42, true]
}
```

Or for references:
```json
{ "$ref": "1" }
```

### Phase 6: ObjectJson Updates

> **Status: DEFERRED** - Not implemented in initial release.
> The actual implementation retains full property names (`Json`, `AssemblyType`) for stability and debugging.
> This optimization may be revisited in a future release.

#### 6.1 Shorten Property Names (DEFERRED)

```csharp
// DEFERRED: Current implementation uses full property names
public class ObjectJson
{
    // Actual: public string Json { get; }
    // Actual: public string AssemblyType { get; }

    // Proposed (not implemented):
    // [JsonPropertyName("J")]
    // public string Json { get; }
    // [JsonPropertyName("T")]
    // public string AssemblyType { get; }
}
```

### Phase 7: Backward Compatibility

#### 7.1 Version Detection

Add version marker to requests:

```csharp
public class RemoteRequestDto
{
    [JsonPropertyName("V")]
    public int Version { get; set; } = 2;  // 1 = named, 2 = ordinal

    [JsonPropertyName("D")]
    public string DelegateAssemblyType { get; set; }

    [JsonPropertyName("P")]
    public IReadOnlyCollection<ObjectJson?>? Parameters { get; set; }
}
```

#### 7.2 Dual-Mode Deserialization

```csharp
public object? Deserialize(string json, Type targetType)
{
    // Detect format by first character
    if (json.TrimStart().StartsWith('['))
    {
        // Ordinal format
        return DeserializeOrdinal(json, targetType);
    }
    else
    {
        // Named format (legacy)
        return DeserializeNamed(json, targetType);
    }
}
```

## Edge Cases

### Nullable Properties

```csharp
public class Person
{
    public string Name { get; set; }
    public string? MiddleName { get; set; }  // Nullable
    public int Age { get; set; }
}
```

Serializes as:
```json
["John", null, 42]
```

### Inheritance

For derived types, include base properties first (alphabetically within each level):

```csharp
public class Employee : Person
{
    public string Department { get; set; }
}
```

Property order: `Active, Age, Name` (base) + `Department` (derived)
```json
[true, 42, "John", "Engineering"]
```

### Collections

Collections serialize as nested arrays:

```csharp
public class Team
{
    public string Name { get; set; }
    public List<string> Members { get; set; }
}
```

```json
["Engineering", ["Alice", "Bob", "Charlie"]]
```

### Dictionaries

Dictionaries serialize as array of key-value pairs:

```csharp
public class Config
{
    public Dictionary<string, string> Settings { get; set; }
}
```

```json
[[["key1", "value1"], ["key2", "value2"]]]
```

### Nested Objects

Nested objects also use ordinal format:

```csharp
public class Order
{
    public Address BillingAddress { get; set; }
    public string OrderId { get; set; }
}
```

```json
[["123 Main", "Boston", "MA"], "ORD-001"]
```

## Files to Modify

### Core Library (src/RemoteFactory/)

| File | Changes |
|------|---------|
| `Internal/NeatooJsonSerializer.cs` | Add ordinal serialization path |
| `Internal/NeatooInterfaceJsonTypeConverter.cs` | Use `$t`/`$v` with ordinal |
| `Internal/ObjectJson.cs` | Shorten property names to `J`/`T` |
| `RemoteRequestDto.cs` | Add version, shorten property names |
| `RemoteResponse.cs` | Shorten property names |
| `IOrdinalSerializable.cs` | **NEW** - Marker interface |
| `Internal/NeatooOrdinalConverter.cs` | **NEW** - Custom converter |
| `Internal/NeatooOrdinalConverterFactory.cs` | **NEW** - Converter factory |

### Source Generator (src/Generator/)

| File | Changes |
|------|---------|
| `FactoryGenerator.Emitter.cs` | Generate IOrdinalSerializable implementation |
| `FactoryGenerator.Types.cs` | Collect property order metadata |
| `Templates/` | Update templates for ordinal serialization |

### Tests

| File | Purpose |
|------|---------|
| `FactoryGeneratorTests/Serialization/OrdinalSerializationTests.cs` | **NEW** - Ordinal format tests |
| `FactoryGeneratorTests/Serialization/BackwardCompatibilityTests.cs` | **NEW** - Named format still works |
| `FactoryGeneratorTests/Serialization/EdgeCaseTests.cs` | **NEW** - Nulls, inheritance, collections |

## Test Plan

### Unit Tests

| Test | Description |
|------|-------------|
| `Serialize_SimpleObject_UsesOrdinalArray` | Basic ordinal output |
| `Serialize_PreservesPropertyOrder_Alphabetical` | Order is alphabetical |
| `Deserialize_OrdinalArray_ReconstructsObject` | Round-trip works |
| `Serialize_NullProperty_ExplicitNull` | Nulls are explicit |
| `Serialize_NestedObject_RecursiveOrdinal` | Nested objects work |
| `Serialize_Collection_AsNestedArray` | Collections work |
| `Serialize_Dictionary_AsKeyValuePairs` | Dictionaries work |
| `Serialize_Interface_UsesDollarTDollarV` | Interface format |
| `Serialize_InheritedType_IncludesBaseProperties` | Inheritance works |
| `Deserialize_LegacyFormat_StillWorks` | Backward compatibility |

### Integration Tests

| Test | Description |
|------|-------------|
| `ClientServer_OrdinalFormat_RoundTrips` | Two-container test |
| `HTTP_OrdinalFormat_Works` | ASP.NET Core integration |
| `MixedVersions_Compatible` | Old client, new server |

### Performance Tests

| Test | Description |
|------|-------------|
| `PayloadSize_Reduction_40Percent` | Verify size reduction |
| `Serialization_Speed_NotRegressed` | No perf regression |

## Success Criteria

- [ ] Property names eliminated from inner JSON
- [ ] Type names preserved for polymorphism
- [ ] Ordinal format uses ~40-50% less bytes
- [ ] Backward compatible with named format
- [ ] All existing tests pass
- [ ] Round-trip serialization verified
- [ ] Interface polymorphism works
- [ ] Circular references work
- [ ] Nested objects work
- [ ] Collections work
- [ ] Null handling works
- [ ] Inheritance works

## Resolved Decisions

1. **Property order for inheritance**: Base properties first, then derived, each level alphabetical.
   - Document in appendix/deep-dive section, not main usage docs.

2. **Default format**: Ordinal (compact) is the new default.
   - Feature flag `UseVerboseSerialization` to fall back to named format.
   - All serialization tests must run against both formats.

3. **Debugging/Logging**: Add comprehensive logging for serialization operations.

## Version Negotiation Design

**Requirement**: Server controls the format, communicates to client via response header.

### Response Header Approach

```
X-Neatoo-Format: ordinal
```

**Flow:**
1. Client makes first remote call (uses ordinal by default)
2. Server includes `X-Neatoo-Format` header in response
3. Client reads header, caches the format preference
4. Subsequent requests use cached format
5. Server always accepts both formats (detects by `[` vs `{`)
6. Server responds in its configured format

**Benefits:**
- No extra endpoint
- Server is authoritative
- Self-correcting on format mismatch
- Graceful fallback (server accepts both)

### Implementation

```csharp
// Server configuration
public class NeatooServerOptions
{
    public SerializationFormat Format { get; set; } = SerializationFormat.Ordinal;
}

public enum SerializationFormat
{
    Ordinal,  // Compact array format (default)
    Named     // Verbose object format (debug/compatibility)
}

// Client-side
public class NeatooClientOptions
{
    public SerializationFormat? Format { get; set; }  // null = discover from server
}

// ASP.NET Core middleware adds header
app.Use(async (context, next) =>
{
    await next();
    context.Response.Headers["X-Neatoo-Format"] =
        options.Format == SerializationFormat.Ordinal ? "ordinal" : "named";
});
```

### Format Discovery Sequence

```
┌─────────┐                              ┌─────────┐
│ Client  │                              │ Server  │
└────┬────┘                              └────┬────┘
     │                                        │
     │  POST /neatoo/remote (ordinal body)    │
     │───────────────────────────────────────>│
     │                                        │
     │  Response + X-Neatoo-Format: ordinal   │
     │<───────────────────────────────────────│
     │                                        │
     │  [Cache format = ordinal]              │
     │                                        │
     │  POST /neatoo/remote (ordinal body)    │
     │───────────────────────────────────────>│
     │                                        │
```

### Mismatch Handling

If client sends ordinal but server wants named (or vice versa):
- Server detects format by first character (`[` = ordinal, `{` = named)
- Server deserializes correctly regardless
- Server responds in its preferred format
- Client adapts on next request based on response header

## Documentation Plan

### Main Documentation (docs/concepts/)
- Brief mention that serialization is optimized for size
- Link to appendix for details

### Appendix Documentation (docs/internals/)

Create `docs/internals/serialization-format.md`:

1. **Format Overview**: Ordinal vs Named
2. **Property Ordering Rules**: Alphabetical, inheritance order
3. **Type Information**: When `$t`/`$v` wrapper is used
4. **Reference Handling**: `$id`/`$ref` with ordinal format
5. **Edge Cases**: Nulls, collections, dictionaries, nested objects
6. **Configuration**: How to switch formats, response header
7. **Debugging**: Logging, verbose mode