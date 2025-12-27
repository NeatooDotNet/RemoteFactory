---
layout: default
title: "Factory Operations"
description: "Create, Fetch, Insert, Update, Delete, and Execute operations in RemoteFactory"
parent: Concepts
nav_order: 2
---

# Factory Operations

RemoteFactory supports six operation types that map to common data access patterns. Each operation is marked with an attribute and generates corresponding factory methods.

## Operation Overview

| Attribute | Purpose | Typical Use |
|-----------|---------|-------------|
| `[Create]` | Construct a new instance | New objects, empty forms |
| `[Fetch]` | Load existing data | Read from database |
| `[Insert]` | Save a new record | First save of new object |
| `[Update]` | Modify existing record | Subsequent saves |
| `[Delete]` | Remove a record | Delete operation |
| `[Execute]` | Run a remote procedure | Static operations |

## Read Operations

### Create

The `[Create]` attribute marks constructors or methods that create new instances of your domain model.

**On a Constructor:**

```csharp
[Factory]
public class PersonModel : IPersonModel
{
    [Create]
    public PersonModel()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsNew = true;
    }

    [Create]
    public PersonModel(string firstName, string lastName)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        IsNew = true;
    }
}
```

**Generated Factory Methods:**

```csharp
public interface IPersonModelFactory
{
    IPersonModel? Create();
    IPersonModel? Create(string firstName, string lastName);
}
```

**On a Method:**

```csharp
[Factory]
public class PersonModel
{
    public PersonModel() { }

    [Create]
    public void Initialize(string template)
    {
        // Setup from template
    }

    [Create]
    public async Task InitializeAsync([Service] ITemplateService templates)
    {
        // Async initialization with services
    }
}
```

**Static Create Methods:**

```csharp
[Factory]
public class PersonModel
{
    private PersonModel() { }

    [Create]
    public static async Task<IPersonModel> CreateWithDefaults([Service] IDefaultsService defaults)
    {
        var model = new PersonModel();
        await model.ApplyDefaults(defaults);
        return model;
    }
}
```

### Fetch

The `[Fetch]` attribute marks methods that load existing data, typically from a database.

```csharp
[Factory]
public partial class PersonModel
{
    public int Id { get; private set; }
    public bool IsNew { get; set; } = true;

    [Create]
    public PersonModel() { }

    // Simple fetch by ID
    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(id);
        if (entity == null) return false;

        MapFrom(entity);
        IsNew = false;
        return true;
    }

    // Fetch with multiple parameters
    [Remote]
    [Fetch]
    public async Task<bool> FetchByEmail(string email, [Service] IPersonContext context)
    {
        var entity = await context.Persons
            .FirstOrDefaultAsync(p => p.Email == email);
        if (entity == null) return false;

        MapFrom(entity);
        IsNew = false;
        return true;
    }
}
```

**Generated Factory Methods:**

```csharp
public interface IPersonModelFactory
{
    IPersonModel? Create();
    Task<IPersonModel?> Fetch(int id);
    Task<IPersonModel?> FetchByEmail(string email);
}
```

**Return Types for Fetch:**

| Return Type | Behavior |
|-------------|----------|
| `void` | Always returns the model |
| `bool` | Returns model if true, null if false |
| `Task` | Async, always returns the model |
| `Task<bool>` | Async, returns model if true, null if false |

## Write Operations

Write operations require implementing `IFactorySaveMeta` to track object state:

```csharp
public interface IFactorySaveMeta
{
    bool IsNew { get; }
    bool IsDeleted { get; }
}
```

### Insert

The `[Insert]` attribute marks methods that create new records:

```csharp
[Factory]
public class PersonModel : IPersonModel, IFactorySaveMeta
{
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote]
    [Insert]
    public async Task Insert([Service] IPersonContext context)
    {
        var entity = new PersonEntity();
        MapTo(entity);
        context.Persons.Add(entity);
        await context.SaveChangesAsync();

        Id = entity.Id;
        IsNew = false;
    }
}
```

### Update

The `[Update]` attribute marks methods that modify existing records:

```csharp
[Factory]
public class PersonModel : IPersonModel, IFactorySaveMeta
{
    [Remote]
    [Update]
    public async Task Update([Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(Id)
            ?? throw new InvalidOperationException("Person not found");

        MapTo(entity);
        await context.SaveChangesAsync();
    }
}
```

### Combined Insert/Update (Upsert)

You can apply both attributes to a single method:

```csharp
[Remote]
[Insert]
[Update]
public async Task Save([Service] IPersonContext context)
{
    PersonEntity entity;

    if (IsNew)
    {
        entity = new PersonEntity();
        context.Persons.Add(entity);
    }
    else
    {
        entity = await context.Persons.FindAsync(Id)
            ?? throw new InvalidOperationException("Person not found");
    }

    MapTo(entity);
    await context.SaveChangesAsync();

    Id = entity.Id;
    IsNew = false;
}
```

### Delete

The `[Delete]` attribute marks methods that remove records:

```csharp
[Remote]
[Delete]
public async Task Delete([Service] IPersonContext context)
{
    var entity = await context.Persons.FindAsync(Id);
    if (entity != null)
    {
        context.Persons.Remove(entity);
        await context.SaveChangesAsync();
    }
}
```

## The Save Method

RemoteFactory generates a `Save` method that automatically routes to Insert, Update, or Delete based on the object's state:

```csharp
// Generated Save logic
public async Task<Authorized<IPersonModel>> LocalSave(IPersonModel target)
{
    if (target.IsDeleted)
    {
        if (target.IsNew)
        {
            // New and deleted = nothing to do
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

**Using Save:**

```csharp
// Create and save new
var person = factory.Create();
person.FirstName = "John";
await factory.Save(person);  // Calls Insert

// Modify and save existing
person.FirstName = "Jane";
await factory.Save(person);  // Calls Update

// Delete
person.IsDeleted = true;
await factory.Save(person);  // Calls Delete
```

### Save vs TrySave

- `Save` throws `NotAuthorizedException` if authorization fails
- `TrySave` returns an `Authorized<T>` result you can check

```csharp
// Save throws on authorization failure
try
{
    var result = await factory.Save(person);
}
catch (NotAuthorizedException ex)
{
    // Handle authorization failure
}

// TrySave returns result
var result = await factory.TrySave(person);
if (result.HasAccess)
{
    var savedPerson = result.Result;
}
else
{
    var message = result.Message;
}
```

## Execute Operations

The `[Execute]` attribute is used for static methods that perform operations without a domain model instance:

```csharp
[Factory]
public static partial class PersonOperations
{
    [Execute]
    public static async Task<int> GetPersonCount([Service] IPersonContext context)
    {
        return await context.Persons.CountAsync();
    }

    [Execute]
    public static async Task<List<string>> GetAllEmails([Service] IPersonContext context)
    {
        return await context.Persons
            .Select(p => p.Email)
            .Where(e => e != null)
            .ToListAsync()!;
    }
}
```

**Generated Factory:**

```csharp
public interface IPersonOperationsFactory
{
    Task<int> GetPersonCount();
    Task<List<string>> GetAllEmails();
}
```

## The Remote Attribute

The `[Remote]` attribute indicates a method should execute on the server in Remote mode:

```csharp
[Factory]
public class PersonModel
{
    // Executes locally (no [Remote])
    [Create]
    public PersonModel() { }

    // Executes on server
    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        // Server-side code with database access
    }
}
```

**When to use `[Remote]`:**

| Scenario | Use Remote? |
|----------|-------------|
| Database access | Yes |
| Server-only services | Yes |
| Simple object construction | No |
| Local validation | No |
| External API calls (from server) | Yes |

## Method Signatures

### Supported Return Types

| Return Type | Create | Fetch | Insert/Update/Delete |
|-------------|--------|-------|---------------------|
| `void` | Model returned | Model returned | Model returned |
| `bool` | Model if true | Model if true | Model if true |
| `Task` | Model returned | Model returned | Model returned |
| `Task<bool>` | Model if true | Model if true | Model if true |
| `T` (static Create) | Returns T | N/A | N/A |
| `Task<T>` (static Create) | Returns T | N/A | N/A |

### Service Parameters

Parameters marked with `[Service]` are excluded from the factory method signature:

```csharp
// Your method
[Fetch]
public async Task<bool> Fetch(int id, [Service] IPersonContext context)

// Generated factory method - context is not a parameter
Task<IPersonModel?> Fetch(int id);
```

## Operation Matching for Save

When multiple Insert, Update, or Delete methods exist, RemoteFactory matches them by non-service parameters:

```csharp
[Factory]
public class OrderModel : IFactorySaveMeta
{
    // Default save operations
    [Insert]
    public void Insert([Service] IOrderContext context) { }

    [Update]
    public void Update([Service] IOrderContext context) { }

    [Delete]
    public void Delete([Service] IOrderContext context) { }

    // Operations with extra parameter
    [Insert]
    public void InsertWithAudit(string auditReason, [Service] IOrderContext context) { }

    [Update]
    public void UpdateWithAudit(string auditReason, [Service] IOrderContext context) { }

    [Delete]
    public void DeleteWithAudit(string auditReason, [Service] IOrderContext context) { }
}
```

**Generated Save Methods:**

```csharp
public interface IOrderModelFactory
{
    Task<IOrderModel?> Save(IOrderModel target);
    Task<IOrderModel?> SaveWithAudit(IOrderModel target, string auditReason);
}
```

## Next Steps

- **[Three-Tier Execution](three-tier-execution.md)**: Server, Remote, and Logical modes
- **[Service Injection](service-injection.md)**: Using `[Service]` for DI
- **[Attributes Reference](../reference/attributes.md)**: Complete attribute documentation
- **[Authorization Overview](../authorization/authorization-overview.md)**: Adding access control
