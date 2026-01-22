using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Logical;

/// <summary>
/// Unit tests for NeatooFactory.Logical mode with [Remote] save methods.
/// These tests validate that Logical mode works correctly when entities
/// have [Remote] attributes on their [Insert], [Update], and [Delete] methods.
/// </summary>
/// <remarks>
/// Logical mode combines client-side factory interfaces with local method execution.
/// Unlike Server mode where [Remote] methods are executed directly,
/// Logical mode uses the client-side factory interface pattern but executes locally.
/// </remarks>
public class LogicalModeTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public LogicalModeTests()
    {
        _provider = new LogicalContainerBuilder()
            .WithService<IService, Service>()
            .Build();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    #region IFactorySave<T> Resolution Tests

    [Fact]
    public void LogicalMode_IFactorySave_CanBeResolved()
    {
        var factorySave = _provider.GetService<IFactorySave<LogicalModeTarget_Remote>>();

        Assert.NotNull(factorySave);
    }

    [Fact]
    public void LogicalMode_IFactorySave_WithService_CanBeResolved()
    {
        var factorySave = _provider.GetService<IFactorySave<LogicalModeTarget_RemoteWithService>>();

        Assert.NotNull(factorySave);
    }

    #endregion

    #region IFactorySave<T>.Save() Tests

    [Fact]
    public async Task LogicalMode_IFactorySave_Save_Insert()
    {
        var factorySave = _provider.GetRequiredService<IFactorySave<LogicalModeTarget_Remote>>();
        var entity = new LogicalModeTarget_Remote { IsNew = true };

        var result = await factorySave.Save(entity);

        Assert.NotNull(result);
        var typedResult = result as LogicalModeTarget_Remote;
        Assert.NotNull(typedResult);
        Assert.True(typedResult.InsertCalled);
    }

    [Fact]
    public async Task LogicalMode_IFactorySave_Save_Update()
    {
        var factorySave = _provider.GetRequiredService<IFactorySave<LogicalModeTarget_Remote>>();
        var entity = new LogicalModeTarget_Remote { IsNew = false, IsDeleted = false };

        var result = await factorySave.Save(entity);

        Assert.NotNull(result);
        var typedResult = result as LogicalModeTarget_Remote;
        Assert.NotNull(typedResult);
        Assert.True(typedResult.UpdateCalled);
    }

    [Fact]
    public async Task LogicalMode_IFactorySave_Save_Delete()
    {
        var factorySave = _provider.GetRequiredService<IFactorySave<LogicalModeTarget_Remote>>();
        var entity = new LogicalModeTarget_Remote { IsNew = false, IsDeleted = true };

        var result = await factorySave.Save(entity);

        Assert.NotNull(result);
        var typedResult = result as LogicalModeTarget_Remote;
        Assert.NotNull(typedResult);
        Assert.True(typedResult.DeleteCalled);
    }

    [Fact]
    public async Task LogicalMode_IFactorySave_Save_WithService_Insert()
    {
        var factorySave = _provider.GetRequiredService<IFactorySave<LogicalModeTarget_RemoteWithService>>();
        var entity = new LogicalModeTarget_RemoteWithService { IsNew = true };

        var result = await factorySave.Save(entity);

        Assert.NotNull(result);
        var typedResult = result as LogicalModeTarget_RemoteWithService;
        Assert.NotNull(typedResult);
        Assert.True(typedResult.InsertCalled);
        Assert.True(typedResult.ServiceWasInjected);
    }

    #endregion

    #region Factory.Save() Tests

    [Fact]
    public async Task LogicalMode_Factory_Save_Insert()
    {
        var factory = _provider.GetRequiredService<ILogicalModeTarget_RemoteFactory>();
        var entity = new LogicalModeTarget_Remote { IsNew = true };

        var result = await factory.Save(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public async Task LogicalMode_Factory_Save_Update()
    {
        var factory = _provider.GetRequiredService<ILogicalModeTarget_RemoteFactory>();
        var entity = new LogicalModeTarget_Remote { IsNew = false, IsDeleted = false };

        var result = await factory.Save(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
    }

    [Fact]
    public async Task LogicalMode_Factory_Save_Delete()
    {
        var factory = _provider.GetRequiredService<ILogicalModeTarget_RemoteFactory>();
        var entity = new LogicalModeTarget_Remote { IsNew = false, IsDeleted = true };

        var result = await factory.Save(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
    }

    [Fact]
    public async Task LogicalMode_Factory_Save_WithService_Insert()
    {
        var factory = _provider.GetRequiredService<ILogicalModeTarget_RemoteWithServiceFactory>();
        var entity = new LogicalModeTarget_RemoteWithService { IsNew = true };

        var result = await factory.Save(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion
}
