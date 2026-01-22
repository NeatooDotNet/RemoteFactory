using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.FactoryRoundTrip;

namespace RemoteFactory.IntegrationTests.FactoryRoundTrip;

/// <summary>
/// Integration tests for [Create] methods with [Remote] attribute.
/// These tests verify full client-server serialization round-trips.
/// </summary>
public class RemoteCreateRoundTripTests
{
    [Fact]
    public async Task RemoteCreate_Simple_RoundTrips()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteCreateTarget_SimpleFactory>();

        var result = await clientFactory.Create(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(42, result.ReceivedValue);
    }

    [Fact]
    public async Task RemoteCreate_WithService_RoundTrips()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteCreateTarget_WithServiceFactory>();

        var result = await clientFactory.Create(99);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(99, result.ReceivedValue);
        // Service is injected on the server side
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task RemoteCreate_Comparison_ClientVsServer()
    {
        var scopes = ClientServerContainers.Scopes();

        // Client (remote mode) - goes through serialization
        var clientFactory = scopes.client.GetRequiredService<IRemoteCreateTarget_SimpleFactory>();
        var clientResult = await clientFactory.Create(10);

        // Server (direct mode) - no serialization but still returns Task
        var serverFactory = scopes.server.GetRequiredService<IRemoteCreateTarget_SimpleFactory>();
        var serverResult = await serverFactory.Create(10);

        // Both should produce same results
        Assert.NotNull(clientResult);
        Assert.NotNull(serverResult);
        Assert.True(clientResult.CreateCalled);
        Assert.True(serverResult.CreateCalled);
        Assert.Equal(10, clientResult.ReceivedValue);
        Assert.Equal(10, serverResult.ReceivedValue);
    }
}
