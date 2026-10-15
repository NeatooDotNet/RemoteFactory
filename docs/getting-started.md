# Getting Started

In [Why RemoteFactory](the-problem.md) you saw the single-layer concept: write your domain model once, load it on both tiers, and let RemoteFactory carry its objects and operations across the seam without DTOs or controllers. This guide makes it real: an `Order` aggregate, the one in RemoteFactory's compiled example set (`src/Design`), built on the client and loaded and saved on the server.

## Prerequisites

- .NET 9.0 or 10.0 SDK
- ASP.NET Core for server (optional — can use Logical mode for single-tier)
- Any .NET client (Blazor WebAssembly, MAUI, WPF, console)

## Project Structure

RemoteFactory's architecture needs three projects: one for the domain model (shared), one for the server, and one for the client. The domain project is referenced by both sides — the same `Order` class is loaded on client and server, with RemoteFactory generating different behavior for each.

```
YourSolution/
├── YourApp.Domain/          # Shared: Domain models with factory methods
├── YourApp.Server/          # ASP.NET Core server (hosts the Blazor WASM client)
└── YourApp.Client/          # Blazor WASM, MAUI, or other client
```

For Blazor WebAssembly, the server project hosts the client (hosted WASM). A single `dotnet run` on the server starts both the API and the client application.

## Installation

### 1. Install NuGet Packages

**Shared domain project:**
```bash
dotnet add package Neatoo.RemoteFactory
```

**Server project:**
```bash
dotnet add package Neatoo.RemoteFactory.AspNetCore
```

**Client project:**
```bash
dotnet add package Neatoo.RemoteFactory
```

The client needs its own direct `Neatoo.RemoteFactory` reference if it declares any factory types of its own. Roslyn source generators only run in projects with a direct package reference; a transitive reference through the domain project does not flow the generator, so no `FactoryServiceRegistrar` is emitted for client-declared types. A project that purely consumes factories (injects the interface, calls methods) without declaring any factory types of its own can rely on the transitive reference. When in doubt, add the direct reference — it is the safe default. See [IL Trimming](trimming.md#prerequisite-direct-neatooremotefactory-reference-in-every-project-with-factory-types) for the full explanation.

## Server Configuration

The server registers RemoteFactory with the assembly that holds your factories:

<!-- snippet: readme-server-setup -->
<a id='snippet-readme-server-setup'></a>
```cs
services.AddNeatooAspNetCore(typeof(IOrder).Assembly);
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L74-L76' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-server-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

That call sits in one method, `AddDesignServer()`, beside the server-only services your factory methods take as `[Service]` parameters (here the order store, `IOrderDbContext`). The server's `Program` calls it, adds the endpoint, and hosts the Blazor WASM client:

<!-- snippet: getting-started-server-program -->
<a id='snippet-getting-started-server-program'></a>
```cs
var builder = WebApplication.CreateBuilder(args);

// RemoteFactory plus the server-only services [Service] parameters resolve (ServerServices.cs)
builder.Services.AddDesignServer();

var app = builder.Build();

// Serve the hosted Blazor WASM client
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Sign the caller in before the endpoint runs: [AspAuthorize] and ICurrentUser read HttpContext.User
app.UseAuthentication();
app.UseAuthorization();

// The RemoteFactory endpoint: one route for every factory call, no controllers
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```
<sup><a href='/src/Design/Design.Server/Program.cs#L61-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-server-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`AddNeatooAspNetCore()` registers factories, serialization, and server-side handlers. `UseNeatoo()` adds the `/api/neatoo` endpoint — the single entry point for all remote factory calls. `UseBlazorFrameworkFiles()` and `MapFallbackToFile()` enable hosted Blazor WASM — the server serves the client application. `UseAuthentication()` and `UseAuthorization()` sign the caller in before the endpoint runs; your application registers its scheme ([Authorization](authorization.md#configure-the-server)).

## Client Configuration

The client registers RemoteFactory in Remote mode and provides an HttpClient for server communication:

<!-- snippet: getting-started-client-program -->
<a id='snippet-getting-started-client-program'></a>
```cs
// The factory proxies: calls marked [Remote] go to the server
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IOrder).Assembly);

// The HttpClient RemoteFactory sends them with: the server that hosts this client
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
```
<sup><a href='/src/Design/Design.Client.Blazor/Program.cs#L65-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-client-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`NeatooFactory.Remote` tells RemoteFactory this is the client side: calls to a `[Remote]` operation are serialized and sent to the server through the keyed HttpClient, and everything else runs on the client.

## Define Your Domain Model

The client holds the aggregate through its public interface:

<!-- snippet: order-interface -->
<a id='snippet-order-interface'></a>
```cs
public interface IOrder : IFactorySaveMeta
{
    int Id { get; set; }
    string CustomerName { get; set; }
    OrderStatus Status { get; set; }
    IOrderLineList Lines { get; set; }
    Money Total { get; }
    void AddLine(IOrderLine line);
    void RemoveLine(IOrderLine line);
    bool CanSubmit { get; }
    void Submit();
    void Cancel();
    void MarkDeleted();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L37-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-order-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The operations live on the domain object itself. Each attribute names the operation; `[Service]` parameters are injected on the tier that runs the method, so the order store (`IOrderDbContext`) is a server service the client never sees. No DTOs, no controllers:

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
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L232-L316' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-domain-model' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Quick reference:
- `[Factory]` — Generates the factory interface and implementation
- `[Create]` — The equivalent of `new`; local, runs on the tier that calls it
- `[Fetch]` — Load existing data; `false` when it is not there, so the factory returns null
- `[Insert]`, `[Update]`, `[Delete]` — Write operations (routed by `Save()`)
- `[Remote]` — A client entry point: the call crosses to the server
- `[Service]` — DI-injected parameter (not serialized)

## Use the Factory

Inject the generated factory and use it. The caller doesn't need to know where each operation runs — it's just method calls:

<!-- snippet: readme-client-usage -->
<a id='snippet-readme-client-usage'></a>
```cs
// The customer's draft: the stored one, or a new one. One server call.
IOrder? order = await orderFactory.OpenDraft("Customer_42");

// Build a line on the client and add it. No server call.
order.AddLine(lineFactory.Create("Widget C", 12.50m, 2));

// Save sends the whole order; keep what it returns.
order = await orderFactory.Save(order);
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/OrderUsageTests.cs#L31-L40' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-client-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## What Just Happened?

### The Remote Call

When the client calls `orderFactory.Save(order)`:

1. Client factory serializes the order and its lines
2. HTTP POST to `/api/neatoo`
3. Server deserializes the order, resolves `IOrderDbContext` from its DI container
4. Server calls the order's `Insert` (a new order) with the injected store
5. Server serializes the saved order and returns it
6. Client deserializes it, and the caller keeps it: `order = await orderFactory.Save(order)`

Creating the order's line, `lineFactory.Create(...)`, made no call at all: `Create` is local. No DTOs. No controllers. No manual mapping.

### Save Routing

When the client calls `orderFactory.Save(order)`, RemoteFactory reads the object's state and runs the matching write:

- `IsNew = true` → calls your `[Insert]` method
- `IsNew = false` → calls your `[Update]` method
- `IsDeleted = true` → calls your `[Delete]` method

One `Save()` call, three possible outcomes. You wrote each write; RemoteFactory picked the one to run.

## Next Steps

- [Factory Operations](factory-operations.md) — Create, Fetch, Insert, Update, Delete and Execute in detail
- [Service Injection](service-injection.md) — Constructor vs method injection across the boundary
- [Client-Server Architecture](client-server-architecture.md) — The big picture mental model
- [Factory Modes](factory-modes.md) — Remote, Logical, and Server registration modes
- [Save Operation](save-operation.md) — IFactorySave routing in depth

## Troubleshooting

**Factory interface not generated:**
- Ensure class has `[Factory]` attribute
- Check at least one method has `[Create]`, `[Fetch]`, or other operation attribute
- Clean and rebuild

**Service parameter not resolved:**
- Verify service is registered in DI container
- Check parameter has `[Service]` attribute
- Ensure service is registered on the correct side — server-only services won't resolve on the client

**Serialization errors:**
- Verify both client and server use the same serialization format (see [Serialization](serialization.md))
- Check all properties are serializable: primitives, collections, records (a value object needs no factory to cross), and `[Factory]` types

**401/403 errors:**
- Check authorization configuration — see [Authorization](authorization.md)
