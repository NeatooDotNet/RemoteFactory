using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Hosting;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Authorization;

// ============================================================================
// Authorization Interface and Implementation
// ============================================================================

#region authorization-interface
/// <summary>
/// Authorization interface for Employee operations.
/// Methods with [AuthorizeFactory] control access to specific operations.
/// </summary>
public interface IEmployeeAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}
#endregion

#region authorization-implementation
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

    public bool CanCreate()
    {
        // Only authenticated users can create employees
        return _userContext.IsAuthenticated;
    }

    public bool CanRead()
    {
        // Only authenticated users can view employee data
        return _userContext.IsAuthenticated;
    }

    public bool CanWrite()
    {
        // Only HRManager or Admin can modify employee records
        return _userContext.IsInRole("HRManager") || _userContext.IsInRole("Admin");
    }
}
#endregion

// ============================================================================
// Employee Aggregate with Authorization Applied
// ============================================================================

#region authorization-apply
/// <summary>
/// Employee aggregate with authorization applied via AuthorizeFactory attribute.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class AuthorizedEmployeeEntity : IFactorySaveMeta
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
#endregion

// ============================================================================
// Generated Authorization Checks - Consumer Example
// ============================================================================

#region authorization-generated
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

    public AuthorizedEmployeeEntity? CreateNewEmployee()
    {
        // Check authorization before attempting create
        // CanCreate() is generated from [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        if (!_employeeFactory.CanCreate().HasAccess)
        {
            return null; // User not authorized to create
        }

        return _employeeFactory.Create();
    }

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
#endregion

// ============================================================================
// Combined Authorization Flags
// ============================================================================

#region authorization-combined-flags
/// <summary>
/// Authorization interface with combined flags to reduce boilerplate.
/// </summary>
public interface IDepartmentAuthorization
{
    // Single method handles both Create and Fetch operations
    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
    bool CanCreateOrFetch();

    // Single method handles all write operations
    [AuthorizeFactory(
        AuthorizeFactoryOperation.Insert |
        AuthorizeFactoryOperation.Update |
        AuthorizeFactoryOperation.Delete)]
    bool CanWrite();
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
#endregion

// ============================================================================
// Method-Level Authorization
// ============================================================================

#region authorization-method-level
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

    /// <summary>
    /// Terminate employee - requires HRManager role in addition to class-level authorization.
    /// Both checks must pass: IEmployeeReadAuthorization.CanRead() AND [AspAuthorize(Roles = "HRManager")].
    /// </summary>
    [Remote, Update]
    [AspAuthorize(Roles = "HRManager")]
    public Task Terminate(CancellationToken ct = default)
    {
        IsTerminated = true;
        return Task.CompletedTask;
    }
}
#endregion

// ============================================================================
// ASP.NET Core Policy-Based Authorization - Applied to Factory Methods
// ============================================================================

#region authorization-policy-apply
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

    /// <summary>
    /// Basic salary fetch - requires authenticated user.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public Task<bool> Fetch(Guid employeeId, CancellationToken ct = default)
    {
        EmployeeId = employeeId;
        AnnualSalary = 75000m;
        EffectiveDate = DateTime.UtcNow;
        return Task.FromResult(true);
    }

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
#endregion

// ============================================================================
// Multiple Policies
// ============================================================================

#region authorization-policy-multiple
/// <summary>
/// Payroll operations requiring multiple authorization policies.
/// Both RequireAuthenticated AND RequirePayroll policies must be satisfied.
/// </summary>
[SuppressFactory]
public static partial class PayrollOperations
{
    [Remote, Execute]
    [AspAuthorize("RequireAuthenticated")]
    [AspAuthorize("RequirePayroll")]
    public static Task _ProcessPayroll(Guid departmentId, DateTime payPeriodEnd, CancellationToken ct = default)
    {
        // Process payroll for all employees in department
        return Task.CompletedTask;
    }
}
#endregion

// ============================================================================
// Roles-Based Authorization
// ============================================================================

#region authorization-policy-roles
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

    /// <summary>
    /// Any employee, HR, or admin can view time off requests.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize(Roles = "Employee,HRManager,Admin")]
    public Task<bool> Fetch(Guid requestId, CancellationToken ct = default)
    {
        Id = requestId;
        return Task.FromResult(true);
    }
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
#endregion

// ============================================================================
// Combining Both Approaches
// ============================================================================

#region authorization-combined
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
/// Execution order: 1) [AuthorizeFactory] checks run first (custom domain auth)
///                  2) [AspAuthorize] checks run second (ASP.NET Core policies)
///                  3) If both pass, domain method executes
/// </summary>
[Factory]
[AuthorizeFactory<IPerformanceReviewAuthorization>]
public partial class PerformanceReview
{
    public Guid Id { get; private set; }
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

    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public Task<bool> Fetch(Guid reviewId, CancellationToken ct = default)
    {
        Id = reviewId;
        return Task.FromResult(true);
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
#endregion

// ============================================================================
// NotAuthorizedException Handling
// ============================================================================

#region authorization-exception
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
        try
        {
            var employee = _employeeFactory.Create();
            if (employee == null)
            {
                // Create returned null - authorization failed
                return;
            }

            employee.FirstName = "John";
            employee.LastName = "Doe";

            // Save throws NotAuthorizedException if user lacks write permission
            await _employeeFactory.Save(employee);
        }
        catch (NotAuthorizedException ex)
        {
            // Handle authorization failure
            // ex.Message contains the failure reason
            Console.WriteLine($"Authorization failed: {ex.Message}");
        }
    }
}
#endregion

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

#region authorization-events
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

    /// <summary>
    /// Notify HR when an employee is terminated.
    /// Events bypass authorization - this runs regardless of user permissions.
    /// </summary>
    [Event]
    public async Task NotifyHROnTermination(
        Guid employeeId,
        string reason,
        [Service] INotificationService notificationService,
        CancellationToken ct)
    {
        await notificationService.SendNotificationAsync(
            "hr@company.com",
            $"Employee {employeeId} terminated. Reason: {reason}",
            ct);
    }
}
#endregion

// ============================================================================
// Testing Authorization
// ============================================================================

#region authorization-testing
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

    /// <summary>
    /// Test that an authorized user can create employees.
    /// Test setup configures IUserContext with IsAuthenticated = true.
    /// </summary>
    public void AuthorizedUser_CanCreate()
    {
        // CanCreate() checks IEmployeeAuthorization.CanCreate()
        var canCreate = _factory.CanCreate().HasAccess;

        if (canCreate)
        {
            var employee = _factory.Create();
            // Verify employee was created
            System.Diagnostics.Debug.Assert(employee != null);
        }
    }

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
#endregion

// ============================================================================
// Context-Specific Authorization
// ============================================================================

#region authorization-context
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

    public bool CanRead()
    {
        // Access user context for authorization decisions
        var userId = _userContext.UserId;
        var username = _userContext.Username;
        var roles = _userContext.Roles;

        // Must be authenticated
        if (!_userContext.IsAuthenticated)
        {
            return false;
        }

        // Only HR staff can access sensitive personal data
        return _userContext.IsInRole("HRStaff") ||
               _userContext.IsInRole("HRManager") ||
               _userContext.IsInRole("Admin");
    }
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
#endregion
