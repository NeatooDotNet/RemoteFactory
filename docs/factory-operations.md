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
<a id='snippet-operations-create-constructor'></a>
```cs
// Constructor marked with [Create] - factory calls this to create instances
[Create]
public EmployeeWithConstructorCreate() => Id = Guid.NewGuid();
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L16-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-constructor' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-operations-create-static'></a>
```cs
// Static factory method with [Create] - returns instance with initialization
[Create]
public static EmployeeWithStaticCreate Create(
    string employeeNumber, string firstName, string lastName, decimal initialSalary)
{
    return new EmployeeWithStaticCreate
    {
        Id = Guid.NewGuid(),
        EmployeeNumber = employeeNumber,
        FirstName = firstName,
        LastName = lastName,
        InitialSalary = initialSalary
    };
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L37-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-static' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-operations-create-return-types'></a>
```cs
// Multiple [Create] overloads - factory generates method for each
[Create]
public EmployeeCreateReturnTypes() { Id = Guid.NewGuid(); IsValid = true; }

[Create]
public EmployeeCreateReturnTypes(string employeeNumber)
{
    Id = Guid.NewGuid();
    EmployeeNumber = employeeNumber;
    IsValid = !string.IsNullOrEmpty(employeeNumber) && employeeNumber.StartsWith('E');
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L65-L77' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-return-types' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-operations-fetch-instance'></a>
```cs
// [Fetch] loads data into instance; [Service] marks DI-injected parameters
[Remote, Fetch]
public async Task<bool> Fetch(Guid employeeId, [Service] IEmployeeRepository repository, CancellationToken ct)
{
    var entity = await repository.GetByIdAsync(employeeId, ct);
    if (entity == null) return false;  // Return false = factory returns null
    Id = entity.Id;
    FirstName = entity.FirstName;
    LastName = entity.LastName;
    IsNew = false;
    return true;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L95-L108' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-fetch-instance' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-operations-fetch-bool-return'></a>
```cs
// Return bool: true = success (instance), false = not found (factory returns null)
[Remote, Fetch]
public async Task<bool> Fetch(Guid employeeId, [Service] IEmployeeRepository repository, CancellationToken ct)
{
    var entity = await repository.GetByIdAsync(employeeId, ct);
    if (entity == null) return false;  // Factory returns null
    Id = entity.Id;
    FirstName = entity.FirstName;
    IsNew = false;
    return true;  // Factory returns instance
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L125-L137' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-fetch-bool-return' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-operations-insert'></a>
```cs
// [Insert] persists new entities to storage
[Remote, Insert]
public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
{
    var entity = new EmployeeEntity { Id = Id, FirstName = FirstName, LastName = LastName, /* ... */ };
    await repository.AddAsync(entity, ct);
    await repository.SaveChangesAsync(ct);
    IsNew = false;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L155-L165' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Update

<!-- snippet: operations-update -->
<a id='snippet-operations-update'></a>
```cs
// [Update] persists changes to existing entities
[Remote, Update]
public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
{
    var entity = new EmployeeEntity { Id = Id, FirstName = FirstName, LastName = LastName, /* ... */ };
    await repository.UpdateAsync(entity, ct);
    await repository.SaveChangesAsync(ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L197-L206' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Delete

<!-- snippet: operations-delete -->
<a id='snippet-operations-delete'></a>
```cs
// [Delete] removes entities from storage
[Remote, Delete]
public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
{
    await repository.DeleteAsync(Id, ct);
    await repository.SaveChangesAsync(ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L234-L242' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Multiple attributes on one method:

<!-- snippet: operations-insert-update -->
<a id='snippet-operations-insert-update'></a>
```cs
// Multiple attributes on one method - same handler for insert and update (upsert)
[Remote, Insert, Update]
public async Task Upsert([Service] ISettingsRepository repository, CancellationToken ct)
{
    await repository.UpsertAsync(Key, Value, ct);
    IsNew = false;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L259-L267' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-insert-update' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-operations-execute'></a>
```cs
// [Execute] for business operations - underscore prefix removed in generated delegate name
[Remote, Execute]
private static async Task<PromotionResult> _PromoteEmployee(
    Guid employeeId, string newTitle, decimal salaryIncrease,
    [Service] IEmployeeRepository repository, CancellationToken ct)
{
    var employee = await repository.GetByIdAsync(employeeId, ct);
    if (employee == null) return new PromotionResult(false, "Employee not found");
    employee.Position = newTitle;
    employee.SalaryAmount += salaryIncrease;
    await repository.UpdateAsync(employee, ct);
    await repository.SaveChangesAsync(ct);
    return new PromotionResult(true, $"Promoted to {newTitle}");
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L284-L299' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-operations-execute-command'></a>
```cs
// Command pattern with [Execute] - static class with private method, underscore prefix removed
[Remote, Execute]
private static async Task<TransferResult> _TransferEmployee(
    Guid employeeId, Guid newDepartmentId, DateTime effectiveDate,
    [Service] IEmployeeRepository employeeRepo, [Service] IDepartmentRepository departmentRepo)
{
    var employee = await employeeRepo.GetByIdAsync(employeeId);
    if (employee == null) throw new InvalidOperationException($"Employee {employeeId} not found.");
    employee.DepartmentId = newDepartmentId;
    await employeeRepo.UpdateAsync(employee);
    await employeeRepo.SaveChangesAsync();
    return new TransferResult(employeeId, newDepartmentId, effectiveDate, true);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Operations/ExecuteOperationSamples.cs#L20-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Event Operation

Fire-and-forget operations with scope isolation.

Events run asynchronously without blocking the caller. They execute in a separate DI scope for transactional independence.

<!-- snippet: operations-event -->
<a id='snippet-operations-event'></a>
```cs
// [Event] for fire-and-forget - CancellationToken required, receives ApplicationStopping
[Event]
public async Task NotifyHROfNewEmployee(
    Guid employeeId, string employeeName,
    [Service] IEmailService emailService, CancellationToken ct)
{
    await emailService.SendAsync("hr@company.com", $"New Employee: {employeeName}", $"ID: {employeeId}", ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L310-L319' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-event' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-operations-event-tracker'></a>
```cs
// IEventTracker for waiting on pending events (useful in tests and shutdown)
[Execute]
private static async Task<int> _WaitForAllEvents([Service] IEventTracker eventTracker, CancellationToken ct)
{
    var pendingCount = eventTracker.PendingCount;
    await eventTracker.WaitAllAsync(ct);
    return pendingCount;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L328-L337' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-event-tracker' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Return value handling:
- **void**: Converted to Task automatically
- **Task**: Tracked by EventTracker

## Collection Factories

Collections can have `[Factory]` to support batch operations and child factory injection.

### Basic Collection Factory

<!-- snippet: collection-factory-basic -->
<a id='snippet-collection-factory-basic'></a>
```cs
// Constructor-injected child factory (survives serialization)
[Create]
public OrderLineList([Service] IOrderLineFactory lineFactory) => _lineFactory = lineFactory;

// Fetch populates collection from data
[Fetch]
public void Fetch(
    IEnumerable<(int id, string name, decimal price, int qty)> items,
    [Service] IOrderLineFactory lineFactory)
{
    foreach (var item in items)
        Add(lineFactory.Fetch(item.id, item.name, item.price, item.qty));
}

// Domain method uses stored factory to add children
public void AddLine(string name, decimal price, int qty) => Add(_lineFactory.Create(name, price, qty));
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Collections/OrderLineCollectionSamples.cs#L45-L62' title='Snippet source file'>snippet source</a> | <a href='#snippet-collection-factory-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated factory interface:
```csharp
public interface IOrderLineListFactory
{
    OrderLineList Create();
    OrderLineList Fetch(IEnumerable<(int, string, decimal, int)> items);
}
```

### Key Points

**Collection inherits from List<T> or implements IList<T>:**
Collections that need factory behavior inherit from `List<T>` or implement a list interface.

**Child factory is constructor-injected:**
Use constructor injection for the child factory so it survives serialization (see [Service Injection](service-injection.md#serialization-caveat-method-injected-services-are-lost)).

**No [Remote] on collection:**
Collections are part of their parent aggregate. The parent's `[Remote]` method is the entry point; collection operations run server-side within that call.

**Parent creates collection via factory:**

<!-- snippet: collection-factory-parent -->
<a id='snippet-collection-factory-parent'></a>
```cs
// Parent creates collection via factory - collection is properly initialized with child factory
[Remote, Create]
public void Create(string customerName, [Service] IOrderLineListFactory lineListFactory)
{
    Id = Random.Shared.Next(1, 10000);
    CustomerName = customerName;
    Lines = lineListFactory.Create();
}

[Remote, Fetch]
public void Fetch(int id, [Service] IOrderLineListFactory lineListFactory)
{
    Id = id;
    Lines = lineListFactory.Fetch([(1, "Widget A", 10.00m, 2), (2, "Widget B", 25.00m, 1)]);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Collections/OrderLineCollectionSamples.cs#L77-L93' title='Snippet source file'>snippet source</a> | <a href='#snippet-collection-factory-parent' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This pattern ensures:
1. Collections are properly initialized with child factories
2. Children can be added after the aggregate is fetched
3. Factory references survive serialization via constructor injection

## Remote Attribute

Marks methods as **entry points from the client to the server**. Once execution crosses to the server, subsequent calls stay there.

<!-- snippet: operations-remote -->
<a id='snippet-operations-remote'></a>
```cs
// No [Remote] = local execution (client-side)
[Create]
public EmployeeRemoteVsLocal() => Id = Guid.NewGuid();

// [Remote] = serialized to server where repository is available
[Remote, Fetch]
public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
{
    var entity = await repository.GetByIdAsync(id, ct);
    if (entity == null) return false;
    Id = entity.Id;
    FirstName = entity.FirstName;
    IsNew = false;
    return true;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L351-L367' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-remote' title='Start of snippet'>anchor</a></sup>
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

Use `[Remote]` for aggregate root factory methods and other client entry points. Most methods with method-injected services do NOT need `[Remote]`—they're called from server-side code after already crossing the boundary.

See [Client-Server Architecture](client-server-architecture.md) for the complete mental model.

## Lifecycle Hooks

Interfaces for operation lifecycle:

### IFactoryOnStart / IFactoryOnStartAsync

Called before the operation executes. Use `IFactoryOnStartAsync` for async validation or preparation:

<!-- snippet: operations-lifecycle-onstart -->
<a id='snippet-operations-lifecycle-onstart'></a>
```cs
// IFactoryOnStart - called before any factory operation executes
public void FactoryStart(FactoryOperation factoryOperation)
{
    OnStartCalled = true;
    LastOperation = factoryOperation;
    if (factoryOperation == FactoryOperation.Delete && Id == Guid.Empty)
        throw new InvalidOperationException("Cannot delete unsaved employee.");
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/LifecycleHookSamples.cs#L18-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-onstart' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnComplete / IFactoryOnCompleteAsync

Called after successful operation. Use `IFactoryOnCompleteAsync` for async post-processing:

<!-- snippet: operations-lifecycle-oncomplete -->
<a id='snippet-operations-lifecycle-oncomplete'></a>
```cs
// IFactoryOnComplete - called after operation succeeds (audit, cache invalidation, etc.)
public void FactoryComplete(FactoryOperation factoryOperation)
{
    OnCompleteCalled = true;
    CompletedOperation = factoryOperation;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/LifecycleHookSamples.cs#L43-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-oncomplete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnCancelled / IFactoryOnCancelledAsync

Called when operation is cancelled via OperationCanceledException. Use `IFactoryOnCancelledAsync` for async cleanup:

<!-- snippet: operations-lifecycle-oncancelled -->
<a id='snippet-operations-lifecycle-oncancelled'></a>
```cs
// IFactoryOnCancelled - called when operation cancelled via CancellationToken
public void FactoryCancelled(FactoryOperation factoryOperation)
{
    OnCancelledCalled = true;
    CancelledOperation = factoryOperation;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/LifecycleHookSamples.cs#L66-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-oncancelled' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Lifecycle execution order:
1. `FactoryStart()` or `FactoryStartAsync()`
2. Operation method executes
3. `FactoryComplete()` or `FactoryCompleteAsync()` (if successful)
4. `FactoryCancelled()` or `FactoryCancelledAsync()` (if cancelled)

## CancellationToken Support

All factory methods accept an optional CancellationToken:

<!-- snippet: operations-cancellation -->
<a id='snippet-operations-cancellation'></a>
```cs
// CancellationToken always last - pass to async calls, check before expensive operations
[Remote, Fetch]
public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
{
    ct.ThrowIfCancellationRequested();
    var entity = await repository.GetByIdAsync(id, ct);
    if (entity == null) return false;
    Id = entity.Id;
    FirstName = entity.FirstName;
    IsNew = false;
    return true;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L393-L406' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-cancellation' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-operations-params-value'></a>
```cs
// Value parameters (without [Service]) are serialized and sent to server
[Remote, Fetch]
public async Task<bool> Fetch(
    Guid departmentId, string? positionFilter, int maxResults,
    [Service] IEmployeeRepository repository, CancellationToken ct)
{
    var employees = await repository.GetByDepartmentIdAsync(departmentId, ct);
    Results = employees
        .Where(e => positionFilter == null || e.Position.Contains(positionFilter))
        .Take(maxResults).Select(e => $"{e.FirstName} {e.LastName}").ToList();
    return Results.Count > 0;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L417-L430' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-value' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Service parameters**: Injected from DI, not serialized
<!-- snippet: operations-params-service -->
<a id='snippet-operations-params-service'></a>
```cs
// [Service] parameters are DI-injected, not serialized
[Remote, Fetch]
public async Task<bool> Fetch(
    Guid employeeId,                          // Value: serialized
    [Service] IEmployeeRepository repository, // Service: DI-injected
    [Service] IAuditLogService auditLog,      // Service: DI-injected
    CancellationToken ct)
{
    var entity = await repository.GetByIdAsync(employeeId, ct);
    if (entity == null) return false;
    Id = entity.Id;
    FirstName = entity.FirstName;
    IsNew = false;
    await auditLog.LogAsync("Fetch", employeeId, "Employee", "Employee loaded", ct);
    return true;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L447-L464' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-service' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Params arrays**: Variable-length arguments
<!-- snippet: operations-params-array -->
<a id='snippet-operations-params-array'></a>
```cs
// params arrays supported for batch operations
[Remote, Fetch]
public async Task<bool> Fetch(
    [Service] IEmployeeRepository repository, CancellationToken ct, params Guid[] employeeIds)
{
    EmployeeNames = [];
    foreach (var id in employeeIds)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity != null) EmployeeNames.Add($"{entity.FirstName} {entity.LastName}");
    }
    return EmployeeNames.Count > 0;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L475-L489' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-array' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**CancellationToken**: Optional, always last parameter
<!-- snippet: operations-params-cancellation -->
<a id='snippet-operations-params-cancellation'></a>
```cs
// Parameter order: value params, [Service] params, CancellationToken (always last)
[Remote, Fetch]
public async Task<bool> Fetch(
    Guid employeeId, string? filter,          // Value parameters
    [Service] IEmployeeRepository repository, // Service parameters
    [Service] IAuditLogService auditLog,
    CancellationToken ct)                     // CancellationToken last
{
    var entity = await repository.GetByIdAsync(employeeId, ct);
    if (entity == null) return false;
    if (filter != null && !entity.Position.Contains(filter)) return false;
    Id = entity.Id;
    FirstName = entity.FirstName;
    IsNew = false;
    await auditLog.LogAsync("Fetch", employeeId, "Employee", "Filtered fetch", ct);
    return true;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L506-L524' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-cancellation' title='Start of snippet'>anchor</a></sup>
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
