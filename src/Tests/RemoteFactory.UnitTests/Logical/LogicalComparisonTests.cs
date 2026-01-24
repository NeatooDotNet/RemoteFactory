using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Logical;

/// <summary>
/// Tests that verify Logical and Server modes produce equivalent results.
/// Logical mode uses client-side factory interfaces with local execution.
/// Server mode executes factory methods directly.
/// Both should produce the same results for the same inputs.
/// </summary>
public class LogicalComparisonTests : IDisposable
{
    private readonly IServiceProvider _serverProvider;
    private readonly IServiceProvider _logicalProvider;

    public LogicalComparisonTests()
    {
        _serverProvider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();

        _logicalProvider = new LogicalContainerBuilder()
            .WithService<IService, Service>()
            .Build();
    }

    public void Dispose()
    {
        (_serverProvider as IDisposable)?.Dispose();
        (_logicalProvider as IDisposable)?.Dispose();
    }

    #region IFactorySave<T> Equivalence

    [Fact]
    public async Task IFactorySave_Server_Vs_Logical_Insert_SameResult()
    {
        // Arrange
        var serverSave = _serverProvider.GetRequiredService<IFactorySave<LogicalModeTarget_Remote>>();
        var logicalSave = _logicalProvider.GetRequiredService<IFactorySave<LogicalModeTarget_Remote>>();

        var serverEntity = new LogicalModeTarget_Remote { IsNew = true };
        var logicalEntity = new LogicalModeTarget_Remote { IsNew = true };

        // Act
        var serverResult = await serverSave.Save(serverEntity);
        var logicalResult = await logicalSave.Save(logicalEntity);

        // Assert - Both should call Insert
        Assert.NotNull(serverResult);
        Assert.NotNull(logicalResult);
        Assert.True((serverResult as LogicalModeTarget_Remote)?.InsertCalled);
        Assert.True((logicalResult as LogicalModeTarget_Remote)?.InsertCalled);
    }

    [Fact]
    public async Task IFactorySave_Server_Vs_Logical_Update_SameResult()
    {
        // Arrange
        var serverSave = _serverProvider.GetRequiredService<IFactorySave<LogicalModeTarget_Remote>>();
        var logicalSave = _logicalProvider.GetRequiredService<IFactorySave<LogicalModeTarget_Remote>>();

        var serverEntity = new LogicalModeTarget_Remote { IsNew = false, IsDeleted = false };
        var logicalEntity = new LogicalModeTarget_Remote { IsNew = false, IsDeleted = false };

        // Act
        var serverResult = await serverSave.Save(serverEntity);
        var logicalResult = await logicalSave.Save(logicalEntity);

        // Assert - Both should call Update
        Assert.NotNull(serverResult);
        Assert.NotNull(logicalResult);
        Assert.True((serverResult as LogicalModeTarget_Remote)?.UpdateCalled);
        Assert.True((logicalResult as LogicalModeTarget_Remote)?.UpdateCalled);
    }

    [Fact]
    public async Task IFactorySave_Server_Vs_Logical_Delete_SameResult()
    {
        // Arrange
        var serverSave = _serverProvider.GetRequiredService<IFactorySave<LogicalModeTarget_Remote>>();
        var logicalSave = _logicalProvider.GetRequiredService<IFactorySave<LogicalModeTarget_Remote>>();

        var serverEntity = new LogicalModeTarget_Remote { IsNew = false, IsDeleted = true };
        var logicalEntity = new LogicalModeTarget_Remote { IsNew = false, IsDeleted = true };

        // Act
        var serverResult = await serverSave.Save(serverEntity);
        var logicalResult = await logicalSave.Save(logicalEntity);

        // Assert - Both should call Delete
        Assert.NotNull(serverResult);
        Assert.NotNull(logicalResult);
        Assert.True((serverResult as LogicalModeTarget_Remote)?.DeleteCalled);
        Assert.True((logicalResult as LogicalModeTarget_Remote)?.DeleteCalled);
    }

    #endregion

    #region Factory Interface Equivalence

    [Fact]
    public async Task Factory_Server_Vs_Logical_Save_Insert_SameResult()
    {
        // Arrange
        var serverFactory = _serverProvider.GetRequiredService<ILogicalModeTarget_RemoteFactory>();
        var logicalFactory = _logicalProvider.GetRequiredService<ILogicalModeTarget_RemoteFactory>();

        var serverEntity = new LogicalModeTarget_Remote { IsNew = true };
        var logicalEntity = new LogicalModeTarget_Remote { IsNew = true };

        // Act
        var serverResult = await serverFactory.Save(serverEntity);
        var logicalResult = await logicalFactory.Save(logicalEntity);

        // Assert
        Assert.NotNull(serverResult);
        Assert.NotNull(logicalResult);
        Assert.True(serverResult.InsertCalled);
        Assert.True(logicalResult.InsertCalled);
    }

    [Fact]
    public async Task Factory_Server_Vs_Logical_Save_WithService_SameResult()
    {
        // Arrange
        var serverFactory = _serverProvider.GetRequiredService<ILogicalModeTarget_RemoteWithServiceFactory>();
        var logicalFactory = _logicalProvider.GetRequiredService<ILogicalModeTarget_RemoteWithServiceFactory>();

        var serverEntity = new LogicalModeTarget_RemoteWithService { IsNew = true };
        var logicalEntity = new LogicalModeTarget_RemoteWithService { IsNew = true };

        // Act
        var serverResult = await serverFactory.Save(serverEntity);
        var logicalResult = await logicalFactory.Save(logicalEntity);

        // Assert - Both should have service injected
        Assert.NotNull(serverResult);
        Assert.NotNull(logicalResult);
        Assert.True(serverResult.InsertCalled);
        Assert.True(logicalResult.InsertCalled);
        Assert.True(serverResult.ServiceWasInjected);
        Assert.True(logicalResult.ServiceWasInjected);
    }

    #endregion

    #region Factory Resolution Equivalence

    [Fact]
    public void Factory_Server_Vs_Logical_BothResolveFactory()
    {
        // Both modes should resolve the generated factory interface
        var serverFactory = _serverProvider.GetService<ILogicalModeTarget_RemoteFactory>();
        var logicalFactory = _logicalProvider.GetService<ILogicalModeTarget_RemoteFactory>();

        Assert.NotNull(serverFactory);
        Assert.NotNull(logicalFactory);
    }

    [Fact]
    public void IFactorySave_Server_Vs_Logical_BothResolve()
    {
        // Both modes should resolve IFactorySave<T>
        var serverSave = _serverProvider.GetService<IFactorySave<LogicalModeTarget_Remote>>();
        var logicalSave = _logicalProvider.GetService<IFactorySave<LogicalModeTarget_Remote>>();

        Assert.NotNull(serverSave);
        Assert.NotNull(logicalSave);
    }

    #endregion
}
