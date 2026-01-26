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
/// <summary>
/// Employee with constructor-based Create operation.
/// </summary>
[Factory]
public partial class EmployeeWithConstructorCreate
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    /// <summary>
    /// Parameterless constructor marked as Create operation.
    /// </summary>
    [Create]
    public EmployeeWithConstructorCreate()
    {
        Id = Guid.NewGuid();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L6-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-constructor' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Employee with static factory method Create operation.
/// </summary>
[Factory]
public partial class EmployeeWithStaticCreate
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public decimal InitialSalary { get; private set; }

    private EmployeeWithStaticCreate() { }

    /// <summary>
    /// Static factory method for parameterized creation.
    /// </summary>
    [Create]
    public static EmployeeWithStaticCreate Create(
        string employeeNumber,
        string firstName,
        string lastName,
        decimal initialSalary)
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
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L28-L63' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-static' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates Create method patterns.
/// </summary>
[Factory]
public partial class EmployeeCreateReturnTypes
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public bool IsValid { get; private set; }

    /// <summary>
    /// Constructor-based Create - simplest pattern.
    /// </summary>
    [Create]
    public EmployeeCreateReturnTypes()
    {
        Id = Guid.NewGuid();
        IsValid = true;
    }

    /// <summary>
    /// Create with parameters.
    /// </summary>
    [Create]
    public EmployeeCreateReturnTypes(string employeeNumber)
    {
        Id = Guid.NewGuid();
        EmployeeNumber = employeeNumber;
        IsValid = !string.IsNullOrEmpty(employeeNumber) && employeeNumber.StartsWith('E');
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L65-L97' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-return-types' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Employee with instance method Fetch operation.
/// </summary>
[Factory]
public partial class EmployeeFetchSample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeFetchSample()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Loads employee data from repository by ID.
    /// [Service] marks the repository for DI injection.
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
        LastName = entity.LastName;
        IsNew = false;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L99-L138' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-fetch-instance' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates Fetch with bool return for optional entities.
/// </summary>
[Factory]
public partial class EmployeeFetchOptional : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeFetchOptional() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Returns false when employee not found.
    /// Factory method will return null for not-found case.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null)
        {
            // Return false = not found, factory returns null
            return false;
        }

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L140-L178' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-fetch-bool-return' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates Insert operation.
/// </summary>
[Factory]
public partial class EmployeeInsertSample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeInsertSample() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Persists a new employee to the repository.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty,
            Position = "New Hire",
            SalaryAmount = 0,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L180-L222' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Update

<!-- snippet: operations-update -->
<a id='snippet-operations-update'></a>
```cs
/// <summary>
/// Demonstrates Update operation.
/// </summary>
[Factory]
public partial class EmployeeUpdateSample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Position { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeUpdateSample() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Position = entity.Position;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Persists changes to an existing employee.
    /// </summary>
    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty,
            Position = Position,
            SalaryAmount = 0,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };

        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L224-L279' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Delete

<!-- snippet: operations-delete -->
<a id='snippet-operations-delete'></a>
```cs
/// <summary>
/// Demonstrates Delete operation.
/// </summary>
[Factory]
public partial class EmployeeDeleteSample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeDeleteSample() { Id = Guid.NewGuid(); }

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

    /// <summary>
    /// Removes the employee from the repository.
    /// </summary>
    [Remote, Delete]
    public async Task Delete(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L281-L319' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Multiple attributes on one method:

<!-- snippet: operations-insert-update -->
<a id='snippet-operations-insert-update'></a>
```cs
/// <summary>
/// Demonstrates Upsert pattern with both [Insert] and [Update] on same method.
/// </summary>
[Factory]
public partial class SettingItem : IFactorySaveMeta
{
    public string Key { get; private set; } = "";
    public string Value { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public SettingItem(string key)
    {
        Key = key;
    }

    /// <summary>
    /// Single method handles both insert and update (upsert pattern).
    /// </summary>
    [Remote, Insert, Update]
    public async Task Upsert(
        [Service] ISettingsRepository repository,
        CancellationToken ct)
    {
        await repository.UpsertAsync(Key, Value, ct);
        IsNew = false;
    }
}

/// <summary>
/// Repository for settings (used by SettingItem sample).
/// </summary>
public interface ISettingsRepository
{
    Task UpsertAsync(string key, string value, CancellationToken ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L321-L359' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-insert-update' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates Execute operation for business commands.
/// </summary>
[Factory]
public static partial class EmployeePromotionOperation
{
    /// <summary>
    /// Promotes an employee with new title and salary increase.
    /// The underscore prefix is removed in the generated delegate name.
    /// </summary>
    [Remote, Execute]
    private static async Task<PromotionResult> _PromoteEmployee(
        Guid employeeId,
        string newTitle,
        decimal salaryIncrease,
        [Service] IEmployeeRepository repository,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        var employee = await repository.GetByIdAsync(employeeId, ct);
        if (employee == null)
        {
            return new PromotionResult(false, "Employee not found");
        }

        var oldPosition = employee.Position;
        employee.Position = newTitle;
        employee.SalaryAmount += salaryIncrease;

        await repository.UpdateAsync(employee, ct);
        await repository.SaveChangesAsync(ct);

        await auditLog.LogAsync(
            "Promotion",
            employeeId,
            "Employee",
            $"Promoted from {oldPosition} to {newTitle}",
            ct);

        return new PromotionResult(true, $"Promoted to {newTitle}");
    }
}

public record PromotionResult(bool Success, string Message);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L361-L406' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Command pattern using Execute operations.
/// </summary>
[Factory]
public static partial class TransferEmployeeToNewDepartmentCommand
{
    /// <summary>
    /// Transfers an employee to a different department.
    /// </summary>
    [Remote, Execute]
    private static async Task<TransferResult> _Execute(
        Guid employeeId,
        Guid newDepartmentId,
        string reason,
        [Service] IEmployeeRepository employeeRepo,
        [Service] IDepartmentRepository departmentRepo,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId, ct);
        if (employee == null)
            return new TransferResult(false, "Employee not found");

        var newDepartment = await departmentRepo.GetByIdAsync(newDepartmentId, ct);
        if (newDepartment == null)
            return new TransferResult(false, "Department not found");

        var oldDepartmentId = employee.DepartmentId;
        employee.DepartmentId = newDepartmentId;

        await employeeRepo.UpdateAsync(employee, ct);
        await employeeRepo.SaveChangesAsync(ct);

        await auditLog.LogAsync(
            "Transfer",
            employeeId,
            "Employee",
            $"Transferred from {oldDepartmentId} to {newDepartmentId}. Reason: {reason}",
            ct);

        return new TransferResult(true, $"Transferred to {newDepartment.Name}");
    }
}

public record TransferResult(bool Success, string Message);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L408-L454' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Event Operation

Fire-and-forget operations with scope isolation.

Events run asynchronously without blocking the caller. They execute in a separate DI scope for transactional independence.

<!-- snippet: operations-event -->
<a id='snippet-operations-event'></a>
```cs
/// <summary>
/// Demonstrates Event operations for fire-and-forget processing.
/// </summary>
[Factory]
public partial class EmployeeNotificationEvents
{
    /// <summary>
    /// Notifies HR when a new employee is hired.
    /// CancellationToken is required as the last parameter.
    /// </summary>
    [Event]
    public async Task NotifyHROfNewEmployee(
        Guid employeeId,
        string employeeName,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            "hr@company.com",
            $"New Employee: {employeeName}",
            $"Employee {employeeName} (ID: {employeeId}) has been added.",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L456-L481' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-event' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates IEventTracker usage for monitoring events.
/// </summary>
[Factory]
public static partial class EventTrackerSample
{
    /// <summary>
    /// Waits for all pending events to complete.
    /// Useful for testing and graceful shutdown.
    /// Returns the number of events that were pending.
    /// </summary>
    [Execute]
    private static async Task<int> _WaitForAllEvents(
        [Service] IEventTracker eventTracker,
        CancellationToken ct)
    {
        // Check how many events are still pending
        var pendingCount = eventTracker.PendingCount;

        // Wait for all events to complete
        await eventTracker.WaitAllAsync(ct);

        return pendingCount;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L483-L509' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-event-tracker' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Return value handling:
- **void**: Converted to Task automatically
- **Task**: Tracked by EventTracker

## Remote Attribute

Marks methods that execute on the server. Without `[Remote]`, methods execute locally.

<!-- snippet: operations-remote -->
<a id='snippet-operations-remote'></a>
```cs
/// <summary>
/// Demonstrates [Remote] attribute for server execution.
/// </summary>
[Factory]
public partial class EmployeeRemoteVsLocal : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Local execution - runs on client without network call.
    /// No [Remote] attribute means local execution.
    /// </summary>
    [Create]
    public EmployeeRemoteVsLocal()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Remote execution - serialized and sent to server.
    /// [Remote] ensures method runs on server where repository exists.
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

    /// <summary>
    /// Remote execution for write operations.
    /// Repository is only available on server.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty,
            Position = "New",
            SalaryAmount = 0,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L511-L577' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-remote' title='Start of snippet'>anchor</a></sup>
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
<a id='snippet-operations-lifecycle-onstart'></a>
```cs
/// <summary>
/// Demonstrates IFactoryOnStart lifecycle hook.
/// </summary>
[Factory]
public partial class EmployeeWithOnStart : IFactorySaveMeta, IFactoryOnStart
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithOnStart() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Called before any factory operation begins.
    /// Use for pre-operation validation or setup.
    /// </summary>
    public void FactoryStart(FactoryOperation factoryOperation)
    {
        // Validate required fields before write operations
        if (factoryOperation == FactoryOperation.Insert ||
            factoryOperation == FactoryOperation.Update)
        {
            if (string.IsNullOrWhiteSpace(FirstName))
                throw new InvalidOperationException("FirstName is required");

            if (string.IsNullOrWhiteSpace(LastName))
                throw new InvalidOperationException("LastName is required");
        }
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/LifecycleSamples.cs#L6-L67' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-onstart' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnComplete / IFactoryOnCompleteAsync

Called after successful operation. Use `IFactoryOnCompleteAsync` for async post-processing:

<!-- snippet: operations-lifecycle-oncomplete -->
<a id='snippet-operations-lifecycle-oncomplete'></a>
```cs
/// <summary>
/// Demonstrates IFactoryOnComplete lifecycle hook.
/// </summary>
[Factory]
public partial class EmployeeWithOnComplete : IFactorySaveMeta, IFactoryOnComplete
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public int Version { get; private set; } = 1;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithOnComplete() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Called when factory operation succeeds.
    /// Use for post-operation state updates or logging.
    /// </summary>
    public void FactoryComplete(FactoryOperation factoryOperation)
    {
        // Increment version after successful save operations
        if (factoryOperation == FactoryOperation.Insert ||
            factoryOperation == FactoryOperation.Update)
        {
            Version++;
        }
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/LifecycleSamples.cs#L69-L125' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-oncomplete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnCancelled / IFactoryOnCancelledAsync

Called when operation is cancelled via OperationCanceledException. Use `IFactoryOnCancelledAsync` for async cleanup:

<!-- snippet: operations-lifecycle-oncancelled -->
<a id='snippet-operations-lifecycle-oncancelled'></a>
```cs
/// <summary>
/// Demonstrates IFactoryOnCancelled lifecycle hook.
/// </summary>
[Factory]
public partial class EmployeeWithOnCancelled : IFactorySaveMeta, IFactoryOnCancelled
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string CancellationReason { get; private set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithOnCancelled() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Called when factory operation is cancelled.
    /// Use for cleanup or logging of cancellation.
    /// </summary>
    public void FactoryCancelled(FactoryOperation factoryOperation)
    {
        CancellationReason = $"Operation {factoryOperation} was cancelled";
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        // Check cancellation to trigger IFactoryOnCancelled
        ct.ThrowIfCancellationRequested();

        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/LifecycleSamples.cs#L127-L166' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-oncancelled' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates CancellationToken usage in factory methods.
/// </summary>
[Factory]
public partial class EmployeeWithCancellation : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithCancellation() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Demonstrates proper cancellation handling.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        // Check cancellation before expensive operations
        ct.ThrowIfCancellationRequested();

        // Pass token to async repository calls
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        // Check again after long-running operation
        ct.ThrowIfCancellationRequested();

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L579-L619' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-cancellation' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Demonstrates value parameters that are serialized.
/// </summary>
[Factory]
public partial class EmployeeSearchSample
{
    public List<string> Results { get; private set; } = [];

    /// <summary>
    /// Value parameters are serialized and sent to server.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid departmentId,
        string? positionFilter,
        int maxResults,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var employees = await repository.GetByDepartmentIdAsync(departmentId, ct);

        Results = employees
            .Where(e => positionFilter == null ||
                       e.Position.Contains(positionFilter, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .Select(e => $"{e.FirstName} {e.LastName}")
            .ToList();

        return Results.Count > 0;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L621-L653' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-value' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Service parameters**: Injected from DI, not serialized
<!-- snippet: operations-params-service -->
<a id='snippet-operations-params-service'></a>
```cs
/// <summary>
/// Demonstrates [Service] parameters injected from DI.
/// </summary>
[Factory]
public partial class EmployeeWithServiceParams : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithServiceParams() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Mix of value parameters (serialized) and service parameters (DI injected).
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,                          // Value: serialized
        [Service] IEmployeeRepository repository, // Service: injected from DI
        [Service] IAuditLogService auditLog,      // Service: injected from DI
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;

        // Services are resolved on the server
        await auditLog.LogAsync("Fetch", employeeId, "Employee", "Employee loaded", ct);

        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L655-L693' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-service' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Params arrays**: Variable-length arguments
<!-- snippet: operations-params-array -->
<a id='snippet-operations-params-array'></a>
```cs
/// <summary>
/// Demonstrates params array parameters.
/// </summary>
[Factory]
public partial class BatchEmployeeFetch
{
    public List<string> EmployeeNames { get; private set; } = [];

    /// <summary>
    /// params arrays are supported for batch operations.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        [Service] IEmployeeRepository repository,
        CancellationToken ct,
        params Guid[] employeeIds)
    {
        EmployeeNames = [];
        foreach (var id in employeeIds)
        {
            var entity = await repository.GetByIdAsync(id, ct);
            if (entity != null)
            {
                EmployeeNames.Add($"{entity.FirstName} {entity.LastName}");
            }
        }
        return EmployeeNames.Count > 0;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L695-L725' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-array' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**CancellationToken**: Optional, always last parameter
<!-- snippet: operations-params-cancellation -->
<a id='snippet-operations-params-cancellation'></a>
```cs
/// <summary>
/// Demonstrates proper parameter ordering with CancellationToken.
/// </summary>
[Factory]
public partial class EmployeeCompleteParams : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeCompleteParams() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Complete method signature with all parameter types.
    /// Order: value params, service params, CancellationToken (always last).
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,                          // Required value parameter
        string? filter,                           // Optional value parameter
        [Service] IEmployeeRepository repository, // Service parameter
        [Service] IAuditLogService auditLog,      // Service parameter
        CancellationToken ct)                     // CancellationToken (always last)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        if (filter != null && !entity.Position.Contains(filter, StringComparison.OrdinalIgnoreCase))
            return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;

        await auditLog.LogAsync("Fetch", employeeId, "Employee", "Filtered fetch", ct);
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L727-L768' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-cancellation' title='Start of snippet'>anchor</a></sup>
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
