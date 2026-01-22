using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.UnitTests.TestTargets.Core;

namespace RemoteFactory.UnitTests.FactoryGenerator.Core;

/// <summary>
/// Tests for custom IFactoryCore implementations with synchronous factory methods.
/// Custom FactoryCore implementations can override methods to add logging, tracking,
/// or modify behavior before/after factory method calls.
/// </summary>
public class FactoryCoreTests
{
    [Fact]
    public void FactoryCore_Should_Call_DoFactoryMethodCall()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(FactoryCoreTarget_Sync).Assembly);

        // Register custom FactoryCore
        var trackingCore = new TrackingFactoryCore_Sync();
        services.AddSingleton<IFactoryCore<FactoryCoreTarget_Sync>>(trackingCore);
        services.AddScoped<FactoryCoreTarget_Sync>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();

        // Act
        var factory = scope.ServiceProvider.GetRequiredService<IFactoryCoreTarget_SyncFactory>();
        var result = factory.Create();

        // Assert
        Assert.NotNull(result);
        Assert.True(trackingCore.DoFactoryMethodCallCalled);
    }
}
