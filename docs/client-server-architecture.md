# Client-Server Architecture

Understanding when to use `[Remote]` and how services flow between client and server.

## The Core Concept

`[Remote]` marks **entry points from the client to the server**. Once execution crosses to the server, it stays there—subsequent method calls don't need `[Remote]`.

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

## Constructor vs Method Injection

The two injection patterns exist to support the client-server split:

| Injection Type | Available On | Typical Use |
|---------------|--------------|-------------|
| Constructor (`[Service]` on constructor) | Client + Server | Validation, logging, client-side services |
| Method (`[Service]` on method parameters) | Server only | Repositories, database, secrets |

**Method injection is the common case.** Most factory methods have method-injected services but are NOT marked `[Remote]`—they're called from server-side code after already crossing the boundary.

## When to Use [Remote]

Use `[Remote]` for **entry points from the client**:
- Aggregate root Create/Fetch operations
- Top-level Execute operations initiated by UI
- Any method the client calls directly

> **Rule of Thumb**: If your Blazor component (or other client code) calls the factory method directly, add `[Remote]`. If the method is only called from other server-side code after already crossing the boundary, no `[Remote]` needed.

## When [Remote] is NOT Needed

Most methods don't need `[Remote]`:
- Methods called from server-side code (the common case)
- Child entity operations within an aggregate
- Any method invoked after already crossing to the server

## Entity Duality

An entity can be an aggregate root in one object graph and a child in another. For example, `Employee` might be:
- **Aggregate root**: When editing an employee directly → needs `[Remote]` factory methods
- **Child entity**: When loaded as part of a `Department` → no `[Remote]` needed

The same class may have `[Remote]` methods for aggregate root scenarios while other methods are server-only.

## Runtime Enforcement

Non-`[Remote]` methods are generated for client assemblies but fail at runtime with a "not-registered" DI exception if called—server-only services aren't in the client container.

This is **runtime enforcement**, not compile-time. The code compiles, but calling a server-only method from the client fails when resolving dependencies.

## Blazor WASM Best Practice

Exclude server-only packages from Blazor WASM client projects:

```xml
<!-- Client.csproj -->
<ItemGroup>
  <!-- Reference domain project but exclude server packages -->
  <ProjectReference Include="..\Domain\Domain.csproj" />

  <!-- Do NOT reference Entity Framework Core packages -->
</ItemGroup>
```

This enforces the boundary at the package level and reduces client bundle size.
