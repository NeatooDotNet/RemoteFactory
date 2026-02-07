using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

// Sample code: suppressing logging performance warnings for readability
#pragma warning disable CA1848 // Use LoggerMessage delegates for performance
#pragma warning disable CA1873 // Evaluation of argument may be expensive and unnecessary if logging is disabled

namespace EmployeeManagement.Server.WebApi.Samples;

/// <summary>
/// ASP.NET Core authorization policy configuration.
/// </summary>
public static class AuthorizationPolicyConfigSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Policy requiring authentication
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());

            // Policy requiring HR role
            options.AddPolicy("RequireHR", policy =>
                policy.RequireRole("HR"));

            // Policy requiring Manager or HR role
            options.AddPolicy("RequireManagerOrHR", policy =>
                policy.RequireRole("Manager", "HR"));

            // Custom policy with claim requirements
            options.AddPolicy("RequireEmployeeAccess", policy =>
                policy.RequireClaim("department")
                      .RequireAuthenticatedUser());
        });
    }
}

/// <summary>
/// Applying ASP.NET Core policies with [AspAuthorize].
/// </summary>
[Factory]
public partial class PolicyProtectedEmployee2 : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public PolicyProtectedEmployee2() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [AspAuthorize] with named policy via constructor.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repo,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    [AspAuthorize("RequireManagerOrHR")]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToUpperInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}

/// <summary>
/// Role-based authorization with [AspAuthorize].
/// </summary>
[Factory]
public partial class RoleProtectedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public RoleProtectedEmployee() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Multiple roles - any of the listed roles can access.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize(Roles = "Employee,Manager,HR")]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repo,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Restricted to HR and Manager roles only.
    /// </summary>
    [Remote, Insert]
    [AspAuthorize(Roles = "HR,Manager")]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToUpperInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}

/// <summary>
/// Multiple [AspAuthorize] attributes - ALL must pass.
/// </summary>
[Factory]
public partial class MultiPolicyEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public MultiPolicyEmployee() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Multiple [AspAuthorize] - user must satisfy ALL requirements.
    /// </summary>
    [Remote, Delete]
    [AspAuthorize("RequireAuthenticated")]
    [AspAuthorize(Roles = "HR")]
    public async Task Delete(
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(auditLog);
        await auditLog.LogAsync("Delete", Id, "Employee", "Deleted", ct);
    }
}

// Save with extensions - snippet in SaveExtensionsSamples.cs
[Factory]
public partial class EmployeeWithExtensions : IFactorySaveMeta, IFactoryOnStart
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithExtensions() { Id = Guid.NewGuid(); }

    public void FactoryStart(FactoryOperation factoryOperation)
    {
        if (factoryOperation == FactoryOperation.Insert ||
            factoryOperation == FactoryOperation.Update)
        {
            if (string.IsNullOrWhiteSpace(FirstName))
                throw new System.ComponentModel.DataAnnotations.ValidationException("FirstName is required");
            if (string.IsNullOrWhiteSpace(LastName))
                throw new System.ComponentModel.DataAnnotations.ValidationException("LastName is required");
        }
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
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
        ArgumentNullException.ThrowIfNull(repo);
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToUpperInvariant()}@example.com",
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
        ArgumentNullException.ThrowIfNull(repo);
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToUpperInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}

// Save usage sample - snippet in SaveUsageSamples.cs
public static class SaveUsageSample
{
    public static async Task SaveWorkflow(IEmployeeSaveStateSampleFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        EmployeeSaveStateSample employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";

        var saved = (EmployeeSaveStateSample?)await factory.Save(employee);
        saved!.FirstName = "Jane";
        saved = (EmployeeSaveStateSample?)await factory.Save(saved);
        saved!.IsDeleted = true;
        await factory.Save(saved);
    }
}

public interface IEmployeeSaveStateSampleFactory : IFactorySave<EmployeeSaveStateSample>
{
    EmployeeSaveStateSample Create();
}

[Factory]
public partial class EmployeeSaveStateSample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeSaveStateSample() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert(CancellationToken ct) { IsNew = false; return Task.CompletedTask; }

    [Remote, Update]
    public Task Update(CancellationToken ct) { return Task.CompletedTask; }

    [Remote, Delete]
    public Task Delete(CancellationToken ct) { return Task.CompletedTask; }
}

// Event samples moved to EmployeeEventHandlers.cs and EventsSamples.cs - no duplicate regions
[Factory]
public partial class AspNetCoreEventHandlers
{
    [Event]
    public async Task OnEmployeeCreated(Guid employeeId, string employeeName,
        [Service] ICorrelationContext correlationContext, [Service] IEmailService emailService,
        [Service] Microsoft.Extensions.Logging.ILogger<AspNetCoreEventHandlers> logger, CancellationToken ct)
    {
        logger.LogInformation("Employee created: {EmployeeId}, Correlation: {CorrelationId}", employeeId, correlationContext.CorrelationId);
        await emailService.SendAsync("hr@company.com", $"New: {employeeName}", $"ID: {employeeId}", ct);
    }
}

[Factory]
public partial class CorrelatedEventHandlers
{
    [Event]
    public async Task LogWithCorrelation(Guid entityId, string action,
        [Service] ICorrelationContext ctx, [Service] IAuditLogService auditLog,
        [Service] Microsoft.Extensions.Logging.ILogger<CorrelatedEventHandlers> logger, CancellationToken ct)
    {
        logger.LogInformation("Event with correlation {CorrelationId}", ctx.CorrelationId);
        await auditLog.LogAsync(action, entityId, "Event", $"Correlation: {ctx.CorrelationId}", ct);
    }
}
