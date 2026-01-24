# Service Injection

RemoteFactory integrates with ASP.NET Core dependency injection, allowing factory methods to receive services without serialization overhead.

## The [Service] Attribute

Mark parameters with `[Service]` to inject from the DI container:

<!-- snippet: service-injection-basic -->
<a id='snippet-service-injection-basic'></a>
```cs
[Factory]
public partial class BasicServiceInjection
{
    public Guid Id { get; private set; }
    public string Data { get; private set; } = string.Empty;

    [Create]
    public BasicServiceInjection() { Id = Guid.NewGuid(); }

    [Remote]
    [Fetch]
    public async Task Fetch(Guid id, [Service] IPersonRepository repository)
    {
        // IPersonRepository is injected from DI container on server
        var entity = await repository.GetByIdAsync(id);
        if (entity != null)
        {
            Id = entity.Id;
            Data = $"{entity.FirstName} {entity.LastName}";
        }
    }
}
```
<sup><a href='/src/docs/samples/ServiceInjectionSamples.cs#L11-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

When the factory calls `Fetch()`:
- **Client**: Serializes `id` parameter only
- **Server**: Deserializes `id`, resolves `IPersonRepository` from DI
- **Server**: Calls method with both parameters
- **Result**: Serialized and returned

`IPersonRepository` is never serialized or sent over HTTP.

## Parameter Rules

Service parameters can appear anywhere in the parameter list, but conventionally appear after value parameters:

<!-- snippet: service-injection-multiple -->
<a id='snippet-service-injection-multiple'></a>
```cs
// Multiple service injection with [Execute] - use static partial class
[SuppressFactory] // Nested in wrapper class - pattern demonstration only
public static partial class MultipleServiceInjection
{
    [Remote]
    [Execute]
    private static async Task<string> _ProcessOrder(
        Guid orderId,
        [Service] IOrderRepository orderRepository,
        [Service] IPersonRepository personRepository,
        [Service] IUserContext userContext)
    {
        var order = await orderRepository.GetByIdAsync(orderId);
        var customer = await personRepository.GetByIdAsync(order?.CustomerId ?? Guid.Empty);

        return $"Order {order?.OrderNumber} for {customer?.FirstName} processed by {userContext.Username}";
    }
}
```
<sup><a href='/src/docs/samples/ServiceInjectionSamples.cs#L36-L55' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-multiple' title='Start of snippet'>anchor</a></sup>
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
public interface IScopedAuditContext
{
    Guid CorrelationId { get; }
    void LogAction(string action);
}

public partial class ScopedAuditContext : IScopedAuditContext
{
    public Guid CorrelationId { get; } = Guid.NewGuid();
    public List<string> Actions { get; } = new();

    public void LogAction(string action)
    {
        Actions.Add($"[{CorrelationId}] {action}");
    }
}

// Scoped service injection with [Execute] - use static partial class
[SuppressFactory] // Nested in wrapper class - pattern demonstration only
public static partial class ScopedServiceInjection
{
    [Remote]
    [Execute]
    private static Task<Guid> _ProcessWithAudit(
        string action,
        [Service] IScopedAuditContext auditContext)
    {
        // Scoped service - same instance throughout the request
        auditContext.LogAction(action);
        return Task.FromResult(auditContext.CorrelationId);
    }
}
```
<sup><a href='/src/docs/samples/ServiceInjectionSamples.cs#L57-L90' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-scoped' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated factory call:
```csharp
// Server-side execution
using var scope = serviceProvider.CreateScope();
var auditContext = scope.ServiceProvider.GetRequiredService<IScopedAuditContext>();
var result = await ScopedServiceInjection._ProcessWithAudit(action, auditContext);
```

Scoped services are disposed when the request completes.

## Constructor Injection

Services can be injected into constructors marked with `[Create]`:

<!-- snippet: service-injection-constructor -->
<a id='snippet-service-injection-constructor'></a>
```cs
public interface ICalculationService
{
    decimal Calculate(decimal amount, decimal rate);
}

public partial class CalculationService : ICalculationService
{
    public decimal Calculate(decimal amount, decimal rate) => amount * rate;
}

[Factory]
public partial class ConstructorServiceInjection
{
    private readonly ICalculationService _calculationService;

    public decimal Result { get; private set; }

    [Create]
    public ConstructorServiceInjection([Service] ICalculationService calculationService)
    {
        _calculationService = calculationService;
    }

    public void Calculate(decimal amount, decimal rate)
    {
        Result = _calculationService.Calculate(amount, rate);
    }
}
```
<sup><a href='/src/docs/samples/ServiceInjectionSamples.cs#L92-L121' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-constructor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Factory behavior:
- **Local Create**: Resolves services from local container
- **Remote Create**: Executes on server with server's services

## Server-Only Services

Some services exist only on the server (databases, file systems, secrets). Mark methods `[Remote]` to ensure server execution:

<!-- snippet: service-injection-server-only -->
<a id='snippet-service-injection-server-only'></a>
```cs
public interface IServerOnlyDatabaseService
{
    Task<string> ExecuteQueryAsync(string query);
}

public partial class ServerOnlyDatabaseService : IServerOnlyDatabaseService
{
    public Task<string> ExecuteQueryAsync(string query)
    {
        // This service only exists on the server
        return Task.FromResult($"Query result for: {query}");
    }
}

[Factory]
public partial class ServerOnlyServiceExample
{
    public string QueryResult { get; private set; } = string.Empty;

    [Create]
    public ServerOnlyServiceExample() { }

    // This method only runs on server where IServerOnlyDatabaseService is registered
    [Remote]
    [Fetch]
    public async Task Fetch(
        string query,
        [Service] IServerOnlyDatabaseService databaseService)
    {
        QueryResult = await databaseService.ExecuteQueryAsync(query);
    }
}
```
<sup><a href='/src/docs/samples/ServiceInjectionSamples.cs#L123-L156' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-server-only' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Without `[Remote]`, clients would call `Fetch()` locally and fail when resolving `IServerOnlyDatabaseService`.

## Client-Side Service Injection

Services can be injected on the client for local operations:

<!-- snippet: service-injection-client -->
<a id='snippet-service-injection-client'></a>
```cs
public interface IClientLoggerService
{
    void Log(string message);
}

public partial class ClientLoggerService : IClientLoggerService
{
    public List<string> Messages { get; } = new();
    public void Log(string message) => Messages.Add(message);
}

[Factory]
public partial class ClientServiceExample
{
    public bool Logged { get; private set; }

    [Create]
    public ClientServiceExample([Service] IClientLoggerService logger)
    {
        // IClientLoggerService available on both client and server
        logger.Log("ClientServiceExample created");
        Logged = true;
    }
}
```
<sup><a href='/src/docs/samples/ServiceInjectionSamples.cs#L158-L183' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-client' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This method runs locally on the client, accessing the client's DI container.

## RegisterMatchingName Helper

RemoteFactory provides a convention-based registration helper:

<!-- snippet: service-injection-matching-name -->
<a id='snippet-service-injection-matching-name'></a>
```cs
// When using RegisterMatchingName:
// IPersonRepository -> PersonRepository
// IOrderRepository -> OrderRepository
// The pattern: Interface name starts with 'I', implementation has same name without 'I'

public static class MatchingNameRegistration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Automatically registers IPersonRepository -> PersonRepository
        // if both types exist in the assembly
        services.RegisterMatchingName(typeof(ServiceInjectionSamples).Assembly);
    }
}
```
<sup><a href='/src/docs/samples/ServiceInjectionSamples.cs#L185-L200' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-matching-name' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This registers interfaces to their implementations with **Transient** lifetime:
- `IPersonRepository` → `PersonRepository`
- `IOrderService` → `OrderService`

Convention: Interface name starts with `I`, implementation removes the `I`.

The method accepts multiple assemblies to register services across different projects.

## Service Resolution Failures

If a service can't be resolved:

**Server-side:**
```
System.InvalidOperationException: No service for type 'IDbContext' has been registered.
```

**Client-side (RemoteOnly mode):**
```
System.InvalidOperationException: No service for type 'IServerOnlyService' has been registered.
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
// Mixed parameters with [Execute] - use static partial class
public record MixedParametersResult(Guid ProcessedId, string ProcessedBy, bool Cancelled);

[SuppressFactory] // Nested in wrapper class - pattern demonstration only
public static partial class MixedParametersExample
{
    [Remote]
    [Execute]
    private static async Task<MixedParametersResult> _ProcessItem(
        Guid itemId,                              // Value parameter
        string notes,                             // Value parameter
        [Service] IPersonRepository repository,   // Service parameter
        [Service] IUserContext userContext,       // Service parameter
        CancellationToken cancellationToken)      // CancellationToken
    {
        cancellationToken.ThrowIfCancellationRequested();

        await repository.GetByIdAsync(itemId, cancellationToken);

        return new MixedParametersResult(
            itemId,
            userContext.Username,
            cancellationToken.IsCancellationRequested);
    }
}
```
<sup><a href='/src/docs/samples/ServiceInjectionSamples.cs#L202-L228' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-mixed' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The factory generates a static method that accepts value parameters (`itemId`, `notes`) and resolves service parameters (`IPersonRepository`, `IUserContext`) from DI. `CancellationToken` is passed through automatically.

## Service Parameter vs Regular Parameter

RemoteFactory determines parameter handling:

```csharp
[Remote]
[Fetch]
public async Task Fetch(
    int id,                          // Value: serialized
    string filter,                   // Value: serialized
    [Service] IDbContext db,         // Service: injected
    [Service] ILogger logger)        // Service: injected
{ }
```

Generated request payload (JSON):
```json
{
  "methodName": "Fetch",
  "args": [42, "active"]  // Only id and filter
}
```

## Specialized Services

### IHttpContextAccessor

Access HTTP context in server-side methods:

<!-- snippet: service-injection-httpcontext -->
<a id='snippet-service-injection-httpcontext'></a>
```cs
// Note: IHttpContextAccessor is available in ASP.NET Core projects
// This example shows the pattern for accessing HttpContext

public interface IHttpContextAccessorSimulator
{
    string? GetUserId();
    string? GetCorrelationId();
}

public partial class HttpContextAccessorSimulator : IHttpContextAccessorSimulator
{
    public string? GetUserId() => "user-123";
    public string? GetCorrelationId() => Guid.NewGuid().ToString();
}

[Factory]
public partial class HttpContextExample
{
    public string? UserId { get; private set; }
    public string? CorrelationId { get; private set; }

    [Create]
    public HttpContextExample() { }

    [Remote]
    [Fetch]
    public Task Fetch([Service] IHttpContextAccessorSimulator httpContextAccessor)
    {
        // Access HttpContext on server to get user info, headers, etc.
        UserId = httpContextAccessor.GetUserId();
        CorrelationId = httpContextAccessor.GetCorrelationId();
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/ServiceInjectionSamples.cs#L230-L265' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-httpcontext' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IServiceProvider

Direct access to the service provider:

<!-- snippet: service-injection-serviceprovider -->
<a id='snippet-service-injection-serviceprovider'></a>
```cs
// ServiceProvider injection with [Execute] - use static partial class
[SuppressFactory] // Nested in wrapper class - pattern demonstration only
public static partial class ServiceProviderExample
{
    [Remote]
    [Execute]
    private static Task<bool> _ResolveServices([Service] IServiceProvider serviceProvider)
    {
        // Dynamically resolve services when needed
        var repository = serviceProvider.GetService<IPersonRepository>();
        var userContext = serviceProvider.GetService<IUserContext>();

        return Task.FromResult(repository != null && userContext != null);
    }
}
```
<sup><a href='/src/docs/samples/ServiceInjectionSamples.cs#L267-L283' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-serviceprovider' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use sparingly. Prefer typed services.

## Transient vs Scoped vs Singleton

Service lifetimes behave as expected:

<!-- snippet: service-injection-lifetimes -->
<a id='snippet-service-injection-lifetimes'></a>
```cs
// Service Lifetimes in RemoteFactory:
//
// Singleton: One instance for application lifetime
//   - Use for stateless services, caches, configuration
//   - Same instance across all requests and scopes
//
// Scoped: One instance per request/operation
//   - Use for DbContext, unit of work, request-specific state
//   - New instance for each factory operation
//
// Transient: New instance every time resolved
//   - Use for lightweight, stateless services
//   - New instance each time [Service] parameter is resolved

public static class ServiceLifetimeRegistration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Singleton - shared across all operations
        services.AddSingleton<ICalculationService, CalculationService>();

        // Scoped - per-operation instance
        services.AddScoped<IScopedAuditContext, ScopedAuditContext>();

        // Transient - new instance each time
        services.AddTransient<IClientLoggerService, ClientLoggerService>();
    }
}
```
<sup><a href='/src/docs/samples/ServiceInjectionSamples.cs#L285-L314' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-lifetimes' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Singleton**: Same instance across all requests
**Scoped**: Same instance within a request
**Transient**: New instance each time

RemoteFactory creates a new scope for each remote request.

## Testing with Service Injection

Inject mock services in tests:

<!-- snippet: service-injection-testing -->
<a id='snippet-service-injection-testing'></a>
```cs
public partial class ServiceInjectionTests
{
    [Fact]
    public async Task TestWithMockedRepository()
    {
        // Arrange - add test data to local scope's repository
        var scopes = SampleTestContainers.Scopes();
        var repository = scopes.local.GetRequiredService<IPersonRepository>();

        var testPerson = new PersonEntity
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User"
        };
        await repository.AddAsync(testPerson);

        var factory = scopes.local.GetRequiredService<IBasicServiceInjectionFactory>();

        // Act - Fetch returns populated instance
        var fetched = await factory.Fetch(testPerson.Id);

        // Assert
        Assert.Equal(testPerson.Id, fetched.Id);
        Assert.Equal("Test User", fetched.Data);
    }
}
```
<sup><a href='/src/docs/samples/ServiceInjectionSamples.cs#L316-L344' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Factory Operations](factory-operations.md) - All operation types
- [Authorization](authorization.md) - Inject auth services
- [ASP.NET Core Integration](aspnetcore-integration.md) - Server DI configuration
