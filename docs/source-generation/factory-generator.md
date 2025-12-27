---
layout: default
title: "Factory Generator"
description: "Understanding how RemoteFactory generates factory classes"
parent: Source Generation
nav_order: 2
---

# Factory Generator

The Factory Generator is a Roslyn Source Generator that analyzes classes marked with `[Factory]` and generates complete factory infrastructure at compile time. This document explains what gets generated and why.

## Input: Your Domain Class

The generator looks for classes with the `[Factory]` attribute:

```csharp
[Factory]
[AuthorizeFactory<IPersonModelAuth>]
public partial class PersonModel : IPersonModel
{
    [Create]
    public PersonModel()
    {
        IsNew = true;
    }

    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }
    public string? FirstName { get; set; }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        // Load from database
    }

    [Remote]
    [Insert]
    [Update]
    public async Task Save([Service] IPersonContext context)
    {
        // Save to database
    }

    [Remote]
    [Delete]
    public async Task Delete([Service] IPersonContext context)
    {
        // Delete from database
    }
}
```

## Output: Generated Components

### 1. Factory Interface

A public interface with all factory methods:

```csharp
public interface IPersonModelFactory
{
    // Create method from [Create] constructor
    IPersonModel? Create();

    // Fetch method from [Fetch] method
    Task<IPersonModel?> Fetch(int id);

    // Save/TrySave from [Insert], [Update], [Delete] methods
    Task<IPersonModel?> Save(IPersonModel target);
    Task<Authorized<IPersonModel>> TrySave(IPersonModel target);

    // Authorization check methods from [AuthorizeFactory<T>]
    Authorized CanCreate();
    Authorized CanFetch();
    Authorized CanSave();
    Authorized CanDelete();
}
```

### 2. Delegate Types

Delegates for each remote operation:

```csharp
// Inside PersonModelFactory class
public delegate Task<Authorized<IPersonModel>> FetchDelegate(int id);
public delegate Task<Authorized<IPersonModel>> SaveDelegate(IPersonModel target);
```

These delegates enable the remote invocation system:
- Registered in DI on the server
- Resolved by type name for remote calls
- Contain the actual execution logic

### 3. Factory Implementation

A class implementing the interface:

```csharp
internal class PersonModelFactory : FactorySaveBase<IPersonModel>,
    IFactorySave<PersonModel>,
    IPersonModelFactory
{
    private readonly IServiceProvider ServiceProvider;
    private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;

    // Delegate properties
    public FetchDelegate FetchProperty { get; }
    public SaveDelegate SaveProperty { get; }

    // Constructor for Server/Logical mode
    public PersonModelFactory(
        IServiceProvider serviceProvider,
        IFactoryCore<IPersonModel> factoryCore) : base(factoryCore)
    {
        ServiceProvider = serviceProvider;
        FetchProperty = LocalFetch;
        SaveProperty = LocalSave;
    }

    // Constructor for Remote mode
    public PersonModelFactory(
        IServiceProvider serviceProvider,
        IMakeRemoteDelegateRequest remoteMethodDelegate,
        IFactoryCore<IPersonModel> factoryCore) : base(factoryCore)
    {
        ServiceProvider = serviceProvider;
        MakeRemoteDelegateRequest = remoteMethodDelegate;
        FetchProperty = RemoteFetch;
        SaveProperty = RemoteSave;
    }

    // ... method implementations
}
```

### 4. Public Factory Methods

Methods exposed through the interface:

```csharp
public virtual IPersonModel? Create()
{
    return (LocalCreate()).Result;
}

public virtual async Task<IPersonModel?> Fetch(int id)
{
    return (await FetchProperty(id)).Result;
}

public virtual async Task<IPersonModel?> Save(IPersonModel target)
{
    var authorized = await SaveProperty(target);
    if (!authorized.HasAccess)
    {
        throw new NotAuthorizedException(authorized);
    }
    return authorized.Result;
}

public virtual async Task<Authorized<IPersonModel>> TrySave(IPersonModel target)
{
    return await SaveProperty(target);
}
```

### 5. Local Methods

Methods that execute on the server:

```csharp
public Authorized<IPersonModel> LocalCreate()
{
    // Authorization checks
    IPersonModelAuth auth = ServiceProvider.GetRequiredService<IPersonModelAuth>();

    var canAccess = auth.CanAccess();
    if (!canAccess.HasAccess)
        return new Authorized<IPersonModel>(canAccess);

    var canCreate = auth.CanCreate();
    if (!canCreate.HasAccess)
        return new Authorized<IPersonModel>(canCreate);

    // Execute operation
    return new Authorized<IPersonModel>(
        DoFactoryMethodCall(FactoryOperation.Create, () => new PersonModel())
    );
}

public async Task<Authorized<IPersonModel>> LocalFetch(int id)
{
    // Authorization checks
    IPersonModelAuth auth = ServiceProvider.GetRequiredService<IPersonModelAuth>();

    var canAccess = auth.CanAccess();
    if (!canAccess.HasAccess)
        return new Authorized<IPersonModel>(canAccess);

    var canFetch = auth.CanFetch();
    if (!canFetch.HasAccess)
        return new Authorized<IPersonModel>(canFetch);

    // Resolve services and execute
    var target = ServiceProvider.GetRequiredService<PersonModel>();
    var context = ServiceProvider.GetRequiredService<IPersonContext>();

    return new Authorized<IPersonModel>(
        await DoFactoryMethodCallBoolAsync(target, FactoryOperation.Fetch,
            () => target.Fetch(id, context))
    );
}
```

### 6. Remote Methods

Methods that call the server from clients:

```csharp
public virtual async Task<Authorized<IPersonModel>> RemoteFetch(int id)
{
    return (await MakeRemoteDelegateRequest!.ForDelegate<Authorized<IPersonModel>>(
        typeof(FetchDelegate),
        [id]  // Parameters to serialize
    ))!;
}

public virtual async Task<Authorized<IPersonModel>> RemoteSave(IPersonModel target)
{
    return (await MakeRemoteDelegateRequest!.ForDelegate<Authorized<IPersonModel>>(
        typeof(SaveDelegate),
        [target]  // Serialize the model
    ))!;
}
```

### 7. Save Logic

The LocalSave method routes to Insert, Update, or Delete:

```csharp
public virtual async Task<Authorized<IPersonModel>> LocalSave(IPersonModel target)
{
    if (target.IsDeleted)
    {
        if (target.IsNew)
        {
            // New and deleted = nothing to persist
            return new Authorized<IPersonModel>();
        }
        return await LocalDelete(target);
    }
    else if (target.IsNew)
    {
        return await LocalInsert(target);
    }
    else
    {
        return await LocalUpdate(target);
    }
}
```

### 8. Can* Methods

Authorization check methods:

```csharp
public virtual Authorized CanCreate()
{
    return LocalCanCreate();
}

public Authorized LocalCanCreate()
{
    IPersonModelAuth auth = ServiceProvider.GetRequiredService<IPersonModelAuth>();

    var canAccess = auth.CanAccess();
    if (!canAccess.HasAccess) return canAccess;

    var canCreate = auth.CanCreate();
    if (!canCreate.HasAccess) return canCreate;

    return new Authorized(true);
}

public virtual Authorized CanSave()
{
    return LocalCanSave();
}

public Authorized LocalCanSave()
{
    IPersonModelAuth auth = ServiceProvider.GetRequiredService<IPersonModelAuth>();

    var canAccess = auth.CanAccess();
    if (!canAccess.HasAccess) return canAccess;

    // Save must check Update, Insert, and Delete
    var canUpdate = auth.CanUpdate();
    if (!canUpdate.HasAccess) return canUpdate;

    var canDelete = auth.CanDelete();
    if (!canDelete.HasAccess) return canDelete;

    return new Authorized(true);
}
```

### 9. DI Registration

Static method for dependency injection:

```csharp
public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
{
    // Factory registrations
    services.AddScoped<PersonModelFactory>();
    services.AddScoped<IPersonModelFactory, PersonModelFactory>();

    // Delegate registrations (for server-side resolution)
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

    // Domain model registrations
    services.AddTransient<PersonModel>();
    services.AddTransient<IPersonModel, PersonModel>();

    // Save interface registration
    services.AddScoped<IFactorySave<PersonModel>, PersonModelFactory>();
}
```

## Method Generation Rules

### Return Type Handling

| Your Method Returns | Factory Method Returns |
|--------------------|----------------------|
| `void` | Model (always) |
| `bool` | Model or null |
| `Task` | Model (always) |
| `Task<bool>` | Model or null |
| `T` (static) | T |
| `Task<T>` (static) | T |

### Parameter Handling

```csharp
// Your method
[Fetch]
public Task<bool> Fetch(int id, string? name, [Service] IContext context)

// Generated factory method (services removed)
Task<IPersonModel?> Fetch(int id, string? name);

// Generated local method (services resolved)
public async Task<Authorized<IPersonModel>> LocalFetch(int id, string? name)
{
    var context = ServiceProvider.GetRequiredService<IContext>();
    return await target.Fetch(id, name, context);
}
```

### Method Naming

Factory method names are derived from your method names:

| Your Method | Factory Method |
|-------------|---------------|
| Constructor with `[Create]` | `Create()` |
| `Fetch(int id)` | `Fetch(int id)` |
| `FetchByEmail(string email)` | `FetchByEmail(string email)` |
| `[Insert][Update] Save()` | Part of `Save()` |
| `[Execute] GetCount()` | `GetCount()` |

## Static Class Factories

For static classes with `[Execute]` methods:

```csharp
[Factory]
public static partial class PersonOperations
{
    [Execute]
    public static async Task<int> GetCount([Service] IPersonContext context)
    {
        return await context.Persons.CountAsync();
    }
}
```

**Generated:**

```csharp
public interface IPersonOperationsFactory
{
    Task<int> GetCount();
}

internal class PersonOperationsFactory : IPersonOperationsFactory
{
    public delegate Task<int> GetCountDelegate();

    public async Task<int> GetCount()
    {
        return await GetCountProperty();
    }

    // Local and Remote implementations...
}
```

## Interface Factories

When `[Factory]` is on an interface:

```csharp
[Factory]
public interface IExecuteMethods
{
    [Execute]
    Task<int> DoSomething(string input);
}

public class ExecuteMethods : IExecuteMethods
{
    public async Task<int> DoSomething(string input)
    {
        // Implementation
    }
}
```

Only delegates are generated; you provide the implementation.

## Viewing Generated Code

### In Visual Studio

1. Solution Explorer > Dependencies > Analyzers
2. Expand `Neatoo.RemoteFactory.FactoryGenerator`
3. View `.g.cs` files

### Enable File Output

Add to your `.csproj`:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

Files appear in: `obj/Debug/net8.0/generated/`

## Next Steps

- **[Mapper Generator](mapper-generator.md)**: MapTo/MapFrom generation
- **[Appendix: Internals](appendix-internals.md)**: Technical deep dive
- **[Generated Code Reference](../reference/generated-code.md)**: Understanding factory structure
