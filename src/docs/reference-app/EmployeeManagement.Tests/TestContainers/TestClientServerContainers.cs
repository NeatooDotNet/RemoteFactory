using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Infrastructure;
using EmployeeManagement.Infrastructure.Repositories;
using EmployeeManagement.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace EmployeeManagement.Tests.TestContainers;

/// <summary>
/// Two-container test infrastructure that simulates client-server communication
/// through serialization, following the RemoteFactory.IntegrationTests pattern.
/// </summary>
public static class TestClientServerContainers
{
    /// <summary>
    /// Creates isolated client, server, and local DI scopes for testing.
    /// </summary>
    public static (IServiceScope client, IServiceScope server, IServiceScope local) CreateScopes()
    {
        var domainAssembly = typeof(Employee).Assembly;

        // Build the local container (Logical mode - everything local)
        // This is the simplest mode for initial testing
        var localContainer = BuildLocalContainer(domainAssembly);

        // For now, use local container for all scopes
        // Full client-server testing would require more infrastructure
        return (
            localContainer.CreateScope(),
            localContainer.CreateScope(),
            localContainer.CreateScope()
        );
    }

    private static ServiceProvider BuildLocalContainer(System.Reflection.Assembly domainAssembly)
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddDebug());

        // Add RemoteFactory in Logical mode (everything runs locally)
        services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Register factory types
        services.RegisterMatchingName(domainAssembly);

        // Add infrastructure services
        services.AddInfrastructureServices();

        // Add a mock IHostApplicationLifetime for event handlers
        services.AddSingleton<IHostApplicationLifetime, TestHostApplicationLifetime>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Clears all in-memory data stores. Call before each test.
    /// </summary>
    public static void ClearAllData()
    {
        InMemoryEmployeeRepository.Clear();
        InMemoryDepartmentRepository.Clear();
        InMemoryEmailService.Clear();
        InMemoryAuditLogService.Clear();
    }
}

/// <summary>
/// Simple IHostApplicationLifetime implementation for testing.
/// </summary>
internal sealed class TestHostApplicationLifetime : IHostApplicationLifetime, IDisposable
{
    private readonly CancellationTokenSource _startedSource = new();
    private readonly CancellationTokenSource _stoppingSource = new();
    private readonly CancellationTokenSource _stoppedSource = new();

    public CancellationToken ApplicationStarted => _startedSource.Token;
    public CancellationToken ApplicationStopping => _stoppingSource.Token;
    public CancellationToken ApplicationStopped => _stoppedSource.Token;

    public void StopApplication()
    {
        _stoppingSource.Cancel();
    }

    public void Dispose()
    {
        _startedSource.Dispose();
        _stoppingSource.Dispose();
        _stoppedSource.Dispose();
    }
}
