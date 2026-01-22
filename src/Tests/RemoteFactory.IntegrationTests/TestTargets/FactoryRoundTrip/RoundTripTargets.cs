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
    public void Create(int value)
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
    public void Create(int value, [Service] IService service)
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
    public void Fetch(int id)
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
    public Task Insert()
    {
        InsertCalled = true;
        return Task.CompletedTask;
    }

    [Update]
    [Remote]
    public Task Update()
    {
        UpdateCalled = true;
        return Task.CompletedTask;
    }

    [Delete]
    [Remote]
    public Task Delete()
    {
        DeleteCalled = true;
        return Task.CompletedTask;
    }
}
