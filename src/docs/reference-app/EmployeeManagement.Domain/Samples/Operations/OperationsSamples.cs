using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Operations;

/// <summary>
/// Employee with constructor-based Create operation.
/// </summary>
[Factory]
public partial class EmployeeWithConstructorCreate
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    #region operations-create-constructor
    // Constructor marked with [Create] - factory calls this to create instances
    [Create]
    public EmployeeWithConstructorCreate() => Id = Guid.NewGuid();
    #endregion
}

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

    #region operations-create-static
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
    #endregion
}

/// <summary>
/// Demonstrates Create method return type patterns.
/// </summary>
[Factory]
public partial class EmployeeCreateReturnTypes
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public bool IsValid { get; private set; }

    #region operations-create-return-types
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
    #endregion
}

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
    public EmployeeFetchSample() => Id = Guid.NewGuid();

    #region operations-fetch-instance
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
    #endregion
}

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
    public EmployeeFetchOptional() => Id = Guid.NewGuid();

    #region operations-fetch-bool-return
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
    #endregion
}

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
    public EmployeeInsertSample() => Id = Guid.NewGuid();

    #region operations-insert
    // [Insert] persists new entities to storage
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = new EmployeeEntity { Id = Id, FirstName = FirstName, LastName = LastName, /* ... */ };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
    #endregion
}

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
    public EmployeeUpdateSample() => Id = Guid.NewGuid();

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

    #region operations-update
    // [Update] persists changes to existing entities
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = new EmployeeEntity { Id = Id, FirstName = FirstName, LastName = LastName, /* ... */ };
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }
    #endregion
}

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
    public EmployeeDeleteSample() => Id = Guid.NewGuid();

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

    #region operations-delete
    // [Delete] removes entities from storage
    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
    #endregion
}

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
    public SettingItem(string key) => Key = key;

    #region operations-insert-update
    // Multiple attributes on one method - same handler for insert and update (upsert)
    [Remote, Insert, Update]
    public async Task Upsert([Service] ISettingsRepository repository, CancellationToken ct)
    {
        await repository.UpsertAsync(Key, Value, ct);
        IsNew = false;
    }
    #endregion
}

/// <summary>
/// Repository for settings (used by SettingItem sample).
/// </summary>
public interface ISettingsRepository
{
    Task UpsertAsync(string key, string value, CancellationToken ct);
}

/// <summary>
/// Demonstrates Execute operation for business commands.
/// </summary>
[Factory]
public static partial class EmployeePromotionOperation
{
    #region operations-execute
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
    #endregion
}

public record PromotionResult(bool Success, string Message);

/// <summary>
/// Demonstrates Event operations for fire-and-forget processing.
/// </summary>
[Factory]
public partial class EmployeeNotificationEvents
{
    #region operations-event
    // [Event] for fire-and-forget - CancellationToken required, receives ApplicationStopping
    [Event]
    public async Task NotifyHROfNewEmployee(
        Guid employeeId, string employeeName,
        [Service] IEmailService emailService, CancellationToken ct)
    {
        await emailService.SendAsync("hr@company.com", $"New Employee: {employeeName}", $"ID: {employeeId}", ct);
    }
    #endregion
}

/// <summary>
/// Demonstrates IEventTracker usage for monitoring events.
/// </summary>
[Factory]
public static partial class EventTrackerSample
{
    #region operations-event-tracker
    // IEventTracker for waiting on pending events (useful in tests and shutdown)
    [Execute]
    private static async Task<int> _WaitForAllEvents([Service] IEventTracker eventTracker, CancellationToken ct)
    {
        var pendingCount = eventTracker.PendingCount;
        await eventTracker.WaitAllAsync(ct);
        return pendingCount;
    }
    #endregion
}

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

    #region operations-remote
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
    #endregion

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = new EmployeeEntity { Id = Id, FirstName = FirstName, LastName = "", /* ... */ };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}

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
    public EmployeeWithCancellation() => Id = Guid.NewGuid();

    #region operations-cancellation
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
    #endregion
}

/// <summary>
/// Demonstrates value parameters that are serialized.
/// </summary>
[Factory]
public partial class EmployeeSearchSample
{
    public List<string> Results { get; private set; } = [];

    #region operations-params-value
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
    #endregion
}

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
    public EmployeeWithServiceParams() => Id = Guid.NewGuid();

    #region operations-params-service
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
    #endregion
}

/// <summary>
/// Demonstrates params array parameters.
/// </summary>
[Factory]
public partial class BatchEmployeeFetch
{
    public List<string> EmployeeNames { get; private set; } = [];

    #region operations-params-array
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
    #endregion
}

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
    public EmployeeCompleteParams() => Id = Guid.NewGuid();

    #region operations-params-cancellation
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
    #endregion
}
