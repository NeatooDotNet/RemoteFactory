using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Infrastructure.Repositories;
using EmployeeManagement.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the service collection.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddScoped<IDepartmentRepository, InMemoryDepartmentRepository>();

        // Services
        services.AddScoped<IEmailService, InMemoryEmailService>();
        services.AddScoped<IAuditLogService, InMemoryAuditLogService>();
        services.AddScoped<IUserContext, DefaultUserContext>();

        return services;
    }
}
