# Service Injection

Two ways to get a service, resolved on different tiers. Constructor injection is both tiers: the object is built through DI on whichever side holds it. A `[Service]` method parameter is the tier that runs the method: the server for a `[Remote]` or `internal` method, the calling tier for a `public` local one.

## Constructor injection: both tiers

<!-- snippet: service-injection-constructor -->
<a id='snippet-service-injection-constructor'></a>
```cs
public CtorInjectedEntity(ITenantTokenService tokens)
{
    _tokens = tokens;
}

// Read on whichever tier holds the instance: the client's service after the round trip
public string TokenFromContext => _tokens.Token;
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/CtorInjectionExample.cs#L130-L138' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-constructor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Take a service in the constructor only when it is registered on both tiers, because the client builds the object too. A context's generated factories are the common case: the checkout context in [SKILL.md](../SKILL.md#which-factory-shape) takes `IOrderFactory` and `IOrderLineFactory` so its verbs work on the client.

**How it survives the wire.** A class whose constructors all need arguments cannot be built from an object initializer, so the generator skips the ordinal serializer for it and the named JSON path is used instead. Named deserialization resolves the instance through `IServiceProvider.GetRequiredService`, and DI calls the constructor with that tier's services: the server's instance on the server, the client's on the client.

| Constructor shape | Ordinal serializer | Deserialized by |
|---|---|---|
| None declared, or parameterless | Yes | the ordinal array |
| Every parameter has a default | Yes | the ordinal array |
| A parameter without a default | No | named JSON and `GetRequiredService` |

The generator looks at parameter count and defaults, not at `[Service]`. A data parameter in the constructor (`(int id, IRepo repo)`) also turns ordinal off, and DI cannot supply `id`; restore data from public properties instead. Private fields never cross the wire on either path. Public fields differ by path: the ordinal array carries properties only, while the named JSON path includes fields, so a public field crosses there. Prefer public properties for state the other side needs.

## `[Service]` parameters: the tier that runs the method

On a `[Remote]` method the service is the server's. This is where server-only services go:

<!-- snippet: service-injection-server-only -->
<a id='snippet-service-injection-server-only'></a>
```cs
[Remote, Fetch]
internal static async Task<TaxRate?> Fetch(string region, [Service] ITaxTable taxTable)
{
    var rate = await taxTable.FindRateAsync(region);
    return rate is null ? null : new TaxRate(region, rate);
}
```
<sup><a href='/src/Design/Design.Domain/ValueObjects/TaxRate.cs#L37-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-server-only' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

On a `public` local method the service is the calling tier's. `Order.Create` takes the line-list factory, which exists on the client:

<!-- snippet: operations-create-method -->
<a id='snippet-operations-create-method'></a>
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
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L235-L249' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-method' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A server-only service belongs only on a method that runs on the server: one that is `[Remote]` or `internal`. Repositories, the database context, email and payment gateways are `[Service]` parameters there.

**Use a method-injected service inside the method; never store it.** A field holding it is empty after the object crosses the wire:

```csharp
// WRONG - the field is null once the object reaches the client
private IOrderDbContext _db = null!;

[Remote, Fetch]
internal async Task<bool> Fetch(int id, [Service] IOrderDbContext db)
{
    _db = db;
    ...
}
```

A service the object needs on both tiers is a constructor parameter.

## Child entities: already on the server

A child's operations run inside its root's operation, which has already crossed. They are `internal` and take no `[Remote]`:

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

`[Remote]` marks where the client's call crosses. On a child operation it costs no extra round trip - once on the server, `[Remote]` does nothing different - but it is wrong: it promotes the operation to `public` on the factory interface and gives the client its own way in to the child, around the root that should reach it. A child's `Create` is `public` and local, so the client builds the line and adds it to the order.

## Registration on each tier

The server registers the services its `[Service]` parameters take, and the domain services the client calls:

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

The client registers only what code running on the client resolves - a bare `[Execute]`'s services, a constructor's services - and nothing server-only:

<!-- snippet: skill-client-services -->
<a id='snippet-skill-client-services'></a>
```cs
builder.Services.AddScoped<ITextScorer, TextScorer>();
```
<sup><a href='/src/Design/Design.Client.Blazor/Program.cs#L84-L86' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-client-services' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`RegisterMatchingName` registers each class for the interface named after it, in the same namespace. It registers concrete types DI may not be able to build, which fails `ValidateOnBuild` ([#131](https://github.com/NeatooDotNet/RemoteFactory/issues/131)).
