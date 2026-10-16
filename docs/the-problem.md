# Why RemoteFactory

## The Goal

Write your domain model as if it's a single layer. Don't think about client vs server, serialization, HTTP endpoints, or service resolution differences between tiers. Put the behavior where it belongs — on the domain object — and let the infrastructure carry it across the wire.

That's what RemoteFactory does. When the library that defines your objects is loaded on both the client and the server, the seam between them needs no DTOs and no controllers: the objects themselves cross, and their operations run on the tier that has what they need. Persistence routing (Create, Fetch, then one `Save()` that runs Insert, Update or Delete) is one use of that, and so are read models, commands and the services a client calls.

## The Traditional Problem

In a typical 3-tier .NET application, getting an entity from the client to the database requires layers of infrastructure:

- **DTOs** to serialize state across the wire
- **API controllers** to expose each operation as an endpoint
- **Factories or mapping code** to convert between DTOs and domain objects
- **Different service wiring** on client vs server

Every property exists in multiple places. Every new field means changes in three or four files. The domain model — the thing that matters — gets buried under plumbing.

## RemoteFactory's Approach

RemoteFactory eliminates those layers by generating them at compile time from attributes on your domain classes. But the real value isn't the code generation — that's just the mechanism. The value is that you write your domain model as a single-layer design, and RemoteFactory takes care of getting each operation to the tier that runs it, with that tier's services.

Consider what happens when you call `Save()` on an entity:

**WRONG: the traditional approach** — what you write without RemoteFactory, and what it replaces:
```csharp
// Controller
[HttpPost] public async Task<ActionResult<OrderDto>> Save(OrderDto dto) { ... }
[HttpPut]  public async Task<ActionResult<OrderDto>> Update(OrderDto dto) { ... }
[HttpDelete] public async Task Delete(int id) { ... }

// Mapping
var entity = MapFromDto(dto);
// ... persist ...
return MapToDto(entity);

// Factory
public Order CreateFromDto(OrderDto dto) { ... }
```

**With RemoteFactory** — you write the operations on the object itself:

<!-- snippet: readme-domain-model -->
<a id='snippet-readme-domain-model'></a>
```cs
/// <summary>
/// Creates a new Order. Local: runs on the tier that calls it.
/// </summary>
[Create]
public void Create(
    string customerName,
    [Service] IOrderLineListFactory lineListFactory)
{
    CustomerName = customerName;
    Lines = lineListFactory.Create();
    Status = OrderStatus.Draft;
    IsNew = true;
}

/// <summary>
/// Loads an existing Order and its lines; false when there is none.
/// </summary>
[Remote, Fetch]
internal async Task<bool> Fetch(
    int id,
    [Service] IOrderDbContext db,
    [Service] IOrderLineListFactory lineListFactory)
{
    var row = await db.FindAsync(id);
    if (row == null)
        return false;

    LoadFrom(row, lineListFactory);
    return true;
}

/// <summary>
/// Inserts a new Order and its lines.
/// </summary>
[Remote, Insert]
internal async Task Insert(
    [Service] IOrderDbContext db,
    [Service] IOrderLineListFactory lineListFactory)
{
    var row = new OrderRow();
    MapTo(row);
    db.Add(row);
    lineListFactory.Save(Lines, row.Lines);
    await db.SaveChangesAsync();
    Id = row.Id;
}

/// <summary>
/// Updates an existing Order and its lines.
/// </summary>
[Remote, Update]
internal async Task Update(
    [Service] IOrderDbContext db,
    [Service] IOrderLineListFactory lineListFactory)
{
    var row = await db.FindAsync(Id)
        ?? throw new KeyNotFoundException($"Order {Id} not found");
    MapTo(row);
    lineListFactory.Save(Lines, row.Lines);
    await db.SaveChangesAsync();
}

/// <summary>
/// Deletes an Order. Its line rows go with its row.
/// </summary>
[Remote, Delete]
internal async Task Delete([Service] IOrderDbContext db)
{
    var row = await db.FindAsync(Id)
        ?? throw new KeyNotFoundException($"Order {Id} not found");
    db.Remove(row);
    await db.SaveChangesAsync();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L234-L318' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-domain-model' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The caller calls `order = await orderFactory.Save(order)`. RemoteFactory picks Insert, Update, or Delete from the object's own state (`IsNew`, `IsDeleted`), serializes the object to the server, resolves the store from server-side DI, and calls your method. No DTOs, no controllers, no mapping.

## What Gets Generated

RemoteFactory generates everything between your domain class and the wire:

```
Your Code                    Generated
─────────                    ─────────
Domain Model + Attributes →  Factory interfaces + implementations
                             Serialization handling
                             HTTP endpoint routing
                             Service resolution per side
```

The domain model is the single source of truth. Changes happen in one place.

## Coming from CSLA?

If you know CSLA, RemoteFactory is the DataPortal pattern implemented through source generation instead of runtime reflection. Same concept — domain objects that carry their own operations across the client/server boundary — but resolved at compile time.

## Who Benefits

RemoteFactory is designed for any .NET client that needs to call a server:

- **Blazor WebAssembly** with ASP.NET Core backends
- **MAUI** applications with server-side persistence
- **WPF, console, or any .NET client** that calls a .NET server
- **Domain-driven designs** where entities have behavior, not just data
- **Teams tired of maintaining** DTO layers and mapping code

The pattern is the same regardless of client technology: your domain class declares its operations, and RemoteFactory carries them to the tier that runs them.

## Next Steps

- [Getting Started](getting-started.md) — Build your first RemoteFactory application
- [Factory Operations](factory-operations.md) — Create, Fetch, Insert, Update, Delete and Execute
- [Client-Server Architecture](client-server-architecture.md) — How the client/server split works
