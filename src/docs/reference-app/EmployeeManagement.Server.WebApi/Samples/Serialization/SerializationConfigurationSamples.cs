using EmployeeManagement.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.Serialization;

public static class SerializationConfiguration
{
    #region serialization-config
    // Configure serialization format: Ordinal (compact) or Named (readable).
    public static void Configure(IServiceCollection services)
    {
        var options = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };
        services.AddNeatooAspNetCore(options, typeof(Employee).Assembly);
    }
    #endregion
}

public static class SerializationLoggingConfiguration
{
    #region serialization-logging
    // Enable verbose logging for serialization debugging.
    public static void ConfigureWithLogging(IServiceCollection services)
    {
        services.AddLogging(b => b.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace));
        services.AddNeatooAspNetCore(typeof(Employee).Assembly);
    }
    #endregion
}

public static class EnvironmentBasedSerializationConfiguration
{
    #region serialization-debug-named
    // Switch to Named format in development for readable JSON debugging.
    public static void ConfigureByEnvironment(IServiceCollection services, bool isDevelopment)
    {
        var format = isDevelopment ? SerializationFormat.Named : SerializationFormat.Ordinal;
        services.AddNeatooAspNetCore(new NeatooSerializationOptions { Format = format }, typeof(Employee).Assembly);
    }
    #endregion
}
