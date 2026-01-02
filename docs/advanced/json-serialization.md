---
layout: default
title: "JSON Serialization"
description: "Custom serialization with NeatooJsonSerializer"
parent: Advanced
nav_order: 4
---

# JSON Serialization

RemoteFactory uses System.Text.Json for serializing objects between client and server. This document covers how serialization works and how to customize it.

## Serialization Formats

RemoteFactory supports two JSON serialization formats for domain objects.

### Ordinal Format (Default)

Compact array-based format that eliminates property names. Properties are serialized in alphabetical order, with base class properties first.

```json
["John", 42, true]
```

This format reduces payload sizes by 40-50% compared to named format.

### Named Format

Verbose object-based format with property names:

```json
{"Active":true,"Age":42,"Name":"John"}
```

Use this format for debugging or backward compatibility.

### Configuring the Format

Configure serialization format when registering RemoteFactory:

```csharp
// Server - use named format for debugging
services.AddNeatooRemoteFactory(
    NeatooFactory.Server,
    new NeatooSerializationOptions { Format = SerializationFormat.Named },
    typeof(MyModel).Assembly);

// Client - typically matches server
services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,
    new NeatooSerializationOptions { Format = SerializationFormat.Named },
    typeof(MyModel).Assembly);
```

### Format Negotiation

The server communicates its preferred format via the `X-Neatoo-Format` HTTP header:

```
X-Neatoo-Format: ordinal
```

- Server always accepts both formats (detects by first character: `[` = ordinal, `{` = named)
- Server responds in its configured format
- Clients adapt based on the response header

### IOrdinalSerializable Interface

The source generator implements `IOrdinalSerializable` on `[Factory]` types:

```csharp
public interface IOrdinalSerializable
{
    object?[] ToOrdinalArray();
}
```

This interface enables efficient ordinal serialization without reflection.

---

## Default Serialization

### NeatooJsonSerializer

RemoteFactory provides `NeatooJsonSerializer` which handles:

- Interface type serialization
- Reference preservation (circular references)
- Custom type resolution across assemblies

```csharp
public class NeatooJsonSerializer : INeatooJsonSerializer
{
    private readonly IServiceAssemblies serviceAssemblies;
    JsonSerializerOptions Options { get; }
    private NeatooReferenceHandler ReferenceHandler { get; }

    public NeatooJsonSerializer(
        IEnumerable<NeatooJsonConverterFactory> converterFactories,
        IServiceAssemblies serviceAssemblies,
        NeatooJsonTypeInfoResolver typeInfoResolver)
    {
        this.Options = new JsonSerializerOptions
        {
            ReferenceHandler = this.ReferenceHandler,
            TypeInfoResolver = typeInfoResolver,
            WriteIndented = true,
            IncludeFields = true
        };

        foreach (var factory in converterFactories)
        {
            this.Options.Converters.Add(factory);
        }
    }

    public string? Serialize(object? target)
    {
        if (target == null) return null;

        using var rr = new NeatooReferenceResolver();
        this.ReferenceHandler.ReferenceResolver.Value = rr;

        return JsonSerializer.Serialize(target, this.Options);
    }

    public T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrEmpty(json)) return default;

        using var rr = new NeatooReferenceResolver();
        this.ReferenceHandler.ReferenceResolver.Value = rr;

        return JsonSerializer.Deserialize<T>(json, this.Options);
    }
}
```

### Default Options

RemoteFactory configures these options by default:

```csharp
new JsonSerializerOptions
{
    ReferenceHandler = customReferenceHandler,  // Handles circular refs
    TypeInfoResolver = neatooTypeInfoResolver,  // Custom type resolution
    WriteIndented = true,                        // Readable JSON
    IncludeFields = true                         // Serialize fields too
}
```

## Interface Serialization

RemoteFactory automatically handles interface-typed properties:

```csharp
// Domain model with interface property
public class OrderModel : IOrderModel
{
    public int Id { get; set; }
    public ICustomerModel Customer { get; set; }  // Interface type
    public List<IOrderLineModel> Lines { get; set; }  // Collection of interfaces
}
```

### NeatooInterfaceJsonConverterFactory

This converter preserves the concrete type during serialization:

```csharp
public class NeatooInterfaceJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsInterface;
    }

    public override JsonConverter? CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return new NeatooInterfaceJsonTypeConverter(typeToConvert);
    }
}
```

The serialized JSON includes type information:

```json
{
  "Id": 1,
  "Customer": {
    "$type": "MyApp.CustomerModel, MyApp.DomainModel",
    "Id": 42,
    "Name": "Acme Corp"
  }
}
```

## Reference Handling

RemoteFactory handles circular references automatically:

```csharp
// Circular reference scenario
public class ParentModel
{
    public int Id { get; set; }
    public List<ChildModel> Children { get; set; } = new();
}

public class ChildModel
{
    public int Id { get; set; }
    public ParentModel Parent { get; set; }  // Back-reference
}
```

### NeatooReferenceHandler

Custom reference handling preserves object identity:

```csharp
public class NeatooReferenceHandler : ReferenceHandler
{
    public AsyncLocal<NeatooReferenceResolver?> ReferenceResolver { get; } = new();

    public override ReferenceResolver CreateResolver()
    {
        return ReferenceResolver.Value
            ?? throw new InvalidOperationException("Reference resolver not set");
    }
}
```

Serialized with `$id` and `$ref`:

```json
{
  "$id": "1",
  "Id": 100,
  "Children": [
    {
      "$id": "2",
      "Id": 1,
      "Parent": { "$ref": "1" }
    },
    {
      "$id": "3",
      "Id": 2,
      "Parent": { "$ref": "1" }
    }
  ]
}
```

## Custom Converters

### Registering Custom Converters

Add custom converters in your DI configuration:

```csharp
builder.Services.AddSingleton<NeatooJsonConverterFactory, MyCustomConverterFactory>();
```

### Example: DateOnly Converter

```csharp
public class DateOnlyConverterFactory : NeatooJsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(DateOnly);
    }

    public override JsonConverter? CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return new DateOnlyConverter();
    }
}

public class DateOnlyConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return DateOnly.Parse(reader.GetString()!);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DateOnly value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }
}

// Register
builder.Services.AddSingleton<NeatooJsonConverterFactory, DateOnlyConverterFactory>();
```

### Example: Enum as String

```csharp
public class EnumStringConverterFactory : NeatooJsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsEnum;
    }

    public override JsonConverter? CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var converterType = typeof(EnumStringConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public class EnumStringConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return Enum.Parse<T>(value!, ignoreCase: true);
    }

    public override void Write(
        Utf8JsonWriter writer,
        T value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
```

## Type Resolution

### IServiceAssemblies

RemoteFactory needs to find types during deserialization:

```csharp
public interface IServiceAssemblies
{
    Type? FindType(string? assemblyQualifiedName);
}
```

Types are resolved from registered assemblies:

```csharp
// Registration tells RemoteFactory where to find types
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,
    typeof(PersonModel).Assembly,     // MyApp.DomainModel
    typeof(OrderModel).Assembly       // MyApp.Orders
);
```

### Remote Request/Response

The serialization wraps requests and responses:

```csharp
public class RemoteRequestDto
{
    public string DelegateAssemblyType { get; set; }  // Type to invoke
    public List<ObjectJson?>? Parameters { get; set; } // Serialized params
    public ObjectJson? Target { get; set; }            // For Save operations
}

public class ObjectJson
{
    public string Json { get; set; }           // Serialized value
    public string AssemblyType { get; set; }   // Type for deserialization
}

public class RemoteResponseDto
{
    public string? Json { get; set; }          // Serialized result
}
```

## Serialization Best Practices

### Use Simple Types

Prefer serializable types in your models:

```csharp
// Good: Simple, serializable types
public class ProductModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Avoid: Complex non-serializable types
public class BadModel
{
    public Func<int, int> Calculator { get; set; }  // Can't serialize
    public IDbConnection Connection { get; set; }   // Can't serialize
}
```

### Handle Nullability

Be explicit about nullable properties:

```csharp
public class CustomerModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";        // Non-nullable with default
    public string? MiddleName { get; set; }        // Explicitly nullable
    public DateTime? DeletedAt { get; set; }       // Nullable DateTime
}
```

### Avoid Large Objects

For large data, use streaming or pagination:

```csharp
// Bad: Serializing huge collection
[Execute]
public static Task<List<LogEntry>> GetAllLogs() { }  // Could be millions

// Good: Paginated
[Execute]
public static Task<PagedResult<LogEntry>> GetLogs(int page, int pageSize) { }

// Good: Streaming (return URL for file download)
[Execute]
public static Task<string> GenerateLogExportUrl() { }
```

### Mark Non-Serializable Properties

Use `[JsonIgnore]` for properties that shouldn't serialize:

```csharp
public class PersonModel : IPersonModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    [JsonIgnore]
    public bool IsDirty { get; set; }  // Local tracking only

    [JsonIgnore]
    public IPersonContext Context { get; set; }  // Service reference
}
```

## Troubleshooting

### Missing Type Error

```
MissingDelegateException: Cannot find delegate type MyApp.PersonModel in the registered assemblies
```

**Solution**: Ensure the assembly is registered:

```csharp
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,
    typeof(PersonModel).Assembly  // Register the assembly
);
```

### Circular Reference Error

```
JsonException: A possible object cycle was detected
```

**Solution**: RemoteFactory should handle this automatically. If you're using custom serialization, ensure you're using `NeatooJsonSerializer` or configure reference handling:

```csharp
var options = new JsonSerializerOptions
{
    ReferenceHandler = ReferenceHandler.Preserve
};
```

### Interface Deserialization Error

```
JsonException: Cannot deserialize interface type 'IPersonModel'
```

**Solution**: Ensure `NeatooInterfaceJsonConverterFactory` is registered. This happens automatically with `AddNeatooRemoteFactory`.

### Type Not Found

```
ArgumentNullException: Type 'MyApp.OldModel' not found
```

**Solution**: This often occurs when:
1. Type was renamed or moved
2. Assembly version mismatch between client/server
3. Type is in an unregistered assembly

Ensure client and server use the same version of domain model assemblies.

## Performance Considerations

### Minimize Payload Size

```csharp
// Send only needed data
public class PersonSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    // Don't include full address, order history, etc.
}

[Execute]
public static Task<List<PersonSummary>> GetPersonList()
{
    // Return summaries, not full models
}
```

### Use DTOs for Large Responses

```csharp
// DTO for specific use case
public class DashboardData
{
    public int TotalOrders { get; set; }
    public decimal Revenue { get; set; }
    public int PendingItems { get; set; }
}

// Instead of loading full models
[Execute]
public static Task<DashboardData> GetDashboard([Service] IContext ctx)
{
    return new DashboardData
    {
        TotalOrders = await ctx.Orders.CountAsync(),
        Revenue = await ctx.Orders.SumAsync(o => o.Total),
        PendingItems = await ctx.Items.CountAsync(i => i.Status == "Pending")
    };
}
```

## Next Steps

- **[Extending FactoryCore](extending-factory-core.md)**: Custom factory behavior
- **[Factory Operations](../concepts/factory-operations.md)**: Operation types
- **[Three-Tier Execution](../concepts/three-tier-execution.md)**: Execution modes
