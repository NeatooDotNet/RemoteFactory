---
layout: default
title: "Authorization Overview"
description: "Understanding authorization approaches in RemoteFactory"
parent: Authorization
nav_order: 1
---

# Authorization Overview

RemoteFactory provides two complementary approaches to authorization: custom authorization rules with `[AuthorizeFactory<T>]` and ASP.NET Core policy integration with `[AspAuthorize]`. Both approaches generate `Can*` methods on the factory for client-side authorization checking.

## RemoteFactory Authorization Benefits

RemoteFactory centralizes authorization at the factory level:
- **Single point of enforcement**: All operations go through the factory
- **Consistent**: Server always checks before execution
- **Client-aware**: Generated `Can*` methods enable permission-driven UI

## Two Authorization Approaches

### 1. Custom Authorization with [AuthorizeFactory<T>]

Define your own authorization logic with full control:

```csharp
public interface IPersonModelAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool CanAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}

internal class PersonModelAuth : IPersonModelAuth
{
    private readonly ICurrentUser _user;

    public PersonModelAuth(ICurrentUser user)
    {
        _user = user;
    }

    public bool CanAccess() => _user.IsAuthenticated;
    public bool CanCreate() => _user.HasRole("Creator");
    public bool CanFetch() => true;  // Anyone can read
    public bool CanDelete() => _user.HasRole("Admin");
}

[Factory]
[AuthorizeFactory<IPersonModelAuth>]
public class PersonModel { }
```

**Best for:**
- Complex business rules
- Role hierarchies
- Data-driven authorization
- Custom user services

### 2. ASP.NET Core Policies with [AspAuthorize]

Integrate with ASP.NET Core's policy-based authorization:

```csharp
[Factory]
public class PersonModel
{
    [Remote]
    [Fetch]
    [AspAuthorize(Policy = "CanReadPersons")]
    public async Task<bool> Fetch([Service] IPersonContext context)
    {
        // Only users meeting "CanReadPersons" policy can execute
    }

    [Remote]
    [Update]
    [AspAuthorize(Roles = "Admin,Manager")]
    public async Task Update([Service] IPersonContext context)
    {
        // Only Admin or Manager roles
    }
}
```

**Best for:**
- Existing ASP.NET Core policies
- Claims-based authorization
- Standard role checks
- Integration with external identity providers

## Authorization Flow

```
Client                                      Server
  │                                           │
  │ factory.CanFetch()                        │
  │      │                                    │
  │      ▼                                    │
  │ ┌─────────────────────┐                   │
  │ │ Check authorization │                   │
  │ │ (can run on client) │                   │
  │ └─────────────────────┘                   │
  │      │                                    │
  │      ▼                                    │
  │ Authorized { HasAccess: true }            │
  │                                           │
  │ factory.Fetch()                           │
  │      │                                    │
  │      └───────────────────────────────────>│
  │                                           │
  │                          ┌────────────────┤
  │                          │ Authorization  │
  │                          │ checked AGAIN  │
  │                          │ on server      │
  │                          └────────────────┤
  │                                           │
  │                          (if authorized)  │
  │                          Execute method   │
  │                                           │
  │<──────────────────────────────────────────┤
  │ PersonModel result                        │
```

## Generated Can* Methods

For each operation type, RemoteFactory generates authorization check methods:

| Factory Method | Generated Can Method |
|----------------|---------------------|
| `Create()` | `CanCreate()` |
| `Fetch()` | `CanFetch()` |
| `Save()` | `CanSave()` |
| `TrySave()` | `CanSave()` |

The `CanSave()` method checks all write operations (Insert, Update, Delete).

### Using Can* Methods

```csharp
@inject IPersonModelFactory PersonFactory

@if (PersonFactory.CanCreate().HasAccess)
{
    <button @onclick="CreatePerson">New Person</button>
}

@if (PersonFactory.CanFetch().HasAccess)
{
    <button @onclick="LoadPerson">Load</button>
}

@if (_person != null && PersonFactory.CanSave().HasAccess)
{
    <button @onclick="SavePerson">Save</button>
}
```

## The Authorized Type

Authorization results are wrapped in the `Authorized` type:

```csharp
public class Authorized
{
    public bool HasAccess { get; init; }
    public string? Message { get; init; }
}

public class Authorized<T> : Authorized
{
    public T? Result { get; init; }
}
```

### Usage Patterns

```csharp
// Simple check
if (factory.CanFetch().HasAccess)
{
    // Show fetch button
}

// Check with message
var canCreate = factory.CanCreate();
if (!canCreate.HasAccess)
{
    Console.WriteLine($"Cannot create: {canCreate.Message}");
}

// TrySave returns Authorized<T>
var result = await factory.TrySave(person);
if (result.HasAccess)
{
    var savedPerson = result.Result;
}
else
{
    ShowError(result.Message);
}
```

### Save vs TrySave

Both methods save, but differ in error handling:

```csharp
// Save throws on authorization failure
try
{
    var person = await factory.Save(model);
}
catch (NotAuthorizedException ex)
{
    // Handle denial
    Console.WriteLine(ex.Authorized.Message);
}

// TrySave returns result to check
var result = await factory.TrySave(model);
if (!result.HasAccess)
{
    // Handle denial without exception
    Console.WriteLine(result.Message);
}
```

## Combining Both Approaches

You can use both authorization approaches together:

```csharp
public interface IPersonModelAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanAccess();
}

[Factory]
[AuthorizeFactory<IPersonModelAuth>]  // Custom for general access
public class PersonModel
{
    [Remote]
    [Fetch]
    [AspAuthorize(Policy = "DetailedView")]  // Policy for specific operation
    public async Task<bool> FetchWithDetails([Service] IPersonContext context)
    {
        // Both checks must pass
    }
}
```

**Execution order:**
1. Custom authorization (`IPersonModelAuth.CanAccess()`) checked first
2. If passes, method-level `[AspAuthorize]` checked
3. If both pass, method executes

## Authorization Flags

The `AuthorizeFactoryOperation` flags control which operations an authorization method covers:

```csharp
[Flags]
public enum AuthorizeFactoryOperation
{
    Create = 1,      // Create operations
    Fetch = 2,       // Fetch operations
    Insert = 4,      // Insert operations
    Update = 8,      // Update operations
    Delete = 16,     // Delete operations
    Read = 64,       // All read ops (Create, Fetch, Execute)
    Write = 128,     // All write ops (Insert, Update, Delete)
    Execute = 256    // Execute operations
}
```

### Flag Composition

The `Read` and `Write` flags are **meta-flags** that cover multiple operations:

| Meta-Flag | Value | Covers These Operations |
|-----------|-------|------------------------|
| `Read` | 64 | Create, Fetch, Execute |
| `Write` | 128 | Insert, Update, Delete |

The `FactoryOperation` enum values are themselves composites of these flags:

| FactoryOperation | Composed Of |
|------------------|-------------|
| `Create` | `AuthorizeFactoryOperation.Create \| AuthorizeFactoryOperation.Read` |
| `Fetch` | `AuthorizeFactoryOperation.Fetch \| AuthorizeFactoryOperation.Read` |
| `Execute` | `AuthorizeFactoryOperation.Execute \| AuthorizeFactoryOperation.Read` |
| `Insert` | `AuthorizeFactoryOperation.Insert \| AuthorizeFactoryOperation.Write` |
| `Update` | `AuthorizeFactoryOperation.Update \| AuthorizeFactoryOperation.Write` |
| `Delete` | `AuthorizeFactoryOperation.Delete \| AuthorizeFactoryOperation.Write` |

**Practical implication:** An authorization method marked with `AuthorizeFactoryOperation.Read` will be checked for Create, Fetch, and Execute operations. Similarly, `AuthorizeFactoryOperation.Write` covers Insert, Update, and Delete.

### Common Patterns

```csharp
public interface IPersonModelAuth
{
    // Check for all operations
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool IsAuthenticated();

    // Check for all read operations
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    // Check for all write operations
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();

    // Check for specific operations
    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}
```

## Client vs Server Enforcement

### Client-Side (Can* Methods)

- Used for UI decisions (show/hide buttons)
- Runs synchronously on client
- Should not be the only check

```csharp
// Good: Inform UI
if (!factory.CanSave().HasAccess)
{
    saveButton.Enabled = false;
}
```

### Server-Side (Automatic)

- Always enforced before method execution
- Cannot be bypassed by client
- Returns `Authorized` result or throws `NotAuthorizedException`

```csharp
// Server-side generated code (simplified)
public async Task<Authorized<IPersonModel>> LocalFetch()
{
    var auth = ServiceProvider.GetRequiredService<IPersonModelAuth>();

    // Checked before method runs
    var canAccess = auth.CanAccess();
    if (!canAccess) return new Authorized<IPersonModel>(canAccess);

    var canFetch = auth.CanFetch();
    if (!canFetch) return new Authorized<IPersonModel>(canFetch);

    // Only now does the actual method run
    var result = await target.Fetch(context);
    return new Authorized<IPersonModel>(result);
}
```

## Best Practices

### 1. Always Check Server-Side

Never rely solely on client-side checks:

```csharp
// Bad: Only client check
if (factory.CanDelete().HasAccess)
{
    await factory.Save(deletedPerson);  // Server might deny!
}

// Good: Handle server response
var result = await factory.TrySave(deletedPerson);
if (!result.HasAccess)
{
    ShowError(result.Message);
}
```

### 2. Use TrySave for Graceful Handling

```csharp
public async Task SavePerson()
{
    var result = await _factory.TrySave(_person);

    if (result.HasAccess)
    {
        _person = result.Result;
        ShowSuccess("Saved!");
    }
    else
    {
        ShowError(result.Message ?? "Not authorized");
    }
}
```

### 3. Centralize Authorization Logic

Keep authorization rules in one place:

```csharp
public class PersonModelAuth : IPersonModelAuth
{
    // All authorization decisions here
    // Easy to audit and modify
}
```

## Next Steps

- **[Custom Authorization](custom-authorization.md)**: Detailed `[AuthorizeFactory<T>]` guide
- **[ASP.NET Core Integration](asp-authorize.md)**: Using `[AspAuthorize]`
- **[Can Methods](can-methods.md)**: Generated authorization check methods
