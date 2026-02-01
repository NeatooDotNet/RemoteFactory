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
/// Employee aggregate with constructor-based creation.
/// </summary>
[Factory]
public partial class EmployeeCreateConstructor
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime HireDate { get; private set; }

    /// <summary>
    /// Creates a new Employee with generated ID and current hire date.
    /// </summary>
    [Create]
    public EmployeeCreateConstructor()
    {
        Id = Guid.NewGuid();
        HireDate = DateTime.UtcNow;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/CreateOperationSamples.cs#L5-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-constructor' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate with static factory method creation.
/// </summary>
[Factory]
public partial class EmployeeCreateStatic
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public decimal Salary { get; private set; }

    private EmployeeCreateStatic()
    {
    }

    /// <summary>
    /// Creates a new Employee with validation and formatting.
    /// </summary>
    [Create]
    public static EmployeeCreateStatic Create(
        string employeeNumber,
        string firstName,
        string lastName,
        decimal initialSalary)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ArgumentException("Employee number is required.", nameof(employeeNumber));

        return new EmployeeCreateStatic
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = employeeNumber.ToUpperInvariant(),
            FirstName = firstName,
            LastName = lastName,
            Salary = initialSalary
        };
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/CreateOperationSamples.cs#L29-L69' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-static' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate demonstrating multiple Create patterns.
/// </summary>
[Factory]
public partial class EmployeeCreateReturnTypes
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool Initialized { get; private set; }

    /// <summary>
    /// Pattern 1: Constructor [Create] - returns the instance.
    /// </summary>
    [Create]
    public EmployeeCreateReturnTypes()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Pattern 2: Instance method [Create] void - sets properties and returns instance.
    /// </summary>
    [Create]
    public void Initialize(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        Initialized = true;
    }

    /// <summary>
    /// Pattern 3: Static method [Create] returning T - returns new instance with defaults.
    /// </summary>
    [Create]
    public static EmployeeCreateReturnTypes CreateWithDefaults()
    {
        return new EmployeeCreateReturnTypes
        {
            Id = Guid.NewGuid(),
            FirstName = "New",
            LastName = "Employee",
            Initialized = true
        };
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/CreateOperationSamples.cs#L71-L118' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-return-types' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate with instance Fetch method.
/// </summary>
[Factory]
public partial class EmployeeFetchInstance
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";
    public decimal Salary { get; private set; }
    public bool IsNew { get; private set; } = true;

    [Create]
    public EmployeeFetchInstance()
    {
    }

    /// <summary>
    /// Fetches an employee by ID from the repository.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null)
            throw new InvalidOperationException($"Employee with ID {employeeId} not found.");

        Id = entity.Id;
        EmployeeNumber = $"EMP-{entity.Id.ToString()[..8].ToUpperInvariant()}";
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Salary = entity.SalaryAmount;
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/FetchOperationSamples.cs#L6-L43' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-fetch-instance' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate with bool-returning Fetch for nullable factory return.
/// </summary>
[Factory]
public partial class EmployeeFetchBoolReturn
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; private set; } = "";
    public string LastName { get; private set; } = "";

    [Create]
    public EmployeeFetchBoolReturn()
    {
    }

    /// <summary>
    /// Attempts to fetch an employee by ID.
    /// Returns false if not found (factory returns null).
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> TryFetch(Guid employeeId, [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null)
            return false;

        Id = entity.Id;
        EmployeeNumber = $"EMP-{entity.Id.ToString()[..8].ToUpperInvariant()}";
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/FetchOperationSamples.cs#L45-L80' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-fetch-bool-return' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate demonstrating Insert operation.
/// </summary>
[Factory]
public partial class EmployeeInsertDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new Employee with generated ID and employee number.
    /// </summary>
    [Create]
    public EmployeeInsertDemo()
    {
        Id = Guid.NewGuid();
        EmployeeNumber = $"EMP-{DateTime.UtcNow:yyyyMMdd}-{Id.ToString()[..8].ToUpperInvariant()}";
    }

    /// <summary>
    /// Inserts a new Employee into the repository.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            SalaryAmount = Salary,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity);
        await repository.SaveChangesAsync();
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/WriteOperationSamples.cs#L6-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Update

<!-- snippet: operations-update -->
<a id='snippet-operations-update'></a>
```cs
/// <summary>
/// Employee aggregate demonstrating Update operation.
/// </summary>
[Factory]
public partial class EmployeeUpdateDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public decimal Salary { get; set; }
    public string Department { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeUpdateDemo()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an existing Employee by ID.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null)
            return false;

        Id = entity.Id;
        EmployeeNumber = $"EMP-{entity.Id.ToString()[..8].ToUpperInvariant()}";
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Salary = entity.SalaryAmount;
        Department = entity.Position;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Updates an existing Employee in the repository.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(Id);
        if (entity == null)
            throw new InvalidOperationException($"Employee with ID {Id} not found.");

        // Update mutable properties
        entity.FirstName = FirstName;
        entity.LastName = LastName;
        entity.SalaryAmount = Salary;
        entity.Position = Department;

        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/WriteOperationSamples.cs#L54-L116' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Delete

<!-- snippet: operations-delete -->
<a id='snippet-operations-delete'></a>
```cs
/// <summary>
/// Employee aggregate demonstrating Delete operation.
/// </summary>
[Factory]
public partial class EmployeeDeleteDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeDeleteDemo()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an existing Employee by ID.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null)
            return false;

        Id = entity.Id;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Deletes the Employee from the repository.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository)
    {
        await repository.DeleteAsync(Id);
        await repository.SaveChangesAsync();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/WriteOperationSamples.cs#L118-L160' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Multiple attributes on one method:

<!-- snippet: operations-insert-update -->
<a id='snippet-operations-insert-update'></a>
```cs
/// <summary>
/// Employee aggregate demonstrating combined Insert/Update (upsert) pattern.
/// </summary>
[Factory]
public partial class EmployeeUpsertDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeUpsertDemo()
    {
        Id = Guid.NewGuid();
        EmployeeNumber = $"EMP-{DateTime.UtcNow:yyyyMMdd}-{Id.ToString()[..8].ToUpperInvariant()}";
    }

    /// <summary>
    /// Upserts the Employee - inserts if new, updates if existing.
    /// </summary>
    [Remote, Insert, Update]
    public async Task Upsert([Service] IEmployeeRepository repository)
    {
        var existing = await repository.GetByIdAsync(Id);

        if (existing == null)
        {
            // Insert new entity
            var entity = new EmployeeEntity
            {
                Id = Id,
                FirstName = FirstName,
                LastName = LastName,
                SalaryAmount = Salary,
                SalaryCurrency = "USD",
                HireDate = DateTime.UtcNow
            };
            await repository.AddAsync(entity);
        }
        else
        {
            // Update existing entity
            existing.FirstName = FirstName;
            existing.LastName = LastName;
            existing.SalaryAmount = Salary;
            await repository.UpdateAsync(existing);
        }

        await repository.SaveChangesAsync();
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/WriteOperationSamples.cs#L162-L219' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-insert-update' title='Start of snippet'>anchor</a></sup>
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
<!-- endSnippet -->

Return value handling:
- **void**: Converted to Task automatically
- **Task**: Tracked by EventTracker

## Collection Factories

Collections can have `[Factory]` to support batch operations and child factory injection.

### Basic Collection Factory

<!-- snippet: collection-factory-basic -->
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
/// Employee aggregate demonstrating the [Remote] attribute for server-side execution.
/// </summary>
[Factory]
public partial class EmployeeRemoteDemo
{
    public string Result { get; private set; } = "";

    [Create]
    public EmployeeRemoteDemo()
    {
    }

    /// <summary>
    /// Fetches data from the server.
    /// </summary>
    /// <remarks>
    /// This code runs on the server - the [Remote] attribute marks it as a client entry point.
    /// </remarks>
    [Remote, Fetch]
    public Task FetchFromServer(string query, [Service] IEmployeeRepository repository)
    {
        // This code executes on the server
        Result = $"Server executed query: {query}";
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/EmployeeRemoteDemo.cs#L6-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-remote' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate demonstrating CancellationToken usage.
/// </summary>
[Factory]
public partial class EmployeeCancellation
{
    public Guid Id { get; private set; }
    public bool Completed { get; private set; }

    [Create]
    public EmployeeCancellation()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an employee with proper CancellationToken handling.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken cancellationToken)
    {
        // Check cancellation before starting
        cancellationToken.ThrowIfCancellationRequested();

        // Pass token to async repository call
        var entity = await repository.GetByIdAsync(id, cancellationToken);

        // Check cancellation during processing
        if (cancellationToken.IsCancellationRequested)
            return;

        if (entity != null)
        {
            Id = entity.Id;
            Completed = true;
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/ParameterSamples.cs#L6-L48' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-cancellation' title='Start of snippet'>anchor</a></sup>
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
/// Employee aggregate demonstrating value parameter types.
/// </summary>
[Factory]
public partial class EmployeeParamsValue
{
    public int YearsOfService { get; private set; }
    public string Department { get; private set; } = "";
    public DateTime ReviewDate { get; private set; }
    public decimal BonusAmount { get; private set; }

    [Create]
    public EmployeeParamsValue()
    {
    }

    /// <summary>
    /// Fetches employee data using various serializable value parameter types.
    /// </summary>
    [Remote, Fetch]
    public Task Fetch(int yearsOfService, string department, DateTime reviewDate, decimal bonusAmount)
    {
        YearsOfService = yearsOfService;
        Department = department;
        ReviewDate = reviewDate;
        BonusAmount = bonusAmount;
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/ParameterSamples.cs#L50-L80' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-value' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Service parameters**: Injected from DI, not serialized
<!-- snippet: operations-params-service -->
<a id='snippet-operations-params-service'></a>
```cs
/// <summary>
/// Employee aggregate demonstrating service parameter injection.
/// </summary>
[Factory]
public partial class EmployeeParamsService
{
    public bool ServicesInjected { get; private set; }

    [Create]
    public EmployeeParamsService()
    {
    }

    /// <summary>
    /// Fetches data with multiple injected services.
    /// Services are resolved from DI container on server.
    /// </summary>
    [Remote, Fetch]
    public Task Fetch(
        Guid id,
        [Service] IEmployeeRepository employeeRepo,
        [Service] IDepartmentRepository departmentRepo,
        [Service] IUserContext userContext)
    {
        // Services are resolved from DI container on server
        ServicesInjected = employeeRepo != null && departmentRepo != null && userContext != null;
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/ParameterSamples.cs#L82-L112' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-service' title='Start of snippet'>anchor</a></sup>
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
<!-- endSnippet -->

**CancellationToken**: Optional, always last parameter
<!-- snippet: operations-params-cancellation -->
<a id='snippet-operations-params-cancellation'></a>
```cs
/// <summary>
/// Employee aggregate demonstrating optional CancellationToken parameter.
/// </summary>
[Factory]
public partial class EmployeeParamsCancellation
{
    public bool Completed { get; private set; }

    [Create]
    public EmployeeParamsCancellation()
    {
    }

    /// <summary>
    /// Fetches data with optional CancellationToken.
    /// CancellationToken is optional - receives default if not provided by caller.
    /// </summary>
    [Remote, Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken cancellationToken = default)
    {
        await repository.GetByIdAsync(id, cancellationToken);
        Completed = true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Operations/ParameterSamples.cs#L114-L142' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-cancellation' title='Start of snippet'>anchor</a></sup>
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
