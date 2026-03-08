# Why RemoteFactory

## The Goal

Write your domain model as if it's a single layer. Don't think about client vs server, serialization, HTTP endpoints, or service resolution differences between tiers. Write the persistence logic where it belongs — on the domain object — and let the infrastructure figure out how to get there.

That's what RemoteFactory does. It's a persistence routing engine: it routes your domain objects to the right method for each stage of their persistence lifecycle — creation, loading, saving, deletion — and handles the client/server split behind the scenes.

## The Traditional Problem

In a typical 3-tier .NET application, getting an entity from the client to the database requires layers of infrastructure:

- **DTOs** to serialize state across the wire
- **API controllers** to expose each operation as an endpoint
- **Factories or mapping code** to convert between DTOs and domain objects
- **Different service wiring** on client vs server

Every property exists in multiple places. Every new field means changes in three or four files. The domain model — the thing that matters — gets buried under plumbing.

## RemoteFactory's Approach

RemoteFactory eliminates those layers by generating them at compile time from attributes on your domain classes. But the real value isn't the code generation — that's just the mechanism. The value is that you write your domain model as a single-layer design, and RemoteFactory takes care of routing each operation to the right place with the right services.

Consider what happens when you call `Save()` on an entity:

**Traditional approach** — you write all of this:
```csharp
// Controller
[HttpPost] public async Task<ActionResult<EmployeeDto>> Save(EmployeeDto dto) { ... }
[HttpPut]  public async Task<ActionResult<EmployeeDto>> Update(EmployeeDto dto) { ... }
[HttpDelete] public async Task Delete(Guid id) { ... }

// Mapping
var entity = MapFromDto(dto);
// ... persist ...
return MapToDto(entity);

// Factory
public Employee CreateFromDto(EmployeeDto dto) { ... }
```

**With RemoteFactory** — you write just the persistence logic:
```csharp
[Factory]
public partial class Employee
{
    [Remote, Insert]
    internal async Task Insert([Service] IEmployeeRepository repo)
    {
        await repo.AddAsync(this);
    }

    [Remote, Update]
    internal async Task Update([Service] IEmployeeRepository repo)
    {
        await repo.UpdateAsync(this);
    }

    [Remote, Delete]
    internal async Task Delete([Service] IEmployeeRepository repo)
    {
        await repo.DeleteAsync(Id);
    }
}
```

The caller just calls `factory.Save(employee)`. RemoteFactory routes to Insert, Update, or Delete based on entity metadata (`IsNew`, `IsDeleted`), serializes the object to the server, resolves the repository from server-side DI, and calls your method. No DTOs, no controllers, no mapping.

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

If you know CSLA, RemoteFactory is the DataPortal pattern implemented through source generation instead of runtime reflection. Same concept — domain objects that know how to persist themselves — but resolved at compile time.

## Who Benefits

RemoteFactory is designed for any .NET client that needs to call a server:

- **Blazor WebAssembly** with ASP.NET Core backends
- **MAUI** applications with server-side persistence
- **WPF, console, or any .NET client** that calls a .NET server
- **Domain-driven designs** where entities have behavior, not just data
- **Teams tired of maintaining** DTO layers and mapping code

The pattern is the same regardless of client technology: your domain class declares its persistence operations, and RemoteFactory handles the rest.

## Next Steps

- [Getting Started](getting-started.md) — Build your first RemoteFactory application
- [Factory Operations](factory-operations.md) — All seven persistence operations
- [Client-Server Architecture](client-server-architecture.md) — How the client/server split works
