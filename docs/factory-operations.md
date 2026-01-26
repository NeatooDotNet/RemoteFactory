# Factory Operations

RemoteFactory supports seven operation types, each mapping to common data access patterns: Create, Fetch, Insert, Update, Delete, Execute, and Event.

## Overview

| Operation | Purpose | Typical Use | Return Handling |
|-----------|---------|-------------|-----------------|
| `[Create]` | Create new instances | Constructors, factory methods | Instance or null |
| `[Fetch]` | Load existing data | Data retrieval | bool/void for success, or instance |
| `[Insert]` | Persist new entities | First save | void or bool |
| `[Update]` | Persist changes | Subsequent saves | void or bool |
| `[Delete]` | Remove entities | Deletion | void or bool |
| `[Execute]` | Business operations | Commands, queries | Any type |
| `[Event]` | Fire-and-forget | Domain events | void or Task (always returns Task) |

## Create Operation

Creates new instances via constructors or static factory methods.

### Constructor-based Creation

<!-- snippet: operations-create-constructor -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute and constructor-based [Create]
- Properties: Guid Id, string FirstName, string LastName, DateTime HireDate
- Constructor sets Id = Guid.NewGuid() and HireDate = DateTime.UtcNow
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates simplest form of factory creation via parameterless constructor
-->
<!-- endSnippet -->

Generated factory interface:
```csharp
public interface IEmployeeFactory
{
    Employee Create(CancellationToken cancellationToken = default);
}
```

### Static Factory Method

<!-- snippet: operations-create-static -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute and static factory method [Create]
- Properties: Guid Id, string EmployeeNumber, string FirstName, string LastName, decimal Salary
- Private parameterless constructor
- Static Create method accepting employeeNumber, firstName, lastName, initialSalary
- Validate employeeNumber is not null/whitespace, throw ArgumentException if invalid
- Generate Id and format EmployeeNumber to uppercase
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates validation and transformation in static factory method
-->
<!-- endSnippet -->

Generated factory interface:
```csharp
public interface IEmployeeFactory
{
    Employee Create(string employeeNumber, string firstName, string lastName, decimal initialSalary, CancellationToken cancellationToken = default);
}
```

### Return Value Handling

Create methods support multiple return types:

<!-- snippet: operations-create-return-types -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute demonstrating three Create patterns
- Properties: Guid Id, string FirstName, string LastName, bool Initialized
- Pattern 1: Constructor [Create] - sets Id = Guid.NewGuid()
- Pattern 2: Instance method [Create] void Initialize(string firstName, string lastName) - sets names and Initialized = true
- Pattern 3: Static method [Create] returning Employee CreateWithDefaults() - returns new instance with default values
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates multiple Create method patterns and return type handling
-->
<!-- endSnippet -->

Factory return types:
- **void**: Returns the instance
- **bool**: Returns instance if true, null if false
- **Task**: Returns instance after await
- **Task\<bool\>**: Returns instance if true, null if false
- **Task\<T\>**: Returns the T instance
- **T**: Returns the instance

## Fetch Operation

Loads data into an existing instance.

### Instance Method Fetch

<!-- snippet: operations-fetch-instance -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute and instance Fetch method
- Properties: Guid Id, string EmployeeNumber, string FirstName, string LastName, decimal Salary, bool IsNew = true
- Parameterless [Create] constructor
- [Remote][Fetch] async Task Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
- Fetch loads from repository, throws InvalidOperationException if not found
- Maps entity properties to aggregate, sets IsNew = false
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates standard fetch pattern with repository injection
-->
<!-- endSnippet -->

Generated factory interface:
```csharp
public interface IEmployeeFactory
{
    Employee Create(CancellationToken cancellationToken = default);
    Task<Employee> Fetch(Guid employeeId, CancellationToken cancellationToken = default);
}
```

The factory creates a new instance, calls Fetch, and returns the instance. If Fetch throws, the exception propagates.

### Parameters and Return Types

Fetch supports:
- **void**: Returns non-nullable instance, throws on error
- **bool**: True = success, false = not found (factory returns null, generated signature is nullable)
- **Task**: Returns non-nullable instance, throws on error
- **Task\<bool\>**: True = success, false = not found (factory returns null, generated signature is nullable)

<!-- snippet: operations-fetch-bool-return -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute and bool-returning Fetch
- Properties: Guid Id, string EmployeeNumber, string FirstName, string LastName
- Parameterless [Create] constructor
- [Remote][Fetch] async Task<bool> TryFetch(Guid employeeId, [Service] IEmployeeRepository repository)
- Returns false if entity not found (no exception)
- Returns true and maps properties if found
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates nullable return pattern for optional fetch scenarios
-->
<!-- endSnippet -->

Generated factory interface for bool return:
```csharp
public interface IEmployeeFactory
{
    Employee Create(CancellationToken cancellationToken = default);
    Task<Employee?> TryFetch(Guid employeeId, CancellationToken cancellationToken = default);
}
```

Note the nullable return type when Fetch returns bool.

## Insert, Update, Delete Operations

Write operations for persisting changes.

### Insert

<!-- snippet: operations-insert -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute implementing IFactorySaveMeta
- Properties: Guid Id, string EmployeeNumber, string FirstName, string LastName, decimal Salary, bool IsNew = true, bool IsDeleted
- [Create] constructor generates Id and EmployeeNumber (format: EMP-yyyyMMdd-{8-char guid})
- [Remote][Insert] async Task Insert([Service] IEmployeeRepository repository)
- Creates new EmployeeEntity, maps properties, sets Created/Modified timestamps
- Calls repository.AddAsync and repository.SaveChangesAsync
- Sets IsNew = false after successful insert
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates insert operation with entity mapping and timestamp handling
-->
<!-- endSnippet -->

### Update

<!-- snippet: operations-update -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute implementing IFactorySaveMeta
- Properties: Guid Id, string EmployeeNumber, string FirstName, string LastName, decimal Salary, string Department, bool IsNew = true, bool IsDeleted
- [Create] constructor generates Id
- [Remote][Fetch] async Task<bool> Fetch with repository, maps all properties, sets IsNew = false
- [Remote][Update] async Task Update([Service] IEmployeeRepository repository)
- Update fetches existing entity, throws if not found
- Updates mutable properties (FirstName, LastName, Salary, Department) and Modified timestamp
- Calls repository.UpdateAsync and repository.SaveChangesAsync
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates update operation with optimistic concurrency pattern
-->
<!-- endSnippet -->

### Delete

<!-- snippet: operations-delete -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute implementing IFactorySaveMeta
- Properties: Guid Id, bool IsNew = true, bool IsDeleted
- [Create] constructor generates Id
- [Remote][Fetch] async Task<bool> Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
- Sets Id and IsNew = false on successful fetch, returns false if not found
- [Remote][Delete] async Task Delete([Service] IEmployeeRepository repository)
- Calls repository.DeleteAsync(Id) and repository.SaveChangesAsync
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates delete operation pattern
-->
<!-- endSnippet -->

Multiple attributes on one method:

<!-- snippet: operations-insert-update -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute implementing IFactorySaveMeta
- Properties: Guid Id, string EmployeeNumber, string FirstName, string LastName, decimal Salary, bool IsNew = true, bool IsDeleted
- [Create] constructor generates Id and EmployeeNumber
- [Remote][Insert, Update] async Task Upsert([Service] IEmployeeRepository repository)
- Checks if entity exists via repository.GetByIdAsync
- If null: creates new EmployeeEntity with all properties, Created/Modified timestamps, calls AddAsync
- If exists: updates mutable properties and Modified timestamp, calls UpdateAsync
- Calls SaveChangesAsync and sets IsNew = false
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates combined Insert/Update (upsert) pattern
-->
<!-- endSnippet -->

Generated factory interfaces include methods that operate on the instance:
```csharp
public interface IEmployeeInsertFactory
{
    Employee Create(CancellationToken cancellationToken = default);
    Task Insert(Employee instance, CancellationToken cancellationToken = default);
}

public interface IEmployeeUpdateFactory
{
    Employee Create(CancellationToken cancellationToken = default);
    Task<Employee?> Fetch(Guid employeeId, CancellationToken cancellationToken = default);
    Task Update(Employee instance, CancellationToken cancellationToken = default);
}

public interface IEmployeeDeleteFactory
{
    Employee Create(CancellationToken cancellationToken = default);
    Task<Employee?> Fetch(Guid employeeId, CancellationToken cancellationToken = default);
    Task Delete(Employee instance, CancellationToken cancellationToken = default);
}
```

Return value handling:
- **void**: Operation succeeded
- **bool**: True = success, false = not authorized or not found
- **Task**: Operation succeeded after await
- **Task\<bool\>**: True = success, false = not authorized or not found

## Execute Operation

Business operations that don't fit Create/Fetch/Write patterns.

<!-- snippet: operations-execute -->
<!--
SNIPPET REQUIREMENTS:
- Record type: PromotionResult(Guid EmployeeId, bool IsApproved, string NewTitle, decimal NewSalary)
- Static partial class EmployeePromotionCommand with [SuppressFactory] attribute (nested class pattern)
- [Remote][Execute] private static async Task<PromotionResult> _PromoteEmployee(Guid employeeId, string newTitle, decimal salaryIncrease, [Service] IEmployeeRepository repository)
- Fetches employee, throws if not found
- Updates employee title and salary, sets Modified timestamp
- Calls UpdateAsync and SaveChangesAsync
- Returns PromotionResult with success details
- Context: Application layer production code
- Domain: Employee Management
- Demonstrates Execute operation for business commands
-->
<!-- endSnippet -->

Generated delegate (not a factory interface):
```csharp
// Method: _PromoteEmployee -> Delegate: PromoteEmployee (underscore prefix removed)
public static partial class EmployeePromotionCommand
{
    public delegate Task<PromotionResult> PromoteEmployee(
        Guid employeeId,
        string newTitle,
        decimal salaryIncrease,
        CancellationToken cancellationToken = default);
}
```

Execute operations generate delegates registered in DI. The delegate name is derived from the method name with underscore prefix removed. They can return any type and accept any parameters.

### Command Pattern

<!-- snippet: operations-execute-command -->
<!--
SNIPPET REQUIREMENTS:
- Record type: TransferResult(Guid EmployeeId, Guid NewDepartmentId, DateTime TransferDate, bool Success)
- Static partial class EmployeeTransferCommand with [SuppressFactory] attribute (nested class pattern)
- [Remote][Execute] private static async Task<TransferResult> _TransferEmployee(Guid employeeId, Guid newDepartmentId, DateTime effectiveDate, [Service] IEmployeeRepository employeeRepo, [Service] IDepartmentRepository departmentRepo)
- Fetches employee and validates exists
- Validates employee is in Active status, throws InvalidOperationException if not
- Updates employee's department and Modified timestamp
- Calls UpdateAsync and SaveChangesAsync
- Returns TransferResult with transfer details
- Context: Application layer production code
- Domain: Employee Management
- Demonstrates command pattern with multiple service dependencies and business validation
-->
<!-- endSnippet -->

## Event Operation

Fire-and-forget operations with scope isolation.

Events run asynchronously without blocking the caller. They execute in a separate DI scope for transactional independence.

<!-- snippet: operations-event -->
<!--
SNIPPET REQUIREMENTS:
- Partial class EmployeeEventHandler with [SuppressFactory] attribute (nested class pattern)
- Properties: Guid EmployeeId
- [Create] constructor
- [Event] async Task SendWelcomeEmail(Guid employeeId, string employeeEmail, [Service] IEmailService emailService, CancellationToken ct)
- Calls emailService.SendAsync with email, subject "Welcome to the Team", and body containing employeeId
- Context: Application layer production code
- Domain: Employee Management
- Demonstrates fire-and-forget event pattern for notifications
-->
<!-- endSnippet -->

Generated delegate (not a factory interface):
```csharp
// Method: SendWelcomeEmail -> Delegate: SendWelcomeEmailEvent (Event suffix added)
public partial class EmployeeEventHandler
{
    // CancellationToken is NOT included in delegate signature - framework provides it
    public delegate Task SendWelcomeEmailEvent(
        Guid employeeId,
        string employeeEmail);
}
```

The delegate name is the method name with "Event" suffix appended. CancellationToken is required in the method signature but excluded from the generated delegate (the framework provides ApplicationStopping token). Both void and Task methods generate Task-returning delegates.

Key characteristics:
- **Scope isolation**: New DI scope per event
- **Fire-and-forget**: Caller doesn't wait for completion
- **Graceful shutdown**: EventTracker waits for pending events
- **CancellationToken required**: Must be last parameter, receives ApplicationStopping

### EventTracker

Track event completion for testing or shutdown:

<!-- snippet: operations-event-tracker -->
<!--
SNIPPET REQUIREMENTS:
- Comment-based documentation snippet (no actual code)
- Show IEventTracker usage pattern:
  - Fire event: _ = sendWelcomeEmail(employeeId, employeeEmail);
  - Wait for all pending events: await eventTracker.WaitAllAsync();
  - Check pending count: eventTracker.PendingCount;
- Context: Usage documentation
- Domain: Employee Management
- Demonstrates EventTracker API for testing and graceful shutdown
-->
<!-- endSnippet -->

Return value handling:
- **void**: Converted to Task automatically
- **Task**: Tracked by EventTracker

## Remote Attribute

Marks methods that execute on the server. Without `[Remote]`, methods execute locally.

<!-- snippet: operations-remote -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute demonstrating [Remote] attribute
- Properties: string Result
- [Create] constructor
- [Remote][Fetch] Task FetchFromServer(string query, [Service] IEmployeeRepository repository)
- Method sets Result to indicate server-side execution with the query value
- Include comment explaining this code runs on the server
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates Remote attribute for server-side execution
-->
<!-- endSnippet -->

When a factory is registered with `NeatooFactory.Remote`:
1. Factory method (e.g., `Fetch()`) routes to `RemoteFetch()`
2. Parameters serialized
3. HTTP POST to `/api/neatoo`
4. Server executes method with injected services
5. Response serialized and returned

When a factory is registered with `NeatooFactory.Logical` or `NeatooFactory.Server`:
- Factory method routes to `LocalFetch()`
- Direct method execution
- No serialization, no HTTP call

Use `[Remote]` when:
- Method requires server-only services (database, file system)
- Method performs operations not allowed on client
- Method accesses sensitive data

## Lifecycle Hooks

Interfaces for operation lifecycle:

### IFactoryOnStart / IFactoryOnStartAsync

Called before the operation executes. Use `IFactoryOnStartAsync` for async validation or preparation:

<!-- snippet: operations-lifecycle-onstart -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute implementing IFactoryOnStart
- Properties: Guid Id, bool OnStartCalled, FactoryOperation? LastOperation
- [Create] constructor generates Id
- FactoryStart(FactoryOperation factoryOperation) implementation:
  - Sets OnStartCalled = true and LastOperation = factoryOperation
  - Validates: if Delete operation and Id == Guid.Empty, throw InvalidOperationException
- Include comment explaining pre-operation validation use case
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates IFactoryOnStart lifecycle hook for pre-operation validation
-->
<!-- endSnippet -->

### IFactoryOnComplete / IFactoryOnCompleteAsync

Called after successful operation. Use `IFactoryOnCompleteAsync` for async post-processing:

<!-- snippet: operations-lifecycle-oncomplete -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute implementing IFactoryOnComplete
- Properties: Guid Id, bool OnCompleteCalled, FactoryOperation? CompletedOperation
- [Create] constructor generates Id
- FactoryComplete(FactoryOperation factoryOperation) implementation:
  - Sets OnCompleteCalled = true and CompletedOperation = factoryOperation
  - Include comment about post-operation logic: logging, notifications, etc.
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates IFactoryOnComplete lifecycle hook for post-operation processing
-->
<!-- endSnippet -->

### IFactoryOnCancelled / IFactoryOnCancelledAsync

Called when operation is cancelled via OperationCanceledException. Use `IFactoryOnCancelledAsync` for async cleanup:

<!-- snippet: operations-lifecycle-oncancelled -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute implementing IFactoryOnCancelled
- Properties: Guid Id, bool OnCancelledCalled, FactoryOperation? CancelledOperation
- [Create] constructor generates Id
- FactoryCancelled(FactoryOperation factoryOperation) implementation:
  - Sets OnCancelledCalled = true and CancelledOperation = factoryOperation
  - Include comment about cleanup logic when operation was cancelled
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates IFactoryOnCancelled lifecycle hook for cancellation handling
-->
<!-- endSnippet -->

Lifecycle execution order:
1. `FactoryStart()` or `FactoryStartAsync()`
2. Operation method executes
3. `FactoryComplete()` or `FactoryCompleteAsync()` (if successful)
4. `FactoryCancelled()` or `FactoryCancelledAsync()` (if cancelled)

## CancellationToken Support

All factory methods accept an optional CancellationToken:

<!-- snippet: operations-cancellation -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute demonstrating CancellationToken usage
- Properties: Guid Id, bool Completed
- [Create] constructor generates Id
- [Remote][Fetch] async Task Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken cancellationToken)
- Check cancellation before starting: cancellationToken.ThrowIfCancellationRequested()
- Pass token to async repository call
- Check cancellation during processing: if (cancellationToken.IsCancellationRequested) return
- Set Id and Completed = true on success
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates proper CancellationToken propagation and checking
-->
<!-- endSnippet -->

Generated factory methods automatically include CancellationToken:
```csharp
public interface IEmployeeFactory
{
    Employee Create(CancellationToken cancellationToken = default);
    Task<Employee?> Fetch(Guid id, CancellationToken cancellationToken = default);
}
```

CancellationToken is:
- Automatically passed to async operation methods
- Linked to HttpContext.RequestAborted on server
- Linked to ApplicationStopping for graceful shutdown
- Triggers IFactoryOnCancelled when fired

## Method Parameters

Factory methods support:

**Value parameters**: Serialized and sent to server
<!-- snippet: operations-params-value -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute demonstrating value parameters
- Properties: int YearsOfService, string Department, DateTime ReviewDate, decimal BonusAmount
- [Create] constructor
- [Remote][Fetch] Task Fetch(int yearsOfService, string department, DateTime reviewDate, decimal bonusAmount)
- Maps all parameters to properties
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates various serializable value parameter types
-->
<!-- endSnippet -->

**Service parameters**: Injected from DI, not serialized
<!-- snippet: operations-params-service -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute demonstrating service injection
- Properties: bool ServicesInjected
- [Create] constructor
- [Remote][Fetch] Task Fetch(Guid id, [Service] IEmployeeRepository employeeRepo, [Service] IDepartmentRepository departmentRepo, [Service] IUserContext userContext)
- Sets ServicesInjected = true if all services are non-null
- Include comment: Services are resolved from DI container on server
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates multiple [Service] parameter injection
-->
<!-- endSnippet -->

**Params arrays**: Variable-length arguments
<!-- snippet: operations-params-array -->
<!--
SNIPPET REQUIREMENTS:
- Record type: BatchAssignmentResult(Guid[] AssignedEmployeeIds, List<string> AssignedDepartments)
- Static partial class BatchAssignmentCommand with [SuppressFactory] attribute (nested class pattern)
- [Remote][Execute] private static Task<BatchAssignmentResult> _AssignToDepartments(Guid[] employeeIds, List<string> departmentNames)
- Returns BatchAssignmentResult with the provided arrays
- Context: Application layer production code
- Domain: Employee Management
- Demonstrates array and List parameters in Execute operations
-->
<!-- endSnippet -->

**CancellationToken**: Optional, always last parameter
<!-- snippet: operations-params-cancellation -->
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate with [Factory] attribute demonstrating optional CancellationToken
- Properties: bool Completed
- [Create] constructor
- [Remote][Fetch] async Task Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken cancellationToken = default)
- Include comment: CancellationToken is optional - receives default if not provided by caller
- Calls repository.GetByIdAsync with cancellationToken
- Sets Completed = true
- Context: Domain layer production code
- Domain: Employee Management
- Demonstrates optional CancellationToken with default value
-->
<!-- endSnippet -->

Parameter order rules:
1. Value parameters (required first, optional last)
2. Service parameters (any order among services)
3. CancellationToken (always last)

## Next Steps

- [Service Injection](service-injection.md) - Inject dependencies into factory methods
- [Authorization](authorization.md) - Secure factory operations
- [Save Operation](save-operation.md) - IFactorySave routing
- [Events](events.md) - Deep dive into event handling
