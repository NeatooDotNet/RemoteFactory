using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.ValueObjects;
using EmployeeManagement.Infrastructure;
using EmployeeManagement.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Tests.Samples.FactoryModes;

/// <summary>
/// Demonstrates using Logical mode for testing without HTTP overhead.
/// </summary>
public class LogicalModeTestingSample
{
    #region modes-logical-testing
    [Fact]
    public async Task TestEmployeeCreationWithLogicalMode()
    {
        // Test domain logic without HTTP overhead
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder => builder.AddDebug());

        // Add IHostApplicationLifetime (required for event handling)
        services.AddSingleton<IHostApplicationLifetime, TestHostLifetime>();

        var domainAssembly = typeof(Employee).Assembly;

        // Configure Logical mode - direct execution, no serialization
        services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Register factory types
        services.RegisterMatchingName(domainAssembly);

        // Register in-memory repository for testing
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();

        // Add infrastructure services
        services.AddInfrastructureServices();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Resolve the factory
        var factory = scope.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create a new employee
        var employee = factory.Create();
        employee.FirstName = "Jane";
        employee.LastName = "Smith";
        employee.Email = new EmailAddress("jane.smith@example.com");

        // Method executes directly, no serialization
        await factory.Save(employee);

        // Fetch the employee to verify persistence
        var fetched = await factory.Fetch(employee.Id);

        // Assert the data was saved correctly
        Assert.NotNull(fetched);
        Assert.Equal("Jane", fetched.FirstName);
        Assert.Equal("Smith", fetched.LastName);
    }

    private class TestHostLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() { }
    }
    #endregion
}
