# Client-Server Architecture

RemoteFactory lets you write your domain model as if it runs in a single process. The same `Employee` class works on both the client and the server — it validates on the client, persists on the server, and serializes across the wire without DTOs or mapping. The client/server boundary is there, but your code doesn't have to think about it.

This page is the big picture. [Factory Operations](factory-operations.md) covers each persistence operation in detail. [Service Injection](service-injection.md) covers how DI works across the boundary.

## One Domain, Two Containers

Your domain assembly is referenced by both the client and the server. Each side has its own DI container with different service registrations:

- **Client container**: UI services, validation, the factory interfaces that serialize calls to the server
- **Server container**: Repositories, database contexts, secrets — plus everything the client has

RemoteFactory generates the factory implementations for each side. The client factory knows to serialize and send. The server factory knows to resolve services and execute locally. Your domain code doesn't change — the same `[Fetch]` method works regardless of which side invokes it.

## [Remote] — The Crossing Point

`[Remote]` marks **entry points from the client to the server**. It's how RemoteFactory knows which calls need to cross the network boundary. Once execution reaches the server, it stays there — subsequent method calls don't need `[Remote]`.

```
Client                          Server
──────                          ──────

[Remote] Fetch ───────────────► Execute Fetch
                                    │
                                    ▼
                               LoadChildren (no [Remote] needed)
                                    │
                                    ▼
                               ValidateRules (no [Remote] needed)
                                    │
◄─────────────────────────────── Return result
```

This is the most important thing to understand: `[Remote]` is not "this method runs on the server." It's "this method is a **client entry point** that crosses to the server." The distinction matters because most server-side methods are *not* entry points — they're called from other server-side code after the boundary has already been crossed.

## When to Use [Remote]

Use `[Remote]` for methods the **client calls directly**:
- Aggregate root Create/Fetch operations
- Top-level Save (Insert/Update/Delete) operations
- Execute operations initiated by the UI

**Rule of thumb**: If your client code (Blazor component, MAUI page, etc.) calls the factory method directly, add `[Remote]`. If the method is only called from server-side code after already crossing the boundary, skip it.

## When [Remote] is NOT Needed

Most methods don't need `[Remote]` — and this is the common case:
- Methods called from server-side code (after already crossing the boundary)
- Child entity operations within an aggregate
- Any method where the caller is already on the server

A parent aggregate's `[Remote, Fetch]` method crosses to the server. From there, it can call child entity factories, collection factories, validation methods — none of which need `[Remote]` because execution is already server-side.

## What Happens If You Get It Wrong

Non-`[Remote]` methods still compile for client assemblies, but calling one from the client fails at runtime — the server-only services it needs aren't registered in the client's DI container. You'll get a standard DI resolution exception:

```
System.InvalidOperationException: No service for type 'IEmployeeRepository' has been registered.
```

This is runtime enforcement, not compile-time. The code compiles, but the client container doesn't have the services to execute the method.

## Best Practice: Exclude Server Packages from Client Projects

Enforce the boundary at the package level by excluding server-only dependencies from client projects:

```xml
<!-- Client.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Domain\Domain.csproj" />
  <!-- Do NOT reference Entity Framework Core or other server-only packages -->
</ItemGroup>
```

This reduces client bundle size and makes the boundary explicit — if your client project can't even reference `DbContext`, you can't accidentally call server-only code.

For Blazor WASM apps where the domain assembly is shared, [IL Trimming](trimming.md) goes further — the IL trimmer removes server-only method bodies, their transitive dependencies, and the decompilable business logic from the published output. No assembly splitting required.

## Next Steps

- [Factory Operations](factory-operations.md) — How each persistence operation works
- [Service Injection](service-injection.md) — Constructor vs method injection across the boundary
- [Factory Modes](factory-modes.md) — Remote, Logical, and Server registration modes
- [IL Trimming](trimming.md) — Remove server-only code from Blazor WASM published output
