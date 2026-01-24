# Authorization

RemoteFactory supports two authorization approaches: custom authorization classes and ASP.NET Core policy-based authorization.

## Custom Authorization with AuthorizeFactory

Define authorization logic in a dedicated class and reference it from your domain model.

### Step 1: Define Authorization Interface

<!-- snippet: authorization-interface -->
<a id='snippet-authorization-interface'></a>
```cs
public interface IDocumentAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L11-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Methods decorated with `[AuthorizeFactory]` control access to specific operations.

### Step 2: Implement Authorization Logic

<!-- snippet: authorization-implementation -->
<a id='snippet-authorization-implementation'></a>
```cs
public partial class DocumentAuthorization : IDocumentAuthorization
{
    private readonly IUserContext _userContext;

    public DocumentAuthorization(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanCreate()
    {
        // Any authenticated user can create
        return _userContext.IsAuthenticated;
    }

    public bool CanRead()
    {
        // All authenticated users can read
        return _userContext.IsAuthenticated;
    }

    public bool CanWrite()
    {
        // Only editors and admins can write (update/delete)
        return _userContext.IsInRole("Editor") || _userContext.IsInRole("Admin");
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L25-L53' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inject any services needed for authorization decisions.

### Step 3: Apply to Domain Model

<!-- snippet: authorization-apply -->
<a id='snippet-authorization-apply'></a>
```cs
[Factory]
[AuthorizeFactory<IDocumentAuthorization>]
public partial class Document : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public Document()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid documentId, [Service] IPersonRepository repository)
    {
        Id = documentId;
        Title = "Sample Document";
        Content = "Document content";
        IsNew = false;
        return Task.FromResult(true);
    }

    [Remote, Insert]
    public Task Insert([Service] IPersonRepository repository)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Update]
    public Task Update([Service] IPersonRepository repository)
    {
        return Task.CompletedTask;
    }

    [Remote, Delete]
    public Task Delete([Service] IPersonRepository repository)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L55-L101' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Generated Authorization Checks

RemoteFactory generates authorization checks in the factory:

<!-- snippet: authorization-generated -->
<a id='snippet-authorization-generated'></a>
```cs
// The generated factory includes Can* methods for client-side checks:
public partial class GeneratedFactoryExample
{
    public static async Task CheckAuthorizationBeforeCalling()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.client.GetRequiredService<IDocumentFactory>();

        // Check authorization before attempting operation
        if (factory.CanCreate().HasAccess)
        {
            var doc = factory.Create();
            // ... modify doc ...
        }

        var docId = Guid.NewGuid();
        if (factory.CanFetch().HasAccess)
        {
            var doc = await factory.Fetch(docId);
            // ... use doc ...
        }
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L103-L127' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-generated' title='Start of snippet'>anchor</a></sup>
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
public interface ICombinedFlagsAuthorization
{
    // Single method covers both Create and Fetch operations
    [AuthorizeFactory(
        AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
    bool CanCreateOrFetch();

    // Single method covers all write operations (Insert, Update, Delete)
    [AuthorizeFactory(
        AuthorizeFactoryOperation.Insert |
        AuthorizeFactoryOperation.Update |
        AuthorizeFactoryOperation.Delete)]
    bool CanWrite();
}

public partial class CombinedFlagsAuthorization : ICombinedFlagsAuthorization
{
    private readonly IUserContext _userContext;

    public CombinedFlagsAuthorization(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanCreateOrFetch() => _userContext.IsAuthenticated;
    public bool CanWrite() => _userContext.IsInRole("Writer") || _userContext.IsInRole("Admin");
}

[Factory]
[AuthorizeFactory<ICombinedFlagsAuthorization>]
public partial class CombinedFlagsDocument
{
    public Guid Id { get; private set; }

    [Create]
    public CombinedFlagsDocument() { Id = Guid.NewGuid(); }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L129-L167' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-combined-flags' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Method-Level Authorization

Override class-level authorization for specific methods:

<!-- snippet: authorization-method-level -->
<a id='snippet-authorization-method-level'></a>
```cs
public interface IProjectAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();
}

public partial class ProjectAuthorization : IProjectAuthorization
{
    private readonly IUserContext _userContext;
    public ProjectAuthorization(IUserContext userContext) { _userContext = userContext; }
    public bool CanRead() => _userContext.IsAuthenticated;
}

[Factory]
[AuthorizeFactory<IProjectAuthorization>]
public partial class ProjectWithMethodAuth : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ProjectWithMethodAuth() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id)
    {
        Id = id;
        IsNew = false;
        return Task.FromResult(true);
    }

    // Method-level authorization - only admins can archive (using Update)
    [Remote, Update]
    [AspAuthorize(Roles = "Admin")]
    public Task Archive()
    {
        IsArchived = true;
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L169-L212' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-method-level' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The `Archive()` method combines class-level `[AuthorizeFactory<IProjectAuthorization>]` with method-level `[AspAuthorize(Roles = "Admin")]`. Both checks must pass for the operation to succeed.

## ASP.NET Core Policy-Based Authorization

Use `[AspAuthorize]` to apply ASP.NET Core authorization policies using the framework's `IAuthorizationService`.

### Step 1: Configure Policies

<!-- snippet: authorization-policy-config -->
<a id='snippet-authorization-policy-config'></a>
```cs
public static class AuthorizationPolicyConfig
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("RequireEditor", policy =>
                policy.RequireRole("Editor", "Admin"));

            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());
        });
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L214-L232' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Step 2: Apply to Factory Methods

<!-- snippet: authorization-policy-apply -->
<a id='snippet-authorization-policy-apply'></a>
```cs
[Factory]
public partial class PolicyProtectedResource
{
    public Guid Id { get; private set; }
    public string Data { get; private set; } = string.Empty;

    [Create]
    public PolicyProtectedResource() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public Task<bool> Fetch(Guid id)
    {
        Id = id;
        Data = "Fetched data";
        return Task.FromResult(true);
    }

    // For admin-only operations with policy, use Fetch with different parameters
    [Remote, Fetch]
    [AspAuthorize("RequireAdmin")]
    public Task<bool> FetchAdminOnly(Guid id, bool includePrivateData)
    {
        Id = id;
        Data = includePrivateData ? "Private admin data" : "Admin operation completed";
        return Task.FromResult(true);
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L234-L263' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-apply' title='Start of snippet'>anchor</a></sup>
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
// For Execute with multiple policies, use static partial class
[SuppressFactory] // Nested in wrapper class - pattern demonstration only
public static partial class MultiplePolicyResource
{
    // Requires both policies to be satisfied
    [Remote, Execute]
    [AspAuthorize("RequireAuthenticated")]
    [AspAuthorize("RequireAdmin")]
    private static Task _SensitiveOperation(Guid resourceId)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L265-L279' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-multiple' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Roles-Based Authorization

Use roles instead of policies:

<!-- snippet: authorization-policy-roles -->
<a id='snippet-authorization-policy-roles'></a>
```cs
[Factory]
public partial class RoleProtectedResource
{
    public Guid Id { get; private set; }

    [Create]
    public RoleProtectedResource() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    [AspAuthorize(Roles = "User,Admin,Manager")]
    public Task<bool> Fetch(Guid id)
    {
        Id = id;
        return Task.FromResult(true);
    }
}

// Role-based Execute operations in static partial class
[SuppressFactory] // Nested in wrapper class - pattern demonstration only
public static partial class RoleProtectedOperations
{
    [Remote, Execute]
    [AspAuthorize(Roles = "Admin,Manager")]
    private static Task _ManagerOperation(Guid resourceId)
    {
        return Task.CompletedTask;
    }

    [Remote, Execute]
    [AspAuthorize(Roles = "Admin")]
    private static Task _AdminOnlyOperation(Guid resourceId)
    {
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L281-L317' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-roles' title='Start of snippet'>anchor</a></sup>
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
public interface IReportAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
    bool CanAccess();
}

public partial class ReportAuthorization : IReportAuthorization
{
    private readonly IUserContext _userContext;
    public ReportAuthorization(IUserContext userContext) { _userContext = userContext; }
    public bool CanAccess() => _userContext.IsAuthenticated;
}

[Factory]
[AuthorizeFactory<IReportAuthorization>]
public partial class Report
{
    public Guid Id { get; private set; }

    [Create]
    public Report() { Id = Guid.NewGuid(); }

    // Custom auth (IReportAuthorization.CanAccess) runs first
    // Then ASP.NET Core policy check
    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public Task<bool> Fetch(Guid id)
    {
        Id = id;
        return Task.FromResult(true);
    }

}

// Combined auth with Execute in static partial class
[SuppressFactory] // Nested in wrapper class - pattern demonstration only
public static partial class ReportOperations
{
    // Custom auth runs through factory auth, then ASP.NET Core policy
    [Remote, Execute]
    [AspAuthorize(Roles = "Admin")]
    private static Task<string> _GenerateReport(Guid reportId)
    {
        return Task.FromResult($"Report {reportId} generated");
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L319-L366' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-combined' title='Start of snippet'>anchor</a></sup>
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
public partial class AuthorizationExceptionHandling
{
    // [Fact]
    public async Task HandleNotAuthorizedException()
    {
        var scopes = SampleTestContainers.Scopes();

        // Configure user without required role
        var userContext = scopes.server.GetRequiredService<MockUserContext>();
        userContext.IsAuthenticated = false;

        var factory = scopes.client.GetRequiredService<IDocumentFactory>();

        try
        {
            // This will throw NotAuthorizedException if user lacks permission
            var doc = factory.Create();
            await Task.CompletedTask;
        }
        catch (NotAuthorizedException ex)
        {
            // Handle unauthorized access
            Assert.NotNull(ex.Message);
        }
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L368-L395' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-exception' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

This translates to a 403 response when called remotely.

## Authorization in Events

Events support authorization:

<!-- snippet: authorization-events -->
<a id='snippet-authorization-events'></a>
```cs
[SuppressFactory] // Nested in wrapper class - pattern demonstration only
public partial class EventAuthorizationExample
{
    public Guid Id { get; private set; }

    [Create]
    public EventAuthorizationExample() { Id = Guid.NewGuid(); }

    // Events bypass authorization - use for internal operations
    // that should always execute regardless of user permissions
    [Event]
    public Task NotifyAdmins(
        string message,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        return emailService.SendAsync("admins@example.com", "Notification", message, ct);
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L397-L417' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-events' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Events bypass authorization checks and always execute. Use events for internal operations like notifications, audit logging, or background processing that should run regardless of user permissions.

## Testing Authorization

Test authorization classes directly:

<!-- snippet: authorization-testing -->
<a id='snippet-authorization-testing'></a>
```cs
public partial class AuthorizationTests
{
    // [Fact]
    public void AuthorizedUser_CanCreate()
    {
        var scopes = SampleTestContainers.Scopes();

        // Configure user with authentication
        var userContext = scopes.server.GetRequiredService<MockUserContext>();
        userContext.IsAuthenticated = true;
        userContext.Roles = ["User"];

        var factory = scopes.local.GetRequiredService<IDocumentFactory>();

        // Should succeed
        var canCreate = factory.CanCreate();
        Assert.True(canCreate.HasAccess);

        var doc = factory.Create();
        Assert.NotNull(doc);
    }

    // [Fact]
    public void UnauthorizedUser_CannotDelete()
    {
        var scopes = SampleTestContainers.Scopes();

        // Configure user without Admin role
        var userContext = scopes.server.GetRequiredService<MockUserContext>();
        userContext.IsAuthenticated = true;
        userContext.Roles = ["User"]; // Not Admin

        var factory = scopes.local.GetRequiredService<IDocumentFactory>();

        // Check authorization first - CanDelete checks delete permission
        var canDelete = factory.CanDelete();
        Assert.False(canDelete.HasAccess);
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L419-L459' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-testing' title='Start of snippet'>anchor</a></sup>
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
public interface IAuthContextAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();
}

public partial class AuthContextAuthorization : IAuthContextAuthorization
{
    private readonly IUserContext _userContext;

    public AuthContextAuthorization(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanRead()
    {
        // Access user claims and context
        var userId = _userContext.UserId;
        var username = _userContext.Username;
        var roles = _userContext.Roles;

        // Check specific claims or roles
        if (!_userContext.IsAuthenticated)
            return false;

        // Custom authorization logic based on claims
        return _userContext.IsInRole("Reader") || _userContext.IsInRole("Admin");
    }
}

[Factory]
[AuthorizeFactory<IAuthContextAuthorization>]
public partial class AuthContextResource
{
    public Guid Id { get; private set; }

    [Create]
    public AuthContextResource() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id)
    {
        Id = id;
        return Task.FromResult(true);
    }
}
```
<sup><a href='/src/docs/samples/AuthorizationSamples.cs#L461-L509' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-context' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Inject any service needed for authorization decisions:
- User context services (user ID, roles, claims)
- Domain repositories (check entity ownership, relationships)
- Business rule validators

## Next Steps

- [Factory Operations](factory-operations.md) - Operations that can be authorized
- [Service Injection](service-injection.md) - Inject auth services
- [ASP.NET Core Integration](aspnetcore-integration.md) - Configure policies
