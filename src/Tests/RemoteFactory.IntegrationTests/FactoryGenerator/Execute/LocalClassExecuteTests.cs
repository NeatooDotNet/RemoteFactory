using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Execute;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Execute;

/// <summary>
/// Integration tests for class-level [Execute] WITHOUT [Remote] (EXRM-002, AC-1 / AC-2 / AC-4).
/// </summary>
/// <remarks>
/// The class-factory counterpart of <see cref="LocalExecuteTests"/>:
/// - A bare [Execute] called through the client's factory runs on the client, resolves [Service]
///   parameters from client DI, and makes no remote request (<see cref="RemoteCallCounter"/> stays
///   at zero).
/// - A bare [Execute] taking a server-only service fails on the client at call time with a
///   service-resolution error, still with no remote request; the same method succeeds on the server.
/// - [Remote, Execute] is unchanged: from the client scope it crosses the wire exactly once.
/// - A client-side [AuthorizeFactory] check on a bare [Execute] runs locally: allowed executes,
///   denied returns null, neither touches the wire.
/// - A [Remote] auth method forces the wire by the same rule as every other operation.
/// </remarks>
public class LocalClassExecuteTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _serverScope;
    private readonly IServiceScope _localScope;

    public LocalClassExecuteTests()
    {
        var scopes = ClientServerContainers.Scopes();
        _clientScope = scopes.client;
        _serverScope = scopes.server;
        _localScope = scopes.local;
    }

    private int ClientRemoteCalls => _clientScope.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count;

    #region Bare [Execute] runs on the client

    [Fact]
    public async Task BareExecute_ClientScope_RunsLocally_WithClientService_NoRemoteRequest()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IClassExecLocalFactory>();

        var result = await factory.RunLocal("hello");

        Assert.Equal("Local: hello", result.Result);
        Assert.Equal(0, ClientRemoteCalls);
    }

    [Fact]
    public async Task BareExecute_ClientScope_ServerOnlyService_FailsAtCallTime_NoRemoteRequest()
    {
        // The factory resolves on the client — the method exists there — but invoking it resolves
        // the server-only service from client DI and fails there. Nothing crosses the wire.
        var factory = _clientScope.ServiceProvider.GetRequiredService<IClassExecLocalFactory>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => factory.RunLocalServerOnly("hello"));

        // Specifically the DI resolution failure for the server-only service.
        Assert.Contains(nameof(IServerOnlyService), ex.Message);
        Assert.Equal(0, ClientRemoteCalls);
    }

    [Fact]
    public async Task BareExecute_ServerScope_ServerOnlyService_Resolves()
    {
        // The same bare method on the server resolves the server-only service from server DI:
        // "runs wherever it is called".
        var factory = _serverScope.ServiceProvider.GetRequiredService<IClassExecLocalFactory>();

        var result = await factory.RunLocalServerOnly("hello");

        Assert.Equal("LocalServerOnly: ServerOnly: hello", result.Result);
    }

    [Fact]
    public async Task BareExecute_NoServices_RunsInRemoteLogicalAndServerContainers()
    {
        var client = _clientScope.ServiceProvider.GetRequiredService<IClassExecLocalFactory>();
        var local = _localScope.ServiceProvider.GetRequiredService<IClassExecLocalFactory>();
        var server = _serverScope.ServiceProvider.GetRequiredService<IClassExecLocalFactory>();

        Assert.Equal("NoServices: a", (await client.RunLocalNoServices("a")).Result);
        Assert.Equal("NoServices: b", (await local.RunLocalNoServices("b")).Result);
        Assert.Equal("NoServices: c", (await server.RunLocalNoServices("c")).Result);
        Assert.Equal(0, ClientRemoteCalls);
    }

    #endregion

    #region [Remote, Execute] is unchanged

    [Fact]
    public async Task RemoteExecute_ClientScope_CrossesTheWireOnce()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IClassExecLocalFactory>();

        var result = await factory.RunRemote("hello");

        Assert.Equal("Remote: ServerOnly: hello", result.Result);
        Assert.Equal(1, ClientRemoteCalls);
    }

    [Fact]
    public async Task RemoteExecute_LogicalScope_RunsLocally_NoWire()
    {
        // The Logical container has no wire implementation, so the counter is structurally zero;
        // the proof is that the [Remote] method resolves and runs there at all.
        var factory = _localScope.ServiceProvider.GetRequiredService<IClassExecLocalFactory>();

        var result = await factory.RunRemote("hello");

        Assert.Equal("Remote: ServerOnly: hello", result.Result);
        Assert.Equal(0, _localScope.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count);
    }

    #endregion

    #region Client-side [AuthorizeFactory] on a bare [Execute] stays local

    [Fact]
    public async Task BareExecute_ClientSideAuth_Allowed_RunsLocally_NoRemoteRequest()
    {
        LocalExecAuth.ShouldAllow = true;
        var factory = _clientScope.ServiceProvider.GetRequiredService<IClassExecLocalAuthFactory>();

        var result = await factory.Run("hello");

        Assert.NotNull(result);
        Assert.Equal("Authorized: hello", result.Result);
        Assert.Equal(0, ClientRemoteCalls);
    }

    [Fact]
    public async Task BareExecute_ClientSideAuth_Denied_ReturnsNull_NoRemoteRequest()
    {
        LocalExecAuth.ShouldAllow = false;
        try
        {
            var factory = _clientScope.ServiceProvider.GetRequiredService<IClassExecLocalAuthFactory>();

            // A denied read-shaped operation returns null (Authorized<T>.Result stays default);
            // only write paths throw. Same as a denied Create.
            var result = await factory.Run("hello");

            Assert.Null(result);
            Assert.Equal(0, ClientRemoteCalls);
        }
        finally
        {
            LocalExecAuth.ShouldAllow = true;
        }
    }

    [Fact]
    public async Task RemoteExecute_WithAuth_Allowed_CrossesTheWireOnce()
    {
        LocalExecAuth.ShouldAllow = true;
        var factory = _clientScope.ServiceProvider.GetRequiredService<IClassExecLocalAuthFactory>();

        var result = await factory.RunRemote("hello");

        Assert.NotNull(result);
        Assert.Equal("AuthorizedRemote: ServerOnly: hello", result.Result);
        Assert.Equal(1, ClientRemoteCalls);
    }

    [Fact]
    public async Task RemoteExecute_WithAuth_Denied_ReturnsNull_StillCrossesTheWireOnce()
    {
        // The [Remote] sibling: the check runs on the server, the denial comes back as a null
        // result through the same nullable factory signature. Pins the generator fix for the
        // [Remote, Execute] shape.
        LocalExecAuth.ShouldAllow = false;
        try
        {
            var factory = _clientScope.ServiceProvider.GetRequiredService<IClassExecLocalAuthFactory>();

            var result = await factory.RunRemote("hello");

            Assert.Null(result);
            Assert.Equal(1, ClientRemoteCalls);
        }
        finally
        {
            LocalExecAuth.ShouldAllow = true;
        }
    }

    #endregion

    #region A [Remote] auth method forces the wire

    [Fact]
    public async Task BareExecute_RemoteAuthMethod_ForcesTheWireOnce()
    {
        // The auth implementation exists only on the server; the client never registers it.
        var (client, _, _) = ClientServerContainers.Scopes(
            configureServer: services => services.AddScoped<IExecAuthRemote, ExecAuthServerOnly>());
        var factory = client.ServiceProvider.GetRequiredService<IClassExecRemoteAuthFactory>();

        var result = await factory.Run("hello");

        Assert.NotNull(result);
        Assert.Equal("RemoteAuth: hello", result.Result);
        Assert.Equal(1, client.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count);
    }

    #endregion

    #region [AspAuthorize] on a bare [Execute] forces the wire

    /// <summary>
    /// Stands in for the AspNetCore <c>IAspAuthorize</c> (which no test project references):
    /// records the policies it was asked about and answers per <see cref="Deny"/>. A null
    /// answer is access; text is a denial message.
    /// </summary>
    private sealed class RecordingAspAuthorize : IAspAuthorize
    {
        public List<string?> Policies { get; } = [];
        public bool Deny { get; init; }

        public Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false)
        {
            Policies.AddRange(authorizeData.Select(d => d.Policy));
            return Task.FromResult(Deny ? "Denied by test" : null);
        }
    }

    private static (IServiceScope client, RecordingAspAuthorize asp) ScopesWithAspAuthorize(bool deny)
    {
        // Singleton per container so the instance read back here is the one the server's
        // generated factory resolved during the request.
        var recorder = new RecordingAspAuthorize { Deny = deny };
        var (client, _, _) = ClientServerContainers.Scopes(
            configureServer: services => services.AddSingleton<IAspAuthorize>(recorder));
        return (client, recorder);
    }

    [Fact]
    public async Task BareExecute_AspAuthorize_Allowed_CrossesTheWireOnce_AndIsConsulted()
    {
        var (client, asp) = ScopesWithAspAuthorize(deny: false);
        var factory = client.ServiceProvider.GetRequiredService<IClassExecAspAuthFactory>();

        var result = await factory.Run("hello");

        Assert.NotNull(result);
        Assert.Equal("Asp: ServerOnly: hello", result.Result);
        Assert.Equal(1, client.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count);
        Assert.Single(asp.Policies);
        Assert.Equal("ExecutePolicy", asp.Policies[0]);
    }

    [Fact]
    public async Task BareExecute_AspAuthorize_Denied_ReturnsNull_StillCrossesTheWireOnce()
    {
        var (client, asp) = ScopesWithAspAuthorize(deny: true);
        var factory = client.ServiceProvider.GetRequiredService<IClassExecAspAuthFactory>();

        var result = await factory.Run("hello");

        Assert.Null(result);
        Assert.Equal(1, client.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count);
        Assert.Single(asp.Policies);
    }

    #endregion
}
