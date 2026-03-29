using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.LazyLoad;

namespace RemoteFactory.IntegrationTests.LazyLoad;

/// <summary>
/// Integration tests for LazyLoad&lt;T&gt; properties across the client-server boundary.
/// Uses ClientServerContainers to validate full ordinal serialization round-trip.
/// </summary>
public class LazyLoadRoundTripTests
{
    // TS-LL-021: Client-server loaded round-trip
    [Fact]
    public async Task ClientServer_Loaded_RoundTrip()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<ILazyLoadRoundTrip_LoadedFactory>();

        var result = await clientFactory.Fetch();

        Assert.NotNull(result);
        Assert.Equal("ServerData", result.Name);
        Assert.NotNull(result.Lines);
        Assert.Equal("loaded-from-server", result.Lines.Value);
        Assert.True(result.Lines.IsLoaded);
    }

    // TS-LL-022: Client-server unloaded round-trip
    [Fact]
    public async Task ClientServer_Unloaded_RoundTrip()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.GetRequiredService<ILazyLoadRoundTrip_UnloadedFactory>();

        var result = await clientFactory.Fetch();

        Assert.NotNull(result);
        Assert.Equal("ServerData", result.Name);
        Assert.NotNull(result.Lines);
        Assert.Null(result.Lines.Value);
        Assert.False(result.Lines.IsLoaded);
    }

    // Additional: verify server-side direct execution produces same results
    [Fact]
    public async Task Server_Loaded_DirectExecution()
    {
        var scopes = ClientServerContainers.Scopes();
        var serverFactory = scopes.server.GetRequiredService<ILazyLoadRoundTrip_LoadedFactory>();

        var result = await serverFactory.Fetch();

        Assert.NotNull(result);
        Assert.Equal("ServerData", result.Name);
        Assert.NotNull(result.Lines);
        Assert.Equal("loaded-from-server", result.Lines.Value);
        Assert.True(result.Lines.IsLoaded);
    }

    // Additional: verify local (logical) mode works
    [Fact]
    public async Task Local_Loaded_LogicalMode()
    {
        var scopes = ClientServerContainers.Scopes();
        var localFactory = scopes.local.GetRequiredService<ILazyLoadRoundTrip_LoadedFactory>();

        var result = await localFactory.Fetch();

        Assert.NotNull(result);
        Assert.Equal("ServerData", result.Name);
        Assert.NotNull(result.Lines);
        Assert.Equal("loaded-from-server", result.Lines.Value);
        Assert.True(result.Lines.IsLoaded);
    }
}
