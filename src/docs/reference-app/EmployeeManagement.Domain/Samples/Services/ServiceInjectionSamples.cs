using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Services;

#region service-injection-basic
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

    /// <summary>
    /// Fetches employee data using an injected repository.
    /// IEmployeeRepository is injected from DI, not serialized.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null) return false;

        Id = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        Department = entity.Position;
        return true;
    }
}
#endregion

#region service-injection-server-only
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
        // Simulated query execution
        return Task.FromResult($"Query result for: {query}");
    }
}

/// <summary>
/// Employee report demonstrating server-only service injection.
/// </summary>
[Factory]
public partial class EmployeeReport
{
    public string QueryResult { get; private set; } = "";

    [Create]
    public EmployeeReport()
    {
    }

    /// <summary>
    /// Fetches report data from the database.
    /// </summary>
    /// <remarks>
    /// This service only exists on the server - [Remote] ensures the method runs there.
    /// </remarks>
    [Remote, Fetch]
    public async Task Fetch(string query, [Service] IEmployeeDatabase database)
    {
        QueryResult = await database.ExecuteQueryAsync(query);
    }
}
#endregion

#region service-injection-multiple
/// <summary>
/// Command demonstrating multiple service injection in [Execute] operation.
/// </summary>
[SuppressFactory]
public static partial class DepartmentTransferCommand
{
    /// <summary>
    /// Processes a department transfer with multiple injected services.
    /// </summary>
    [Remote, Execute]
    private static async Task<string> _ProcessTransfer(
        Guid employeeId,
        Guid newDepartmentId,
        [Service] IEmployeeRepository employeeRepo,
        [Service] IDepartmentRepository departmentRepo,
        [Service] IUserContext userContext)
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId);
        var department = await departmentRepo.GetByIdAsync(newDepartmentId);

        if (employee == null || department == null)
            return "Transfer failed: Employee or department not found";

        var employeeName = $"{employee.FirstName} {employee.LastName}";
        return $"Transfer of {employeeName} to {department.Name} by {userContext.Username}";
    }
}
#endregion

#region service-injection-scoped
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

    public void LogAction(string action)
    {
        _actions.Add($"[{DateTime.UtcNow:O}] {action}");
    }
}

/// <summary>
/// Command demonstrating scoped service injection.
/// </summary>
[SuppressFactory]
public static partial class AuditExample
{
    /// <summary>
    /// Logs an action and returns the correlation ID.
    /// </summary>
    /// <remarks>
    /// Scoped services maintain state within a request - all operations
    /// in the same request share the same CorrelationId.
    /// </remarks>
    [Remote, Execute]
    private static Task<Guid> _LogEmployeeAction(string action, [Service] IAuditContext auditContext)
    {
        auditContext.LogAction(action);
        return Task.FromResult(auditContext.CorrelationId);
    }
}
#endregion

#region service-injection-constructor
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
    public decimal Calculate(decimal baseSalary, decimal bonus)
    {
        return baseSalary + bonus;
    }
}

/// <summary>
/// Employee compensation demonstrating constructor service injection.
/// </summary>
[Factory]
public partial class EmployeeCompensation
{
    private readonly ISalaryCalculator _calculator;

    public decimal TotalCompensation { get; private set; }

    /// <summary>
    /// Constructor with service injection.
    /// ISalaryCalculator is resolved from DI when the factory creates the instance.
    /// </summary>
    [Create]
    public EmployeeCompensation([Service] ISalaryCalculator calculator)
    {
        _calculator = calculator;
    }

    public void CalculateTotal(decimal baseSalary, decimal bonus)
    {
        TotalCompensation = _calculator.Calculate(baseSalary, bonus);
    }
}
#endregion

#region service-injection-client
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

    public void Notify(string message)
    {
        _messages.Add(message);
    }
}

/// <summary>
/// Employee notifier with constructor-injected client service.
/// </summary>
[Factory]
public partial class EmployeeNotifier
{
    public bool Notified { get; private set; }

    /// <summary>
    /// Constructor with client-side service injection.
    /// </summary>
    /// <remarks>
    /// This service is available on both client and server.
    /// </remarks>
    [Create]
    public EmployeeNotifier([Service] INotificationService notificationService)
    {
        notificationService.Notify("Employee created");
        Notified = true;
    }
}
#endregion

#region service-injection-mixed
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
    /// <summary>
    /// Transfers an employee to a new department.
    /// </summary>
    [Remote, Execute]
    private static async Task<EmployeeTransferResult> _TransferEmployee(
        Guid employeeId,         // Value: serialized
        Guid newDepartmentId,    // Value: serialized
        [Service] IEmployeeRepository repository,  // Service: injected
        [Service] IUserContext userContext,        // Service: injected
        CancellationToken cancellationToken)       // CancellationToken: passed through
    {
        cancellationToken.ThrowIfCancellationRequested();

        var employee = await repository.GetByIdAsync(employeeId, cancellationToken);
        if (employee == null)
            return new EmployeeTransferResult(employeeId, userContext.Username, true);

        employee.DepartmentId = newDepartmentId;
        await repository.UpdateAsync(employee, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new EmployeeTransferResult(employeeId, userContext.Username, false);
    }
}
#endregion

#region service-injection-httpcontext
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
    public EmployeeContext()
    {
    }

    /// <summary>
    /// Fetches context information from the HTTP request.
    /// </summary>
    /// <remarks>
    /// Access HttpContext on server to get user info, headers, etc.
    /// </remarks>
    [Remote, Fetch]
    public Task Fetch([Service] IHttpContextAccessorWrapper accessor)
    {
        UserId = accessor.GetUserId();
        CorrelationId = accessor.GetCorrelationId();
        return Task.CompletedTask;
    }
}
#endregion

#region service-injection-serviceprovider
/// <summary>
/// Command demonstrating IServiceProvider injection.
/// </summary>
[SuppressFactory]
public static partial class ServiceResolutionExample
{
    /// <summary>
    /// Dynamically resolves services from the provider.
    /// </summary>
    /// <remarks>
    /// Dynamically resolve services when needed - use sparingly.
    /// </remarks>
    [Remote, Execute]
    private static Task<bool> _ResolveEmployeeServices([Service] IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetService(typeof(IEmployeeRepository));
        var userContext = serviceProvider.GetService(typeof(IUserContext));

        return Task.FromResult(repository != null && userContext != null);
    }
}
#endregion
