using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Parameters;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Parameters;

/// <summary>
/// Integration tests for remote operations with multiple [Service] parameters.
/// Verifies services are resolved on the server side during remote execution.
/// </summary>
public class RemoteMultipleServiceParameterTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _serverScope;
    private readonly IMultiServiceRemoteTargetFactory _readFactory;
    private readonly IMultiServiceRemoteWriteTargetFactory _writeFactory;

    public RemoteMultipleServiceParameterTests()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        _serverScope = server;
        _clientScope = client;
        _readFactory = _clientScope.ServiceProvider.GetRequiredService<IMultiServiceRemoteTargetFactory>();
        _writeFactory = _clientScope.ServiceProvider.GetRequiredService<IMultiServiceRemoteWriteTargetFactory>();
    }

    #region Remote Read Tests

    [Fact]
    public async Task CreateRemoteWithTwoServices_Works()
    {
        var result = await _readFactory.CreateRemoteWithTwoServices();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Create Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task CreateRemoteWithThreeServices_Works()
    {
        var result = await _readFactory.CreateRemoteWithThreeServices();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Create Service2: 42, Service3: Service3", result.ServiceInfo);
    }

    [Fact]
    public async Task CreateRemoteWithParamsAndServices_Works()
    {
        var result = await _readFactory.CreateRemoteWithParamsAndServices(100, "RemoteTest");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Create Id: 100, Name: RemoteTest, Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task FetchRemoteWithTwoServices_Works()
    {
        var result = await _readFactory.FetchRemoteWithTwoServices();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Remote Fetch Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task CreateRemoteWithTwoServicesAsync_Works()
    {
        var result = await _readFactory.CreateRemoteWithTwoServicesAsync();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Async Create Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task CreateRemoteWithTwoServicesBoolAsync_Works()
    {
        var result = await _readFactory.CreateRemoteWithTwoServicesBoolAsync();

        Assert.NotNull(result);
        Assert.True(result!.CreateCalled);
        Assert.Equal("Remote Async Bool Create Service2: 42", result.ServiceInfo);
    }

    #endregion

    #region Remote Write Tests

    [Fact]
    public async Task InsertRemoteWithTwoServices_Works()
    {
        var obj = _writeFactory.Create();
        obj.IsNew = true;

        var result = await _writeFactory.SaveRemoteWithTwoServices(obj);

        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
        Assert.Equal("Remote Insert Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task UpdateRemoteWithTwoServices_Works()
    {
        var obj = _writeFactory.Create();
        obj.IsNew = false;
        obj.IsDeleted = false;

        var result = await _writeFactory.SaveRemoteWithTwoServices(obj);

        Assert.NotNull(result);
        Assert.True(result!.UpdateCalled);
        Assert.Equal("Remote Update Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task DeleteRemoteWithTwoServices_Works()
    {
        var obj = _writeFactory.Create();
        obj.IsDeleted = true;

        var result = await _writeFactory.SaveRemoteWithTwoServices(obj);

        Assert.NotNull(result);
        Assert.True(result!.DeleteCalled);
        Assert.Equal("Remote Delete Service2: 42", result.ServiceInfo);
    }

    [Fact]
    public async Task InsertRemoteWithThreeServicesAsync_Works()
    {
        var obj = _writeFactory.Create();
        obj.IsNew = true;

        var result = await _writeFactory.SaveRemoteWithThreeServicesAsync(obj, 5);

        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
        Assert.Equal("Remote Insert Priority: 5, Service2: 42, Service3: Service3", result.ServiceInfo);
    }

    #endregion
}
