using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Read;

/// <summary>
/// Test target for [Remote][Create] operations with all method signature variations.
/// This mirrors the original RemoteReadDataMapper Create methods in FactoryGeneratorTests for coverage parity.
/// </summary>
/// <remarks>
/// Method naming convention: Create{ReturnType}[True|False][Param][Dep]
/// - ReturnType: Void, Bool, Task, TaskBool
/// - True/False: for bool returns, indicates return value
/// - Param: optional int? parameter
/// - Dep: optional [Service] dependency
/// </remarks>
[Factory]
public partial class RemoteCreateTarget
{
    // Tracking properties
    public bool CreateCalled { get; set; }
    public int? ReceivedParam { get; set; }
    public bool ServiceWasInjected { get; set; }

    #region Create - Void Returns

    [Create]
    [Remote]
    public void CreateVoid()
    {
        CreateCalled = true;
    }

    [Create]
    [Remote]
    public void CreateVoidParam(int? param)
    {
        CreateCalled = true;
        ReceivedParam = param;
    }

    [Create]
    [Remote]
    public void CreateVoidDep([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
    }

    [Create]
    [Remote]
    public void CreateVoidParamDep(int? param, [Service] IService service)
    {
        CreateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
    }

    #endregion

    #region Create - Bool Returns

    [Create]
    [Remote]
    public bool CreateBoolTrue()
    {
        CreateCalled = true;
        return true;
    }

    [Create]
    [Remote]
    public bool CreateBoolFalse()
    {
        CreateCalled = true;
        return false;
    }

    [Create]
    [Remote]
    public bool CreateBoolTrueParam(int? param)
    {
        CreateCalled = true;
        ReceivedParam = param;
        return true;
    }

    [Create]
    [Remote]
    public bool CreateBoolFalseParam(int? param)
    {
        CreateCalled = true;
        ReceivedParam = param;
        return false;
    }

    [Create]
    [Remote]
    public bool CreateBoolTrueDep([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
        return true;
    }

    [Create]
    [Remote]
    public bool CreateBoolFalseDep([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
        return false;
    }

    [Create]
    [Remote]
    public bool CreateBoolTrueParamDep(int? param, [Service] IService service)
    {
        CreateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return true;
    }

    [Create]
    [Remote]
    public bool CreateBoolFalseParamDep(int? param, [Service] IService service)
    {
        CreateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return false;
    }

    #endregion

    #region Create - Task Returns

    [Create]
    [Remote]
    public Task CreateTask()
    {
        CreateCalled = true;
        return Task.CompletedTask;
    }

    [Create]
    [Remote]
    public Task CreateTaskParam(int? param)
    {
        CreateCalled = true;
        ReceivedParam = param;
        return Task.CompletedTask;
    }

    [Create]
    [Remote]
    public Task CreateTaskDep([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    [Create]
    [Remote]
    public Task CreateTaskParamDep(int? param, [Service] IService service)
    {
        CreateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    #endregion

    #region Create - Task<bool> Returns

    [Create]
    [Remote]
    public Task<bool> CreateTaskBoolTrue()
    {
        CreateCalled = true;
        return Task.FromResult(true);
    }

    [Create]
    [Remote]
    public Task<bool> CreateTaskBoolFalse()
    {
        CreateCalled = true;
        return Task.FromResult(false);
    }

    [Create]
    [Remote]
    public Task<bool> CreateTaskBoolTrueParam(int? param)
    {
        CreateCalled = true;
        ReceivedParam = param;
        return Task.FromResult(true);
    }

    [Create]
    [Remote]
    public Task<bool> CreateTaskBoolFalseParam(int? param)
    {
        CreateCalled = true;
        ReceivedParam = param;
        return Task.FromResult(false);
    }

    [Create]
    [Remote]
    public Task<bool> CreateTaskBoolTrueDep([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Create]
    [Remote]
    public Task<bool> CreateTaskBoolFalseDep([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    [Create]
    [Remote]
    public Task<bool> CreateTaskBoolTrueParamDep(int? param, [Service] IService service)
    {
        CreateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Create]
    [Remote]
    public Task<bool> CreateTaskBoolFalseParamDep(int? param, [Service] IService service)
    {
        CreateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    #endregion
}

/// <summary>
/// Test target for [Remote][Fetch] operations with all method signature variations.
/// This mirrors the original RemoteReadDataMapper Fetch methods in FactoryGeneratorTests for coverage parity.
/// </summary>
/// <remarks>
/// Method naming convention: Fetch{ReturnType}[True|False][Param][Dep]
/// - ReturnType: Void, Bool, Task, TaskBool
/// - True/False: for bool returns, indicates return value
/// - Param: optional int? parameter
/// - Dep: optional [Service] dependency
/// </remarks>
[Factory]
public partial class RemoteFetchTarget
{
    // Tracking properties
    public bool FetchCalled { get; set; }
    public int? ReceivedParam { get; set; }
    public bool ServiceWasInjected { get; set; }

    #region Fetch - Void Returns

    [Fetch]
    [Remote]
    public void FetchVoid()
    {
        FetchCalled = true;
    }

    [Fetch]
    [Remote]
    public void FetchVoidParam(int? param)
    {
        FetchCalled = true;
        ReceivedParam = param;
    }

    [Fetch]
    [Remote]
    public void FetchVoidDep([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
    }

    [Fetch]
    [Remote]
    public void FetchVoidParamDep(int? param, [Service] IService service)
    {
        FetchCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
    }

    #endregion

    #region Fetch - Bool Returns

    [Fetch]
    [Remote]
    public bool FetchBoolTrue()
    {
        FetchCalled = true;
        return true;
    }

    [Fetch]
    [Remote]
    public bool FetchBoolFalse()
    {
        FetchCalled = true;
        return false;
    }

    [Fetch]
    [Remote]
    public bool FetchBoolTrueParam(int? param)
    {
        FetchCalled = true;
        ReceivedParam = param;
        return true;
    }

    [Fetch]
    [Remote]
    public bool FetchBoolFalseParam(int? param)
    {
        FetchCalled = true;
        ReceivedParam = param;
        return false;
    }

    [Fetch]
    [Remote]
    public bool FetchBoolTrueDep([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
        return true;
    }

    [Fetch]
    [Remote]
    public bool FetchBoolFalseDep([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
        return false;
    }

    [Fetch]
    [Remote]
    public bool FetchBoolTrueParamDep(int? param, [Service] IService service)
    {
        FetchCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return true;
    }

    [Fetch]
    [Remote]
    public bool FetchBoolFalseParamDep(int? param, [Service] IService service)
    {
        FetchCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return false;
    }

    #endregion

    #region Fetch - Task Returns

    [Fetch]
    [Remote]
    public Task FetchTask()
    {
        FetchCalled = true;
        return Task.CompletedTask;
    }

    [Fetch]
    [Remote]
    public Task FetchTaskParam(int? param)
    {
        FetchCalled = true;
        ReceivedParam = param;
        return Task.CompletedTask;
    }

    [Fetch]
    [Remote]
    public Task FetchTaskDep([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    [Fetch]
    [Remote]
    public Task FetchTaskParamDep(int? param, [Service] IService service)
    {
        FetchCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    #endregion

    #region Fetch - Task<bool> Returns

    [Fetch]
    [Remote]
    public Task<bool> FetchTaskBoolTrue()
    {
        FetchCalled = true;
        return Task.FromResult(true);
    }

    [Fetch]
    [Remote]
    public Task<bool> FetchTaskBoolFalse()
    {
        FetchCalled = true;
        return Task.FromResult(false);
    }

    [Fetch]
    [Remote]
    public Task<bool> FetchTaskBoolTrueParam(int? param)
    {
        FetchCalled = true;
        ReceivedParam = param;
        return Task.FromResult(true);
    }

    [Fetch]
    [Remote]
    public Task<bool> FetchTaskBoolFalseParam(int? param)
    {
        FetchCalled = true;
        ReceivedParam = param;
        return Task.FromResult(false);
    }

    [Fetch]
    [Remote]
    public Task<bool> FetchTaskBoolTrueDep([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Fetch]
    [Remote]
    public Task<bool> FetchTaskBoolFalseDep([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    [Fetch]
    [Remote]
    public Task<bool> FetchTaskBoolTrueParamDep(int? param, [Service] IService service)
    {
        FetchCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Fetch]
    [Remote]
    public Task<bool> FetchTaskBoolFalseParamDep(int? param, [Service] IService service)
    {
        FetchCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    #endregion
}
