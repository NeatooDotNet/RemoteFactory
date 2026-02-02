using EmployeeManagement.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.AspNetCore;

#region aspnetcore-logging
public static class LoggingConfigurationSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            // Neatoo categories: Server, Client, Serialization
            builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Debug);
        });

        services.AddNeatooAspNetCore(typeof(Employee).Assembly);
    }
}
#endregion
