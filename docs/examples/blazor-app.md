---
layout: default
title: "Blazor Application"
description: "Complete Blazor WebAssembly example with RemoteFactory"
parent: Examples
nav_order: 1
---

# Blazor Application Example

This guide walks through a complete Blazor WebAssembly application using RemoteFactory, based on the Person Demo in the repository.

## Project Structure

```
PersonDemo/
├── Person.DomainModel/         # Shared domain models
│   ├── IPersonModel.cs
│   ├── PersonModel.cs
│   ├── PersonModelAuth.cs
│   └── User.cs
├── Person.Ef/                  # Entity Framework Core
│   ├── PersonEntity.cs
│   └── PersonContext.cs
├── Person.Server/              # ASP.NET Core server
│   └── Program.cs
└── PersonApp/                  # Blazor WebAssembly client
    ├── Program.cs
    └── Pages/Home.razor
```

## Domain Model Project

### IPersonModel.cs

Define the interface for your domain model:

```csharp
using Neatoo.RemoteFactory;
using System.ComponentModel;

namespace Person.DomainModel;

public interface IPersonModel : INotifyPropertyChanged, IFactorySaveMeta
{
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    string? Phone { get; set; }
    string? Notes { get; set; }
    DateTime Created { get; }
    DateTime Modified { get; }
    new bool IsDeleted { get; set; }
}
```

### PersonModel.cs

The main domain model with factory operations:

```csharp
using Microsoft.EntityFrameworkCore;
using Neatoo.RemoteFactory;
using Person.Ef;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Person.DomainModel;

[Factory]
[AuthorizeFactory<IPersonModelAuth>]
internal partial class PersonModel : IPersonModel
{
    [Create]
    public PersonModel()
    {
        this.Created = DateTime.Now;
        this.Modified = DateTime.Now;
    }

    [Required(ErrorMessage = "First Name is required")]
    public string? FirstName { get; set { field = value; OnPropertyChanged(); } }

    [Required(ErrorMessage = "Last Name is required")]
    public string? LastName { get; set { field = value; OnPropertyChanged(); } }

    public string? Email { get; set { field = value; OnPropertyChanged(); } }
    public string? Phone { get; set { field = value; OnPropertyChanged(); } }
    public string? Notes { get; set { field = value; OnPropertyChanged(); } }
    public DateTime Created { get; set { field = value; OnPropertyChanged(); } }
    public DateTime Modified { get; set { field = value; OnPropertyChanged(); } }
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Mapper method declarations
    public partial void MapFrom(PersonEntity personEntity);
    public partial void MapTo(PersonEntity personEntity);

    [Remote]
    [Fetch]
    public async Task<bool> Fetch([Service] IPersonContext personContext)
    {
        var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == 1);
        if (personEntity == null)
        {
            return false;
        }
        this.MapFrom(personEntity);
        this.IsNew = false;
        return true;
    }

    [Remote]
    [Update]
    [Insert]
    public async Task Upsert([Service] IPersonContext personContext)
    {
        var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == 1);
        if (personEntity == null)
        {
            personEntity = new PersonEntity();
            personContext.Persons.Add(personEntity);
        }
        this.Modified = DateTime.Now;
        this.MapTo(personEntity);
        await personContext.SaveChangesAsync();
    }

    [Remote]
    [Delete]
    public async Task Delete([Service] IPersonContext personContext)
    {
        var personEntity = await personContext.Persons.FirstOrDefaultAsync(x => x.Id == 1);
        if (personEntity != null)
        {
            personContext.Persons.Remove(personEntity);
            await personContext.SaveChangesAsync();
        }
    }
}
```

### PersonModelAuth.cs

Authorization rules for the domain model:

```csharp
using Neatoo.RemoteFactory;

namespace Person.DomainModel;

public interface IPersonModelAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool CanAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    bool CanUpdate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}

internal class PersonModelAuth : IPersonModelAuth
{
    public PersonModelAuth(IUser user)
    {
        User = user;
    }

    public IUser User { get; }

    public bool CanAccess() => User.Role > Role.None;

    public bool CanCreate() => User.Role >= Role.Create;

    public bool CanFetch() => User.Role >= Role.Fetch;

    public bool CanUpdate() => User.Role >= Role.Update;

    public bool CanDelete() => User.Role >= Role.Delete;
}
```

### User.cs

Simple user/role abstraction:

```csharp
namespace Person.DomainModel;

public enum Role
{
    None = 0,
    Create = 1,
    Fetch = 2,
    Update = 3,
    Delete = 4
}

public interface IUser
{
    Role Role { get; set; }
}

public class User : IUser
{
    public Role Role { get; set; }
}
```

## Entity Framework Project

### PersonContext.cs

Database context and entity:

```csharp
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Person.Ef;

public interface IPersonContext
{
    DbSet<PersonEntity> Persons { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class PersonContext : DbContext, IPersonContext
{
    public virtual DbSet<PersonEntity> Persons { get; set; } = null!;

    public string DbPath { get; }

    public PersonContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "Person.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={DbPath}");
}

public class PersonEntity
{
    [Key]
    public int Id { get; set; } = 1;

    [Required]
    public string FirstName { get; set; } = null!;

    [Required]
    public string LastName { get; set; } = null!;

    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}
```

## Server Project

### Program.cs

ASP.NET Core server configuration:

```csharp
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using Person.DomainModel;
using Person.Ef;

var builder = WebApplication.CreateBuilder(args);

// CORS for Blazor client
builder.Services.AddCors();

// RemoteFactory server setup
builder.Services.AddNeatooAspNetCore(typeof(IPersonModel).Assembly);

// Application services
builder.Services.AddScoped<IPersonContext, PersonContext>();
builder.Services.RegisterMatchingName(typeof(IPersonModelAuth).Assembly);
builder.Services.AddScoped<IUser, User>();

var app = builder.Build();

// Enable CORS
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// RemoteFactory endpoint
app.UseNeatoo();

// Middleware to set user role from header (demo purposes)
app.Use((context, next) =>
{
    var role = context.Request.Headers["UserRoles"];
    var user = context.RequestServices.GetRequiredService<IUser>();
    user.Role = Role.None;
    if (!string.IsNullOrEmpty(role))
    {
        user.Role = Enum.Parse<Role>(role.ToString());
    }
    return next(context);
});

await app.RunAsync();
```

## Blazor Client Project

### Program.cs

Blazor WebAssembly client configuration:

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neatoo.RemoteFactory;
using Person.DomainModel;
using PersonApp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Standard HttpClient for Blazor
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// RemoteFactory client setup
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IPersonModel).Assembly);

// HTTP client for RemoteFactory (with role header for demo)
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    var user = sp.GetRequiredService<IUser>();
    var client = new HttpClient { BaseAddress = new Uri("http://localhost:5183/") };
    client.DefaultRequestHeaders.Add("UserRoles", user.Role.ToString());
    return client;
});

// Client-side user management
builder.Services.RegisterMatchingName(typeof(IPersonModelAuth).Assembly);
builder.Services.AddScoped<IUser, User>();

await builder.Build().RunAsync();
```

### Home.razor

Main Blazor page with factory usage:

```razor
@page "/"
@using Person.DomainModel
@inject IPersonModelFactory PersonFactory
@inject IUser User

<h1>Person Editor</h1>

<div class="mb-3">
    <label>Role:</label>
    <select @bind="SelectedRole" @bind:after="UpdateRole">
        <option value="None">None</option>
        <option value="Create">Create</option>
        <option value="Fetch">Fetch</option>
        <option value="Update">Update</option>
        <option value="Delete">Delete</option>
    </select>
</div>

<div class="mb-3">
    @if (PersonFactory.CanCreate().HasAccess)
    {
        <button class="btn btn-primary" @onclick="CreatePerson">Create New</button>
    }

    @if (PersonFactory.CanFetch().HasAccess)
    {
        <button class="btn btn-secondary" @onclick="FetchPerson">Load</button>
    }
</div>

@if (_person != null)
{
    <div class="card p-3">
        <div class="mb-2">
            <label>First Name:</label>
            <input class="form-control" @bind="_person.FirstName" />
        </div>

        <div class="mb-2">
            <label>Last Name:</label>
            <input class="form-control" @bind="_person.LastName" />
        </div>

        <div class="mb-2">
            <label>Email:</label>
            <input class="form-control" @bind="_person.Email" />
        </div>

        <div class="mb-2">
            <label>Phone:</label>
            <input class="form-control" @bind="_person.Phone" />
        </div>

        <div class="mb-2">
            <label>Notes:</label>
            <textarea class="form-control" @bind="_person.Notes"></textarea>
        </div>

        <div class="mb-2">
            <small>Created: @_person.Created | Modified: @_person.Modified</small>
        </div>

        <div class="mt-3">
            @if (PersonFactory.CanSave().HasAccess && !_person.IsDeleted)
            {
                <button class="btn btn-success" @onclick="SavePerson">Save</button>
            }

            @if (PersonFactory.CanDelete().HasAccess && !_person.IsNew)
            {
                <button class="btn btn-danger" @onclick="DeletePerson">Delete</button>
            }
        </div>
    </div>
}

@if (!string.IsNullOrEmpty(_message))
{
    <div class="alert @_alertClass mt-3">@_message</div>
}

@code {
    private IPersonModel? _person;
    private string? _message;
    private string _alertClass = "alert-info";
    private string SelectedRole { get; set; } = "None";

    private void UpdateRole()
    {
        User.Role = Enum.Parse<Role>(SelectedRole);
        StateHasChanged();
    }

    private void CreatePerson()
    {
        _person = PersonFactory.Create();
        _message = "New person created";
        _alertClass = "alert-info";
    }

    private async Task FetchPerson()
    {
        _person = await PersonFactory.Fetch();
        if (_person != null)
        {
            _message = "Person loaded successfully";
            _alertClass = "alert-success";
        }
        else
        {
            _message = "No person found";
            _alertClass = "alert-warning";
        }
    }

    private async Task SavePerson()
    {
        if (_person == null) return;

        var result = await PersonFactory.TrySave(_person);

        if (result.HasAccess)
        {
            _person = result.Result;
            _message = "Person saved successfully";
            _alertClass = "alert-success";
        }
        else
        {
            _message = $"Save failed: {result.Message}";
            _alertClass = "alert-danger";
        }
    }

    private async Task DeletePerson()
    {
        if (_person == null) return;

        _person.IsDeleted = true;
        var result = await PersonFactory.TrySave(_person);

        if (result.HasAccess)
        {
            _person = null;
            _message = "Person deleted successfully";
            _alertClass = "alert-success";
        }
        else
        {
            _person.IsDeleted = false;
            _message = $"Delete failed: {result.Message}";
            _alertClass = "alert-danger";
        }
    }
}
```

## Running the Example

### 1. Create Database

```bash
cd Person.Ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 2. Start the Server

```bash
cd Person.Server
dotnet run
# Server runs on http://localhost:5183
```

### 3. Start the Client

```bash
cd PersonApp
dotnet run
# Client runs on http://localhost:5000 or similar
```

### 4. Test the Application

1. Open the client in a browser
2. Select different roles to see authorization in action
3. Try Create, Fetch, Save, and Delete operations
4. Observe how buttons appear/disappear based on role

## Key Patterns Demonstrated

### Authorization-Aware UI

```razor
@if (PersonFactory.CanCreate().HasAccess)
{
    <button @onclick="CreatePerson">Create New</button>
}
```

### TrySave for Graceful Error Handling

```csharp
var result = await PersonFactory.TrySave(_person);
if (result.HasAccess)
{
    _person = result.Result;
    ShowSuccess("Saved!");
}
else
{
    ShowError(result.Message);
}
```

### Role Propagation via HTTP Header

```csharp
client.DefaultRequestHeaders.Add("UserRoles", user.Role.ToString());
```

### Property Change Notification

```csharp
public string? FirstName
{
    get;
    set { field = value; OnPropertyChanged(); }
}
```

## Next Steps

- **[WPF Application](wpf-app.md)**: WPF example with MVVM
- **[Common Patterns](common-patterns.md)**: Reusable patterns
- **[Authorization](../authorization/authorization-overview.md)**: Deep dive into authorization
