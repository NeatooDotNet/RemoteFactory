# Authorization

RemoteFactory supports two authorization approaches: custom authorization classes and ASP.NET Core policy-based authorization.

## Custom Authorization with AuthorizeFactory

Define authorization logic in a dedicated class and reference it from your domain model.

### Step 1: Define Authorization Interface

In the Application layer, define an interface that declares authorization checks for Employee operations:

<!-- snippet: authorization-interface -->
<a id='snippet-authorization-interface'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L11-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Methods decorated with `[AuthorizeFactory]` control access to specific operations.

### Step 2: Implement Authorization Logic

In the Application layer, implement the authorization interface with injected user context:

<!-- snippet: authorization-implementation -->
<a id='snippet-authorization-implementation'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L29-L60' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inject any services needed for authorization decisions.

### Step 3: Apply to Domain Model

In the Domain layer, apply the authorization interface to the Employee aggregate:

<!-- snippet: authorization-apply -->
<a id='snippet-authorization-apply'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L66-L145' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Generated Authorization Checks

RemoteFactory generates authorization checks in the factory:

<!-- snippet: authorization-generated -->
<a id='snippet-authorization-generated'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L151-L189' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-generated' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Authorization failures are wrapped in the `Authorized<T>` result. Methods like `Create()` and `Fetch()` return null when authorization fails, while `Save()` throws `NotAuthorizedException`.

## AuthorizeFactoryOperation Flags

Authorization checks are based on the flags in your `[AuthorizeFactory]` interface methods:

| Flag | Description |
|------|-------------|
| `Create` | Checked by Create operations |
| `Fetch` | Checked by Fetch operations |
| `Insert` | Checked by Insert operations |
| `Update` | Checked by Update operations |
| `Delete` | Checked by Delete operations |
| `Read` | General read access (Create and Fetch check this) |
| `Write` | General write access (Insert, Update, Delete check this) |
| `Execute` | Checked by Execute operations |
| `Event` | Events bypass authorization |

The generator calls all interface methods whose `[AuthorizeFactory]` flags match the operation being performed.

### Combining Flags

Use bitwise OR to check multiple operations:

<!-- snippet: authorization-combined-flags -->
<a id='snippet-authorization-combined-flags'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L195-L253' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-combined-flags' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Method-Level Authorization

Override class-level authorization for specific methods:

<!-- snippet: authorization-method-level -->
<a id='snippet-authorization-method-level'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L259-L325' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-method-level' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `Terminate()` method combines class-level `[AuthorizeFactory<IEmployeeReadAuthorization>]` with method-level `[AspAuthorize(Roles = "HRManager")]`. Both checks must pass for the operation to succeed.

## ASP.NET Core Policy-Based Authorization

Use `[AspAuthorize]` to apply ASP.NET Core authorization policies using the framework's `IAuthorizationService`.

### Step 1: Configure Policies

In the Server layer (Program.cs or Startup), configure authorization policies:

<!-- snippet: authorization-policy-config -->
<a id='snippet-authorization-policy-config'></a>
```cs
/// <summary>
/// ASP.NET Core authorization policy configuration for HR domain.
/// </summary>
public static class AuthorizationPolicyConfig
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // HR Manager policy - only HR managers can access
            options.AddPolicy("RequireHRManager", policy =>
                policy.RequireRole("HRManager"));

            // Payroll policy - payroll staff or HR managers can access
            options.AddPolicy("RequirePayroll", policy =>
                policy.RequireRole("Payroll", "HRManager"));

            // Authenticated policy - any authenticated user can access
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());
        });
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Authorization/AuthorizationPolicyConfig.cs#L6-L30' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-config' title='Start of snippet'>anchor</a></sup>
<a id='snippet-authorization-policy-config-1'></a>
```cs
/// <summary>
/// ASP.NET Core authorization policy configuration.
/// </summary>
public static class AuthorizationPolicyConfigSample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Policy requiring authentication
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());

            // Policy requiring HR role
            options.AddPolicy("RequireHR", policy =>
                policy.RequireRole("HR"));

            // Policy requiring Manager or HR role
            options.AddPolicy("RequireManagerOrHR", policy =>
                policy.RequireRole("Manager", "HR"));

            // Custom policy with claim requirements
            options.AddPolicy("RequireEmployeeAccess", policy =>
                policy.RequireClaim("department")
                      .RequireAuthenticatedUser());
        });
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L10-L39' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-config-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Step 2: Apply to Factory Methods

In the Domain layer, apply policies to factory methods:

<!-- snippet: authorization-policy-apply -->
<a id='snippet-authorization-policy-apply'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L331-L375' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-apply' title='Start of snippet'>anchor</a></sup>
<a id='snippet-authorization-policy-apply-1'></a>
```cs
/// <summary>
/// Applying ASP.NET Core policies with [AspAuthorize].
/// </summary>
[Factory]
public partial class PolicyProtectedEmployee2 : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public PolicyProtectedEmployee2() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [AspAuthorize] with named policy via constructor.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repo,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    [AspAuthorize("RequireManagerOrHR")]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToUpperInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L41-L92' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-apply-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Authorization Execution

RemoteFactory generates authorization checks in the factory's local method implementations. The `IAspAuthorize` service is resolved and called before executing the domain method:

```csharp
public async Task<Authorized<T>> LocalFetch(...)
{
    var aspAuthorize = ServiceProvider.GetRequiredService<IAspAuthorize>();
    var authorized = await aspAuthorize.Authorize([new AspAuthorizeData() { Policy = "RequireAuthenticated" }]);
    if (!authorized.HasAccess)
        return new Authorized<T>(authorized);

    // Execute domain method
}
```

### Multiple Policies

Apply multiple authorization requirements:

<!-- snippet: authorization-policy-multiple -->
<a id='snippet-authorization-policy-multiple'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L381-L398' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-multiple' title='Start of snippet'>anchor</a></sup>
<a id='snippet-authorization-policy-multiple-1'></a>
```cs
/// <summary>
/// Multiple [AspAuthorize] attributes - ALL must pass.
/// </summary>
[Factory]
public partial class MultiPolicyEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public MultiPolicyEmployee() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Multiple [AspAuthorize] - user must satisfy ALL requirements.
    /// </summary>
    [Remote, Delete]
    [AspAuthorize("RequireAuthenticated")]
    [AspAuthorize(Roles = "HR")]
    public async Task Delete(
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(auditLog);
        await auditLog.LogAsync("Delete", Id, "Employee", "Deleted", ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L150-L179' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-multiple-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Roles-Based Authorization

Use roles instead of policies:

<!-- snippet: authorization-policy-roles -->
<a id='snippet-authorization-policy-roles'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L404-L461' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-roles' title='Start of snippet'>anchor</a></sup>
<a id='snippet-authorization-policy-roles-1'></a>
```cs
/// <summary>
/// Role-based authorization with [AspAuthorize].
/// </summary>
[Factory]
public partial class RoleProtectedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public RoleProtectedEmployee() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Multiple roles - any of the listed roles can access.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize(Roles = "Employee,Manager,HR")]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repo,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Restricted to HR and Manager roles only.
    /// </summary>
    [Remote, Insert]
    [AspAuthorize(Roles = "HR,Manager")]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(repo);
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToUpperInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L94-L148' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-roles-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Comparing Approaches

| Feature | AuthorizeFactory | AspAuthorize |
|---------|------------------|--------------|
| **Where** | Custom auth class | ASP.NET Core policies |
| **When** | In factory method | In factory method |
| **Testable** | Yes (inject auth class) | Requires HTTP context |
| **Return Value** | null on failure | null on failure (remote: 401/403) |
| **DI Integration** | Full | Full (via policies) |
| **Granularity** | Per-operation | Per-method |
| **Logic Location** | Domain-specific class | Framework policies |

### Use AuthorizeFactory When:
- Authorization logic is domain-specific
- Different operations have different rules
- You want testable auth without HTTP infrastructure
- Null return values are acceptable

### Use AspAuthorize When:
- Using ASP.NET Core Identity
- Authorization is role or policy-based
- Leveraging existing policy infrastructure
- You want consistent authorization across HTTP and local calls

## Combining Both Approaches

Use both for defense in depth:

<!-- snippet: authorization-combined -->
<a id='snippet-authorization-combined'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L467-L543' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-combined' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Execution order in generated factory methods:
1. `[AuthorizeFactory]` checks run first (custom domain auth)
2. `[AspAuthorize]` checks run second (ASP.NET Core policies)
3. If both pass, domain method executes

Both authorization mechanisms execute in the factory implementation, not at the HTTP endpoint level.

## NotAuthorizedException

Throw `NotAuthorizedException` for explicit auth failures:

<!-- snippet: authorization-exception -->
<a id='snippet-authorization-exception'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L549-L587' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-exception' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This translates to a 403 response when called remotely.

## Authorization in Events

Events support authorization:

<!-- snippet: authorization-events -->
<a id='snippet-authorization-events'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L601-L635' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-events' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Events bypass authorization checks and always execute. Use events for internal operations like notifications, audit logging, or background processing that should run regardless of user permissions.

## Testing Authorization

Test authorization classes directly:

<!-- snippet: authorization-testing -->
<a id='snippet-authorization-testing'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L641-L686' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-testing' title='Start of snippet'>anchor</a></sup>
<a id='snippet-authorization-testing-1'></a>
```cs
/// <summary>
/// Testing authorization rules.
/// </summary>
public class AuthorizationTestingSamples
{
    [Fact]
    public void CanCreate_WithHRRole_ReturnsTrue()
    {
        // Arrange - Create user context with HR role
        var userContext = new TestUserContext
        {
            IsAuthenticated = true,
            Roles = ["HR"]
        };

        var authorization = new EmployeeAuthorizationImpl(userContext);

        // Act
        var canCreate = authorization.CanCreate();

        // Assert
        Assert.True(canCreate);
    }

    [Fact]
    public void CanCreate_WhenAuthenticatedWithAnyRole_ReturnsTrue()
    {
        // Arrange - Create user context with any role (no specific role required for Create)
        var userContext = new TestUserContext
        {
            IsAuthenticated = true,
            Roles = ["Employee"]
        };

        var authorization = new EmployeeAuthorizationImpl(userContext);

        // Act
        var canCreate = authorization.CanCreate();

        // Assert - CanCreate only requires authentication, not specific roles
        Assert.True(canCreate);
    }

    [Fact]
    public void CanRead_WhenAuthenticated_ReturnsTrue()
    {
        // Arrange
        var userContext = new TestUserContext
        {
            IsAuthenticated = true,
            Roles = []
        };

        var authorization = new EmployeeAuthorizationImpl(userContext);

        // Act
        var canRead = authorization.CanRead();

        // Assert - All authenticated users can read
        Assert.True(canRead);
    }
}

/// <summary>
/// Test user context for authorization tests.
/// </summary>
internal class TestUserContext : EmployeeManagement.Domain.Interfaces.IUserContext
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "testuser";
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool IsAuthenticated { get; set; }
    public bool IsInRole(string role) => Roles.Contains(role);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L97-L172' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-testing-1' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Authorization Enforcement by Mode

Authorization always executes in the factory's local implementation, regardless of factory mode.

**RemoteOnly mode (client):**
- All calls are remote HTTP requests
- Authorization enforced on server before execution

**Full mode (server):**
- Local calls: Authorization enforced before method execution
- Remote calls: Authorization enforced before method execution

**Logical mode (single-tier):**
- All calls are local
- Authorization still enforced before method execution

## Context-Specific Authorization

Use injected services for context-aware authorization:

<!-- snippet: authorization-context -->
<a id='snippet-authorization-context'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L692-L763' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-context' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inject any service needed for authorization decisions:
- User context services (user ID, roles, claims)
- Domain repositories (check entity ownership, relationships)
- Business rule validators

## Next Steps

- [Factory Operations](factory-operations.md) - Operations that can be authorized
- [Service Injection](service-injection.md) - Inject auth services
- [ASP.NET Core Integration](aspnetcore-integration.md) - Configure policies
