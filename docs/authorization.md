# Authorization

RemoteFactory gives you a unified authorization layer: define your authorization rules once, and they're enforced on both client and server. The client can check `CanCreate()` before showing a "New Employee" button, and the server enforces the same check before actually executing Create. Same definition, both sides — the client uses it for UI decisions, the server uses it for security.

Since RemoteFactory replaces controllers, you lose the `[Authorize]` attribute on controller actions. Authorization moves to the factory level instead — which actually gives you more: you can define rules at a broad scope (Read/Write) or at a fine scope (Create/Fetch/Insert/Update/Delete), with full DI and async support.

## Two Approaches

RemoteFactory supports two authorization mechanisms:

| Approach | Why Use It | Best For |
|----------|-----------|----------|
| **AuthorizeFactory** | Unified client+server auth, domain-specific rules, testable without HTTP | New projects, domain-driven authorization |
| **AspAuthorize** | Familiar to ASP.NET Core developers, easy to port from controllers | Teams migrating from controller-based auth, role/policy-based rules |

## AuthorizeFactory

Define authorization logic in a dedicated class. The factory checks it before every operation, on both client and server.

### Define the Authorization Interface

Each method declares which operations it controls. Return `bool` — true grants access, false denies it:

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

### Implement with Injected Services

The implementation receives any services it needs via constructor injection — user context, repositories, whatever the authorization decision requires:

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

### Apply to Your Domain Class

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

The factory generates `CanCreate()`, `CanFetch()`, etc. methods that the client can call to drive UI decisions:

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

### Operation Flags

Authorization checks map to operations via flags. Use `Read` and `Write` for broad scope, or individual flags for fine-grained control:

| Flag | Checked By |
|------|------------|
| `Create` | Create operations |
| `Fetch` | Fetch operations |
| `Insert` | Insert operations |
| `Update` | Update operations |
| `Delete` | Delete operations |
| `Read` | Create and Fetch (broad read access) |
| `Write` | Insert, Update, Delete (broad write access) |
| `Execute` | Execute operations |

Combine flags with bitwise OR for a single method that covers multiple operations:

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

## AspAuthorize

For developers coming from controller-based authorization, `[AspAuthorize]` works the same way as `[Authorize]` on controllers — policies, roles, and authentication requirements. It's the easiest path when migrating from a controller-based architecture.

### Configure Policies

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

### Apply to Factory Methods

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

Multiple policies and roles work as expected:

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

## Combining Both Approaches

Use both for defense in depth — AuthorizeFactory for domain-level checks, AspAuthorize for infrastructure-level checks. Both must pass:

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

Execution order: AuthorizeFactory checks first, then AspAuthorize. If either fails, the operation is denied.

## Authorization Failures

- `Create()` and `Fetch()` return null when authorization fails
- `Save()` throws `NotAuthorizedException`
- Remote calls translate to 401/403 HTTP responses
- Events bypass authorization — they always execute (use for notifications, audit logging, etc.)

## Testing

AuthorizeFactory classes are testable without HTTP — inject a mock user context and call the `Can*` methods directly:

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

## Next Steps

- [Factory Operations](factory-operations.md) — Operations that can be authorized
- [Service Injection](service-injection.md) — Inject auth services
- [Save Operation](save-operation.md) — Authorization with Save routing
- [ASP.NET Core Integration](aspnetcore-integration.md) — Configure policies
