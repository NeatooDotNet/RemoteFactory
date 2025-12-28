---
layout: default
title: "Attributes Reference"
description: "Complete reference for all RemoteFactory attributes"
parent: Reference
nav_order: 1
---

# Attributes Reference

This document provides a complete reference for all attributes in the RemoteFactory framework.

## Class-Level Attributes

### [Factory]

Marks a class for factory generation. The source generator will create an interface and implementation for this class.

**Target:** Class, Interface

**Usage:**

```csharp
[Factory]
public class PersonModel
{
    [Create]
    public PersonModel() { }
}
```

**Requirements:**
- Class must not be abstract
- Class must not be generic
- Class should have at least one operation method

**Generated Output:**
- `I{ClassName}Factory` interface
- `{ClassName}Factory` implementation
- DI registration method

---

### [SuppressFactory]

Prevents factory generation for a class that would otherwise have a factory generated. Useful for base classes.

**Target:** Class, Interface

**Usage:**

```csharp
[Factory]
[SuppressFactory]  // No factory will be generated
public abstract class BaseModel
{
    // Shared functionality
}

[Factory]
public class PersonModel : BaseModel
{
    // Factory generated for this class only
}
```

---

### [AuthorizeFactory<T>]

Links an authorization class to a factory. The authorization class must implement methods decorated with `[AuthorizeFactory]`.

**Target:** Class, Interface

**Type Parameter:** The authorization interface type

**Usage:**

```csharp
public interface IPersonModelAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool CanAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();
}

[Factory]
[AuthorizeFactory<IPersonModelAuth>]
public class PersonModel
{
    // Operations will check authorization
}
```

**Generated Output:**
- `CanCreate()`, `CanFetch()`, `CanSave()`, etc. methods on factory
- Authorization checks before each operation

---

## Method-Level Attributes

### [Create]

Marks a constructor or method as a Create operation.

**Target:** Constructor, Method

**Usage on Constructor:**

```csharp
[Factory]
public class PersonModel
{
    [Create]
    public PersonModel()
    {
        Id = Guid.NewGuid();
    }

    [Create]
    public PersonModel(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
}
```

**Usage on Method:**

```csharp
[Create]
public void Initialize(string template)
{
    // Initialize from template
}

[Create]
public static async Task<IPersonModel> CreateWithDefaults([Service] IDefaultsService defaults)
{
    var model = new PersonModel();
    await model.ApplyDefaults(defaults);
    return model;
}
```

**Supported Return Types:**
- `void` - Always returns the model
- `bool` - Returns model if true, null if false
- `Task` - Async, always returns the model
- `Task<bool>` - Async, returns model if true, null if false
- `T` (static) - Returns the specified type
- `Task<T>` (static) - Async, returns the specified type

---

### [Fetch]

Marks a method as a Fetch operation for loading existing data.

**Target:** Method

**Usage:**

```csharp
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
```

**Supported Return Types:**
- `void` - Always returns the model
- `bool` - Returns model if true, null if false
- `Task` - Async, always returns the model
- `Task<bool>` - Async, returns model if true, null if false

---

### [Insert]

Marks a method as an Insert operation for saving new records.

**Target:** Method

**Usage:**

```csharp
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
```

**Notes:**
- Called by `Save()` when `IsNew == true` and `IsDeleted == false`
- Can be combined with `[Update]` for upsert pattern

---

### [Update]

Marks a method as an Update operation for modifying existing records.

**Target:** Method

**Usage:**

```csharp
[Remote]
[Update]
public async Task Update([Service] IPersonContext context)
{
    var entity = await context.Persons.FindAsync(Id);
    MapTo(entity);
    await context.SaveChangesAsync();
}
```

**Notes:**
- Called by `Save()` when `IsNew == false` and `IsDeleted == false`
- Can be combined with `[Insert]` for upsert pattern

---

### [Delete]

Marks a method as a Delete operation for removing records.

**Target:** Method

**Usage:**

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

**Notes:**
- Called by `Save()` when `IsDeleted == true` and `IsNew == false`
- If `IsDeleted == true` and `IsNew == true`, `Save()` returns without action

---

### [Execute]

Marks a static method for remote execution without a domain model instance.

**Target:** Static Method

**Usage:**

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
    public static async Task<List<PersonSummary>> GetSummaries([Service] IPersonContext context)
    {
        return await context.Persons
            .Select(p => new PersonSummary { Name = p.FirstName + " " + p.LastName })
            .ToListAsync();
    }
}
```

**Generated Factory:**

```csharp
public interface IPersonOperationsFactory
{
    Task<int> GetPersonCount();
    Task<List<PersonSummary>> GetSummaries();
}
```

---

### [Remote]

Indicates that a method should execute on the server when called from a Remote mode client.

**Target:** Method

**Usage:**

```csharp
[Remote]  // This method executes on server
[Fetch]
public async Task<bool> Fetch(int id, [Service] IPersonContext context)
{
    // Database access - must run on server
}

[Create]  // No [Remote] - executes locally
public PersonModel()
{
    // Simple construction - can run anywhere
}
```

**Behavior by Mode:**

| Mode | [Remote] Method | Non-[Remote] Method |
|------|-----------------|---------------------|
| Server | Executes locally | Executes locally |
| Remote | Calls server via HTTP | Executes locally |
| Logical | Executes locally (serialized) | Executes locally |

**Inheritance:**

The `[Remote]` attribute has `Inherited = true`, meaning:
- Methods in derived classes inherit the `[Remote]` behavior from base class methods
- If a base class method is marked `[Remote]`, overriding methods in derived classes will also execute remotely
- This allows base classes to define remote behavior that derived classes automatically inherit

```csharp
[Factory]
public class BaseModel
{
    [Remote]
    [Fetch]
    public virtual async Task<bool> Fetch([Service] IContext ctx) { ... }
}

[Factory]
public class DerivedModel : BaseModel
{
    // This override inherits [Remote] from base - executes on server
    public override async Task<bool> Fetch([Service] IContext ctx) { ... }
}
```

---

### [AspAuthorize]

Integrates with ASP.NET Core authorization policies.

**Target:** Method

**Properties:**
- `Policy` (string): Policy name to require
- `Roles` (string): Comma-delimited list of allowed roles
- `AuthenticationSchemes` (string): Comma-delimited list of schemes

**Usage:**

```csharp
[Remote]
[Fetch]
[AspAuthorize(Policy = "CanReadPersons")]
public async Task<bool> Fetch(int id, [Service] IPersonContext context)
{
    // Only users meeting "CanReadPersons" policy can call this
}

[Remote]
[Update]
[AspAuthorize(Roles = "Admin,Manager")]
public async Task Update([Service] IPersonContext context)
{
    // Only Admin or Manager roles can call this
}
```

**Notes:**
- Requires `Neatoo.RemoteFactory.AspNetCore` package on server
- Authorization is checked on the server before method execution

---

### [AuthorizeFactory] (on methods)

Marks an authorization method and specifies which operations it authorizes.

**Target:** Method (in authorization interface/class)

**Parameter:** `AuthorizeFactoryOperation` flags

**Usage:**

```csharp
public interface IPersonModelAuth
{
    // Authorizes all read and write operations
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool CanAccess();

    // Authorizes only Create
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    // Authorizes only Fetch
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    // Authorizes only Delete
    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}
```

**AuthorizeFactoryOperation Flags:**

| Flag | Value | Description |
|------|-------|-------------|
| `Create` | 1 | Create operations |
| `Fetch` | 2 | Fetch operations |
| `Insert` | 4 | Insert operations |
| `Update` | 8 | Update operations |
| `Delete` | 16 | Delete operations |
| `Read` | 64 | All read operations (Create, Fetch) |
| `Write` | 128 | All write operations (Insert, Update, Delete) |
| `Execute` | 256 | Execute operations |

---

## Parameter Attributes

### [Service]

Marks a parameter for dependency injection resolution on the server.

**Target:** Parameter

**Usage:**

```csharp
[Remote]
[Fetch]
public async Task<bool> Fetch(
    int id,                              // Regular parameter - part of factory signature
    [Service] IPersonContext context,    // Service - resolved from DI
    [Service] ILogger<PersonModel> logger // Another service
)
{
    logger.LogInformation("Fetching person {Id}", id);
    var entity = await context.Persons.FindAsync(id);
    // ...
}
```

**Generated Factory Method:**

```csharp
// Services are excluded from the signature
Task<IPersonModel?> Fetch(int id);
```

**Notes:**
- Services are resolved from `IServiceProvider` on the server
- Services are never serialized or sent from client
- Method fails on client if called without `[Remote]` and service isn't registered

---

## Property Attributes

### [MapperIgnore]

Excludes a property from generated MapTo/MapFrom methods.

**Target:** Property

**Usage:**

```csharp
[Factory]
public partial class PersonModel
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [MapperIgnore]  // Not included in mapping
    public string FullName => $"{FirstName} {LastName}";

    [MapperIgnore]  // Not included in mapping
    public bool IsDirty { get; set; }

    public partial void MapFrom(PersonEntity entity);
    public partial void MapTo(PersonEntity entity);
}
```

**Generated Mapper:**

```csharp
public partial void MapFrom(PersonEntity entity)
{
    this.FirstName = entity.FirstName;
    this.LastName = entity.LastName;
    // FullName and IsDirty are not mapped
}
```

---

## Assembly Attributes

### [FactoryHintNameLength]

Controls the maximum length of generated file names (hint names).

**Target:** Assembly

**Parameter:** Maximum length in characters

**Usage:**

```csharp
// In AssemblyInfo.cs or any file
[assembly: FactoryHintNameLength(100)]
```

**Notes:**
- Some file systems have path length limits
- Default behavior uses full namespace paths
- Reduce this value if you encounter path-too-long errors

---

## Attribute Summary Table

| Attribute | Target | Purpose |
|-----------|--------|---------|
| `[Factory]` | Class/Interface | Enable factory generation |
| `[SuppressFactory]` | Class/Interface | Disable factory generation |
| `[AuthorizeFactory<T>]` | Class/Interface | Link authorization class |
| `[Create]` | Constructor/Method | Mark as Create operation |
| `[Fetch]` | Method | Mark as Fetch operation |
| `[Insert]` | Method | Mark as Insert operation |
| `[Update]` | Method | Mark as Update operation |
| `[Delete]` | Method | Mark as Delete operation |
| `[Execute]` | Static Method | Mark as Execute operation |
| `[Remote]` | Method | Execute on server |
| `[AspAuthorize]` | Method | ASP.NET Core authorization |
| `[AuthorizeFactory]` | Method | Authorization method marker |
| `[Service]` | Parameter | DI resolution marker |
| `[MapperIgnore]` | Property | Exclude from mapping |
| `[FactoryHintNameLength]` | Assembly | Control file name length |

## Next Steps

- **[Interfaces Reference](interfaces.md)**: All framework interfaces
- **[Factory Modes](factory-modes.md)**: NeatooFactory enum reference
- **[Generated Code](generated-code.md)**: Understanding factory structure
