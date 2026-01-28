using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.Samples.Services;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Infrastructure.Samples;

#region service-injection-matching-name
/// <summary>
/// Service registration using the RegisterMatchingName convention.
/// </summary>
public static class EmployeeServiceRegistration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // RegisterMatchingName registers interfaces to their implementations
        // using the naming convention: IEmployeeRepository -> EmployeeRepository
        // All matches are registered with Transient lifetime
        services.RegisterMatchingName(typeof(IEmployeeRepository).Assembly);
    }
}
#endregion

#region service-injection-lifetimes
/// <summary>
/// Service registration demonstrating different lifetimes.
/// </summary>
/// <remarks>
/// - Singleton: same instance across all requests, use for caches/configuration
/// - Scoped: same instance within a request, use for DbContext/unit of work
/// - Transient: new instance each resolution
/// </remarks>
public static class EmployeeServiceLifetimes
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Singleton: same instance for entire application lifetime
        services.AddSingleton<ISalaryCalculator, SalaryCalculator>();

        // Scoped: same instance within a single request
        services.AddScoped<IAuditContext, AuditContext>();

        // Transient: new instance each time requested
        services.AddTransient<INotificationService, NotificationService>();
    }
}
#endregion

#region service-injection-testing
/// <summary>
/// Test service registration for unit and integration tests.
/// </summary>
public static class EmployeeTestServices
{
    /// <summary>
    /// Register test doubles instead of production services.
    /// </summary>
    public static void ConfigureTestServices(IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddScoped<IUserContext, TestUserContext>();
    }
}

/// <summary>
/// In-memory repository for testing.
/// </summary>
public class InMemoryEmployeeRepository : IEmployeeRepository
{
    private readonly Dictionary<Guid, EmployeeEntity> _employees = new();

    public Task<EmployeeEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _employees.TryGetValue(id, out var employee);
        return Task.FromResult(employee);
    }

    public Task<List<EmployeeEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_employees.Values.ToList());
    }

    public Task<List<EmployeeEntity>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken ct = default)
    {
        var employees = _employees.Values.Where(e => e.DepartmentId == departmentId).ToList();
        return Task.FromResult(employees);
    }

    public Task AddAsync(EmployeeEntity entity, CancellationToken ct = default)
    {
        _employees[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EmployeeEntity entity, CancellationToken ct = default)
    {
        _employees[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _employees.Remove(id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test user context with configurable properties.
/// </summary>
public class TestUserContext : IUserContext
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "TestUser";
    public IReadOnlyList<string> Roles { get; set; } = new[] { "User" };
    public bool IsAuthenticated { get; set; } = true;

    public bool IsInRole(string role) => Roles.Contains(role);
}
#endregion
