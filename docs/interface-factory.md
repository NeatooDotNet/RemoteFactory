# Interface Factory

The Interface Factory pattern generates a client-side proxy for a server-only service. You define the contract as a C# interface with `[Factory]`, provide the implementation on the server, and RemoteFactory generates a proxy that serializes calls across the client/server boundary. Clients inject the interface from DI and call it normally — the proxy handles serialization and HTTP transport.

This pattern is for services without entity identity: query services, report generators, third-party API wrappers. If you need entity lifecycle management (Create, Fetch, Save), use a [Class Factory](factory-operations.md) instead. If you need stateless command delegates, use a [Static Factory](factory-operations.md#execute-operation).

See the [Decision Guide](decision-guide.md#which-factory-pattern) for a comparison of all three patterns.

## Complete Example

### 1. Define the interface

Place `[Factory]` on the interface. Methods need no operation attributes — every method on the interface is automatically a remote entry point.

```csharp
[Factory]
public interface IOrderQueryService
{
    Task<IReadOnlyList<OrderSummary>> GetAllAsync();
    Task<OrderSummary?> GetByIdAsync(int id);
    Task<int> CountAsync();
}
```

### 2. Implement on the server

The implementation is a plain class. It does **not** get `[Factory]` — only the interface has it.

```csharp
public class OrderQueryService : IOrderQueryService
{
    private readonly AppDbContext _db;

    public OrderQueryService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrderSummary>> GetAllAsync()
    {
        return await _db.Orders
            .Select(o => new OrderSummary { Id = o.Id, CustomerName = o.CustomerName })
            .ToListAsync();
    }

    public async Task<OrderSummary?> GetByIdAsync(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        return order == null ? null : new OrderSummary { Id = order.Id, CustomerName = order.CustomerName };
    }

    public async Task<int> CountAsync()
    {
        return await _db.Orders.CountAsync();
    }
}
```

### 3. Register in DI (server only)

```csharp
// Program.cs (server)
builder.Services.AddScoped<IOrderQueryService, OrderQueryService>();
```

Or use convention-based registration:
```csharp
builder.Services.RegisterMatchingName<IOrderQueryService>();  // Auto-finds OrderQueryService
```

### 4. Use from client code

Inject the interface and call it. The generated proxy handles the rest.

```csharp
@inject IOrderQueryService OrderQuery

@code {
    private IReadOnlyList<OrderSummary>? orders;

    protected override async Task OnInitializedAsync()
    {
        orders = await OrderQuery.GetAllAsync();
    }
}
```

## What the Generator Produces

For an interface with `[Factory]`, the generator creates:

1. **A proxy class** that implements the interface. Each method serializes its arguments, sends an HTTP POST to `/api/neatoo`, and deserializes the response.
2. **Delegate types** for each method on the interface.
3. **DI registrations** that wire the proxy in Remote mode and the real implementation in Server/Logical mode.

The client never sees the proxy directly — it injects `IOrderQueryService` from DI and gets the proxy automatically.

## Critical Rule: No Attributes on Interface Methods

Interface methods do **not** need `[Fetch]`, `[Create]`, `[Remote]`, or any other operation attribute. The `[Factory]` attribute on the interface itself is sufficient. Every method on the interface is automatically remote.

```csharp
// WRONG - causes duplicate generation
[Factory]
public interface IOrderQueryService
{
    [Fetch]  // Don't do this
    Task<OrderSummary?> GetByIdAsync(int id);
}

// RIGHT - no attributes on methods
[Factory]
public interface IOrderQueryService
{
    Task<OrderSummary?> GetByIdAsync(int id);
}
```

**Why it matters:** The generator treats every interface method as a remote entry point. Adding operation attributes creates duplicate registrations and generation conflicts.

## Anti-Pattern: [Factory] on the Implementation Class

Only the interface gets `[Factory]`. The implementation is a plain service class registered in DI.

```csharp
// WRONG - duplicate registration
[Factory]
public interface IOrderQueryService { ... }

[Factory]  // Don't do this
public class OrderQueryService : IOrderQueryService { ... }

// RIGHT - only interface has [Factory]
[Factory]
public interface IOrderQueryService { ... }

public class OrderQueryService : IOrderQueryService { ... }  // No [Factory]
```

**Why it matters:** The interface defines the factory contract. Adding `[Factory]` to the implementation creates a second, conflicting factory registration.

## Differences from Class Factory

| Aspect | Class Factory | Interface Factory |
|--------|---------------|-------------------|
| `[Factory]` goes on | The class (`internal partial class Order`) | The interface (`public interface IOrderQueryService`) |
| Operation attributes | Required (`[Create]`, `[Fetch]`, `[Remote]`, etc.) | Not used — all methods are implicitly remote |
| Entity state | Yes — properties serialized across boundary | No — request/response only |
| `IFactorySaveMeta` | Supported (Insert/Update/Delete routing) | Not applicable |
| `partial` keyword | Required on the class | Not required on the interface |
| Implementation | The `[Factory]` class IS the implementation | Separate implementation class, no `[Factory]` |
| Generated output | Factory that creates/manages instances | Proxy that forwards calls to server |

## When to Use Interface Factory

- **Query services** — Read-only data access that returns DTOs or projections
- **Third-party API wrappers** — Wrap external services behind a clean interface
- **Report generators** — Server-side computation exposed to the client
- **Any remote service without entity lifecycle** — If you don't need Create/Fetch/Save semantics, interface factory is the simpler choice

When you need entity identity, state management, and persistence routing, use a [Class Factory](factory-operations.md) instead.

## Next Steps

- [Decision Guide](decision-guide.md#which-factory-pattern) — Choosing between factory patterns
- [Factory Operations](factory-operations.md) — Class Factory operations reference
- [Client-Server Architecture](client-server-architecture.md) — How the proxy fits in the architecture
- [ASP.NET Core Integration](aspnetcore-integration.md) — Server setup for the `/api/neatoo` endpoint
