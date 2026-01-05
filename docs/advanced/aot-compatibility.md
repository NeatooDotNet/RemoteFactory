---
layout: default
title: "AOT Compatibility"
description: "Native AOT compatibility and limitations in RemoteFactory"
parent: Advanced
nav_order: 10
---

# AOT Compatibility

This document describes RemoteFactory's compatibility with .NET Native AOT (Ahead-of-Time) compilation and provides guidance for AOT scenarios.

## Overview

RemoteFactory provides **partial AOT support**. The ordinal serialization system generates strongly-typed JSON converters that work without reflection, but some features have inherent limitations that prevent full AOT compatibility.

## What Works with AOT

### Ordinal Serialization (Reflection-Free)

Types that support ordinal serialization get generated strongly-typed converters:

```csharp
[Factory]
public partial class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }

    [Create]
    public static Person Create(string name, int age)
        => new() { Name = name, Age = age };
}
```

Generated converter (no reflection):
```csharp
internal sealed class PersonOrdinalConverter : JsonConverter<Person>
{
    public override Person? Read(ref Utf8JsonReader reader, ...)
    {
        // Strongly-typed deserialization - no reflection
        reader.Read();
        var prop0 = JsonSerializer.Deserialize<int>(ref reader, options);
        reader.Read();
        var prop1 = JsonSerializer.Deserialize<string>(ref reader, options);
        return new Person { Age = prop0, Name = prop1! };
    }
}
```

### Dependency Injection

.NET's dependency injection container works with AOT when properly configured. Factory operations that use `[Service]` parameters work correctly:

```csharp
[Fetch]
public static Person Fetch(Guid id, [Service] IDbContext db)
{
    // DI resolution is AOT-compatible
    return db.People.Find(id);
}
```

### Factory Operations

All factory operations (Create, Fetch, Insert, Update, Delete, Execute) work with AOT for types that support ordinal serialization.

## What Requires Reflection

### Types Requiring Constructor Injection

Types that require constructor parameters for dependency injection cannot use ordinal serialization:

```csharp
[Factory]
public partial class DomainEntity
{
    private readonly IEntityServices _services;

    // Constructor requires DI - cannot use object initializer
    public DomainEntity(IEntityServices services)
    {
        _services = services;
    }
}
```

These types fall back to **named-property serialization**, which uses System.Text.Json's default reflection-based serialization. This is **not AOT-compatible** without additional configuration.

### Interface Polymorphism

RemoteFactory's interface serialization uses runtime type discovery:

```json
{
  "$type": "MyApp.Domain.Employee",
  "$value": ["John", 42, true]
}
```

The `$type` discriminator contains a runtime type name that must be resolved dynamically. This **cannot be made AOT-compatible** without changing the wire format.

### Dynamic Delegate Invocation

The remote call dispatch mechanism uses `DynamicInvoke`, which is not AOT-friendly:

```csharp
// Internal RemoteFactory code
method.DynamicInvoke(parameters);
```

## AOT Compatibility Matrix

| Feature | AOT Compatible | Notes |
|---------|---------------|-------|
| Ordinal serialization | **Yes** | Generated typed converters |
| Named serialization | **No** | Uses reflection |
| DI container | **Yes** | When properly configured |
| Factory operations | **Partial** | Depends on serialization |
| Interface polymorphism | **No** | Runtime type resolution |
| Delegate dispatch | **No** | Uses DynamicInvoke |
| CancellationToken support | **Yes** | No reflection needed |

## Recommendations for AOT Scenarios

### 1. Use Simple DTOs for Remote Operations

For types that cross the client-server boundary, prefer simple types with parameterless constructors:

```csharp
// Good for AOT - has parameterless constructor
[Factory]
public partial class PersonDto
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
}

// Less ideal for AOT - requires constructor DI
[Factory]
public partial class PersonEntity
{
    public PersonEntity(IEntityServices services) { }
}
```

### 2. Avoid Interface-Typed Properties in Remote Types

Interface properties require polymorphic serialization:

```csharp
// Avoid for AOT
public partial class Order
{
    public ICustomer Customer { get; set; }  // Requires $type discriminator
}

// Better for AOT
public partial class Order
{
    public CustomerDto Customer { get; set; }  // Concrete type
}
```

### 3. Pre-Register All Polymorphic Types

If you must use interface polymorphism with AOT, pre-register all possible implementations:

```csharp
services.AddNeatooRemoteFactory(NeatooFactory.Server, opt =>
{
    // Pre-registration for AOT scenarios
    opt.RegisterType<Employee>();
    opt.RegisterType<Manager>();
    opt.RegisterType<Contractor>();
});
```

### 4. Consider Blazor WebAssembly

Blazor WebAssembly (RemoteFactory's primary target) does **not** use Native AOT. The IL interpreter runs managed code, so reflection works normally. AOT concerns primarily apply to:
- Console applications with `PublishAot=true`
- Server applications requiring minimal footprint
- iOS/Android native applications

## Current Limitations

### Cannot Be Made AOT-Compatible

These limitations are fundamental to RemoteFactory's architecture:

1. **Runtime type resolution** - The `$type`/`$value` polymorphism pattern requires resolving type names from JSON at runtime

2. **DynamicInvoke for delegates** - The factory delegate dispatch mechanism uses runtime invocation

3. **Types requiring constructor DI** - Cannot use object initializer syntax in generated converters

### Future Improvements

Potential future enhancements for better AOT support:

1. **JsonSerializerContext generation** - Generate STJ source contexts for named serialization
2. **Static delegate dispatch** - Pre-generate typed delegate invokers
3. **DI-aware converters** - Converters that resolve constructor parameters from IServiceProvider

## Checking AOT Compatibility

To test AOT compatibility of your application:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <TrimMode>full</TrimMode>
</PropertyGroup>
```

Build with:
```bash
dotnet publish -c Release
```

AOT warnings will indicate reflection usage that may fail at runtime.

## Summary

RemoteFactory provides meaningful AOT improvements through generated ordinal converters, but **full Native AOT support is not currently possible** due to architectural constraints around interface polymorphism and dynamic dispatch. For most Blazor WebAssembly applications, this is not a concern since WebAssembly uses an IL interpreter rather than Native AOT.
