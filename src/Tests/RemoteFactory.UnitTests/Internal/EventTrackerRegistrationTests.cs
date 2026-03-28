using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.Internal;

/// <summary>
/// DI container validation tests verifying correct IEventTracker
/// registration for each NeatooFactory mode. Remote-mode clients
/// do not register IEventTracker at all; Server/Logical get EventTracker.
/// </summary>
public class EventTrackerRegistrationTests
{
    /// <summary>
    /// Use the RemoteFactory assembly which has no [Factory] types,
    /// so RegisterFactories is a no-op and the test isolates core
    /// service registration behavior.
    /// </summary>
    private static readonly System.Reflection.Assembly CoreAssembly = typeof(IEventTracker).Assembly;

    [Fact]
    public void RemoteMode_DoesNotRegister_IEventTracker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, CoreAssembly);
        using var sp = services.BuildServiceProvider();

        // Act
        var tracker = sp.GetService<IEventTracker>();

        // Assert -- IEventTracker is not registered for Remote mode
        Assert.Null(tracker);
    }

    [Fact]
    public void ServerMode_Resolves_EventTracker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // EventTracker requires ILogger<EventTracker>
        services.AddNeatooRemoteFactory(NeatooFactory.Server, CoreAssembly);
        using var sp = services.BuildServiceProvider();

        // Act
        var tracker = sp.GetRequiredService<IEventTracker>();

        // Assert
        Assert.IsType<EventTracker>(tracker);
    }

    [Fact]
    public void LogicalMode_Resolves_EventTracker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // EventTracker requires ILogger<EventTracker>
        services.AddNeatooRemoteFactory(NeatooFactory.Logical, CoreAssembly);
        using var sp = services.BuildServiceProvider();

        // Act
        var tracker = sp.GetRequiredService<IEventTracker>();

        // Assert
        Assert.IsType<EventTracker>(tracker);
    }

    [Fact]
    public void RemoteMode_BuildsWithValidateOnBuild_WithoutLoggingAndHosting()
    {
        // Arrange -- no AddLogging(), no IHostApplicationLifetime
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Remote, CoreAssembly);

        // Act -- ValidateOnBuild walks constructor dependency graphs.
        // IEventTracker is not registered for Remote mode, so there is no
        // EventTracker constructor to validate and no ILogger dependency.
        var sp = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        // Assert -- container built successfully; IEventTracker is not registered
        var tracker = sp.GetService<IEventTracker>();
        Assert.Null(tracker);
        sp.Dispose();
    }
}
