using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.Generated.CombinationTargets;
using RemoteFactory.IntegrationTests.TestContainers;

namespace RemoteFactory.IntegrationTests.Combinations;

/// <summary>
/// Behavioral tests for Create operations across all valid combinations.
/// Validates that:
/// - Operation is invoked correctly
/// - Parameters are received correctly
/// - Service injection works
/// - Local and Remote execution modes work
/// </summary>
public class CreateBehaviorTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _localScope;

    public CreateBehaviorTests()
    {
        var scopes = ClientServerContainers.Scopes();
        _clientScope = scopes.client;
        _localScope = scopes.local;
    }

    #region Local Mode - Sync Return (TResult)

    [Fact]
    public void Create_TResult_None_Local_OperationIsCalled()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TResult_None_LocalFactory>();

        var result = factory.CreateOp();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
    }

    [Fact]
    public void Create_TResult_Single_Local_ReceivesParameter()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TResult_Single_LocalFactory>();

        var result = factory.CreateOp(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
    }

    [Fact]
    public void Create_TResult_Multiple_Local_ReceivesAllParameters()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TResult_Multiple_LocalFactory>();

        var result = factory.CreateOp(42, "test");

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.Equal("test", result.ReceivedStringParam);
    }

    [Fact]
    public void Create_TResult_Service_Local_ServiceIsInjected()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TResult_Service_LocalFactory>();

        var result = factory.CreateOp();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public void Create_TResult_Mixed_Local_ReceivesParamAndService()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TResult_Mixed_LocalFactory>();

        var result = factory.CreateOp(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region Local Mode - Async Return (TaskTResult)

    [Fact]
    public async Task Create_TaskTResult_None_Local_OperationIsCalled()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TaskTResult_None_LocalFactory>();

        var result = await factory.CreateOp();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
    }

    [Fact]
    public async Task Create_TaskTResult_Single_Local_ReceivesParameter()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TaskTResult_Single_LocalFactory>();

        var result = await factory.CreateOp(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
    }

    [Fact]
    public async Task Create_TaskTResult_Multiple_Local_ReceivesAllParameters()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TaskTResult_Multiple_LocalFactory>();

        var result = await factory.CreateOp(42, "test");

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.Equal("test", result.ReceivedStringParam);
    }

    [Fact]
    public async Task Create_TaskTResult_Service_Local_ServiceIsInjected()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TaskTResult_Service_LocalFactory>();

        var result = await factory.CreateOp();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Create_TaskTResult_Mixed_Local_ReceivesParamAndService()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TaskTResult_Mixed_LocalFactory>();

        var result = await factory.CreateOp(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region Remote Mode - Sync Return (TResult) - Factory returns Task<T>

    [Fact]
    public async Task Create_TResult_None_Remote_OperationIsCalled()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TResult_None_RemoteFactory>();

        var result = await factory.CreateOp();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
    }

    [Fact]
    public async Task Create_TResult_Single_Remote_ReceivesParameter()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TResult_Single_RemoteFactory>();

        var result = await factory.CreateOp(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
    }

    [Fact]
    public async Task Create_TResult_Multiple_Remote_ReceivesAllParameters()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TResult_Multiple_RemoteFactory>();

        var result = await factory.CreateOp(42, "test");

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.Equal("test", result.ReceivedStringParam);
    }

    [Fact]
    public async Task Create_TResult_Service_Remote_ServiceIsInjected()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TResult_Service_RemoteFactory>();

        var result = await factory.CreateOp();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Create_TResult_Mixed_Remote_ReceivesParamAndService()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TResult_Mixed_RemoteFactory>();

        var result = await factory.CreateOp(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region Remote Mode - Async Return (TaskTResult)

    [Fact]
    public async Task Create_TaskTResult_None_Remote_OperationIsCalled()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TaskTResult_None_RemoteFactory>();

        var result = await factory.CreateOp();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
    }

    [Fact]
    public async Task Create_TaskTResult_Single_Remote_ReceivesParameter()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TaskTResult_Single_RemoteFactory>();

        var result = await factory.CreateOp(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
    }

    [Fact]
    public async Task Create_TaskTResult_Multiple_Remote_ReceivesAllParameters()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TaskTResult_Multiple_RemoteFactory>();

        var result = await factory.CreateOp(42, "test");

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.Equal("test", result.ReceivedStringParam);
    }

    [Fact]
    public async Task Create_TaskTResult_Service_Remote_ServiceIsInjected()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TaskTResult_Service_RemoteFactory>();

        var result = await factory.CreateOp();

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Create_TaskTResult_Mixed_Remote_ReceivesParamAndService()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TaskTResult_Mixed_RemoteFactory>();

        var result = await factory.CreateOp(42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public void Create_TResult_CancellationToken_Local_ReceivesToken()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TResult_CancellationToken_LocalFactory>();

        var result = factory.CreateOp(42, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.CancellationTokenReceived);
    }

    [Fact]
    public async Task Create_TResult_CancellationToken_Remote_ReceivesToken()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TResult_CancellationToken_RemoteFactory>();

        var result = await factory.CreateOp(42, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.CancellationTokenReceived);
    }

    [Fact]
    public async Task Create_TaskTResult_CancellationToken_Local_ReceivesToken()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TaskTResult_CancellationToken_LocalFactory>();

        var result = await factory.CreateOp(42, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.CancellationTokenReceived);
    }

    [Fact]
    public async Task Create_TaskTResult_CancellationToken_Remote_ReceivesToken()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Create_Static_TaskTResult_CancellationToken_RemoteFactory>();

        var result = await factory.CreateOp(42, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.CancellationTokenReceived);
    }

    #endregion
}
