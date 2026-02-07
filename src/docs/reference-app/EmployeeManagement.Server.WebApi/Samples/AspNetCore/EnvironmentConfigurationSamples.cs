using EmployeeManagement.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.AspNetCore;

#region aspnetcore-development
public static class DevelopmentConfigurationSample
{
    public static void ConfigureServices(IServiceCollection services, bool isDevelopment)
    {
        // Named format for debugging, Ordinal for production
        var format = isDevelopment ? SerializationFormat.Named : SerializationFormat.Ordinal;
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = format },
            typeof(Employee).Assembly);
    }
}
#endregion

#region aspnetcore-production
public static class ProductionConfigurationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Ordinal format: 40-50% smaller payloads (default)
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            typeof(Employee).Assembly);
    }
}
#endregion
