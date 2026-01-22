using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.FactoryRoundTrip;

namespace RemoteFactory.IntegrationTests.FactoryRoundTrip;

/// <summary>
/// Integration tests for [Fetch] methods with [Remote] attribute.
/// These tests verify full client-server serialization round-trips.
/// </summary>
public class RemoteFetchRoundTripTests
{
    [Fact]
    public async Task RemoteFetch_Simple_RoundTrips()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteFetchTarget_SimpleFactory>();

        var result = await clientFactory.Fetch(123);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(123, result.ReceivedId);
    }

    [Fact]
    public async Task RemoteFetch_Comparison_ClientVsServer()
    {
        var scopes = ClientServerContainers.Scopes();

        // Client (remote mode) - goes through serialization
        var clientFactory = scopes.client.GetRequiredService<IRemoteFetchTarget_SimpleFactory>();
        var clientResult = await clientFactory.Fetch(50);

        // Server (direct mode) - no serialization but still returns Task
        var serverFactory = scopes.server.GetRequiredService<IRemoteFetchTarget_SimpleFactory>();
        var serverResult = await serverFactory.Fetch(50);

        // Both should produce same results
        Assert.NotNull(clientResult);
        Assert.NotNull(serverResult);
        Assert.True(clientResult.FetchCalled);
        Assert.True(serverResult.FetchCalled);
        Assert.Equal(50, clientResult.ReceivedId);
        Assert.Equal(50, serverResult.ReceivedId);
    }
}
