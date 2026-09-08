using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.Generated.CombinationTargets;
using RemoteFactory.IntegrationTests.TestContainers;

namespace RemoteFactory.IntegrationTests.Combinations;

/// <summary>
/// Behavioral tests for Execute operations across all valid combinations.
/// Execute obeys [Remote] like every other operation (EXRM-001): the Remote-mode combination
/// targets carry [Remote], so a client-scope call crosses the wire, while a Logical-scope call
/// runs locally. The Local-mode targets (EXRM-002) carry no [Remote] and run on the client
/// itself. Validates that:
/// - Operation is invoked correctly via delegate resolution
/// - Parameters are received correctly
/// - Service injection works
/// - A [Remote] target called from the client scope makes exactly one remote request; the same
///   target from the Logical scope makes none; a bare target from the client scope makes none
/// </summary>
public class ExecuteBehaviorTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _localScope;

    public ExecuteBehaviorTests()
    {
        var scopes = ClientServerContainers.Scopes();
        _clientScope = scopes.client;
        _localScope = scopes.local;
    }

    private int ClientRemoteCalls => _clientScope.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count;
    private int LocalRemoteCalls => _localScope.ServiceProvider.GetRequiredService<RemoteCallCounter>().Count;

    #region Remote Mode ([Remote, Execute] from the client scope crosses the wire)

    [Fact]
    public async Task Execute_TaskTResult_None_Remote_OperationIsCalled()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_None_Remote.Op>();

        var result = await op();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(1, ClientRemoteCalls);
    }

    [Fact]
    public async Task Execute_TaskTResult_Single_Remote_ReceivesParameter()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Single_Remote.Op>();

        var result = await op(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.Equal(1, ClientRemoteCalls);
    }

    [Fact]
    public async Task Execute_TaskTResult_Multiple_Remote_ReceivesAllParameters()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Multiple_Remote.Op>();

        var result = await op(42, "test");

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.Equal("test", result.ReceivedStringParam);
        Assert.Equal(1, ClientRemoteCalls);
    }

    [Fact]
    public async Task Execute_TaskTResult_Service_Remote_ServiceIsInjected()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Service_Remote.Op>();

        var result = await op();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
        Assert.Equal(1, ClientRemoteCalls);
    }

    [Fact]
    public async Task Execute_TaskTResult_Mixed_Remote_ReceivesParamAndService()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Mixed_Remote.Op>();

        var result = await op(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.ServiceWasInjected);
        Assert.Equal(1, ClientRemoteCalls);
    }

    #endregion

    #region Local/Server Mode ([Remote, Execute] from the Logical scope runs locally, no wire)

    // The Logical container registers no wire implementation at all, so LocalRemoteCalls is
    // structurally zero here. What proves local execution in this region is that the delegate
    // resolves and runs in a container that has nothing to send a request through; the counter
    // assertion documents the expectation rather than discriminating it.

    [Fact]
    public async Task Execute_TaskTResult_None_Local_OperationIsCalled()
    {
        var op = _localScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_None_Remote.Op>();

        var result = await op();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(0, LocalRemoteCalls);
    }

    [Fact]
    public async Task Execute_TaskTResult_Single_Local_ReceivesParameter()
    {
        var op = _localScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Single_Remote.Op>();

        var result = await op(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.Equal(0, LocalRemoteCalls);
    }

    [Fact]
    public async Task Execute_TaskTResult_Multiple_Local_ReceivesAllParameters()
    {
        var op = _localScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Multiple_Remote.Op>();

        var result = await op(42, "test");

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.Equal("test", result.ReceivedStringParam);
        Assert.Equal(0, LocalRemoteCalls);
    }

    [Fact]
    public async Task Execute_TaskTResult_Service_Local_ServiceIsInjected()
    {
        var op = _localScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Service_Remote.Op>();

        var result = await op();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
        Assert.Equal(0, LocalRemoteCalls);
    }

    [Fact]
    public async Task Execute_TaskTResult_Mixed_Local_ReceivesParamAndService()
    {
        var op = _localScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Mixed_Remote.Op>();

        var result = await op(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.ServiceWasInjected);
        Assert.Equal(0, LocalRemoteCalls);
    }

    #endregion

    #region Bare [Execute] targets (no [Remote]) from the client scope run locally, no wire

    // These are the discriminating legs: the client scope has a live wire and a counter, so a
    // bare target that resolves and runs there with the counter still at zero proves the
    // delegate never left the client. [Service] parameters come from client DI (IService is
    // mapped there by RegisterMatchingName).

    [Fact]
    public async Task BareExecute_TaskTResult_None_ClientScope_RunsLocally_NoRemoteRequest()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_None_Local.Op>();

        var result = await op();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(0, ClientRemoteCalls);
    }

    [Fact]
    public async Task BareExecute_TaskTResult_Single_ClientScope_ReceivesParameter_NoRemoteRequest()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Single_Local.Op>();

        var result = await op(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.Equal(0, ClientRemoteCalls);
    }

    [Fact]
    public async Task BareExecute_TaskTResult_Multiple_ClientScope_ReceivesAllParameters_NoRemoteRequest()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Multiple_Local.Op>();

        var result = await op(42, "test");

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.Equal("test", result.ReceivedStringParam);
        Assert.Equal(0, ClientRemoteCalls);
    }

    [Fact]
    public async Task BareExecute_TaskTResult_Service_ClientScope_ServiceInjectedFromClientDI_NoRemoteRequest()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Service_Local.Op>();

        var result = await op();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
        Assert.Equal(0, ClientRemoteCalls);
    }

    [Fact]
    public async Task BareExecute_TaskTResult_Mixed_ClientScope_ReceivesParamAndClientService_NoRemoteRequest()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Mixed_Local.Op>();

        var result = await op(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.ServiceWasInjected);
        Assert.Equal(0, ClientRemoteCalls);
    }

    #endregion
}
