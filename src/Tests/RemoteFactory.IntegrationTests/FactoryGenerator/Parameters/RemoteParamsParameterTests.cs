using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Parameters;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Parameters;

/// <summary>
/// Integration tests for remote factory methods with params parameters.
/// Verifies that params arrays serialize correctly over the client-server boundary.
/// </summary>
public class RemoteParamsParameterTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _serverScope;
    private readonly IRemoteParamsTargetFactory _factory;

    public RemoteParamsParameterTests()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        _serverScope = server;
        _clientScope = client;
        _factory = _clientScope.ServiceProvider.GetRequiredService<IRemoteParamsTargetFactory>();
    }

    [Fact]
    public async Task CreateRemoteWithParamsInt_SingleValue_Works()
    {
        var result = await _factory.CreateRemoteWithParamsInt(default, 1);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote ParamsInt count: 1, sum: 1", result.Result);
    }

    [Fact]
    public async Task CreateRemoteWithParamsInt_MultipleValues_Works()
    {
        var result = await _factory.CreateRemoteWithParamsInt(default, 1, 2, 3, 4, 5);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote ParamsInt count: 5, sum: 15", result.Result);
    }

    [Fact]
    public async Task CreateRemoteWithParamsInt_NoValues_Works()
    {
        var result = await _factory.CreateRemoteWithParamsInt();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote ParamsInt count: 0, sum: 0", result.Result);
    }

    [Fact]
    public async Task CreateRemoteWithMixedParams_Works()
    {
        var result = await _factory.CreateRemoteWithMixedParams(42, default, "tag1", "tag2");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Mixed id: 42, tags: 2", result.Result);
    }

    /// <summary>
    /// Tests that CancellationToken flows correctly when domain method has BOTH CT and params.
    /// </summary>
    [Fact]
    public async Task CreateRemoteWithParamsAndCancellation_CTFlows()
    {
        var result = await _factory.CreateRemoteWithParamsAndCancellation(default, 1, 2, 3);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.False(result.WasCancelled);
        Assert.Equal("Remote ParamsWithCT count: 3, cancelled: False", result.Result);
    }
}
