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
        // Choose format based on environment
        var format = isDevelopment
            ? SerializationFormat.Named   // Readable JSON for debugging
            : SerializationFormat.Ordinal; // Compact arrays for production

        var options = new NeatooSerializationOptions { Format = format };
        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);

        // Enable verbose logging in development
        if (isDevelopment)
        {
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace);
            });
        }
    }
}
#endregion

#region aspnetcore-production
public static class ProductionConfigurationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Ordinal format for minimal payload size (default)
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };

        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);

        // Production logging - less verbose
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);
            builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Information);
        });
    }
}
#endregion
