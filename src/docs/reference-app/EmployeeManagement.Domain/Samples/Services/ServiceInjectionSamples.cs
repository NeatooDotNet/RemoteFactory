using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Services;

#region service-injection-basic
/// <summary>
/// Demonstrates basic [Service] parameter injection.
/// </summary>
[Factory]
public partial class EmployeeBasicService : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeBasicService() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [Service] marks parameters for DI injection.
    /// employeeId is serialized; repository is resolved from server DI.
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
        IsNew = false;
        return true;
    }
}
#endregion

#region service-injection-multiple
/// <summary>
/// Demonstrates multiple service parameter injection.
/// </summary>
[Factory]
public partial class EmployeeMultipleServices : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeMultipleServices() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Multiple services injected for complex operations.
    /// All service parameters are resolved from server DI.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        [Service] IEmailService emailService,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;

        // Multiple services working together
        await emailService.SendAsync(
            entity.Email,
            "Welcome!",
            $"Welcome {FirstName}!",
            ct);

        await auditLog.LogAsync("Insert", Id, "Employee", $"Created {FirstName}", ct);
    }
}
#endregion

#region service-injection-scoped
/// <summary>
/// Demonstrates scoped service lifetime with factory operations.
/// </summary>
[Factory]
public partial class EmployeeScopedService : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeScopedService() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Scoped services are disposed when the request completes.
    /// Each remote call gets a fresh scope.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,
        [Service] IAuditLogService auditLog, // Scoped - disposed after request
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;

        // Scoped service records audit in same transaction scope
        await auditLog.LogAsync("Fetch", employeeId, "Employee", "Loaded", ct);

        return true;
    }
}
#endregion

#region service-injection-constructor
/// <summary>
/// Demonstrates service injection in [Create] constructor.
/// </summary>
[Factory]
public partial class EmployeeWithDefaults
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public Guid DefaultDepartmentId { get; private set; }
    public DateTime HireDate { get; private set; }

    // Internal constructor allows generated serializer access
    internal EmployeeWithDefaults() { }

    /// <summary>
    /// Services injected during object creation.
    /// </summary>
    [Create]
    public static EmployeeWithDefaults Create(
        [Service] IDefaultValueProvider defaults)
    {
        return new EmployeeWithDefaults
        {
            Id = Guid.NewGuid(),
            DefaultDepartmentId = defaults.GetDefaultDepartmentId(),
            HireDate = defaults.GetDefaultHireDate()
        };
    }
}

/// <summary>
/// Provides default values for new entities.
/// </summary>
public interface IDefaultValueProvider
{
    Guid GetDefaultDepartmentId();
    DateTime GetDefaultHireDate();
}
#endregion

#region service-injection-server-only
/// <summary>
/// Demonstrates server-only services with [Remote] attribute.
/// </summary>
[Factory]
public partial class EmployeeServerOnly : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeServerOnly() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [Remote] ensures server execution where repository exists.
    /// Without [Remote], clients would fail resolving IEmployeeRepository.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository, // Server-only service
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
#endregion

#region service-injection-client
/// <summary>
/// Demonstrates client-side service injection for local operations.
/// </summary>
[Factory]
public partial class EmployeeClientService
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string ClientInfo { get; private set; } = "";

    [Create]
    public EmployeeClientService() { Id = Guid.NewGuid(); }

    /// <summary>
    /// No [Remote] - runs locally on client.
    /// Uses platform-agnostic client service (ILogger).
    /// </summary>
    [Fetch]
    public void LoadFromCache(
        string cachedData,
        [Service] ILogger<EmployeeClientService> logger)
    {
        // Local operation using client-side logger
        logger.LogInformation("Loading employee from cache: {Data}", cachedData);
        FirstName = cachedData;
        ClientInfo = $"Loaded at {DateTime.UtcNow}";
    }
}
#endregion

#region service-injection-mixed
/// <summary>
/// Mixing local and remote methods with different services.
/// </summary>
[Factory]
public partial class EmployeeMixedServices : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastModified { get; private set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeMixedServices() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Local method - uses client-side logger (no [Remote]).
    /// </summary>
    [Fetch]
    public void LoadDefaults([Service] ILogger<EmployeeMixedServices> logger)
    {
        logger.LogInformation("Initializing with defaults");
        FirstName = "New Employee";
        LastModified = DateTime.UtcNow.ToString("o");
    }

    /// <summary>
    /// Remote method - uses server-side repository ([Remote]).
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

    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}
#endregion

// Note: IHttpContextAccessor injection is demonstrated in the Server.WebApi project
// where Microsoft.AspNetCore.Http is available.
// See EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs for examples.

#region service-injection-serviceprovider
/// <summary>
/// Demonstrates IServiceProvider injection for dynamic resolution.
/// </summary>
[Factory]
public partial class EmployeeServiceProvider : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeServiceProvider() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Use IServiceProvider sparingly - prefer typed services.
    /// Useful for conditional or plugin-based service resolution.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        // Prefer typed [Service] parameters when possible
        // Use IServiceProvider only for dynamic scenarios
        var repository = serviceProvider.GetService(typeof(IEmployeeRepository)) as IEmployeeRepository;
        if (repository == null)
            throw new InvalidOperationException("IEmployeeRepository not registered");

        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}
#endregion
