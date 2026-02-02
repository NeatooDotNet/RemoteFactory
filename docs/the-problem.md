# What Problem Does RemoteFactory Solve?

## The Shift

For years, .NET web applications followed a pattern: C# on the server, JavaScript in the browser. This created a fundamental tension—your domain model couldn't cross the boundary.

The result was **translation layers**:
- DTOs to serialize server state
- API controllers to expose operations
- Client-side models to receive data
- Mapping code to convert between them

Every property existed in multiple places. Every change rippled through layers.

## The Opportunity

Blazor WebAssembly changed the equation. Now the same .NET library can execute in both the browser and on the server.

This means your `Employee` class doesn't need a `EmployeeDto` twin. The same type can:
- Validate on the client (fast feedback)
- Execute business logic on the server (with database access)
- Serialize directly across the wire (no mapping)

## The Gap

But sharing the library isn't enough. You still need:
- Factories to create instances with proper DI
- Serialization that handles the client-server round trip
- HTTP endpoints to route remote calls
- Service injection that works differently on each side

Writing this infrastructure by hand recreates the boilerplate problem.

## RemoteFactory's Solution

RemoteFactory generates all of it at compile time:

1. **Mark your domain methods** with attributes (`[Factory]`, `[Remote]`, `[Fetch]`, etc.)
2. **Generator creates** factory interfaces, implementations, serialization, and HTTP handling
3. **Same class works** on client and server with appropriate behavior

```
Traditional 3-Tier          With RemoteFactory
─────────────────          ─────────────────
Domain Model               Domain Model + Attributes
      ↓                           ↓
DTOs                       (generated)
      ↓                           ↓
Factories/Mapping          (generated)
      ↓                           ↓
API Controllers            (generated endpoint)
```

The domain model becomes the single source of truth. Changes happen in one place.

## Who Benefits Most

RemoteFactory is designed for:

- **Blazor WebAssembly applications** with ASP.NET Core backends
- **Domain-driven designs** where entities have behavior, not just data
- **Teams tired of maintaining** DTO layers and mapping code

If you're building a JavaScript SPA with a .NET API, RemoteFactory won't help—you still need that translation layer. But if you're all-.NET, the translation layer is now optional.
