# Authorization

RemoteFactory replaces controllers, so authorization moves from controller actions to the factory. You define a rule once, and the generated factory checks it wherever the operation runs. The client can ask `CanCreate()` before it offers a "New" button, and the server enforces the same rule before a Fetch or a Save runs.

The code on this page comes from RemoteFactory's compiled example set, `src/Design`.

## Two Approaches

| Approach | Rules live in | Runs on |
|----------|---------------|---------|
| **`[AuthorizeFactory<T>]`** | A class you write, with DI | The tier that runs the operation, the client included |
| **`[AspAuthorize]`** | ASP.NET Core policies and roles | The server, against `HttpContext.User` |

Authorization is a rule about who may do what to which record: only Sales creates orders, and nobody edits a Locked order. A state the user can change themselves is not authorization. An empty draft that cannot be submitted yet is a verdict the screen reads before it acts (`Order.CanSubmit`), not a refusal.

## AuthorizeFactory

### Define the Authorization Interface

Each method carries `[AuthorizeFactory]` with the operations it covers, and returns `bool`, `Task<bool>`, `string?` or `Task<string?>`. `true` or a null or empty string allows the operation; `false` or a message refuses it:

<!-- snippet: authorization-interface -->
<a id='snippet-authorization-interface'></a>
```cs
public interface IAuthorizedOrderAuth
{
    /// <summary>
    /// Broad-scope check covering all Read (Create, Fetch) and Write (Insert, Update, Delete) operations.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool HasAccess();

    /// <summary>
    /// Who may create an order: checked by the local Create (and CanCreate) on
    /// the client, and again on the server when the new order is saved (Insert).
    /// </summary>
    /// <remarks>
    /// A local Create runs only on the client, so the client check is for the
    /// screen; the Insert scope is what the server enforces.
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Insert)]
    bool CanCreate();

    /// <summary>
    /// Fine-grained check for Fetch operations only.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    /// <summary>
    /// Fine-grained check for Delete operations only.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs#L68-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`HasAccess` covers every operation through `Read | Write`. The others narrow it. `CanCreate` covers `Create | Insert`: a local Create runs only on the client, so the server applies the same rule when the new order is saved. When several methods match an operation, all of them must pass.

### Operation Flags

| Flag | Checked for |
|------|-------------|
| `Create`, `Fetch`, `Insert`, `Update`, `Delete`, `Execute` | that operation |
| `Read` | Create, Fetch and Execute |
| `Write` | Insert, Update and Delete |

Combine flags with `|`, as `HasAccess` does.

### Implement with Injected Services

The implementation takes what it decides from by constructor. `AuthorizedOrderAuth` reads the signed-in user:

<!-- snippet: authorization-implementation -->
<a id='snippet-authorization-implementation'></a>
```cs
public class AuthorizedOrderAuth(ICurrentUser user) : IAuthorizedOrderAuth
{
    public bool HasAccess() => user.IsAuthenticated;

    public bool CanCreate() => user.IsInRole(OrderRoles.Sales);

    public bool CanFetch() => user.IsInRole(OrderRoles.Sales) || user.IsInRole(OrderRoles.Support);

    public bool CanDelete() => user.IsInRole(OrderRoles.Manager);
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs#L122-L133' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ICurrentUser` is Design's own service. Both tiers register it, because the rules run on both:
- the server builds it from `HttpContext.User`;
- a Blazor client builds it from its `AuthenticationStateProvider`.

The generator registers the auth class itself (`IAuthorizedOrderAuth` → `AuthorizedOrderAuth`), so each tier needs only what the constructor takes.

### Apply to Your Domain Class

<!-- snippet: authorization-apply -->
<a id='snippet-authorization-apply'></a>
```cs
[Factory]
[AuthorizeFactory<IAuthorizedOrderAuth>]
internal partial class AuthorizedOrder : IAuthorizedOrder, IFactorySaveMeta, IFactoryOnCompleteAsync
```
<sup><a href='/src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs#L99-L103' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Checking Before Acting

For each authorized operation the factory gains a `Can` method named after the operation's method: `CanCreate`, `CanFetch`, `CanSave`, and for the writes `CanInsert`, `CanUpdate` and `CanDelete`. Each returns `Authorized`, whose `HasAccess` the screen reads, or `Task<Authorized>` when the check has to run on the server (see [Can* Method Behavior](#can-method-behavior)). `AuthorizedOrder`'s Create is local, so both calls below run on the client, with no server call. A check that runs only on the client is for the screen; the server enforces the rule again at Save:

<!-- snippet: authorization-generated -->
<a id='snippet-authorization-generated'></a>
```cs
if (factory.CanCreate().HasAccess)   // the rules Create runs, asked first: show "New" only if they pass
{
    order = factory.Create("New Customer");
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/AuthorizationTests.cs#L407-L412' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-generated' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## AspAuthorize

`[AspAuthorize]` works like `[Authorize]` on a controller action: a policy name, `Roles`, or `AuthenticationSchemes`. It is evaluated on the server, so an operation that carries it runs there.

### Configure the Server

Register authentication, the policies the attributes name, and `IHttpContextAccessor`, which `AspAuthorize` reads the request from:

<!-- snippet: authorization-policy-config -->
<a id='snippet-authorization-policy-config'></a>
```cs
services.AddAuthentication();   // add your scheme here: cookies, JWT bearer, ...
services.AddAuthorization(options =>
    options.AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser()));
services.AddHttpContextAccessor();
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L102-L107' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`AddAuthentication()` here registers no scheme; your application adds its own, such as cookies or JWT bearer. `UseAuthentication()` and `UseAuthorization()` run before `UseNeatoo()`, so `HttpContext.User` is set when a policy checks it ([ASP.NET Core Integration — Middleware Order](aspnetcore-integration.md#middleware-order)).

### Apply to Factory Methods

A policy:

<!-- snippet: authorization-policy-apply -->
<a id='snippet-authorization-policy-apply'></a>
```cs
[Remote, Fetch]
[AspAuthorize("RequireAuthenticated")]
internal void Fetch(int id)
{
    Id = id;
    CustomerName = $"Customer_{id}";
    Total = 100.00m;
    IsNew = false;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/SecureOrder.cs#L102-L112' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Roles. Any one of the listed roles passes:

<!-- snippet: authorization-policy-roles -->
<a id='snippet-authorization-policy-roles'></a>
```cs
[Remote, Insert]
[AspAuthorize(Roles = "Admin,Manager")]
internal Task Insert()
{
    Id = Random.Shared.Next(1000, 9999);
    return Task.CompletedTask;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/SecureOrder.cs#L127-L135' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-roles' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Several attributes. All of them must pass:

<!-- snippet: authorization-policy-multiple -->
<a id='snippet-authorization-policy-multiple'></a>
```cs
[Remote, Update]
[AspAuthorize("RequireAuthenticated")]
[AspAuthorize(Roles = "Manager")]
internal Task Update()
{
    return Task.CompletedTask;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/SecureOrder.cs#L158-L166' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-multiple' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Placement on [Execute]

`[Execute]` obeys `[Remote]`, so a bare `[Execute]` normally runs on whichever tier resolves it. Authorization changes that in one direction only: **`[AspAuthorize]` on a bare `[Execute]` forces the call to the server** on both shapes, because a policy can only be evaluated there. The policy is then enforced on the server before the method runs, on both shapes. On a static factory (since v1.10.5; #91) a refused command never runs, and the request is forbidden as for an interface factory (see [Authorization Failures](#authorization-failures)), which the client raises as `HttpRequestException`; a delegate has no `Authorized<T>` to carry the refusal back. Enforcement needs `IAspAuthorize`, which `AddNeatooAspNetCore` registers. `[AuthorizeFactory]` does not move the call: an auth method without `[Remote]` runs alongside a bare `[Execute]` on the tier that resolved it, while a `[Remote]` auth method forces the call to the server.

## Combining Both Approaches

One class can carry `[AuthorizeFactory<T>]` while its methods carry `[AspAuthorize]`. For each operation the generated factory runs the auth class's matching methods first, then the `[AspAuthorize]` checks, and every one must pass. An operation with an `[AspAuthorize]` check runs on the server, so keep such checks off a Create that should stay local.

## Parameterized Authorization Methods

An auth method can take a parameter of the operation. The generator passes the operation's argument of the same type, so the method can refuse one record rather than the whole operation:

<!-- snippet: authorization-parameterized -->
<a id='snippet-authorization-parameterized'></a>
```cs
[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
bool CanFetchOrder(Guid orderId);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/ParamAuthOrderAuth.cs#L89-L92' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-parameterized' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ParamAuthOrder`'s `Fetch(Guid orderId)` passes `orderId` to `CanFetchOrder`. The match is by type; the names need not agree. The factory's `CanFetch(Guid orderId)` takes the same parameter, so the client can ask about one order before it fetches it.

Give a parameterized method the narrowest scope, `Fetch` here, not `Read`. A `Read`-scoped method with a `Guid` parameter would also have to match Create, which has none. When a broad parameterless method and a narrow parameterized one both apply to an operation, both run and both must pass.

## Target Parameter Authorization

A Write method can take the object being saved, typed as its class or its interface. The rule can then read the object's state:

<!-- snippet: authorization-target -->
<a id='snippet-authorization-target'></a>
```cs
[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
bool CanWrite(IParamAuthOrder target);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/ParamAuthOrderAuth.cs#L131-L134' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-target' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ParamAuthOrderAuth` refuses a write to an order whose `Status` is `Locked`.

### Can* with Target Parameters

`CanInsert`, `CanUpdate` and `CanDelete` are not generated when a Write method takes the target: the screen asks them before it has an object to pass. The check still runs inside `Save`.

`CanSave` is the exception, because the caller holds the object when it saves. It comes in two overloads:
- **`CanSave()`** runs only the Write methods that take no target, and returns `Authorized(true)` when there are none;
- **`CanSave(target)`** runs them all: the ones without a target first, then the ones that inspect the object.

Create and Fetch rules are unaffected.

## Can* Method Behavior

Where a `Can*` method runs follows the auth class's methods, and whether the operation carries `[AspAuthorize]`, not where the operation itself runs:

| Auth method | Can* behavior | Use when |
|---|---|---|
| `public`, no `[Remote]` | Runs locally, with no guard; synchronous unless an auth method returns `Task` | The rule needs only services both tiers register, such as `ICurrentUser` |
| `internal`, no `[Remote]` | Server-only, behind the `IsServerRuntime` guard | The rule needs server services and is never asked from the client |
| `[Remote] internal` | Called on the server through the remote delegate, asynchronously | The rule needs server services and the client must ask it |
| Any, on an operation with `[AspAuthorize]` | Called on the server, asynchronously (`Task<Authorized>`), where the policy is checked too | The operation has a policy |

So a `[Remote] internal` Fetch can have a `CanFetch` that runs on the client, as long as the auth class's methods are `public` and need nothing server-only. An auth class that isn't resolvable on the client fails with a DI resolution error when `Can*` is called there.

### CanSave Aggregation

`CanSave` combines the auth methods of Insert, Update and Delete. If any of them is `internal` or `[Remote]`, `CanSave` gets the guard: the most restrictive wins.

## Authorization Failures

A class factory's refusal is a result:
- **Create and Fetch** return `null`.
- **`Save`** throws `NotAuthorizedException`, and **`TrySave`** returns an `Authorized<T>` with `HasAccess` false and a null `Result`.
- **`Can*`** returns `Authorized` with `HasAccess` false.

Over HTTP the refusal travels in a normal response. The client then sees the `null`, or `NotAuthorizedException` from `Save`, as on the server. `[AspAuthorize]` on a class-factory method refuses the same way; an anonymous caller is refused too (since v1.10.6). See [ASP.NET Core Integration — When a Call Fails](aspnetcore-integration.md#when-a-call-fails).

An interface factory's method, and a static factory's `[Execute]`, have no result to carry a refusal:
- **An `[AuthorizeFactory]` refusal** is a `NotAuthorizedException` on the server. `UseNeatoo()` answers it with a 403 that carries the exception, and the client throws it again, with the same `Message` and `Context` (#103). Code that catches `NotAuthorizedException` therefore works the same on either tier.
- **An `[AspAuthorize]` refusal forbids the request** through the default scheme's forbid. JWT bearer answers 403, which the client raises as `HttpRequestException`. The cookie scheme redirects instead. With no scheme registered, the forbid itself throws, and the response is a 500.

### What a Denial Says

`NotAuthorizedException.Message` is never empty:

| The refusal came from | `Message` |
|---|---|
| An auth method returning a non-empty `string?` / `Task<string?>` | That string, verbatim |
| An auth method returning `false` | `Authorization denied for operation Save on IAuthorizedOrder; no reason was supplied.` |
| `[AspAuthorize]` | The failed requirements, or `Not Authenticated` / `Not Authorized` |

A `bool` method cannot carry a reason, so the framework names what was refused rather than leaving the message blank. An empty exception message is dropped by some telemetry pipelines.

`NotAuthorizedException.Context` holds the operation and type on its own (`operation Save on IAuthorizedOrder`), for structured logging. On an interface factory, where one method can run several auth methods, it also names the one that refused, as in `operation GetCustomer on ICustomerDirectory (ICustomerDirectoryAuth.HasAccess)`. Return a `string?` when the caller would benefit from knowing why.

## Testing

An `[AuthorizeFactory<T>]` class is tested without HTTP. Design's test containers share one signed-in test user, and a test refuses an operation by taking a role away:

<!-- snippet: authorization-testing -->
<a id='snippet-authorization-testing'></a>
```cs
[Fact]
public void AuthorizedOrder_Create_ReturnsNull_WhenDenied()
{
    // Arrange
    var (_, _, local) = DesignClientServerContainers.Scopes();
    var factory = local.GetRequiredService<IAuthorizedOrderFactory>();
    local.GetRequiredService<TestUser>().Roles.Remove(OrderRoles.Sales);

    // Act
    var result = factory.Create("Should Not Create");

    // Assert
    Assert.Null(result);

    local.Dispose();
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/AuthorizationTests.cs#L155-L172' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`[AspAuthorize]` needs a request. Design tests it over HTTP with `WebApplicationFactory` and a test authentication scheme that signs each request in as the user the test names (`Design.Tests/Http/AspAuthorizeHttpTests.cs`).

## Next Steps

- [Factory Operations](factory-operations.md): the operations that can be authorized
- [Save Operation](save-operation.md): authorization and Save's outcomes
- [ASP.NET Core Integration](aspnetcore-integration.md): server setup and what a failed call looks like
- [Service Injection](service-injection.md): injecting services
