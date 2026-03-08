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
    internal void InsertVoid()
    {
        InsertCalled = true;
    }

    [Insert]
    [Remote]
    internal void InsertVoidParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
    }

    [Insert]
    [Remote]
    internal void InsertVoidDep([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
    }

    [Insert]
    [Remote]
    internal void InsertVoidParamDep(int? param, [Service] IService service)
    {
        InsertCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
    }

    #endregion

    #region Insert - Bool Returns

    [Insert]
    [Remote]
    internal bool InsertBoolTrue()
    {
        InsertCalled = true;
        return true;
    }

    [Insert]
    [Remote]
    internal bool InsertBoolFalse()
    {
        InsertCalled = true;
        return false;
    }

    [Insert]
    [Remote]
    internal bool InsertBoolTrueParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
        return true;
    }

    [Insert]
    [Remote]
    internal bool InsertBoolFalseParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
        return false;
    }

    [Insert]
    [Remote]
    internal bool InsertBoolTrueDep([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
        return true;
    }

    [Insert]
    [Remote]
    internal bool InsertBoolFalseDep([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
        return false;
    }

    [Insert]
    [Remote]
    internal bool InsertBoolTrueParamDep(int? param, [Service] IService service)
    {
        InsertCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return true;
    }

    [Insert]
    [Remote]
    internal bool InsertBoolFalseParamDep(int? param, [Service] IService service)
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
    internal Task InsertTask()
    {
        InsertCalled = true;
        return Task.CompletedTask;
    }

    [Insert]
    [Remote]
    internal Task InsertTaskParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
        return Task.CompletedTask;
    }

    [Insert]
    [Remote]
    internal Task InsertTaskDep([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    [Insert]
    [Remote]
    internal Task InsertTaskParamDep(int? param, [Service] IService service)
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
    internal Task<bool> InsertTaskBoolTrue()
    {
        InsertCalled = true;
        return Task.FromResult(true);
    }

    [Insert]
    [Remote]
    internal Task<bool> InsertTaskBoolFalse()
    {
        InsertCalled = true;
        return Task.FromResult(false);
    }

    [Insert]
    [Remote]
    internal Task<bool> InsertTaskBoolTrueParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
        return Task.FromResult(true);
    }

    [Insert]
    [Remote]
    internal Task<bool> InsertTaskBoolFalseParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
        return Task.FromResult(false);
    }

    [Insert]
    [Remote]
    internal Task<bool> InsertTaskBoolTrueDep([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Insert]
    [Remote]
    internal Task<bool> InsertTaskBoolFalseDep([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    [Insert]
    [Remote]
    internal Task<bool> InsertTaskBoolTrueParamDep(int? param, [Service] IService service)
    {
        InsertCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Insert]
    [Remote]
    internal Task<bool> InsertTaskBoolFalseParamDep(int? param, [Service] IService service)
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
    internal void UpdateVoid()
    {
        UpdateCalled = true;
    }

    [Update]
    [Remote]
    internal void UpdateVoidParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
    }

    [Update]
    [Remote]
    internal void UpdateVoidDep([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
    }

    [Update]
    [Remote]
    internal void UpdateVoidParamDep(int? param, [Service] IService service)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
    }

    #endregion

    #region Update - Bool Returns

    [Update]
    [Remote]
    internal bool UpdateBoolTrue()
    {
        UpdateCalled = true;
        return true;
    }

    [Update]
    [Remote]
    internal bool UpdateBoolFalse()
    {
        UpdateCalled = true;
        return false;
    }

    [Update]
    [Remote]
    internal bool UpdateBoolTrueParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        return true;
    }

    [Update]
    [Remote]
    internal bool UpdateBoolFalseParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        return false;
    }

    [Update]
    [Remote]
    internal bool UpdateBoolTrueDep([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
        return true;
    }

    [Update]
    [Remote]
    internal bool UpdateBoolFalseDep([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
        return false;
    }

    [Update]
    [Remote]
    internal bool UpdateBoolTrueParamDep(int? param, [Service] IService service)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return true;
    }

    [Update]
    [Remote]
    internal bool UpdateBoolFalseParamDep(int? param, [Service] IService service)
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
    internal Task UpdateTask()
    {
        UpdateCalled = true;
        return Task.CompletedTask;
    }

    [Update]
    [Remote]
    internal Task UpdateTaskParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        return Task.CompletedTask;
    }

    [Update]
    [Remote]
    internal Task UpdateTaskDep([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    [Update]
    [Remote]
    internal Task UpdateTaskParamDep(int? param, [Service] IService service)
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
    internal Task<bool> UpdateTaskBoolTrue()
    {
        UpdateCalled = true;
        return Task.FromResult(true);
    }

    [Update]
    [Remote]
    internal Task<bool> UpdateTaskBoolFalse()
    {
        UpdateCalled = true;
        return Task.FromResult(false);
    }

    [Update]
    [Remote]
    internal Task<bool> UpdateTaskBoolTrueParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        return Task.FromResult(true);
    }

    [Update]
    [Remote]
    internal Task<bool> UpdateTaskBoolFalseParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        return Task.FromResult(false);
    }

    [Update]
    [Remote]
    internal Task<bool> UpdateTaskBoolTrueDep([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Update]
    [Remote]
    internal Task<bool> UpdateTaskBoolFalseDep([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    [Update]
    [Remote]
    internal Task<bool> UpdateTaskBoolTrueParamDep(int? param, [Service] IService service)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Update]
    [Remote]
    internal Task<bool> UpdateTaskBoolFalseParamDep(int? param, [Service] IService service)
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
    internal void DeleteVoid()
    {
        DeleteCalled = true;
    }

    [Delete]
    [Remote]
    internal void DeleteVoidParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
    }

    [Delete]
    [Remote]
    internal void DeleteVoidDep([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
    }

    [Delete]
    [Remote]
    internal void DeleteVoidParamDep(int? param, [Service] IService service)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
    }

    #endregion

    #region Delete - Bool Returns

    [Delete]
    [Remote]
    internal bool DeleteBoolTrue()
    {
        DeleteCalled = true;
        return true;
    }

    [Delete]
    [Remote]
    internal bool DeleteBoolFalse()
    {
        DeleteCalled = true;
        return false;
    }

    [Delete]
    [Remote]
    internal bool DeleteBoolTrueParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        return true;
    }

    [Delete]
    [Remote]
    internal bool DeleteBoolFalseParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        return false;
    }

    [Delete]
    [Remote]
    internal bool DeleteBoolTrueDep([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
        return true;
    }

    [Delete]
    [Remote]
    internal bool DeleteBoolFalseDep([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
        return false;
    }

    [Delete]
    [Remote]
    internal bool DeleteBoolTrueParamDep(int? param, [Service] IService service)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return true;
    }

    [Delete]
    [Remote]
    internal bool DeleteBoolFalseParamDep(int? param, [Service] IService service)
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
    internal Task DeleteTask()
    {
        DeleteCalled = true;
        return Task.CompletedTask;
    }

    [Delete]
    [Remote]
    internal Task DeleteTaskParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        return Task.CompletedTask;
    }

    [Delete]
    [Remote]
    internal Task DeleteTaskDep([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    [Delete]
    [Remote]
    internal Task DeleteTaskParamDep(int? param, [Service] IService service)
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
    internal Task<bool> DeleteTaskBoolTrue()
    {
        DeleteCalled = true;
        return Task.FromResult(true);
    }

    [Delete]
    [Remote]
    internal Task<bool> DeleteTaskBoolFalse()
    {
        DeleteCalled = true;
        return Task.FromResult(false);
    }

    [Delete]
    [Remote]
    internal Task<bool> DeleteTaskBoolTrueParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        return Task.FromResult(true);
    }

    [Delete]
    [Remote]
    internal Task<bool> DeleteTaskBoolFalseParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        return Task.FromResult(false);
    }

    [Delete]
    [Remote]
    internal Task<bool> DeleteTaskBoolTrueDep([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Delete]
    [Remote]
    internal Task<bool> DeleteTaskBoolFalseDep([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    [Delete]
    [Remote]
    internal Task<bool> DeleteTaskBoolTrueParamDep(int? param, [Service] IService service)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Delete]
    [Remote]
    internal Task<bool> DeleteTaskBoolFalseParamDep(int? param, [Service] IService service)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    #endregion
}
