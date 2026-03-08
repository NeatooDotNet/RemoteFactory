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
    internal void CreateVoid()
    {
        CreateCalled = true;
    }

    [Create]
    [Remote]
    internal void CreateVoidParam(int? param)
    {
        CreateCalled = true;
        ReceivedParam = param;
    }

    [Create]
    [Remote]
    internal void CreateVoidDep([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
    }

    [Create]
    [Remote]
    internal void CreateVoidParamDep(int? param, [Service] IService service)
    {
        CreateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
    }

    #endregion

    #region Create - Bool Returns

    [Create]
    [Remote]
    internal bool CreateBoolTrue()
    {
        CreateCalled = true;
        return true;
    }

    [Create]
    [Remote]
    internal bool CreateBoolFalse()
    {
        CreateCalled = true;
        return false;
    }

    [Create]
    [Remote]
    internal bool CreateBoolTrueParam(int? param)
    {
        CreateCalled = true;
        ReceivedParam = param;
        return true;
    }

    [Create]
    [Remote]
    internal bool CreateBoolFalseParam(int? param)
    {
        CreateCalled = true;
        ReceivedParam = param;
        return false;
    }

    [Create]
    [Remote]
    internal bool CreateBoolTrueDep([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
        return true;
    }

    [Create]
    [Remote]
    internal bool CreateBoolFalseDep([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
        return false;
    }

    [Create]
    [Remote]
    internal bool CreateBoolTrueParamDep(int? param, [Service] IService service)
    {
        CreateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return true;
    }

    [Create]
    [Remote]
    internal bool CreateBoolFalseParamDep(int? param, [Service] IService service)
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
    internal Task CreateTask()
    {
        CreateCalled = true;
        return Task.CompletedTask;
    }

    [Create]
    [Remote]
    internal Task CreateTaskParam(int? param)
    {
        CreateCalled = true;
        ReceivedParam = param;
        return Task.CompletedTask;
    }

    [Create]
    [Remote]
    internal Task CreateTaskDep([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    [Create]
    [Remote]
    internal Task CreateTaskParamDep(int? param, [Service] IService service)
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
    internal Task<bool> CreateTaskBoolTrue()
    {
        CreateCalled = true;
        return Task.FromResult(true);
    }

    [Create]
    [Remote]
    internal Task<bool> CreateTaskBoolFalse()
    {
        CreateCalled = true;
        return Task.FromResult(false);
    }

    [Create]
    [Remote]
    internal Task<bool> CreateTaskBoolTrueParam(int? param)
    {
        CreateCalled = true;
        ReceivedParam = param;
        return Task.FromResult(true);
    }

    [Create]
    [Remote]
    internal Task<bool> CreateTaskBoolFalseParam(int? param)
    {
        CreateCalled = true;
        ReceivedParam = param;
        return Task.FromResult(false);
    }

    [Create]
    [Remote]
    internal Task<bool> CreateTaskBoolTrueDep([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Create]
    [Remote]
    internal Task<bool> CreateTaskBoolFalseDep([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    [Create]
    [Remote]
    internal Task<bool> CreateTaskBoolTrueParamDep(int? param, [Service] IService service)
    {
        CreateCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Create]
    [Remote]
    internal Task<bool> CreateTaskBoolFalseParamDep(int? param, [Service] IService service)
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
    internal void FetchVoid()
    {
        FetchCalled = true;
    }

    [Fetch]
    [Remote]
    internal void FetchVoidParam(int? param)
    {
        FetchCalled = true;
        ReceivedParam = param;
    }

    [Fetch]
    [Remote]
    internal void FetchVoidDep([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
    }

    [Fetch]
    [Remote]
    internal void FetchVoidParamDep(int? param, [Service] IService service)
    {
        FetchCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
    }

    #endregion

    #region Fetch - Bool Returns

    [Fetch]
    [Remote]
    internal bool FetchBoolTrue()
    {
        FetchCalled = true;
        return true;
    }

    [Fetch]
    [Remote]
    internal bool FetchBoolFalse()
    {
        FetchCalled = true;
        return false;
    }

    [Fetch]
    [Remote]
    internal bool FetchBoolTrueParam(int? param)
    {
        FetchCalled = true;
        ReceivedParam = param;
        return true;
    }

    [Fetch]
    [Remote]
    internal bool FetchBoolFalseParam(int? param)
    {
        FetchCalled = true;
        ReceivedParam = param;
        return false;
    }

    [Fetch]
    [Remote]
    internal bool FetchBoolTrueDep([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
        return true;
    }

    [Fetch]
    [Remote]
    internal bool FetchBoolFalseDep([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
        return false;
    }

    [Fetch]
    [Remote]
    internal bool FetchBoolTrueParamDep(int? param, [Service] IService service)
    {
        FetchCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return true;
    }

    [Fetch]
    [Remote]
    internal bool FetchBoolFalseParamDep(int? param, [Service] IService service)
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
    internal Task FetchTask()
    {
        FetchCalled = true;
        return Task.CompletedTask;
    }

    [Fetch]
    [Remote]
    internal Task FetchTaskParam(int? param)
    {
        FetchCalled = true;
        ReceivedParam = param;
        return Task.CompletedTask;
    }

    [Fetch]
    [Remote]
    internal Task FetchTaskDep([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    [Fetch]
    [Remote]
    internal Task FetchTaskParamDep(int? param, [Service] IService service)
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
    internal Task<bool> FetchTaskBoolTrue()
    {
        FetchCalled = true;
        return Task.FromResult(true);
    }

    [Fetch]
    [Remote]
    internal Task<bool> FetchTaskBoolFalse()
    {
        FetchCalled = true;
        return Task.FromResult(false);
    }

    [Fetch]
    [Remote]
    internal Task<bool> FetchTaskBoolTrueParam(int? param)
    {
        FetchCalled = true;
        ReceivedParam = param;
        return Task.FromResult(true);
    }

    [Fetch]
    [Remote]
    internal Task<bool> FetchTaskBoolFalseParam(int? param)
    {
        FetchCalled = true;
        ReceivedParam = param;
        return Task.FromResult(false);
    }

    [Fetch]
    [Remote]
    internal Task<bool> FetchTaskBoolTrueDep([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Fetch]
    [Remote]
    internal Task<bool> FetchTaskBoolFalseDep([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    [Fetch]
    [Remote]
    internal Task<bool> FetchTaskBoolTrueParamDep(int? param, [Service] IService service)
    {
        FetchCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(true);
    }

    [Fetch]
    [Remote]
    internal Task<bool> FetchTaskBoolFalseParamDep(int? param, [Service] IService service)
    {
        FetchCalled = true;
        ReceivedParam = param;
        ServiceWasInjected = service != null;
        return Task.FromResult(false);
    }

    #endregion
}
