---
title: Interfaces Reference
nav_order: 11
---

# Interfaces Reference

RemoteFactory provides interfaces for lifecycle hooks, authorization, state management, and save routing. Implement these interfaces on your domain models to integrate with factory-generated code.

## Lifecycle Hooks

### IFactoryOnStart

Called before a factory operation executes.

```csharp
public interface IFactoryOnStart
{
    void FactoryStart(FactoryOperation factoryOperation);
}
```

**When to use:** Pre-operation validation, logging, or setup that doesn't require async work.

Implement this interface on your domain model to receive a callback before any factory operation:

<!-- snippet: interfaces-factoryonstart -->
<a id='snippet-interfaces-factoryonstart'></a>
```cs
/// <summary>
/// Employee aggregate implementing IFactoryOnStart for pre-operation validation.
/// </summary>
[Factory]
public partial class EmployeeWithOnStart : IFactoryOnStart, IFactorySaveMeta
{
    public Guid EmployeeId { get; private set; }
    public string Name { get; set; } = "";
    public FactoryOperation? StartedOperation { get; private set; }
    public DateTime? StartTime { get; private set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithOnStart()
    {
        EmployeeId = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Called before any factory operation executes.
    /// Validates business rules before the operation proceeds.
    /// </summary>
    public void FactoryStart(FactoryOperation factoryOperation)
    {
        StartedOperation = factoryOperation;
        StartTime = DateTime.UtcNow;

        // For Delete operation, throw if EmployeeId is empty (cannot delete unsaved employee)
        if (factoryOperation == FactoryOperation.Delete && EmployeeId == Guid.Empty)
        {
            throw new InvalidOperationException("Cannot delete an employee that has not been saved.");
        }
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(EmployeeId, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L6-L61' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryonstart' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-factoryonstart-1'></a>
```cs
/// <summary>
/// Called before any factory operation begins.
/// Use for pre-operation validation or setup.
/// </summary>
public void FactoryStart(FactoryOperation factoryOperation)
{
    OnStartCalled = true;
    LastOperation = factoryOperation;

    // Pre-save validation for write operations
    if (factoryOperation == FactoryOperation.Insert ||
        factoryOperation == FactoryOperation.Update)
    {
        if (string.IsNullOrWhiteSpace(FirstName))
            throw new InvalidOperationException("FirstName is required");

        if (string.IsNullOrWhiteSpace(LastName))
            throw new InvalidOperationException("LastName is required");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/EmployeeWithSave.cs#L56-L77' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryonstart-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnStartAsync

Async version of `IFactoryOnStart`.

```csharp
public interface IFactoryOnStartAsync
{
    Task FactoryStartAsync(FactoryOperation factoryOperation);
}
```

**When to use:** Pre-operation work that requires async calls (database queries, external services).

Async pre-operation hook with database access:

<!-- snippet: interfaces-factoryonstart-async -->
<a id='snippet-interfaces-factoryonstart-async'></a>
```cs
/// <summary>
/// Demonstrates IFactoryOnStartAsync for async pre-operation work.
/// </summary>
[Factory]
public partial class EmployeeAsyncStart : IFactorySaveMeta, IFactoryOnStartAsync
{
    private readonly IEmployeeRepository? _repository;

    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Services accessed via constructor injection on the domain class.
    /// </summary>
    public EmployeeAsyncStart(IEmployeeRepository repository)
    {
        _repository = repository;
        Id = Guid.NewGuid();
    }

    [Create]
    public EmployeeAsyncStart()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Async pre-operation validation using constructor-injected repository.
    /// </summary>
    public async Task FactoryStartAsync(FactoryOperation factoryOperation)
    {
        if (factoryOperation == FactoryOperation.Insert && _repository != null)
        {
            // Validate department exists before insert
            var employees = await _repository.GetByDepartmentIdAsync(DepartmentId, default);
            if (employees.Count >= 100)
            {
                throw new InvalidOperationException(
                    "Department has reached maximum capacity of 100 employees");
            }
        }
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = DepartmentId, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L8-L70' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryonstart-async' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-factoryonstart-async-1'></a>
```cs
/// <summary>
/// Department aggregate implementing IFactoryOnStartAsync for async pre-operation validation.
/// </summary>
[Factory]
public partial class DepartmentWithAsyncOnStart : IFactoryOnStartAsync, IFactorySaveMeta
{
    public Guid DepartmentId { get; private set; }
    public string Name { get; set; } = "";
    public bool PreConditionsValidated { get; private set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    private IDepartmentRepository? _repository;

    [Create]
    public DepartmentWithAsyncOnStart()
    {
        DepartmentId = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IDepartmentRepository repository, CancellationToken ct)
    {
        _repository = repository;
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        DepartmentId = entity.Id;
        Name = entity.Name;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Called before any factory operation executes.
    /// Validates department budget limits via async database query before operation.
    /// </summary>
    public async Task FactoryStartAsync(FactoryOperation factoryOperation)
    {
        if (_repository == null)
        {
            PreConditionsValidated = true;
            return;
        }

        // Check existing department count to enforce budget constraints
        var existingDepartments = await _repository.GetAllAsync();
        if (factoryOperation == FactoryOperation.Insert && existingDepartments.Count >= 100)
        {
            throw new InvalidOperationException("Maximum department limit reached.");
        }

        PreConditionsValidated = true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IDepartmentRepository repository, CancellationToken ct)
    {
        _repository = repository;
        var entity = new DepartmentEntity
        {
            Id = DepartmentId,
            Name = Name,
            Code = Name.Length >= 3 ? Name.ToUpperInvariant()[..3] : Name.ToUpperInvariant()
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L63-L134' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryonstart-async-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnComplete

Called after a factory operation completes successfully.

```csharp
public interface IFactoryOnComplete
{
    void FactoryComplete(FactoryOperation factoryOperation);
}
```

**When to use:** Post-operation cleanup, audit logging, or state updates that don't require async work.

Implement this interface to track successful operations:

<!-- snippet: interfaces-factoryoncomplete -->
<a id='snippet-interfaces-factoryoncomplete'></a>
```cs
/// <summary>
/// Employee aggregate implementing IFactoryOnComplete for post-operation tracking.
/// </summary>
[Factory]
public partial class EmployeeWithOnComplete : IFactoryOnComplete
{
    public Guid EmployeeId { get; private set; }
    public string Name { get; set; } = "";
    public FactoryOperation? CompletedOperation { get; private set; }
    public DateTime? CompleteTime { get; private set; }

    [Create]
    public EmployeeWithOnComplete()
    {
        EmployeeId = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        return true;
    }

    /// <summary>
    /// Called after factory operation completes successfully.
    /// Tracks successful operation for audit logging.
    /// </summary>
    public void FactoryComplete(FactoryOperation factoryOperation)
    {
        CompletedOperation = factoryOperation;
        CompleteTime = DateTime.UtcNow;

        // Post-operation logic: audit logging, cache invalidation, etc.
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L136-L177' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncomplete' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-factoryoncomplete-1'></a>
```cs
/// <summary>
/// Called when factory operation succeeds.
/// Use for post-operation state updates, logging, or notifications.
/// </summary>
public void FactoryComplete(FactoryOperation factoryOperation)
{
    OnCompleteCalled = true;
    LastOperation = factoryOperation;

    // Increment version after successful save
    if (factoryOperation == FactoryOperation.Insert ||
        factoryOperation == FactoryOperation.Update)
    {
        Version++;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/EmployeeWithSave.cs#L79-L96' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncomplete-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnCompleteAsync

Async version of `IFactoryOnComplete`.

```csharp
public interface IFactoryOnCompleteAsync
{
    Task FactoryCompleteAsync(FactoryOperation factoryOperation);
}
```

**When to use:** Post-operation work that requires async calls (notifications, external logging).

Async post-operation hook for notifications:

<!-- snippet: interfaces-factoryoncomplete-async -->
<a id='snippet-interfaces-factoryoncomplete-async'></a>
```cs
/// <summary>
/// Demonstrates IFactoryOnCompleteAsync for async post-operation work.
/// </summary>
[Factory]
public partial class EmployeeAsyncComplete : IFactorySaveMeta, IFactoryOnCompleteAsync
{
    private readonly IEmailService? _emailService;

    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Services accessed via constructor injection on the domain class.
    /// </summary>
    public EmployeeAsyncComplete(IEmailService emailService)
    {
        _emailService = emailService;
        Id = Guid.NewGuid();
    }

    [Create]
    public EmployeeAsyncComplete()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Async post-operation notification using constructor-injected service.
    /// </summary>
    public async Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        if (factoryOperation == FactoryOperation.Insert && _emailService != null)
        {
            await _emailService.SendAsync(
                Email,
                "Welcome!",
                $"Welcome to the team, {FirstName}!",
                default);
        }
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = Email, DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L72-L131' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncomplete-async' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-factoryoncomplete-async-1'></a>
```cs
/// <summary>
/// Service for sending notifications.
/// </summary>
public interface IAsyncNotificationService
{
    Task SendAsync(string recipient, string message);
}

/// <summary>
/// Employee aggregate implementing IFactoryOnCompleteAsync for async post-operation notifications.
/// </summary>
[Factory]
public partial class EmployeeWithAsyncOnComplete : IFactoryOnCompleteAsync
{
    public Guid EmployeeId { get; private set; }
    public string Name { get; set; } = "";
    public bool PostProcessingComplete { get; private set; }

    private IAsyncNotificationService? _notificationService;

    [Create]
    public EmployeeWithAsyncOnComplete()
    {
        EmployeeId = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        [Service] IAsyncNotificationService notificationService,
        CancellationToken ct)
    {
        _notificationService = notificationService;
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        return true;
    }

    /// <summary>
    /// Called after factory operation completes successfully.
    /// Sends notification via injected IAsyncNotificationService.
    /// </summary>
    public async Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        if (_notificationService != null)
        {
            var message = $"Employee operation {factoryOperation} completed for {Name}";
            await _notificationService.SendAsync("admin@company.com", message);
        }
        PostProcessingComplete = true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L179-L236' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncomplete-async-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnCancelled

Called when a factory operation is cancelled via `CancellationToken`.

```csharp
public interface IFactoryOnCancelled
{
    void FactoryCancelled(FactoryOperation factoryOperation);
}
```

**When to use:** Cleanup after operation cancellation, rollback logic, or cancellation logging.

Handle operation cancellation:

<!-- snippet: interfaces-factoryoncancelled -->
<a id='snippet-interfaces-factoryoncancelled'></a>
```cs
/// <summary>
/// Employee aggregate implementing IFactoryOnCancelled for cancellation handling.
/// </summary>
[Factory]
public partial class EmployeeWithOnCancelled : IFactoryOnCancelled
{
    public Guid EmployeeId { get; private set; }
    public string Name { get; set; } = "";
    public FactoryOperation? CancelledOperation { get; private set; }
    public bool CleanupPerformed { get; private set; }

    [Create]
    public EmployeeWithOnCancelled()
    {
        EmployeeId = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        // Demonstrates cancellation point in fetch operation
        ct.ThrowIfCancellationRequested();

        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        return true;
    }

    /// <summary>
    /// Called when factory operation is cancelled via CancellationToken.
    /// Handles cleanup when operation is cancelled.
    /// </summary>
    public void FactoryCancelled(FactoryOperation factoryOperation)
    {
        CancelledOperation = factoryOperation;
        CleanupPerformed = true;

        // Perform cleanup logic for cancelled operation
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L238-L282' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncancelled' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-factoryoncancelled-1'></a>
```cs
/// <summary>
/// Called when factory operation is cancelled via CancellationToken.
/// Use for cleanup or logging of cancellation.
/// </summary>
public void FactoryCancelled(FactoryOperation factoryOperation)
{
    OnCancelledCalled = true;
    LastOperation = factoryOperation;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/EmployeeWithSave.cs#L98-L108' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncancelled-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnCancelledAsync

Async version of `IFactoryOnCancelled`.

```csharp
public interface IFactoryOnCancelledAsync
{
    Task FactoryCancelledAsync(FactoryOperation factoryOperation);
}
```

**When to use:** Async cleanup after cancellation (database rollback, external API calls).

Async cancellation with database rollback:

<!-- snippet: interfaces-factoryoncancelled-async -->
<a id='snippet-interfaces-factoryoncancelled-async'></a>
```cs
/// <summary>
/// Demonstrates IFactoryOnCancelledAsync for async cancellation cleanup.
/// </summary>
[Factory]
public partial class EmployeeAsyncCancelled : IFactorySaveMeta, IFactoryOnCancelledAsync
{
    private readonly IAuditLogService? _auditLog;

    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Services accessed via constructor injection on the domain class.
    /// </summary>
    public EmployeeAsyncCancelled(IAuditLogService auditLog)
    {
        _auditLog = auditLog;
        Id = Guid.NewGuid();
    }

    [Create]
    public EmployeeAsyncCancelled()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Async cancellation handling with constructor-injected audit service.
    /// </summary>
    public async Task FactoryCancelledAsync(FactoryOperation factoryOperation)
    {
        if (_auditLog != null)
        {
            await _auditLog.LogAsync(
                "Cancelled",
                Id,
                "Employee",
                $"Operation {factoryOperation} was cancelled",
                default);
        }
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L133-L190' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncancelled-async' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-factoryoncancelled-async-1'></a>
```cs
/// <summary>
/// Unit of work interface for transaction management.
/// </summary>
public interface IUnitOfWork
{
    Task RollbackAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
}

/// <summary>
/// Employee aggregate implementing IFactoryOnCancelledAsync for async cancellation cleanup.
/// </summary>
[Factory]
public partial class EmployeeWithAsyncOnCancelled : IFactoryOnCancelledAsync
{
    public Guid EmployeeId { get; private set; }
    public string Name { get; set; } = "";
    public bool AsyncCleanupComplete { get; private set; }

    private IUnitOfWork? _unitOfWork;

    [Create]
    public EmployeeWithAsyncOnCancelled()
    {
        EmployeeId = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        [Service] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        _unitOfWork = unitOfWork;

        // Demonstrates cancellable async operation
        ct.ThrowIfCancellationRequested();

        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        return true;
    }

    /// <summary>
    /// Called when factory operation is cancelled.
    /// Performs async cleanup via injected IUnitOfWork to rollback partial changes.
    /// </summary>
    public async Task FactoryCancelledAsync(FactoryOperation factoryOperation)
    {
        if (_unitOfWork != null)
        {
            await _unitOfWork.RollbackAsync();
        }
        AsyncCleanupComplete = true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L284-L345' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncancelled-async-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Lifecycle Hook Execution Order

When a factory operation executes:

1. `IFactoryOnStart` / `IFactoryOnStartAsync` - Before operation
2. Factory operation method (`[Create]`, `[Fetch]`, `[Update]`, etc.)
3. `IFactoryOnComplete` / `IFactoryOnCompleteAsync` - After successful operation

If cancelled:
- `IFactoryOnCancelled` / `IFactoryOnCancelledAsync` - After `OperationCanceledException`

Combining sync and async hooks:

<!-- snippet: interfaces-lifecycle-order -->
<a id='snippet-interfaces-lifecycle-order'></a>
```cs
/// <summary>
/// Employee aggregate implementing all three lifecycle interfaces to demonstrate execution order.
/// </summary>
[Factory]
public partial class EmployeeWithLifecycleOrder : IFactoryOnStart, IFactoryOnComplete, IFactoryOnCancelled
{
    public Guid EmployeeId { get; private set; }
    public string Name { get; set; } = "";
    public List<string> LifecycleEvents { get; } = new();

    [Create]
    public EmployeeWithLifecycleOrder()
    {
        EmployeeId = Guid.NewGuid();
    }

    // 1. Start: Called before operation executes
    public void FactoryStart(FactoryOperation factoryOperation)
    {
        LifecycleEvents.Add($"Start: {factoryOperation}");
    }

    // 2. Operation: The actual factory method executes
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        LifecycleEvents.Add("Operation: Fetch");

        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        return true;
    }

    // 3a. Complete: Called after successful operation
    public void FactoryComplete(FactoryOperation factoryOperation)
    {
        LifecycleEvents.Add($"Complete: {factoryOperation}");
    }

    // 3b. Cancelled: Called if operation was cancelled (instead of Complete)
    public void FactoryCancelled(FactoryOperation factoryOperation)
    {
        LifecycleEvents.Add($"Cancelled: {factoryOperation}");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L347-L396' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-lifecycle-order' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-lifecycle-order-1'></a>
```cs
/// <summary>
/// Employee aggregate demonstrating the complete IFactorySaveMeta workflow
/// with all lifecycle hooks: IFactoryOnStart, IFactoryOnComplete, IFactoryOnCancelled.
/// Execution order: FactoryStart -> Operation -> FactoryComplete (or FactoryCancelled).
/// </summary>
[Factory]
public partial class EmployeeWithSave : IFactorySaveMeta, IFactoryOnStart, IFactoryOnComplete, IFactoryOnCancelled
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public string Position { get; set; } = "";
    public decimal Salary { get; set; }
    public int Version { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    // Tracking properties for lifecycle demonstration
    public bool OnStartCalled { get; private set; }
    public bool OnCompleteCalled { get; private set; }
    public bool OnCancelledCalled { get; private set; }
    public FactoryOperation? LastOperation { get; private set; }

    [Create]
    public EmployeeWithSave()
    {
        Id = Guid.NewGuid();
        Version = 1;
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        DepartmentId = entity.DepartmentId;
        Position = entity.Position;
        Salary = entity.SalaryAmount;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Called before any factory operation begins.
    /// Use for pre-operation validation or setup.
    /// </summary>
    public void FactoryStart(FactoryOperation factoryOperation)
    {
        OnStartCalled = true;
        LastOperation = factoryOperation;

        // Pre-save validation for write operations
        if (factoryOperation == FactoryOperation.Insert ||
            factoryOperation == FactoryOperation.Update)
        {
            if (string.IsNullOrWhiteSpace(FirstName))
                throw new InvalidOperationException("FirstName is required");

            if (string.IsNullOrWhiteSpace(LastName))
                throw new InvalidOperationException("LastName is required");
        }
    }

    /// <summary>
    /// Called when factory operation succeeds.
    /// Use for post-operation state updates, logging, or notifications.
    /// </summary>
    public void FactoryComplete(FactoryOperation factoryOperation)
    {
        OnCompleteCalled = true;
        LastOperation = factoryOperation;

        // Increment version after successful save
        if (factoryOperation == FactoryOperation.Insert ||
            factoryOperation == FactoryOperation.Update)
        {
            Version++;
        }
    }

    /// <summary>
    /// Called when factory operation is cancelled via CancellationToken.
    /// Use for cleanup or logging of cancellation.
    /// </summary>
    public void FactoryCancelled(FactoryOperation factoryOperation)
    {
        OnCancelledCalled = true;
        LastOperation = factoryOperation;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        // Check cancellation before proceeding
        ct.ThrowIfCancellationRequested();

        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            DepartmentId = DepartmentId,
            Position = Position,
            SalaryAmount = Salary,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            DepartmentId = DepartmentId,
            Position = Position,
            SalaryAmount = Salary,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };

        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Save/EmployeeWithSave.cs#L6-L161' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-lifecycle-order-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Save Operation

### IFactorySaveMeta

Provides state properties that the generated factory's `Save` method uses to route to Insert, Update, or Delete.

```csharp
public interface IFactorySaveMeta
{
    bool IsDeleted { get; }
    bool IsNew { get; }
}
```

**Routing logic:**
- `IsNew = true, IsDeleted = false` → Insert
- `IsNew = false, IsDeleted = false` → Update
- `IsNew = false, IsDeleted = true` → Delete
- `IsNew = true, IsDeleted = true` → No operation (new item deleted before save)

Implement this interface on domain models that use the Save pattern:

<!-- snippet: interfaces-factorysavemeta -->
<a id='snippet-interfaces-factorysavemeta'></a>
```cs
/// <summary>
/// Demonstrates IFactorySaveMeta for save state tracking.
/// </summary>
[Factory]
public partial class EmployeeSaveDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    /// <summary>
    /// True for new entities not yet persisted.
    /// Set to true in constructor, false after Fetch or successful Insert.
    /// </summary>
    public bool IsNew { get; private set; } = true;

    /// <summary>
    /// True for entities marked for deletion.
    /// Set by application code before calling Save().
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Create sets IsNew = true for new entities.
    /// </summary>
    [Create]
    public EmployeeSaveDemo()
    {
        Id = Guid.NewGuid();
        IsNew = true;  // New entity
    }

    /// <summary>
    /// Fetch sets IsNew = false for existing entities.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false;  // Existing entity
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
        IsNew = false;  // No longer new after insert
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L192-L277' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factorysavemeta' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-factorysavemeta-1'></a>
```cs
/// <summary>
/// Employee aggregate implementing IFactorySaveMeta for Save operation routing.
/// </summary>
[Factory]
public partial class EmployeeWithSaveMeta : IFactorySaveMeta
{
    public Guid EmployeeId { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new employee. Save will call Insert.
    /// </summary>
    [Create]
    public EmployeeWithSaveMeta()
    {
        EmployeeId = Guid.NewGuid();
        IsNew = true;  // Save will call Insert
    }

    /// <summary>
    /// Fetches an existing employee. Save will call Update.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        IsNew = false;  // Save will call Update
        return true;
    }

    /// <summary>
    /// Inserts a new employee into the database.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var parts = Name.Split(' ', 2);
        var entity = new EmployeeEntity
        {
            Id = EmployeeId,
            FirstName = parts.Length > 0 ? parts[0] : "",
            LastName = parts.Length > 1 ? parts[1] : "",
            Email = $"{Name.Replace(" ", ".", StringComparison.Ordinal).ToUpperInvariant()}@company.com",
            DepartmentId = Guid.Empty,
            Position = "New Hire",
            SalaryAmount = 50000,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;  // After insert, no longer new
    }

    /// <summary>
    /// Updates an existing employee in the database.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(EmployeeId, ct);
        if (entity == null) return;

        var parts = Name.Split(' ', 2);
        entity.FirstName = parts.Length > 0 ? parts[0] : "";
        entity.LastName = parts.Length > 1 ? parts[1] : "";
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Deletes the employee from the database.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(EmployeeId, ct);
        await repository.SaveChangesAsync(ct);
    }
}

// Save routing logic:
// IsNew=true, IsDeleted=false  -> Insert
// IsNew=false, IsDeleted=false -> Update
// IsNew=false, IsDeleted=true  -> Delete
// IsNew=true, IsDeleted=true   -> No operation (new item deleted before save)
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/SaveMetaSamples.cs#L6-L99' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factorysavemeta-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See [Save Operation](save-operation.md) for complete usage details.

### IFactorySave&lt;T&gt;

Generated factories implement this interface when the domain model implements `IFactorySaveMeta`.

```csharp
public interface IFactorySave<T> where T : IFactorySaveMeta
{
    Task<IFactorySaveMeta?> Save(T entity, CancellationToken cancellationToken = default);
}
```

**You do not implement this interface.** The generator creates it automatically.

Using the generated Save method:

<!-- snippet: interfaces-factorysave -->
<a id='snippet-interfaces-factorysave'></a>
```cs
/// <summary>
/// Demonstrates IFactorySave&lt;T&gt; usage from the generated factory.
/// </summary>
public class EmployeeSaveDemo
{
    private readonly IEmployeeWithSaveMetaFactory _factory;

    public EmployeeSaveDemo(IEmployeeWithSaveMetaFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Demonstrates the complete Save lifecycle: Create, Insert, Update, Delete.
    /// </summary>
    public async Task DemonstrateSaveLifecycleAsync()
    {
        // Create new employee
        var employee = _factory.Create();
        employee.Name = "John Smith";

        // First Save (Insert): IsNew=true -> Insert
        var saved = await _factory.Save(employee);

        // Assert saved is not null and IsNew is false after save
        if (saved == null)
            throw new InvalidOperationException("Save returned null");
        if (saved.IsNew)
            throw new InvalidOperationException("IsNew should be false after insert");

        // Second Save (Update): IsNew=false -> Update
        var savedEmployee = (EmployeeWithSaveMeta)saved;
        savedEmployee.Name = "Jane Smith";
        await _factory.Save(savedEmployee);

        // Third Save (Delete): IsDeleted=true -> Delete
        savedEmployee.IsDeleted = true;
        await _factory.Save(savedEmployee);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Interfaces/FactorySaveSamples.cs#L6-L47' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factorysave' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-factorysave-1'></a>
```cs
/// <summary>
/// Demonstrates using the generated IFactorySave interface.
/// </summary>
[Factory]
public partial class EmployeeFactorySaveDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeFactorySaveDemo() { Id = Guid.NewGuid(); }

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

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}

// Usage example (would be in a consumer/test project):
// var factory = serviceProvider.GetRequiredService<IEmployeeFactorySaveDemoFactory>();
// var employee = factory.Create();
// employee.FirstName = "John";
// var saved = await factory.Save(employee);  // IFactorySave<T>.Save()
// Assert.False(saved?.IsNew);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L459-L528' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factorysave-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Authorization

### IAspAuthorize

Performs ASP.NET Core authorization checks on the server. Injected into factory operations decorated with `[AspAuthorize]`.

```csharp
public interface IAspAuthorize
{
    Task<string?> Authorize(
        IEnumerable<AspAuthorizeData> authorizeData,
        bool forbid = false);
}
```

**When to use:** Custom ASP.NET Core authorization implementations that need different policy evaluation logic.

**You rarely implement this interface.** The default implementation (`AspAuthorize`) is registered automatically by `AddNeatooAspNetCore()` and integrates with ASP.NET Core's `IAuthorizationPolicyProvider` and `IPolicyEvaluator`.

**Return value:**
- Empty string if authorized
- Error message string if not authorized
- Throws `AspForbidException` if `forbid = true` and authorization fails

Custom authorization implementation:

<!-- snippet: interfaces-aspauthorize -->
<a id='snippet-interfaces-aspauthorize'></a>
```cs
/// <summary>
/// Custom IAspAuthorize implementation for logging and custom policy evaluation.
/// IAspAuthorize is commonly implemented for custom authorization requirements.
/// </summary>
public class AuditingAspAuthorize : IAspAuthorize
{
    private readonly IAspAuthorize _inner;
    private readonly IAuditLogService _auditLog;

    public AuditingAspAuthorize(
        IAspAuthorize inner,
        IAuditLogService auditLog)
    {
        _inner = inner;
        _auditLog = auditLog;
    }

    /// <summary>
    /// Custom implementation that logs authorization attempts.
    /// </summary>
    public async Task<string?> Authorize(
        IEnumerable<AspAuthorizeData> authorizeData,
        bool forbid = false)
    {
        // Log authorization attempt
        var policies = string.Join(", ",
            authorizeData.Select(a => a.Policy ?? a.Roles ?? "Default"));

        await _auditLog.LogAsync(
            "AuthorizationCheck",
            Guid.Empty,
            "Authorization",
            $"Checking policies: {policies}",
            default);

        // Delegate to inner implementation
        var result = await _inner.Authorize(authorizeData, forbid);

        // Log result
        var success = string.IsNullOrEmpty(result);
        await _auditLog.LogAsync(
            success ? "AuthorizationSuccess" : "AuthorizationFailed",
            Guid.Empty,
            "Authorization",
            success ? "Authorized" : $"Denied: {result}",
            default);

        return result;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L406-L457' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-aspauthorize' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-aspauthorize-1'></a>
```cs
// Custom IAspAuthorize for testing or non-ASP.NET Core environments

/// <summary>
/// Custom IAspAuthorize implementation for simplified authorization.
/// </summary>
public class CustomAspAuthorize : IAspAuthorize
{
    private readonly IUserContext _userContext;

    public CustomAspAuthorize(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Performs authorization checks based on AspAuthorizeData requirements.
    /// </summary>
    /// <param name="authorizeData">Collection of authorization requirements.</param>
    /// <param name="forbid">If true, throws AspForbidException on failure.</param>
    /// <returns>Empty string if authorized, error message if not authorized.</returns>
    public Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false)
    {
        // Check if user is authenticated
        if (!_userContext.IsAuthenticated)
        {
            if (forbid)
                throw new AspForbidException("User is not authenticated.");
            return Task.FromResult<string?>("User is not authenticated.");
        }

        // Iterate through AspAuthorizeData to check Roles requirements
        foreach (var data in authorizeData)
        {
            if (!string.IsNullOrEmpty(data.Roles))
            {
                var requiredRoles = data.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var hasRequiredRole = requiredRoles.Any(role => _userContext.IsInRole(role.Trim()));

                if (!hasRequiredRole)
                {
                    if (forbid)
                        throw new AspForbidException($"User does not have required role(s): {data.Roles}");
                    return Task.FromResult<string?>($"User does not have required role(s): {data.Roles}");
                }
            }
        }

        // Return empty string on success
        return Task.FromResult<string?>(string.Empty);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/Interfaces/AuthorizationSamples.cs#L6-L58' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-aspauthorize-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See [Authorization](authorization.md) for standard authorization patterns.

## Serialization

### IOrdinalSerializable

Interface for types that support ordinal (positional) JSON serialization. Types implementing this interface are serialized as JSON arrays instead of objects, reducing payload size.

```csharp
public interface IOrdinalSerializable
{
    object?[] ToOrdinalArray();
}
```

**When to use:** Types that need compact array-based serialization instead of the default object-based format. The source generator automatically implements this for types with `[Factory]` attribute.

**Ordinal order:** Properties are serialized alphabetically by name. For inherited types, base class properties come first (alphabetically), followed by derived class properties (alphabetically).

Example of implementing IOrdinalSerializable:

<!-- snippet: interfaces-ordinalserializable -->
<a id='snippet-interfaces-ordinalserializable'></a>
```cs
/// <summary>
/// Money value object implementing IOrdinalSerializable.
/// Useful for value objects and third-party types that cannot use [Factory].
/// </summary>
public class MoneyValueObject : IOrdinalSerializable
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyValueObject(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Returns properties in alphabetical order for ordinal serialization.
    /// Order: Amount, Currency (alphabetical)
    /// </summary>
    public object?[] ToOrdinalArray()
    {
        // Alphabetical order: Amount, Currency
        return [Amount, Currency];
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L279-L305' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-ordinalserializable' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-ordinalserializable-1'></a>
```cs
/// <summary>
/// Value object implementing IOrdinalSerializable for compact JSON serialization.
/// </summary>
public class EmployeeSnapshot : IOrdinalSerializable
{
    public string DepartmentCode { get; set; } = "";  // Index 0 (alphabetically first)
    public int EmployeeCount { get; set; }            // Index 1
    public DateTime LastUpdated { get; set; }          // Index 2

    /// <summary>
    /// Converts the object to an array of property values in alphabetical order.
    /// </summary>
    public object?[] ToOrdinalArray()
    {
        // Properties in alphabetical order: DepartmentCode, EmployeeCount, LastUpdated
        return [DepartmentCode, EmployeeCount, LastUpdated];
    }
}

// JSON comparison:
// Array format:  ["HR", 42, "2024-01-15T10:30:00Z"]
// Object format: {"DepartmentCode":"HR","EmployeeCount":42,"LastUpdated":"2024-01-15T10:30:00Z"}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/SerializationSamples.cs#L7-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-ordinalserializable-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See [Serialization](serialization.md) for details on ordinal format.

### IOrdinalConverterProvider&lt;TSelf&gt;

Provides a custom `JsonConverter` for ordinal serialization. Uses static abstract interface members for AOT compatibility.

```csharp
public interface IOrdinalConverterProvider<TSelf> where TSelf : class
{
    static abstract JsonConverter<TSelf> CreateOrdinalConverter();
}
```

**When to use:** Types implementing `IOrdinalSerializable` that need a custom converter for compact serialization. The source generator automatically implements this for `[Factory]` types.

**This is a static abstract interface (C# 11+).** The implementing type provides a static factory method for its converter.

Custom ordinal converter for a value object:

<!-- snippet: interfaces-ordinalconverterprovider -->
<a id='snippet-interfaces-ordinalconverterprovider'></a>
```cs
/// <summary>
/// Money value object implementing IOrdinalConverterProvider for custom converter.
/// </summary>
public class MoneyWithConverter : IOrdinalSerializable, IOrdinalConverterProvider<MoneyWithConverter>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyWithConverter(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public object?[] ToOrdinalArray()
    {
        return [Amount, Currency];
    }

    /// <summary>
    /// Static factory method provides custom converter.
    /// Required for types implementing IOrdinalConverterProvider.
    /// </summary>
    public static JsonConverter<MoneyWithConverter> CreateOrdinalConverter()
    {
        return new MoneyOrdinalConverter();
    }

    /// <summary>
    /// Custom ordinal converter for Money.
    /// </summary>
    private sealed class MoneyOrdinalConverter : JsonConverter<MoneyWithConverter>
    {
        public override MoneyWithConverter Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            // Expect array: [amount, currency]
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected array for Money");

            reader.Read();
            var amount = reader.GetDecimal();

            reader.Read();
            var currency = reader.GetString() ?? "USD";

            reader.Read(); // EndArray

            return new MoneyWithConverter(amount, currency);
        }

        public override void Write(
            Utf8JsonWriter writer,
            MoneyWithConverter value,
            JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.Amount);
            writer.WriteStringValue(value.Currency);
            writer.WriteEndArray();
        }
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L307-L373' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-ordinalconverterprovider' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-ordinalconverterprovider-1'></a>
```cs
// IOrdinalConverterProvider<TSelf> enables types to provide their own ordinal converter

/// <summary>
/// Money value object implementing IOrdinalConverterProvider for custom ordinal serialization.
/// </summary>
public class Money : IOrdinalConverterProvider<Money>
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Creates the ordinal converter for this type.
    /// </summary>
    public static JsonConverter<Money> CreateOrdinalConverter()
    {
        return new MoneyOrdinalConverter();
    }
}

/// <summary>
/// Custom ordinal converter for Money value object.
/// </summary>
public class MoneyOrdinalConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array");

        reader.Read();
        var amount = reader.GetDecimal();

        reader.Read();
        var currency = reader.GetString() ?? "USD";

        reader.Read(); // EndArray

        return new Money { Amount = amount, Currency = currency };
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Amount);
        writer.WriteStringValue(value.Currency);
        writer.WriteEndArray();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/SerializationSamples.cs#L32-L81' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-ordinalconverterprovider-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IOrdinalSerializationMetadata

Provides metadata about ordinal serialization for a specific type. Used by the serializer to reconstruct objects from ordinal arrays.

```csharp
public interface IOrdinalSerializationMetadata
{
    static abstract string[] PropertyNames { get; }
    static abstract Type[] PropertyTypes { get; }
    static abstract object FromOrdinalArray(object?[] values);
}
```

**You do not implement this interface.** The source generator automatically implements it for types with `[Factory]` attribute to enable ordinal deserialization.

**PropertyNames and PropertyTypes:** Arrays in ordinal order (alphabetical by property name, base class properties first).

**FromOrdinalArray:** Creates an instance from an array of property values in ordinal order.

## Event Tracking

### IEventTracker

Tracks pending asynchronous event tasks for fire-and-forget operations. Enables graceful shutdown by waiting for all pending events to complete.

```csharp
public interface IEventTracker
{
    void Track(Task eventTask);
    Task WaitAllAsync(CancellationToken ct = default);
    int PendingCount { get; }
}
```

**When to use:** Application shutdown logic that needs to wait for all pending fire-and-forget events to complete before terminating.

**You rarely interact with this interface directly.** RemoteFactory uses it internally for `[Event]` delegate tracking. The default implementation is registered by `AddNeatooRemoteFactory()`.

Using IEventTracker for graceful shutdown:

<!-- snippet: interfaces-eventtracker -->
<a id='snippet-interfaces-eventtracker'></a>
```cs
/// <summary>
/// Demonstrates IEventTracker usage for graceful shutdown.
/// </summary>
public class GracefulShutdownDemo
{
    private readonly IEventTracker _eventTracker;

    public GracefulShutdownDemo(IEventTracker eventTracker)
    {
        _eventTracker = eventTracker;
    }

    /// <summary>
    /// Waits for all pending fire-and-forget events before shutdown.
    /// </summary>
    public async Task ShutdownGracefullyAsync(CancellationToken ct)
    {
        // IEventTracker monitors pending fire-and-forget events

        // Check PendingCount to see how many fire-and-forget events are in progress
        var pendingCount = _eventTracker.PendingCount;
        Console.WriteLine($"Waiting for {pendingCount} pending events to complete...");

        // Wait for all pending events to complete
        await _eventTracker.WaitAllAsync(ct);

        // Assert PendingCount equals 0 after WaitAllAsync completes
        if (_eventTracker.PendingCount != 0)
            throw new InvalidOperationException("Expected no pending events after WaitAllAsync");

        Console.WriteLine("All events completed. Safe to shutdown.");
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Interfaces/EventTrackerSamples.cs#L5-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-eventtracker' title='Start of snippet'>anchor</a></sup>
<a id='snippet-interfaces-eventtracker-1'></a>
```cs
/// <summary>
/// Demonstrates IEventTracker usage for graceful shutdown.
/// </summary>
[Factory]
public static partial class EventTrackerDemo
{
    /// <summary>
    /// Uses IEventTracker to wait for all pending events.
    /// Returns the number of events that were pending.
    /// </summary>
    [Execute]
    private static async Task<int> _WaitForEvents(
        [Service] IEventTracker eventTracker,
        CancellationToken ct)
    {
        // Check how many events are pending
        var pendingCount = eventTracker.PendingCount;

        if (pendingCount > 0)
        {
            // Wait for all pending events to complete
            // Used during graceful shutdown
            await eventTracker.WaitAllAsync(ct);
        }

        return pendingCount;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L375-L404' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-eventtracker-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Factory Core

### IFactoryCore&lt;T&gt;

Low-level factory execution abstraction. Generated factories use this internally for lifecycle hook invocation and operation tracking.

```csharp
public interface IFactoryCore<T>
{
    T DoFactoryMethodCall(FactoryOperation operation, Func<T> factoryMethodCall);
    Task<T> DoFactoryMethodCallAsync(FactoryOperation operation, Func<Task<T>> factoryMethodCall);
    Task<T?> DoFactoryMethodCallAsyncNullable(FactoryOperation operation, Func<Task<T?>> factoryMethodCall);
    T DoFactoryMethodCall(T target, FactoryOperation operation, Action factoryMethodCall);
    T? DoFactoryMethodCallBool(T target, FactoryOperation operation, Func<bool> factoryMethodCall);
    Task<T> DoFactoryMethodCallAsync(T target, FactoryOperation operation, Func<Task> factoryMethodCall);
    Task<T?> DoFactoryMethodCallBoolAsync(T target, FactoryOperation operation, Func<Task<bool>> factoryMethodCall);
}
```

**You rarely implement this interface.** The default implementation (`FactoryCore<T>`) handles lifecycle hooks (IFactoryOnStart, IFactoryOnComplete, IFactoryOnCancelled) and logging. You can register a custom implementation for a specific type to add custom factory behavior without inheritance.

## Summary

| Interface | Purpose | Who Implements |
|-----------|---------|----------------|
| `IFactoryOnStart` | Pre-operation sync hook | Domain models |
| `IFactoryOnStartAsync` | Pre-operation async hook | Domain models |
| `IFactoryOnComplete` | Post-operation sync hook | Domain models |
| `IFactoryOnCompleteAsync` | Post-operation async hook | Domain models |
| `IFactoryOnCancelled` | Cancellation sync hook | Domain models |
| `IFactoryOnCancelledAsync` | Cancellation async hook | Domain models |
| `IFactorySaveMeta` | Save routing state | Domain models |
| `IFactorySave<T>` | Save method signature | Generated factories |
| `IAspAuthorize` | ASP.NET Core authorization | Custom auth implementations |
| `IOrdinalSerializable` | Ordinal serialization marker | Domain models, value objects |
| `IOrdinalConverterProvider<TSelf>` | Ordinal converter provider | Source generator (automatic) |
| `IOrdinalSerializationMetadata` | Ordinal deserialization metadata | Source generator (automatic) |
| `IEventTracker` | Fire-and-forget event tracking | Framework (rarely customized) |
| `IFactoryCore<T>` | Factory execution pipeline | Framework (rarely customized) |

## Next Steps

- [Attributes Reference](attributes-reference.md) - All available attributes
- [Factory Operations](factory-operations.md) - CRUD operation details
- [Service Injection](service-injection.md) - DI integration
- [Authorization](authorization.md) - Authorization patterns
