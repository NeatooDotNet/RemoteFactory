using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Parameters;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Parameters;

/// <summary>
/// Integration tests for nullable parameter handling in remote operations.
/// Verifies null values serialize correctly over the client-server boundary.
/// </summary>
public class RemoteNullableParameterTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _serverScope;
    private readonly IRemoteNullableTargetFactory _factory;

    public RemoteNullableParameterTests()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        _serverScope = server;
        _clientScope = client;
        _factory = _clientScope.ServiceProvider.GetRequiredService<IRemoteNullableTargetFactory>();
    }

    [Fact]
    public async Task CreateRemote_WithNullValue_Works()
    {
        var result = await _factory.CreateRemote(null);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Null(result.ReceivedValue);
    }

    [Fact]
    public async Task CreateRemote_WithValue_Works()
    {
        var result = await _factory.CreateRemote(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(42, result.ReceivedValue);
    }
}
