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

<!-- snippet: docs:concepts/client-server-separation:factory-mode-remote-only -->
```csharp
using Neatoo.RemoteFactory;

[assembly: FactoryMode(FactoryMode.RemoteOnly)]
```
<!-- /snippet -->

## Entity Structure

Entities use `#if CLIENT` / `#else` to provide different implementations:

<!-- snippet: docs:concepts/client-server-separation:order-entity -->
```csharp
/// <summary>
/// Order aggregate root with client-server separation.
/// Client: Placeholder methods (never called directly - factory handles remote calls).
/// Server: Full implementations with [Service] parameters including child factories.
/// </summary>
[Factory]
internal class Order : IOrder
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Customer name is required")]
    public string? CustomerName { get; set { field = value; OnPropertyChanged(); } }

    public DateTime OrderDate { get; set { field = value; OnPropertyChanged(); } }

    public decimal Total => Lines?.Sum(l => l.LineTotal) ?? 0;

    /// <summary>
    /// Child collection of order lines.
    /// </summary>
    public IOrderLineList Lines { get; set; } = null!;

    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

#if CLIENT
    // CLIENT: Placeholder methods - RemoteOnly factory generates remote stubs only
    // These method signatures define what the factory will generate.
    // The implementations throw if called directly (they shouldn't be).

    [Remote, Create]
    public void Create()
    {
        throw new InvalidOperationException("Client should call through IOrderFactory.Create()");
    }

    [Remote, Fetch]
    public Task Fetch(Guid id)
    {
        throw new InvalidOperationException("Client should call through IOrderFactory.Fetch()");
    }

    [Remote, Insert]
    public Task Insert()
    {
        throw new InvalidOperationException("Client should call through IOrderFactory.Save()");
    }

    [Remote, Update]
    public Task Update()
    {
        throw new InvalidOperationException("Client should call through IOrderFactory.Save()");
    }

    [Remote, Delete]
    public Task Delete()
    {
        throw new InvalidOperationException("Client should call through IOrderFactory.Save()");
    }

#else
    // SERVER: Full implementations with [Service] parameters

    /// <summary>
    /// Server-side Create with child factory injection.
    /// Creates a new order with an empty Lines collection.
    /// </summary>
    [Remote, Create]
    public void Create([Service] IOrderLineListFactory lineListFactory)
    {
        Id = Guid.NewGuid();
        OrderDate = DateTime.Now;
        Lines = lineListFactory.Create();
        IsNew = true;
    }

    /// <summary>
    /// Server-side Fetch with EF and child factory.
    /// Loads order and its lines from database.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IOrderEntryContext db,
        [Service] IOrderLineListFactory lineListFactory)
    {
        var entity = await db.Orders
            .Include(o => o.Lines)
            .FirstAsync(o => o.Id == id);

        Id = entity.Id;
        CustomerName = entity.CustomerName;
        OrderDate = entity.OrderDate;
        Lines = lineListFactory.Fetch(entity.Lines);
        IsNew = false;
    }

    /// <summary>
    /// Server-side Insert - persists new order and lines to database.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IOrderEntryContext db)
    {
        var entity = new OrderEntity
        {
            Id = Id,
            CustomerName = CustomerName!,
            OrderDate = OrderDate,
            Total = Total
        };

        // Add lines to entity
        foreach (var line in Lines)
        {
            entity.Lines.Add(new OrderLineEntity
            {
                Id = line.Id,
                OrderId = Id,
                ProductName = line.ProductName!,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice
            });
        }

        db.Orders.Add(entity);
        await db.SaveChangesAsync();
        IsNew = false;
    }

    /// <summary>
    /// Server-side Update - updates order and syncs lines.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IOrderEntryContext db)
    {
        var entity = await db.Orders
            .Include(o => o.Lines)
            .FirstAsync(o => o.Id == Id);

        entity.CustomerName = CustomerName!;
        entity.Total = Total;

        // Sync lines: remove deleted, update existing, add new
        var lineIds = Lines.Select(l => l.Id).ToHashSet();

        // Remove lines not in current collection
        var toRemove = entity.Lines.Where(e => !lineIds.Contains(e.Id)).ToList();
        foreach (var line in toRemove)
        {
            entity.Lines.Remove(line);
        }

        // Update existing and add new
        foreach (var line in Lines)
        {
            var existingLine = entity.Lines.FirstOrDefault(e => e.Id == line.Id);
            if (existingLine != null)
            {
                existingLine.ProductName = line.ProductName!;
                existingLine.Quantity = line.Quantity;
                existingLine.UnitPrice = line.UnitPrice;
            }
            else
            {
                entity.Lines.Add(new OrderLineEntity
                {
                    Id = line.Id,
                    OrderId = Id,
                    ProductName = line.ProductName!,
                    Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice
                });
            }
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Server-side Delete - removes order from database.
    /// Lines are cascade deleted by EF.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IOrderEntryContext db)
    {
        var entity = await db.Orders.FirstAsync(o => o.Id == Id);
        db.Orders.Remove(entity);
        await db.SaveChangesAsync();
    }
#endif
}
```
<!-- /snippet -->

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

<!-- snippet: docs:concepts/client-server-separation:simple-child-entity -->
```csharp
/// <summary>
/// Order line item entity.
/// Simple entity with local [Create] - runs on both client and server.
/// </summary>
[Factory]
internal class OrderLine : IOrderLine
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Product name is required")]
    public string? ProductName { get; set { field = value; OnPropertyChanged(); } }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); } } = 1;

    [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be positive")]
    public decimal UnitPrice { get; set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineTotal)); } }

    public decimal LineTotal => Quantity * UnitPrice;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Local Create - runs on both client and server.
    /// No [Remote] attribute because this doesn't need server-side services.
    /// </summary>
    [Create]
    public OrderLine()
    {
        Id = Guid.NewGuid();
    }

#if !CLIENT
    /// <summary>
    /// Server-only Fetch - loads line from EF entity.
    /// Called by OrderLineList.Fetch to populate children.
    /// </summary>
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
<!-- /snippet -->

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
