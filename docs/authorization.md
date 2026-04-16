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
internal Task<bool> Fetch(Guid employeeId, CancellationToken ct = default)
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
internal Task<bool> Fetch(Guid requestId, CancellationToken ct = default)
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
    internal Task<bool> Fetch(Guid reviewId, CancellationToken ct = default)
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
internal Task Terminate(CancellationToken ct = default)
{
    IsTerminated = true;
    return Task.CompletedTask;
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L297-L307' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-method-level' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Execution order: AuthorizeFactory checks first, then AspAuthorize. If either fails, the operation is denied.

## Parameterized Authorization Methods

Authorization methods can receive parameters matched by type from the factory method. This enables per-entity access control — denying access to specific records, not just operations:

```csharp
public interface IOrderAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetchOrder(Guid orderId);
}
```

When `Fetch(Guid orderId)` is called, the generator passes `orderId` to `CanFetchOrder(Guid orderId)` by matching the `Guid` type. The auth method name doesn't matter — only the parameter type must match.

The generator produces a parameterized `CanFetch(Guid)` on the factory interface, so the client can check access for a specific entity before calling Fetch:

```csharp
if (factory.CanFetch(orderId).HasAccess)
{
    var order = await factory.Fetch(orderId);
}
```

### Scope Matters for Parameterized Methods

Use specific operation scopes (e.g., `Fetch`) rather than broad scopes (e.g., `Read`) when the auth method has parameters. If a `Read`-scoped method has a `Guid` parameter, it would try to match against Create as well — but Create may not have a `Guid` parameter.

```csharp
// GOOD: Fetch scope applies only to Fetch, which has a Guid parameter
[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
bool CanFetchOrder(Guid orderId);

// Parameterless Read scope covers Create (no type matching needed)
[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
bool CanRead();
```

When both a broad-scope parameterless method and a fine-grained parameterized method apply to the same operation, both are checked and both must pass.

## Target Parameter Authorization

Authorization methods can receive the **target entity** on write operations. This enables state-based authorization — denying writes based on entity state (e.g., locked records):

```csharp
public interface IOrderAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite(IOrder target);
}
```

The parameter type must match the entity's class or interface type. On Insert, Update, and Delete operations, the generator passes the entity to the auth method so it can inspect state:

```csharp
public class OrderAuthorization : IOrderAuthorization
{
    public bool CanWrite(IOrder target)
    {
        // Deny writes to locked orders
        return target.Status != "Locked";
    }
}
```

### CanXxx Generation with Target Parameters

When a Write auth method has a target parameter, the generator suppresses `CanInsert`, `CanUpdate`, and `CanDelete` on the factory interface -- these methods are called before the operation, when the entity isn't available at the call site.

**CanSave is the exception.** The caller has the entity in hand when calling Save, so the generator produces two CanSave overloads:

- **`CanSave()`** (parameterless) -- runs only non-target Write auth methods (role checks, permissions). Returns `Authorized(true)` if no non-target auth methods exist.
- **`CanSave(target)`** -- runs ALL Write auth methods: non-target auth first, then target-parameterized auth that inspects entity state.

```csharp
// Check broad write permission (no entity needed)
if (factory.CanSave().HasAccess)
{
    // Check entity-specific permission
    if (factory.CanSave(order).HasAccess)
    {
        await factory.Save(order);
    }
}
```

For `CanInsert`, `CanUpdate`, and `CanDelete`, the auth check happens inside `Save()` where the entity is available. If authorization fails, `Save()` throws `NotAuthorizedException` (or `TrySave()` returns `HasAccess=false`).

Read auth methods (Create, Fetch) are unaffected -- `CanCreate` and `CanFetch` are still generated when their auth methods are parameterless or use type-matched parameters (not target parameters).

## Can* Method Behavior

`Can*` methods derive their guard and remote behavior from the **auth class methods**, not from the parent factory method. This follows the same accessibility paradigm as all other methods in RemoteFactory: the method being called determines the behavior.

| Auth Method Accessibility | Can* Behavior | Use When |
|---|---|---|
| `public` (no `[Remote]`) | Runs locally on client, no guard, synchronous | Auth logic uses client-available services (e.g., `IUser` registered on both sides) |
| `internal` (no `[Remote]`) | Server-only, `IsServerRuntime` guard | Auth logic uses server-only services but doesn't need remote routing |
| `[Remote] internal` | Routes to server via remote delegate, asynchronous | Auth logic needs server-only services and must be callable from the client |

This means a factory method can be `[Remote] internal` (server-side execution) while its `Can*` methods run on the client -- as long as the auth class has `public` methods with no server-only dependencies. The factory method's visibility is irrelevant to whether the auth check can run locally.

### Client-Side Can* (Public Auth Methods)

When auth methods are `public`, `Can*` runs locally on the client without a server round-trip. This means `CanCreate()` can drive UI decisions (show/hide a "New" button) instantly, with no network call.

For this to work, the authorization service must be resolvable on the client. Register your authorization implementation on the client via `RegisterMatchingName` or manual DI registration:

```csharp
// Client Program.cs — register auth services so Can* methods resolve locally
builder.Services.RegisterMatchingName(typeof(IEmployeeAuthorization).Assembly);
```

If the authorization service is not registered on the client, calling `CanCreate()` will produce a standard DI resolution exception. This is a developer configuration choice, not a framework error -- some apps intentionally keep auth server-only.

### Server-Side Can* ([Remote] Auth Methods)

When auth methods have `[Remote] internal`, `Can*` routes to the server. Use this when your authorization logic depends on server-only services (e.g., a database query to check permissions):

```csharp
// Auth interface with [Remote] — Can* routes to server
public interface ISecureEntityAuth
{
    [Remote]
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    internal bool CanCreate();  // Can* will route to server
}

// Auth implementation with server-only dependency
internal class SecureEntityAuth(IPermissionRepository repo) : ISecureEntityAuth
{
    public bool CanCreate() => repo.HasPermission("create");
}
```

### CanSave Aggregation

`CanSave` aggregates auth methods from Insert, Update, and Delete operations. If ANY constituent auth method is `internal` or `[Remote]`, CanSave gets the guard (most restrictive wins for security). This means CanSave is server-only if even one write operation requires server-side authorization.

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
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Authorization/AuthorizationSamples.cs#L571-L578' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Factory Operations](factory-operations.md) — Operations that can be authorized
- [Service Injection](service-injection.md) — Inject auth services
- [Save Operation](save-operation.md) — Authorization with Save routing
- [ASP.NET Core Integration](aspnetcore-integration.md) — Configure policies
