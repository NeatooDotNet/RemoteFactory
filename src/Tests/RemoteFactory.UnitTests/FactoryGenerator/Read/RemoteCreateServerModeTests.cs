using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Read;

namespace RemoteFactory.UnitTests.FactoryGenerator.Read;

/// <summary>
/// Unit tests for [Remote][Create] factory methods executed in Server mode.
/// These tests verify that methods with [Remote] attribute execute correctly locally when in Server mode.
/// All generated Create methods return Task because [Remote] makes all operations async.
/// </summary>
/// <remarks>
/// Test naming convention: Create_{ReturnType}_{ParameterVariation}_ExecutesInServerMode
/// The generated factory interface has 24 Create methods (combinations of return type and parameters).
/// </remarks>
public class RemoteCreateServerModeTests : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly IRemoteCreateTargetFactory _factory;

    public RemoteCreateServerModeTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
        _factory = _provider.GetRequiredService<IRemoteCreateTargetFactory>();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    #region CreateVoid - No Params

    [Fact]
    public async Task Create_Void_NoParams_ExecutesInServerMode()
    {
        var result = await _factory.CreateVoid();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    #endregion

    #region CreateVoidParam - With Param

    [Fact]
    public async Task Create_Void_Param_ExecutesWithParameter()
    {
        var result = await _factory.CreateVoidParam(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    #endregion

    #region CreateVoidDep - With Service

    [Fact]
    public async Task Create_Void_Service_ExecutesWithService()
    {
        var result = await _factory.CreateVoidDep();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region CreateVoidParamDep - With Param and Service

    [Fact]
    public async Task Create_Void_ParamService_ExecutesWithBoth()
    {
        var result = await _factory.CreateVoidParamDep(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(42, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region CreateBoolTrue - Bool Return True, No Params

    [Fact]
    public async Task Create_BoolTrue_NoParams_ReturnsEntity()
    {
        var result = await _factory.CreateBoolTrue();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    #endregion

    #region CreateBoolFalse - Bool Return False, No Params

    [Fact]
    public async Task Create_BoolFalse_NoParams_ReturnsNull()
    {
        var result = await _factory.CreateBoolFalse();

        Assert.Null(result);
    }

    #endregion

    #region CreateBoolTrueParam - Bool True with Param

    [Fact]
    public async Task Create_BoolTrue_Param_ReturnsEntityWithParameter()
    {
        var result = await _factory.CreateBoolTrueParam(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    #endregion

    #region CreateBoolFalseParam - Bool False with Param

    [Fact]
    public async Task Create_BoolFalse_Param_ReturnsNull()
    {
        var result = await _factory.CreateBoolFalseParam(42);

        Assert.Null(result);
    }

    #endregion

    #region CreateBoolTrueDep - Bool True with Service

    [Fact]
    public async Task Create_BoolTrue_Service_ReturnsEntityWithService()
    {
        var result = await _factory.CreateBoolTrueDep();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region CreateBoolFalseDep - Bool False with Service

    [Fact]
    public async Task Create_BoolFalse_Service_ReturnsNull()
    {
        var result = await _factory.CreateBoolFalseDep();

        Assert.Null(result);
    }

    #endregion

    #region CreateBoolTrueParamDep - Bool True with Param and Service

    [Fact]
    public async Task Create_BoolTrue_ParamService_ReturnsEntityWithBoth()
    {
        var result = await _factory.CreateBoolTrueParamDep(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(42, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region CreateBoolFalseParamDep - Bool False with Param and Service

    [Fact]
    public async Task Create_BoolFalse_ParamService_ReturnsNull()
    {
        var result = await _factory.CreateBoolFalseParamDep(42);

        Assert.Null(result);
    }

    #endregion

    #region CreateTask - Task Return, No Params

    [Fact]
    public async Task Create_Task_NoParams_ExecutesInServerMode()
    {
        var result = await _factory.CreateTask();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    #endregion

    #region CreateTaskParam - Task with Param

    [Fact]
    public async Task Create_Task_Param_ExecutesWithParameter()
    {
        var result = await _factory.CreateTaskParam(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    #endregion

    #region CreateTaskDep - Task with Service

    [Fact]
    public async Task Create_Task_Service_ExecutesWithService()
    {
        var result = await _factory.CreateTaskDep();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region CreateTaskParamDep - Task with Param and Service

    [Fact]
    public async Task Create_Task_ParamService_ExecutesWithBoth()
    {
        var result = await _factory.CreateTaskParamDep(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(42, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region CreateTaskBoolTrue - Task<bool> True, No Params

    [Fact]
    public async Task Create_TaskBoolTrue_NoParams_ReturnsEntity()
    {
        var result = await _factory.CreateTaskBoolTrue();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    #endregion

    #region CreateTaskBoolFalse - Task<bool> False, No Params

    [Fact]
    public async Task Create_TaskBoolFalse_NoParams_ReturnsNull()
    {
        var result = await _factory.CreateTaskBoolFalse();

        Assert.Null(result);
    }

    #endregion

    #region CreateTaskBoolTrueParam - Task<bool> True with Param

    [Fact]
    public async Task Create_TaskBoolTrue_Param_ReturnsEntityWithParameter()
    {
        var result = await _factory.CreateTaskBoolTrueParam(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    #endregion

    #region CreateTaskBoolFalseParam - Task<bool> False with Param

    [Fact]
    public async Task Create_TaskBoolFalse_Param_ReturnsNull()
    {
        var result = await _factory.CreateTaskBoolFalseParam(42);

        Assert.Null(result);
    }

    #endregion

    #region CreateTaskBoolTrueDep - Task<bool> True with Service

    [Fact]
    public async Task Create_TaskBoolTrue_Service_ReturnsEntityWithService()
    {
        var result = await _factory.CreateTaskBoolTrueDep();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region CreateTaskBoolFalseDep - Task<bool> False with Service

    [Fact]
    public async Task Create_TaskBoolFalse_Service_ReturnsNull()
    {
        var result = await _factory.CreateTaskBoolFalseDep();

        Assert.Null(result);
    }

    #endregion

    #region CreateTaskBoolTrueParamDep - Task<bool> True with Param and Service

    [Fact]
    public async Task Create_TaskBoolTrue_ParamService_ReturnsEntityWithBoth()
    {
        var result = await _factory.CreateTaskBoolTrueParamDep(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(42, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region CreateTaskBoolFalseParamDep - Task<bool> False with Param and Service

    [Fact]
    public async Task Create_TaskBoolFalse_ParamService_ReturnsNull()
    {
        var result = await _factory.CreateTaskBoolFalseParamDep(42);

        Assert.Null(result);
    }

    #endregion
}
