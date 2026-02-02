using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.FactoryModes;

// Server configuration is now in FactoryModeAttributes.cs (modes-server-config snippet)
// This file contains supporting implementation code

/// <summary>
/// Server-specific factory mode helpers (implementation, not for docs).
/// </summary>
public static class ServerModeConfigurationSample
{
    public static void ConfigureServerMode(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
    }
}
