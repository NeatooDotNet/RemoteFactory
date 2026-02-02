using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Infrastructure.Samples.FactoryModes;

// Complete setup examples are now consolidated as minimal snippets in FactoryModesSamples.cs
// This file contains supporting implementation code

/// <summary>
/// Client-side state service (supporting code, not for docs).
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
    public void SetCurrentEmployeeId(Guid employeeId) => _currentEmployeeId = employeeId;
}

/// <summary>
/// Client setup helper (implementation, not for docs).
/// </summary>
public static class RemoteOnlyModeClientSetup
{
    public static void Configure(IServiceCollection services, string serverUrl)
    {
        var domainAssembly = typeof(Employee).Assembly;
        services.AddNeatooRemoteFactory(NeatooFactory.Remote,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);
        services.RegisterMatchingName(domainAssembly);
        services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
            new HttpClient { BaseAddress = new Uri(serverUrl), Timeout = TimeSpan.FromSeconds(30) });
        services.AddSingleton<IClientStateService, ClientStateService>();
    }
}

/// <summary>
/// Logical mode setup helper (implementation, not for docs).
/// </summary>
public static class LogicalModeSetup
{
    public static void Configure(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;
        services.AddNeatooRemoteFactory(NeatooFactory.Logical,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);
        services.RegisterMatchingName(domainAssembly);
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddScoped<IDepartmentRepository, InMemoryDepartmentRepository>();
    }
}

/// <summary>
/// Single-tier app example (implementation, not for docs).
/// </summary>
public static class SingleTierAppExample
{
    public static async Task RunLocally()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostApplicationLifetime, SingleTierHostLifetime>();
        LogicalModeSetup.Configure(services);

        using var scope = services.BuildServiceProvider().CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "John";
        await factory.Save(employee);

        var fetched = await factory.Fetch(employee.Id);
        System.Diagnostics.Debug.Assert(fetched?.FirstName == "John");
    }

    private class SingleTierHostLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() { }
    }
}
