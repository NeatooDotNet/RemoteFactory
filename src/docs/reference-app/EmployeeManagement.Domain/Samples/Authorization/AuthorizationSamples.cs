using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Authorization;

#region authorization-interface
/// <summary>
/// Authorization interface defining access checks for Employee operations.
/// Methods decorated with [AuthorizeFactory] control access to specific operations.
/// </summary>
public interface IEmployeeAuthorization
{
    /// <summary>
    /// Checks if the current user can create new employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    /// <summary>
    /// Checks if the current user can read employee data.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    /// <summary>
    /// Checks if the current user can modify employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}
#endregion

#region authorization-implementation
/// <summary>
/// Implementation of employee authorization with injected user context.
/// </summary>
public class EmployeeAuthorizationImpl : IEmployeeAuthorization
{
    private readonly IUserContext _userContext;

    public EmployeeAuthorizationImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Only HR and Managers can create new employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    public bool CanCreate()
    {
        return _userContext.IsAuthenticated &&
               (_userContext.IsInRole("HR") || _userContext.IsInRole("Manager"));
    }

    /// <summary>
    /// All authenticated users can read employee data.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool CanRead()
    {
        return _userContext.IsAuthenticated;
    }

    /// <summary>
    /// Only HR and Managers can modify employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    public bool CanWrite()
    {
        return _userContext.IsAuthenticated &&
               (_userContext.IsInRole("HR") || _userContext.IsInRole("Manager"));
    }
}
#endregion

#region authorization-apply
/// <summary>
/// Employee aggregate with authorization applied via [AuthorizeFactory<T>].
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class EmployeeWithAuthorization : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithAuthorization()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
#endregion

#region authorization-combined-flags
/// <summary>
/// Authorization with combined operation flags.
/// </summary>
public interface IEmployeeCombinedAuth
{
    /// <summary>
    /// Single method checks both Read and Write operations.
    /// Use bitwise OR to combine flags.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool CanAccess();
}
#endregion

#region authorization-method-level
/// <summary>
/// Employee with method-level authorization adding to class-level auth.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class EmployeeWithMethodAuth : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsTerminated { get; private set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithMethodAuth() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// TerminateEmployee requires both class-level auth AND method-level HRManager role.
    /// [AuthorizeFactory<T>] runs first, then [AspAuthorize].
    /// </summary>
    [Remote, Delete]
    [AspAuthorize(Roles = "HRManager")]
    public async Task Delete(
        [Service] IEmployeeRepository repo,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        await auditLog.LogAsync("Terminate", Id, "Employee", "Terminated", ct);
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
#endregion

#region authorization-exception
/// <summary>
/// Demonstrates throwing NotAuthorizedException for explicit failures.
/// </summary>
[Factory]
public partial class EmployeeWithExplicitAuth : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithExplicitAuth() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Business rule: Only HR can modify salary above threshold.
    /// Throws NotAuthorizedException for explicit auth failures.
    /// </summary>
    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repo,
        [Service] IUserContext userContext,
        CancellationToken ct)
    {
        var existing = await repo.GetByIdAsync(Id, ct);
        if (existing == null) return;

        // Business rule enforcement
        if (Salary != existing.SalaryAmount && Salary > 100000)
        {
            if (!userContext.IsInRole("HR"))
            {
                throw new NotAuthorizedException(
                    "Only HR can set salary above $100,000");
            }
        }

        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }
}
#endregion

#region authorization-events
/// <summary>
/// Events bypass authorization - they are internal operations.
/// </summary>
[Factory]
public partial class EmployeeEventNoAuth
{
    /// <summary>
    /// Events do NOT require authorization checks.
    /// They are triggered by application code, not user requests.
    /// AuthorizeFactoryOperation.Event flag is never checked.
    /// </summary>
    [Event]
    public async Task LogActivity(
        Guid employeeId,
        string activity,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        // No authorization check - events are internal
        await auditLog.LogAsync("Activity", employeeId, "Employee", activity, ct);
    }
}
#endregion

#region authorization-context
/// <summary>
/// Context-aware authorization checking entity ownership.
/// </summary>
public interface IDepartmentMembershipAuth
{
    /// <summary>
    /// User can only modify employees in their own department.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    Task<bool> CanModifyInDepartment(Guid departmentId);
}

public class DepartmentMembershipAuth : IDepartmentMembershipAuth
{
    private readonly IUserContext _userContext;
    private readonly IDepartmentRepository _departmentRepo;

    public DepartmentMembershipAuth(
        IUserContext userContext,
        IDepartmentRepository departmentRepo)
    {
        _userContext = userContext;
        _departmentRepo = departmentRepo;
    }

    /// <summary>
    /// Context-aware authorization using domain repositories.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    public async Task<bool> CanModifyInDepartment(Guid departmentId)
    {
        if (!_userContext.IsAuthenticated)
            return false;

        // HR can modify any department
        if (_userContext.IsInRole("HR"))
            return true;

        // Managers can only modify their own department
        if (_userContext.IsInRole("Manager"))
        {
            var department = await _departmentRepo.GetByIdAsync(departmentId, default);
            return department?.ManagerId == _userContext.UserId;
        }

        return false;
    }
}
#endregion

#region authorization-combined
/// <summary>
/// Combines AuthorizeFactory and AspAuthorize for defense in depth.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]  // Custom domain auth
public partial class EmployeeDefenseInDepth : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    // Execution order:
    // 1. [AuthorizeFactory] custom domain checks run first
    // 2. [AspAuthorize] ASP.NET Core policies run second
    // 3. If both pass, the domain method executes

    [Create]
    public EmployeeDefenseInDepth() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [AspAuthorize] with policy uses constructor argument.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]  // Policy via constructor
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
#endregion

#region save-authorization
/// <summary>
/// Authorization interface with granular Insert, Update, Delete checks.
/// </summary>
public interface IEmployeeWriteAuth
{
    /// <summary>
    /// All managers can insert new employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
    bool CanInsert();

    /// <summary>
    /// Managers can update their direct reports.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    bool CanUpdate();

    /// <summary>
    /// Only HR can delete employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}

public class EmployeeWriteAuthImpl : IEmployeeWriteAuth
{
    private readonly IUserContext _userContext;

    public EmployeeWriteAuthImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    [AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
    public bool CanInsert() =>
        _userContext.IsAuthenticated &&
        (_userContext.IsInRole("HR") || _userContext.IsInRole("Manager"));

    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    public bool CanUpdate() =>
        _userContext.IsAuthenticated &&
        (_userContext.IsInRole("HR") || _userContext.IsInRole("Manager"));

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    public bool CanDelete() =>
        _userContext.IsAuthenticated && _userContext.IsInRole("HR");
}
#endregion

#region save-authorization-combined
/// <summary>
/// Single authorization check covering all write operations.
/// </summary>
public interface IEmployeeWriteCombinedAuth
{
    /// <summary>
    /// Single method authorizes all write operations (Insert, Update, Delete).
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

public class EmployeeWriteCombinedAuthImpl : IEmployeeWriteCombinedAuth
{
    private readonly IUserContext _userContext;

    public EmployeeWriteCombinedAuthImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    public bool CanWrite() =>
        _userContext.IsAuthenticated &&
        (_userContext.IsInRole("HR") || _userContext.IsInRole("Manager"));
}
#endregion
