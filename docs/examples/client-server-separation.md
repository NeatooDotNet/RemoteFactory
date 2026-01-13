---
layout: default
title: "Client-Server Separation"
description: "Example demonstrating FactoryMode.RemoteOnly for lean client assemblies"
parent: Examples
nav_order: 3
---

# Client-Server Separation Example

This example demonstrates using `FactoryMode.RemoteOnly` to create separate client and server assemblies from shared source files. The client assembly contains only remote HTTP stubs while the server assembly includes full local execution.

## Project Structure

```
OrderEntry/
├── OrderEntry.Domain/            # Shared source files (not compiled directly)
│   ├── IOrder.cs
│   ├── Order.cs
│   ├── IOrderLine.cs
│   ├── OrderLine.cs
│   ├── IOrderLineList.cs
│   └── OrderLineList.cs
├── OrderEntry.Domain.Client/     # Client assembly (FactoryMode.RemoteOnly)
│   ├── OrderEntry.Domain.Client.csproj
│   └── AssemblyAttributes.cs
├── OrderEntry.Domain.Server/     # Server assembly (FactoryMode.Full)
│   └── OrderEntry.Domain.Server.csproj
├── OrderEntry.Ef/                # Entity Framework (server only)
│   ├── OrderEntryContext.cs
│   ├── OrderEntity.cs
│   └── OrderLineEntity.cs
├── OrderEntry.Server/            # ASP.NET Core server
│   └── Program.cs
└── OrderEntry.BlazorClient/      # Blazor WebAssembly client
    └── Pages/Home.razor
```

## Key Concept: Conditional Compilation

The shared source files use `#if CLIENT` directives to provide different implementations:

- **Client side**: Placeholder methods that throw (never called directly)
- **Server side**: Full implementations with database access

<!-- pseudo:conditional-compilation-example -->
```csharp
[Factory]
internal class Order : IOrder
{
    // Properties shared by both...

#if CLIENT
    [Remote, Create]
    public void Create()
    {
        throw new InvalidOperationException("Client should call through IOrderFactory");
    }
#else
    [Remote, Create]
    public void Create([Service] IOrderLineListFactory lineListFactory)
    {
        Id = Guid.NewGuid();
        OrderDate = DateTime.Now;
        Lines = lineListFactory.Create();
        IsNew = true;
    }
#endif
}
```

## Client Assembly Setup

### OrderEntry.Domain.Client.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- Define CLIENT symbol for conditional compilation -->
    <DefineConstants>$(DefineConstants);CLIENT</DefineConstants>
    <!-- Output generated factories for inspection -->
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>

  <!-- Link shared source files -->
  <ItemGroup>
    <Compile Include="..\OrderEntry.Domain\*.cs" Link="%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <!-- NO reference to OrderEntry.Ef - keeps client lean -->
    <ProjectReference Include="..\..\RemoteFactory\RemoteFactory.csproj" />
    <ProjectReference Include="..\..\Generator\Generator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>

</Project>
```

### AssemblyAttributes.cs

<!-- pseudo:assembly-attributes-remoteonly -->
```csharp
using Neatoo.RemoteFactory;

[assembly: FactoryMode(FactoryMode.RemoteOnly)]
```

The `FactoryMode.RemoteOnly` attribute tells the source generator to:
- Generate only the remote HTTP-calling constructor
- Skip local execution code paths
- Produce a smaller, leaner assembly

## Server Assembly Setup

### OrderEntry.Domain.Server.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <!-- No CLIENT symbol - gets full implementations -->
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>

  <!-- Link shared source files -->
  <ItemGroup>
    <Compile Include="..\OrderEntry.Domain\*.cs" Link="%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <!-- Server DOES reference EF for database access -->
    <ProjectReference Include="..\OrderEntry.Ef\OrderEntry.Ef.csproj" />
    <ProjectReference Include="..\..\RemoteFactory\RemoteFactory.csproj" />
    <ProjectReference Include="..\..\Generator\Generator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>

</Project>
```

No `FactoryModeAttribute` means `FactoryMode.Full` (default) - both local and remote constructors are generated.

## Domain Model with Conditional Compilation

### Order.cs

<!-- pseudo:order-domain-model -->
```csharp
using Neatoo.RemoteFactory;
#if !CLIENT
using Microsoft.EntityFrameworkCore;
using OrderEntry.Ef;
#endif

namespace OrderEntry.Domain;

[Factory]
internal class Order : IOrder
{
    public Guid Id { get; set; }
    public string? CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total => Lines?.Sum(l => l.LineTotal) ?? 0;
    public IOrderLineList Lines { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

#if CLIENT
    [Remote, Create]
    public void Create()
    {
        throw new InvalidOperationException("Client stub - not called directly");
    }

    [Remote, Fetch]
    public Task Fetch(Guid id)
    {
        throw new InvalidOperationException("Client stub - not called directly");
    }

    [Remote, Insert, Update]
    public Task Upsert()
    {
        throw new InvalidOperationException("Client stub - not called directly");
    }

    [Remote, Delete]
    public Task Delete()
    {
        throw new InvalidOperationException("Client stub - not called directly");
    }
#else
    [Remote, Create]
    public void Create([Service] IOrderLineListFactory lineListFactory)
    {
        Id = Guid.NewGuid();
        OrderDate = DateTime.Now;
        Lines = lineListFactory.Create();
        IsNew = true;
    }

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

    [Remote, Insert, Update]
    public async Task Upsert([Service] IOrderEntryContext db)
    {
        OrderEntity entity;
        if (IsNew)
        {
            entity = new OrderEntity { Id = Id };
            db.Orders.Add(entity);
        }
        else
        {
            entity = await db.Orders
                .Include(o => o.Lines)
                .FirstAsync(o => o.Id == Id);
        }

        entity.CustomerName = CustomerName;
        entity.OrderDate = OrderDate;
        // Sync lines...
        await db.SaveChangesAsync();
        IsNew = false;
    }

    [Remote, Delete]
    public async Task Delete([Service] IOrderEntryContext db)
    {
        var entity = await db.Orders.FindAsync(Id);
        if (entity != null)
        {
            db.Orders.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
#endif
}
```

### OrderLine.cs (Local Create, Server-Only Fetch)

Child entities often have local `[Create]` for adding new items on the client:

<!-- pseudo:orderline-local-create -->
```csharp
[Factory]
internal class OrderLine : IOrderLine
{
    public Guid Id { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;

    [Create]  // No [Remote] - executes locally on both client and server
    public OrderLine()
    {
        Id = Guid.NewGuid();
    }

#if !CLIENT
    [Fetch]  // Server-only - loads from entity
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

## Generated Factory Comparison

### Client Factory (RemoteOnly)

<!-- pseudo:generated-client-factory-example -->
```csharp
public partial class OrderFactory : IOrderFactory
{
    // Only remote constructor - no local execution
    public OrderFactory(
        IServiceProvider serviceProvider,
        IMakeRemoteDelegateRequest remoteMethodDelegate,
        IFactoryCore<IOrder> factoryCore)
    {
        // All operations call server via HTTP
        CreateProperty = async (ct) => await remoteMethodDelegate.ForDelegate<...>(...);
        FetchProperty = async (id, ct) => await remoteMethodDelegate.ForDelegate<...>(...);
        // ...
    }
}
```

### Server Factory (Full)

<!-- pseudo:generated-server-factory-example -->
```csharp
public partial class OrderFactory : IOrderFactory
{
    // Local constructor for server execution
    public OrderFactory(
        IServiceProvider serviceProvider,
        IFactoryCore<IOrder> factoryCore)
    {
        // Operations execute locally
        CreateProperty = async (ct) => {
            var target = factoryCore.CreateInstance();
            var lineListFactory = serviceProvider.GetRequiredService<IOrderLineListFactory>();
            target.Create(lineListFactory);
            return target;
        };
        // ...
    }

    // Remote constructor also available
    public OrderFactory(
        IServiceProvider serviceProvider,
        IMakeRemoteDelegateRequest remoteMethodDelegate,
        IFactoryCore<IOrder> factoryCore)
    {
        // For remote execution if needed
    }
}
```

## Benefits

| Aspect | Client Assembly | Server Assembly |
|--------|-----------------|-----------------|
| EF Core types | Not included | Included |
| Database code | Not included | Included |
| Factory constructors | Remote only | Local + Remote |
| Assembly size | Smaller | Larger |
| Security | No business logic exposed | Full implementation |

## Running the Example

### Start the Server

```bash
cd src/Examples/OrderEntry/OrderEntry.Server
dotnet run
```

### Start the Blazor Client

```bash
cd src/Examples/OrderEntry/OrderEntry.BlazorClient
dotnet run
```

## See Also

- **[Client-Server Separation Concept](../concepts/client-server-separation.md)**: Detailed explanation
- **[Client-Server Setup Guide](../getting-started/client-server-setup.md)**: Step-by-step setup
- **[FactoryMode Attribute](../reference/attributes.md#factorymode)**: Attribute reference
