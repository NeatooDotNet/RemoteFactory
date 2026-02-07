using EmployeeManagement.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.AspNetCore;

#region aspnetcore-service-registration
public static class ServiceRegistrationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        var assembly = typeof(Employee).Assembly;

        services.AddNeatooAspNetCore(assembly);           // 1. RemoteFactory
        services.RegisterMatchingName(assembly);          // 2. IName -> Name pairs
        // services.AddScoped<ICustom, CustomImpl>();     // 3. Manual registrations
    }
}
#endregion

#region aspnetcore-multi-assembly
public static class MultiAssemblySample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register factories from multiple domain assemblies
        services.AddNeatooAspNetCore(
            typeof(Employee).Assembly
            // , typeof(OtherDomain.Entity).Assembly
        );
    }
}
#endregion
