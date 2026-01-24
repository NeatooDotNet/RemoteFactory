using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.UnitTests.Logical;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using System.Reflection;

namespace RemoteFactory.UnitTests.FactoryModes;

/// <summary>
/// Tests that verify correct constructor selection and behavior for each NeatooFactory mode.
/// These tests ensure that Logical mode behaves like Server mode (no serialization).
/// </summary>
public class ModeBehaviorTests
{
    #region DI Registration Tests

    [Fact]
    public void ServerMode_DoesNotRegister_IMakeRemoteDelegateRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(
            NeatooFactory.Server,
            new NeatooSerializationOptions(),
            Assembly.GetExecutingAssembly());

        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetService<IMakeRemoteDelegateRequest>();

        // Assert - Server mode should NOT register IMakeRemoteDelegateRequest
        Assert.Null(service);
    }

    [Fact]
    public void LogicalMode_DoesNotRegister_IMakeRemoteDelegateRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,
            new NeatooSerializationOptions(),
            Assembly.GetExecutingAssembly());

        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetService<IMakeRemoteDelegateRequest>();

        // Assert - Logical mode should NOT register IMakeRemoteDelegateRequest (fixed behavior)
        Assert.Null(service);
    }

    [Fact]
    public void RemoteMode_Registers_IMakeRemoteDelegateRequest()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            new NeatooSerializationOptions(),
            Assembly.GetExecutingAssembly());
        // Remote mode requires an HttpClient
        services.AddKeyedScoped<HttpClient>(RemoteFactoryServices.HttpClientKey,
            (sp, key) => new HttpClient());

        var provider = services.BuildServiceProvider();

        // Act
        var service = provider.GetService<IMakeRemoteDelegateRequest>();

        // Assert - Remote mode SHOULD register IMakeRemoteDelegateRequest
        Assert.NotNull(service);
        Assert.IsType<MakeRemoteDelegateRequest>(service);
    }

    #endregion

    #region Constructor Selection Tests

    [Fact]
    public void ServerMode_UsesLocalConstructor()
    {
        // Arrange
        var provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ILogicalModeTarget_RemoteFactory>();

        // Act - Get the underlying factory and check MakeRemoteDelegateRequest field
        var factoryType = factory.GetType();
        var delegateField = factoryType.GetField("MakeRemoteDelegateRequest",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Assert - Local constructor leaves MakeRemoteDelegateRequest null
        Assert.NotNull(delegateField);
        var delegateValue = delegateField.GetValue(factory);
        Assert.Null(delegateValue);
    }

    [Fact]
    public void LogicalMode_UsesLocalConstructor()
    {
        // Arrange
        var provider = new LogicalContainerBuilder()
            .WithService<IService, Service>()
            .Build();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ILogicalModeTarget_RemoteFactory>();

        // Act - Get the underlying factory and check MakeRemoteDelegateRequest field
        var factoryType = factory.GetType();
        var delegateField = factoryType.GetField("MakeRemoteDelegateRequest",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Assert - Local constructor leaves MakeRemoteDelegateRequest null (same as Server mode)
        Assert.NotNull(delegateField);
        var delegateValue = delegateField.GetValue(factory);
        Assert.Null(delegateValue);
    }

    #endregion

    #region Behavioral Equivalence Tests

    [Fact]
    public async Task ServerMode_Insert_ExecutesLocally()
    {
        // Arrange
        var provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ILogicalModeTarget_RemoteFactory>();
        var entity = new LogicalModeTarget_Remote { IsNew = true };

        // Act
        var result = await factory.Save(entity);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public async Task LogicalMode_Insert_ExecutesLocally()
    {
        // Arrange
        var provider = new LogicalContainerBuilder()
            .WithService<IService, Service>()
            .Build();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ILogicalModeTarget_RemoteFactory>();
        var entity = new LogicalModeTarget_Remote { IsNew = true };

        // Act
        var result = await factory.Save(entity);

        // Assert - Should behave identically to Server mode
        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public async Task ServerAndLogicalModes_ProduceIdenticalResults()
    {
        // Arrange - Server mode
        var serverProvider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
        using var serverScope = serverProvider.CreateScope();
        var serverFactory = serverScope.ServiceProvider.GetRequiredService<ILogicalModeTarget_RemoteFactory>();
        var serverEntity = new LogicalModeTarget_Remote { IsNew = true };

        // Arrange - Logical mode
        var logicalProvider = new LogicalContainerBuilder()
            .WithService<IService, Service>()
            .Build();
        using var logicalScope = logicalProvider.CreateScope();
        var logicalFactory = logicalScope.ServiceProvider.GetRequiredService<ILogicalModeTarget_RemoteFactory>();
        var logicalEntity = new LogicalModeTarget_Remote { IsNew = true };

        // Act
        var serverResult = await serverFactory.Save(serverEntity);
        var logicalResult = await logicalFactory.Save(logicalEntity);

        // Assert - Both modes should produce identical results
        Assert.NotNull(serverResult);
        Assert.NotNull(logicalResult);
        Assert.Equal(serverResult.InsertCalled, logicalResult.InsertCalled);
        Assert.Equal(serverResult.UpdateCalled, logicalResult.UpdateCalled);
        Assert.Equal(serverResult.DeleteCalled, logicalResult.DeleteCalled);
    }

    #endregion
}
