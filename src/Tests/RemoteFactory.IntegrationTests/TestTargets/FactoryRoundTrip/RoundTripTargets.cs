using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.FactoryRoundTrip;

/// <summary>
/// Test target for basic client-server round-trip with [Remote] Create.
/// </summary>
[Factory]
public partial class RemoteCreateTarget_Simple
{
    public bool CreateCalled { get; set; }
    public int ReceivedValue { get; set; }

    [Create]
    [Remote]
    internal void Create(int value)
    {
        CreateCalled = true;
        ReceivedValue = value;
    }
}

/// <summary>
/// Test target for client-server round-trip with [Remote] and [Service] parameter.
/// </summary>
[Factory]
public partial class RemoteCreateTarget_WithService
{
    public bool CreateCalled { get; set; }
    public int ReceivedValue { get; set; }
    public bool ServiceWasInjected { get; set; }

    [Create]
    [Remote]
    internal void Create(int value, [Service] IService service)
    {
        CreateCalled = true;
        ReceivedValue = value;
        ServiceWasInjected = service != null;
    }
}

/// <summary>
/// Test target for client-server round-trip with [Remote] Fetch.
/// </summary>
[Factory]
public partial class RemoteFetchTarget_Simple
{
    public bool FetchCalled { get; set; }
    public int ReceivedId { get; set; }

    [Fetch]
    [Remote]
    internal void Fetch(int id)
    {
        FetchCalled = true;
        ReceivedId = id;
    }
}

/// <summary>
/// Test target for Save round-trip with [Remote] Insert/Update/Delete.
/// </summary>
[Factory]
public partial class RemoteSaveTarget_Simple : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }

    [Insert]
    [Remote]
    internal Task Insert()
    {
        InsertCalled = true;
        return Task.CompletedTask;
    }

    [Update]
    [Remote]
    internal Task Update()
    {
        UpdateCalled = true;
        return Task.CompletedTask;
    }

    [Delete]
    [Remote]
    internal Task Delete()
    {
        DeleteCalled = true;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test target for [Remote, Fetch] returning Task&lt;bool&gt; false (object not found).
/// When a Fetch method returns false, the factory should return null to the caller.
/// </summary>
[Factory]
public partial class RemoteFetchTarget_BoolFalse
{
    public bool FetchCalled { get; set; }
    public int ReceivedId { get; set; }

    [Fetch]
    [Remote]
    internal Task<bool> Fetch(int id)
    {
        FetchCalled = true;
        ReceivedId = id;
        // Return false = "object not found"
        return Task.FromResult(false);
    }
}

/// <summary>
/// Test target for [Remote, Fetch] returning Task&lt;bool&gt; true (object found).
/// Verifies the happy path: Fetch returns true and the object is returned to the caller.
/// </summary>
[Factory]
public partial class RemoteFetchTarget_BoolTrue
{
    public bool FetchCalled { get; set; }
    public int ReceivedId { get; set; }

    [Fetch]
    [Remote]
    internal Task<bool> Fetch(int id)
    {
        FetchCalled = true;
        ReceivedId = id;
        // Return true = "object found"
        return Task.FromResult(true);
    }
}

/// <summary>
/// Test target for [Remote, Fetch] returning Task&lt;bool&gt; false with [Service] injection.
/// The [Service] parameter proves execution happens on the server (IServerOnlyService
/// is not registered in the client container). If the factory tried to execute locally
/// on the client, the service would be null or throw.
/// </summary>
[Factory]
public partial class RemoteFetchTarget_RemoteBoolFalse
{
    public bool FetchCalled { get; set; }
    public int ReceivedId { get; set; }
    public bool ServiceWasInjected { get; set; }

    [Remote]
    [Fetch]
    internal Task<bool> Fetch(int id, [Service] IServerOnlyService service)
    {
        FetchCalled = true;
        ReceivedId = id;
        ServiceWasInjected = service != null;
        // Return false = "object not found"
        return Task.FromResult(false);
    }
}

/// <summary>
/// Test target for [Remote, Fetch] returning Task&lt;bool&gt; true with [Service] injection.
/// The [Service] parameter proves execution happens on the server.
/// </summary>
[Factory]
public partial class RemoteFetchTarget_RemoteBoolTrue
{
    public bool FetchCalled { get; set; }
    public int ReceivedId { get; set; }
    public bool ServiceWasInjected { get; set; }

    [Remote]
    [Fetch]
    internal Task<bool> Fetch(int id, [Service] IServerOnlyService service)
    {
        FetchCalled = true;
        ReceivedId = id;
        ServiceWasInjected = service != null;
        // Return true = "object found"
        return Task.FromResult(true);
    }
}
