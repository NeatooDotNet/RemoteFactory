---
layout: default
title: "Custom Authorization"
description: "Implementing custom authorization with [AuthorizeFactory<T>]"
parent: Authorization
nav_order: 2
---

# Custom Authorization

The `[AuthorizeFactory<T>]` attribute links a custom authorization class to your domain model. This gives you complete control over authorization logic with full access to dependency injection.

## Basic Setup

### 1. Define the Authorization Interface

Create an interface with methods for each authorization check:

```csharp
public interface IPersonModelAuth
{
    // General access check - applied to all operations
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool CanAccess();

    // Specific operation checks
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    bool CanUpdate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}
```

### 2. Implement the Authorization Class

```csharp
internal class PersonModelAuth : IPersonModelAuth
{
    private readonly ICurrentUser _user;

    // Inject dependencies via constructor
    public PersonModelAuth(ICurrentUser user)
    {
        _user = user;
    }

    public bool CanAccess()
    {
        // User must be authenticated
        return _user.IsAuthenticated;
    }

    public bool CanCreate()
    {
        // Creators and Admins can create
        return _user.HasRole("Creator") || _user.HasRole("Admin");
    }

    public bool CanFetch()
    {
        // Anyone authenticated can read
        return true;
    }

    public bool CanUpdate()
    {
        // Editors and Admins can update
        return _user.HasRole("Editor") || _user.HasRole("Admin");
    }

    public bool CanDelete()
    {
        // Only Admins can delete
        return _user.HasRole("Admin");
    }
}
```

### 3. Apply to Domain Model

```csharp
[Factory]
[AuthorizeFactory<IPersonModelAuth>]
public class PersonModel : IPersonModel
{
    [Create]
    public PersonModel() { }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch([Service] IPersonContext context)
    {
        // Authorization is checked BEFORE this runs
    }
}
```

### 4. Register in DI

```csharp
// Server Program.cs
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.RegisterMatchingName(typeof(IPersonModelAuth).Assembly);
// Or explicitly:
// builder.Services.AddScoped<IPersonModelAuth, PersonModelAuth>();
```

## How Authorization is Checked

When a factory method is called, the generated code:

1. Resolves the authorization interface from DI
2. Calls all applicable authorization methods
3. If any return `false`, returns `Authorized` with `HasAccess = false`
4. If all pass, executes the actual method

### Generated Code Example

```csharp
// Generated LocalFetch method
public async Task<Authorized<IPersonModel>> LocalFetch()
{
    // Step 1: Resolve authorization
    IPersonModelAuth auth = ServiceProvider.GetRequiredService<IPersonModelAuth>();

    // Step 2: Check general access (Read | Write flag includes Fetch)
    var canAccess = auth.CanAccess();
    if (!canAccess)
    {
        return new Authorized<IPersonModel>(new Authorized(false));
    }

    // Step 3: Check specific operation
    var canFetch = auth.CanFetch();
    if (!canFetch)
    {
        return new Authorized<IPersonModel>(new Authorized(false));
    }

    // Step 4: Execute the actual method
    var target = ServiceProvider.GetRequiredService<PersonModel>();
    var context = ServiceProvider.GetRequiredService<IPersonContext>();
    return new Authorized<IPersonModel>(
        await DoFactoryMethodCallBoolAsync(target, FactoryOperation.Fetch,
            () => target.Fetch(context))
    );
}
```

## Operation Flags Explained

The `AuthorizeFactoryOperation` enum uses flags to specify which operations a method authorizes:

| Flag | Value | Applies To |
|------|-------|------------|
| `Create` | 1 | Create operations |
| `Fetch` | 2 | Fetch operations |
| `Insert` | 4 | Insert operations |
| `Update` | 8 | Update operations |
| `Delete` | 16 | Delete operations |
| `Read` | 64 | Create + Fetch |
| `Write` | 128 | Insert + Update + Delete |
| `Execute` | 256 | Execute operations |

### Flag Combinations

```csharp
// Check all operations
[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]

// Check all read operations
[AuthorizeFactory(AuthorizeFactoryOperation.Read)]

// Check all write operations
[AuthorizeFactory(AuthorizeFactoryOperation.Write)]

// Check only Create and Fetch (same as Read)
[AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]

// Check Update and Delete but not Insert
[AuthorizeFactory(AuthorizeFactoryOperation.Update | AuthorizeFactoryOperation.Delete)]
```

## Returning Messages

Authorization methods can return `bool` or `Authorized` to include messages:

```csharp
public interface IOrderModelAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    Authorized CanDelete();  // Returns Authorized instead of bool
}

internal class OrderModelAuth : IOrderModelAuth
{
    private readonly IOrderContext _context;
    private readonly ICurrentUser _user;

    public OrderModelAuth(IOrderContext context, ICurrentUser user)
    {
        _context = context;
        _user = user;
    }

    public Authorized CanDelete()
    {
        if (!_user.HasRole("Admin"))
        {
            return "Only administrators can delete orders";
        }

        // Could check additional conditions
        return true;  // Implicit conversion to Authorized(true)
    }
}
```

**Using the message:**

```csharp
var result = await factory.TrySave(deletedOrder);
if (!result.HasAccess)
{
    // Shows: "Only administrators can delete orders"
    ShowError(result.Message);
}
```

## Dependency Injection in Authorization

Authorization classes support full constructor injection:

```csharp
internal class PersonModelAuth : IPersonModelAuth
{
    private readonly ICurrentUser _user;
    private readonly IPermissionService _permissions;
    private readonly ILogger<PersonModelAuth> _logger;

    public PersonModelAuth(
        ICurrentUser user,
        IPermissionService permissions,
        ILogger<PersonModelAuth> logger)
    {
        _user = user;
        _permissions = permissions;
        _logger = logger;
    }

    public bool CanDelete()
    {
        _logger.LogDebug("Checking delete permission for user {UserId}", _user.Id);

        // Complex permission check
        return _permissions.HasPermission(_user.Id, "Person", "Delete");
    }
}
```

## Role-Based Authorization

### Simple Role Check

```csharp
internal class PersonModelAuth : IPersonModelAuth
{
    private readonly IUser _user;

    public PersonModelAuth(IUser user) => _user = user;

    public bool CanAccess() => _user.Role > Role.None;
    public bool CanCreate() => _user.Role >= Role.Create;
    public bool CanFetch() => _user.Role >= Role.Fetch;
    public bool CanUpdate() => _user.Role >= Role.Update;
    public bool CanDelete() => _user.Role >= Role.Delete;
}
```

### Role Hierarchy

```csharp
public enum Role
{
    None = 0,
    Viewer = 1,      // Can Fetch
    Creator = 2,     // Can Fetch, Create
    Editor = 3,      // Can Fetch, Create, Update
    Admin = 4        // All operations
}

internal class PersonModelAuth : IPersonModelAuth
{
    private readonly IUser _user;

    public bool CanAccess() => _user.Role >= Role.Viewer;
    public bool CanCreate() => _user.Role >= Role.Creator;
    public bool CanFetch() => _user.Role >= Role.Viewer;
    public bool CanUpdate() => _user.Role >= Role.Editor;
    public bool CanDelete() => _user.Role >= Role.Admin;
}
```

## Data-Driven Authorization

Check authorization based on the data itself:

```csharp
internal class OrderModelAuth : IOrderModelAuth
{
    private readonly ICurrentUser _user;
    private readonly IOrderContext _context;

    public OrderModelAuth(ICurrentUser user, IOrderContext context)
    {
        _user = user;
        _context = context;
    }

    public Authorized CanUpdate()
    {
        // Managers can update any order
        if (_user.HasRole("Manager"))
            return true;

        // Users can only update their own orders
        // Note: This would need the order ID passed somehow
        return "You can only update your own orders";
    }

    public Authorized CanDelete()
    {
        if (!_user.HasRole("Manager"))
            return "Only managers can delete orders";

        return true;
    }
}
```

## Testing Authorization

```csharp
public class PersonModelAuthTests
{
    [Fact]
    public void AdminCanDelete()
    {
        var user = new MockUser { Role = Role.Admin };
        var auth = new PersonModelAuth(user);

        Assert.True(auth.CanDelete());
    }

    [Fact]
    public void EditorCannotDelete()
    {
        var user = new MockUser { Role = Role.Editor };
        var auth = new PersonModelAuth(user);

        Assert.False(auth.CanDelete());
    }

    [Fact]
    public void ViewerCanFetch()
    {
        var user = new MockUser { Role = Role.Viewer };
        var auth = new PersonModelAuth(user);

        Assert.True(auth.CanFetch());
    }
}
```

## Complete Example

Here's a full authorization implementation:

```csharp
// Authorization interface
public interface IPersonModelAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    Authorized CanAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    Authorized CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    Authorized CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    Authorized CanUpdate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    Authorized CanDelete();
}

// Authorization implementation
internal class PersonModelAuth : IPersonModelAuth
{
    private readonly IUser _user;
    private readonly ILogger<PersonModelAuth> _logger;

    public PersonModelAuth(IUser user, ILogger<PersonModelAuth> logger)
    {
        _user = user;
        _logger = logger;
    }

    public Authorized CanAccess()
    {
        if (_user.Role == Role.None)
        {
            _logger.LogWarning("Unauthorized access attempt");
            return "You must be logged in to access this feature";
        }
        return true;
    }

    public Authorized CanCreate()
    {
        if (_user.Role < Role.Create)
            return $"Your role ({_user.Role}) does not allow creating records";
        return true;
    }

    public Authorized CanFetch()
    {
        if (_user.Role < Role.Fetch)
            return "You do not have read access";
        return true;
    }

    public Authorized CanUpdate()
    {
        if (_user.Role < Role.Update)
            return "You do not have edit access";
        return true;
    }

    public Authorized CanDelete()
    {
        if (_user.Role < Role.Delete)
            return "Only administrators can delete records";
        return true;
    }
}

// Domain model
[Factory]
[AuthorizeFactory<IPersonModelAuth>]
public partial class PersonModel : IPersonModel, IFactorySaveMeta
{
    [Create]
    public PersonModel() => IsNew = true;

    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        // Only runs if CanAccess() and CanFetch() both return true
    }

    [Remote]
    [Insert]
    [Update]
    public async Task Save([Service] IPersonContext context)
    {
        // Only runs if CanAccess() and CanUpdate() (or CanCreate for insert) return true
    }

    [Remote]
    [Delete]
    public async Task Delete([Service] IPersonContext context)
    {
        // Only runs if CanAccess() and CanDelete() both return true
    }
}
```

## Next Steps

- **[ASP.NET Core Integration](asp-authorize.md)**: Using `[AspAuthorize]` for policy-based authorization
- **[Can Methods](can-methods.md)**: Generated authorization check methods
- **[Authorization Overview](authorization-overview.md)**: Comparing approaches
