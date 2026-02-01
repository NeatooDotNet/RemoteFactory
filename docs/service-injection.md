# Service Injection

RemoteFactory integrates with ASP.NET Core dependency injection, allowing factory methods to receive services without serialization overhead.

## The [Service] Attribute

Mark parameters with `[Service]` to inject from the DI container:

<!-- snippet: service-injection-basic -->
<a id='snippet-service-injection-basic'></a>
```cs
/// <summary>
/// Employee aggregate demonstrating basic [Service] injection.
/// </summary>
[Factory]
public partial class EmployeeBasicService
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Department { get; private set; } = "";

    [Create]
    public EmployeeBasicService()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches employee data using an injected repository.
    /// IEmployeeRepository is injected from DI, not serialized.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null) return false;

        Id = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        Department = entity.Position;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L6-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-basic' title='Start of snippet'>anchor</a></sup>
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
<!-- endSnippet -->

When the client fetches an `OrderLineList` via a remote call:
1. Server executes `Fetch()` with `IOrderLineFactory` injected
2. Server stores the factory in `_lineFactory`
3. Response serializes the list (but NOT the factory reference)
4. Client deserializes the list—`_lineFactory` is null
5. Calling `AddLine()` on the client throws `NullReferenceException`

**Solution: Use constructor injection for services needed on both sides**

<!-- snippet: serialization-caveat-fixed -->
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
/// <summary>
/// Command demonstrating multiple service injection in [Execute] operation.
/// </summary>
[SuppressFactory]
public static partial class DepartmentTransferCommand
{
    /// <summary>
    /// Processes a department transfer with multiple injected services.
    /// </summary>
    [Remote, Execute]
    private static async Task<string> _ProcessTransfer(
        Guid employeeId,
        Guid newDepartmentId,
        [Service] IEmployeeRepository employeeRepo,
        [Service] IDepartmentRepository departmentRepo,
        [Service] IUserContext userContext)
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId);
        var department = await departmentRepo.GetByIdAsync(newDepartmentId);

        if (employee == null || department == null)
            return "Transfer failed: Employee or department not found";

        var employeeName = $"{employee.FirstName} {employee.LastName}";
        return $"Transfer of {employeeName} to {department.Name} by {userContext.Username}";
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L89-L117' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-multiple' title='Start of snippet'>anchor</a></sup>
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
/// Audit context interface for tracking operations within a request scope.
/// </summary>
public interface IAuditContext
{
    Guid CorrelationId { get; }
    void LogAction(string action);
}

/// <summary>
/// Scoped audit context that maintains state within a request.
/// </summary>
public class AuditContext : IAuditContext
{
    private readonly List<string> _actions = new();

    public Guid CorrelationId { get; } = Guid.NewGuid();

    public void LogAction(string action)
    {
        _actions.Add($"[{DateTime.UtcNow:O}] {action}");
    }
}

/// <summary>
/// Command demonstrating scoped service injection.
/// </summary>
[SuppressFactory]
public static partial class AuditExample
{
    /// <summary>
    /// Logs an action and returns the correlation ID.
    /// </summary>
    /// <remarks>
    /// Scoped services maintain state within a request - all operations
    /// in the same request share the same CorrelationId.
    /// </remarks>
    [Remote, Execute]
    private static Task<Guid> _LogEmployeeAction(string action, [Service] IAuditContext auditContext)
    {
        auditContext.LogAction(action);
        return Task.FromResult(auditContext.CorrelationId);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L119-L164' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-scoped' title='Start of snippet'>anchor</a></sup>
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
/// Service for calculating employee salary.
/// </summary>
public interface ISalaryCalculator
{
    decimal Calculate(decimal baseSalary, decimal bonus);
}

/// <summary>
/// Simple salary calculator implementation.
/// </summary>
public class SalaryCalculator : ISalaryCalculator
{
    public decimal Calculate(decimal baseSalary, decimal bonus)
    {
        return baseSalary + bonus;
    }
}

/// <summary>
/// Employee compensation demonstrating constructor service injection.
/// </summary>
[Factory]
public partial class EmployeeCompensation
{
    private readonly ISalaryCalculator _calculator;

    public decimal TotalCompensation { get; private set; }

    /// <summary>
    /// Constructor with service injection.
    /// ISalaryCalculator is resolved from DI when the factory creates the instance.
    /// </summary>
    [Create]
    public EmployeeCompensation([Service] ISalaryCalculator calculator)
    {
        _calculator = calculator;
    }

    public void CalculateTotal(decimal baseSalary, decimal bonus)
    {
        TotalCompensation = _calculator.Calculate(baseSalary, bonus);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L166-L211' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-constructor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Factory behavior:
- **Local Create**: Resolves services from local container
- **Remote Create**: Executes on server with server's services

## Server-Only Services

Method-injected services are typically server-only (databases, file systems, secrets). This is the common case—most factory methods have these but do NOT need `[Remote]`.

<!-- snippet: service-injection-server-only -->
<a id='snippet-service-injection-server-only'></a>
```cs
/// <summary>
/// Interface for database access (server-only service).
/// </summary>
public interface IEmployeeDatabase
{
    Task<string> ExecuteQueryAsync(string query);
}

/// <summary>
/// Simple implementation for demonstration.
/// </summary>
public class EmployeeDatabase : IEmployeeDatabase
{
    public Task<string> ExecuteQueryAsync(string query)
    {
        // Simulated query execution
        return Task.FromResult($"Query result for: {query}");
    }
}

/// <summary>
/// Employee report demonstrating server-only service injection.
/// </summary>
[Factory]
public partial class EmployeeReport
{
    public string QueryResult { get; private set; } = "";

    [Create]
    public EmployeeReport()
    {
    }

    /// <summary>
    /// Fetches report data from the database.
    /// </summary>
    /// <remarks>
    /// This service only exists on the server - [Remote] ensures the method runs there.
    /// </remarks>
    [Remote, Fetch]
    public async Task Fetch(string query, [Service] IEmployeeDatabase database)
    {
        QueryResult = await database.ExecuteQueryAsync(query);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L41-L87' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-server-only' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`[Remote]` is only needed here because `Fetch` is an **entry point from the client**. Methods called from server-side code (after already crossing the boundary) don't need `[Remote]` even with server-only services.

## Client-Side Service Injection

Services can be injected on the client for local operations:

<!-- snippet: service-injection-client -->
<a id='snippet-service-injection-client'></a>
```cs
/// <summary>
/// Service for client-side notifications.
/// </summary>
public interface INotificationService
{
    void Notify(string message);
}

/// <summary>
/// Simple notification service implementation.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly List<string> _messages = new();

    public void Notify(string message)
    {
        _messages.Add(message);
    }
}

/// <summary>
/// Employee notifier with constructor-injected client service.
/// </summary>
[Factory]
public partial class EmployeeNotifier
{
    public bool Notified { get; private set; }

    /// <summary>
    /// Constructor with client-side service injection.
    /// </summary>
    /// <remarks>
    /// This service is available on both client and server.
    /// </remarks>
    [Create]
    public EmployeeNotifier([Service] INotificationService notificationService)
    {
        notificationService.Notify("Employee created");
        Notified = true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L213-L256' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-client' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This method runs locally on the client, accessing the client's DI container.

## RegisterMatchingName Helper

RemoteFactory provides a convention-based registration helper:

<!-- snippet: service-injection-matching-name -->
<a id='snippet-service-injection-matching-name'></a>
```cs
/// <summary>
/// Service registration using the RegisterMatchingName convention.
/// </summary>
public static class EmployeeServiceRegistration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // RegisterMatchingName registers interfaces to their implementations
        // using the naming convention: IEmployeeRepository -> EmployeeRepository
        // All matches are registered with Transient lifetime
        services.RegisterMatchingName(typeof(IEmployeeRepository).Assembly);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/ServiceRegistrationSamples.cs#L8-L22' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-matching-name' title='Start of snippet'>anchor</a></sup>
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
/// Result of an employee transfer operation.
/// </summary>
public record EmployeeTransferResult(Guid EmployeeId, string TransferredBy, bool Cancelled);

/// <summary>
/// Command demonstrating mixed parameter types.
/// </summary>
[SuppressFactory]
public static partial class EmployeeTransferCommand
{
    /// <summary>
    /// Transfers an employee to a new department.
    /// </summary>
    [Remote, Execute]
    private static async Task<EmployeeTransferResult> _TransferEmployee(
        Guid employeeId,         // Value: serialized
        Guid newDepartmentId,    // Value: serialized
        [Service] IEmployeeRepository repository,  // Service: injected
        [Service] IUserContext userContext,        // Service: injected
        CancellationToken cancellationToken)       // CancellationToken: passed through
    {
        cancellationToken.ThrowIfCancellationRequested();

        var employee = await repository.GetByIdAsync(employeeId, cancellationToken);
        if (employee == null)
            return new EmployeeTransferResult(employeeId, userContext.Username, true);

        employee.DepartmentId = newDepartmentId;
        await repository.UpdateAsync(employee, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new EmployeeTransferResult(employeeId, userContext.Username, false);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L258-L294' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-mixed' title='Start of snippet'>anchor</a></sup>
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
/// Wrapper for accessing HTTP context information.
/// </summary>
public interface IHttpContextAccessorWrapper
{
    string? GetUserId();
    string? GetCorrelationId();
}

/// <summary>
/// Simple implementation for demonstration.
/// </summary>
public class HttpContextAccessorWrapper : IHttpContextAccessorWrapper
{
    public string? GetUserId() => "user-123";
    public string? GetCorrelationId() => Guid.NewGuid().ToString();
}

/// <summary>
/// Employee context demonstrating HTTP context accessor injection.
/// </summary>
[Factory]
public partial class EmployeeContext
{
    public string? UserId { get; private set; }
    public string? CorrelationId { get; private set; }

    [Create]
    public EmployeeContext()
    {
    }

    /// <summary>
    /// Fetches context information from the HTTP request.
    /// </summary>
    /// <remarks>
    /// Access HttpContext on server to get user info, headers, etc.
    /// </remarks>
    [Remote, Fetch]
    public Task Fetch([Service] IHttpContextAccessorWrapper accessor)
    {
        UserId = accessor.GetUserId();
        CorrelationId = accessor.GetCorrelationId();
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L296-L343' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-httpcontext' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IServiceProvider

Direct access to the service provider:

<!-- snippet: service-injection-serviceprovider -->
<a id='snippet-service-injection-serviceprovider'></a>
```cs
/// <summary>
/// Command demonstrating IServiceProvider injection.
/// </summary>
[SuppressFactory]
public static partial class ServiceResolutionExample
{
    /// <summary>
    /// Dynamically resolves services from the provider.
    /// </summary>
    /// <remarks>
    /// Dynamically resolve services when needed - use sparingly.
    /// </remarks>
    [Remote, Execute]
    private static Task<bool> _ResolveEmployeeServices([Service] IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetService(typeof(IEmployeeRepository));
        var userContext = serviceProvider.GetService(typeof(IUserContext));

        return Task.FromResult(repository != null && userContext != null);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Services/ServiceInjectionSamples.cs#L345-L367' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-serviceprovider' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use sparingly. Prefer typed services.

## Transient vs Scoped vs Singleton

Service lifetimes behave as expected:

<!-- snippet: service-injection-lifetimes -->
<a id='snippet-service-injection-lifetimes'></a>
```cs
/// <summary>
/// Service registration demonstrating different lifetimes.
/// </summary>
/// <remarks>
/// - Singleton: same instance across all requests, use for caches/configuration
/// - Scoped: same instance within a request, use for DbContext/unit of work
/// - Transient: new instance each resolution
/// </remarks>
public static class EmployeeServiceLifetimes
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Singleton: same instance for entire application lifetime
        services.AddSingleton<ISalaryCalculator, SalaryCalculator>();

        // Scoped: same instance within a single request
        services.AddScoped<IAuditContext, AuditContext>();

        // Transient: new instance each time requested
        services.AddTransient<INotificationService, NotificationService>();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/ServiceRegistrationSamples.cs#L24-L47' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-lifetimes' title='Start of snippet'>anchor</a></sup>
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
/// Test service registration for unit and integration tests.
/// </summary>
public static class EmployeeTestServices
{
    /// <summary>
    /// Register test doubles instead of production services.
    /// </summary>
    public static void ConfigureTestServices(IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddScoped<IUserContext, TestUserContext>();
    }
}

/// <summary>
/// In-memory repository for testing.
/// </summary>
public class InMemoryEmployeeRepository : IEmployeeRepository
{
    private readonly Dictionary<Guid, EmployeeEntity> _employees = new();

    public Task<EmployeeEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _employees.TryGetValue(id, out var employee);
        return Task.FromResult(employee);
    }

    public Task<List<EmployeeEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_employees.Values.ToList());
    }

    public Task<List<EmployeeEntity>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken ct = default)
    {
        var employees = _employees.Values.Where(e => e.DepartmentId == departmentId).ToList();
        return Task.FromResult(employees);
    }

    public Task AddAsync(EmployeeEntity entity, CancellationToken ct = default)
    {
        _employees[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EmployeeEntity entity, CancellationToken ct = default)
    {
        _employees[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _employees.Remove(id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test user context with configurable properties.
/// </summary>
public class TestUserContext : IUserContext
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "TestUser";
    public IReadOnlyList<string> Roles { get; set; } = new[] { "User" };
    public bool IsAuthenticated { get; set; } = true;

    public bool IsInRole(string role) => Roles.Contains(role);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/ServiceRegistrationSamples.cs#L49-L125' title='Snippet source file'>snippet source</a> | <a href='#snippet-service-injection-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Client/Server Container Testing

For integration tests that validate remote call serialization without HTTP, use the **ClientServerContainers** pattern:

<!-- snippet: clientserver-container-setup -->
<!-- endSnippet -->

Example test using local (Logical) mode:

<!-- snippet: clientserver-container-usage -->
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
