# Authorization

RemoteFactory supports two authorization approaches: custom authorization classes and ASP.NET Core policy-based authorization.

## Custom Authorization with AuthorizeFactory

Define authorization logic in a dedicated class and reference it from your domain model.

### Step 1: Define Authorization Interface

In the Application layer, define an interface that declares authorization checks for Employee operations:

<!-- snippet: authorization-interface -->
<a id='snippet-authorization-interface'></a>
```cs
// [AuthorizeFactory] declares which operations this method controls
[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
bool CanCreate();

[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
bool CanRead();

[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
bool CanWrite();
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L17-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Methods decorated with `[AuthorizeFactory]` control access to specific operations.

### Step 2: Implement Authorization Logic

In the Application layer, implement the authorization interface with injected user context:

<!-- snippet: authorization-implementation -->
<a id='snippet-authorization-implementation'></a>
```cs
// Inject services via constructor for authorization decisions
public bool CanCreate() => _userContext.IsAuthenticated;

public bool CanRead() => _userContext.IsAuthenticated;

public bool CanWrite() =>
    _userContext.IsInRole("HRManager") || _userContext.IsInRole("Admin");
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L42-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inject any services needed for authorization decisions.

### Step 3: Apply to Domain Model

In the Domain layer, apply the authorization interface to the Employee aggregate:

<!-- snippet: authorization-apply -->
<a id='snippet-authorization-apply'></a>
```cs
// Apply authorization interface to entity - all operations check IEmployeeAuthorization
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class AuthorizedEmployeeEntity : IFactorySaveMeta
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L60-L65' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Generated Authorization Checks

RemoteFactory generates authorization checks in the factory:

<!-- snippet: authorization-generated -->
<a id='snippet-authorization-generated'></a>
```cs
// Generated CanCreate()/CanFetch() methods check authorization before operations
public AuthorizedEmployeeEntity? CreateNewEmployee()
{
    if (!_employeeFactory.CanCreate().HasAccess)
        return null;
    return _employeeFactory.Create();
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L156-L164' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-generated' title='Start of snippet'>anchor</a></sup>
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
// Bitwise OR combines multiple operations into single authorization check
[AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
bool CanCreateOrFetch();

[AuthorizeFactory(AuthorizeFactoryOperation.Insert | AuthorizeFactoryOperation.Update
                | AuthorizeFactoryOperation.Delete)]
bool CanWrite();
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L188-L196' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-combined-flags' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Method-Level Authorization

Override class-level authorization for specific methods:

<!-- snippet: authorization-method-level -->
<a id='snippet-authorization-method-level'></a>
```cs
// [AspAuthorize] adds method-level auth on top of class-level [AuthorizeFactory]
// Both checks must pass: IEmployeeReadAuthorization AND HRManager role
[Remote, Update]
[AspAuthorize(Roles = "HRManager")]
public Task Terminate(CancellationToken ct = default)
{
    IsTerminated = true;
    return Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L297-L307' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-method-level' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `Terminate()` method combines class-level `[AuthorizeFactory<IEmployeeReadAuthorization>]` with method-level `[AspAuthorize(Roles = "HRManager")]`. Both checks must pass for the operation to succeed.

## ASP.NET Core Policy-Based Authorization

Use `[AspAuthorize]` to apply ASP.NET Core authorization policies using the framework's `IAuthorizationService`.

### Step 1: Configure Policies

In the Server layer (Program.cs or Startup), configure authorization policies:

<!-- snippet: authorization-policy-config -->
<a id='snippet-authorization-policy-config'></a>
```cs
// Configure ASP.NET Core policies for [AspAuthorize] to reference
public static void ConfigureServices(IServiceCollection services) =>
    services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireHRManager", p => p.RequireRole("HRManager"));
        options.AddPolicy("RequireAuthenticated", p => p.RequireAuthenticatedUser());
    });
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/Authorization/AuthorizationPolicyConfig.cs#L11-L19' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Step 2: Apply to Factory Methods

In the Domain layer, apply policies to factory methods:

<!-- snippet: authorization-policy-apply -->
<a id='snippet-authorization-policy-apply'></a>
```cs
// [AspAuthorize] applies ASP.NET Core policies to factory methods
[Remote, Fetch]
[AspAuthorize("RequireAuthenticated")]
public Task<bool> Fetch(Guid employeeId, CancellationToken ct = default)
{
    EmployeeId = employeeId;
    AnnualSalary = 75000m;
    return Task.FromResult(true);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L331-L341' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-apply' title='Start of snippet'>anchor</a></sup>
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
// Multiple [AspAuthorize] - ALL policies must pass
[Remote, Execute]
[AspAuthorize("RequireAuthenticated")]
[AspAuthorize("RequirePayroll")]
public static Task _ProcessPayroll(Guid departmentId, DateTime payPeriodEnd, CancellationToken ct = default)
    => Task.CompletedTask;
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L368-L375' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-multiple' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Roles-Based Authorization

Use roles instead of policies:

<!-- snippet: authorization-policy-roles -->
<a id='snippet-authorization-policy-roles'></a>
```cs
// Roles property - any listed role can access (comma-separated)
[Remote, Fetch]
[AspAuthorize(Roles = "Employee,HRManager,Admin")]
public Task<bool> Fetch(Guid requestId, CancellationToken ct = default)
{
    Id = requestId;
    return Task.FromResult(true);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L400-L409' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-roles' title='Start of snippet'>anchor</a></sup>
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L473-L489' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-combined' title='Start of snippet'>anchor</a></sup>
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
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L539-L550' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-exception' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This translates to a 403 response when called remotely.

## Authorization in Events

Events support authorization:

<!-- snippet: authorization-events -->
<a id='snippet-authorization-events'></a>
```cs
// [Event] methods bypass authorization - always execute
[Event]
public async Task NotifyHROnTermination(
    Guid employeeId, string reason,
    [Service] INotificationService notificationService, CancellationToken ct)
{
    await notificationService.SendNotificationAsync("hr@company.com", $"Terminated: {employeeId}", ct);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L582-L591' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-events' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Events bypass authorization checks and always execute. Use events for internal operations like notifications, audit logging, or background processing that should run regardless of user permissions.

## Testing Authorization

Test authorization classes directly:

<!-- snippet: authorization-testing -->
<a id='snippet-authorization-testing'></a>
```cs
// Test authorization by injecting mock IUserContext and calling Can* methods
public void AuthorizedUser_CanCreate()
{
    var canCreate = _factory.CanCreate().HasAccess;
    System.Diagnostics.Debug.Assert(canCreate);
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L611-L618' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-testing' title='Start of snippet'>anchor</a></sup>
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
// Inject any service needed for authorization decisions (user context, repos, etc.)
public bool CanRead()
{
    if (!_userContext.IsAuthenticated) return false;
    return _userContext.IsInRole("HRStaff") || _userContext.IsInRole("Admin");
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L660-L667' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-context' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inject any service needed for authorization decisions:
- User context services (user ID, roles, claims)
- Domain repositories (check entity ownership, relationships)
- Business rule validators

## Next Steps

- [Factory Operations](factory-operations.md) - Operations that can be authorized
- [Service Injection](service-injection.md) - Inject auth services
- [ASP.NET Core Integration](aspnetcore-integration.md) - Configure policies
