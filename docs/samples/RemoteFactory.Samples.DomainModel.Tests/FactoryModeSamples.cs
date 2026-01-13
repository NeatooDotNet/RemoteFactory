/// <summary>
/// Code samples for docs/reference/factory-modes.md
///
/// Snippets in this file:
/// - docs:reference/factory-modes:logical-configuration
/// - docs:reference/factory-modes:logical-use-cases
/// - docs:reference/factory-modes:switching-modes
/// - docs:reference/factory-modes:register-matching-name
/// </summary>

using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.Samples.DomainModel.FactoryOperations;
#pragma warning disable xUnit1013 // Suppress warning about public test method lacking Fact attribute
using Xunit;

namespace RemoteFactory.Samples.DomainModel.Tests;

/// <summary>
/// Factory mode configuration and testing examples.
/// </summary>
public class FactoryModeSamples
{
    public void ConfigureLogicalMode(IServiceCollection services)
    {
#region docs:reference/factory-modes:logical-configuration
services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(IPersonModel).Assembly);
#endregion
    }

#region docs:reference/factory-modes:logical-use-cases
// Unit testing
[Fact]
public void TestCreatePerson()
{
    var services = new ServiceCollection();
    services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(IPersonModel).Assembly);

    var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();
    var factory = scope.ServiceProvider.GetRequiredService<IPersonModelFactory>();

    var person = factory.Create();
    Assert.NotNull(person);
}

// Single-tier desktop app
public class DesktopApp
{
    public DesktopApp()
    {
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(IPersonModel).Assembly);
        // No HTTP, no server - everything local
    }
}
#endregion

    public void ConfigureFromSettings(IServiceCollection services, string? factoryModeSetting)
    {
#region docs:reference/factory-modes:switching-modes
// Determined by configuration
var mode = factoryModeSetting switch
{
    "Server" => NeatooFactory.Server,
    "Remote" => NeatooFactory.Remote,
    "Logical" => NeatooFactory.Logical,
    _ => NeatooFactory.Remote
};

services.AddNeatooRemoteFactory(mode, typeof(IPersonModel).Assembly);
#endregion
    }

    public void RegisterServices(IServiceCollection services)
    {
#region docs:reference/factory-modes:register-matching-name
// Register types following naming convention
services.RegisterMatchingName(typeof(IPersonModel).Assembly);
#endregion
    }
}
