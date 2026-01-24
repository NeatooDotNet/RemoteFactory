using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Events;

/// <summary>
/// Simple [Event] method that returns Task.
/// Method name: FireSimple -> delegate name: FireSimpleEvent
/// </summary>
[Factory]
public partial class EventTarget_Simple
{
    public string Value { get; set; } = "";
    public bool EventFired { get; private set; }

    [Event]
    public Task FireSimple(string message)
    {
        EventFired = true;
        Value = message;
        return Task.CompletedTask;
    }

    [Create]
    public static EventTarget_Simple Create()
    {
        return new EventTarget_Simple();
    }
}

/// <summary>
/// [Event] method that returns void (generator should wrap in Task delegate).
/// Method name: FireVoid -> delegate name: FireVoidEvent
/// </summary>
[Factory]
public partial class EventTarget_VoidReturn
{
    public bool EventFired { get; private set; }
    public string ReceivedMessage { get; private set; } = "";

    [Event]
    public void FireVoid(string message)
    {
        EventFired = true;
        ReceivedMessage = message;
    }

    [Create]
    public static EventTarget_VoidReturn Create()
    {
        return new EventTarget_VoidReturn();
    }
}

/// <summary>
/// [Event] method with [Service] parameter.
/// Method name: FireWithService -> delegate name: FireWithServiceEvent
/// </summary>
[Factory]
public partial class EventTarget_WithService
{
    public bool EventFired { get; private set; }
    public bool ServiceInjected { get; private set; }

    [Event]
    public Task FireWithService(Guid id, [Service] IService service)
    {
        EventFired = true;
        ServiceInjected = service != null;
        return Task.CompletedTask;
    }

    [Create]
    public static EventTarget_WithService Create()
    {
        return new EventTarget_WithService();
    }
}

/// <summary>
/// [Event] method with multiple parameters.
/// Method name: FireMultiParam -> delegate name: FireMultiParamEvent
/// </summary>
[Factory]
public partial class EventTarget_MultipleParams
{
    public bool EventFired { get; private set; }
    public Guid ReceivedId { get; private set; }
    public string ReceivedName { get; private set; } = "";
    public int ReceivedCount { get; private set; }

    [Event]
    public Task FireMultiParam(Guid id, string name, int count)
    {
        EventFired = true;
        ReceivedId = id;
        ReceivedName = name;
        ReceivedCount = count;
        return Task.CompletedTask;
    }

    [Create]
    public static EventTarget_MultipleParams Create()
    {
        return new EventTarget_MultipleParams();
    }
}

/// <summary>
/// Static class with [Event] method.
/// Method name: FireStatic -> delegate name: FireStaticEvent
/// </summary>
[Factory]
public static partial class EventTarget_Static
{
    public static bool EventFired { get; private set; }
    public static string ReceivedMessage { get; private set; } = "";

    public static void Reset()
    {
        EventFired = false;
        ReceivedMessage = "";
    }

    [Event]
    public static Task FireStatic(string message)
    {
        EventFired = true;
        ReceivedMessage = message;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Static class [Event] with [Service] parameter.
/// Method name: FireStaticService -> delegate name: FireStaticServiceEvent
/// </summary>
[Factory]
public static partial class EventTarget_StaticWithService
{
    public static bool EventFired { get; private set; }
    public static bool ServiceInjected { get; private set; }

    public static void Reset()
    {
        EventFired = false;
        ServiceInjected = false;
    }

    [Event]
    public static Task FireStaticService(Guid id, [Service] IService service)
    {
        EventFired = true;
        ServiceInjected = service != null;
        return Task.CompletedTask;
    }
}

/// <summary>
/// [Event] method with CancellationToken parameter.
/// Method name: FireWithCancellation -> delegate name: FireWithCancellationEvent
/// </summary>
[Factory]
public partial class EventTarget_WithCancellation
{
    public bool EventFired { get; private set; }
    public bool CancellationTokenReceived { get; private set; }

    [Event]
    public Task FireWithCancellation(string message, CancellationToken ct)
    {
        EventFired = true;
        CancellationTokenReceived = true;
        return Task.CompletedTask;
    }

    [Create]
    public static EventTarget_WithCancellation Create()
    {
        return new EventTarget_WithCancellation();
    }
}
