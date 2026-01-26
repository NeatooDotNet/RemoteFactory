using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Operations;

#region operations-lifecycle-onstart
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
#endregion

#region operations-lifecycle-oncomplete
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
#endregion

#region operations-lifecycle-oncancelled
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
#endregion
