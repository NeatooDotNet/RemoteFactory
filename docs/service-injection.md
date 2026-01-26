# Service Injection

RemoteFactory integrates with ASP.NET Core dependency injection, allowing factory methods to receive services without serialization overhead.

## The [Service] Attribute

Mark parameters with `[Service]` to inject from the DI container:

<!-- snippet: service-injection-basic -->
<a id='snippet-service-injection-basic'></a>
```cs
/// <summary>
/// Demonstrates basic [Service] parameter injection.
/// </summary>
[Factory]
public partial class EmployeeBasicService : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeBasicService() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [Service] marks parameters for DI injection.
    /// employeeId is serialized; repository is resolved from server DI.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L7-L41' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

When the factory calls `Fetch()`:
- **Client**: Serializes `employeeId` parameter only
- **Server**: Deserializes `employeeId`, resolves `IEmployeeRepository` from DI
- **Server**: Calls method with both parameters
- **Result**: Serialized and returned

`IEmployeeRepository` is never serialized or sent over HTTP.

## Parameter Rules

Service parameters can appear anywhere in the parameter list, but conventionally appear after value parameters:

<!-- snippet: service-injection-multiple -->
<a id='snippet-service-injection-multiple'></a>
```cs
/// <summary>
/// Demonstrates multiple service parameter injection.
/// </summary>
[Factory]
public partial class EmployeeMultipleServices : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeMultipleServices() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Multiple services injected for complex operations.
    /// All service parameters are resolved from server DI.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        [Service] IEmailService emailService,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;

        // Multiple services working together
        await emailService.SendAsync(
            entity.Email,
            "Welcome!",
            $"Welcome {FirstName}!",
            ct);

        await auditLog.LogAsync("Insert", Id, "Employee", $"Created {FirstName}", ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L43-L91' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-multiple' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates scoped service lifetime with factory operations.
/// </summary>
[Factory]
public partial class EmployeeScopedService : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeScopedService() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Scoped services are disposed when the request completes.
    /// Each remote call gets a fresh scope.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,
        [Service] IAuditLogService auditLog, // Scoped - disposed after request
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;

        // Scoped service records audit in same transaction scope
        await auditLog.LogAsync("Fetch", employeeId, "Employee", "Loaded", ct);

        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L93-L132' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-scoped' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates service injection in [Create] constructor.
/// </summary>
[Factory]
public partial class EmployeeWithDefaults
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public Guid DefaultDepartmentId { get; private set; }
    public DateTime HireDate { get; private set; }

    // Internal constructor allows generated serializer access
    internal EmployeeWithDefaults() { }

    /// <summary>
    /// Services injected during object creation.
    /// </summary>
    [Create]
    public static EmployeeWithDefaults Create(
        [Service] IDefaultValueProvider defaults)
    {
        return new EmployeeWithDefaults
        {
            Id = Guid.NewGuid(),
            DefaultDepartmentId = defaults.GetDefaultDepartmentId(),
            HireDate = defaults.GetDefaultHireDate()
        };
    }
}

/// <summary>
/// Provides default values for new entities.
/// </summary>
public interface IDefaultValueProvider
{
    Guid GetDefaultDepartmentId();
    DateTime GetDefaultHireDate();
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L134-L173' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-constructor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Factory behavior:
- **Local Create**: Resolves services from local container
- **Remote Create**: Executes on server with server's services

## Server-Only Services

Some services exist only on the server (databases, file systems, secrets). Mark methods `[Remote]` to ensure server execution:

<!-- snippet: service-injection-server-only -->
<a id='snippet-service-injection-server-only'></a>
```cs
/// <summary>
/// Demonstrates server-only services with [Remote] attribute.
/// </summary>
[Factory]
public partial class EmployeeServerOnly : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeServerOnly() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [Remote] ensures server execution where repository exists.
    /// Without [Remote], clients would fail resolving IEmployeeRepository.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository, // Server-only service
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L175-L208' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-server-only' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Without `[Remote]`, clients would call `Fetch()` locally and fail when resolving `IEmployeeDatabase`.

## Client-Side Service Injection

Services can be injected on the client for local operations:

<!-- snippet: service-injection-client -->
<a id='snippet-service-injection-client'></a>
```cs
/// <summary>
/// Demonstrates client-side service injection for local operations.
/// </summary>
[Factory]
public partial class EmployeeClientService
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string ClientInfo { get; private set; } = "";

    [Create]
    public EmployeeClientService() { Id = Guid.NewGuid(); }

    /// <summary>
    /// No [Remote] - runs locally on client.
    /// Uses platform-agnostic client service (ILogger).
    /// </summary>
    [Fetch]
    public void LoadFromCache(
        string cachedData,
        [Service] ILogger<EmployeeClientService> logger)
    {
        // Local operation using client-side logger
        logger.LogInformation("Loading employee from cache: {Data}", cachedData);
        FirstName = cachedData;
        ClientInfo = $"Loaded at {DateTime.UtcNow}";
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L210-L239' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-client' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This method runs locally on the client, accessing the client's DI container.

## RegisterMatchingName Helper

RemoteFactory provides a convention-based registration helper:

<!-- snippet: service-injection-matching-name -->
<a id='snippet-service-injection-matching-name'></a>
```cs
/// <summary>
/// RegisterMatchingName pattern for interface/implementation pairs.
/// </summary>
public class MatchingNameTests
{
    [Fact]
    public void RegisterMatchingName_ResolvesCorrectImplementation()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();

        // Act - IEmployeeRepository -> InMemoryEmployeeRepository
        var repository = scopes.local.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();

        // Assert - Correct implementation resolved
        Assert.IsType<InMemoryEmployeeRepository>(repository);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L501-L521' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-matching-name' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Mixing local and remote methods with different services.
/// </summary>
[Factory]
public partial class EmployeeMixedServices : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastModified { get; private set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeMixedServices() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Local method - uses client-side logger (no [Remote]).
    /// </summary>
    [Fetch]
    public void LoadDefaults([Service] ILogger<EmployeeMixedServices> logger)
    {
        logger.LogInformation("Initializing with defaults");
        FirstName = "New Employee";
        LastModified = DateTime.UtcNow.ToString("o");
    }

    /// <summary>
    /// Remote method - uses server-side repository ([Remote]).
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L241-L302' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-mixed' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates IHttpContextAccessor injection for HTTP context access.
/// Use this to access request headers, user claims, or other HTTP-specific data.
/// </summary>
[Factory]
public partial class EmployeeHttpContext : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string CreatedBy { get; private set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeHttpContext() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Access HTTP context information via IHttpContextAccessor.
    /// Available only in server-side methods.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        [Service] Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,
        CancellationToken ct)
    {
        // Access HTTP context (null in non-HTTP scenarios like testing)
        var httpContext = httpContextAccessor.HttpContext;

        // Get user identity from claims
        CreatedBy = httpContext?.User?.Identity?.Name ?? "system";

        // Access request headers
        var correlationId = httpContext?.Request?.Headers["X-Correlation-ID"].FirstOrDefault();

        // Access other request information
        var userAgent = httpContext?.Request?.Headers.UserAgent.FirstOrDefault();

        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToUpperInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs#L447-L499' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-httpcontext' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IServiceProvider

Direct access to the service provider:

<!-- snippet: service-injection-serviceprovider -->
<a id='snippet-service-injection-serviceprovider'></a>
```cs
/// <summary>
/// Demonstrates IServiceProvider injection for dynamic resolution.
/// </summary>
[Factory]
public partial class EmployeeServiceProvider : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeServiceProvider() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Use IServiceProvider sparingly - prefer typed services.
    /// Useful for conditional or plugin-based service resolution.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        // Prefer typed [Service] parameters when possible
        // Use IServiceProvider only for dynamic scenarios
        var repository = serviceProvider.GetService(typeof(IEmployeeRepository)) as IEmployeeRepository;
        if (repository == null)
            throw new InvalidOperationException("IEmployeeRepository not registered");

        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L308-L351' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-serviceprovider' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use sparingly. Prefer typed services.

## Transient vs Scoped vs Singleton

Service lifetimes behave as expected:

<!-- snippet: service-injection-lifetimes -->
<a id='snippet-service-injection-lifetimes'></a>
```cs
/// <summary>
/// Demonstrates service lifetime scoping.
/// </summary>
public class ServiceLifetimeTests
{
    [Fact]
    public void ScopedServices_SameWithinScope()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();

        // Act - Get service twice within same scope
        var repo1 = scopes.local.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();
        var repo2 = scopes.local.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();

        // Assert - Same instance within scope
        Assert.Same(repo1, repo2);
    }

    [Fact]
    public void ScopedServices_DifferentAcrossScopes()
    {
        // Arrange - Create two separate scopes
        var scopes1 = TestClientServerContainers.CreateScopes();
        var scopes2 = TestClientServerContainers.CreateScopes();

        // Act
        var repo1 = scopes1.local.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();
        var repo2 = scopes2.local.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();

        // Assert - Different instances across scopes
        Assert.NotSame(repo1, repo2);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L460-L499' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-lifetimes' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Testing service injection patterns.
/// </summary>
public class ServiceInjectionTests
{
    [Fact]
    public void ServiceParameter_ResolvedFromDI()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();

        // Services are resolved from DI container
        var repository = scopes.local.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();
        var emailService = scopes.local.ServiceProvider
            .GetRequiredService<IEmailService>();
        var auditLog = scopes.local.ServiceProvider
            .GetRequiredService<IAuditLogService>();

        // Assert - Services are not null
        Assert.NotNull(repository);
        Assert.NotNull(emailService);
        Assert.NotNull(auditLog);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L432-L458' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Factory Operations](factory-operations.md) - All operation types
- [Authorization](authorization.md) - Inject auth services
- [ASP.NET Core Integration](aspnetcore-integration.md) - Server DI configuration
