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
    public async Task TestWithLogicalMode()
    {
        // Logical mode: all operations local, no HTTP, no serialization
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddDebug());
        services.AddSingleton<IHostApplicationLifetime, TestHostLifetime>();
        services.AddNeatooRemoteFactory(NeatooFactory.Logical,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            typeof(Employee).Assembly);
        services.RegisterMatchingName(typeof(Employee).Assembly);
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
        services.AddInfrastructureServices();

        using var scope = services.BuildServiceProvider().CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Jane";
        employee.LastName = "Smith";
        employee.Email = new EmailAddress("jane@example.com");
        employee.DepartmentId = Guid.NewGuid();
        await factory.Save(employee);  // Executes directly, no HTTP

        var fetched = await factory.Fetch(employee.Id);
        Assert.Equal("Jane", fetched?.FirstName);
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
