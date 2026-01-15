---
layout: default
title: "Migrating to Client-Server"
description: "Guide for migrating existing single-project applications to client-server separation"
parent: Concepts
nav_order: 11
---

# Migrating to Client-Server Separation

This guide walks through migrating an existing single-project RemoteFactory application to the client-server separation pattern.

## When to Migrate

**Consider migrating when:**
- Client assembly size is a concern (e.g., Blazor WASM download time)
- You want to protect business logic from reverse engineering
- Database types or schema hints should not be on the client
- You need clear separation between client and server responsibilities

**Keep single-project when:**
- Using Blazor Server (client and server share the same process)
- Assembly size isn't a concern
- You don't need to hide implementation details
- Simplicity is more valuable than separation

## Pre-Migration Assessment

Before migrating, analyze your codebase:

### 1. Identify Remote Operations

Find all methods with `[Remote]` attributes - these will need `#if CLIENT` / `#else` sections:

<!-- pseudo:migration-identify-remote -->
```csharp
// Look for patterns like:
[Remote, Create]
public void Create([Service] IChildFactory factory) { ... }

[Remote, Fetch]
public async Task Fetch(Guid id, [Service] IDbContext db) { ... }
```

### 2. Review Service Parameters

Methods with `[Service]` parameters need special handling - the client version won't have these:

| Server Method | Client Placeholder |
|---------------|-------------------|
| `Create([Service] IFactory f)` | `Create()` |
| `Fetch(Guid id, [Service] IDb db)` | `Fetch(Guid id)` |

### 3. List Server-Only Dependencies

Identify types that should NOT be on the client:
- Database contexts (`DbContext`, EF entities)
- Server infrastructure (file system, email services)
- Child factories with server dependencies

## Migration Steps

### Step 1: Create Project Structure

Create three projects from your existing domain:

<!-- pseudo:migration-project-structure -->
```
Before:
/MyDomain/
  MyDomain.csproj
  Order.cs
  OrderLine.cs

After:
/MyDomain/                    # Shared source files (no .csproj)
  Order.cs                    # Modified with #if CLIENT
  OrderLine.cs
  IOrder.cs
  IOrderLine.cs

/MyDomain.Client/             # Client assembly
  MyDomain.Client.csproj
  AssemblyAttributes.cs       # [assembly: FactoryMode(RemoteOnly)]

/MyDomain.Server/             # Server assembly
  MyDomain.Server.csproj
```

### Step 2: Configure Client Project

Create the client project with the `CLIENT` constant:

<!-- pseudo:migration-client-csproj -->
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <!-- Define CLIENT constant for conditional compilation -->
    <DefineConstants>$(DefineConstants);CLIENT</DefineConstants>
  </PropertyGroup>

  <!-- Link to shared source files -->
  <ItemGroup>
    <Compile Include="..\MyDomain\*.cs" Link="%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Neatoo.RemoteFactory" Version="10.*" />
  </ItemGroup>
</Project>
```

Add the FactoryMode attribute:

<!-- pseudo:migration-assembly-attributes -->
```csharp
// AssemblyAttributes.cs
using Neatoo.RemoteFactory;

[assembly: FactoryMode(FactoryMode.RemoteOnly)]
```

### Step 3: Configure Server Project

Create the server project (no CLIENT constant):

<!-- pseudo:migration-server-csproj -->
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <!-- No CLIENT constant - full implementations -->
  </PropertyGroup>

  <!-- Link to same shared source files -->
  <ItemGroup>
    <Compile Include="..\MyDomain\*.cs" Link="%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Neatoo.RemoteFactory" Version="10.*" />
    <!-- Server can reference database layer -->
    <ProjectReference Include="..\MyDomain.Ef\MyDomain.Ef.csproj" />
  </ItemGroup>
</Project>
```

### Step 4: Convert Entity Methods

Transform each `[Remote]` method using conditional compilation:

**Before (single project):**

<!-- pseudo:migration-before-entity -->
```csharp
[Factory]
public class Order : IOrder, IFactorySaveMeta
{
    public Guid Id { get; set; }
    public string? CustomerName { get; set; }
    public IOrderLineList Lines { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

    [Remote, Create]
    public void Create([Service] IOrderLineListFactory lineListFactory)
    {
        Id = Guid.NewGuid();
        Lines = lineListFactory.Create();
    }

    [Remote, Fetch]
    public async Task Fetch(Guid id, [Service] IDbContext db, [Service] IOrderLineListFactory lineListFactory)
    {
        var entity = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == id);
        Id = entity.Id;
        CustomerName = entity.CustomerName;
        Lines = lineListFactory.Fetch(entity.Lines);
        IsNew = false;
    }
}
```

**After (client-server):**

<!-- pseudo:migration-after-entity -->
```csharp
[Factory]
public class Order : IOrder, IFactorySaveMeta
{
    // Shared properties - same as before
    public Guid Id { get; set; }
    public string? CustomerName { get; set; }
    public IOrderLineList Lines { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

#if CLIENT
    // Client placeholders - throw if called directly
    [Remote, Create]
    public void Create()
    {
        throw new InvalidOperationException("Call through IOrderFactory.Create()");
    }

    [Remote, Fetch]
    public Task Fetch(Guid id)
    {
        throw new InvalidOperationException("Call through IOrderFactory.Fetch()");
    }
#else
    // Server implementations - full logic with services
    [Remote, Create]
    public void Create([Service] IOrderLineListFactory lineListFactory)
    {
        Id = Guid.NewGuid();
        Lines = lineListFactory.Create();
    }

    [Remote, Fetch]
    public async Task Fetch(Guid id, [Service] IDbContext db, [Service] IOrderLineListFactory lineListFactory)
    {
        var entity = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == id);
        Id = entity.Id;
        CustomerName = entity.CustomerName;
        Lines = lineListFactory.Fetch(entity.Lines);
        IsNew = false;
    }
#endif
}
```

### Step 5: Handle Simple Child Entities

Child entities without server dependencies can use local `[Create]`:

<!-- pseudo:migration-simple-child -->
```csharp
[Factory]
public class OrderLine : IOrderLine
{
    public Guid Id { get; set; }
    public string? ProductName { get; set; }

    // Local Create - works on both client and server (no #if needed)
    [Create]
    public OrderLine()
    {
        Id = Guid.NewGuid();
    }

#if !CLIENT
    // Server-only Fetch
    [Fetch]
    public void Fetch(OrderLineEntity entity)
    {
        Id = entity.Id;
        ProductName = entity.ProductName;
    }
#endif
}
```

### Step 6: Update DI Registration

**Client (Blazor WASM):**

<!-- pseudo:migration-client-di -->
```csharp
// Program.cs
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,
    typeof(IOrder).Assembly);  // Client assembly

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
```

**Server (ASP.NET Core):**

<!-- pseudo:migration-server-di -->
```csharp
// Program.cs
builder.Services.AddNeatooAspNetCore(
    typeof(Order).Assembly);  // Server assembly

builder.Services.AddDbContext<MyDbContext>();

// Map the factory endpoint
app.UseNeatoo();
```

## Verification Checklist

After migration, verify:

- [ ] **Client compiles without server dependencies**
  ```bash
  dotnet build MyDomain.Client.csproj
  # Should NOT reference EF, database types, etc.
  ```

- [ ] **Server compiles with all dependencies**
  ```bash
  dotnet build MyDomain.Server.csproj
  ```

- [ ] **Factory interfaces are identical**
  - Both assemblies generate the same `IOrderFactory` interface
  - Method signatures match (excluding `[Service]` parameters)

- [ ] **End-to-end operations work**
  - Create → entity initialized with children
  - Fetch → data loaded from database
  - Save → changes persisted

## Common Pitfalls

### 1. Service Parameter Signature Mismatch

**Problem:** Client placeholder has different non-service parameters than server.

<!-- invalid:migration-signature-mismatch -->
```csharp
#if CLIENT
    [Remote, Fetch]
    public Task Fetch(Guid id) { ... }  // Missing 'includeDeleted' parameter
#else
    [Remote, Fetch]
    public Task Fetch(Guid id, bool includeDeleted, [Service] IDb db) { ... }
#endif
```

**Solution:** Non-service parameters must match exactly:

<!-- pseudo:migration-signature-correct -->
```csharp
#if CLIENT
    [Remote, Fetch]
    public Task Fetch(Guid id, bool includeDeleted) { ... }
#else
    [Remote, Fetch]
    public Task Fetch(Guid id, bool includeDeleted, [Service] IDb db) { ... }
#endif
```

### 2. Missing FactoryMode Attribute

**Problem:** Client generates Full mode factories (includes local methods).

**Solution:** Add to client assembly:

<!-- pseudo:migration-factorymode-fix -->
```csharp
[assembly: FactoryMode(FactoryMode.RemoteOnly)]
```

### 3. Local Create Needing Server Dependencies

**Problem:** `[Create]` method needs child factory that only exists on server.

<!-- invalid:migration-local-create-problem -->
```csharp
// This won't work - Create is local but needs server-only factory
[Create]
public void Create([Service] IOrderLineListFactory lineListFactory)
{
    Lines = lineListFactory.Create();
}
```

**Solution:** Make it `[Remote, Create]` with client placeholder:

<!-- pseudo:migration-local-create-fix -->
```csharp
#if CLIENT
    [Remote, Create]
    public void Create() { throw new InvalidOperationException(); }
#else
    [Remote, Create]
    public void Create([Service] IOrderLineListFactory lineListFactory)
    {
        Lines = lineListFactory.Create();
    }
#endif
```

### 4. Forgetting to Remove Server Types from Client

**Problem:** Client code still references database types.

**Solution:** Move all server-type usage inside `#if !CLIENT` blocks.

## Example Project

See the complete OrderEntry example in the repository:

```
src/Examples/OrderEntry/
├── OrderEntry.Domain/              # Shared source files
├── OrderEntry.Domain.Client/       # RemoteOnly factories
├── OrderEntry.Domain.Server/       # Full factories
├── OrderEntry.Ef/                  # Database context
├── OrderEntry.Server/              # ASP.NET Core API
└── OrderEntry.BlazorClient/        # Blazor WASM app
```

## See Also

- [Client-Server Separation](client-server-separation.md) - Architecture overview
- [Client-Server Setup](../getting-started/client-server-setup.md) - Step-by-step implementation guide
- [Service Injection](service-injection.md) - `[Service]` parameter details
- [Generated Code Reference](../reference/generated-code.md) - How RemoteOnly mode affects generated code
