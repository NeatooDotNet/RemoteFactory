using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Read;

/// <summary>
/// Test target for [Fetch] that returns void with no parameters.
/// </summary>
[Factory]
public partial class FetchTarget_Void_NoParams
{
    public bool FetchCalled { get; set; }

    [Fetch]
    public void Fetch()
    {
        FetchCalled = true;
    }
}

/// <summary>
/// Test target for [Fetch] that returns bool with no parameters.
/// </summary>
[Factory]
public partial class FetchTarget_Bool_NoParams
{
    public bool FetchCalled { get; set; }

    [Fetch]
    public bool FetchBoolTrue()
    {
        FetchCalled = true;
        return true;
    }

    [Fetch]
    public bool FetchBoolFalse()
    {
        FetchCalled = true;
        return false;
    }
}

/// <summary>
/// Test target for [Fetch] that returns Task with no parameters.
/// </summary>
[Factory]
public partial class FetchTarget_Task_NoParams
{
    public bool FetchCalled { get; set; }

    [Fetch]
    public Task FetchTask()
    {
        FetchCalled = true;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test target for [Fetch] that returns Task&lt;bool&gt; with no parameters.
/// </summary>
[Factory]
public partial class FetchTarget_TaskBool_NoParams
{
    public bool FetchCalled { get; set; }

    [Fetch]
    public Task<bool> FetchTaskBoolTrue()
    {
        FetchCalled = true;
        return Task.FromResult(true);
    }

    [Fetch]
    public Task<bool> FetchTaskBoolFalse()
    {
        FetchCalled = true;
        return Task.FromResult(false);
    }
}

/// <summary>
/// Test target for [Fetch] with a [Service] parameter injection.
/// </summary>
[Factory]
public partial class FetchTarget_Void_ServiceParam
{
    public bool FetchCalled { get; set; }
    public bool ServiceWasInjected { get; set; }

    [Fetch]
    public void Fetch([Service] IService service)
    {
        FetchCalled = true;
        ServiceWasInjected = service != null;
    }
}

/// <summary>
/// Test target for [Fetch] with int parameter.
/// </summary>
[Factory]
public partial class FetchTarget_Void_IntParam
{
    public bool FetchCalled { get; set; }
    public int? ReceivedId { get; set; }

    [Fetch]
    public void Fetch(int? id)
    {
        FetchCalled = true;
        ReceivedId = id;
    }
}

/// <summary>
/// Test target for [Fetch] with both parameter and [Service].
/// </summary>
[Factory]
public partial class FetchTarget_Void_MixedParams
{
    public bool FetchCalled { get; set; }
    public int? ReceivedId { get; set; }
    public bool ServiceWasInjected { get; set; }

    [Fetch]
    public void Fetch(int? id, [Service] IService service)
    {
        FetchCalled = true;
        ReceivedId = id;
        ServiceWasInjected = service != null;
    }
}

/// <summary>
/// Test target for [Fetch] via constructor.
/// </summary>
[Factory]
public partial class FetchTarget_Constructor_IntParam
{
    public bool ConstructorCalled { get; }
    public int? ReceivedId { get; }

    [Fetch]
    public FetchTarget_Constructor_IntParam(int? id)
    {
        ConstructorCalled = true;
        ReceivedId = id;
    }
}
