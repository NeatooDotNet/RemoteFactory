using EmployeeManagement.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.Serialization;

#region serialization-config
/// <summary>
/// Demonstrates configuring RemoteFactory serialization formats during server startup.
/// </summary>
public static class SerializationConfiguration
{
    /// <summary>
    /// Configures RemoteFactory with Ordinal format (default).
    /// Produces compact JSON arrays without property names.
    /// </summary>
    public static void ConfigureOrdinalFormat(IServiceCollection services)
    {
        // Ordinal format: Compact arrays, 40-50% smaller payloads
        // Example: ["Engineering", "john@example.com", "2024-01-15", "John Doe"]
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };
        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);
    }

    /// <summary>
    /// Configures RemoteFactory with Named format.
    /// Produces traditional JSON objects with property names.
    /// </summary>
    public static void ConfigureNamedFormat(IServiceCollection services)
    {
        // Named format: Traditional JSON, easier to debug
        // Example: {"Department":"Engineering","Email":"john@example.com","HireDate":"2024-01-15","Name":"John Doe"}
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };
        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);
    }
}
#endregion

#region serialization-logging
/// <summary>
/// Demonstrates enabling verbose logging for serialization debugging.
/// </summary>
public static class SerializationLoggingConfiguration
{
    public static void ConfigureWithLogging(IServiceCollection services)
    {
        // Enable verbose logging for serialization debugging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace);
        });

        services.AddNeatooAspNetCore(typeof(Employee).Assembly);
    }
}
#endregion

#region serialization-debug-named
/// <summary>
/// Demonstrates switching serialization format based on environment.
/// </summary>
public static class EnvironmentBasedSerializationConfiguration
{
    public static void ConfigureByEnvironment(IServiceCollection services, bool isDevelopment)
    {
        // Use Named format in development for readable JSON debugging
        // Use Ordinal format in production for smaller payloads
        var format = isDevelopment
            ? SerializationFormat.Named
            : SerializationFormat.Ordinal;

        var options = new NeatooSerializationOptions { Format = format };
        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);
    }
}
// Development (Named):   {"Department":"Engineering","Email":"john@example.com","Name":"John Doe"}
// Production (Ordinal):  ["Engineering","john@example.com","John Doe"]
#endregion
