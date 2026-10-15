# Service Injection

You already know DI from ASP.NET Core: constructor injection for what a class always needs, and action injection for what one request needs. RemoteFactory replaces the controller, so the second becomes factory-method injection. RemoteFactory does not manage DI for you. It decides which container each service comes from, and you register each service in the containers that need it. The code comes from RemoteFactory's compiled example set, `src/Design`.

## The [Service] Attribute

RemoteFactory is a source generator. The generated code that calls your factory method cannot inspect DI registrations, so `[Service]` tells it which parameters to resolve from DI rather than take from the caller. `CustomerPreferences` loads a customer's stored preferences:

<!-- snippet: service-injection-basic -->
<a id='snippet-service-injection-basic'></a>
```cs
[Remote, Fetch]
internal async Task<bool> Fetch(string customerName, [Service] ICustomerPreferencesStore store)
{
    var row = await store.FindAsync(customerName);
    if (row == null)
        return false;

    CustomerName = row.CustomerName;
    DeliveryWindow = row.DeliveryWindow;
    NotifyBySms = row.NotifyBySms;
    LastSavedAt = row.LastSavedAt;
    IsNew = false;
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/CustomerPreferences.cs#L76-L91' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

When the client calls `Fetch("Ada")`:
- **On the client**, the factory serializes `customerName` and nothing else.
- **On the server**, it deserializes `customerName`, resolves `ICustomerPreferencesStore` from the server's container, and calls the method.
- **The result** is serialized back to the client.

A `[Service]` parameter is never serialized, and never appears on the generated factory method.

## Where a Service Is Resolved

A `[Service]` parameter is resolved from the container of the tier that runs the method. `[Remote]` sends the call to the server, so its services come from the server's container. A method without `[Remote]` runs on the tier that calls it, and its services come from that tier's container, the client's included. The exception is authorization: an operation with an `[AspAuthorize]` check, or a matching `[Remote]` authorization method, goes to the server as if it carried `[Remote]` ([Authorization](authorization.md#placement-on-execute)). See [Factory Operations](factory-operations.md#method-parameters).

| Injection | Resolved | Typical use |
|-----------|----------|-------------|
| Constructor parameter | By DI, each time the object is built on either tier: when it is created, and again when it is deserialized | Services the object needs on both tiers |
| `[Service]` method parameter | Once, from the container of the tier that runs the method | Repositories, database contexts, anything only the server has, behind `[Remote]` |

### Constructor Injection

`CtorInjectedEntity` takes its service in the constructor and reads it later:

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

The entity has no parameterless constructor, so it is built through DI on both tiers. The server builds it with the server's `ITenantTokenService` for the Fetch. When the result reaches the client, the client's container builds it again with the client's service, and the deserializer fills its state. `TokenFromContext` then reads the client's token.

On a `[Create]` constructor, `[Service]` marks the parameters that come from DI instead of from the caller.

### A Server-Only Service

`TaxRate`'s rates live in a table only the server reads:

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
<sup><a href='/src/Design/Design.Domain/ValueObjects/TaxRate.cs#L36-L43' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-server-only' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`[Remote]` sends the call to the server, where `ITaxTable` is registered. The client sends a region, a key, and the server decides what to read. A method that ran a query the client wrote would hand the client the server's data source.

## Method-Injected Services Do Not Cross the Wire

The serializer writes an object's state, not the services it was handed. A `[Service]` parameter stored in a field is gone when the object reaches the other tier:

```csharp
// WRONG - a method-injected service kept in a field is null after the round trip
private IOrderLineFactory? _lineFactory;

[Remote, Fetch]
internal void Fetch(int id, [Service] IOrderLineFactory lineFactory)
{
    _lineFactory = lineFactory;   // set on the server; null on the client
}
```

Take a service the object needs on both tiers in its constructor, as `CtorInjectedEntity` does. DI supplies it again wherever the object is built. Otherwise take it again as a `[Service]` parameter of the method that needs it.

### Side Effects in a Constructor

A constructor runs every time the object is built, and that includes every deserialization. Be aware that `[Create]` constructors are called on deserialization: if there is side-effect behaviour, such as sending a notification or writing a log entry, a `[Create]` factory method is the better place for it. Keep a constructor to storing what it is given.

## Multiple Service Parameters

A method can take any number of services. The caller sees only the value parameters and the `CancellationToken`:

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

The generated `ReportProblem` delegate takes `(int orderId, string description, CancellationToken cancellationToken = default)`. Value parameters come first, then the `[Service]` parameters, then the `CancellationToken`, as [Factory Operations — Method Parameters](factory-operations.md#method-parameters) explains.

## Service Lifetimes

RemoteFactory does not change how DI lifetimes behave. Design's server registers its services with the standard lifetimes:

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
services.AddScoped(_ => new TenantTokenService("server-token"));
services.AddScoped<ITenantTokenService>(sp => sp.GetRequiredService<TenantTokenService>());

services.RegisterMatchingName(typeof(IOrder).Assembly);
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L197-L209' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-service-registration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Each remote call is one HTTP request, and its services come from that request's scope. A scoped service is therefore shared within one call: by the factory method, the operations it calls through other factories, and its `Immediate` event handlers. `ICorrelationContext`, above, is scoped: the middleware writes the request's correlation ID into it, and `_ReportProblem` reads it in the same scope. A client usually has one long-lived scope, so scoped and singleton services behave alike there.

## RegisterMatchingName

`RegisterMatchingName`, the last line above, scans the assemblies it is given and registers each interface `IFoo` with the class `Foo` in the same namespace. It registers both the interface and the class as **Transient**, with `TryAdd`, so a registration made before it stands. Register stateful services explicitly first, as Design does.

The convention registers whatever matches, in whichever container calls it: a client that calls it registers the same matching services the server does. To keep an implementation server-only, name it off the convention and register it explicitly on the server, as Design does with `InMemoryTaxTable` for `ITaxTable` and `InMemorySupportLog` for `ISupportLog`.

## Specialized Services

### The HTTP Context

Domain code should not depend on ASP.NET Core. Design's authorization classes take `ICurrentUser`, and the server builds it from the request's `HttpContext`:

<!-- snippet: service-injection-httpcontext -->
<a id='snippet-service-injection-httpcontext'></a>
```cs
services.AddScoped<ICurrentUser>(sp =>
    new ClaimsUser(sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User ?? new ClaimsPrincipal()));
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L109-L112' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-httpcontext' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`IHttpContextAccessor` comes from `AddHttpContextAccessor()`, in the same registration (see [Authorization](authorization.md)). The client registers its own `ICurrentUser`, so code that takes it runs on either tier.

### IServiceProvider

A `[Service] IServiceProvider` parameter receives the running tier's provider, for a service chosen at run time. Prefer typed `[Service]` parameters: they show what the method needs, and a missing registration fails at the call rather than inside the method's logic.

## Service Resolution Failures

A service that is not registered in the container of the tier running the method fails when the method is called. For a method without `[Remote]` that needs a server-only service, called on the client:

```
System.InvalidOperationException: No service for type 'Design.Domain.ValueObjects.ITaxTable' has been registered.
```

Check that:
1. the service is registered in the container of the tier that runs the method: the client's, the server's, or both;
2. a method that needs a server-only service carries `[Remote]`;
3. the lifetime is right: a singleton that captures a scoped service keeps the first scope's instance.

## Testing with Service Injection

`DesignClientServerContainers.Scopes` takes an optional configuration for the server container and one for the client container, so a test can replace a service on one tier:

<!-- snippet: service-injection-testing -->
<a id='snippet-service-injection-testing'></a>
```cs
var recorder = new RecordingNotificationService();
var (server, client, _) = DesignClientServerContainers.Scopes(
    configureServer: services => services.AddSingleton<INotificationService>(recorder));
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/StaticFactoryTests.cs#L95-L99' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The recorder replaces the server's `INotificationService`, and the test asserts that the call arrived on the server.

## Client/Server Container Testing

To test the serialization round trip without HTTP, build three containers: a server, a client whose `[Remote]` calls are serialized and handed to that server, and a local one that runs everything in-process. Design's test harness configures them like this:

<!-- snippet: clientserver-container-setup -->
<a id='snippet-clientserver-container-setup'></a>
```cs
var serializationOptions = new NeatooSerializationOptions { Format = format };

var serverCollection = new ServiceCollection();
var clientCollection = new ServiceCollection();
var localCollection = new ServiceCollection();

// One mode per container: Server handles requests, Remote serializes its
// [Remote] calls, Logical runs everything in-process
ConfigureContainer(serverCollection, NeatooFactory.Server, serializationOptions);
ConfigureContainer(clientCollection, NeatooFactory.Remote, serializationOptions);
ConfigureContainer(localCollection, NeatooFactory.Logical, serializationOptions);

// The client's remote calls go to the server scope, serialized, in place of HTTP
clientCollection.AddScoped<ServerServiceProvider>();
clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>();
```
<sup><a href='/src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs#L245-L261' title='Snippet source file'>snippet source</a> | <a href='#snippet-clientserver-container-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A test resolves its factories from the client scope, to exercise what a real client sends and receives:

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

This approach is:
1. **Fast:** no HTTP.
2. **Deterministic:** no timing.
3. **A serialization check:** every `[Remote]` call still goes through the serializer.
4. **Isolated:** no external dependencies.

The whole harness is `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs`.

## Next Steps

- [Factory Operations](factory-operations.md): every operation type, and parameter ordering
- [Authorization](authorization.md): services in authorization classes
- [ASP.NET Core Integration](aspnetcore-integration.md): server DI configuration
- [Client-Server Architecture](client-server-architecture.md): the complete mental model
