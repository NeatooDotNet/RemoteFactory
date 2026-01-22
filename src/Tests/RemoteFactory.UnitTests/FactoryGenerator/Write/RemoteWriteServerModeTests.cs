using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Write;

namespace RemoteFactory.UnitTests.FactoryGenerator.Write;

/// <summary>
/// Unit tests for [Remote][Insert]/[Update]/[Delete] factory methods executed in Server mode.
/// These tests verify that methods with [Remote] attribute execute correctly locally when in Server mode.
/// All generated Save methods return Task because [Remote] makes all operations async.
/// </summary>
/// <remarks>
/// Test naming convention: {Operation}_{ReturnType}_{ParameterVariation}_ExecutesInServerMode
/// where Operation is Insert/Update/Delete, ReturnType is Void/Bool/Task/TaskBool, and
/// ParameterVariation is NoParams/Param/Service/ParamService.
///
/// The generated factory interface has 24 Save methods (combinations of return type and parameters).
/// Each Save method routes to Insert/Update/Delete based on entity state, so we test all 72 paths
/// (24 methods x 3 operations).
/// </remarks>
public class RemoteWriteServerModeTests : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly IRemoteWriteTargetFactory _factory;

    public RemoteWriteServerModeTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
        _factory = _provider.GetRequiredService<IRemoteWriteTargetFactory>();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    #region SaveVoid - No Params (Insert/Update/Delete)

    [Fact]
    public async Task Insert_Void_NoParams_ExecutesInServerMode()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveVoid(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.False(result.UpdateCalled);
        Assert.False(result.DeleteCalled);
    }

    [Fact]
    public async Task Update_Void_NoParams_ExecutesInServerMode()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveVoid(entity);

        Assert.NotNull(result);
        Assert.False(result.InsertCalled);
        Assert.True(result.UpdateCalled);
        Assert.False(result.DeleteCalled);
    }

    [Fact]
    public async Task Delete_Void_NoParams_ExecutesInServerMode()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveVoid(entity);

        Assert.NotNull(result);
        Assert.False(result.InsertCalled);
        Assert.False(result.UpdateCalled);
        Assert.True(result.DeleteCalled);
    }

    #endregion

    #region SaveVoidDep - Service Only (Insert/Update/Delete)

    [Fact]
    public async Task Insert_Void_Service_ExecutesInServerMode()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveVoidDep(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Update_Void_Service_ExecutesInServerMode()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveVoidDep(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Delete_Void_Service_ExecutesInServerMode()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveVoidDep(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region SaveBoolTrue - Bool Return True, No Params (Insert/Update/Delete)

    [Fact]
    public async Task Insert_BoolTrue_NoParams_ReturnsEntity()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public async Task Update_BoolTrue_NoParams_ReturnsEntity()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
    }

    [Fact]
    public async Task Delete_BoolTrue_NoParams_ReturnsEntity()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
    }

    #endregion

    #region SaveBoolFalse - Bool Return False, No Params (Insert/Update/Delete)

    [Fact]
    public async Task Insert_BoolFalse_NoParams_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveBoolFalse(entity);

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_BoolFalse_NoParams_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveBoolFalse(entity);

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_BoolFalse_NoParams_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveBoolFalse(entity);

        Assert.Null(result);
    }

    #endregion

    #region SaveBoolTrueDep - Bool True with Service (Insert/Update/Delete)

    [Fact]
    public async Task Insert_BoolTrue_Service_ReturnsEntityWithService()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveBoolTrueDep(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Update_BoolTrue_Service_ReturnsEntityWithService()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveBoolTrueDep(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Delete_BoolTrue_Service_ReturnsEntityWithService()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveBoolTrueDep(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region SaveBoolFalseDep - Bool False with Service (Insert/Update/Delete)

    [Fact]
    public async Task Insert_BoolFalse_Service_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveBoolFalseDep(entity);

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_BoolFalse_Service_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveBoolFalseDep(entity);

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_BoolFalse_Service_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveBoolFalseDep(entity);

        Assert.Null(result);
    }

    #endregion

    #region SaveTask - Task Return, No Params (Insert/Update/Delete)

    [Fact]
    public async Task Insert_Task_NoParams_ExecutesInServerMode()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveTask(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public async Task Update_Task_NoParams_ExecutesInServerMode()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveTask(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
    }

    [Fact]
    public async Task Delete_Task_NoParams_ExecutesInServerMode()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveTask(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
    }

    #endregion

    #region SaveTaskDep - Task Return with Service (Insert/Update/Delete)

    [Fact]
    public async Task Insert_Task_Service_ExecutesInServerMode()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveTaskDep(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Update_Task_Service_ExecutesInServerMode()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveTaskDep(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Delete_Task_Service_ExecutesInServerMode()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveTaskDep(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region SaveTaskBoolTrue - Task<bool> True, No Params (Insert/Update/Delete)

    [Fact]
    public async Task Insert_TaskBoolTrue_NoParams_ReturnsEntity()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveTaskBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    [Fact]
    public async Task Update_TaskBoolTrue_NoParams_ReturnsEntity()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveTaskBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
    }

    [Fact]
    public async Task Delete_TaskBoolTrue_NoParams_ReturnsEntity()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveTaskBoolTrue(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
    }

    #endregion

    #region SaveTaskBoolFalse - Task<bool> False, No Params (Insert/Update/Delete)

    [Fact]
    public async Task Insert_TaskBoolFalse_NoParams_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveTaskBoolFalse(entity);

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_TaskBoolFalse_NoParams_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveTaskBoolFalse(entity);

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_TaskBoolFalse_NoParams_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveTaskBoolFalse(entity);

        Assert.Null(result);
    }

    #endregion

    #region SaveTaskBoolTrueDep - Task<bool> True with Service (Insert/Update/Delete)

    [Fact]
    public async Task Insert_TaskBoolTrue_Service_ReturnsEntityWithService()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveTaskBoolTrueDep(entity);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Update_TaskBoolTrue_Service_ReturnsEntityWithService()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveTaskBoolTrueDep(entity);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Delete_TaskBoolTrue_Service_ReturnsEntityWithService()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveTaskBoolTrueDep(entity);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region SaveTaskBoolFalseDep - Task<bool> False with Service (Insert/Update/Delete)

    [Fact]
    public async Task Insert_TaskBoolFalse_Service_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveTaskBoolFalseDep(entity);

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_TaskBoolFalse_Service_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveTaskBoolFalseDep(entity);

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_TaskBoolFalse_Service_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveTaskBoolFalseDep(entity);

        Assert.Null(result);
    }

    #endregion

    #region SaveVoidParam - Void with Param (Insert/Update/Delete)

    [Fact]
    public async Task Insert_Void_Param_ExecutesWithParameter()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveVoidParam(entity, 42);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    [Fact]
    public async Task Update_Void_Param_ExecutesWithParameter()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveVoidParam(entity, 99);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal(99, result.ReceivedParam);
    }

    [Fact]
    public async Task Delete_Void_Param_ExecutesWithParameter()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveVoidParam(entity, 123);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal(123, result.ReceivedParam);
    }

    #endregion

    #region SaveVoidParamDep - Void with Param and Service (Insert/Update/Delete)

    [Fact]
    public async Task Insert_Void_ParamService_ExecutesWithBoth()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveVoidParamDep(entity, 42);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal(42, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Update_Void_ParamService_ExecutesWithBoth()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveVoidParamDep(entity, 99);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal(99, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Delete_Void_ParamService_ExecutesWithBoth()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveVoidParamDep(entity, 123);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal(123, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region SaveBoolTrueParam - Bool True with Param (Insert/Update/Delete)

    [Fact]
    public async Task Insert_BoolTrue_Param_ReturnsEntityWithParameter()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveBoolTrueParam(entity, 42);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    [Fact]
    public async Task Update_BoolTrue_Param_ReturnsEntityWithParameter()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveBoolTrueParam(entity, 99);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal(99, result.ReceivedParam);
    }

    [Fact]
    public async Task Delete_BoolTrue_Param_ReturnsEntityWithParameter()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveBoolTrueParam(entity, 123);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal(123, result.ReceivedParam);
    }

    #endregion

    #region SaveBoolFalseParam - Bool False with Param (Insert/Update/Delete)

    [Fact]
    public async Task Insert_BoolFalse_Param_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveBoolFalseParam(entity, 42);

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_BoolFalse_Param_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveBoolFalseParam(entity, 99);

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_BoolFalse_Param_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveBoolFalseParam(entity, 123);

        Assert.Null(result);
    }

    #endregion

    #region SaveBoolTrueParamDep - Bool True with Param and Service (Insert/Update/Delete)

    [Fact]
    public async Task Insert_BoolTrue_ParamService_ReturnsEntityWithBoth()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveBoolTrueParamDep(entity, 42);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal(42, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Update_BoolTrue_ParamService_ReturnsEntityWithBoth()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveBoolTrueParamDep(entity, 99);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal(99, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Delete_BoolTrue_ParamService_ReturnsEntityWithBoth()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveBoolTrueParamDep(entity, 123);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal(123, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region SaveBoolFalseParamDep - Bool False with Param and Service (Insert/Update/Delete)

    [Fact]
    public async Task Insert_BoolFalse_ParamService_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveBoolFalseParamDep(entity, 42);

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_BoolFalse_ParamService_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveBoolFalseParamDep(entity, 99);

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_BoolFalse_ParamService_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveBoolFalseParamDep(entity, 123);

        Assert.Null(result);
    }

    #endregion

    #region SaveTaskParam - Task with Param (Insert/Update/Delete)

    [Fact]
    public async Task Insert_Task_Param_ExecutesWithParameter()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveTaskParam(entity, 42);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    [Fact]
    public async Task Update_Task_Param_ExecutesWithParameter()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveTaskParam(entity, 99);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal(99, result.ReceivedParam);
    }

    [Fact]
    public async Task Delete_Task_Param_ExecutesWithParameter()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveTaskParam(entity, 123);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal(123, result.ReceivedParam);
    }

    #endregion

    #region SaveTaskParamDep - Task with Param and Service (Insert/Update/Delete)

    [Fact]
    public async Task Insert_Task_ParamService_ExecutesWithBoth()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveTaskParamDep(entity, 42);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal(42, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Update_Task_ParamService_ExecutesWithBoth()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveTaskParamDep(entity, 99);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal(99, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Delete_Task_ParamService_ExecutesWithBoth()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveTaskParamDep(entity, 123);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal(123, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region SaveTaskBoolTrueParam - Task<bool> True with Param (Insert/Update/Delete)

    [Fact]
    public async Task Insert_TaskBoolTrue_Param_ReturnsEntityWithParameter()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveTaskBoolTrueParam(entity, 42);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal(42, result.ReceivedParam);
    }

    [Fact]
    public async Task Update_TaskBoolTrue_Param_ReturnsEntityWithParameter()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveTaskBoolTrueParam(entity, 99);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal(99, result.ReceivedParam);
    }

    [Fact]
    public async Task Delete_TaskBoolTrue_Param_ReturnsEntityWithParameter()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveTaskBoolTrueParam(entity, 123);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal(123, result.ReceivedParam);
    }

    #endregion

    #region SaveTaskBoolFalseParam - Task<bool> False with Param (Insert/Update/Delete)

    [Fact]
    public async Task Insert_TaskBoolFalse_Param_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveTaskBoolFalseParam(entity, 42);

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_TaskBoolFalse_Param_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveTaskBoolFalseParam(entity, 99);

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_TaskBoolFalse_Param_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveTaskBoolFalseParam(entity, 123);

        Assert.Null(result);
    }

    #endregion

    #region SaveTaskBoolTrueParamDep - Task<bool> True with Param and Service (Insert/Update/Delete)

    [Fact]
    public async Task Insert_TaskBoolTrue_ParamService_ReturnsEntityWithBoth()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveTaskBoolTrueParamDep(entity, 42);

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal(42, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Update_TaskBoolTrue_ParamService_ReturnsEntityWithBoth()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveTaskBoolTrueParamDep(entity, 99);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal(99, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    [Fact]
    public async Task Delete_TaskBoolTrue_ParamService_ReturnsEntityWithBoth()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveTaskBoolTrueParamDep(entity, 123);

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal(123, result.ReceivedParam);
        Assert.True(result.ServiceWasInjected);
    }

    #endregion

    #region SaveTaskBoolFalseParamDep - Task<bool> False with Param and Service (Insert/Update/Delete)

    [Fact]
    public async Task Insert_TaskBoolFalse_ParamService_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = true };

        var result = await _factory.SaveTaskBoolFalseParamDep(entity, 42);

        Assert.Null(result);
    }

    [Fact]
    public async Task Update_TaskBoolFalse_ParamService_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsNew = false, IsDeleted = false };

        var result = await _factory.SaveTaskBoolFalseParamDep(entity, 99);

        Assert.Null(result);
    }

    [Fact]
    public async Task Delete_TaskBoolFalse_ParamService_ReturnsNull()
    {
        var entity = new RemoteWriteTarget { IsDeleted = true };

        var result = await _factory.SaveTaskBoolFalseParamDep(entity, 123);

        Assert.Null(result);
    }

    #endregion

    #region Edge Case: New and Deleted

    [Fact]
    public async Task Save_NewAndDeleted_ReturnsNull()
    {
        // When IsNew = true AND IsDeleted = true, should return null (no operation needed)
        var entity = new RemoteWriteTarget { IsNew = true, IsDeleted = true };

        var result = await _factory.SaveVoid(entity);

        // New and deleted means no database operation - just return null
        Assert.Null(result);
    }

    #endregion
}
