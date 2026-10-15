# Neatoo RemoteFactory

**One domain model on the client and the server, with no DTOs and no controllers between them**

When the library that defines your objects is loaded on both tiers, as it is with Blazor WebAssembly, the client/server seam needs no DTOs and no API controllers. RemoteFactory is a Roslyn source generator that makes that practical: it generates the factories, the serialization and the one endpoint that carry your objects and their operations across the wire. Persistence routing (Create, Fetch, then Save to Insert, Update or Delete) is one use of that.

## The Opportunity

With Blazor WebAssembly, the same .NET library can run on both client and server. This changes everything.

**The old world (JavaScript SPA + ASP.NET Core):**
Your domain model lived on the server. To get data to the browser, you serialized to JSON, wrote DTOs to receive it, and mapped back and forth. Two representations of the same thing.

**The new world (Blazor WASM + ASP.NET Core):**
Your domain model can run in the browser. The same `Order` is built and edited on the client and loaded and saved on the server. No translation layer needed.

**RemoteFactory makes this practical.** It generates the factories, serialization, and HTTP plumbing so your domain methods "just work" across the wire. One source of truth, zero boilerplate.

## Why RemoteFactory?

**Traditional 3-tier architecture:**
- Write domain model methods
- Create DTOs to transfer state
- Build factories to map between DTOs and domain models
- Write API controllers to expose operations
- Maintain all four layers as requirements change

**With RemoteFactory:**
- Write domain model methods
- Add attributes (`[Factory]`, `[Remote]`, `[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, etc.)
- Done. Generator creates factories, serialization, and endpoints.

## Quick Example

The domain model's public interface, which is what the client holds:

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
    void Submit();
    void Cancel();
    void MarkDeleted();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L37-L51' title='Snippet source file'>snippet source</a> | <a href='#snippet-order-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Its factory operations. `Create` runs wherever it is called; the `[Remote]` ones cross to the server, where their `[Service]` parameters are resolved:

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
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L215-L289' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-domain-model' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Client code calls the generated factory:

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

Server automatically exposes the endpoint at `/api/neatoo`. No controllers needed.

## Key Features

- **Zero boilerplate**: No DTOs, no manual mapping, no controllers
- **Type-safe**: Roslyn generates strongly-typed factories from your domain methods
- **DI integration**: Inject services into factory methods with `[Service]` attribute
- **Authorization**: Built-in support for custom auth or ASP.NET Core policies
- **Compact serialization**: Ordinal format reduces payloads by 40-50%
- **Lifecycle hooks**: `IFactoryOnStart`, `IFactoryOnComplete`, `IFactoryOnCancelled`
- **Domain events**: Transactional events via `[FactoryEventHandler<T>]` + `IFactoryEvents.Raise`, with client-side `IFactoryEventRelay`
- **Flexible modes**: Server, Remote, or Logical runtime modes

## Installation

Install NuGet packages:

**Server project:**
```bash
dotnet add package Neatoo.RemoteFactory
dotnet add package Neatoo.RemoteFactory.AspNetCore
```

**Client project (Blazor WASM, etc.):**
```bash
dotnet add package Neatoo.RemoteFactory
```

**Shared project (domain models):**
```bash
dotnet add package Neatoo.RemoteFactory
```

For reducing client bundle size, see [IL Trimming](docs/trimming.md) — it removes server-only code at publish time.


## Getting Started

**Server setup (ASP.NET Core):** register RemoteFactory with the assembly that holds your factories:

<!-- snippet: readme-server-setup -->
<a id='snippet-readme-server-setup'></a>
```cs
services.AddNeatooAspNetCore(typeof(IOrder).Assembly);
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L72-L74' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-server-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

That call sits in `AddDesignServer()`, the example set's one composition method, beside the server-only services its factory methods take as `[Service]` parameters. `Program` calls that method and adds the endpoint to the pipeline:

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

// The RemoteFactory endpoint: one route for every factory call, no controllers
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```
<sup><a href='/src/Design/Design.Server/Program.cs#L59-L77' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-server-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Client setup (Blazor WASM):**

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
<sup><a href='/src/Design/Design.Client.Blazor/Program.cs#L64-L72' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-client-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For authorization, see [Authorization](docs/authorization.md). See [Getting Started](docs/getting-started.md) for a complete walkthrough.

## Documentation

- [What Problem Does RemoteFactory Solve?](docs/the-problem.md) - The opportunity and approach
- [Getting Started](docs/getting-started.md) - Installation and first example
- [Decision Guide](docs/decision-guide.md) - When to use what
- [Factory Operations](docs/factory-operations.md) - Create, Fetch, Insert, Update, Delete, Execute
- [Service Injection](docs/service-injection.md) - DI integration with `[Service]` attribute
- [Authorization](docs/authorization.md) - Custom auth and ASP.NET Core policies
- [Serialization](docs/serialization.md) - Ordinal vs Named formats
- [Save Operation](docs/save-operation.md) - IFactorySave routing pattern
- [Factory Modes](docs/factory-modes.md) - Server, Remote, Logical runtime modes
- [Factory Events](docs/factory-events.md) - Transactional handlers in the caller's scope, dispatch phases, and the client relay
- [ASP.NET Core Integration](docs/aspnetcore-integration.md) - Server-side configuration
- [Attributes Reference](docs/attributes-reference.md) - All available attributes
- [Interfaces Reference](docs/interfaces-reference.md) - All available interfaces

## Supported Frameworks

- .NET 9.0 (STS)
- .NET 10.0 (LTS)

## Examples

The compiled, tested source of every example on this page is `src/Design` (the Order domain). Complete applications are in `src/Examples/`:

- **Person** - Simple Blazor WASM CRUD application
- **OrderEntry** - Order entry system with aggregate roots

## License

MIT License - see [LICENSE](LICENSE) for details.

## Links

- [NuGet: Neatoo.RemoteFactory](https://nuget.org/packages/Neatoo.RemoteFactory)
- [NuGet: Neatoo.RemoteFactory.AspNetCore](https://nuget.org/packages/Neatoo.RemoteFactory.AspNetCore)
- [Release Notes](docs/release-notes/index.md)
