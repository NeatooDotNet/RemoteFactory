---
layout: default
title: "How It Works"
description: "High-level understanding of RemoteFactory source generation"
parent: Source Generation
nav_order: 1
---

# How Source Generation Works

RemoteFactory uses Roslyn Source Generators to analyze your code at compile time and generate factory infrastructure. This document explains what gets generated, where to find it, and how to troubleshoot issues.

## What Gets Generated?

For each class marked with `[Factory]`, RemoteFactory generates:

### 1. Factory Interface

A public interface with methods for each operation:

```csharp
// Your class
[Factory]
public class PersonModel { ... }

// Generated interface
public interface IPersonModelFactory
{
    IPersonModel? Create();
    Task<IPersonModel?> Fetch(int id);
    Task<IPersonModel?> Save(IPersonModel target);
    Task<Authorized<IPersonModel>> TrySave(IPersonModel target);
    Authorized CanCreate();
    Authorized CanFetch();
    Authorized CanSave();
}
```

### 2. Factory Implementation

An internal class implementing the interface with both local and remote execution paths:

```csharp
internal class PersonModelFactory : FactorySaveBase<IPersonModel>, IPersonModelFactory
{
    // Delegates for remote invocation
    public delegate Task<Authorized<IPersonModel>> FetchDelegate(int id);
    public delegate Task<Authorized<IPersonModel>> SaveDelegate(IPersonModel target);

    // Delegate properties switch local/remote
    public FetchDelegate FetchProperty { get; }
    public SaveDelegate SaveProperty { get; }

    // Constructor for local (Server) mode
    public PersonModelFactory(IServiceProvider serviceProvider, IFactoryCore<IPersonModel> factoryCore)
    {
        FetchProperty = LocalFetch;
        SaveProperty = LocalSave;
    }

    // Constructor for remote (Client) mode
    public PersonModelFactory(IServiceProvider serviceProvider,
        IMakeRemoteDelegateRequest remoteMethodDelegate,
        IFactoryCore<IPersonModel> factoryCore)
    {
        FetchProperty = RemoteFetch;
        SaveProperty = RemoteSave;
    }

    // Local methods execute the actual logic
    public async Task<Authorized<IPersonModel>> LocalFetch(int id) { ... }

    // Remote methods serialize and call the server
    public async Task<Authorized<IPersonModel>> RemoteFetch(int id) { ... }
}
```

### 3. DI Registration Method

A static method that registers all components with dependency injection:

```csharp
public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
{
    // Register factory
    services.AddScoped<PersonModelFactory>();
    services.AddScoped<IPersonModelFactory, PersonModelFactory>();

    // Register delegates for server-side resolution
    services.AddScoped<FetchDelegate>(cc =>
    {
        var factory = cc.GetRequiredService<PersonModelFactory>();
        return (int id) => factory.LocalFetch(id);
    });

    services.AddScoped<SaveDelegate>(cc =>
    {
        var factory = cc.GetRequiredService<PersonModelFactory>();
        return (IPersonModel target) => factory.LocalSave(target);
    });

    // Register the domain model itself
    services.AddTransient<PersonModel>();
    services.AddTransient<IPersonModel, PersonModel>();
}
```

### 4. Mapper Methods (Optional)

If your class has partial `MapTo` or `MapFrom` methods, they're generated:

```csharp
// Your class
public partial class PersonModel
{
    public partial void MapFrom(PersonEntity entity);
    public partial void MapTo(PersonEntity entity);
}

// Generated mapper
public partial void MapFrom(PersonEntity entity)
{
    this.FirstName = entity.FirstName;
    this.LastName = entity.LastName;
    this.Email = entity.Email;
}

public partial void MapTo(PersonEntity entity)
{
    entity.FirstName = this.FirstName;
    entity.LastName = this.LastName;
    entity.Email = this.Email;
}
```

## Where to Find Generated Code

### Visual Studio

1. Open **Solution Explorer**
2. Expand your project
3. Expand **Dependencies** > **Analyzers**
4. Expand **Neatoo.RemoteFactory.FactoryGenerator**
5. View generated `.g.cs` files

### File System

Generated code is written to:

```
YourProject/
└── obj/
    └── Debug/
        └── net8.0/
            └── generated/
                └── Neatoo.RemoteFactory.FactoryGenerator/
                    ├── Neatoo.RemoteFactory.FactoryGenerator.FactoryGenerator/
                    │   └── YourNamespace.PersonModelFactory.g.cs
                    └── Neatoo.RemoteFactory.FactoryGenerator.MapperGenerator/
                        └── YourNamespace.PersonModelMapper.g.cs
```

### JetBrains Rider

1. Open the project in Solution Explorer
2. Navigate to **Dependencies** > **Source Generators**
3. Expand the RemoteFactory generator to see generated files

## Triggering Regeneration

Source generators run automatically during build. To force regeneration:

1. **Clean and Rebuild**: Right-click solution > Clean > Rebuild
2. **Modify source file**: Any change to a `[Factory]` class triggers regeneration
3. **Restart IDE**: Sometimes needed after package updates

## Generation Rules

### Class Requirements

For the `[Factory]` attribute to work, your class must be:

- **Concrete**: Not abstract
- **Non-generic**: Generic classes aren't supported
- **Partial** (optional): Required only for mapper generation

```csharp
// Valid
[Factory]
public class PersonModel { }

[Factory]
public partial class PersonModel { }

[Factory]
internal class PersonModel { }

// Invalid
[Factory]
public abstract class PersonModel { }  // Abstract

[Factory]
public class PersonModel<T> { }  // Generic
```

### Method Detection

The generator looks for methods with these attributes:

| Attribute | Creates Factory Method |
|-----------|----------------------|
| `[Create]` | `Create(...)` or constructor params |
| `[Fetch]` | `Fetch(...)` |
| `[Insert]` | Part of `Save(...)` |
| `[Update]` | Part of `Save(...)` |
| `[Delete]` | Part of `Save(...)` |
| `[Execute]` | Method name as factory method |

### Parameter Handling

Parameters are analyzed to determine factory method signatures:

```csharp
// Method with service and regular parameters
[Fetch]
public Task<bool> Fetch(int id, string name, [Service] IContext context)

// Generated factory method excludes [Service] parameters
Task<IPersonModel?> Fetch(int id, string name);
```

## Suppressing Generation

Use `[SuppressFactory]` to prevent factory generation for a class:

```csharp
[Factory]
[SuppressFactory]  // No factory will be generated
public class InternalHelper { }
```

This is useful for base classes that shouldn't have their own factories.

## Common Issues

### No Factory Generated

**Symptoms:** `IPersonModelFactory` not found, IntelliSense not working

**Causes and solutions:**

1. **Missing `[Factory]` attribute**
   ```csharp
   [Factory]  // Add this
   public class PersonModel { }
   ```

2. **Class is abstract or generic**
   ```csharp
   // Change from
   public abstract class PersonModel { }
   // To
   public class PersonModel { }
   ```

3. **Build errors preventing generation**
   - Fix other compilation errors first
   - Source generators don't run if build fails

4. **Package not installed correctly**
   - Check NuGet package reference
   - Try removing and re-adding the package

### Mapper Not Generated

**Symptoms:** `MapTo` or `MapFrom` shows "not implemented"

**Causes and solutions:**

1. **Class not partial**
   ```csharp
   [Factory]
   public partial class PersonModel  // Add 'partial'
   {
       public partial void MapFrom(PersonEntity entity);
   }
   ```

2. **Method signature mismatch**
   - Parameter type must be accessible
   - Return type must be `void`

### Stale Generated Code

**Symptoms:** Generated code doesn't match source

**Solutions:**

1. Clean and rebuild the solution
2. Close and reopen the IDE
3. Delete `obj` and `bin` folders manually

### Authorization Methods Not Generated

**Symptoms:** `CanCreate()`, `CanFetch()` not appearing

**Causes and solutions:**

1. **Missing authorization interface**
   ```csharp
   [Factory]
   [AuthorizeFactory<IPersonModelAuth>]  // Add this
   public class PersonModel { }
   ```

2. **Authorization interface not implemented**
   - Ensure `IPersonModelAuth` exists and has proper attributes

## Debugging Tips

### View Generation Diagnostics

Add to your `.csproj` to emit generator diagnostics:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

### Check Generator Output

The generator adds a header comment with diagnostic info:

```csharp
/*
    READONLY - DO NOT EDIT!!!!
    Generated by Neatoo.RemoteFactory
    Predicate Count: 1
    Transform Count: 1
    Generate Count: 1
*/
```

These counts help diagnose caching issues:
- **Predicate Count**: How many times syntax was analyzed
- **Transform Count**: How many times semantic analysis ran
- **Generate Count**: How many times code was emitted

## Performance Considerations

RemoteFactory uses incremental generation for performance:

- **Caching**: Unchanged classes don't trigger regeneration
- **Parallelization**: Multiple classes generate in parallel
- **Minimal analysis**: Only necessary symbols are analyzed

Large projects should see minimal build time impact from generation.

## Next Steps

- **[Factory Generator](factory-generator.md)**: Deep dive into factory generation
- **[Mapper Generator](mapper-generator.md)**: Understanding MapTo/MapFrom generation
- **[Appendix: Internals](appendix-internals.md)**: Technical deep dive for curious engineers
