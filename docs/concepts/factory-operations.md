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

**Simple Example:**

<!-- snippet: docs:concepts/factory-operations:create-constructor -->
```csharp
[Create]
    public PersonModel()
    {
        IsNew = true;
    }
```
<!-- /snippet -->

**Multiple Constructors:**

<!-- snippet: docs:concepts/factory-operations:multiple-constructors -->
```csharp
[Factory]
public class PersonWithMultipleConstructors : IPersonModel
{
    [Create]
    public PersonWithMultipleConstructors()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsNew = true;
    }

    [Create]
    public PersonWithMultipleConstructors(string firstName, string lastName)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        IsNew = true;
    }

    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }
}
```
<!-- /snippet -->

**Generated Factory Methods:**

```csharp
// Generated factory interface:
// public interface IPersonWithMultipleConstructorsFactory
// {
//     IPersonWithMultipleConstructors? Create();
//     IPersonWithMultipleConstructors? Create(string firstName, string lastName);
// }
```

**On a Method:**

<!-- snippet: docs:concepts/factory-operations:create-on-method -->
```csharp
[Factory]
public class PersonWithMethodCreate
{
    public PersonWithMethodCreate() { }

    [Create]
    public void Initialize(string template)
    {
        // Setup from template
        Template = template;
    }

    [Create]
    public async Task InitializeAsync([Service] ITemplateService templates)
    {
        // Async initialization with services
        Template = await templates.GetDefaultTemplateAsync();
    }

    public string? Template { get; private set; }
}
```
<!-- /snippet -->

**Static Create Methods:**

<!-- snippet: docs:concepts/factory-operations:static-create -->
```csharp
[Factory]
public class PersonWithStaticCreate
{
    private PersonWithStaticCreate() { }

    [Create]
    public static async Task<PersonWithStaticCreate> CreateWithDefaults([Service] IDefaultsService defaults)
    {
        var model = new PersonWithStaticCreate();
        await model.ApplyDefaults(defaults);
        return model;
    }

    private async Task ApplyDefaults(IDefaultsService defaults)
    {
        DefaultValue = await defaults.GetDefaultValueAsync();
    }

    public string? DefaultValue { get; private set; }
}
```
<!-- /snippet -->

### Fetch

The `[Fetch]` attribute marks methods that load existing data, typically from a database.

**Simple Example:**

<!-- snippet: docs:concepts/factory-operations:fetch-method -->
```csharp
[Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(id);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        IsNew = false;
        return true;
    }
```
<!-- /snippet -->

**Complete Example with Multiple Fetch Methods:**

<!-- snippet: docs:concepts/factory-operations:multiple-fetch-methods -->
```csharp
[Factory]
public class PersonWithMultipleFetch
{
    public int Id { get; private set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool IsNew { get; set; } = true;

    [Create]
    public PersonWithMultipleFetch() { }

    // Simple fetch by ID
    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(id);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
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

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        IsNew = false;
        return true;
    }
}
```
<!-- /snippet -->

**Generated Factory Methods:**

```csharp
// Generated factory interface:
// public interface IPersonWithMultipleFetchFactory
// {
//     IPersonWithMultipleFetch? Create();
//     Task<IPersonWithMultipleFetch?> Fetch(int id);
//     Task<IPersonWithMultipleFetch?> FetchByEmail(string email);
// }
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

<!-- snippet: docs:concepts/factory-operations:insert-example -->
```csharp
[Factory]
public class PersonInsertExample : IFactorySaveMeta
{
    public int Id { get; private set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote]
    [Insert]
    public async Task Insert([Service] IPersonContext context)
    {
        var entity = new PersonEntity();
        entity.FirstName = FirstName;
        entity.LastName = LastName;
        context.Persons.Add(entity);
        await context.SaveChangesAsync();

        Id = entity.Id;
        IsNew = false;
    }
}
```
<!-- /snippet -->

### Update

The `[Update]` attribute marks methods that modify existing records:

<!-- snippet: docs:concepts/factory-operations:update-example -->
```csharp
[Factory]
public class PersonUpdateExample : IFactorySaveMeta
{
    public int Id { get; private set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote]
    [Update]
    public async Task Update([Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(Id)
            ?? throw new InvalidOperationException("Person not found");

        entity.FirstName = FirstName;
        entity.LastName = LastName;
        await context.SaveChangesAsync();
    }
}
```
<!-- /snippet -->

### Combined Insert/Update (Upsert)

You can apply both attributes to a single method:

<!-- snippet: docs:concepts/factory-operations:combined-save -->
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

        entity.FirstName = FirstName;
        entity.LastName = LastName;
        entity.Email = Email;
        await context.SaveChangesAsync();

        Id = entity.Id;
        IsNew = false;
    }
```
<!-- /snippet -->

### Delete

The `[Delete]` attribute marks methods that remove records:

<!-- snippet: docs:concepts/factory-operations:delete-method -->
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
<!-- /snippet -->

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

<!-- snippet: docs:concepts/factory-operations:using-save -->
```csharp
public class UsingSaveExample
{
    public async Task DemoSave(IPersonModelFactory factory)
    {
        // Create and save new
        var person = factory.Create()!;
        person.FirstName = "John";
        await factory.Save(person);  // Calls Insert

        // Modify and save existing
        person.FirstName = "Jane";
        await factory.Save(person);  // Calls Update

        // Delete
        person.IsDeleted = true;
        await factory.Save(person);  // Calls Delete
    }
}
```
<!-- /snippet -->

### Save vs TrySave

- `Save` throws `NotAuthorizedException` if authorization fails
- `TrySave` returns an `Authorized<T>` result you can check

<!-- snippet: docs:concepts/factory-operations:save-vs-trysave -->
```csharp
public class SaveVsTrySaveExample
{
    public async Task DemoSaveWithException(IPersonModelFactory factory, IPersonModel person)
    {
        // Save throws on authorization failure
        try
        {
            var result = await factory.Save(person);
        }
        catch (NotAuthorizedException)
        {
            // Handle authorization failure
        }
    }

    public async Task DemoTrySave(IPersonModelFactory factory, IPersonModel person)
    {
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
    }
}
```
<!-- /snippet -->

## Execute Operations

The `[Execute]` attribute is used for static methods that perform operations without a domain model instance.

**Naming Convention:** Execute methods use an underscore prefix (e.g., `_GetPersonCount`) because the source generator creates a delegate type with the base name (`GetPersonCount`). Without the underscore, you'd have a naming conflict between your method and the generated delegate type. The generated factory interface exposes the method without the underscore.

<!-- snippet: docs:concepts/factory-operations:execute-operations -->
```csharp
[Factory]
public static partial class PersonOperationsExample
{
    [Remote]
    [Execute]
    private static async Task<int> _GetPersonCount([Service] IPersonContext context)
    {
        return await context.Persons.CountAsync();
    }

    [Remote]
    [Execute]
    private static async Task<List<string>> _GetAllEmails([Service] IPersonContext context)
    {
        return await context.Persons
            .Where(p => p.Email != null)
            .Select(p => p.Email!)
            .ToListAsync();
    }
}
```
<!-- /snippet -->

**Generated Factory:**

```csharp
// Generated factory interface:
// public interface IPersonOperationsExampleFactory
// {
//     Task<int> GetPersonCount();
//     Task<List<string>> GetAllEmails();
// }
```

## Commands & Queries Pattern

For simple **request-response** operations that don't involve domain object graphs, use `[Execute]` with static classes. This pattern is ideal for:

- **Queries**: Fetch data without loading a full domain model
- **Commands**: Perform actions that return simple results
- **Lookups**: Get dropdown options, validate codes, check availability
- **Reports**: Generate aggregated data or summaries

### Simple Query Example

**Request and Response:**

<!-- snippet: docs:concepts/factory-operations:query-request-response -->
```csharp
// Request (criteria value object)
public class GetUserQuery
{
    public int UserId { get; set; }
}

// Response (result value object)
public class UserResult
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}
```
<!-- /snippet -->

**The Query Handler:**

<!-- snippet: docs:concepts/factory-operations:query-handler -->
```csharp
// The query handler
[Factory]
public static partial class UserQueriesExample
{
    [Remote]
    [Execute]
    private static async Task<UserResult?> _GetUser(
        GetUserQuery query,
        [Service] IUserContext ctx)
    {
        var user = await ctx.Users.FindAsync(query.UserId);
        if (user == null) return null;

        return new UserResult
        {
            Name = user.Name,
            Email = user.Email,
            IsActive = user.IsActive
        };
    }
}
```
<!-- /snippet -->

**Generated factory:**

```csharp
// public interface IUserQueriesExampleFactory
// {
//     Task<UserResult?> GetUser(GetUserQuery query);
// }
```

**Usage in Blazor:**

<!-- snippet: docs:concepts/factory-operations:blazor-query-usage -->
```csharp
public class BlazorQueryUsageExample
{
    private readonly IUserQueriesFactory _userQueries;
    private UserResult? _user;

    public BlazorQueryUsageExample(IUserQueriesFactory userQueries)
    {
        _userQueries = userQueries;
    }

    public async Task LoadUser(int userId)
    {
        _user = await _userQueries.GetUser(new GetUserQuery { UserId = userId });
    }
}
```
<!-- /snippet -->

### Simple Command Example

**Command Request and Result:**

<!-- snippet: docs:concepts/factory-operations:command-request-response -->
```csharp
// Command request
public class DeactivateUserCommand
{
    public int UserId { get; set; }
    public string? Reason { get; set; }
}

// Command result
public class CommandResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}
```
<!-- /snippet -->

**The Command Handler:**

<!-- snippet: docs:concepts/factory-operations:command-handler -->
```csharp
[Factory]
public static partial class UserCommandsExample
{
    [Remote]
    [Execute]
    private static async Task<CommandResult> _DeactivateUser(
        DeactivateUserCommand command,
        [Service] IUserContext ctx)
    {
        var user = await ctx.Users.FindAsync(command.UserId);
        if (user == null)
        {
            return new CommandResult { Success = false, Message = "User not found" };
        }

        user.IsActive = false;
        user.DeactivationReason = command.Reason;
        await ctx.SaveChangesAsync();

        return new CommandResult { Success = true, Message = "User deactivated" };
    }
}
```
<!-- /snippet -->

### Search with Criteria

**Search Criteria and Results:**

<!-- snippet: docs:concepts/factory-operations:search-criteria -->
```csharp
public class ProductSearchCriteria
{
    public string? Keyword { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ProductSearchResults
{
    public List<ProductDto> Products { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class ProductDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Price { get; set; }
}
```
<!-- /snippet -->

**The Search Handler:**

<!-- snippet: docs:concepts/factory-operations:search-handler -->
```csharp
[Factory]
public static partial class ProductSearchExample
{
    [Remote]
    [Execute]
    private static async Task<ProductSearchResults> _Search(
        ProductSearchCriteria criteria,
        [Service] IProductContext ctx)
    {
        var query = ctx.Products.AsQueryable();

        if (!string.IsNullOrEmpty(criteria.Keyword))
            query = query.Where(p => p.Name!.Contains(criteria.Keyword));

        if (criteria.MinPrice.HasValue)
            query = query.Where(p => p.Price >= criteria.MinPrice.Value);

        if (criteria.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= criteria.MaxPrice.Value);

        var totalCount = await query.CountAsync();

        var products = await query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price })
            .ToListAsync();

        return new ProductSearchResults
        {
            Products = products,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)criteria.PageSize)
        };
    }
}
```
<!-- /snippet -->

For more advanced patterns including authorization, multiple methods per class, and error handling, see **[Commands, Queries & Static Execute](../advanced/static-execute.md)**.

## The Remote Attribute

The `[Remote]` attribute indicates a method should execute on the server in Remote mode:

<!-- snippet: docs:concepts/factory-operations:remote-attribute -->
```csharp
[Factory]
public class PersonRemoteExample
{
    // Executes locally (no [Remote])
    [Create]
    public PersonRemoteExample() { }

    // Executes on server
    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        // Server-side code with database access
        var entity = await context.Persons.FindAsync(id);
        return entity != null;
    }
}
```
<!-- /snippet -->

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

<!-- snippet: docs:concepts/factory-operations:operation-matching -->
```csharp
[Factory]
public class OrderModelWithMatching : IFactorySaveMeta
{
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // Default save operations
    [Remote]
    [Insert]
    public void Insert([Service] IOrderContext context) { }

    [Remote]
    [Update]
    public void Update([Service] IOrderContext context) { }

    [Remote]
    [Delete]
    public void Delete([Service] IOrderContext context) { }

    // Operations with extra parameter
    [Remote]
    [Insert]
    public void InsertWithAudit(string auditReason, [Service] IOrderContext context) { }

    [Remote]
    [Update]
    public void UpdateWithAudit(string auditReason, [Service] IOrderContext context) { }

    [Remote]
    [Delete]
    public void DeleteWithAudit(string auditReason, [Service] IOrderContext context) { }
}
```
<!-- /snippet -->

**Generated Save Methods:**

```csharp
// public interface IOrderModelWithMatchingFactory
// {
//     Task<IOrderModelWithMatching?> Save(IOrderModelWithMatching target);
//     Task<IOrderModelWithMatching?> SaveWithAudit(IOrderModelWithMatching target, string auditReason);
// }
```

## Next Steps

- **[Three-Tier Execution](three-tier-execution.md)**: Server, Remote, and Logical modes
- **[Service Injection](service-injection.md)**: Using `[Service]` for DI
- **[Attributes Reference](../reference/attributes.md)**: Complete attribute documentation
- **[Authorization Overview](../authorization/authorization-overview.md)**: Adding access control
