using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Infrastructure.Repositories;
using EmployeeManagement.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.AspNetCore;

#region aspnetcore-service-registration
public static class ServiceRegistrationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // 1. Register RemoteFactory services first
        services.AddNeatooAspNetCore(domainAssembly);

        // 2. Register domain repositories
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddScoped<IDepartmentRepository, InMemoryDepartmentRepository>();

        // 3. Register infrastructure services
        services.AddScoped<IUserContext, DefaultUserContext>();
        services.AddScoped<IEmailService, InMemoryEmailService>();
        services.AddScoped<IAuditLogService, InMemoryAuditLogService>();

        // 4. Auto-register IName/Name pattern as transient
        // Services are available via [Service] parameters in factory methods
        services.RegisterMatchingName(domainAssembly);
    }
}
#endregion

#region aspnetcore-multi-assembly
public static class MultiAssemblySample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Primary domain assembly
        var employeeDomainAssembly = typeof(Employee).Assembly;

        // Additional assemblies containing [Factory] types:
        // var hrDomainAssembly = typeof(HR.Domain.HrEntity).Assembly;
        // var payrollDomainAssembly = typeof(Payroll.Domain.PayrollEntity).Assembly;

        // Register all assemblies with RemoteFactory
        services.AddNeatooAspNetCore(
            employeeDomainAssembly
            // hrDomainAssembly,
            // payrollDomainAssembly
        );

        // Auto-register services from all assemblies
        services.RegisterMatchingName(
            employeeDomainAssembly
            // hrDomainAssembly,
            // payrollDomainAssembly
        );
    }
}
#endregion
