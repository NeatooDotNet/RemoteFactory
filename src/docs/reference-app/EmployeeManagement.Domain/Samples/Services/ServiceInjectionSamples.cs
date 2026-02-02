using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Services;

/// <summary>
/// Employee aggregate demonstrating basic [Service] injection.
/// </summary>
[Factory]
public partial class EmployeeBasicService
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Department { get; private set; } = "";

    [Create]
    public EmployeeBasicService()
    {
        Id = Guid.NewGuid();
    }

    #region service-injection-basic
    // [Service] marks parameters for DI injection (not serialized)
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null) return false;
        Id = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        return true;
    }
    #endregion
}

/// <summary>
/// Interface for database access (server-only service).
/// </summary>
public interface IEmployeeDatabase
{
    Task<string> ExecuteQueryAsync(string query);
}

/// <summary>
/// Simple implementation for demonstration.
/// </summary>
public class EmployeeDatabase : IEmployeeDatabase
{
    public Task<string> ExecuteQueryAsync(string query)
    {
        return Task.FromResult($"Query result for: {query}");
    }
}

/// <summary>
/// Employee report demonstrating server-only service injection.
/// </summary>
[Factory]
public partial class ServiceEmployeeReport
{
    public string QueryResult { get; private set; } = "";

    [Create]
    public ServiceEmployeeReport() { }

    #region service-injection-server-only
    // Server-only service - [Remote] ensures execution on server where IEmployeeDatabase exists
    [Remote, Fetch]
    public async Task Fetch(string query, [Service] IEmployeeDatabase database)
    {
        QueryResult = await database.ExecuteQueryAsync(query);
    }
    #endregion
}

/// <summary>
/// Command demonstrating multiple service injection in [Execute] operation.
/// </summary>
[SuppressFactory]
public static partial class DepartmentTransferCommand
{
    #region service-injection-multiple
    // Multiple [Service] parameters - all resolved from DI, none serialized
    [Remote, Execute]
    private static async Task<string> _ProcessTransfer(
        Guid employeeId,                               // Value: serialized
        Guid newDepartmentId,                          // Value: serialized
        [Service] IEmployeeRepository employeeRepo,    // Service: injected
        [Service] IDepartmentRepository departmentRepo,// Service: injected
        [Service] IUserContext userContext)            // Service: injected
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId);
        var department = await departmentRepo.GetByIdAsync(newDepartmentId);
        if (employee == null || department == null) return "Not found";
        return $"Transfer by {userContext.Username}";
    }
    #endregion
}

/// <summary>
/// Audit context interface for tracking operations within a request scope.
/// </summary>
public interface IAuditContext
{
    Guid CorrelationId { get; }
    void LogAction(string action);
}

/// <summary>
/// Scoped audit context that maintains state within a request.
/// </summary>
public class AuditContext : IAuditContext
{
    private readonly List<string> _actions = new();
    public Guid CorrelationId { get; } = Guid.NewGuid();
    public void LogAction(string action) => _actions.Add($"[{DateTime.UtcNow:O}] {action}");
}

/// <summary>
/// Command demonstrating scoped service injection.
/// </summary>
[SuppressFactory]
public static partial class AuditExample
{
    #region service-injection-scoped
    // Scoped services share state within a request
    [Remote, Execute]
    private static Task<Guid> _LogEmployeeAction(string action, [Service] IAuditContext auditContext)
    {
        auditContext.LogAction(action);
        return Task.FromResult(auditContext.CorrelationId);
    }
    #endregion
}

/// <summary>
/// Service for calculating employee salary.
/// </summary>
public interface ISalaryCalculator
{
    decimal Calculate(decimal baseSalary, decimal bonus);
}

/// <summary>
/// Simple salary calculator implementation.
/// </summary>
public class SalaryCalculator : ISalaryCalculator
{
    public decimal Calculate(decimal baseSalary, decimal bonus) => baseSalary + bonus;
}

/// <summary>
/// Employee compensation demonstrating constructor service injection.
/// </summary>
[Factory]
public partial class EmployeeCompensation
{
    private readonly ISalaryCalculator _calculator;
    public decimal TotalCompensation { get; private set; }

    #region service-injection-constructor
    // Constructor injection - service available on BOTH client and server
    [Create]
    public EmployeeCompensation([Service] ISalaryCalculator calculator)
    {
        _calculator = calculator;
    }
    #endregion

    public void CalculateTotal(decimal baseSalary, decimal bonus)
    {
        TotalCompensation = _calculator.Calculate(baseSalary, bonus);
    }
}

/// <summary>
/// Service for client-side notifications.
/// </summary>
public interface INotificationService
{
    void Notify(string message);
}

/// <summary>
/// Simple notification service implementation.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly List<string> _messages = new();
    public void Notify(string message) => _messages.Add(message);
}

/// <summary>
/// Employee notifier with constructor-injected client service.
/// </summary>
[Factory]
public partial class EmployeeNotifier
{
    public bool Notified { get; private set; }

    #region service-injection-client
    // Constructor injection runs locally - available on client
    [Create]
    public EmployeeNotifier([Service] INotificationService notificationService)
    {
        notificationService.Notify("Employee created");
        Notified = true;
    }
    #endregion
}

/// <summary>
/// Result of an employee transfer operation.
/// </summary>
public record EmployeeTransferResult(Guid EmployeeId, string TransferredBy, bool Cancelled);

/// <summary>
/// Command demonstrating mixed parameter types.
/// </summary>
[SuppressFactory]
public static partial class EmployeeTransferCommand
{
    #region service-injection-mixed
    // Mix of value params (serialized), services (injected), and CancellationToken
    [Remote, Execute]
    private static async Task<EmployeeTransferResult> _TransferEmployee(
        Guid employeeId,                           // Value: serialized
        Guid newDepartmentId,                      // Value: serialized
        [Service] IEmployeeRepository repository,  // Service: injected
        [Service] IUserContext userContext,        // Service: injected
        CancellationToken cancellationToken)       // CancellationToken: passed through
    {
        var employee = await repository.GetByIdAsync(employeeId, cancellationToken);
        if (employee == null) return new EmployeeTransferResult(employeeId, userContext.Username, true);
        employee.DepartmentId = newDepartmentId;
        await repository.UpdateAsync(employee, cancellationToken);
        return new EmployeeTransferResult(employeeId, userContext.Username, false);
    }
    #endregion
}

/// <summary>
/// Wrapper for accessing HTTP context information.
/// </summary>
public interface IHttpContextAccessorWrapper
{
    string? GetUserId();
    string? GetCorrelationId();
}

/// <summary>
/// Simple implementation for demonstration.
/// </summary>
public class HttpContextAccessorWrapper : IHttpContextAccessorWrapper
{
    public string? GetUserId() => "user-123";
    public string? GetCorrelationId() => Guid.NewGuid().ToString();
}

/// <summary>
/// Employee context demonstrating HTTP context accessor injection.
/// </summary>
[Factory]
public partial class EmployeeContext
{
    public string? UserId { get; private set; }
    public string? CorrelationId { get; private set; }

    [Create]
    public EmployeeContext() { }

    #region service-injection-httpcontext
    // Access HTTP context via wrapper service (server-only)
    [Remote, Fetch]
    public Task Fetch([Service] IHttpContextAccessorWrapper accessor)
    {
        UserId = accessor.GetUserId();
        CorrelationId = accessor.GetCorrelationId();
        return Task.CompletedTask;
    }
    #endregion
}

/// <summary>
/// Command demonstrating IServiceProvider injection.
/// </summary>
[SuppressFactory]
public static partial class ServiceResolutionExample
{
    #region service-injection-serviceprovider
    // IServiceProvider for dynamic resolution - use sparingly
    [Remote, Execute]
    private static Task<bool> _ResolveEmployeeServices([Service] IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetService(typeof(IEmployeeRepository));
        return Task.FromResult(repository != null);
    }
    #endregion
}
