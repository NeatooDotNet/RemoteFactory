using EmployeeManagement.Domain.Samples.GettingStarted;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples;

#region getting-started-serialization-config
public static class SerializationConfig
{
    public static void ConfigureOrdinal(IServiceCollection services)
    {
        // Ordinal format (default): Compact array format, 40-50% smaller
        // Example payload: ["Jane", "Smith", "jane@example.com"]
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };
        services.AddNeatooAspNetCore(options, typeof(EmployeeModel).Assembly);
    }

    public static void ConfigureNamed(IServiceCollection services)
    {
        // Named format: Verbose with property names, easier to debug
        // Example payload: {"FirstName":"Jane","LastName":"Smith","Email":"jane@example.com"}
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };
        services.AddNeatooAspNetCore(options, typeof(EmployeeModel).Assembly);
    }
}
#endregion
