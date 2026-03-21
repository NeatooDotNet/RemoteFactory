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

    /// <summary>
    /// When a [Remote, Fetch] method returns Task&lt;bool&gt; false, the factory
    /// should return null to the client (meaning "object not found").
    /// </summary>
    [Fact]
    public async Task RemoteFetch_BoolFalse_ReturnsNull()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteFetchTarget_BoolFalseFactory>();

        var result = await clientFactory.Fetch(42);

        Assert.Null(result);
    }

    /// <summary>
    /// When a [Remote, Fetch] method returns Task&lt;bool&gt; true, the factory
    /// should return the object to the client with state intact.
    /// </summary>
    [Fact]
    public async Task RemoteFetch_BoolTrue_ReturnsObject()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteFetchTarget_BoolTrueFactory>();

        var result = await clientFactory.Fetch(99);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(99, result.ReceivedId);
    }

    /// <summary>
    /// Verify that the server container also returns null when Fetch returns false (no serialization).
    /// </summary>
    [Fact]
    public async Task RemoteFetch_BoolFalse_ServerAlsoReturnsNull()
    {
        var scopes = ClientServerContainers.Scopes();
        var serverFactory = scopes.server.GetRequiredService<IRemoteFetchTarget_BoolFalseFactory>();

        var result = await serverFactory.Fetch(42);

        Assert.Null(result);
    }

    /// <summary>
    /// [Remote, Fetch] with Task&lt;bool&gt; returning false through the remote transport.
    /// Uses [Service] IServerOnlyService to prove the Fetch executes on the server
    /// (IServerOnlyService is not registered in the client container).
    /// The factory should return null to the client (meaning "object not found").
    /// </summary>
    [Fact]
    public async Task RemoteFetch_Remote_BoolFalse_ReturnsNull()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteFetchTarget_RemoteBoolFalseFactory>();

        var result = await clientFactory.Fetch(42);

        Assert.Null(result);
    }

    /// <summary>
    /// [Remote, Fetch] with Task&lt;bool&gt; returning true through the remote transport.
    /// Uses [Service] IServerOnlyService to prove the Fetch executes on the server.
    /// The factory should return the object to the client with state intact.
    /// </summary>
    [Fact]
    public async Task RemoteFetch_Remote_BoolTrue_ReturnsObject()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteFetchTarget_RemoteBoolTrueFactory>();

        var result = await clientFactory.Fetch(99);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(99, result.ReceivedId);
        // Proves execution happened on the server where IServerOnlyService is available
        Assert.True(result.ServiceWasInjected);
    }

    // --- Auth + bool-false tests (NRE bug fix) ---

    /// <summary>
    /// Scenario 1: [AuthorizeFactory] + [Fetch] returning Task&lt;bool&gt; false via server container.
    /// Before the fix, this threw NullReferenceException because the local method returned
    /// default! (null) for Authorized&lt;T&gt;, and the public wrapper called .Result on null.
    /// After the fix, the local method returns new Authorized&lt;T&gt;() (non-null wrapper with
    /// null Result), so the public wrapper returns null correctly.
    /// </summary>
    [Fact]
    public async Task RemoteFetch_AuthBoolFalse_ServerReturnsNull()
    {
        var scopes = ClientServerContainers.Scopes();
        var serverFactory = scopes.server.GetRequiredService<IRemoteFetchTarget_AuthBoolFalseFactory>();

        var result = await serverFactory.Fetch(42);

        Assert.Null(result);
    }

    /// <summary>
    /// Scenario 2: [AuthorizeFactory] + [Fetch] returning Task&lt;bool&gt; false via client container.
    /// Validates the fix survives serialization round-trip through the client/server transport.
    /// </summary>
    [Fact]
    public async Task RemoteFetch_AuthBoolFalse_ClientReturnsNull()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteFetchTarget_AuthBoolFalseFactory>();

        var result = await clientFactory.Fetch(42);

        Assert.Null(result);
    }

    /// <summary>
    /// Scenario 3: [AuthorizeFactory] + [Fetch] returning Task&lt;bool&gt; true via client container.
    /// Regression guard: verifies the happy path still works when authorization is present.
    /// </summary>
    [Fact]
    public async Task RemoteFetch_AuthBoolTrue_ReturnsObject()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteFetchTarget_AuthBoolTrueFactory>();

        var result = await clientFactory.Fetch(99);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(99, result.ReceivedId);
    }

    /// <summary>
    /// Scenario 5: [AuthorizeFactory] + [Fetch] returning Task&lt;bool&gt; false with [Service]
    /// injection via client container. The [Service] parameter proves execution happens on the
    /// server. Validates that the fix works through the remote transport with service injection.
    /// </summary>
    [Fact]
    public async Task RemoteFetch_RemoteAuthBoolFalse_ReturnsNull()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<IRemoteFetchTarget_RemoteAuthBoolFalseFactory>();

        var result = await clientFactory.Fetch(42);

        Assert.Null(result);
    }
}
