using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Interfaces;

// Full working classes for compilation - snippets show only essential parts

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

    #region interfaces-factoryonstart
    // IFactoryOnStart: Called before factory operation executes
    public void FactoryStart(FactoryOperation factoryOperation)
    {
        // Pre-operation validation
        if (factoryOperation == FactoryOperation.Delete && EmployeeId == Guid.Empty)
            throw new InvalidOperationException("Cannot delete unsaved employee");
    }
    #endregion

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(EmployeeId, ct);
        await repository.SaveChangesAsync(ct);
    }
}

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

    #region interfaces-factoryonstart-async
    // IFactoryOnStartAsync: Async pre-operation hook for database/service calls
    public async Task FactoryStartAsync(FactoryOperation factoryOperation)
    {
        if (_repository == null) return;

        // Async validation: check department limit before insert
        var existing = await _repository.GetAllAsync();
        if (factoryOperation == FactoryOperation.Insert && existing.Count >= 100)
            throw new InvalidOperationException("Maximum department limit reached");
    }
    #endregion

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

    #region interfaces-factoryoncomplete
    // IFactoryOnComplete: Called after factory operation succeeds
    public void FactoryComplete(FactoryOperation factoryOperation)
    {
        CompletedOperation = factoryOperation;
        CompleteTime = DateTime.UtcNow;
        // Post-operation: audit logging, cache invalidation, etc.
    }
    #endregion
}

public interface IAsyncNotificationService
{
    Task SendAsync(string recipient, string message);
}

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

    #region interfaces-factoryoncomplete-async
    // IFactoryOnCompleteAsync: Async post-operation hook
    public async Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        if (_notificationService != null)
            await _notificationService.SendAsync("admin@company.com",
                $"Operation {factoryOperation} completed for {Name}");
    }
    #endregion
}

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
        ct.ThrowIfCancellationRequested();
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        return true;
    }

    #region interfaces-factoryoncancelled
    // IFactoryOnCancelled: Called when operation cancelled via CancellationToken
    public void FactoryCancelled(FactoryOperation factoryOperation)
    {
        CancelledOperation = factoryOperation;
        CleanupPerformed = true;
        // Cleanup logic for cancelled operation
    }
    #endregion
}

public interface IUnitOfWork
{
    Task RollbackAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
}

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
        ct.ThrowIfCancellationRequested();
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        return true;
    }

    #region interfaces-factoryoncancelled-async
    // IFactoryOnCancelledAsync: Async cancellation cleanup
    public async Task FactoryCancelledAsync(FactoryOperation factoryOperation)
    {
        if (_unitOfWork != null)
            await _unitOfWork.RollbackAsync();  // Rollback partial changes
    }
    #endregion
}

#region interfaces-lifecycle-order
// Lifecycle execution order: Start -> Operation -> Complete (or Cancelled)
[Factory]
public partial class EmployeeWithLifecycleOrder : IFactoryOnStart, IFactoryOnComplete, IFactoryOnCancelled
{
    public List<string> LifecycleEvents { get; } = new();

    public void FactoryStart(FactoryOperation factoryOperation)
        => LifecycleEvents.Add($"Start: {factoryOperation}");
    public void FactoryComplete(FactoryOperation factoryOperation)
        => LifecycleEvents.Add($"Complete: {factoryOperation}");
    public void FactoryCancelled(FactoryOperation factoryOperation)
        => LifecycleEvents.Add($"Cancelled: {factoryOperation}");

    // After Fetch: ["Start: Fetch", "Complete: Fetch"]
    // If cancelled: ["Start: Fetch", "Cancelled: Fetch"]

    public Guid EmployeeId { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public EmployeeWithLifecycleOrder() => EmployeeId = Guid.NewGuid();

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        return true;
    }
}
#endregion
