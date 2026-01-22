using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Read;

namespace RemoteFactory.UnitTests.FactoryGenerator.Read;

/// <summary>
/// Unit tests for [Remote][Fetch] factory methods executed in Server mode.
/// These tests verify that methods with [Remote] attribute execute correctly locally when in Server mode.
/// All generated Fetch methods return Task because [Remote] makes all operations async.
/// </summary>
/// <remarks>
/// Test naming convention: Fetch_{ReturnType}_{ParameterVariation}_ExecutesInServerMode
/// The generated factory interface has 24 Fetch methods (combinations of return type and parameters).
/// </remarks>
public class RemoteFetchServerModeTests : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly IRemoteFetchTargetFactory _factory;

    public RemoteFetchServerModeTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
        _factory = _provider.GetRequiredService<IRemoteFetchTargetFactory>();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    #region FetchVoid - No Params

    [Fact]
    public async Task Fetch_Void_NoParams_ExecutesInServerMode()
    {
        var result = await _factory.FetchVoid();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
    }

    #endregion

    #region FetchVoidParam - With Param

    [Fact]
    public async Task Fetch_Void_Param_ExecutesWithParameter()
    {
        var result = await _factory.FetchVoidParam(42);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    #endregion

    #region FetchVoidDep - With Service

    [Fact]
    public async Task Fetch_Void_Service_ExecutesWithService()
    {
        var result = await _factory.FetchVoidDep();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region FetchVoidParamDep - With Param and Service

    [Fact]
    public async Task Fetch_Void_ParamService_ExecutesWithBoth()
    {
        var result = await _factory.FetchVoidParamDep(42);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(42, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region FetchBoolTrue - Bool Return True, No Params

    [Fact]
    public async Task Fetch_BoolTrue_NoParams_ReturnsEntity()
    {
        var result = await _factory.FetchBoolTrue();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
    }

    #endregion

    #region FetchBoolFalse - Bool Return False, No Params

    [Fact]
    public async Task Fetch_BoolFalse_NoParams_ReturnsNull()
    {
        var result = await _factory.FetchBoolFalse();

        Assert.Null(result);
    }

    #endregion

    #region FetchBoolTrueParam - Bool True with Param

    [Fact]
    public async Task Fetch_BoolTrue_Param_ReturnsEntityWithParameter()
    {
        var result = await _factory.FetchBoolTrueParam(42);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    #endregion

    #region FetchBoolFalseParam - Bool False with Param

    [Fact]
    public async Task Fetch_BoolFalse_Param_ReturnsNull()
    {
        var result = await _factory.FetchBoolFalseParam(42);

        Assert.Null(result);
    }

    #endregion

    #region FetchBoolTrueDep - Bool True with Service

    [Fact]
    public async Task Fetch_BoolTrue_Service_ReturnsEntityWithService()
    {
        var result = await _factory.FetchBoolTrueDep();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region FetchBoolFalseDep - Bool False with Service

    [Fact]
    public async Task Fetch_BoolFalse_Service_ReturnsNull()
    {
        var result = await _factory.FetchBoolFalseDep();

        Assert.Null(result);
    }

    #endregion

    #region FetchBoolTrueParamDep - Bool True with Param and Service

    [Fact]
    public async Task Fetch_BoolTrue_ParamService_ReturnsEntityWithBoth()
    {
        var result = await _factory.FetchBoolTrueParamDep(42);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(42, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region FetchBoolFalseParamDep - Bool False with Param and Service

    [Fact]
    public async Task Fetch_BoolFalse_ParamService_ReturnsNull()
    {
        var result = await _factory.FetchBoolFalseParamDep(42);

        Assert.Null(result);
    }

    #endregion

    #region FetchTask - Task Return, No Params

    [Fact]
    public async Task Fetch_Task_NoParams_ExecutesInServerMode()
    {
        var result = await _factory.FetchTask();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
    }

    #endregion

    #region FetchTaskParam - Task with Param

    [Fact]
    public async Task Fetch_Task_Param_ExecutesWithParameter()
    {
        var result = await _factory.FetchTaskParam(42);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    #endregion

    #region FetchTaskDep - Task with Service

    [Fact]
    public async Task Fetch_Task_Service_ExecutesWithService()
    {
        var result = await _factory.FetchTaskDep();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region FetchTaskParamDep - Task with Param and Service

    [Fact]
    public async Task Fetch_Task_ParamService_ExecutesWithBoth()
    {
        var result = await _factory.FetchTaskParamDep(42);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(42, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region FetchTaskBoolTrue - Task<bool> True, No Params

    [Fact]
    public async Task Fetch_TaskBoolTrue_NoParams_ReturnsEntity()
    {
        var result = await _factory.FetchTaskBoolTrue();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
    }

    #endregion

    #region FetchTaskBoolFalse - Task<bool> False, No Params

    [Fact]
    public async Task Fetch_TaskBoolFalse_NoParams_ReturnsNull()
    {
        var result = await _factory.FetchTaskBoolFalse();

        Assert.Null(result);
    }

    #endregion

    #region FetchTaskBoolTrueParam - Task<bool> True with Param

    [Fact]
    public async Task Fetch_TaskBoolTrue_Param_ReturnsEntityWithParameter()
    {
        var result = await _factory.FetchTaskBoolTrueParam(42);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    #endregion

    #region FetchTaskBoolFalseParam - Task<bool> False with Param

    [Fact]
    public async Task Fetch_TaskBoolFalse_Param_ReturnsNull()
    {
        var result = await _factory.FetchTaskBoolFalseParam(42);

        Assert.Null(result);
    }

    #endregion

    #region FetchTaskBoolTrueDep - Task<bool> True with Service

    [Fact]
    public async Task Fetch_TaskBoolTrue_Service_ReturnsEntityWithService()
    {
        var result = await _factory.FetchTaskBoolTrueDep();

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region FetchTaskBoolFalseDep - Task<bool> False with Service

    [Fact]
    public async Task Fetch_TaskBoolFalse_Service_ReturnsNull()
    {
        var result = await _factory.FetchTaskBoolFalseDep();

        Assert.Null(result);
    }

    #endregion

    #region FetchTaskBoolTrueParamDep - Task<bool> True with Param and Service

    [Fact]
    public async Task Fetch_TaskBoolTrue_ParamService_ReturnsEntityWithBoth()
    {
        var result = await _factory.FetchTaskBoolTrueParamDep(42);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(42, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region FetchTaskBoolFalseParamDep - Task<bool> False with Param and Service

    [Fact]
    public async Task Fetch_TaskBoolFalse_ParamService_ReturnsNull()
    {
        var result = await _factory.FetchTaskBoolFalseParamDep(42);

        Assert.Null(result);
    }

    #endregion
}
