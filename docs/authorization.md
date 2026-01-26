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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L6-L31' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Methods decorated with `[AuthorizeFactory]` control access to specific operations.

### Step 2: Implement Authorization Logic

In the Application layer, implement the authorization interface with injected user context:

<!-- snippet: authorization-implementation -->
<a id='snippet-authorization-implementation'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L33-L75' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inject any services needed for authorization decisions.

### Step 3: Apply to Domain Model

In the Domain layer, apply the authorization interface to the Employee aggregate:

<!-- snippet: authorization-apply -->
<a id='snippet-authorization-apply'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L77-L124' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Generated Authorization Checks

RemoteFactory generates authorization checks in the factory:

The generator creates authorization checks in the factory's local methods:

```csharp
// Source: Generated factory pattern from Generated/Neatoo.Generator/...
// Authorization is checked BEFORE the domain method executes
public async Task<Authorized<Employee>> LocalFetch(Guid id, CancellationToken ct = default)
{
    // 1. Resolve authorization service
    var auth = ServiceProvider.GetRequiredService<IEmployeeAuthorization>();

    // 2. Check authorization (calls CanRead for Fetch operations)
    if (!await auth.CanRead())
        return new Authorized<Employee>(hasAccess: false);

    // 3. Execute domain method only if authorized
    var target = ServiceProvider.GetRequiredService<Employee>();
    var repository = ServiceProvider.GetRequiredService<IEmployeeRepository>();
    return new Authorized<Employee>(
        await DoFactoryMethodCallAsync(target, FactoryOperation.Fetch,
            () => target.Fetch(id, repository)));
}
```

*Source: Pattern from `Generated/Neatoo.Generator/Neatoo.Factory/` for types with `[AuthorizeFactory<T>]`*

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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L126-L139' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-combined-flags' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Method-Level Authorization

Override class-level authorization for specific methods:

<!-- snippet: authorization-method-level -->
<a id='snippet-authorization-method-level'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L141-L185' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-method-level' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L7-L36' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Step 2: Apply to Factory Methods

In the Domain layer, apply policies to factory methods:

<!-- snippet: authorization-policy-apply -->
<a id='snippet-authorization-policy-apply'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L38-L89' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-apply' title='Start of snippet'>anchor</a></sup>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L147-L176' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-multiple' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Roles-Based Authorization

Use roles instead of policies:

<!-- snippet: authorization-policy-roles -->
<a id='snippet-authorization-policy-roles'></a>
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AuthorizationPolicySamples.cs#L91-L145' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-roles' title='Start of snippet'>anchor</a></sup>
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L315-L351' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-combined' title='Start of snippet'>anchor</a></sup>
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L187-L237' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-exception' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This translates to a 403 response when called remotely.

## Authorization in Events

Events support authorization:

<!-- snippet: authorization-events -->
<a id='snippet-authorization-events'></a>
```cs
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L239-L262' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-events' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Events bypass authorization checks and always execute. Use events for internal operations like notifications, audit logging, or background processing that should run regardless of user permissions.

## Testing Authorization

Test authorization classes directly:

<!-- snippet: authorization-testing -->
<a id='snippet-authorization-testing'></a>
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
    public void CanCreate_WithoutHROrManagerRole_ReturnsFalse()
    {
        // Arrange - Create user context without required roles
        var userContext = new TestUserContext
        {
            IsAuthenticated = true,
            Roles = ["Employee"]
        };

        var authorization = new EmployeeAuthorizationImpl(userContext);

        // Act
        var canCreate = authorization.CanCreate();

        // Assert
        Assert.False(canCreate);
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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs#L97-L172' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-testing' title='Start of snippet'>anchor</a></sup>
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L264-L313' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-context' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inject any service needed for authorization decisions:
- User context services (user ID, roles, claims)
- Domain repositories (check entity ownership, relationships)
- Business rule validators

## Next Steps

- [Factory Operations](factory-operations.md) - Operations that can be authorized
- [Service Injection](service-injection.md) - Inject auth services
- [ASP.NET Core Integration](aspnetcore-integration.md) - Configure policies
