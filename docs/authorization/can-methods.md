---
layout: default
title: "Can Methods"
description: "Generated CanCreate, CanFetch, CanUpdate, CanDelete, and CanSave methods"
parent: Authorization
nav_order: 4
---

# Can Methods

RemoteFactory generates authorization check methods (Can* methods) that let you verify permissions without executing operations. These methods are essential for building permission-aware UIs.

## Generated Can Methods

When you define authorization using `[AuthorizeFactory<T>]`, the generator creates corresponding Can* methods:

| Authorization Method | Generated Factory Method | Purpose |
|---------------------|-------------------------|---------|
| Methods with `AuthorizeFactoryOperation.Read` | `CanCreate()`, `CanFetch()` | Check read permissions |
| Methods with `AuthorizeFactoryOperation.Write` | `CanUpdate()`, `CanDelete()` | Check write permissions |
| (Automatic) | `CanSave()` | Combined update and delete check |
| Named for operations | `CanUpsert()`, custom names | Match your operation names |

## Basic Example

### Define Authorization

```csharp
public interface IPersonModelAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanUpdate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanDelete();
}

public class PersonModelAuth : IPersonModelAuth
{
    private readonly ICurrentUser _user;

    public PersonModelAuth(ICurrentUser user) => _user = user;

    public bool CanAccess() => _user.IsAuthenticated;
    public bool CanCreate() => _user.HasRole("Editor");
    public bool CanFetch() => true;  // All authenticated users can read
    public bool CanUpdate() => _user.HasRole("Editor");
    public bool CanDelete() => _user.HasRole("Admin");
}
```

### Generated Factory Interface

```csharp
public interface IPersonModelFactory
{
    // Operation methods
    IPersonModel? Create();
    Task<IPersonModel?> Fetch(int id);
    Task<IPersonModel?> Save(IPersonModel target);

    // Generated Can* methods
    Authorized CanCreate();
    Authorized CanFetch();
    Authorized CanUpdate();
    Authorized CanDelete();
    Authorized CanSave();
}
```

### Generated Implementation

```csharp
public Authorized CanCreate()
{
    return LocalCanCreate();
}

public Authorized LocalCanCreate()
{
    Authorized authorized;
    IPersonModelAuth auth = ServiceProvider.GetRequiredService<IPersonModelAuth>();

    // Check CanAccess (Read operations require this)
    authorized = auth.CanAccess();
    if (!authorized.HasAccess)
    {
        return authorized;
    }

    // Check CanCreate
    authorized = auth.CanCreate();
    if (!authorized.HasAccess)
    {
        return authorized;
    }

    return new Authorized(true);
}

public Authorized CanSave()
{
    return LocalCanSave();
}

public Authorized LocalCanSave()
{
    Authorized authorized;
    IPersonModelAuth auth = ServiceProvider.GetRequiredService<IPersonModelAuth>();

    authorized = auth.CanAccess();
    if (!authorized.HasAccess)
    {
        return authorized;
    }

    // Save requires both Update and Delete permissions
    authorized = auth.CanUpdate();
    if (!authorized.HasAccess)
    {
        return authorized;
    }

    authorized = auth.CanDelete();
    if (!authorized.HasAccess)
    {
        return authorized;
    }

    return new Authorized(true);
}
```

## The Authorized Type

Can* methods return the `Authorized` struct:

```csharp
public readonly struct Authorized
{
    public bool HasAccess { get; }
    public string? Message { get; }

    public Authorized(bool hasAccess, string? message = null)
    {
        HasAccess = hasAccess;
        Message = message;
    }

    public static implicit operator Authorized(bool hasAccess)
        => new Authorized(hasAccess);

    public static implicit operator bool(Authorized authorized)
        => authorized.HasAccess;
}
```

### Checking Authorization

```csharp
// Simple boolean check
if (_factory.CanCreate().HasAccess)
{
    // Can create
}

// Implicit conversion to bool
if (_factory.CanCreate())
{
    // Also works
}

// Get failure reason
var canUpdate = _factory.CanUpdate();
if (!canUpdate.HasAccess)
{
    Console.WriteLine($"Cannot update: {canUpdate.Message}");
}
```

### Returning Messages from Authorization

Your authorization methods can return `Authorized` with messages:

```csharp
public class OrderAuth : IOrderAuth
{
    private readonly ICurrentUser _user;
    private readonly IOrderContext _context;

    public Authorized CanUpdate()
    {
        if (!_user.IsAuthenticated)
        {
            return new Authorized(false, "You must be logged in");
        }

        if (!_user.HasRole("OrderManager"))
        {
            return new Authorized(false, "Only Order Managers can update orders");
        }

        return new Authorized(true);
    }
}
```

## UI Integration

### Blazor Conditional Rendering

```razor
@inject IPersonModelFactory PersonFactory

@if (PersonFactory.CanCreate().HasAccess)
{
    <button class="btn btn-primary" @onclick="CreateNew">
        Add New Person
    </button>
}

@if (_person != null)
{
    <div class="form-group">
        <label>Name</label>
        <input @bind="_person.Name"
               disabled="@(!PersonFactory.CanUpdate().HasAccess)" />
    </div>

    @if (PersonFactory.CanUpdate().HasAccess)
    {
        <button class="btn btn-success" @onclick="Save">Save</button>
    }

    @if (PersonFactory.CanDelete().HasAccess)
    {
        <button class="btn btn-danger" @onclick="Delete">Delete</button>
    }
}

@code {
    private IPersonModel? _person;

    private void CreateNew()
    {
        _person = PersonFactory.Create();
    }

    private async Task Save()
    {
        if (_person != null)
        {
            _person = await PersonFactory.Save(_person);
        }
    }

    private async Task Delete()
    {
        if (_person != null)
        {
            _person.IsDeleted = true;
            await PersonFactory.Save(_person);
            _person = null;
        }
    }
}
```

### Cache Authorization Results

For performance, cache authorization checks that don't change within a component lifecycle:

```razor
@code {
    private bool _canCreate;
    private bool _canUpdate;
    private bool _canDelete;

    protected override void OnInitialized()
    {
        _canCreate = PersonFactory.CanCreate().HasAccess;
        _canUpdate = PersonFactory.CanUpdate().HasAccess;
        _canDelete = PersonFactory.CanDelete().HasAccess;
    }
}
```

### WPF ViewModel Integration

```csharp
public class PersonViewModel : INotifyPropertyChanged
{
    private readonly IPersonModelFactory _factory;

    public PersonViewModel(IPersonModelFactory factory)
    {
        _factory = factory;

        // Initialize commands with Can* checks
        CreateCommand = new RelayCommand(
            execute: () => Person = _factory.Create(),
            canExecute: () => _factory.CanCreate().HasAccess);

        SaveCommand = new RelayCommand(
            execute: async () => await SaveAsync(),
            canExecute: () => _factory.CanSave().HasAccess);

        DeleteCommand = new RelayCommand(
            execute: async () => await DeleteAsync(),
            canExecute: () => _factory.CanDelete().HasAccess);
    }

    public ICommand CreateCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }

    public IPersonModel? Person { get; private set; }
}
```

### Display Authorization Messages

```razor
@if (!_canSaveResult.HasAccess)
{
    <div class="alert alert-warning">
        <strong>Read Only:</strong> @_canSaveResult.Message
    </div>
}

@code {
    private Authorized _canSaveResult;

    protected override void OnInitialized()
    {
        _canSaveResult = PersonFactory.CanSave();
    }
}
```

## Authorization Order

Can* methods check authorization in the same order as operations:

1. **CanAccess** (if defined) - Universal access check
2. **Operation-specific check** - CanCreate, CanFetch, CanUpdate, or CanDelete

```csharp
// For CanCreate():
authorized = auth.CanAccess();    // Check access first
if (!authorized.HasAccess) return authorized;

authorized = auth.CanCreate();    // Then operation-specific
if (!authorized.HasAccess) return authorized;

return new Authorized(true);
```

## CanSave Behavior

`CanSave()` is special - it checks permissions for all write operations since Save routes to Insert, Update, or Delete based on object state:

```csharp
public Authorized LocalCanSave()
{
    Authorized authorized;
    IPersonModelAuth auth = ServiceProvider.GetRequiredService<IPersonModelAuth>();

    authorized = auth.CanAccess();
    if (!authorized.HasAccess) return authorized;

    // Must be able to update...
    authorized = auth.CanUpdate();
    if (!authorized.HasAccess) return authorized;

    // ...and delete
    authorized = auth.CanDelete();
    if (!authorized.HasAccess) return authorized;

    return new Authorized(true);
}
```

If you want more granular control, check the specific operation:

```razor
@if (_person.IsNew)
{
    @if (Factory.CanCreate().HasAccess)
    {
        <button @onclick="Save">Create</button>
    }
}
else if (_person.IsDeleted)
{
    @if (Factory.CanDelete().HasAccess)
    {
        <button @onclick="Save">Confirm Delete</button>
    }
}
else
{
    @if (Factory.CanUpdate().HasAccess)
    {
        <button @onclick="Save">Update</button>
    }
}
```

## Custom Operation Can Methods

When you use custom operation names, corresponding Can* methods are generated:

```csharp
[Factory]
[AuthorizeFactory<IOrderAuth>]
public class OrderModel : IOrderModel, IFactorySaveMeta
{
    [Remote]
    [Insert][Update]  // Combined as "Upsert"
    public async Task Upsert([Service] IOrderContext ctx) { }
}

public interface IOrderAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanUpsert();  // Matches the operation name
}

// Generated:
public interface IOrderModelFactory
{
    // ...
    Authorized CanUpsert();  // Generated for the custom operation
}
```

## Local-Only Execution

Can* methods always execute locally, even in Remote mode:

```csharp
// These always run on the client (no HTTP call)
var canCreate = _factory.CanCreate();
var canFetch = _factory.CanFetch();
var canSave = _factory.CanSave();
```

This means your authorization implementation must be available on the client. If authorization depends on server-side data, consider:

1. **Pre-fetch authorization data** - Load permissions on authentication
2. **Use client-side checks for UI** - Show/hide based on roles
3. **Server enforces final authorization** - Operations still check on server

```csharp
// Client-side implementation for UI
public class ClientPersonModelAuth : IPersonModelAuth
{
    private readonly IAuthState _auth;

    public bool CanAccess() => _auth.IsAuthenticated;
    public bool CanCreate() => _auth.Roles.Contains("Editor");
    // ...
}

// Server-side implementation (more complete checks)
public class ServerPersonModelAuth : IPersonModelAuth
{
    private readonly ICurrentUser _user;
    private readonly IPersonContext _context;

    public bool CanAccess() => _user.IsAuthenticated;
    public bool CanCreate()
    {
        // Additional server-side validation
        return _user.HasRole("Editor") &&
               _user.DepartmentId != null;
    }
}
```

## Best Practices

### Keep Authorization Logic Simple

Can* methods should be fast since they're called frequently:

```csharp
// Good: Fast role check
public bool CanUpdate() => _user.HasRole("Editor");

// Avoid: Database query in authorization
public bool CanUpdate()
{
    return _context.Users
        .Any(u => u.Id == _user.Id && u.CanEdit);  // Slow!
}
```

### Consistent Authorization Experience

Match Can* results with operation behavior:

```csharp
// If CanUpdate returns true, Update should not fail due to authorization
var canUpdate = _factory.CanUpdate();
if (canUpdate.HasAccess)
{
    // This should succeed (authorization-wise)
    await _factory.Save(person);
}
```

### Use Meaningful Messages

Provide helpful feedback to users:

```csharp
public Authorized CanDelete()
{
    if (!_user.IsAuthenticated)
    {
        return new Authorized(false, "Please log in to delete records");
    }

    if (!_user.HasRole("Admin"))
    {
        return new Authorized(false,
            "Only administrators can delete records. " +
            "Contact your admin for assistance.");
    }

    return new Authorized(true);
}
```

### Test Authorization

Unit test your authorization logic:

```csharp
[Fact]
public void CanUpdate_RequiresEditorRole()
{
    var user = new MockUser { Roles = ["Viewer"] };
    var auth = new PersonModelAuth(user);

    var result = auth.CanUpdate();

    Assert.False(result.HasAccess);
}

[Fact]
public void CanUpdate_AllowsEditors()
{
    var user = new MockUser { Roles = ["Editor"] };
    var auth = new PersonModelAuth(user);

    var result = auth.CanUpdate();

    Assert.True(result.HasAccess);
}
```

## Next Steps

- **[Authorization Overview](authorization-overview.md)**: Understanding authorization patterns
- **[Custom Authorization](custom-authorization.md)**: Using [AuthorizeFactory<T>]
- **[ASP.NET Core Authorization](asp-authorize.md)**: Policy-based authorization
