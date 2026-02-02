using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Authorization;

// ============================================================================
// Authorization Interface and Implementation
// ============================================================================

/// <summary>
/// Authorization interface for Employee operations.
/// Methods with [AuthorizeFactory] control access to specific operations.
/// </summary>
public interface IEmployeeAuthorization
{
    #region authorization-interface
    // [AuthorizeFactory] declares which operations this method controls
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
    #endregion
}

/// <summary>
/// Authorization rules for Employee operations with realistic HR domain logic.
/// </summary>
public partial class EmployeeAuthorizationImpl : IEmployeeAuthorization
{
    private readonly IUserContext _userContext;

    public EmployeeAuthorizationImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    #region authorization-implementation
    // Inject services via constructor for authorization decisions
    public bool CanCreate() => _userContext.IsAuthenticated;

    public bool CanRead() => _userContext.IsAuthenticated;

    public bool CanWrite() =>
        _userContext.IsInRole("HRManager") || _userContext.IsInRole("Admin");
    #endregion
}

// ============================================================================
// Employee Aggregate with Authorization Applied
// ============================================================================

/// <summary>
/// Employee aggregate with authorization applied via AuthorizeFactory attribute.
/// </summary>
#region authorization-apply
// Apply authorization interface to entity - all operations check IEmployeeAuthorization
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class AuthorizedEmployeeEntity : IFactorySaveMeta
#endregion
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthorizedEmployeeEntity()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository repository,
        CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        DepartmentId = entity.DepartmentId;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            DepartmentId = DepartmentId,
            HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            DepartmentId = DepartmentId
        };
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct = default)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}

// ============================================================================
// Generated Authorization Checks - Consumer Example
// ============================================================================

/// <summary>
/// Example showing how consumers use the generated factory's authorization methods.
/// The CanCreate() and CanFetch() methods are generated based on [AuthorizeFactory] attributes.
/// </summary>
public class EmployeeManagementService
{
    private readonly IAuthorizedEmployeeEntityFactory _employeeFactory;

    public EmployeeManagementService(IAuthorizedEmployeeEntityFactory employeeFactory)
    {
        _employeeFactory = employeeFactory;
    }

    #region authorization-generated
    // Generated CanCreate()/CanFetch() methods check authorization before operations
    public AuthorizedEmployeeEntity? CreateNewEmployee()
    {
        if (!_employeeFactory.CanCreate().HasAccess)
            return null;
        return _employeeFactory.Create();
    }
    #endregion

    public async Task<AuthorizedEmployeeEntity?> GetEmployeeById(Guid employeeId)
    {
        // Check authorization before attempting fetch
        // CanFetch() is generated from [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
        if (!_employeeFactory.CanFetch().HasAccess)
        {
            return null; // User not authorized to read
        }

        return await _employeeFactory.Fetch(employeeId);
    }
}

// ============================================================================
// Combined Authorization Flags
// ============================================================================

/// <summary>
/// Authorization interface with combined flags to reduce boilerplate.
/// </summary>
public interface IDepartmentAuthorization
{
    #region authorization-combined-flags
    // Bitwise OR combines multiple operations into single authorization check
    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
    bool CanCreateOrFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Insert | AuthorizeFactoryOperation.Update
                    | AuthorizeFactoryOperation.Delete)]
    bool CanWrite();
    #endregion
}

/// <summary>
/// Department authorization with combined operation flags.
/// </summary>
public partial class DepartmentAuthorizationImpl : IDepartmentAuthorization
{
    private readonly IUserContext _userContext;

    public DepartmentAuthorizationImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanCreateOrFetch()
    {
        return _userContext.IsAuthenticated;
    }

    public bool CanWrite()
    {
        return _userContext.IsInRole("Admin") || _userContext.IsInRole("HRManager");
    }
}

/// <summary>
/// Department entity with combined flag authorization.
/// </summary>
[Factory]
[AuthorizeFactory<IDepartmentAuthorization>]
public partial class AuthorizedDepartment
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public Guid? ManagerId { get; set; }

    [Create]
    public AuthorizedDepartment()
    {
        Id = Guid.NewGuid();
    }
}

// ============================================================================
// Method-Level Authorization
// ============================================================================

/// <summary>
/// Authorization interface for basic read access.
/// </summary>
public interface IEmployeeReadAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();
}

/// <summary>
/// Basic read authorization implementation.
/// </summary>
public partial class EmployeeReadAuthorizationImpl : IEmployeeReadAuthorization
{
    private readonly IUserContext _userContext;

    public EmployeeReadAuthorizationImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanRead()
    {
        return _userContext.IsAuthenticated;
    }
}

/// <summary>
/// Employee with class-level authorization and method-level override for sensitive operations.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeReadAuthorization>]
public partial class EmployeeWithMethodAuth : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public bool IsTerminated { get; private set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithMethodAuth()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id, CancellationToken ct = default)
    {
        Id = id;
        IsNew = false;
        return Task.FromResult(true);
    }

    #region authorization-method-level
    // [AspAuthorize] adds method-level auth on top of class-level [AuthorizeFactory]
    // Both checks must pass: IEmployeeReadAuthorization AND HRManager role
    [Remote, Update]
    [AspAuthorize(Roles = "HRManager")]
    public Task Terminate(CancellationToken ct = default)
    {
        IsTerminated = true;
        return Task.CompletedTask;
    }
    #endregion
}

// ============================================================================
// ASP.NET Core Policy-Based Authorization - Applied to Factory Methods
// ============================================================================

/// <summary>
/// Salary information with policy-based authorization for sensitive data.
/// </summary>
[Factory]
public partial class SalaryInfo
{
    public Guid EmployeeId { get; private set; }
    public decimal AnnualSalary { get; set; }
    public DateTime EffectiveDate { get; set; }

    [Create]
    public SalaryInfo()
    {
        EmployeeId = Guid.NewGuid();
        EffectiveDate = DateTime.UtcNow;
    }

    #region authorization-policy-apply
    // [AspAuthorize] applies ASP.NET Core policies to factory methods
    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public Task<bool> Fetch(Guid employeeId, CancellationToken ct = default)
    {
        EmployeeId = employeeId;
        AnnualSalary = 75000m;
        return Task.FromResult(true);
    }
    #endregion

    /// <summary>
    /// Fetch with full compensation details - requires Payroll access.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize("RequirePayroll")]
    public Task<bool> FetchWithCompensation(Guid employeeId, decimal bonusAmount, CancellationToken ct = default)
    {
        EmployeeId = employeeId;
        AnnualSalary = 75000m + bonusAmount;
        EffectiveDate = DateTime.UtcNow;
        return Task.FromResult(true);
    }
}

// ============================================================================
// Multiple Policies
// ============================================================================

/// <summary>
/// Payroll operations requiring multiple authorization policies.
/// Both RequireAuthenticated AND RequirePayroll policies must be satisfied.
/// </summary>
[SuppressFactory]
public static partial class PayrollOperations
{
    #region authorization-policy-multiple
    // Multiple [AspAuthorize] - ALL policies must pass
    [Remote, Execute]
    [AspAuthorize("RequireAuthenticated")]
    [AspAuthorize("RequirePayroll")]
    public static Task _ProcessPayroll(Guid departmentId, DateTime payPeriodEnd, CancellationToken ct = default)
        => Task.CompletedTask;
    #endregion
}

// ============================================================================
// Roles-Based Authorization
// ============================================================================

/// <summary>
/// Time off request with role-based authorization.
/// </summary>
[Factory]
public partial class TimeOffRequest
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; private set; } = "Pending";

    [Create]
    public TimeOffRequest()
    {
        Id = Guid.NewGuid();
    }

    #region authorization-policy-roles
    // Roles property - any listed role can access (comma-separated)
    [Remote, Fetch]
    [AspAuthorize(Roles = "Employee,HRManager,Admin")]
    public Task<bool> Fetch(Guid requestId, CancellationToken ct = default)
    {
        Id = requestId;
        return Task.FromResult(true);
    }
    #endregion
}

/// <summary>
/// Time off operations with role-based authorization.
/// </summary>
[SuppressFactory]
public static partial class TimeOffOperations
{
    /// <summary>
    /// Only HRManager or Admin can approve requests.
    /// </summary>
    [Remote, Execute]
    [AspAuthorize(Roles = "HRManager,Admin")]
    public static Task _ApproveRequest(Guid requestId, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Only HRManager can cancel approved requests.
    /// </summary>
    [Remote, Execute]
    [AspAuthorize(Roles = "HRManager")]
    public static Task _CancelRequest(Guid requestId, string reason, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

// ============================================================================
// Combining Both Approaches
// ============================================================================

/// <summary>
/// Authorization interface for performance review access.
/// </summary>
public interface IPerformanceReviewAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
    bool CanAccess();
}

/// <summary>
/// Performance review authorization implementation.
/// </summary>
public partial class PerformanceReviewAuthorizationImpl : IPerformanceReviewAuthorization
{
    private readonly IUserContext _userContext;

    public PerformanceReviewAuthorizationImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanAccess()
    {
        return _userContext.IsAuthenticated;
    }
}

/// <summary>
/// Performance review with combined custom and ASP.NET Core authorization.
/// </summary>
#region authorization-combined
// Combine [AuthorizeFactory] (class-level) with [AspAuthorize] (method-level)
// Execution order: AuthorizeFactory checks first, then AspAuthorize
[Factory]
[AuthorizeFactory<IPerformanceReviewAuthorization>]
public partial class PerformanceReview
{
    public Guid Id { get; private set; }

    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public Task<bool> Fetch(Guid reviewId, CancellationToken ct = default)
    {
        Id = reviewId;
        return Task.FromResult(true);
    }
#endregion

    public Guid EmployeeId { get; set; }
    public DateTime ReviewDate { get; set; }
    public int Rating { get; set; }
    public string Comments { get; set; } = "";

    [Create]
    public PerformanceReview()
    {
        Id = Guid.NewGuid();
        ReviewDate = DateTime.UtcNow;
    }
}

/// <summary>
/// Performance review operations with combined authorization.
/// </summary>
[SuppressFactory]
public static partial class PerformanceReviewOperations
{
    /// <summary>
    /// Submit review - requires HRManager role.
    /// </summary>
    [Remote, Execute]
    [AspAuthorize(Roles = "HRManager")]
    public static Task _SubmitReview(Guid reviewId, int rating, string comments, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

// ============================================================================
// NotAuthorizedException Handling
// ============================================================================

/// <summary>
/// Demonstrates exception handling pattern for authorization failures.
/// </summary>
public class EmployeeAuthorizationHandler
{
    private readonly IAuthorizedEmployeeEntityFactory _employeeFactory;

    public EmployeeAuthorizationHandler(IAuthorizedEmployeeEntityFactory employeeFactory)
    {
        _employeeFactory = employeeFactory;
    }

    public async Task HandleNotAuthorizedException()
    {
        #region authorization-exception
        // Save() throws NotAuthorizedException when write access denied
        try
        {
            var employee = _employeeFactory.Create();
            await _employeeFactory.Save(employee!);
        }
        catch (NotAuthorizedException ex)
        {
            Console.WriteLine($"Authorization failed: {ex.Message}");
        }
        #endregion
    }
}

// ============================================================================
// Events - Bypass Authorization
// ============================================================================

/// <summary>
/// Notification service interface for event handlers.
/// </summary>
public interface INotificationService
{
    Task SendNotificationAsync(string recipient, string message, CancellationToken ct = default);
}

/// <summary>
/// Employee lifecycle events that bypass authorization.
/// Events are for internal operations like notifications and audit logging
/// that should always execute regardless of user permissions.
/// </summary>
[SuppressFactory]
public partial class EmployeeLifecycleEvents
{
    public Guid Id { get; private set; }

    [Create]
    public EmployeeLifecycleEvents()
    {
        Id = Guid.NewGuid();
    }

    #region authorization-events
    // [Event] methods bypass authorization - always execute
    [Event]
    public async Task NotifyHROnTermination(
        Guid employeeId, string reason,
        [Service] INotificationService notificationService, CancellationToken ct)
    {
        await notificationService.SendNotificationAsync("hr@company.com", $"Terminated: {employeeId}", ct);
    }
    #endregion
}

// ============================================================================
// Testing Authorization
// ============================================================================

/// <summary>
/// Demonstrates how to test authorization using the generated factory methods.
/// Test setup would configure IUserContext with appropriate user state.
/// </summary>
public class EmployeeAuthorizationTests
{
    private readonly IAuthorizedEmployeeEntityFactory _factory;

    public EmployeeAuthorizationTests(IAuthorizedEmployeeEntityFactory factory)
    {
        _factory = factory;
    }

    #region authorization-testing
    // Test authorization by injecting mock IUserContext and calling Can* methods
    public void AuthorizedUser_CanCreate()
    {
        var canCreate = _factory.CanCreate().HasAccess;
        System.Diagnostics.Debug.Assert(canCreate);
    }
    #endregion

    /// <summary>
    /// Test that unauthorized users cannot delete employees.
    /// Test setup configures IUserContext without HRManager role.
    /// </summary>
    public void UnauthorizedUser_CannotDelete()
    {
        // CanDelete() checks IEmployeeAuthorization.CanWrite()
        // which requires HRManager or Admin role
        var canDelete = _factory.CanDelete().HasAccess;

        // User without HRManager role should not have delete access
        System.Diagnostics.Debug.Assert(!canDelete);
    }
}

// ============================================================================
// Context-Specific Authorization
// ============================================================================

/// <summary>
/// Authorization interface for sensitive employee data.
/// </summary>
public interface IEmployeeDataAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();
}

/// <summary>
/// Claims-based authorization for PII protection.
/// </summary>
public partial class EmployeeDataAuthorizationImpl : IEmployeeDataAuthorization
{
    private readonly IUserContext _userContext;

    public EmployeeDataAuthorizationImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    #region authorization-context
    // Inject any service needed for authorization decisions (user context, repos, etc.)
    public bool CanRead()
    {
        if (!_userContext.IsAuthenticated) return false;
        return _userContext.IsInRole("HRStaff") || _userContext.IsInRole("Admin");
    }
    #endregion
}

/// <summary>
/// Employee personal data with claims-based authorization.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeDataAuthorization>]
public partial class EmployeePersonalData
{
    public Guid Id { get; private set; }
    public string SSN { get; set; } = "";
    public string BankAccount { get; set; } = "";
    public string EmergencyContact { get; set; } = "";

    [Create]
    public EmployeePersonalData()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid employeeId, CancellationToken ct = default)
    {
        Id = employeeId;
        // Load sensitive data from repository
        SSN = "***-**-****";
        BankAccount = "****1234";
        EmergencyContact = "John Doe (555-1234)";
        return Task.FromResult(true);
    }
}
