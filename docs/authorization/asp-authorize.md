---
layout: default
title: "ASP.NET Core Authorization"
description: "Using [AspAuthorize] for policy-based authorization in RemoteFactory"
parent: Authorization
nav_order: 3
---

# ASP.NET Core Authorization

RemoteFactory integrates with ASP.NET Core's authorization system through the `[AspAuthorize]` attribute. This enables policy-based and role-based authorization using the same patterns you use in controllers.

## The AspAuthorize Attribute

`[AspAuthorize]` mirrors ASP.NET Core's `[Authorize]` attribute:

```csharp
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class AspAuthorizeAttribute : Attribute
{
    public AspAuthorizeAttribute() { }
    public AspAuthorizeAttribute(string policy) { Policy = policy; }

    public string? Policy { get; }
    public string? Roles { get; set; }
    public string? AuthenticationSchemes { get; set; }
}
```

## Basic Usage

### Require Authentication

Apply `[AspAuthorize]` without parameters to require an authenticated user:

```csharp
[Factory]
public class OrderModel : IOrderModel, IFactorySaveMeta
{
    [Remote]
    [Fetch]
    [AspAuthorize]  // User must be authenticated
    public async Task<bool> Fetch(int id, [Service] IOrderContext ctx)
    {
        var entity = await ctx.Orders.FindAsync(id);
        if (entity == null) return false;
        MapFrom(entity);
        return true;
    }
}
```

### Role-Based Authorization

Specify required roles using the `Roles` property:

```csharp
[Factory]
public class AdminReportModel : IAdminReportModel
{
    [Remote]
    [Fetch]
    [AspAuthorize(Roles = "Admin")]
    public async Task<bool> Fetch([Service] IReportContext ctx)
    {
        // Only users in Admin role can access
    }

    [Remote]
    [Insert]
    [AspAuthorize(Roles = "Admin,SuperUser")]  // Multiple roles (OR)
    public async Task Insert([Service] IReportContext ctx)
    {
        // Users in Admin OR SuperUser role can insert
    }
}
```

### Policy-Based Authorization

Use named policies for complex requirements:

```csharp
[Factory]
public class DocumentModel : IDocumentModel, IFactorySaveMeta
{
    [Remote]
    [Fetch]
    [AspAuthorize("CanViewDocuments")]
    public async Task<bool> Fetch(int id, [Service] IDocumentContext ctx)
    {
        // User must satisfy CanViewDocuments policy
    }

    [Remote]
    [Update]
    [AspAuthorize("CanEditDocuments")]
    public async Task Update([Service] IDocumentContext ctx)
    {
        // User must satisfy CanEditDocuments policy
    }

    [Remote]
    [Delete]
    [AspAuthorize("CanDeleteDocuments")]
    public async Task Delete([Service] IDocumentContext ctx)
    {
        // User must satisfy CanDeleteDocuments policy
    }
}
```

## Server Configuration

### Register Authorization Policies

Configure policies in your ASP.NET Core server:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Configure authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-auth-server.com";
        options.Audience = "your-api";
    });

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanViewDocuments", policy =>
        policy.RequireClaim("permission", "documents:read"));

    options.AddPolicy("CanEditDocuments", policy =>
        policy.RequireClaim("permission", "documents:write"));

    options.AddPolicy("CanDeleteDocuments", policy =>
        policy.RequireRole("Admin")
              .RequireClaim("permission", "documents:delete"));

    options.AddPolicy("MinimumAge", policy =>
        policy.Requirements.Add(new MinimumAgeRequirement(18)));
});

// Register RemoteFactory with ASP.NET Core integration
builder.Services.AddNeatooAspNetCore(typeof(DocumentModel).Assembly);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseNeatoo();

app.Run();
```

### IHttpContextAccessor Requirement

`[AspAuthorize]` requires `IHttpContextAccessor` to access the current user. `AddNeatooAspNetCore` registers this automatically, but if using custom configuration, ensure it's registered:

```csharp
builder.Services.AddHttpContextAccessor();
```

## How AspAuthorize Works

### The IAspAuthorize Service

RemoteFactory uses `IAspAuthorize` to evaluate authorization:

```csharp
public interface IAspAuthorize
{
    Task<string?> Authorize(
        IEnumerable<AspAuthorizeData> authorizeData,
        bool forbid = false);
}
```

The implementation uses ASP.NET Core's `IAuthorizationPolicyProvider` and `IPolicyEvaluator`:

```csharp
public class AspAuthorize : IAspAuthorize
{
    private readonly IAuthorizationPolicyProvider policyProvider;
    private readonly IPolicyEvaluator policyEvaluator;
    private readonly IHttpContextAccessor httpContextAccessor;

    public async Task<string?> Authorize(
        IEnumerable<AspAuthorizeData> authorizeData,
        bool forbid = false)
    {
        var context = httpContextAccessor.HttpContext;
        var policy = await AuthorizationPolicy.CombineAsync(
            policyProvider, authorizeData);

        var authenticateResult = await policyEvaluator
            .AuthenticateAsync(policy, context);

        if (!authenticateResult.Succeeded)
        {
            if (forbid)
            {
                await context.ForbidAsync();
                throw new AspForbidException("Forbidden");
            }
            return authenticateResult.Failure?.Message;
        }

        var authorizeResult = await policyEvaluator
            .AuthorizeAsync(policy, authenticateResult, context, null);

        if (!authorizeResult.Succeeded)
        {
            if (forbid)
            {
                await context.ForbidAsync();
                throw new AspForbidException("Not Authorized");
            }
            return "Not Authorized";
        }

        return string.Empty;  // Success
    }
}
```

### Generated Authorization Checks

When you use `[AspAuthorize]`, the generator includes authorization checks:

```csharp
// Generated factory method
public async Task<Authorized<IDocumentModel>> LocalFetch(int id)
{
    // Check AspAuthorize
    IAspAuthorize aspAuthorize = ServiceProvider.GetRequiredService<IAspAuthorize>();
    var authResult = await aspAuthorize.Authorize([
        new AspAuthorizeData { Policy = "CanViewDocuments" }
    ]);

    if (!string.IsNullOrEmpty(authResult))
    {
        return new Authorized<IDocumentModel>(new Authorized(false, authResult));
    }

    // Execute the fetch
    var target = ServiceProvider.GetRequiredService<DocumentModel>();
    var ctx = ServiceProvider.GetRequiredService<IDocumentContext>();
    return new Authorized<IDocumentModel>(
        await DoFactoryMethodCallBoolAsync(target, FactoryOperation.Fetch,
            () => target.Fetch(id, ctx)));
}
```

## Multiple Attributes

You can apply multiple `[AspAuthorize]` attributes. They are evaluated with AND logic:

```csharp
[Remote]
[Fetch]
[AspAuthorize(Roles = "Employee")]
[AspAuthorize("HasDepartmentAccess")]
public async Task<bool> Fetch(int id, [Service] IContext ctx)
{
    // User must be in Employee role AND satisfy HasDepartmentAccess policy
}
```

## Combining with AuthorizeFactory

You can use `[AspAuthorize]` alongside `[AuthorizeFactory<T>]`:

```csharp
public interface IOrderAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanAccessOrders();
}

[Factory]
[AuthorizeFactory<IOrderAuth>]
public class OrderModel : IOrderModel, IFactorySaveMeta
{
    [Remote]
    [Fetch]
    [AspAuthorize("CanViewOrders")]  // ASP.NET Core policy
    public async Task<bool> Fetch(int id, [Service] IOrderContext ctx)
    {
        // Checks BOTH:
        // 1. IOrderAuth.CanAccessOrders() (custom authorization)
        // 2. CanViewOrders policy (ASP.NET Core)
    }
}
```

Both authorization checks must pass for the operation to execute.

## Error Handling

### Default Behavior (Forbid = false)

By default, authorization failures return an `Authorized` result with the error message:

```csharp
var result = await _factory.TrySave(document);
if (!result.HasAccess)
{
    Console.WriteLine(result.Message);  // "Not Authorized" or policy failure reason
}
```

### Forbid Mode

When using the factory's `Save` method (not `TrySave`), authorization failures throw:

```csharp
try
{
    await _factory.Save(document);
}
catch (NotAuthorizedException ex)
{
    // Thrown when authorization fails
    _logger.LogWarning("Authorization failed: {Message}", ex.Message);
}
```

For server-side forbid responses (403), the `IAspAuthorize` implementation can call `ForbidAsync`:

```csharp
public async Task<string?> Authorize(
    IEnumerable<AspAuthorizeData> authorizeData,
    bool forbid = false)
{
    // ...
    if (!authorizeResult.Succeeded && forbid)
    {
        await context.ForbidAsync();
        throw new AspForbidException("Not Authorized");
    }
}
```

## Authentication Schemes

Specify authentication schemes when using multiple authentication methods:

```csharp
[Remote]
[Fetch]
[AspAuthorize(AuthenticationSchemes = "Bearer")]
public async Task<bool> Fetch(int id, [Service] IContext ctx)
{
    // Only accepts Bearer token authentication
}

[Remote]
[Fetch]
[AspAuthorize(AuthenticationSchemes = "Bearer,Cookie")]
public async Task<bool> FetchAlternate(int id, [Service] IContext ctx)
{
    // Accepts either Bearer or Cookie authentication
}
```

## Best Practices

### Use Policies Over Roles

Prefer named policies for flexibility:

```csharp
// Better: Policy can be changed without code modifications
[AspAuthorize("CanManageUsers")]

// Less flexible: Role names are hardcoded
[AspAuthorize(Roles = "Admin,UserManager")]
```

### Keep Policies Granular

Create specific policies for specific actions:

```csharp
// Good: Granular policies
[AspAuthorize("Documents:Read")]
[AspAuthorize("Documents:Write")]
[AspAuthorize("Documents:Delete")]

// Avoid: Overly broad policies
[AspAuthorize("DocumentsFullAccess")]
```

### Document Policy Requirements

Create a reference for your policies:

```csharp
public static class Policies
{
    public const string CanViewDocuments = "CanViewDocuments";
    public const string CanEditDocuments = "CanEditDocuments";
    public const string CanDeleteDocuments = "CanDeleteDocuments";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(CanViewDocuments, policy =>
            policy.RequireClaim("scope", "documents:read"));
        // ...
    }
}

// Usage
[AspAuthorize(Policies.CanViewDocuments)]
```

### Test Authorization Independently

Test policies without the full factory:

```csharp
[Fact]
public async Task CanViewDocuments_RequiresReadScope()
{
    var user = new ClaimsPrincipal(new ClaimsIdentity([
        new Claim("scope", "documents:read")
    ], "Test"));

    var authService = _services.GetRequiredService<IAuthorizationService>();
    var result = await authService.AuthorizeAsync(
        user, null, Policies.CanViewDocuments);

    Assert.True(result.Succeeded);
}
```

## Limitations

1. **Server-side only**: `[AspAuthorize]` is evaluated on the server. The client cannot check these policies locally.

2. **Requires HTTP context**: Only works within HTTP requests. Not applicable for background jobs or console applications.

3. **No Can* method generation**: Unlike `[AuthorizeFactory<T>]`, `[AspAuthorize]` doesn't generate `Can*()` methods. Use it in combination with custom authorization for client-side permission checks.

## Next Steps

- **[Custom Authorization](custom-authorization.md)**: Using [AuthorizeFactory<T>]
- **[Can Methods](can-methods.md)**: Generated authorization check methods
- **[Authorization Overview](authorization-overview.md)**: Choosing an authorization approach
