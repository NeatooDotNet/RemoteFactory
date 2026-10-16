# Client-Server Architecture

RemoteFactory lets you write your domain model as if it runs in a single process. The same `Order` class is loaded on both the client and the server: it is built and edited on the client, loaded and saved on the server, and serialized across the wire without DTOs or mapping. The client/server boundary is there, but your code doesn't have to think about it.

This page is the big picture. [Factory Operations](factory-operations.md) covers each factory operation in detail. [Service Injection](service-injection.md) covers how DI works across the boundary.

## One Domain, Two Containers

Your domain assembly is referenced by both the client and the server. Each side has its own DI container with different service registrations:

- **Client container**: UI services, client-safe services, the factory interfaces that serialize calls to the server
- **Server container**: Data stores, database contexts, secrets — plus everything the client has

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

Use `[Remote]` when a call the client makes **has to cross to the server** because the method needs something only the server has: a data store, a server-only service, a secret.
- A root's Fetch, and the writes `Save` routes to (Insert/Update/Delete)
- An `[Execute]` that needs server services — `[Execute]` obeys `[Remote]`; a bare `[Execute]` runs on whichever tier resolves it, with that tier's services

**Rule of thumb**: ask whether the call needs to cross, not whether the client calls it. The client calls plenty of factory methods that must not cross:
- A root's `[Create]`, which is the equivalent of `new` and is local unless creation needs the server (tenant defaults, a sequence)
- A child's `[Create]`, which is `public` and local: the client builds a line, then adds it to its order
- A `Can*` authorization check whose auth method needs no server service
- A bare `[Execute]` that runs on the client with client services

If a method is only called from server-side code after already crossing the boundary, it needs no `[Remote]` either.

## When [Remote] is NOT Needed

Most methods don't need `[Remote]` — and this is the common case:
- Methods called from server-side code (after already crossing the boundary)
- Child entity operations within an aggregate
- Any method where the caller is already on the server
- Local operations the client calls directly (above)

A parent aggregate's `[Remote, Fetch]` method crosses to the server. From there, it can call child entity factories, collection factories, validation methods — none of which need `[Remote]` because execution is already server-side.

## What Happens If You Get It Wrong

A method without `[Remote]` runs on the tier that calls it. If the client calls one that needs the server, what happens depends on the method's visibility and on how the client was built:

- **A `public` method** runs on the client. If it takes a `[Service]` the client's container does not register, the call fails with the standard DI resolution error, for example `InvalidOperationException: No service for type 'IOrderDbContext' has been registered.`
- **An `internal` method** is generated with a server-only guard. In a client published with the `Neatoo.RemoteFactory.IsServerRuntime` switch set to `false` (the [IL Trimming](trimming.md) setup), the guard throws `InvalidOperationException` and the trimmer removes the body. With the default (`true`), the guard passes, and the method fails only the way a `public` one does, on a missing server service.
- **From another assembly**, such as your client project, an `internal` factory member cannot be called at all: it is a compile-time error.

The first two fail only when the method is called; the domain assembly compiles either way. The third is caught by the compiler.

## Method Visibility and the Client/Server Boundary

Method visibility (`public` vs `internal`) and the `[Remote]` attribute together control what the client can access:

| Declaration | Client Can Call? | Crosses Network? | Factory Interface |
|---|---|---|---|
| `[Remote] internal` | Yes | Yes — serializes to server | Promoted to `public` on interface |
| `public` (no Remote) | Yes | No — runs locally | Included in public interface |
| `internal` (no Remote) | No | N/A — server-only | Included with `internal` modifier (or `internal` interface if all methods are internal) |
| `[Remote] public static` `[Execute]` | Yes | Yes — serializes to server | Included in public interface; static methods are exempt from NF0105 |
| `public static` `[Execute]` (no Remote) | Yes | No — runs locally | Included in public interface |
| `internal static` `[Execute]` (no Remote) | No | N/A — server-only | Included with `internal` modifier |

`[Remote]` requires `internal` on instance methods — `[Remote] public` is a compile-time error (NF0105). The `[Remote] internal` combination means the method body is trimmable on the client, but the generated factory interface exposes it as `public` so clients can call it through the factory. Static methods are exempt from NF0105: a static-factory `[Execute]` is `private static` behind a generated public wrapper and a class-level one is `public static` by convention, so `[Remote]` alone decides placement there — and `[Execute]` obeys it like every other operation.

A child entity's operations are `internal` except its `[Create]`, which is `public` and local so the client can build a child and add it to its root. Because Create is public, the child's factory interface is `public`; its other members carry the `internal` modifier, so a client assembly cannot call them, and their bodies are trimmable:

<!-- snippet: orderline-child -->
<a id='snippet-orderline-child'></a>
```cs
/// <summary>
/// Creates a new OrderLine. Public and local: the client calls it.
/// </summary>
[Create]
public void Create(string productName, decimal unitPrice, int quantity)
{
    Id = Guid.NewGuid();
    ProductName = productName;
    UnitPrice = new Money(unitPrice);
    Quantity = quantity;
    IsNew = true;
}

/// <summary>
/// Loads a line from the row its order's load hands it, as a child.
/// </summary>
[Fetch]
internal void Fetch(OrderLineRow row)
{
    LoadFrom(row);
}

/// <summary>
/// Maps a new line into the row the list made for it.
/// </summary>
[Insert]
internal void Insert(OrderLineRow row)
{
    row.Id = Id;
    MapTo(row);
    IsNew = false;
}

/// <summary>
/// Maps an existing line into the row the list found for it.
/// </summary>
[Update]
internal void Update(OrderLineRow row)
{
    MapTo(row);
}
```
<sup><a href='/src/Design/Design.Domain/Entities/OrderLine.cs#L145-L187' title='Snippet source file'>snippet source</a> | <a href='#snippet-orderline-child' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The generated `IOrderLineFactory` therefore exposes `Create` to the client and carries the row `Fetch` and `Save` as `internal` members that the order's server-side cascade calls.

An entity can also be a root in one context and a child in another. `OrderLine` is a child of `Order`, and a line-detail screen loads one line on its own. The factory-method signature is the difference: the child `Fetch` takes the row its parent's load hands it and stays `internal`; the root `Fetch` takes a key, is a client entry point, and is `[Remote]` (promoted to `public` on the interface):

<!-- snippet: orderline-duality -->
<a id='snippet-orderline-duality'></a>
```cs
/// <summary>
/// Loads one line by its id, as a root; false when no order holds it.
/// </summary>
[Remote, Fetch]
internal async Task<bool> Fetch(Guid id, [Service] IOrderDbContext db)
{
    var row = await db.FindLineAsync(id);
    if (row == null)
        return false;

    LoadFrom(row);
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/Entities/OrderLine.cs#L189-L203' title='Snippet source file'>snippet source</a> | <a href='#snippet-orderline-duality' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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

- [Factory Operations](factory-operations.md) — How each factory operation works
- [Service Injection](service-injection.md) — Constructor vs method injection across the boundary
- [Factory Modes](factory-modes.md) — Remote, Logical, and Server registration modes
- [IL Trimming](trimming.md) — Remove server-only code from Blazor WASM published output
