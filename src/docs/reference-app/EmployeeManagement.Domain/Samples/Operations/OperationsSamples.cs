using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Operations;

#region operations-create-constructor
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
#endregion

#region operations-create-static
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
#endregion

#region operations-create-return-types
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
#endregion

#region operations-fetch-instance
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
#endregion

#region operations-fetch-bool-return
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
#endregion

#region operations-insert
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
#endregion

#region operations-update
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
#endregion

#region operations-delete
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
#endregion

#region operations-insert-update
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
#endregion

#region operations-execute
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
#endregion

#region operations-execute-command
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
#endregion

#region operations-event
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
#endregion

#region operations-event-tracker
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
#endregion

#region operations-remote
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
#endregion

#region operations-cancellation
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
#endregion

#region operations-params-value
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
#endregion

#region operations-params-service
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
#endregion

#region operations-params-array
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
#endregion

#region operations-params-cancellation
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
#endregion
