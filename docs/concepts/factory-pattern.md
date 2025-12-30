---
layout: default
title: "Factory Pattern"
description: "How factories work in RemoteFactory - generated interfaces and implementations"
parent: Concepts
nav_order: 2
---

# Factory Pattern

RemoteFactory generates factory classes that encapsulate object creation, retrieval, and persistence. This document explains how factories work, what gets generated, and how to use them effectively.

## What is a Factory in RemoteFactory?

A factory in RemoteFactory is a generated class that:

1. **Creates domain model instances** - via `Create()` methods
2. **Retrieves data from persistence** - via `Fetch()` methods
3. **Saves changes** - via `Save()` methods that route to Insert, Update, or Delete
4. **Checks authorization** - via `Can*()` methods

Unlike manually written factories, RemoteFactory factories are generated at compile time from your domain model classes, eliminating boilerplate while ensuring type safety.

## Generated Components

When you apply `[Factory]` to a class, the source generator creates several components:

### Factory Interface

```csharp
public interface IPersonModelFactory
{
    // Create methods - synchronous, return nullable
    IPersonModel? Create();
    IPersonModel? Create(string firstName, string lastName);

    // Fetch methods - async, return nullable
    Task<IPersonModel?> Fetch(int id);
    Task<IPersonModel?> Fetch();

    // Save methods
    Task<IPersonModel?> Save(IPersonModel target);
    Task<Authorized<IPersonModel>> TrySave(IPersonModel target);

    // Authorization check methods
    Authorized CanCreate();
    Authorized CanFetch();
    Authorized CanUpdate();
    Authorized CanDelete();
    Authorized CanSave();
}
```

### Factory Implementation

```csharp
internal class PersonModelFactory : FactorySaveBase<IPersonModel>, IPersonModelFactory
{
    private readonly IServiceProvider ServiceProvider;
    private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;

    // Delegate types for remote execution
    public delegate Task<Authorized<IPersonModel>> FetchDelegate(int id);
    public delegate Task<Authorized<IPersonModel>> SaveDelegate(IPersonModel target);

    // Delegate properties for Local or Remote execution
    public FetchDelegate FetchProperty { get; }
    public SaveDelegate SaveProperty { get; }

    // Constructor for Server mode (local execution)
    public PersonModelFactory(
        IServiceProvider serviceProvider,
        IFactoryCore<IPersonModel> factoryCore) : base(factoryCore)
    {
        this.ServiceProvider = serviceProvider;
        FetchProperty = LocalFetch;
        SaveProperty = LocalSave;
    }

    // Constructor for Remote mode (HTTP execution)
    public PersonModelFactory(
        IServiceProvider serviceProvider,
        IMakeRemoteDelegateRequest remoteMethodDelegate,
        IFactoryCore<IPersonModel> factoryCore) : base(factoryCore)
    {
        this.ServiceProvider = serviceProvider;
        this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        FetchProperty = RemoteFetch;
        SaveProperty = RemoteSave;
    }

    // ... implementation methods
}
```

### DI Registrations

```csharp
public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory mode)
{
    // Factory registrations
    services.AddScoped<PersonModelFactory>();
    services.AddScoped<IPersonModelFactory, PersonModelFactory>();

    // Delegate registrations (for handling remote calls on server)
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
}
```

## Factory Method Anatomy

### Create Methods

Create methods are synchronous and invoke your `[Create]` constructors or methods:

```csharp
// Your domain model
[Factory]
public class PersonModel : IPersonModel
{
    [Create]
    public PersonModel()
    {
        FirstName = "";
        LastName = "";
    }

    [Create]
    public PersonModel(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
}

// Generated factory methods
public IPersonModel? Create()
{
    return LocalCreate().Result;
}

public IPersonModel? Create(string firstName, string lastName)
{
    return LocalCreate(firstName, lastName).Result;
}
```

Create methods:
- Are synchronous (no `Task<>` return)
- Return nullable (`IPersonModel?`) to handle authorization failures
- Check authorization before executing
- Execute locally (never remote)

### Fetch Methods

Fetch methods are async and invoke your `[Fetch]` methods:

```csharp
// Your domain model
[Factory]
public class PersonModel : IPersonModel
{
    public int Id { get; private set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext ctx)
    {
        var entity = await ctx.Persons.FindAsync(id);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        return true;
    }
}

// Generated factory method
public async Task<IPersonModel?> Fetch(int id)
{
    return (await FetchProperty(id)).Result;
}
```

Fetch methods:
- Are async (`Task<IPersonModel?>`)
- Return nullable to handle not-found or authorization failures
- Route through `FetchProperty` delegate for local/remote forking
- Handle `[Service]` parameters automatically

### Save Method

The Save method routes to Insert, Update, or Delete based on object state:

```csharp
public async Task<Authorized<IPersonModel>> LocalSave(IPersonModel target)
{
    if (target.IsDeleted)
    {
        if (target.IsNew)
        {
            // New + Deleted = nothing to do
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

Save methods:
- Use `IFactorySaveMeta.IsNew` and `IsDeleted` to determine operation
- Combine Insert and Update when you use `[Insert][Update]` on the same method
- Throw `NotAuthorizedException` on failure (use `TrySave` for non-throwing)

### TrySave Method

`TrySave` returns authorization status without throwing:

```csharp
public async Task<Authorized<IPersonModel>> TrySave(IPersonModel target)
{
    return await SaveProperty(target);
}

// Usage
var result = await _factory.TrySave(person);
if (!result.HasAccess)
{
    _errorMessage = result.Message;
    return;
}
var savedPerson = result.Result;
```

### Can* Methods

Authorization check methods let you verify permissions without executing operations:

```csharp
public Authorized CanCreate()
{
    return LocalCanCreate();
}

public Authorized LocalCanCreate()
{
    Authorized authorized;
    IPersonModelAuth auth = ServiceProvider.GetRequiredService<IPersonModelAuth>();

    authorized = auth.CanAccess();
    if (!authorized.HasAccess) return authorized;

    authorized = auth.CanCreate();
    if (!authorized.HasAccess) return authorized;

    return new Authorized(true);
}
```

Use Can* methods for:
- Conditional UI rendering (show/hide buttons)
- Pre-flight authorization checks
- Permission-based navigation guards

## Local vs Remote Execution

The factory constructor determines execution mode:

### Server Mode (Local Execution)

```csharp
// Constructor selected when IMakeRemoteDelegateRequest is NOT registered
public PersonModelFactory(
    IServiceProvider serviceProvider,
    IFactoryCore<IPersonModel> factoryCore)
{
    FetchProperty = LocalFetch;  // Direct local execution
    SaveProperty = LocalSave;
}
```

### Remote Mode (HTTP Execution)

```csharp
// Constructor selected when IMakeRemoteDelegateRequest IS registered
public PersonModelFactory(
    IServiceProvider serviceProvider,
    IMakeRemoteDelegateRequest remoteMethodDelegate,
    IFactoryCore<IPersonModel> factoryCore)
{
    FetchProperty = RemoteFetch;  // HTTP call to server
    SaveProperty = RemoteSave;
}
```

Remote methods serialize parameters and make HTTP calls:

```csharp
public async Task<Authorized<IPersonModel>> RemoteFetch(int id)
{
    return (await MakeRemoteDelegateRequest!
        .ForDelegate<Authorized<IPersonModel>>(typeof(FetchDelegate), [id]))!;
}
```

## Factory Inheritance

Generated factories inherit from `FactorySaveBase<T>`:

```csharp
public abstract class FactorySaveBase<T> where T : IFactorySaveMeta
{
    protected readonly IFactoryCore<T> FactoryCore;

    protected FactorySaveBase(IFactoryCore<T> factoryCore)
    {
        FactoryCore = factoryCore;
    }

    // Helper methods for lifecycle hooks
    protected T DoFactoryMethodCall(FactoryOperation operation, Func<T> call)
        => FactoryCore.DoFactoryMethodCall(operation, call);

    protected Task<T?> DoFactoryMethodCallBoolAsync(
        T target, FactoryOperation operation, Func<Task<bool>> call)
        => FactoryCore.DoFactoryMethodCallBoolAsync(target, operation, call);
}
```

This base class:
- Provides `IFactoryCore<T>` integration for lifecycle hooks
- Enables custom `IFactoryCore<T>` implementations
- Wraps factory method calls for pre/post processing

## Using Factories

### Inject the Factory Interface

```csharp
public class PersonService
{
    private readonly IPersonModelFactory _factory;

    public PersonService(IPersonModelFactory factory)
    {
        _factory = factory;
    }

    public async Task<IPersonModel?> GetPerson(int id)
    {
        return await _factory.Fetch(id);
    }

    public IPersonModel CreatePerson()
    {
        return _factory.Create()
            ?? throw new UnauthorizedException("Cannot create person");
    }
}
```

### Blazor Component Example

```razor
@inject IPersonModelFactory PersonFactory

@if (_canCreate)
{
    <button @onclick="CreateNew">New Person</button>
}

<button @onclick="Save" disabled="@(!_canSave)">Save</button>

@code {
    private IPersonModel? _person;
    private bool _canCreate;
    private bool _canSave;

    protected override void OnInitialized()
    {
        _canCreate = PersonFactory.CanCreate().HasAccess;
        _canSave = PersonFactory.CanSave().HasAccess;
    }

    private void CreateNew()
    {
        _person = PersonFactory.Create();
    }

    private async Task Save()
    {
        if (_person != null)
        {
            var result = await PersonFactory.TrySave(_person);
            if (result.HasAccess)
            {
                _person = result.Result;
            }
        }
    }
}
```

## Best Practices

### Use Interfaces

Always inject the factory interface, not the implementation:

```csharp
// Good
public class MyService(IPersonModelFactory factory) { }

// Avoid
public class MyService(PersonModelFactory factory) { }
```

### Handle Nullable Returns

Factory methods return nullable types to indicate failure:

```csharp
var person = await _factory.Fetch(id);
if (person == null)
{
    // Not found or not authorized
    return NotFound();
}
```

### Use TrySave for Non-Throwing Authorization

```csharp
var result = await _factory.TrySave(person);
if (!result.HasAccess)
{
    _message = $"Cannot save: {result.Message}";
    return;
}
// Success - result.Result contains the saved object
```

### Check Authorization Before UI Actions

```csharp
@if (_factory.CanDelete().HasAccess)
{
    <button @onclick="Delete">Delete</button>
}
```

## Next Steps

- **[Factory Operations](factory-operations.md)**: Deep dive into Create, Fetch, Insert, Update, Delete, Execute
- **[Three-Tier Execution](three-tier-execution.md)**: Local vs Remote execution modes
- **[Generated Code](../reference/generated-code.md)**: Understanding generated factory structure
