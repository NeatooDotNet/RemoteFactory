using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;
using RemoteFactory.IntegrationTests.TestContainers;

namespace RemoteFactory.IntegrationTests.BugScenarios;

#region Test Target Classes

/// <summary>
/// REMOTE: Entity with CancellationToken on Remote save operations.
/// The generated factory SHOULD implement IFactorySave&lt;T&gt;.
/// </summary>
[Factory]
public class RemoteSaveWithCancellation : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }

    [Create]
    public RemoteSaveWithCancellation() { }

    [Remote]
    [Insert]
    public async Task InsertAsync(CancellationToken cancellationToken, [Service] IServerOnlyService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        cancellationToken.ThrowIfCancellationRequested();
        InsertCalled = true;
        await Task.CompletedTask;
    }

    [Remote]
    [Update]
    public async Task UpdateAsync(CancellationToken cancellationToken, [Service] IServerOnlyService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        cancellationToken.ThrowIfCancellationRequested();
        UpdateCalled = true;
        await Task.CompletedTask;
    }

    [Remote]
    [Delete]
    public async Task DeleteAsync(CancellationToken cancellationToken, [Service] IServerOnlyService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        cancellationToken.ThrowIfCancellationRequested();
        DeleteCalled = true;
        await Task.CompletedTask;
    }
}

#endregion

/// <summary>
/// REGRESSION TEST: IFactorySave&lt;T&gt; should be resolvable from DI when
/// REMOTE Insert/Update/Delete have CancellationToken parameters.
/// </summary>
public class RemoteSaveWithCancellationTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _serverScope;
    private readonly IRemoteSaveWithCancellationFactory _factory;

    public RemoteSaveWithCancellationTests()
    {
        var (client, server, _) = ClientServerContainers.Scopes();
        _clientScope = client;
        _serverScope = server;
        _factory = _clientScope.ServiceProvider.GetRequiredService<IRemoteSaveWithCancellationFactory>();
    }

    /// <summary>
    /// REGRESSION TEST: IFactorySave&lt;T&gt; should be resolvable from DI when
    /// REMOTE Insert/Update/Delete have CancellationToken parameters.
    /// </summary>
    [Fact]
    public void IFactorySave_ShouldBeRegistered_ForRemoteWithCancellation()
    {
        // Act - Try to resolve IFactorySave<T> from DI
        var factorySave = _clientScope.ServiceProvider.GetService<IFactorySave<RemoteSaveWithCancellation>>();

        // Assert - IFactorySave<T> should be registered
        Assert.NotNull(factorySave);
    }

    /// <summary>
    /// REGRESSION TEST: Saving via IFactorySave&lt;T&gt; should work correctly with remote.
    /// </summary>
    [Fact]
    public async Task IFactorySave_Save_ShouldWork_ForRemoteWithCancellation()
    {
        // Arrange
        var factorySave = _clientScope.ServiceProvider.GetRequiredService<IFactorySave<RemoteSaveWithCancellation>>();
        var entity = _factory.Create();
        entity.IsNew = true;

        // Act
        var result = await factorySave.Save(entity);

        // Assert
        Assert.NotNull(result);
        var typedResult = Assert.IsType<RemoteSaveWithCancellation>(result);
        Assert.True(typedResult.InsertCalled);
    }

    /// <summary>
    /// Verify the factory's Save method still works with remote.
    /// </summary>
    [Fact]
    public async Task Factory_SaveAsync_ShouldWork()
    {
        // Arrange
        var entity = _factory.Create();
        entity.IsNew = true;

        // Act
        var result = await _factory.SaveAsync(entity);

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
    }
}
