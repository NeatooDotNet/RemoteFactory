# Authorization

RemoteFactory supports two authorization approaches: custom authorization classes and ASP.NET Core policy-based authorization.

## Custom Authorization with AuthorizeFactory

Define authorization logic in a dedicated class and reference it from your domain model.

### Step 1: Define Authorization Interface

In the Application layer, define an interface that declares authorization checks for Employee operations:

<!-- snippet: authorization-interface -->
<!--
SNIPPET REQUIREMENTS:
- Define IEmployeeAuthorization interface in Application layer
- Include three methods with [AuthorizeFactory] attributes:
  - CanCreate() with AuthorizeFactoryOperation.Create
  - CanRead() with AuthorizeFactoryOperation.Read
  - CanWrite() with AuthorizeFactoryOperation.Write
- Domain: Employee Management (controls access to Employee aggregate)
- Context: Production code
-->
<!-- endSnippet -->

Methods decorated with `[AuthorizeFactory]` control access to specific operations.

### Step 2: Implement Authorization Logic

In the Application layer, implement the authorization interface with injected user context:

<!-- snippet: authorization-implementation -->
<!--
SNIPPET REQUIREMENTS:
- Implement EmployeeAuthorization class (partial) implementing IEmployeeAuthorization
- Inject IUserContext via constructor for accessing current user info
- CanCreate(): return true if user is authenticated
- CanRead(): return true if user is authenticated
- CanWrite(): return true if user has "HRManager" or "Admin" role
- Domain: Employee Management
- Context: Production code
- Show realistic HR domain authorization rules
-->
<!-- endSnippet -->

Inject any services needed for authorization decisions.

### Step 3: Apply to Domain Model

In the Domain layer, apply the authorization interface to the Employee aggregate:

<!-- snippet: authorization-apply -->
<!--
SNIPPET REQUIREMENTS:
- Define Employee class with [Factory] and [AuthorizeFactory<IEmployeeAuthorization>] attributes
- Implement IFactorySaveMeta interface
- Include properties: Id (Guid), FirstName (string), LastName (string), Email (string), DepartmentId (Guid), IsNew (bool), IsDeleted (bool)
- [Create] constructor that generates new Guid
- [Remote, Fetch] method Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
- [Remote, Insert] method Insert([Service] IEmployeeRepository repository)
- [Remote, Update] method Update([Service] IEmployeeRepository repository)
- [Remote, Delete] method Delete([Service] IEmployeeRepository repository)
- Domain: Employee Management
- Context: Production code
-->
<!-- endSnippet -->

### Generated Authorization Checks

RemoteFactory generates authorization checks in the factory:

<!-- snippet: authorization-generated -->
<!--
SNIPPET REQUIREMENTS:
- Show example class that uses IEmployeeFactory (injected via constructor)
- Demonstrate checking CanCreate().HasAccess before calling Create()
- Demonstrate checking CanFetch().HasAccess before calling Fetch(employeeId)
- Show null handling for unauthorized access
- Include comments explaining that Can* methods are generated
- Domain: Employee Management
- Context: Production code - showing how consumers use the factory
-->
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
<!--
SNIPPET REQUIREMENTS:
- Define IDepartmentAuthorization interface with combined flags:
  - CanCreateOrFetch() with Create | Fetch flags
  - CanWrite() with Insert | Update | Delete flags
- Implement DepartmentAuthorization class (partial) with IUserContext injection
- Apply to Department class with [Factory] and [AuthorizeFactory<IDepartmentAuthorization>]
- Department has: Id (Guid), Name (string), ManagerId (Guid?)
- [Create] constructor
- Domain: Employee Management - Department entity
- Context: Production code
- Show how combined flags reduce boilerplate
-->
<!-- endSnippet -->

## Method-Level Authorization

Override class-level authorization for specific methods:

<!-- snippet: authorization-method-level -->
<!--
SNIPPET REQUIREMENTS:
- Define IEmployeeReadAuthorization interface with CanRead() method
- Implement EmployeeReadAuthorization (partial) checking IsAuthenticated
- Define EmployeeWithMethodAuth class with:
  - [Factory] and [AuthorizeFactory<IEmployeeReadAuthorization>]
  - IFactorySaveMeta implementation
  - Properties: Id, IsTerminated, IsNew, IsDeleted
  - [Create] constructor
  - [Remote, Fetch] method Fetch(Guid id)
  - [Remote, Update] with [AspAuthorize(Roles = "HRManager")] for Terminate() method
- Domain: Employee Management
- Context: Production code
- Demonstrate method-level override for sensitive operation (termination)
-->
<!-- endSnippet -->

The `Terminate()` method combines class-level `[AuthorizeFactory<IEmployeeReadAuthorization>]` with method-level `[AspAuthorize(Roles = "HRManager")]`. Both checks must pass for the operation to succeed.

## ASP.NET Core Policy-Based Authorization

Use `[AspAuthorize]` to apply ASP.NET Core authorization policies using the framework's `IAuthorizationService`.

### Step 1: Configure Policies

In the Server layer (Program.cs or Startup), configure authorization policies:

<!-- snippet: authorization-policy-config -->
<!--
SNIPPET REQUIREMENTS:
- Static class AuthorizationPolicyConfig with ConfigureServices method
- Configure policies relevant to HR domain:
  - "RequireHRManager" - require "HRManager" role
  - "RequirePayroll" - require "Payroll" or "HRManager" role
  - "RequireAuthenticated" - require authenticated user
- Use standard ASP.NET Core authorization configuration
- Domain: Employee Management
- Context: Production code - server configuration
-->
<!-- endSnippet -->

### Step 2: Apply to Factory Methods

In the Domain layer, apply policies to factory methods:

<!-- snippet: authorization-policy-apply -->
<!--
SNIPPET REQUIREMENTS:
- Define SalaryInfo class with [Factory] attribute
- Properties: EmployeeId (Guid), AnnualSalary (decimal), EffectiveDate (DateTime)
- [Create] constructor
- [Remote, Fetch] with [AspAuthorize("RequireAuthenticated")] for basic fetch
- [Remote, Fetch] with [AspAuthorize("RequirePayroll")] for FetchWithCompensation method (includes sensitive salary data)
- Domain: Employee Management - salary/compensation info (sensitive data)
- Context: Production code
- Show how policies protect sensitive data at different levels
-->
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
<!--
SNIPPET REQUIREMENTS:
- Static partial class PayrollOperations with [SuppressFactory]
- [Remote, Execute] method for ProcessPayroll with:
  - [AspAuthorize("RequireAuthenticated")]
  - [AspAuthorize("RequirePayroll")]
- Method signature: _ProcessPayroll(Guid departmentId, DateTime payPeriodEnd)
- Comment explaining both policies must be satisfied
- Domain: Employee Management - payroll processing
- Context: Production code
- Show defense in depth with multiple policies
-->
<!-- endSnippet -->

### Roles-Based Authorization

Use roles instead of policies:

<!-- snippet: authorization-policy-roles -->
<!--
SNIPPET REQUIREMENTS:
- Define TimeOffRequest class with [Factory] attribute
- Properties: Id (Guid), EmployeeId (Guid), StartDate (DateTime), EndDate (DateTime), Status (string)
- [Create] constructor
- [Remote, Fetch] with [AspAuthorize(Roles = "Employee,HRManager,Admin")] - any employee can view
- Static partial class TimeOffOperations with [SuppressFactory]:
  - [Remote, Execute] _ApproveRequest with [AspAuthorize(Roles = "HRManager,Admin")]
  - [Remote, Execute] _CancelRequest with [AspAuthorize(Roles = "HRManager")]
- Domain: Employee Management - time off requests
- Context: Production code
- Show role hierarchy for different operations
-->
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
<!--
SNIPPET REQUIREMENTS:
- Define IPerformanceReviewAuthorization interface:
  - CanAccess() with Create | Fetch flags
- Implement PerformanceReviewAuthorization (partial) checking IsAuthenticated
- Define PerformanceReview class with:
  - [Factory] and [AuthorizeFactory<IPerformanceReviewAuthorization>]
  - Properties: Id (Guid), EmployeeId (Guid), ReviewDate (DateTime), Rating (int), Comments (string)
  - [Create] constructor
  - [Remote, Fetch] with [AspAuthorize("RequireAuthenticated")]
- Static partial class PerformanceReviewOperations with [SuppressFactory]:
  - [Remote, Execute] _SubmitReview with [AspAuthorize(Roles = "HRManager")]
- Comment explaining execution order (custom auth first, then ASP.NET Core)
- Domain: Employee Management - performance reviews
- Context: Production code
- Show combined authorization for sensitive HR data
-->
<!-- endSnippet -->

Execution order in generated factory methods:
1. `[AuthorizeFactory]` checks run first (custom domain auth)
2. `[AspAuthorize]` checks run second (ASP.NET Core policies)
3. If both pass, domain method executes

Both authorization mechanisms execute in the factory implementation, not at the HTTP endpoint level.

## NotAuthorizedException

Throw `NotAuthorizedException` for explicit auth failures:

<!-- snippet: authorization-exception -->
<!--
SNIPPET REQUIREMENTS:
- Define class with IEmployeeFactory injected via constructor
- Method HandleNotAuthorizedException showing try/catch pattern
- Try block: Create employee, attempt to Save
- Catch NotAuthorizedException: log/handle the authorization failure
- Show ex.Message access for failure reason
- Domain: Employee Management
- Context: Production code - showing exception handling pattern
-->
<!-- endSnippet -->

This translates to a 403 response when called remotely.

## Authorization in Events

Events support authorization:

<!-- snippet: authorization-events -->
<!--
SNIPPET REQUIREMENTS:
- Define partial class EmployeeLifecycleEvents with [SuppressFactory]
- Properties: Id (Guid)
- [Create] constructor
- [Event] method NotifyHROnTermination with:
  - Parameters: Guid employeeId, string reason, [Service] INotificationService notificationService, CancellationToken ct
  - Send notification to HR about termination
- Comment explaining events bypass authorization - for internal operations
- Domain: Employee Management - HR notifications
- Context: Production code
- Show events for audit/notification that should always execute
-->
<!-- endSnippet -->

Events bypass authorization checks and always execute. Use events for internal operations like notifications, audit logging, or background processing that should run regardless of user permissions.

## Testing Authorization

Test authorization classes directly:

<!-- snippet: authorization-testing -->
<!--
SNIPPET REQUIREMENTS:
- Define class with IEmployeeFactory injected via constructor
- Method AuthorizedUser_CanCreate():
  - Check factory.CanCreate().HasAccess
  - Call factory.Create() and verify not null
- Method UnauthorizedUser_CannotTerminate():
  - Check factory.CanDelete().HasAccess (for users without HRManager role)
  - Verify HasAccess is false
- Comments explaining test setup would configure user context
- Domain: Employee Management
- Context: Test code - showing how to test authorization
- Note: This is test context so test-style assertions are appropriate
-->
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
<!--
SNIPPET REQUIREMENTS:
- Define IEmployeeDataAuthorization interface:
  - CanRead() with AuthorizeFactoryOperation.Read
- Implement EmployeeDataAuthorization (partial) with IUserContext injection
- CanRead() implementation:
  - Access _userContext.UserId, Username, Roles
  - Check IsAuthenticated first
  - Allow if user has "HRStaff", "HRManager", or "Admin" role
- Define EmployeePersonalData class with:
  - [Factory] and [AuthorizeFactory<IEmployeeDataAuthorization>]
  - Properties: Id (Guid), SSN (string), BankAccount (string), EmergencyContact (string)
  - [Create] constructor
  - [Remote, Fetch] method
- Domain: Employee Management - sensitive personal data
- Context: Production code
- Show claims-based authorization for PII protection
-->
<!-- endSnippet -->

Inject any service needed for authorization decisions:
- User context services (user ID, roles, claims)
- Domain repositories (check entity ownership, relationships)
- Business rule validators

## Next Steps

- [Factory Operations](factory-operations.md) - Operations that can be authorized
- [Service Injection](service-injection.md) - Inject auth services
- [ASP.NET Core Integration](aspnetcore-integration.md) - Configure policies
