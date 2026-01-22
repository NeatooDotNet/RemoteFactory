using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Read;

/// <summary>
/// Test target for [Create] that returns void with no parameters.
/// </summary>
[Factory]
public partial class CreateTarget_Void_NoParams
{
    public bool CreateCalled { get; set; }

    [Create]
    public void Create()
    {
        CreateCalled = true;
    }
}

/// <summary>
/// Test target for [Create] that returns bool with no parameters.
/// </summary>
[Factory]
public partial class CreateTarget_Bool_NoParams
{
    public bool CreateCalled { get; set; }

    [Create]
    public bool CreateBoolTrue()
    {
        CreateCalled = true;
        return true;
    }

    [Create]
    public bool CreateBoolFalse()
    {
        CreateCalled = true;
        return false;
    }
}

/// <summary>
/// Test target for [Create] that returns Task with no parameters.
/// </summary>
[Factory]
public partial class CreateTarget_Task_NoParams
{
    public bool CreateCalled { get; set; }

    [Create]
    public Task CreateTask()
    {
        CreateCalled = true;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test target for [Create] that returns Task&lt;bool&gt; with no parameters.
/// </summary>
[Factory]
public partial class CreateTarget_TaskBool_NoParams
{
    public bool CreateCalled { get; set; }

    [Create]
    public Task<bool> CreateTaskBoolTrue()
    {
        CreateCalled = true;
        return Task.FromResult(true);
    }

    [Create]
    public Task<bool> CreateTaskBoolFalse()
    {
        CreateCalled = true;
        return Task.FromResult(false);
    }
}

/// <summary>
/// Test target for [Create] with a [Service] parameter injection.
/// </summary>
[Factory]
public partial class CreateTarget_Void_ServiceParam
{
    public bool CreateCalled { get; set; }
    public bool ServiceWasInjected { get; set; }

    [Create]
    public void Create([Service] IService service)
    {
        CreateCalled = true;
        ServiceWasInjected = service != null;
    }
}

/// <summary>
/// Test target for [Create] with int parameter.
/// </summary>
[Factory]
public partial class CreateTarget_Void_IntParam
{
    public bool CreateCalled { get; set; }
    public int? ReceivedValue { get; set; }

    [Create]
    public void Create(int? value)
    {
        CreateCalled = true;
        ReceivedValue = value;
    }
}

/// <summary>
/// Test target for [Create] with both parameter and [Service].
/// </summary>
[Factory]
public partial class CreateTarget_Void_MixedParams
{
    public bool CreateCalled { get; set; }
    public int? ReceivedValue { get; set; }
    public bool ServiceWasInjected { get; set; }

    [Create]
    public void Create(int? value, [Service] IService service)
    {
        CreateCalled = true;
        ReceivedValue = value;
        ServiceWasInjected = service != null;
    }
}

/// <summary>
/// Test target for [Create] via constructor (no method, just constructor).
/// </summary>
[Factory]
public partial class CreateTarget_Constructor_NoParams
{
    public bool ConstructorCalled { get; }

    [Create]
    public CreateTarget_Constructor_NoParams()
    {
        ConstructorCalled = true;
    }
}

/// <summary>
/// Test target for [Create] via constructor with parameters.
/// </summary>
[Factory]
public partial class CreateTarget_Constructor_IntParam
{
    public bool ConstructorCalled { get; }
    public int? ReceivedValue { get; }

    [Create]
    public CreateTarget_Constructor_IntParam(int? value)
    {
        ConstructorCalled = true;
        ReceivedValue = value;
    }
}
