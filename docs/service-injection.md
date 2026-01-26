# Service Injection

RemoteFactory integrates with ASP.NET Core dependency injection, allowing factory methods to receive services without serialization overhead.

## The [Service] Attribute

Mark parameters with `[Service]` to inject from the DI container:

<!-- snippet: service-injection-basic -->
<!--
SNIPPET REQUIREMENTS:
- Domain class with [Factory] attribute representing an Employee entity
- Properties: Id (Guid), Name (string), Department (string)
- [Create] constructor that initializes a new Employee
- [Remote][Fetch] method that takes employeeId (Guid) and [Service] IEmployeeRepository
- Fetch method retrieves employee by ID and populates properties from the entity
- Show that IEmployeeRepository is injected from DI, not serialized
- Context: Domain/Application layer
-->
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
<!--
SNIPPET REQUIREMENTS:
- Static partial class with [SuppressFactory] attribute for demonstration
- [Remote][Execute] method that processes a department transfer
- Value parameters: employeeId (Guid), newDepartmentId (Guid)
- Multiple [Service] parameters: IEmployeeRepository, IDepartmentRepository, IUserContext
- Method retrieves employee and department, then returns a summary string
- Include comment explaining this is for [Execute] pattern demonstration
- Context: Application layer command execution
-->
<!-- endSnippet -->

Parameter resolution:
- **Value parameters**: Deserialized from request
- **Service parameters**: Resolved from server DI container
- **CancellationToken**: Special handling (always optional, always last)

## Service Lifetime

Services are resolved from the server's DI scope:

<!-- snippet: service-injection-scoped -->
<!--
SNIPPET REQUIREMENTS:
- Interface IAuditContext with CorrelationId (Guid) property and LogAction(string) method
- Implementation AuditContext that generates CorrelationId on construction and stores actions in a list
- Static partial class with [SuppressFactory] for [Execute] demonstration
- [Remote][Execute] method that takes action (string) and [Service] IAuditContext
- Method logs the action and returns the CorrelationId
- Include comment about scoped services maintaining state within a request
- Context: Infrastructure layer - audit/logging service
-->
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
<!--
SNIPPET REQUIREMENTS:
- Interface ISalaryCalculator with Calculate(decimal baseSalary, decimal bonus) method returning decimal
- Simple implementation SalaryCalculator that adds baseSalary and bonus
- [Factory] class EmployeeCompensation with private readonly ISalaryCalculator field
- Properties: TotalCompensation (decimal)
- [Create] constructor that takes [Service] ISalaryCalculator and stores it
- Public method CalculateTotal(decimal baseSalary, decimal bonus) that uses the service
- Context: Domain layer - service injected at construction time
-->
<!-- endSnippet -->

Factory behavior:
- **Local Create**: Resolves services from local container
- **Remote Create**: Executes on server with server's services

## Server-Only Services

Some services exist only on the server (databases, file systems, secrets). Mark methods `[Remote]` to ensure server execution:

<!-- snippet: service-injection-server-only -->
<!--
SNIPPET REQUIREMENTS:
- Interface IEmployeeDatabase with ExecuteQueryAsync(string query) method
- Simple implementation EmployeeDatabase that simulates query execution
- [Factory] class EmployeeReport with QueryResult (string) property
- [Create] constructor with no parameters
- [Remote][Fetch] method that takes query (string) and [Service] IEmployeeDatabase
- Fetch sets QueryResult from the database service
- Include comment: "This service only exists on the server"
- Context: Infrastructure layer - database access only on server
-->
<!-- endSnippet -->

Without `[Remote]`, clients would call `Fetch()` locally and fail when resolving `IEmployeeDatabase`.

## Client-Side Service Injection

Services can be injected on the client for local operations:

<!-- snippet: service-injection-client -->
<!--
SNIPPET REQUIREMENTS:
- Interface INotificationService with Notify(string message) method
- Implementation NotificationService that stores messages in a list
- [Factory] class EmployeeNotifier with Notified (bool) property
- [Create] constructor that takes [Service] INotificationService
- Constructor calls Notify with "Employee created" message and sets Notified = true
- Include comment: service is available on both client and server
- Context: Application layer - client-side notification service
-->
<!-- endSnippet -->

This method runs locally on the client, accessing the client's DI container.

## RegisterMatchingName Helper

RemoteFactory provides a convention-based registration helper:

<!-- snippet: service-injection-matching-name -->
<!--
SNIPPET REQUIREMENTS:
- Static class EmployeeServiceRegistration with ConfigureServices method
- Takes IServiceCollection parameter
- Comment explaining the naming convention: IEmployeeRepository -> EmployeeRepository
- Call services.RegisterMatchingName with the assembly containing employee services
- Show that this registers all interface/implementation pairs following the convention
- Context: Server layer - DI registration in Program.cs or Startup
-->
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
<!--
SNIPPET REQUIREMENTS:
- Record EmployeeTransferResult(Guid EmployeeId, string TransferredBy, bool Cancelled)
- Static partial class with [SuppressFactory] for [Execute] demonstration
- [Remote][Execute] method _TransferEmployee with mixed parameters:
  - Value parameters: employeeId (Guid), newDepartmentId (Guid)
  - Service parameters: [Service] IEmployeeRepository, [Service] IUserContext
  - CancellationToken at the end
- Method checks cancellation, retrieves employee, returns EmployeeTransferResult
- Include inline comments labeling each parameter type (Value, Service, CancellationToken)
- Context: Application layer - command with multiple parameter types
-->
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
<!--
SNIPPET REQUIREMENTS:
- Interface IHttpContextAccessorWrapper with GetUserId() and GetCorrelationId() methods returning string?
- Implementation HttpContextAccessorWrapper that returns simulated values
- [Factory] class EmployeeContext with UserId and CorrelationId properties (string?)
- [Create] constructor with no parameters
- [Remote][Fetch] method that takes [Service] IHttpContextAccessorWrapper
- Fetch populates UserId and CorrelationId from the accessor
- Include comment: "Access HttpContext on server to get user info, headers, etc."
- Context: Server layer - accessing HTTP request context
-->
<!-- endSnippet -->

### IServiceProvider

Direct access to the service provider:

<!-- snippet: service-injection-serviceprovider -->
<!--
SNIPPET REQUIREMENTS:
- Static partial class with [SuppressFactory] for [Execute] demonstration
- [Remote][Execute] method _ResolveEmployeeServices that takes [Service] IServiceProvider
- Method dynamically resolves IEmployeeRepository and IUserContext from the provider
- Returns Task<bool> indicating whether both services resolved successfully
- Include comment: "Dynamically resolve services when needed"
- Context: Application layer - dynamic service resolution (use sparingly)
-->
<!-- endSnippet -->

Use sparingly. Prefer typed services.

## Transient vs Scoped vs Singleton

Service lifetimes behave as expected:

<!-- snippet: service-injection-lifetimes -->
<!--
SNIPPET REQUIREMENTS:
- Comment block explaining service lifetimes in RemoteFactory context:
  - Singleton: application lifetime, use for caches/configuration
  - Scoped: per request/operation, use for DbContext/unit of work
  - Transient: new instance each resolution
- Static class EmployeeServiceLifetimes with ConfigureServices method
- Register ISalaryCalculator as Singleton
- Register IAuditContext as Scoped
- Register INotificationService as Transient
- Use services from earlier snippets in this document
- Context: Server layer - DI configuration
-->
<!-- endSnippet -->

**Singleton**: Same instance across all requests
**Scoped**: Same instance within a request
**Transient**: New instance each time

RemoteFactory creates a new scope for each remote request.

## Testing with Service Injection

Register test doubles in your DI container:

<!-- snippet: service-injection-testing -->
<!--
SNIPPET REQUIREMENTS:
- Static class EmployeeTestServices with ConfigureTestServices method
- Comment: "Register test doubles instead of production services"
- Register in-memory implementations for testing
- InMemoryEmployeeRepository class implementing IEmployeeRepository:
  - Private Dictionary<Guid, EmployeeEntity> for storage
  - GetByIdAsync, AddAsync, GetAllAsync, UpdateAsync, DeleteAsync methods
  - SaveChangesAsync returns completed task
- TestUserContext class implementing IUserContext:
  - Public settable properties: UserId, Username, Roles, IsAuthenticated
  - IsInRole method checking Roles array
- Context: Test layer - in-memory doubles for unit/integration tests
-->
<!-- endSnippet -->

## Next Steps

- [Factory Operations](factory-operations.md) - All operation types
- [Authorization](authorization.md) - Inject auth services
- [ASP.NET Core Integration](aspnetcore-integration.md) - Server DI configuration
