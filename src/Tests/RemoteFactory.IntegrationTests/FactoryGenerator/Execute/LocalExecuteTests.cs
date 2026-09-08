using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.IntegrationTests.Shared;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Execute;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Execute;

/// <summary>
/// Integration tests for static [Execute] WITHOUT [Remote] (EXRM-001, AC-1 / AC-2).
/// </summary>
/// <remarks>
/// Pins the new contract from the client side:
/// - A bare [Execute] runs on the client, resolves [Service] parameters from client DI, and makes
///   no remote request (<see cref="RemoteCallCounter"/> stays at zero).
/// - A bare [Execute] taking a server-only service fails on the client at call time with a
///   service-resolution error, still with no remote request.
/// - A bare delegate resolves and runs in the Remote (client), Logical, and Server containers.
/// - [Remote, Execute] is unchanged: from the client scope it crosses the wire exactly once.
/// - The server handler refuses a crafted remote request naming a bare delegate as if the
///   delegate were unknown, while a [Remote] delegate named the same way is served.
/// </remarks>
public class LocalExecuteTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _serverScope;
    private readonly IServiceScope _localScope;

    public LocalExecuteTests()
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
        var del = _clientScope.ServiceProvider.GetRequiredService<LocalExecuteTarget.RunLocal>();

        var result = await del("hello");

        Assert.Equal("Local: hello", result);
        Assert.Equal(0, ClientRemoteCalls);
    }

    [Fact]
    public async Task BareExecute_ClientScope_ServerOnlyService_FailsAtCallTime_NoRemoteRequest()
    {
        // Registration succeeds — the delegate exists on the client — but invoking it resolves the
        // server-only service from client DI and fails there. Nothing crosses the wire.
        var del = _clientScope.ServiceProvider.GetRequiredService<LocalExecuteTarget.RunLocalServerOnly>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => del("hello"));

        // Specifically the DI resolution failure for the server-only service, not any other
        // InvalidOperationException on the path.
        Assert.Contains(nameof(IServerOnlyService), ex.Message);
        Assert.Equal(0, ClientRemoteCalls);
    }

    [Fact]
    public async Task BareExecute_ServerScope_ServerOnlyService_Resolves()
    {
        // The same bare delegate on the server resolves the server-only service from server DI:
        // "runs wherever it is called".
        var del = _serverScope.ServiceProvider.GetRequiredService<LocalExecuteTarget.RunLocalServerOnly>();

        var result = await del("hello");

        Assert.Equal("LocalServerOnly: ServerOnly: hello", result);
    }

    [Fact]
    public async Task BareExecute_ResolvesAndRuns_InRemoteLogicalAndServerContainers()
    {
        var client = _clientScope.ServiceProvider.GetRequiredService<LocalExecuteTarget.RunLocalNoServices>();
        var local = _localScope.ServiceProvider.GetRequiredService<LocalExecuteTarget.RunLocalNoServices>();
        var server = _serverScope.ServiceProvider.GetRequiredService<LocalExecuteTarget.RunLocalNoServices>();

        Assert.Equal(42, await client(21));
        Assert.Equal(42, await local(21));
        Assert.Equal(42, await server(21));
        Assert.Equal(0, ClientRemoteCalls);
    }

    #endregion

    #region [Remote, Execute] is unchanged

    [Fact]
    public async Task RemoteExecute_ClientScope_CrossesTheWireOnce()
    {
        var del = _clientScope.ServiceProvider.GetRequiredService<LocalExecuteTarget.RunRemote>();

        var result = await del("hello");

        Assert.Equal("Remote: ServerOnly: hello", result);
        Assert.Equal(1, ClientRemoteCalls);
    }

    [Fact]
    public async Task RemoteExecute_LogicalScope_RunsLocally_NoWire()
    {
        // The Logical container has no wire implementation, so the counter is structurally zero;
        // the proof is that the [Remote] delegate resolves and runs there at all — its guarded
        // local registration exists on a server runtime.
        var del = _localScope.ServiceProvider.GetRequiredService<LocalExecuteTarget.RunRemote>();

        var result = await del("hello");

        Assert.Equal("Remote: ServerOnly: hello", result);
        Assert.Equal(0, _localScope.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count);
    }

    #endregion

    #region Wire refusal

    [Fact]
    public async Task Handler_RefusesCraftedRequest_ForBareDelegate_AsUnknownDelegate()
    {
        // No generated code ever sends this request — the client has no remote registration for a
        // bare delegate. A crafted request must be refused exactly like an unknown delegate.
        var wire = _clientScope.ServiceProvider.GetRequiredService<IMakeRemoteDelegateRequest>();

        await Assert.ThrowsAsync<MissingDelegateException>(
            () => wire.ForDelegate<string>(typeof(LocalExecuteTarget.RunLocal), ["hello"]));
    }

    [Fact]
    public async Task Handler_StillServes_RemoteDelegate_NamedTheSameWay()
    {
        var wire = _clientScope.ServiceProvider.GetRequiredService<IMakeRemoteDelegateRequest>();

        var result = await wire.ForDelegate<string>(typeof(LocalExecuteTarget.RunRemote), ["hello"]);

        Assert.Equal("Remote: ServerOnly: hello", result);
    }

    #endregion
}
