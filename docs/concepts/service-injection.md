---
layout: default
title: "Service Injection"
description: "Using the [Service] attribute for dependency injection in RemoteFactory"
parent: Concepts
nav_order: 4
---

# Service Injection

RemoteFactory integrates with Microsoft.Extensions.DependencyInjection to resolve services within factory methods. The `[Service]` attribute marks parameters that should be resolved from the DI container rather than passed by the caller.

## The [Service] Attribute

Parameters marked with `[Service]` are:
- Excluded from the generated factory method signature
- Resolved from `IServiceProvider` at execution time
- Resolved on the server for `[Remote]` methods

```csharp
[Factory]
public class PersonModel
{
    // Factory method signature: Fetch(int id)
    // The context parameter is resolved from DI
    [Remote]
    [Fetch]
    public async Task<bool> Fetch(
        int id,                              // Caller provides this
        [Service] IPersonContext context,    // DI provides this
        [Service] ILogger<PersonModel> logger // DI provides this
    )
    {
        logger.LogInformation("Fetching person {Id}", id);
        var entity = await context.Persons.FindAsync(id);
        // ...
    }
}
```

**Generated Factory Interface:**

```csharp
public interface IPersonModelFactory
{
    // Service parameters are not in the signature
    Task<IPersonModel?> Fetch(int id);
}
```

## How Service Resolution Works

### In Server Mode

Services resolve directly from the request's `IServiceProvider`:

```csharp
// Generated factory code (simplified)
public async Task<Authorized<IPersonModel>> LocalFetch(int id)
{
    var target = ServiceProvider.GetRequiredService<PersonModel>();

    // Services are resolved from DI
    var context = ServiceProvider.GetRequiredService<IPersonContext>();
    var logger = ServiceProvider.GetRequiredService<ILogger<PersonModel>>();

    return await target.Fetch(id, context, logger);
}
```

### In Remote Mode

On the client, the factory serializes non-service parameters and sends them to the server. The server then resolves services:

```
Client                                  Server
  │                                       │
  │ factory.Fetch(123)                    │
  │     │                                 │
  │     ▼                                 │
  │ RemoteFetch(123)                      │
  │     │                                 │
  │     │ Serialize: { id: 123 }          │
  │     │                                 │
  │     └────────────────────────────────>│
  │       HTTP POST /api/neatoo           │
  │                                       │
  │                                       │ Deserialize request
  │                                       │ Resolve: IPersonContext
  │                                       │ Resolve: ILogger
  │                                       │ Call: Fetch(123, context, logger)
  │                                       │
  │<──────────────────────────────────────┤
  │       Serialized PersonModel          │
```

## Common Patterns

### Database Context

The most common use case is injecting your database context:

```csharp
[Factory]
public class OrderModel
{
    [Remote]
    [Fetch]
    public async Task<bool> Fetch(Guid orderId, [Service] IOrderContext context)
    {
        var entity = await context.Orders
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (entity == null) return false;
        // Map entity properties
        Id = entity.Id;
        // ... map other properties
        return true;
    }

    [Remote]
    [Insert]
    [Update]
    public async Task Save([Service] IOrderContext context)
    {
        // Save logic with database context
    }
}
```

**Server Registration:**

```csharp
// Program.cs
builder.Services.AddDbContext<OrderContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<IOrderContext>(sp =>
    sp.GetRequiredService<OrderContext>());
```

### Logging

Inject loggers for diagnostics:

```csharp
[Remote]
[Fetch]
public async Task<bool> Fetch(int id, [Service] ILogger<PersonModel> logger)
{
    logger.LogDebug("Fetching person with ID {Id}", id);

    try
    {
        // Fetch logic
        logger.LogInformation("Person {Id} fetched successfully", id);
        return true;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to fetch person {Id}", id);
        throw;
    }
}
```

### Multiple Services

You can inject multiple services:

```csharp
[Remote]
[Insert]
public async Task Insert(
    [Service] IPersonContext context,
    [Service] IEmailService emailService,
    [Service] IAuditService auditService,
    [Service] ILogger<PersonModel> logger)
{
    // Create entity
    var entity = new PersonEntity();
    entity.FirstName = FirstName;
    entity.LastName = LastName;
    entity.Email = Email;
    context.Persons.Add(entity);
    await context.SaveChangesAsync();

    // Send welcome email
    await emailService.SendWelcomeEmail(Email);

    // Audit the creation
    await auditService.Log($"Created person: {FirstName} {LastName}");

    logger.LogInformation("Person created: {Email}", Email);
}
```

### Custom Application Services

Inject your own application services:

```csharp
public interface IPricingService
{
    Task<decimal> CalculatePrice(Order order);
    Task<decimal> ApplyDiscount(Order order, string promoCode);
}

[Factory]
public class OrderModel
{
    [Remote]
    [Update]
    public async Task ApplyPromoCode(
        string promoCode,
        [Service] IPricingService pricingService,
        [Service] IOrderContext context)
    {
        var discount = await pricingService.ApplyDiscount(this, promoCode);
        TotalDiscount = discount;
        await context.SaveChangesAsync();
    }
}
```

### User/Principal Access

Access the current user through a custom service:

```csharp
public interface ICurrentUser
{
    string UserId { get; }
    string Email { get; }
    IEnumerable<string> Roles { get; }
}

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

    public string Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? "";

    public IEnumerable<string> Roles =>
        _httpContextAccessor.HttpContext?.User?.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value) ?? Enumerable.Empty<string>();
}

// Usage in factory
[Remote]
[Insert]
public async Task Insert(
    [Service] ICurrentUser user,
    [Service] IPersonContext context)
{
    CreatedBy = user.UserId;
    // ...
}
```

## Service Lifetimes

Services follow standard DI lifetime rules:

| Lifetime | Behavior |
|----------|----------|
| Transient | New instance per resolution |
| Scoped | Same instance within HTTP request |
| Singleton | Same instance for application lifetime |

```csharp
// Server registration
builder.Services.AddScoped<IPersonContext, PersonContext>();      // Per-request
builder.Services.AddTransient<IEmailService, EmailService>();     // Per-resolution
builder.Services.AddSingleton<ICacheService, CacheService>();     // Shared
```

**Recommendation:** Use `Scoped` for database contexts to ensure proper transaction handling per request.

## Server-Only Services

Some services should only exist on the server. RemoteFactory handles this automatically:

```csharp
[Factory]
public class PersonModel
{
    // IPersonContext only registered on server
    [Remote]  // ← Forces server execution
    [Fetch]
    public async Task<bool> Fetch([Service] IPersonContext context)
    {
        // This always runs on server, context is always available
    }

    // Without [Remote], this would fail on client
    [Create]  // No [Remote]
    public void Initialize([Service] IClientService clientService)
    {
        // IClientService must be registered on both client and server
        // if this method can be called from either location
    }
}
```

### What Happens Without [Remote]

If you call a method without `[Remote]` that has server-only services:

```csharp
[Create]  // No [Remote]
public void BadPattern([Service] IDbContext context)
{
    // On client: throws because IDbContext isn't registered
    // On server: works fine
}
```

**Solution:** Either add `[Remote]` or ensure the service is available on both client and server.

## Optional Services

Services should generally be required. If you need optional services, use a pattern like:

```csharp
public interface IOptionalService
{
    void DoSomething();
}

[Remote]
[Insert]
public async Task Insert([Service] IServiceProvider sp)
{
    // Resolve optionally
    var optional = sp.GetService<IOptionalService>();
    optional?.DoSomething();
}
```

However, this is rarely needed. Design your services to always be available.

## Errors and Troubleshooting

### Service Not Registered

**Error:** `InvalidOperationException: No service for type 'IPersonContext'`

**Cause:** The service isn't registered in DI.

**Solution:**

```csharp
// Ensure service is registered on server
builder.Services.AddScoped<IPersonContext, PersonContext>();
```

### Wrong Execution Location

**Error:** Method works on server but fails on client.

**Cause:** `[Remote]` attribute missing on method with server-only services.

**Solution:**

```csharp
[Remote]  // Add this
[Fetch]
public async Task<bool> Fetch([Service] IServerOnlyService svc)
```

### Service Lifetime Mismatch

**Error:** `Cannot consume scoped service from singleton`

**Cause:** Injecting a scoped service into a singleton.

**Solution:** Adjust service lifetimes appropriately. Factory methods run in request scope, so scoped services work correctly.

## Best Practices

### 1. Use Interfaces

Always inject interfaces, not concrete types:

```csharp
// Good
[Fetch]
public Task<bool> Fetch([Service] IPersonContext context)

// Avoid
[Fetch]
public Task<bool> Fetch([Service] PersonContext context)
```

### 2. Keep Service Count Reasonable

If a method needs many services, consider introducing a facade:

```csharp
// Instead of this
public Task Insert(
    [Service] IContext context,
    [Service] IEmailService email,
    [Service] IAuditService audit,
    [Service] INotificationService notify,
    [Service] ICacheService cache)

// Consider this
public interface IPersonInsertService
{
    Task Insert(PersonModel person);
}

public Task Insert([Service] IPersonInsertService service)
{
    await service.Insert(this);
}
```

### 3. Document Server-Only Services

Make it clear which services are server-only:

```csharp
/// <summary>
/// Server-only database context. Only use with [Remote] methods.
/// </summary>
public interface IPersonContext { }
```

## Next Steps

- **[Architecture Overview](architecture-overview.md)**: Understanding the full system
- **[Factory Operations](factory-operations.md)**: Using services in operations
- **[Attributes Reference](../reference/attributes.md)**: Complete `[Service]` documentation
