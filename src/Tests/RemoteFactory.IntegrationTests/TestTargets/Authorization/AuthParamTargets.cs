using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Authorization;

// ============================================================================
// Feature #1: Type-based parameter matching for class factories
// ============================================================================

#region Feature 1 - Auth class with parameterized methods

/// <summary>
/// Authorization class with both parameterless and parameterized auth methods.
/// Parameterized methods are matched by type to factory method parameters.
/// </summary>
public class ClassAuthWithParams
{
    public static readonly Guid DenyFetchGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static bool AllowWrite { get; set; } = true;

    /// <summary>
    /// Parameterless Read auth - always allows.
    /// Applied to Create (no Guid param) and Fetch (alongside CanFetchWithId).
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool CanRead()
    {
        return true;
    }

    /// <summary>
    /// Parameterized Fetch auth - matched by type to Fetch's Guid parameter.
    /// Denies when id matches DenyFetchGuid.
    /// Uses Fetch (not Read) so it doesn't apply to Create which has no Guid param.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    public bool CanFetchWithId(Guid id)
    {
        return id != DenyFetchGuid;
    }

    /// <summary>
    /// Parameterless Write auth - uses static bool for test control.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    public bool CanWrite()
    {
        return AllowWrite;
    }
}

#endregion

#region Feature 1 - Factory target class

/// <summary>
/// Class factory with parameterized auth methods for type-based matching tests.
/// Write methods have no extra Guid param (auth matching is tested on Fetch).
/// </summary>
[Factory]
[AuthorizeFactory<ClassAuthWithParams>]
public partial class AuthParamClassTarget : IFactorySaveMeta
{
    public Guid Id { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthParamClassTarget()
    {
        Id = Guid.NewGuid();
    }

    [Remote]
    [Fetch]
    internal Task<bool> Fetch(Guid id, [Service] IServerOnlyService svc, CancellationToken ct = default)
    {
        Id = id;
        IsNew = false;
        return Task.FromResult(true);
    }

    [Remote]
    [Insert]
    internal Task Insert([Service] IServerOnlyService svc, CancellationToken ct = default)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote]
    [Update]
    internal Task Update([Service] IServerOnlyService svc, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    [Remote]
    [Delete]
    internal Task Delete([Service] IServerOnlyService svc, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

#endregion

// ============================================================================
// Feature #2: Target parameter in auth methods (write operations)
// ============================================================================

#region Feature 2 - Auth class with target parameter

/// <summary>
/// Authorization class where the Write auth method receives the target entity.
/// Inspects target.Status to make authorization decisions.
/// </summary>
public class AuthWithTargetParam
{
    /// <summary>
    /// Parameterless Read auth - always allows.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool CanRead()
    {
        return true;
    }

    /// <summary>
    /// Write auth that receives the target entity.
    /// Denies when target.Status is "Locked".
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    public bool CanWrite(IAuthTargetParamObj target)
    {
        return target.Status != "Locked";
    }
}

#endregion

#region Feature 2 - Target interface and factory class

/// <summary>
/// Interface for the target-param auth test entity.
/// </summary>
public interface IAuthTargetParamObj : IFactorySaveMeta
{
    string Status { get; set; }
    new bool IsNew { get; set; }
    new bool IsDeleted { get; set; }
}

/// <summary>
/// Factory class where write authorization receives the target entity.
/// CanInsert/CanUpdate/CanDelete should NOT be generated
/// because the auth method has a target parameter and these operations
/// run before the entity is available.
/// CanCreate/CanFetch SHOULD be generated (Read auth is parameterless).
/// CanSave() and CanSave(target) SHOULD be generated — the caller has
/// the entity when deciding whether to save.
/// </summary>
[Factory]
[AuthorizeFactory<AuthWithTargetParam>]
public partial class AuthTargetParamObj : IAuthTargetParamObj
{
    public string Status { get; set; } = "Active";
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthTargetParamObj()
    {
    }

    [Remote]
    [Fetch]
    internal Task<bool> Fetch(Guid id, [Service] IServerOnlyService svc, CancellationToken ct = default)
    {
        IsNew = false;
        Status = "Active";
        return Task.FromResult(true);
    }

    [Remote]
    [Insert]
    internal Task Insert([Service] IServerOnlyService svc, CancellationToken ct = default)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote]
    [Update]
    internal Task Update([Service] IServerOnlyService svc, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    [Remote]
    [Delete]
    internal Task Delete([Service] IServerOnlyService svc, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

#endregion
