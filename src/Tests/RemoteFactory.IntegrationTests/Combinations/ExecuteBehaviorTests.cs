using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.Generated.CombinationTargets;
using RemoteFactory.IntegrationTests.TestContainers;

namespace RemoteFactory.IntegrationTests.Combinations;

/// <summary>
/// Behavioral tests for Execute operations across all valid combinations.
/// Execute operations are always remote and use static classes with delegate resolution.
/// Validates that:
/// - Operation is invoked correctly via delegate resolution
/// - Parameters are received correctly
/// - Service injection works
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

    #region Remote Mode (Execute is always remote)

    [Fact]
    public async Task Execute_TaskTResult_None_Remote_OperationIsCalled()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_None_Remote.Op>();

        var result = await op();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
    }

    [Fact]
    public async Task Execute_TaskTResult_Single_Remote_ReceivesParameter()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Single_Remote.Op>();

        var result = await op(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
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
    }

    [Fact]
    public async Task Execute_TaskTResult_Service_Remote_ServiceIsInjected()
    {
        var op = _clientScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Service_Remote.Op>();

        var result = await op();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
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
    }

    #endregion

    #region Local/Server Mode (Execute works in logical mode too)

    [Fact]
    public async Task Execute_TaskTResult_None_Local_OperationIsCalled()
    {
        var op = _localScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_None_Remote.Op>();

        var result = await op();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
    }

    [Fact]
    public async Task Execute_TaskTResult_Single_Local_ReceivesParameter()
    {
        var op = _localScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Single_Remote.Op>();

        var result = await op(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
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
    }

    [Fact]
    public async Task Execute_TaskTResult_Service_Local_ServiceIsInjected()
    {
        var op = _localScope.ServiceProvider.GetRequiredService<Comb_Execute_Static_TaskTResult_Service_Remote.Op>();

        var result = await op();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
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
    }

    #endregion
}
