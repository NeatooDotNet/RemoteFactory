using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Interfaces;

#region interfaces-factoryonstart
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
#endregion

#region interfaces-factoryonstart-async
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
#endregion

#region interfaces-factoryoncomplete
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
#endregion

#region interfaces-factoryoncomplete-async
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
#endregion

#region interfaces-factoryoncancelled
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
#endregion

#region interfaces-factoryoncancelled-async
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
#endregion

#region interfaces-lifecycle-order
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
#endregion
