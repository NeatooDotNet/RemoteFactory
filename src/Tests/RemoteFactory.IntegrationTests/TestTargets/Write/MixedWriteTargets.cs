using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Write;

/// <summary>
/// Target class testing mixed return types and parameter combinations for write operations.
/// Covers Insert/Update/Delete with void/bool/Task/Task&lt;bool&gt; returns.
/// Some methods have [Remote] attribute for client-server round-trip testing.
/// </summary>
[Factory]
public class MixedWriteTarget : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public int? ReceivedParam { get; set; }
    public bool ServiceInjected { get; set; }

    // ============================================================================
    // INSERT methods
    // ============================================================================

    [Insert]
    public void InsertVoid()
    {
        InsertCalled = true;
    }

    [Insert]
    public bool InsertBoolTrue()
    {
        InsertCalled = true;
        return true;
    }

    [Insert]
    public bool InsertBoolFalse()
    {
        InsertCalled = true;
        return false;
    }

    [Insert]
    public Task InsertTask()
    {
        InsertCalled = true;
        return Task.CompletedTask;
    }

    [Insert]
    public Task<bool> InsertTaskBoolTrue()
    {
        InsertCalled = true;
        return Task.FromResult(true);
    }

    [Insert]
    public Task<bool> InsertTaskBoolFalse()
    {
        InsertCalled = true;
        return Task.FromResult(false);
    }

    [Insert]
    public void InsertVoidParam(int? param)
    {
        InsertCalled = true;
        ReceivedParam = param;
    }

    [Insert]
    public void InsertVoidService([Service] IService service)
    {
        InsertCalled = true;
        ServiceInjected = service != null;
    }

    [Insert]
    public void InsertVoidParamService(int? param, [Service] IService service)
    {
        InsertCalled = true;
        ReceivedParam = param;
        ServiceInjected = service != null;
    }

    // ============================================================================
    // UPDATE methods
    // ============================================================================

    [Update]
    public void UpdateVoid()
    {
        UpdateCalled = true;
    }

    [Update]
    public bool UpdateBoolTrue()
    {
        UpdateCalled = true;
        return true;
    }

    [Update]
    public bool UpdateBoolFalse()
    {
        UpdateCalled = true;
        return false;
    }

    [Update]
    public Task UpdateTask()
    {
        UpdateCalled = true;
        return Task.CompletedTask;
    }

    [Update]
    public Task<bool> UpdateTaskBoolTrue()
    {
        UpdateCalled = true;
        return Task.FromResult(true);
    }

    [Update]
    public Task<bool> UpdateTaskBoolFalse()
    {
        UpdateCalled = true;
        return Task.FromResult(false);
    }

    [Update]
    public void UpdateVoidParam(int? param)
    {
        UpdateCalled = true;
        ReceivedParam = param;
    }

    [Update]
    public void UpdateVoidService([Service] IService service)
    {
        UpdateCalled = true;
        ServiceInjected = service != null;
    }

    [Remote]
    [Update]
    public bool UpdateBoolTrueRemote([Service] IService service)
    {
        UpdateCalled = true;
        ServiceInjected = service != null;
        return true;
    }

    [Remote]
    [Update]
    public bool UpdateBoolTrueRemoteParam(int? param, [Service] IService service)
    {
        UpdateCalled = true;
        ReceivedParam = param;
        ServiceInjected = service != null;
        return true;
    }

    // ============================================================================
    // DELETE methods
    // ============================================================================

    [Delete]
    public void DeleteVoid()
    {
        DeleteCalled = true;
    }

    [Delete]
    public bool DeleteBoolTrue()
    {
        DeleteCalled = true;
        return true;
    }

    [Delete]
    public bool DeleteBoolFalse()
    {
        DeleteCalled = true;
        return false;
    }

    [Delete]
    public Task DeleteTask()
    {
        DeleteCalled = true;
        return Task.CompletedTask;
    }

    [Delete]
    public Task<bool> DeleteTaskBoolTrue()
    {
        DeleteCalled = true;
        return Task.FromResult(true);
    }

    [Delete]
    public Task<bool> DeleteTaskBoolFalse()
    {
        DeleteCalled = true;
        return Task.FromResult(false);
    }

    [Delete]
    public void DeleteVoidParam(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
    }

    [Remote]
    [Delete]
    public bool DeleteBoolTrueRemote(int? param)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        return true;
    }

    [Remote]
    [Delete]
    public void DeleteVoidRemote([Service] IService service)
    {
        DeleteCalled = true;
        ServiceInjected = service != null;
    }

    [Remote]
    [Delete]
    public bool DeleteBoolFalseRemote(int? param, [Service] IService service)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        ServiceInjected = service != null;
        return false;
    }

    [Remote]
    [Delete]
    public Task DeleteTaskRemote(int? param, [Service] IService service)
    {
        DeleteCalled = true;
        ReceivedParam = param;
        ServiceInjected = service != null;
        return Task.CompletedTask;
    }
}
