using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Execute;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Execute;

/// <summary>
/// Integration tests for [Execute] methods on non-static [Factory] classes.
/// Verifies client-server round-trip with JSON serialization.
/// </summary>
public class ClassExecuteRoundTripTests
{
    private readonly IServiceScope _serverScope;
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _localScope;

    public ClassExecuteRoundTripTests()
    {
        var scopes = ClientServerContainers.Scopes();
        _serverScope = scopes.server;
        _clientScope = scopes.client;
        _localScope = scopes.local;
    }

    #region Remote (Client-Server) Tests

    [Fact]
    public async Task ClassExecute_Remote_WithService()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IClassExecRemoteFactory>();

        var result = await factory.Run("test");

        Assert.NotNull(result);
        Assert.Equal(99, result.Id);
        Assert.Equal("Remote: test", result.Name);
    }

    [Fact]
    public async Task ClassExecute_Remote_MultiService()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IClassExecMultiFactory>();

        var result = await factory.RunSvc("data");

        Assert.NotNull(result);
        Assert.Equal("data-42", result.Result);
    }

    [Fact]
    public async Task ClassExecute_Remote_NoService()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IClassExecMultiFactory>();

        var result = await factory.RunNoSvc("plain");

        Assert.NotNull(result);
        Assert.Equal("NoSvc: plain", result.Result);
    }

    [Fact]
    public async Task ClassExecute_Remote_CreateStillWorks()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IClassExecRemoteFactory>();

        var result = await factory.Create("created");

        Assert.NotNull(result);
        Assert.Equal("created", result.Name);
    }

    #endregion

    #region Local (Logical) Tests

    [Fact]
    public async Task ClassExecute_Local_WithService()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IClassExecRemoteFactory>();

        var result = await factory.Run("local");

        Assert.NotNull(result);
        Assert.Equal(99, result.Id);
        Assert.Equal("Remote: local", result.Name);
    }

    [Fact]
    public async Task ClassExecute_Local_MultiService()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IClassExecMultiFactory>();

        var result = await factory.RunSvc("local data");

        Assert.NotNull(result);
        Assert.Equal("local data-42", result.Result);
    }

    [Fact]
    public async Task ClassExecute_Local_NoService()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IClassExecMultiFactory>();

        var result = await factory.RunNoSvc("local plain");

        Assert.NotNull(result);
        Assert.Equal("NoSvc: local plain", result.Result);
    }

    #endregion

    #region Server (Direct) Tests

    [Fact]
    public async Task ClassExecute_Server_WithService()
    {
        var factory = _serverScope.ServiceProvider.GetRequiredService<IClassExecRemoteFactory>();

        var result = await factory.Run("server");

        Assert.NotNull(result);
        Assert.Equal(99, result.Id);
        Assert.Equal("Remote: server", result.Name);
    }

    #endregion
}
