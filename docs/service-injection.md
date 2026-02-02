# Service Injection

RemoteFactory integrates with ASP.NET Core dependency injection, allowing factory methods to receive services without serialization overhead.

## The [Service] Attribute

Mark parameters with `[Service]` to inject from the DI container:

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

The two injection patterns support the client-server split:

| Injection Type | Available On | Typical Use |
|---------------|--------------|-------------|
| Constructor (`[Service]` on constructor) | Client + Server | Validation, logging, client-side services |
| Method (`[Service]` on method parameters) | Server only | Repositories, database, secrets |

Method injection is the common case—most factory methods have method-injected services but don't need `[Remote]`. They're called from server-side code after already crossing the boundary via an aggregate root's `[Remote]` method.

See [Client-Server Architecture](client-server-architecture.md) for the complete mental model.

## Serialization Caveat: Method-Injected Services Are Lost

**Important**: Services injected via method parameters and stored in fields are **not serialized**. After crossing the client-server boundary, these fields will be null.

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

When the client fetches an `OrderLineList` via a remote call:
1. Server executes `Fetch()` with `IOrderLineFactory` injected
2. Server stores the factory in `_lineFactory`
3. Response serializes the list (but NOT the factory reference)
4. Client deserializes the list—`_lineFactory` is null
5. Calling `AddLine()` on the client throws `NullReferenceException`

**Solution: Use constructor injection for services needed on both sides**

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

With constructor injection:
1. Server executes `Fetch()` and populates the list
2. Response serializes the list
3. Client deserializes the list
4. RemoteFactory resolves `IOrderLineFactory` from client DI
5. `AddLine()` works because `_lineFactory` is resolved on both sides

**Rule of thumb**: If you need a service reference after the object crosses the wire, use constructor injection.

## Parameter Rules

Service parameters can appear anywhere in the parameter list, but conventionally appear after value parameters:

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

Parameter resolution:
- **Value parameters**: Deserialized from request
- **Service parameters**: Resolved from server DI container
- **CancellationToken**: Special handling (always optional, always last)

## Service Lifetime

Services are resolved from the server's DI scope:

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

Generated factory call:
```csharp
// Server-side execution
using var scope = serviceProvider.CreateScope();
var auditContext = scope.ServiceProvider.GetRequiredService<IAuditContext>();
var result = await AuditExample._LogEmployeeAction(action, auditContext);
```

Scoped services are disposed when the request completes.

## Constructor Injection

Services can be injected into constructors marked with `[Create]`:

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

Factory behavior:
- **Local Create**: Resolves services from local container
- **Remote Create**: Executes on server with server's services

## Server-Only Services

Method-injected services are typically server-only (databases, file systems, secrets). This is the common case—most factory methods have these but do NOT need `[Remote]`.

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

`[Remote]` is only needed here because `Fetch` is an **entry point from the client**. Methods called from server-side code (after already crossing the boundary) don't need `[Remote]` even with server-only services.

## Client-Side Service Injection

Services can be injected on the client for local operations:

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

This method runs locally on the client, accessing the client's DI container.

## RegisterMatchingName Helper

RemoteFactory provides a convention-based registration helper:

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

This registers interfaces to their implementations with **Transient** lifetime:
- `IEmployeeRepository` → `EmployeeRepository`
- `IDepartmentService` → `DepartmentService`

Convention: Interface name starts with `I`, implementation removes the `I`.

The method accepts multiple assemblies to register services across different projects.

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
1. Service is registered in DI container
2. Method marked `[Remote]` if service is server-only
3. Service lifetime is appropriate (avoid singleton capturing scoped)

## Mixing Local and Remote Methods

Classes can have both local and remote factory methods:

<!-- snippet: service-injection-mixed -->
<a id='snippet-service-injection-mixed'></a>
```cs
// Mix of value params (serialized), services (injected), and CancellationToken
[Remote, Execute]
private static async Task<EmployeeTransferResult> _TransferEmployee(
    Guid employeeId,                           // Value: serialized
    Guid newDepartmentId,                      // Value: serialized
    [Service] IEmployeeRepository repository,  // Service: injected
    [Service] IUserContext userContext,        // Service: injected
    CancellationToken cancellationToken)       // CancellationToken: passed through
{
    var employee = await repository.GetByIdAsync(employeeId, cancellationToken);
    if (employee == null) return new EmployeeTransferResult(employeeId, userContext.Username, true);
    employee.DepartmentId = newDepartmentId;
    await repository.UpdateAsync(employee, cancellationToken);
    return new EmployeeTransferResult(employeeId, userContext.Username, false);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L223-L239' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-mixed' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The factory generates a static method that accepts value parameters (`employeeId`, `newDepartmentId`) and resolves service parameters (`IEmployeeRepository`, `IUserContext`) from DI. `CancellationToken` is passed through automatically.

## Service Parameter vs Regular Parameter

RemoteFactory determines parameter handling:

```csharp
[Remote]
[Fetch]
public async Task Fetch(
    Guid employeeId,                 // Value: serialized
    string filter,                   // Value: serialized
    [Service] IEmployeeRepository db,// Service: injected
    [Service] ILogger logger)        // Service: injected
{ }
```

Generated request payload (JSON):
```json
{
  "methodName": "Fetch",
  "args": ["3fa85f64-5717-4562-b3fc-2c963f66afa6", "active"]
}
```

## Specialized Services

### IHttpContextAccessor

Access HTTP context in server-side methods:

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

Direct access to the service provider:

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

Use sparingly. Prefer typed services.

## Transient vs Scoped vs Singleton

Service lifetimes behave as expected:

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

**Singleton**: Same instance across all requests
**Scoped**: Same instance within a request
**Transient**: New instance each time

RemoteFactory creates a new scope for each remote request.

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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L338-L346' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-testing-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Client/Server Container Testing

For integration tests that validate remote call serialization without HTTP, use the **ClientServerContainers** pattern:

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

Example test using local (Logical) mode:

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
1. **Faster** - No HTTP overhead
2. **Deterministic** - No timing issues
3. **Validates serialization** - JSON round-trip still happens
4. **Isolated** - No external dependencies

For a complete implementation, see `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs`.

## Next Steps

- [Factory Operations](factory-operations.md) - All operation types
- [Authorization](authorization.md) - Inject auth services
- [ASP.NET Core Integration](aspnetcore-integration.md) - Server DI configuration
