using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Infrastructure.Samples.FactoryModes;

/// <summary>
/// Configuration samples demonstrating different factory modes.
/// </summary>
public static class FactoryModeConfigurationSamples
{
    #region modes-full-config
    /// <summary>
    /// Configures services for Full mode (server-side).
    /// Full mode is the default - no [assembly: FactoryMode] attribute needed.
    /// </summary>
    public static void ConfigureFullMode(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // Full mode is the default (no assembly attribute required)
        // Use NeatooFactory.Server for ASP.NET Core server applications
        services.AddNeatooRemoteFactory(
            NeatooFactory.Server,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);
    }
    #endregion

    #region modes-remoteonly-config
    // In AssemblyAttributes.cs or GlobalUsings.cs:
    // [assembly: FactoryMode(FactoryMode.RemoteOnly)]

    /// <summary>
    /// Configures services for RemoteOnly mode (client-side).
    /// RemoteOnly generates HTTP stubs only - smaller assemblies for clients.
    /// </summary>
    public static void ConfigureRemoteOnlyMode(IServiceCollection services, string serverUrl)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // RemoteOnly mode - all methods make HTTP calls to server
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Register HttpClient with the key RemoteFactory expects
        services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
        {
            return new HttpClient { BaseAddress = new Uri(serverUrl) };
        });
    }
    #endregion

    #region modes-remote-config
    /// <summary>
    /// Configures Remote runtime mode for client applications.
    /// All factory operations go via HTTP to server.
    /// </summary>
    public static void ConfigureRemoteMode(IServiceCollection services, string serverUrl)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // Remote mode - all factory operations serialize and POST to /api/neatoo
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Configure HttpClient with server base address
        services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
        {
            return new HttpClient { BaseAddress = new Uri(serverUrl) };
        });
    }
    #endregion

    #region modes-logical-config
    /// <summary>
    /// Configures Logical runtime mode for single-tier applications or tests.
    /// Direct execution, no serialization, no HTTP overhead.
    /// </summary>
    public static void ConfigureLogicalMode(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // Logical mode - executes all methods locally, no HTTP
        services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);
    }
    #endregion

    #region modes-logging
    /// <summary>
    /// Configures factory with verbose logging for debugging.
    /// </summary>
    public static void ConfigureWithLogging(IServiceCollection services, NeatooFactory mode)
    {
        // Configure detailed logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace);
        });
        // Logs show:
        // - "Executing local factory method..." for Server/Logical modes
        // - "Sending remote factory request..." for Remote mode
        // - Serialization format and payload size

        var domainAssembly = typeof(Employee).Assembly;

        services.AddNeatooRemoteFactory(
            mode,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);
    }
    #endregion
}
