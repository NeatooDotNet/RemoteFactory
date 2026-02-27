# Service Injection

You already know DI from ASP.NET Core. You had two injection points: constructor injection for services the class always needs, and controller action injection for services needed to handle a specific request. RemoteFactory replaces the controller layer — now you have constructor injection and factory method injection. Same concept, different mechanism. RemoteFactory doesn't manage DI for you; it gives you the runway to wire DI correctly across the client/server boundary.

## The [Service] Attribute

RemoteFactory is a Roslyn Source Generator. The code that resolves services and calls your factory method is generated at compile time — the generator can't inspect DI registrations or make runtime decisions about which parameters are services. `[Service]` is the explicit marker that tells the generator: "resolve this from DI, don't serialize it."

<!-- snippet: service-injection-basic -->
<a id='snippet-service-injection-basic'></a>
```cs
// [Service] marks parameters for DI injection (not serialized)
[Remote, Fetch]
public async Task<bool> Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
{
    var entity = await repository.GetByIdAsync(employeeId);
    if (entity == null) return false;
    Id = entity.Id;
    Name = $"{entity.FirstName} {entity.LastName}";
    return true;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L22-L33' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

When the factory calls `Fetch()`:
- **Client**: Serializes `employeeId` parameter only
- **Server**: Deserializes `employeeId`, resolves `IEmployeeRepository` from DI
- **Server**: Calls method with both parameters
- **Result**: Serialized and returned

`IEmployeeRepository` is never serialized or sent over HTTP.

## Constructor vs Method Injection

Think of it as the ASP.NET Core parallel: before RemoteFactory, you had constructor injection + controller action injection. Now you have constructor injection + factory method injection. The factory method *is* the controller action — just without the controller boilerplate.

| Injection Type | The Parallel | Available On | Typical Use |
|---------------|-------------|--------------|-------------|
| Constructor (`[Service]` on constructor) | Same as before | Client + Server | Services the object always needs — survives serialization |
| Method (`[Service]` on method parameters) | Controller action injection | Server only | Repositories, database contexts, secrets |

Constructor-injected services are resolved from DI on *both* sides of the wire. When an object is deserialized on the client, RemoteFactory resolves constructor services from the client's DI container. This means the service reference survives the round-trip.

Method-injected services are resolved once, on whichever side executes the method — typically the server. They're the common case: most factory methods need a repository or database context that only exists where the data lives.

<!-- snippet: service-injection-constructor -->
<a id='snippet-service-injection-constructor'></a>
```cs
// Constructor injection - service available on BOTH client and server
[Create]
public EmployeeCompensation([Service] ISalaryCalculator calculator)
{
    _calculator = calculator;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L161-L168' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-constructor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: service-injection-server-only -->
<a id='snippet-service-injection-server-only'></a>
```cs
// Server-only service - [Remote] ensures execution on server where IEmployeeDatabase exists
[Remote, Fetch]
public async Task Fetch(string query, [Service] IEmployeeDatabase database)
{
    QueryResult = await database.ExecuteQueryAsync(query);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L66-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-server-only' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Serialization Caveat: Method-Injected Services Are Lost

RemoteFactory doesn't manage your DI — it gives you the runway to do DI correctly. The most important thing to understand: you register your DI differently on the client vs the server, and you need to choose the right injection point for each service. If you store a method-injected service in a field, that reference won't survive serialization — it may cause serialization errors or null references on the other side.

<!-- snippet: serialization-caveat-broken -->
<a id='snippet-serialization-caveat-broken'></a>
```cs
// BROKEN: Method-injected service stored in field is lost after serialization
private IOrderLineBrokenFactory? _lineFactory;  // NULL after crossing wire!

[Fetch]
public void Fetch(IEnumerable<(int id, string name, decimal price, int qty)> items,
    [Service] IOrderLineBrokenFactory lineFactory)
{
    _lineFactory = lineFactory;  // Stored - but NOT serialized
    foreach (var item in items)
        Add(lineFactory.Fetch(item.id, item.name, item.price, item.qty));
}

public void AddLine(string name, decimal price, int qty)
{
    var line = _lineFactory!.Create(name, price, qty);  // NullReferenceException!
    Add(line);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Collections/SerializationCaveatSamples.cs#L11-L29' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-caveat-broken' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Solution: Use constructor injection for services needed on both sides.** Constructor-injected services are resolved from DI after deserialization — the reference is restored on the client.

<!-- snippet: serialization-caveat-fixed -->
<a id='snippet-serialization-caveat-fixed'></a>
```cs
// CORRECT: Constructor injection - resolved from DI on BOTH client and server
private readonly IOrderLineFixedFactory _lineFactory;

[Create]
public OrderLineListFixed([Service] IOrderLineFixedFactory lineFactory)
{
    _lineFactory = lineFactory;  // Resolved on both sides after deserialization
}

public void AddLine(string name, decimal price, int qty)
{
    var line = _lineFactory.Create(name, price, qty);  // Works on client!
    Add(line);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Collections/SerializationCaveatSamples.cs#L66-L81' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-caveat-fixed' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Rule of thumb**: If you need a service reference after the object crosses the wire, use constructor injection.

## Multiple Service Parameters

A factory method can receive any number of services. The generator strips all `[Service]` parameters from the generated caller signature — the caller only sees value parameters and CancellationToken.

<!-- snippet: service-injection-multiple -->
<a id='snippet-service-injection-multiple'></a>
```cs
// Multiple [Service] parameters - all resolved from DI, none serialized
[Remote, Execute]
private static async Task<string> _ProcessTransfer(
    Guid employeeId,                               // Value: serialized
    Guid newDepartmentId,                          // Value: serialized
    [Service] IEmployeeRepository employeeRepo,    // Service: injected
    [Service] IDepartmentRepository departmentRepo,// Service: injected
    [Service] IUserContext userContext)            // Service: injected
{
    var employee = await employeeRepo.GetByIdAsync(employeeId);
    var department = await departmentRepo.GetByIdAsync(newDepartmentId);
    if (employee == null || department == null) return "Not found";
    return $"Transfer by {userContext.Username}";
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L82-L97' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-multiple' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For parameter ordering rules, see [Factory Operations — Method Parameters](factory-operations.md#method-parameters).

## Service Lifetimes

Standard ASP.NET Core lifetimes work with `[Service]` injection — RemoteFactory doesn't change how DI lifetimes behave. You register services the same way you always have:

<!-- snippet: service-injection-lifetimes -->
<a id='snippet-service-injection-lifetimes'></a>
```cs
// Standard ASP.NET Core lifetimes work with [Service] injection
public static void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<ISalaryCalculator, SalaryCalculator>();  // App lifetime
    services.AddScoped<IAuditContext, AuditContext>();             // Request lifetime
    services.AddTransient<INotificationService, NotificationService>(); // Per-resolution
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/ServiceRegistrationSamples.cs#L27-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-lifetimes' title='Start of snippet'>anchor</a></sup>
<a id='snippet-service-injection-lifetimes-1'></a>
```cs
[Fact]
public void ScopedServices_SameWithinScope()
{
    var scopes = TestClientServerContainers.CreateScopes();
    var repo1 = scopes.local.ServiceProvider.GetRequiredService<IEmployeeRepository>();
    var repo2 = scopes.local.ServiceProvider.GetRequiredService<IEmployeeRepository>();
    Assert.Same(repo1, repo2);  // Same instance within scope
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L354-L363' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-lifetimes-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Scoped services are particularly useful on the server side — scoped per factory call, just as they would be scoped per controller action. On the client side, scoped services are less common since there's typically no per-request scope.

RemoteFactory creates a new scope for each remote request.

<!-- snippet: service-injection-scoped -->
<a id='snippet-service-injection-scoped'></a>
```cs
// Scoped services share state within a request
[Remote, Execute]
private static Task<Guid> _LogEmployeeAction(string action, [Service] IAuditContext auditContext)
{
    auditContext.LogAction(action);
    return Task.FromResult(auditContext.CorrelationId);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L125-L133' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-scoped' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Client-Side Service Injection

Constructor-injected services are resolved on the client too. This is useful for services the object needs regardless of which side it's on — validation, calculation, notification:

<!-- snippet: service-injection-client -->
<a id='snippet-service-injection-client'></a>
```cs
// Constructor injection runs locally - available on client
[Create]
public EmployeeNotifier([Service] INotificationService notificationService)
{
    notificationService.Notify("Employee created");
    Notified = true;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L201-L209' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-client' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## RegisterMatchingName Helper

A convenience method for bulk DI registration. Scans an assembly and registers interfaces to matching implementations by name convention:

<!-- snippet: service-injection-matching-name -->
<a id='snippet-service-injection-matching-name'></a>
```cs
// Convention: IName -> Name (Transient lifetime)
public static void ConfigureServices(IServiceCollection services)
{
    services.RegisterMatchingName(typeof(IEmployeeRepository).Assembly);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/ServiceRegistrationSamples.cs#L13-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-matching-name' title='Start of snippet'>anchor</a></sup>
<a id='snippet-service-injection-matching-name-1'></a>
```cs
[Fact]
public void RegisterMatchingName_ResolvesCorrectImplementation()
{
    var scopes = TestClientServerContainers.CreateScopes();
    var repository = scopes.local.ServiceProvider.GetRequiredService<IEmployeeRepository>();
    Assert.IsType<InMemoryEmployeeRepository>(repository);  // IName -> Name convention
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L381-L389' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-matching-name-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Registers with **Transient** lifetime. Accepts multiple assemblies.

## Specialized Services

### IHttpContextAccessor

Server-side methods can access the HTTP context through a wrapper service — useful for extracting user identity or correlation IDs:

<!-- snippet: service-injection-httpcontext -->
<a id='snippet-service-injection-httpcontext'></a>
```cs
// Access HTTP context via wrapper service (server-only)
[Remote, Fetch]
public Task Fetch([Service] IHttpContextAccessorWrapper accessor)
{
    UserId = accessor.GetUserId();
    CorrelationId = accessor.GetCorrelationId();
    return Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L272-L281' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-httpcontext' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IServiceProvider

Direct service provider access for dynamic resolution. Use sparingly — prefer typed `[Service]` parameters:

<!-- snippet: service-injection-serviceprovider -->
<a id='snippet-service-injection-serviceprovider'></a>
```cs
// IServiceProvider for dynamic resolution - use sparingly
[Remote, Execute]
private static Task<bool> _ResolveEmployeeServices([Service] IServiceProvider serviceProvider)
{
    var repository = serviceProvider.GetService(typeof(IEmployeeRepository));
    return Task.FromResult(repository != null);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L290-L298' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-serviceprovider' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Service Resolution Failures

If a service can't be resolved:

**Server-side:**
```
System.InvalidOperationException: No service for type 'IEmployeeRepository' has been registered.
```

**Client-side (RemoteOnly mode):**
```
System.InvalidOperationException: No service for type 'IEmployeeDatabase' has been registered.
```

Ensure:
1. Service is registered in the correct DI container (client, server, or both)
2. Method marked `[Remote]` if service is server-only
3. Service lifetime is appropriate (avoid singleton capturing scoped)

## Testing with Service Injection

Register test doubles in your DI container:

<!-- snippet: service-injection-testing -->
<a id='snippet-service-injection-testing'></a>
```cs
// Register test doubles for unit/integration tests
public static void ConfigureTestServices(IServiceCollection services)
{
    services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
    services.AddScoped<IUserContext, TestUserContext>();
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/ServiceRegistrationSamples.cs#L43-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-testing' title='Start of snippet'>anchor</a></sup>
<a id='snippet-service-injection-testing-1'></a>
```cs
[Fact]
public void ServiceParameter_ResolvedFromDI()
{
    var scopes = TestClientServerContainers.CreateScopes();
    var repository = scopes.local.ServiceProvider.GetRequiredService<IEmployeeRepository>();
    Assert.NotNull(repository);  // Services resolve from test container
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L338-L346' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Client/Server Container Testing

For integration tests that validate the full serialization round-trip without HTTP, use the **ClientServerContainers** pattern. This creates three isolated DI containers — client, server, and local — each with different service registrations, just like a real deployment:

<!-- snippet: clientserver-container-setup -->
<a id='snippet-clientserver-container-setup'></a>
```cs
// Three containers: client (Remote), server (Server), local (Logical)
public static (IServiceScope server, IServiceScope client, IServiceScope local) Scopes()
{
    var options = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };
    var serverCollection = new ServiceCollection();
    var clientCollection = new ServiceCollection();
    var localCollection = new ServiceCollection();

    // Configure each container with appropriate mode
    serverCollection.AddNeatooRemoteFactory(NeatooFactory.Server, options, typeof(Employee).Assembly);
    clientCollection.AddNeatooRemoteFactory(NeatooFactory.Remote, options, typeof(Employee).Assembly);
    localCollection.AddNeatooRemoteFactory(NeatooFactory.Logical, options, typeof(Employee).Assembly);

    // Server/local get infrastructure; client gets server reference
    serverCollection.AddInfrastructureServices();
    localCollection.AddInfrastructureServices();
    clientCollection.AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>();

    // ... build providers, create scopes, link client to server
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/ClientServerContainerSamples.cs#L16-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-clientserver-container-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: clientserver-container-usage -->
<a id='snippet-clientserver-container-usage'></a>
```cs
[Fact]
public void Local_Create_WorksWithoutSerialization()
{
    var (server, client, local) = ClientServerContainers.Scopes();
    var factory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();
    var employee = factory.Create();  // Runs locally (Logical mode)
    Assert.NotNull(employee);
    server.Dispose(); client.Dispose(); local.Dispose();
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/ClientServerContainerSamples.cs#L63-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-clientserver-container-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Benefits of this pattern:
1. **Faster** — No HTTP overhead
2. **Deterministic** — No timing issues
3. **Validates serialization** — JSON round-trip still happens
4. **Isolated** — No external dependencies

For a complete implementation, see `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs`.

## Next Steps

- [Factory Operations](factory-operations.md) — All operation types and parameter ordering
- [Authorization](authorization.md) — Inject auth services
- [ASP.NET Core Integration](aspnetcore-integration.md) — Server DI configuration
- [Client-Server Architecture](client-server-architecture.md) — The complete mental model
