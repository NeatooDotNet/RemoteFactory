---
layout: default
title: "Client-Server Separation"
description: "Separating client and server code for minimal client assemblies"
parent: Concepts
nav_order: 10
---

# Client-Server Separation

Client-server separation allows you to create minimal client assemblies that exclude server-only code like database access and business logic implementations.

## When to Use This Pattern

**Use client-server separation when you need:**
- Smaller client assembly size
- Protection of business logic from reverse engineering
- No database types or schema hints on the client
- Clear separation between what runs where

**Use the simpler single-project approach when:**
- Client and server share the same codebase (e.g., Blazor Server)
- Assembly size isn't a concern
- You don't need to hide implementation details

## Architecture Overview

The pattern uses conditional compilation (`#if CLIENT`) to include different code for client and server builds from the same source files.

<!-- pseudo:client-server-project-structure -->
```
/Domain/
  Order.cs                     # Shared source with #if CLIENT / #else
  IOrder.cs                    # Interface (shared)

/Domain.Client/
  Domain.Client.csproj         # Defines CLIENT constant
  AssemblyAttributes.cs        # [assembly: FactoryMode(RemoteOnly)]

/Domain.Server/
  Domain.Server.csproj         # No CLIENT constant
  # Links to same Domain/*.cs files
  # References EF and other server dependencies
```

## The FactoryMode Attribute

The `[assembly: FactoryMode]` attribute controls how factories are generated:

| Mode | Constructor | Local Methods | Remote Methods | Use Case |
|------|-------------|---------------|----------------|----------|
| `Full` (default) | Both local and remote | Generated | Generated | Server, single-project |
| `RemoteOnly` | Remote only | Not generated | Generated | Client assemblies |

**Client assembly:**

<!-- pseudo:factory-mode-remote-only -->
```csharp
using Neatoo.RemoteFactory;

[assembly: FactoryMode(FactoryMode.RemoteOnly)]
```

## Entity Structure

Entities use `#if CLIENT` / `#else` to provide different implementations:

<!-- pseudo:entity-with-conditional-compilation -->
```csharp
[Factory]
internal class Order : IOrder
{
    // Shared - compiled by both client and server
    public Guid Id { get; set; }
    public string? CustomerName { get; set; }
    public IOrderLineList Lines { get; set; }

#if CLIENT
    // Client: Placeholder methods that throw if called directly
    // The factory generates remote HTTP stubs for these
    [Remote, Create]
    public void Create()
    {
        throw new InvalidOperationException("Call through IOrderFactory");
    }

    [Remote, Fetch]
    public Task Fetch(Guid id)
    {
        throw new InvalidOperationException("Call through IOrderFactory");
    }
#else
    // Server: Full implementations with [Service] parameters
    [Remote, Create]
    public void Create([Service] IOrderLineListFactory lineListFactory)
    {
        Id = Guid.NewGuid();
        Lines = lineListFactory.Create();
    }

    [Remote, Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IDbContext db,
        [Service] IOrderLineListFactory lineListFactory)
    {
        var entity = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == id);
        Id = entity.Id;
        CustomerName = entity.CustomerName;
        Lines = lineListFactory.Fetch(entity.Lines);
    }
#endif
}
```

## What Goes Where

| Component | Always Shared | Client (`#if CLIENT`) | Server (`#else`) |
|-----------|---------------|----------------------|------------------|
| Properties | ✓ | | |
| Validation rules | ✓ | | |
| `[Create]` (simple) | ✓ | | |
| `[Remote]` placeholders | | ✓ (throw) | |
| `[Remote]` implementations | | | ✓ |
| `[Service]` parameters | | | ✓ |
| Database types | | | ✓ |

## Project Configuration

### Client Project (.csproj)

<!-- pseudo:client-csproj -->
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- Define CLIENT constant for conditional compilation -->
    <DefineConstants>$(DefineConstants);CLIENT</DefineConstants>
  </PropertyGroup>

  <!-- Link to shared source files -->
  <ItemGroup>
    <Compile Include="..\Domain\*.cs" Link="%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <!-- Generator and core library only - no EF reference -->
    <ProjectReference Include="..\Generator.csproj" OutputItemType="Analyzer" />
    <ProjectReference Include="..\RemoteFactory.csproj" />
  </ItemGroup>
</Project>
```

### Server Project (.csproj)

<!-- pseudo:server-csproj -->
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- No CLIENT constant - full implementations -->
  
  <!-- Link to same shared source files -->
  <ItemGroup>
    <Compile Include="..\Domain\*.cs" Link="%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Generator.csproj" OutputItemType="Analyzer" />
    <ProjectReference Include="..\RemoteFactory.csproj" />
    <!-- Server references database layer -->
    <ProjectReference Include="..\Ef.csproj" />
  </ItemGroup>
</Project>
```

## Generated Factory Differences

### Client (RemoteOnly)

The client factory only has remote stubs:

<!-- pseudo:generated-client-factory -->
```csharp
internal class OrderFactory : IOrderFactory
{
    private readonly IMakeRemoteDelegateRequest MakeRemoteDelegateRequest;

    // Single constructor - remote only
    public OrderFactory(IServiceProvider sp, IMakeRemoteDelegateRequest remote, ...) 
    {
        MakeRemoteDelegateRequest = remote;
        CreateProperty = RemoteCreate;  // Always remote
    }

    public async Task<IOrder> RemoteCreate(CancellationToken ct)
    {
        return await MakeRemoteDelegateRequest.ForDelegate<IOrder>(...);
    }

    // No LocalCreate, LocalFetch, etc.
}
```

### Server (Full)

The server factory has both local implementations and remote capability:

<!-- pseudo:generated-server-factory -->
```csharp
internal class OrderFactory : IOrderFactory
{
    private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;

    // Constructor for local execution
    public OrderFactory(IServiceProvider sp, ...) 
    {
        CreateProperty = LocalCreate;
    }

    // Constructor for remote execution
    public OrderFactory(IServiceProvider sp, IMakeRemoteDelegateRequest remote, ...)
    {
        MakeRemoteDelegateRequest = remote;
        CreateProperty = RemoteCreate;
    }

    public Task<IOrder> LocalCreate(CancellationToken ct)
    {
        var target = ServiceProvider.GetRequiredService<Order>();
        var lineListFactory = ServiceProvider.GetRequiredService<IOrderLineListFactory>();
        return Task.FromResult(DoFactoryMethodCall(target, () => target.Create(lineListFactory)));
    }
}
```

## Child Entities

Child entities without server dependencies can use local `[Create]`:

<!-- pseudo:simple-child-entity -->
```csharp
[Factory]
internal class OrderLine : IOrderLine
{
    public Guid Id { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Local Create - works on both client and server
    // No #if CLIENT needed
    [Create]
    public OrderLine()
    {
        Id = Guid.NewGuid();
    }

#if !CLIENT
    // Server-only Fetch for loading from database
    [Fetch]
    public void Fetch(OrderLineEntity entity)
    {
        Id = entity.Id;
        ProductName = entity.ProductName;
        Quantity = entity.Quantity;
        UnitPrice = entity.UnitPrice;
    }
#endif
}
```

## Validation

Validation rules are defined in shared code (outside `#if`) and run on both client and server:

- **Client**: Real-time validation feedback as user types
- **Server**: Validation before persistence (defense in depth)

## Example Project

See the `OrderEntry` example in the repository for a complete implementation:

```
src/Examples/OrderEntry/
├── OrderEntry.Domain/              # Shared source files
├── OrderEntry.Domain.Client/       # CLIENT constant, RemoteOnly factories
├── OrderEntry.Domain.Server/       # Full factories with EF
├── OrderEntry.Ef/                  # Database context and entities
├── OrderEntry.Server/              # ASP.NET Core API
└── OrderEntry.BlazorClient/        # Blazor WASM app
```

## See Also

- [Factory Operations](factory-operations.md) - Operation attributes
- [Service Injection](service-injection.md) - `[Service]` parameter injection
- [Three-Tier Execution](three-tier-execution.md) - How remote calls work
