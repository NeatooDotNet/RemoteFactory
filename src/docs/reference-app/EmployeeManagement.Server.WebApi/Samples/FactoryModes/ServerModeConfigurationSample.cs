using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.FactoryModes;

/// <summary>
/// Server-specific factory mode configuration samples.
/// </summary>
public static class ServerModeConfigurationSample
{
    #region modes-server-config
    /// <summary>
    /// Configures Server runtime mode with ASP.NET Core integration.
    /// AddNeatooAspNetCore internally uses NeatooFactory.Server.
    /// </summary>
    public static void ConfigureServerMode(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // AddNeatooAspNetCore handles incoming HTTP requests and executes locally
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Register server-side services
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
    }
    #endregion
}
