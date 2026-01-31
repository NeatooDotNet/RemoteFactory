using EmployeeManagement.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.AspNetCore;

#region aspnetcore-logging
public static class LoggingConfigurationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Configure logging with Neatoo categories
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);

            // Neatoo log categories:
            // - Neatoo.RemoteFactory.Server: Remote delegate execution
            // - Neatoo.RemoteFactory.Client: HTTP client requests
            // - Neatoo.RemoteFactory.Serialization: JSON serialization/deserialization
            builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Debug);
        });

        // Add RemoteFactory after logging is configured
        services.AddNeatooAspNetCore(typeof(Employee).Assembly);
    }
}
#endregion
