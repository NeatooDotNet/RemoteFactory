using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.SaveOperation;

// ============================================================================
// Granular Authorization for Save Operations
// ============================================================================

#region save-authorization
/// <summary>
/// Authorization interface with granular control over Employee operations.
/// </summary>
public interface IEmployeeWriteAuth
{
    /// <summary>
    /// Controls Create operation authorization.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    /// <summary>
    /// Controls Insert, Update, and Delete operations.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

/// <summary>
/// Authorization implementation with role-based rules.
/// </summary>
public class EmployeeWriteAuth : IEmployeeWriteAuth
{
    private readonly IUserContext _userContext;

    public EmployeeWriteAuth(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Any authenticated user can create employees.
    /// </summary>
    public bool CanCreate()
    {
        return _userContext.IsAuthenticated;
    }

    /// <summary>
    /// Only HR or Admin can modify employee records.
    /// </summary>
    public bool CanWrite()
    {
        return _userContext.IsInRole("HR") || _userContext.IsInRole("Admin");
    }
}

/// <summary>
/// Employee aggregate with granular authorization on write operations.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeWriteAuth>]
public partial class AuthorizedEmployeeWrite : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthorizedEmployeeWrite()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Insert requires CanWrite() = true.
    /// </summary>
    [Remote, Insert]
    public Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Update requires CanWrite() = true.
    /// </summary>
    [Remote, Update]
    public Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Delete requires CanWrite() = true.
    /// </summary>
    [Remote, Delete]
    public Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
#endregion

// ============================================================================
// Combined Authorization for All Write Operations
// ============================================================================

#region save-authorization-combined
/// <summary>
/// Authorization interface with single check for all write operations.
/// Write = Insert | Update | Delete
/// </summary>
public interface ICombinedWriteAuth
{
    /// <summary>
    /// Single authorization check covering Insert, Update, and Delete.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

/// <summary>
/// Combined authorization implementation.
/// </summary>
public class CombinedWriteAuth : ICombinedWriteAuth
{
    private readonly IUserContext _userContext;

    public CombinedWriteAuth(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Only Editor or Admin can perform any write operation.
    /// </summary>
    public bool CanWrite()
    {
        return _userContext.IsInRole("Editor") || _userContext.IsInRole("Admin");
    }
}

/// <summary>
/// Department aggregate with combined authorization for all write operations.
/// </summary>
[Factory]
[AuthorizeFactory<ICombinedWriteAuth>]
public partial class AuthorizedDepartmentWrite : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthorizedDepartmentWrite()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Insert]
    public Task Insert(CancellationToken ct)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Update]
    public Task Update(CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    [Remote, Delete]
    public Task Delete(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
#endregion
