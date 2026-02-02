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
/// Result of an employee promotion operation.
/// </summary>
public record PromotionResult(Guid EmployeeId, bool IsApproved, string NewTitle, decimal NewSalary);

/// <summary>
/// Command for promoting an employee.
/// </summary>
[SuppressFactory]
public static partial class EmployeePromotionCommand
{
    /// <summary>
    /// Promotes an employee with a new title and salary increase.
    /// </summary>
    [Remote, Execute]
    private static async Task<PromotionResult> _PromoteEmployee(
        Guid employeeId,
        string newTitle,
        decimal salaryIncrease,
        [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null)
            throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        entity.Position = newTitle;
        entity.SalaryAmount += salaryIncrease;

        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();

        return new PromotionResult(employeeId, true, newTitle, entity.SalaryAmount);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Operations/ExecuteOperationSamples.cs#L6-L41' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute' title='Start of snippet'>anchor</a></sup>
<a id='snippet-operations-execute-1'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L361-L406' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute-1' title='Start of snippet'>anchor</a></sup>
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
/// Result of an employee transfer operation.
/// </summary>
public record TransferResult(Guid EmployeeId, Guid NewDepartmentId, DateTime TransferDate, bool Success);

/// <summary>
/// Command for transferring an employee to a new department.
/// </summary>
[SuppressFactory]
public static partial class EmployeeTransferCommand
{
    /// <summary>
    /// Transfers an employee to a new department.
    /// </summary>
    [Remote, Execute]
    private static async Task<TransferResult> _TransferEmployee(
        Guid employeeId,
        Guid newDepartmentId,
        DateTime effectiveDate,
        [Service] IEmployeeRepository employeeRepo,
        [Service] IDepartmentRepository departmentRepo)
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId);
        if (employee == null)
            throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        // Validate employee is in Active status (using Position as a status proxy)
        if (employee.Position == "Terminated")
            throw new InvalidOperationException("Cannot transfer a terminated employee.");

        employee.DepartmentId = newDepartmentId;

        await employeeRepo.UpdateAsync(employee);
        await employeeRepo.SaveChangesAsync();

        return new TransferResult(employeeId, newDepartmentId, effectiveDate, true);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Operations/ExecuteOperationSamples.cs#L43-L82' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute-command' title='Start of snippet'>anchor</a></sup>
<a id='snippet-operations-execute-command-1'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L408-L454' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute-command-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Event Operation

Fire-and-forget operations with scope isolation.

Events run asynchronously without blocking the caller. They execute in a separate DI scope for transactional independence.

<!-- snippet: operations-event -->
<a id='snippet-operations-event'></a>
```cs
/// <summary>
/// Event handler for employee notifications.
/// </summary>
[SuppressFactory]
public partial class EmployeeEventHandler
{
    public Guid EmployeeId { get; private set; }

    [Create]
    public EmployeeEventHandler()
    {
    }

    /// <summary>
    /// Sends a welcome email to a new employee.
    /// Fire-and-forget event pattern for notifications.
    /// </summary>
    [Event]
    public async Task SendWelcomeEmail(
        Guid employeeId,
        string employeeEmail,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            employeeEmail,
            "Welcome to the Team",
            $"Welcome! Your employee ID is {employeeId}.",
            ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Operations/EventOperationSamples.cs#L6-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-event' title='Start of snippet'>anchor</a></sup>
<a id='snippet-operations-event-1'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L456-L481' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-event-1' title='Start of snippet'>anchor</a></sup>
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
// EventTracker usage pattern:
//
// Fire event (fire-and-forget):
// _ = sendWelcomeEmail(employeeId, employeeEmail);
//
// Wait for all pending events (useful for testing or shutdown):
// await eventTracker.WaitAllAsync();
//
// Check pending event count:
// var pending = eventTracker.PendingCount;
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Operations/EventOperationSamples.cs#L40-L51' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-event-tracker' title='Start of snippet'>anchor</a></sup>
<a id='snippet-operations-event-tracker-1'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L483-L509' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-event-tracker-1' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Collection of OrderLines within an Order aggregate.
/// </summary>
[Factory]
public partial class OrderLineList : List<OrderLine>
{
    private readonly IOrderLineFactory _lineFactory;

    /// <summary>
    /// Creates an empty collection with injected child factory.
    /// </summary>
    [Create]
    public OrderLineList([Service] IOrderLineFactory lineFactory)
    {
        _lineFactory = lineFactory;
    }

    /// <summary>
    /// Fetches a collection from data.
    /// </summary>
    [Fetch]
    public void Fetch(
        IEnumerable<(int id, string name, decimal price, int qty)> items,
        [Service] IOrderLineFactory lineFactory)
    {
        foreach (var item in items)
        {
            Add(lineFactory.Fetch(item.id, item.name, item.price, item.qty));
        }
    }

    /// <summary>
    /// Domain method using stored factory to add children.
    /// </summary>
    public void AddLine(string name, decimal price, int qty)
    {
        var line = _lineFactory.Create(name, price, qty);
        Add(line);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Collections/OrderLineCollectionSamples.cs#L43-L84' title='Snippet source file'>snippet source</a> | <a href='#snippet-collection-factory-basic' title='Start of snippet'>anchor</a></sup>
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
/// <summary>
/// Order aggregate root demonstrating collection factory usage.
/// </summary>
[Factory]
public partial class Order
{
    public int Id { get; private set; }
    public string CustomerName { get; set; } = "";
    public OrderLineList Lines { get; set; } = null!;

    public decimal OrderTotal => Lines?.Sum(l => l.LineTotal) ?? 0;

    [Remote, Create]
    public void Create(
        string customerName,
        [Service] IOrderLineListFactory lineListFactory)
    {
        Id = Random.Shared.Next(1, 10000);
        CustomerName = customerName;
        Lines = lineListFactory.Create();  // Factory creates collection
    }

    [Remote, Fetch]
    public void Fetch(
        int id,
        [Service] IOrderLineListFactory lineListFactory)
    {
        Id = id;
        // Factory fetches collection with data
        Lines = lineListFactory.Fetch([
            (1, "Widget A", 10.00m, 2),
            (2, "Widget B", 25.00m, 1)
        ]);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Collections/OrderLineCollectionSamples.cs#L86-L122' title='Snippet source file'>snippet source</a> | <a href='#snippet-collection-factory-parent' title='Start of snippet'>anchor</a></sup>
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

Use `[Remote]` for aggregate root factory methods and other client entry points. Most methods with method-injected services do NOT need `[Remote]`—they're called from server-side code after already crossing the boundary.

See [Client-Server Architecture](client-server-architecture.md) for the complete mental model.

## Lifecycle Hooks

Interfaces for operation lifecycle:

### IFactoryOnStart / IFactoryOnStartAsync

Called before the operation executes. Use `IFactoryOnStartAsync` for async validation or preparation:

<!-- snippet: operations-lifecycle-onstart -->
<a id='snippet-operations-lifecycle-onstart'></a>
```cs
/// <summary>
/// Employee aggregate implementing IFactoryOnStart for pre-operation validation.
/// </summary>
[Factory]
public partial class EmployeeLifecycleOnStart : IFactoryOnStart
{
    public Guid Id { get; private set; }
    public bool OnStartCalled { get; private set; }
    public FactoryOperation? LastOperation { get; private set; }

    [Create]
    public EmployeeLifecycleOnStart()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Called before any factory operation executes.
    /// Use for pre-operation validation and preparation.
    /// </summary>
    public void FactoryStart(FactoryOperation factoryOperation)
    {
        OnStartCalled = true;
        LastOperation = factoryOperation;

        // Validate: cannot delete an employee with empty ID
        if (factoryOperation == FactoryOperation.Delete && Id == Guid.Empty)
        {
            throw new InvalidOperationException("Cannot delete an employee that has not been saved.");
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/LifecycleHookSamples.cs#L5-L38' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-onstart' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnComplete / IFactoryOnCompleteAsync

Called after successful operation. Use `IFactoryOnCompleteAsync` for async post-processing:

<!-- snippet: operations-lifecycle-oncomplete -->
<a id='snippet-operations-lifecycle-oncomplete'></a>
```cs
/// <summary>
/// Employee aggregate implementing IFactoryOnComplete for post-operation processing.
/// </summary>
[Factory]
public partial class EmployeeLifecycleOnComplete : IFactoryOnComplete
{
    public Guid Id { get; private set; }
    public bool OnCompleteCalled { get; private set; }
    public FactoryOperation? CompletedOperation { get; private set; }

    [Create]
    public EmployeeLifecycleOnComplete()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Called after factory operation completes successfully.
    /// Use for post-operation logic: audit logging, cache invalidation, notifications, etc.
    /// </summary>
    public void FactoryComplete(FactoryOperation factoryOperation)
    {
        OnCompleteCalled = true;
        CompletedOperation = factoryOperation;

        // Post-operation logic: logging, notifications, etc.
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/LifecycleHookSamples.cs#L40-L69' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-oncomplete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnCancelled / IFactoryOnCancelledAsync

Called when operation is cancelled via OperationCanceledException. Use `IFactoryOnCancelledAsync` for async cleanup:

<!-- snippet: operations-lifecycle-oncancelled -->
<a id='snippet-operations-lifecycle-oncancelled'></a>
```cs
/// <summary>
/// Employee aggregate implementing IFactoryOnCancelled for cancellation handling.
/// </summary>
[Factory]
public partial class EmployeeLifecycleOnCancelled : IFactoryOnCancelled
{
    public Guid Id { get; private set; }
    public bool OnCancelledCalled { get; private set; }
    public FactoryOperation? CancelledOperation { get; private set; }

    [Create]
    public EmployeeLifecycleOnCancelled()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Called when factory operation is cancelled via CancellationToken.
    /// Use for cleanup logic when operation was cancelled.
    /// </summary>
    public void FactoryCancelled(FactoryOperation factoryOperation)
    {
        OnCancelledCalled = true;
        CancelledOperation = factoryOperation;

        // Cleanup logic when operation was cancelled
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/LifecycleHookSamples.cs#L71-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-oncancelled' title='Start of snippet'>anchor</a></sup>
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
/// Result of a batch assignment operation.
/// </summary>
public record BatchAssignmentResult(Guid[] AssignedEmployeeIds, List<string> AssignedDepartments);

/// <summary>
/// Command for batch assignment operations.
/// </summary>
[SuppressFactory]
public static partial class BatchAssignmentCommand
{
    /// <summary>
    /// Assigns multiple employees to departments.
    /// Demonstrates array and List parameter types.
    /// </summary>
    [Remote, Execute]
    private static Task<BatchAssignmentResult> _AssignToDepartments(
        Guid[] employeeIds,
        List<string> departmentNames)
    {
        return Task.FromResult(new BatchAssignmentResult(employeeIds, departmentNames));
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Operations/ExecuteOperationSamples.cs#L84-L108' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-array' title='Start of snippet'>anchor</a></sup>
<a id='snippet-operations-params-array-1'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/OperationsSamples.cs#L695-L725' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-array-1' title='Start of snippet'>anchor</a></sup>
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
