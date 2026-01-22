using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Write;

/// <summary>
/// Test target for [Remote][Insert]/[Update]/[Delete] operations with all method signature variations.
/// This mirrors the original RemoteWriteObject in FactoryGeneratorTests for coverage parity.
/// </summary>
/// <remarks>
/// Method naming convention: {Operation}{ReturnType}[Param][Dep]
/// - Operation: Insert, Update, Delete
/// - ReturnType: Void, Bool, Task, TaskBool
/// - Param: optional int? parameter
/// - Dep: optional [Service] dependency
///
/// Return values for bool methods:
/// - True suffix = returns true (entity returned)
/// - False suffix = returns false (null returned)
/// </remarks>
[Factory]
public partial class RemoteWriteTarget : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    // Tracking properties
    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public int? ReceivedParam { get; set; }
    public bool ServiceWasInjected { get; set; }

    #region Insert - Void Returns

    [Insert]
    [Remote]
    public void InsertVoid()
    {
        InsertCalled = true;
    }

    [Insert]
    [Remote]
    public void InsertVoidParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
    }

    [Insert]
    [Remote]
    public void InsertVoidDep([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
    }

    [Insert]
    [Remote]
    public void InsertVoidParamDep(int? param, [Service] IService service)
    {
        InsertCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
    }

    #endregion

    #region Insert - Bool Returns

    [Insert]
    [Remote]
    public bool InsertBoolTrue()
    {
        InsertCalled = true;
        return true;
    }

    [Insert]
    [Remote]
    public bool InsertBoolFalse()
    {
        InsertCalled = true;
        return false;
    }

    [Insert]
    [Remote]
    public bool InsertBoolTrueParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
        return true;
    }

    [Insert]
    [Remote]
    public bool InsertBoolFalseParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
        return false;
    }

    [Insert]
    [Remote]
    public bool InsertBoolTrueDep([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
        return true;
    }

    [Insert]
    [Remote]
    public bool InsertBoolFalseDep([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
        return false;
    }

    [Insert]
    [Remote]
    public bool InsertBoolTrueParamDep(int? param, [Service] IService service)
    {
        InsertCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return true;
    }

    [Insert]
    [Remote]
    public bool InsertBoolFalseParamDep(int? param, [Service] IService service)
    {
        InsertCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return false;
    }

    #endregion

    #region Insert - Task Returns

    [Insert]
    [Remote]
    public Task InsertTask()
    {
        InsertCalled = true;
        return Task.CompletedTask;
    }

    [Insert]
    [Remote]
    public Task InsertTaskParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
        return Task.CompletedTask;
    }

    [Insert]
    [Remote]
    public Task InsertTaskDep([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    [Insert]
    [Remote]
    public Task InsertTaskParamDep(int? param, [Service] IService service)
    {
        InsertCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    #endregion

    #region Insert - Task<bool> Returns

    [Insert]
    [Remote]
    public Task<bool> InsertTaskBoolTrue()
    {
        InsertCalled = true;
        return Task.FromResult(true);
    }

    [Insert]
    [Remote]
    public Task<bool> InsertTaskBoolFalse()
    {
        InsertCalled = true;
        return Task.FromResult(false);
    }

    [Insert]
    [Remote]
    public Task<bool> InsertTaskBoolTrueParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
        return Task.FromResult(true);
    }

    [Insert]
    [Remote]
    public Task<bool> InsertTaskBoolFalseParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
        return Task.FromResult(false);
    }

    [Insert]
    [Remote]
    public Task<bool> InsertTaskBoolTrueDep([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Insert]
    [Remote]
    public Task<bool> InsertTaskBoolFalseDep([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    [Insert]
    [Remote]
    public Task<bool> InsertTaskBoolTrueParamDep(int? param, [Service] IService service)
    {
        InsertCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Insert]
    [Remote]
    public Task<bool> InsertTaskBoolFalseParamDep(int? param, [Service] IService service)
    {
        InsertCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    #endregion

    #region Update - Void Returns

    [Update]
    [Remote]
    public void UpdateVoid()
    {
        UpdateCalled = true;
    }

    [Update]
    [Remote]
    public void UpdateVoidParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
    }

    [Update]
    [Remote]
    public void UpdateVoidDep([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
    }

    [Update]
    [Remote]
    public void UpdateVoidParamDep(int? param, [Service] IService service)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
    }

    #endregion

    #region Update - Bool Returns

    [Update]
    [Remote]
    public bool UpdateBoolTrue()
    {
        UpdateCalled = true;
        return true;
    }

    [Update]
    [Remote]
    public bool UpdateBoolFalse()
    {
        UpdateCalled = true;
        return false;
    }

    [Update]
    [Remote]
    public bool UpdateBoolTrueParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        return true;
    }

    [Update]
    [Remote]
    public bool UpdateBoolFalseParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        return false;
    }

    [Update]
    [Remote]
    public bool UpdateBoolTrueDep([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
        return true;
    }

    [Update]
    [Remote]
    public bool UpdateBoolFalseDep([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
        return false;
    }

    [Update]
    [Remote]
    public bool UpdateBoolTrueParamDep(int? param, [Service] IService service)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return true;
    }

    [Update]
    [Remote]
    public bool UpdateBoolFalseParamDep(int? param, [Service] IService service)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return false;
    }

    #endregion

    #region Update - Task Returns

    [Update]
    [Remote]
    public Task UpdateTask()
    {
        UpdateCalled = true;
        return Task.CompletedTask;
    }

    [Update]
    [Remote]
    public Task UpdateTaskParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        return Task.CompletedTask;
    }

    [Update]
    [Remote]
    public Task UpdateTaskDep([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    [Update]
    [Remote]
    public Task UpdateTaskParamDep(int? param, [Service] IService service)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    #endregion

    #region Update - Task<bool> Returns

    [Update]
    [Remote]
    public Task<bool> UpdateTaskBoolTrue()
    {
        UpdateCalled = true;
        return Task.FromResult(true);
    }

    [Update]
    [Remote]
    public Task<bool> UpdateTaskBoolFalse()
    {
        UpdateCalled = true;
        return Task.FromResult(false);
    }

    [Update]
    [Remote]
    public Task<bool> UpdateTaskBoolTrueParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        return Task.FromResult(true);
    }

    [Update]
    [Remote]
    public Task<bool> UpdateTaskBoolFalseParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        return Task.FromResult(false);
    }

    [Update]
    [Remote]
    public Task<bool> UpdateTaskBoolTrueDep([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Update]
    [Remote]
    public Task<bool> UpdateTaskBoolFalseDep([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    [Update]
    [Remote]
    public Task<bool> UpdateTaskBoolTrueParamDep(int? param, [Service] IService service)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Update]
    [Remote]
    public Task<bool> UpdateTaskBoolFalseParamDep(int? param, [Service] IService service)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    #endregion

    #region Delete - Void Returns

    [Delete]
    [Remote]
    public void DeleteVoid()
    {
        DeleteCalled = true;
    }

    [Delete]
    [Remote]
    public void DeleteVoidParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
    }

    [Delete]
    [Remote]
    public void DeleteVoidDep([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
    }

    [Delete]
    [Remote]
    public void DeleteVoidParamDep(int? param, [Service] IService service)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
    }

    #endregion

    #region Delete - Bool Returns

    [Delete]
    [Remote]
    public bool DeleteBoolTrue()
    {
        DeleteCalled = true;
        return true;
    }

    [Delete]
    [Remote]
    public bool DeleteBoolFalse()
    {
        DeleteCalled = true;
        return false;
    }

    [Delete]
    [Remote]
    public bool DeleteBoolTrueParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        return true;
    }

    [Delete]
    [Remote]
    public bool DeleteBoolFalseParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        return false;
    }

    [Delete]
    [Remote]
    public bool DeleteBoolTrueDep([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
        return true;
    }

    [Delete]
    [Remote]
    public bool DeleteBoolFalseDep([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
        return false;
    }

    [Delete]
    [Remote]
    public bool DeleteBoolTrueParamDep(int? param, [Service] IService service)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return true;
    }

    [Delete]
    [Remote]
    public bool DeleteBoolFalseParamDep(int? param, [Service] IService service)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return false;
    }

    #endregion

    #region Delete - Task Returns

    [Delete]
    [Remote]
    public Task DeleteTask()
    {
        DeleteCalled = true;
        return Task.CompletedTask;
    }

    [Delete]
    [Remote]
    public Task DeleteTaskParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        return Task.CompletedTask;
    }

    [Delete]
    [Remote]
    public Task DeleteTaskDep([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    [Delete]
    [Remote]
    public Task DeleteTaskParamDep(int? param, [Service] IService service)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    #endregion

    #region Delete - Task<bool> Returns

    [Delete]
    [Remote]
    public Task<bool> DeleteTaskBoolTrue()
    {
        DeleteCalled = true;
        return Task.FromResult(true);
    }

    [Delete]
    [Remote]
    public Task<bool> DeleteTaskBoolFalse()
    {
        DeleteCalled = true;
        return Task.FromResult(false);
    }

    [Delete]
    [Remote]
    public Task<bool> DeleteTaskBoolTrueParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        return Task.FromResult(true);
    }

    [Delete]
    [Remote]
    public Task<bool> DeleteTaskBoolFalseParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        return Task.FromResult(false);
    }

    [Delete]
    [Remote]
    public Task<bool> DeleteTaskBoolTrueDep([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Delete]
    [Remote]
    public Task<bool> DeleteTaskBoolFalseDep([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    [Delete]
    [Remote]
    public Task<bool> DeleteTaskBoolTrueParamDep(int? param, [Service] IService service)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Delete]
    [Remote]
    public Task<bool> DeleteTaskBoolFalseParamDep(int? param, [Service] IService service)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    #endregion
}
