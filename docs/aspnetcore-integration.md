---
title: ASP.NET Core Integration
nav_order: 12
---

# ASP.NET Core Integration

`Neatoo.RemoteFactory.AspNetCore` puts the server side of RemoteFactory into an ASP.NET Core app. It registers the generated factories in server mode and maps one endpoint, `POST /api/neatoo`, that runs every `[Remote]` call a client makes. There are no controllers.

The code on this page comes from RemoteFactory's compiled example set, `src/Design`. Its server, `Design.Server`, hosts a Blazor WebAssembly client, and its tests run that server's real `Program` over HTTP.

## Installation

```bash
dotnet add package Neatoo.RemoteFactory.AspNetCore
```

The package depends on `Neatoo.RemoteFactory`.

## Basic Setup

The server's `Program` registers RemoteFactory and adds its endpoint. Design.Server also hosts its Blazor WASM client:

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

// Sign the caller in before the endpoint runs: the authorization checks read HttpContext.User
app.UseAuthentication();
app.UseAuthorization();

// The RemoteFactory endpoint: one route for every factory call, no controllers
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```
<sup><a href='/src/Design/Design.Server/Program.cs#L61-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-server-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`AddDesignServer()` holds the server's whole composition, so a test can build exactly what the server builds. Its RemoteFactory line is:

<!-- snippet: readme-server-setup -->
<a id='snippet-readme-server-setup'></a>
```cs
services.AddNeatooAspNetCore(typeof(IOrder).Assembly);
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L74-L76' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-server-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Two calls do the work:

- **`AddNeatooAspNetCore(params Assembly[])`** registers the generated factories of each assembly you pass, in server mode. It also registers the serializer and its options, the default `IAspAuthorize`, and the request handler the endpoint calls. An overload takes `NeatooSerializationOptions` first ([Serialization Format](#serialization-format)).
- **`UseNeatoo()`** maps `POST /api/neatoo`. The path is fixed: the client posts to the same one. Other endpoints coexist with it, such as Design.Server's `MapFallbackToFile` for the hosted client.

## Service Registration

The services your factory methods take as `[Service]` parameters are server registrations like any other. Design.Server registers the ones that need a lifetime or a constructor argument explicitly, then lets the naming convention pick up the rest:

<!-- snippet: aspnetcore-service-registration -->
<a id='snippet-aspnetcore-service-registration'></a>
```cs
services.AddScoped<IPhaseAuditService, PhaseAuditService>();
services.AddSingleton<IOrderDbContext, InMemoryOrderDbContext>();
services.AddScoped<IProductReviewService, InMemoryProductReviewService>();
services.AddScoped<IExampleService, ExampleService>();
services.AddSingleton<ITaxTable, InMemoryTaxTable>();
services.AddSingleton<ICustomerPreferencesStore, InMemoryCustomerPreferencesStore>();
services.AddSingleton<ISupportLog, InMemorySupportLog>();
services.AddSingleton<ICheckoutLedger, InMemoryCheckoutLedger>();
services.AddScoped(_ => new TenantTokenService("server-token"));
services.AddScoped<ITenantTokenService>(sp => sp.GetRequiredService<TenantTokenService>());

services.RegisterMatchingName(typeof(IOrder).Assembly);
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L197-L210' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-service-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`RegisterMatchingName` registers each `IName` → `Name` pair in the assemblies you pass, **transient**, with `TryAdd`. It also registers each matched concrete type by itself. Three things follow:

- **Explicit registrations come first.** `TryAdd` leaves an existing registration alone.
- **Register anything stateful explicitly, as scoped.** Two `[Service]` parameters of a transient type get two instances.
- **A matched type whose constructor needs a value DI cannot supply must be registered first.** Design's `TenantTokenService`, which takes a token string, is one. Otherwise the convention's registration of the concrete type can never be built, and in Development, where ASP.NET Core builds every registration at startup, the server does not start.

### Calling It Alongside Another Registration

`AddNeatooAspNetCore` and `AddNeatooRemoteFactory` may both run on one server, in either order: a Neatoo server calls `AddNeatooServices(NeatooFactory.Server, ...)`, which reaches `AddNeatooRemoteFactory`, and `AddNeatooAspNetCore` for `IAspAuthorize`. Since v1.10.5 (#117) the second registration adds only what is new: its assemblies join the existing list and their factories are registered, and everything else the first call set up stands, including a JSON converter factory the host replaced. Serialization options passed explicitly replace the registered ones; the overloads without options keep them. A second call in a different `NeatooFactory` mode throws.

## Multiple Assemblies

Pass every assembly that declares `[Factory]` types to `AddNeatooAspNetCore`: `AddNeatooAspNetCore(typeof(IOrder).Assembly, typeof(IInvoice).Assembly)`. A second registration adds its assemblies to the first (see above). The client's `AddNeatooRemoteFactory` call takes the same list, so keep it where both tiers read it.

## Serialization Format

With no options, both tiers use **Ordinal**, the compact array format. To use **Named** (property-name JSON, easier to read in a network trace), pass `new NeatooSerializationOptions { Format = SerializationFormat.Named }` to `AddNeatooAspNetCore` **and** to the client's `AddNeatooRemoteFactory`.

The two tiers must register the same format. The server announces its format in an `X-Neatoo-Format` response header, but the client does not read it: a Named server and an Ordinal client fail every call.

So be careful about switching formats by environment. On .NET 10, a Blazor WebAssembly client's environment is fixed when the client is built: a `dotnet build`, Debug or Release, is `Development` whatever the server runs as. A client and a server can therefore disagree about the environment, and so about the format. Key the choice to something both tiers share, such as the build configuration of a library they both load, or leave the default. See [Serialization](serialization.md) for the formats themselves.

## What UseNeatoo Does

- **Maps `POST /api/neatoo`.** It deserializes the request, runs the operation, and serializes the result.
- **Adds correlation middleware, for every request.** It reads `X-Correlation-Id` into the request's `ICorrelationContext`, generating one when the header is absent, and writes it back on the response ([Correlation IDs](#correlation-ids)).
- **Writes `X-Neatoo-Format`** (`ordinal` or `named`) on every endpoint response.
- **Gives the operation a cancellation token** that fires when the client disconnects or the server begins shutting down ([Cancellation](#cancellation)).
- **Hands the app's `ILoggerFactory` to RemoteFactory's serializer**, so its log entries reach your logging.

### Middleware Order

`UseAuthentication()` and `UseAuthorization()` run before `UseNeatoo()`, as in the [Basic Setup](#basic-setup) Program above. Authentication sets `HttpContext.User` before the endpoint runs, and `[AspAuthorize]`, which names no scheme of its own, checks its policies against that user. The application registers its scheme, cookies or JWT bearer, with `AddAuthentication`; see [Authorization](authorization.md).

A client served from another origin, such as a standalone WASM app on its own host, needs CORS. Add a policy that allows the client's origin, with `POST` and the `Content-Type` and `X-Correlation-Id` request headers. Expose `X-Correlation-Id` if the client reads it, and call `UseCors()` before `UseNeatoo()`. A hosted client like Design's is same-origin and needs none of this.

## The Endpoint

The client posts a `RemoteRequestDto`, and the server answers with a `RemoteResponseDto`. Generated factories build and read both; you never construct them.

- **`RemoteRequestDto`:**
  - `DelegateFullName`: which operation;
  - `Parameters`: its serialized arguments;
  - `Target`: the serialized object, for an operation on an instance, such as a Save.
- **`RemoteResponseDto`:**
  - `Json`: the serialized result;
  - `RelayedEvents`: any events raised for the client relay ([Factory Events](factory-events.md)).

There is no error field. What a failure looks like is the HTTP status, and on the client, the exception the factory call throws.

## When a Call Fails

What the client sees, over HTTP:

| On the server | HTTP | On the client |
|---|---|---|
| A class factory's authorization refuses a read (`Create`, `Fetch`) | 200 | The factory call returns `null`, as for a missing object |
| A class factory's authorization refuses a `Save` | 200 | `NotAuthorizedException` |
| The operation throws | 500 | `HttpRequestException`, with `StatusCode` 500 |
| The client cancels the call | — | `OperationCanceledException` (a `TaskCanceledException`) |

The handler rethrows a server exception, so ASP.NET Core answers 500, and the exception's type and message do not reach the client. Log it on the server. An interface factory's authorization failure is a server exception too, so it also arrives as a 500. A denied `[AspAuthorize]` check that forbids the request is covered in [Authorization](authorization.md).

A screen handles the cases it can tell apart:

<!-- snippet: aspnetcore-error-handling -->
<a id='snippet-aspnetcore-error-handling'></a>
```cs
public async Task Load(int id)
{
    // Null when there is no such order, and when the server's rules refuse the read
    Order = await orderFactory.Fetch(id);
    Error = Order == null ? $"Order {id} was not found." : null;
}

public async Task Save()
{
    try
    {
        Order = await orderFactory.Save(Order!);
        Error = null;
    }
    catch (NotAuthorizedException)
    {
        // The server's rules refused the write; the order on screen is unsaved
        Error = "You are not allowed to save this order.";
    }
    catch (HttpRequestException)
    {
        // The server threw (it answers 500), or it could not be reached
        Error = "The order could not be saved. Try again.";
    }
}
```
<sup><a href='/src/Design/Design.Tests/Http/ClientErrorHandlingTests.cs#L94-L120' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-error-handling' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Cancellation

A `[Remote]` operation that declares a `CancellationToken` parameter receives the request's token. It is not serialized and is not a `[Service]`. On the server it fires when the client cancels or disconnects, or when the host begins shutting down (`IHostApplicationLifetime.ApplicationStopping`). On shutdown the client is still connected: the operation's cancellation is a server exception, and the client's call fails as one. Pass the token on to the work that can stop:

<!-- snippet: aspnetcore-cancellation -->
<a id='snippet-aspnetcore-cancellation'></a>
```cs
[Remote, Fetch]
internal async Task<bool> Fetch(
    int orderId,
    [Service] IOrderDbContext db,
    CancellationToken cancellationToken)
{
    var row = await db.FindAsync(orderId, cancellationToken);
    if (row == null)
        return false;

    OrderId = row.Id;
    CustomerName = row.CustomerName;
    Status = row.Status;
    Lines = row.Lines
        .Select(l => new OrderSummaryLine(l.ProductName, l.Quantity, l.UnitPrice * l.Quantity))
        .ToList();
    Total = Lines.Sum(l => l.LineTotal);
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/ReadModels/OrderSummary.cs#L87-L107' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Every generated factory method takes an optional `CancellationToken`, so the client passes one in the same call: `await summaryFactory.Fetch(orderId, cancellationToken)`. Cancelling it ends the client's HTTP call and cancels the server's token.

## Correlation IDs

The client generates a correlation ID once per DI scope (in Blazor WebAssembly, once per app session) and sends it as `X-Correlation-Id` on every call. On the server, `UseNeatoo()` puts it in the request's `ICorrelationContext` and echoes it on the response. RemoteFactory's own log entries for the call carry it as a `[{CorrelationId}]` message parameter. It is not added as a logging scope.

A server operation reads it through `[Service] ICorrelationContext`. Design hands it back as a support reference, which then finds the session's calls in the server's logs:

<!-- snippet: aspnetcore-correlation-id -->
<a id='snippet-aspnetcore-correlation-id'></a>
```cs
[Remote, Execute]
private static async Task<string> _ReportProblem(
    int orderId,
    string description,
    [Service] ICorrelationContext correlation,
    [Service] ISupportLog supportLog)
{
    // The same ID tags the server's log lines for this call
    var reference = correlation.CorrelationId ?? "unknown";
    await supportLog.RecordAsync(new SupportEntry(reference, orderId, description));
    return reference;
}
```
<sup><a href='/src/Design/Design.Domain/Services/SupportRequests.cs#L39-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-correlation-id' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Logging

RemoteFactory logs under three prefixes:

- **`Neatoo.RemoteFactory`.** It covers:
  - `Neatoo.RemoteFactory.Server`: request handling, event dispatch and phase scheduling;
  - `Neatoo.RemoteFactory.Factory`: event type registration;
  - `Neatoo.RemoteFactory.Serialization`: the ordinal converters;
  - the serializer and the client's remote calls (`Neatoo.RemoteFactory.Internal.*`);
  - `AspAuthorize`.
- **`RemoteFactory.Trace`.** Each request's serialize and deserialize steps, at `Trace`. It is **not** under the prefix above, so a filter on `Neatoo.RemoteFactory` leaves it off.
- **The generated class factories.** Each one logs under its own type name, in your namespace (`Design.Domain.ReadModels.OrderSummaryFactory`), at `Information` for each operation's start and finish. Static and interface factories write no log entries of their own.

<!-- snippet: aspnetcore-logging -->
<a id='snippet-aspnetcore-logging'></a>
```cs
services.AddLogging(logging => logging
    .AddFilter("Neatoo.RemoteFactory", LogLevel.Information)
    .AddFilter("RemoteFactory.Trace", LogLevel.Warning)   // Trace: every request's (de)serialization
    .AddFilter("Design", LogLevel.Information));          // the generated class factories
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L132-L137' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-logging' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Authorization

`AddNeatooAspNetCore` registers `AspAuthorize` as the `IAspAuthorize` implementation, which evaluates `[AspAuthorize]` policies and roles through ASP.NET Core's authorization services. It needs those services and `IHttpContextAccessor` registered. Register your own `IAspAuthorize` before `AddNeatooAspNetCore` to replace it. See [Authorization](authorization.md).

## Event Handlers

`[FactoryEventHandler<T>]` handlers raised during a request run in that request's DI scope, one after another, and are awaited: the HTTP response waits for all of them. An exception from an in-transaction handler (`Immediate`, or an `AfterFlush` drain) fails the operation and reaches the client. An `AfterCommit` handler's exception is logged and never fails the response. Nothing runs handlers in a background or hosted service. See [Factory Events](factory-events.md).

## Testing

`DesignClientServerContainers.Scopes()` builds a server container, a client container whose `[Remote]` calls are serialized and handed to that server, and a local one with nothing serialized. Resolve factories from the client scope to test what a client sends and receives:

<!-- snippet: aspnetcore-testing -->
<a id='snippet-aspnetcore-testing'></a>
```cs
var (server, client, local) = DesignClientServerContainers.Scopes();
var orderFactory = client.GetRequiredService<IOrderFactory>();

IOrder? order = orderFactory.Create("Test_Customer");   // local: no server call
order = await orderFactory.Save(order);                 // serialized to the server and back
var fetched = await orderFactory.Fetch(order!.Id);      // serialized again
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/ClientServerTestingTests.cs#L27-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

That round trip has no HTTP, so no middleware runs and there are no status codes or headers. Design.Tests covers those by hosting Design.Server's real `Program` with `WebApplicationFactory` (`Design.Tests/Http`), and by pointing a Remote-mode client's keyed `HttpClient` at it.

## Summary

| Call | Purpose |
|------|---------|
| `AddNeatooAspNetCore(assemblies)` | Register the generated factories in server mode, with the serializer, `IAspAuthorize` and the request handler |
| `AddNeatooAspNetCore(options, assemblies)` | The same, with a serialization format the client must match |
| `UseNeatoo()` | Map `POST /api/neatoo`, with correlation, format header and cancellation |

## Next Steps

- [Getting Started](getting-started.md): the first working example
- [Authorization](authorization.md): `[AspAuthorize]` and `[AuthorizeFactory<T>]`
- [Factory Events](factory-events.md): `[FactoryEventHandler<T>]` and the client relay
- [Serialization](serialization.md): Ordinal and Named formats
