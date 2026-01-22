using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.Logical;

/// <summary>
/// Test target for Logical mode with [Remote] on save methods.
/// This matches the pattern used in Neatoo's Person entity.
/// </summary>
[Factory]
public partial class LogicalModeTarget_Remote : IFactorySaveMeta
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

/// <summary>
/// Test target for Logical mode with [Remote] and [Service] parameters.
/// </summary>
[Factory]
public partial class LogicalModeTarget_RemoteWithService : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public bool ServiceWasInjected { get; set; }

    [Insert]
    [Remote]
    public Task Insert([Service] IService service)
    {
        InsertCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    [Update]
    [Remote]
    public Task Update([Service] IService service)
    {
        UpdateCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }

    [Delete]
    [Remote]
    public Task Delete([Service] IService service)
    {
        DeleteCalled = true;
        ServiceWasInjected = service != null;
        return Task.CompletedTask;
    }
}
