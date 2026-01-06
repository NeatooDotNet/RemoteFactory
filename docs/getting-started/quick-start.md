---
layout: default
title: "Quick Start"
description: "Build your first RemoteFactory application in 5 minutes"
parent: Getting Started
nav_order: 2
---

# Quick Start

Build a working 3-tier application with RemoteFactory in 5 minutes. This tutorial creates a simple Person management system with a Blazor WebAssembly client and ASP.NET Core server.

## What You'll Build

- A `PersonModel` domain class with Create, Fetch, and Save operations
- A generated `IPersonModelFactory` for all CRUD operations
- A Blazor WASM client that communicates with the server
- An ASP.NET Core server with Entity Framework Core

## Step 1: Create the Solution

```bash
# Create solution and projects
dotnet new sln -n PersonDemo
dotnet new classlib -n PersonDemo.DomainModel
dotnet new webapi -n PersonDemo.Server
dotnet new blazorwasm -n PersonDemo.Client

# Add projects to solution
dotnet sln add PersonDemo.DomainModel
dotnet sln add PersonDemo.Server
dotnet sln add PersonDemo.Client

# Add project references
dotnet add PersonDemo.Server reference PersonDemo.DomainModel
dotnet add PersonDemo.Client reference PersonDemo.DomainModel

# Add NuGet packages
dotnet add PersonDemo.DomainModel package Neatoo.RemoteFactory
dotnet add PersonDemo.Server package Neatoo.RemoteFactory.AspNetCore
dotnet add PersonDemo.Server package Microsoft.EntityFrameworkCore.Sqlite
```

## Step 2: Create the Domain Model

Create `PersonDemo.DomainModel/IPersonModel.cs` - the interface:

<!-- snippet: docs:getting-started/quick-start:person-interface -->
```csharp
public interface IPersonModel : IFactorySaveMeta
{
    int Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    new bool IsNew { get; set; }
    new bool IsDeleted { get; set; }
}
```
<!-- /snippet -->

Create `PersonDemo.DomainModel/PersonModel.cs` - the implementation:

<!-- snippet: docs:getting-started/quick-start:person-model-full -->
```csharp
[Factory]
public class PersonModel : IPersonModel
{
    #region docs:concepts/factory-operations:create-constructor
    [Create]
    public PersonModel()
    {
        IsNew = true;
    }
```
<!-- /snippet -->

## Step 3: Create the Entity and DbContext

Create `PersonDemo.DomainModel/PersonEntity.cs`:

**Entity:**

<!-- snippet: docs:getting-started/quick-start:person-entity -->
```csharp
public class PersonEntity
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}
```
<!-- /snippet -->

**Context Interface:**

<!-- snippet: docs:getting-started/quick-start:person-context -->
```csharp
public interface IPersonContext
{
    DbSet<PersonEntity> Persons { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```
<!-- /snippet -->

**Context Implementation:**

```csharp
public class PersonContext : DbContext, IPersonContext
{
    public DbSet<PersonEntity> Persons { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var path = Path.Join(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), "persons.db");
        options.UseSqlite($"Data Source={path}");
    }
}
```

## Step 4: Configure the Server

Replace `PersonDemo.Server/Program.cs`:

```csharp
using Neatoo.RemoteFactory.AspNetCore;
using PersonDemo.DomainModel;

var builder = WebApplication.CreateBuilder(args);

// Add CORS for Blazor client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add RemoteFactory services - pass the domain model assembly
builder.Services.AddNeatooAspNetCore(typeof(IPersonModel).Assembly);

// Register the EF Core context
builder.Services.AddScoped<IPersonContext, PersonContext>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IPersonContext>() as PersonContext;
    context?.Database.EnsureCreated();
}

app.UseCors();

// Add the RemoteFactory endpoint at /api/neatoo
app.UseNeatoo();

app.Run();
```

## Step 5: Configure the Client

Replace `PersonDemo.Client/Program.cs`:

```csharp
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neatoo.RemoteFactory;
using PersonDemo.DomainModel;
using PersonDemo.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add RemoteFactory in Remote mode
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IPersonModel).Assembly);

// Configure HTTP client for remote calls
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    // Update this URL to match your server
    return new HttpClient { BaseAddress = new Uri("https://localhost:5001/") };
});

await builder.Build().RunAsync();
```

## Step 6: Create a Blazor Component

Create `PersonDemo.Client/Pages/PersonEditor.razor`:

```razor
@page "/person"
@using PersonDemo.DomainModel
@inject IPersonModelFactory PersonFactory

<h3>Person Editor</h3>

@if (_person == null)
{
    <button @onclick="CreateNew">Create New Person</button>
    <button @onclick="LoadPerson">Load Person (ID: 1)</button>
}
else
{
    <div>
        <label>First Name:</label>
        <input @bind="_person.FirstName" />
    </div>
    <div>
        <label>Last Name:</label>
        <input @bind="_person.LastName" />
    </div>
    <div>
        <label>Email:</label>
        <input @bind="_person.Email" />
    </div>
    <div>
        <button @onclick="SavePerson">Save</button>
        <button @onclick="Cancel">Cancel</button>
    </div>

    @if (!string.IsNullOrEmpty(_message))
    {
        <p>@_message</p>
    }
}

@code {
    private IPersonModel? _person;
    private string? _message;

    private void CreateNew()
    {
        // Uses the generated Create() method
        _person = PersonFactory.Create();
        _message = "New person created";
    }

    private async Task LoadPerson()
    {
        // Uses the generated Fetch() method - calls server
        _person = await PersonFactory.Fetch(1);
        _message = _person != null ? "Person loaded" : "Person not found";
    }

    private async Task SavePerson()
    {
        if (_person == null) return;

        // Uses the generated Save() method - calls server
        var result = await PersonFactory.TrySave(_person);

        if (result.HasAccess)
        {
            _person = result.Result;
            _message = "Person saved successfully";
        }
        else
        {
            _message = $"Save failed: {result.Message}";
        }
    }

    private void Cancel()
    {
        _person = null;
        _message = null;
    }
}
```

## Step 7: Run the Application

1. Start the server:
   ```bash
   cd PersonDemo.Server
   dotnet run
   ```

2. In another terminal, start the client:
   ```bash
   cd PersonDemo.Client
   dotnet run
   ```

3. Open the Blazor app in your browser and navigate to `/person`

## What Just Happened?

When you built the solution, RemoteFactory's source generator analyzed your `PersonModel` class and generated:

### Generated Factory Interface

```csharp
public interface IPersonModelFactory
{
    IPersonModel? Create();
    Task<IPersonModel?> Fetch(int id);
    Task<IPersonModel?> Save(IPersonModel target);
    Task<Authorized<IPersonModel>> TrySave(IPersonModel target);
}
```

### Generated Factory Implementation

The implementation includes:
- **Local methods** that execute on the server
- **Remote methods** that serialize calls over HTTP
- **Delegate registration** for the remote invocation system

## Key Takeaways

1. **One attribute, complete factory**: The `[Factory]` attribute generates everything
2. **No DTOs**: Your domain model serializes directly
3. **No controller code**: The single `/api/neatoo` endpoint handles all operations
4. **Server-only services**: `[Service]` parameters are resolved on the server
5. **Remote execution**: `[Remote]` methods execute on the server, results return to client

## Next Steps

- **[Architecture Overview](../concepts/architecture-overview.md)**: Understand the full data flow
- **[Factory Operations](../concepts/factory-operations.md)**: Deep dive into Create, Fetch, Insert, Update, Delete
- **[Authorization](../authorization/authorization-overview.md)**: Add access control to your factories
- **[Attributes Reference](../reference/attributes.md)**: Complete attribute documentation
