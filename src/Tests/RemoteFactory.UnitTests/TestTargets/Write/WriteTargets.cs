using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Write;

/// <summary>
/// Test target for [Insert]/[Update]/[Delete] with void return and no parameters.
/// </summary>
[Factory]
public partial class WriteTarget_Void_NoParams : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }

    [Insert]
    public void Insert()
    {
        InsertCalled = true;
    }

    [Update]
    public void Update()
    {
        UpdateCalled = true;
    }

    [Delete]
    public void Delete()
    {
        DeleteCalled = true;
    }
}

/// <summary>
/// Test target for [Insert]/[Update]/[Delete] with bool return type.
/// </summary>
[Factory]
public partial class WriteTarget_Bool_NoParams : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }

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
}

/// <summary>
/// Test target for [Insert]/[Update]/[Delete] with Task return type.
/// </summary>
[Factory]
public partial class WriteTarget_Task_NoParams : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }

    [Insert]
    public Task InsertTask()
    {
        InsertCalled = true;
        return Task.CompletedTask;
    }

    [Update]
    public Task UpdateTask()
    {
        UpdateCalled = true;
        return Task.CompletedTask;
    }

    [Delete]
    public Task DeleteTask()
    {
        DeleteCalled = true;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test target for [Insert]/[Update]/[Delete] with Task&lt;bool&gt; return type.
/// </summary>
[Factory]
public partial class WriteTarget_TaskBool_NoParams : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }

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
}

/// <summary>
/// Test target for [Insert]/[Update]/[Delete] with [Service] parameter.
/// </summary>
[Factory]
public partial class WriteTarget_Void_ServiceParam : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public bool ServiceWasInjected { get; set; }

    [Insert]
    public void Insert([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
    }

    [Update]
    public void Update([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
    }

    [Delete]
    public void Delete([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
    }
}

/// <summary>
/// Test target for [Insert]/[Update]/[Delete] with int parameter.
/// </summary>
[Factory]
public partial class WriteTarget_Void_IntParam : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public int? ReceivedValue { get; set; }

    [Insert]
    public void Insert(int? value)
    {
        InsertCalled = true;
        ReceivedValue = value;
    }

    [Update]
    public void Update(int? value)
    {
        UpdateCalled = true;
        ReceivedValue = value;
    }

    [Delete]
    public void Delete(int? value)
    {
        DeleteCalled = true;
        ReceivedValue = value;
    }
}

/// <summary>
/// Test target for [Insert]/[Update]/[Delete] with CancellationToken.
/// </summary>
[Factory]
public partial class WriteTarget_Task_CancellationToken : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public bool CancellationTokenReceived { get; set; }

    [Insert]
    public Task InsertTask(CancellationToken ct)
    {
        InsertCalled = true;
        CancellationTokenReceived = ct != default;
        return Task.CompletedTask;
    }

    [Update]
    public Task UpdateTask(CancellationToken ct)
    {
        UpdateCalled = true;
        CancellationTokenReceived = ct != default;
        return Task.CompletedTask;
    }

    [Delete]
    public Task DeleteTask(CancellationToken ct)
    {
        DeleteCalled = true;
        CancellationTokenReceived = ct != default;
        return Task.CompletedTask;
    }
}
