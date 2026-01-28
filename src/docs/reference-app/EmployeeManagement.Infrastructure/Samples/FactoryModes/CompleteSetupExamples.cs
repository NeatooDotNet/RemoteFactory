using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Infrastructure.Samples.FactoryModes;

#region modes-remoteonly-example
/// <summary>
/// Client-side state service (client-only dependency).
/// </summary>
public interface IClientStateService
{
    Guid CurrentUserId { get; }
    void SetCurrentEmployeeId(Guid employeeId);
}

/// <summary>
/// Default implementation of client state service.
/// </summary>
public class ClientStateService : IClientStateService
{
    public Guid CurrentUserId { get; } = Guid.NewGuid();
    private Guid _currentEmployeeId;

    public void SetCurrentEmployeeId(Guid employeeId)
    {
        _currentEmployeeId = employeeId;
    }
}

/// <summary>
/// Client setup with RemoteOnly mode and Remote runtime.
/// </summary>
public static class RemoteOnlyModeClientSetup
{
    public static void Configure(IServiceCollection services, string serverUrl)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // Remote mode - all operations serialize and POST to server
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Register factory types
        services.RegisterMatchingName(domainAssembly);

        // Configure HttpClient with server address and timeout
        services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
        {
            return new HttpClient
            {
                BaseAddress = new Uri(serverUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        });

        // Register client-only services
        services.AddSingleton<IClientStateService, ClientStateService>();
    }
}
#endregion

#region modes-logical-example
/// <summary>
/// Logical mode setup for single-tier applications.
/// </summary>
public static class LogicalModeSetup
{
    public static void Configure(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // Logical mode - direct local execution, no HTTP
        services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Register factory types
        services.RegisterMatchingName(domainAssembly);

        // Register repositories locally
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddScoped<IDepartmentRepository, InMemoryDepartmentRepository>();
    }
}

/// <summary>
/// Demonstrates single-tier application using Logical mode.
/// </summary>
public static class SingleTierAppExample
{
    public static async Task RunLocally()
    {
        // Build the service container
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add IHostApplicationLifetime (required for some features)
        services.AddSingleton<IHostApplicationLifetime, SingleTierHostLifetime>();

        // Configure Logical mode
        LogicalModeSetup.Configure(services);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Resolve the factory
        var factory = scope.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create a new employee
        var employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";

        // Executes directly - no HTTP, no serialization
        await factory.Save(employee);

        // Fetch the employee back
        var fetched = await factory.Fetch(employee.Id);

        // Verify the data persisted
        System.Diagnostics.Debug.Assert(fetched != null);
        System.Diagnostics.Debug.Assert(fetched.FirstName == "John");
    }

    private class SingleTierHostLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() { }
    }
}
#endregion
