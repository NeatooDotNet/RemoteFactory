using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.Samples.Services;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Infrastructure.Samples;

/// <summary>
/// Service registration using the RegisterMatchingName convention.
/// </summary>
public static class EmployeeServiceRegistration
{
    #region service-injection-matching-name
    // Convention: IName -> Name (Transient lifetime)
    public static void ConfigureServices(IServiceCollection services)
    {
        services.RegisterMatchingName(typeof(IEmployeeRepository).Assembly);
    }
    #endregion
}

/// <summary>
/// Service registration demonstrating different lifetimes.
/// </summary>
public static class EmployeeServiceLifetimes
{
    #region service-injection-lifetimes
    // Standard ASP.NET Core lifetimes work with [Service] injection
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ISalaryCalculator, SalaryCalculator>();  // App lifetime
        services.AddScoped<IAuditContext, AuditContext>();             // Request lifetime
        services.AddTransient<INotificationService, NotificationService>(); // Per-resolution
    }
    #endregion
}

/// <summary>
/// Test service registration for unit and integration tests.
/// </summary>
public static class EmployeeTestServices
{
    #region service-injection-testing
    // Register test doubles for unit/integration tests
    public static void ConfigureTestServices(IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddScoped<IUserContext, TestUserContext>();
    }
    #endregion
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

    public Task<List<EmployeeEntity>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(_employees.Values.ToList());

    public Task<List<EmployeeEntity>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken ct = default) =>
        Task.FromResult(_employees.Values.Where(e => e.DepartmentId == departmentId).ToList());

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

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
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
