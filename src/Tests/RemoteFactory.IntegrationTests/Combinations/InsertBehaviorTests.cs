using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.Generated.CombinationTargets;
using RemoteFactory.IntegrationTests.TestContainers;

namespace RemoteFactory.IntegrationTests.Combinations;

/// <summary>
/// Behavioral tests for Insert operations across all valid combinations.
/// Validates that:
/// - Insert operation is invoked via SaveOp when IsNew is true
/// - Parameters are received correctly
/// - Service injection works
/// - Local and Remote execution modes work
/// </summary>
public class InsertBehaviorTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _localScope;

    public InsertBehaviorTests()
    {
        var scopes = ClientServerContainers.Scopes();
        _clientScope = scopes.client;
        _localScope = scopes.local;
    }

    #region Local Mode - Sync Return (Void)

    [Fact]
    public void Insert_Void_None_Local_OperationIsCalled()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Void_None_LocalFactory>();
        var target = factory.CreateOp();
        Assert.True(target.IsNew);

        var result = factory.SaveOp(target);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal("Insert", result.LastOperationCalled);
    }

    [Fact]
    public void Insert_Void_Single_Local_ReceivesParameter()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Void_Single_LocalFactory>();
        var target = factory.CreateOp();

        var result = factory.SaveOp(target, 42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
    }

    [Fact]
    public void Insert_Void_Service_Local_ServiceIsInjected()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Void_Service_LocalFactory>();
        var target = factory.CreateOp();

        var result = factory.SaveOp(target);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public void Insert_Void_Mixed_Local_ReceivesParamAndService()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Void_Mixed_LocalFactory>();
        var target = factory.CreateOp();

        var result = factory.SaveOp(target, 42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region Local Mode - Async Return (Task)

    [Fact]
    public async Task Insert_Task_None_Local_OperationIsCalled()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Task_None_LocalFactory>();
        var target = factory.CreateOp();

        var result = await factory.SaveOp(target);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal("Insert", result.LastOperationCalled);
    }

    [Fact]
    public async Task Insert_Task_Single_Local_ReceivesParameter()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Task_Single_LocalFactory>();
        var target = factory.CreateOp();

        var result = await factory.SaveOp(target, 42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
    }

    [Fact]
    public async Task Insert_Task_Service_Local_ServiceIsInjected()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Task_Service_LocalFactory>();
        var target = factory.CreateOp();

        var result = await factory.SaveOp(target);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Insert_Task_Mixed_Local_ReceivesParamAndService()
    {
        var factory = _localScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Task_Mixed_LocalFactory>();
        var target = factory.CreateOp();

        var result = await factory.SaveOp(target, 42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region Remote Mode - Sync Return (Void) - Factory returns Task<T>

    [Fact]
    public async Task Insert_Void_None_Remote_OperationIsCalled()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Void_None_RemoteFactory>();
        var target = factory.CreateOp();
        Assert.True(target.IsNew);

        var result = await factory.SaveOp(target);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal("Insert", result.LastOperationCalled);
    }

    [Fact]
    public async Task Insert_Void_Single_Remote_ReceivesParameter()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Void_Single_RemoteFactory>();
        var target = factory.CreateOp();

        var result = await factory.SaveOp(target, 42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
    }

    [Fact]
    public async Task Insert_Void_Service_Remote_ServiceIsInjected()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Void_Service_RemoteFactory>();
        var target = factory.CreateOp();

        var result = await factory.SaveOp(target);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Insert_Void_Mixed_Remote_ReceivesParamAndService()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Void_Mixed_RemoteFactory>();
        var target = factory.CreateOp();

        var result = await factory.SaveOp(target, 42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region Remote Mode - Async Return (Task)

    [Fact]
    public async Task Insert_Task_None_Remote_OperationIsCalled()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Task_None_RemoteFactory>();
        var target = factory.CreateOp();

        var result = await factory.SaveOp(target);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal("Insert", result.LastOperationCalled);
    }

    [Fact]
    public async Task Insert_Task_Single_Remote_ReceivesParameter()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Task_Single_RemoteFactory>();
        var target = factory.CreateOp();

        var result = await factory.SaveOp(target, 42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
    }

    [Fact]
    public async Task Insert_Task_Service_Remote_ServiceIsInjected()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Task_Service_RemoteFactory>();
        var target = factory.CreateOp();

        var result = await factory.SaveOp(target);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Insert_Task_Mixed_Remote_ReceivesParamAndService()
    {
        var factory = _clientScope.ServiceProvider.GetRequiredService<IComb_Insert_Instance_Task_Mixed_RemoteFactory>();
        var target = factory.CreateOp();

        var result = await factory.SaveOp(target, 42);

        Assert.NotNull(result);
        Assert.True(result.OperationCalled);
        Assert.Equal(42, result.ReceivedIntParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion
}
