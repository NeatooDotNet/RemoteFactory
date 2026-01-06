/// <summary>
/// Code samples for docs/concepts/service-injection.md
/// </summary>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace RemoteFactory.Samples.DomainModel.ServiceInjection;

#region docs:concepts/service-injection:service-attribute
[Factory]
public class PersonServiceExample
{
    // Factory method signature: Fetch(int id)
    // The context parameter is resolved from DI
    [Remote]
    [Fetch]
    public async Task<bool> Fetch(
        int id,                              // Caller provides this
        [Service] IPersonContext context,    // DI provides this
        [Service] ILogger<PersonServiceExample> logger // DI provides this
    )
    {
        logger.LogInformation("Fetching person {Id}", id);
        var entity = await context.Persons.FindAsync(id);
        // ...
        return entity != null;
    }
}
#endregion

// Note: The generated factory interface is shown in the docs as:
// public interface IPersonServiceExampleFactory
// {
//     // Service parameters are not in the signature
//     Task<IPersonServiceExample?> Fetch(int id);
// }

#region docs:concepts/service-injection:database-context-fetch
[Factory]
public class OrderFetchExample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

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
        await context.SaveChangesAsync();
    }
}
#endregion

#region docs:concepts/service-injection:logging
[Factory]
public class PersonWithLogging
{
    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] ILogger<PersonWithLogging> logger, [Service] IPersonContext context)
    {
        logger.LogDebug("Fetching person with ID {Id}", id);

        try
        {
            // Fetch logic
            var entity = await context.Persons.FindAsync(id);
            logger.LogInformation("Person {Id} fetched successfully", id);
            return entity != null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch person {Id}", id);
            throw;
        }
    }
}
#endregion

#region docs:concepts/service-injection:multiple-services
[Factory]
public class OrderModel
{
    [Remote]
    [Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IOrderContext context,
        [Service] ILogger<OrderModel> logger,
        [Service] ICurrencyService currency)
    {
        logger.LogInformation("Fetching order {OrderId}", id);

        var entity = await context.Orders.FindAsync(id);
        if (entity == null)
        {
            logger.LogWarning("Order {OrderId} not found", id);
            return false;
        }

        OrderId = entity.Id;
        Total = currency.ConvertToDisplayCurrency(entity.TotalAmount);
        return true;
    }

    public Guid OrderId { get; private set; }
    public decimal Total { get; private set; }
}
#endregion

#region docs:concepts/service-injection:insert-multiple-services
[Factory]
public class PersonInsertExample : IFactorySaveMeta
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote]
    [Insert]
    public async Task Insert(
        [Service] IPersonContext context,
        [Service] IEmailService emailService,
        [Service] IAuditService auditService,
        [Service] ILogger<PersonInsertExample> logger)
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
}
#endregion

#region docs:concepts/service-injection:custom-service
public interface IPricingService
{
    Task<decimal> CalculatePrice(IOrderModelSample order);
    Task<decimal> ApplyDiscount(IOrderModelSample order, string promoCode);
}

[Factory]
public class OrderWithPricing : IOrderModelSample, IFactorySaveMeta
{
    public decimal TotalDiscount { get; private set; }
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

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
#endregion

public interface IOrderModelSample { }

#region docs:concepts/service-injection:current-user-interface
public interface ICurrentUser
{
    string UserId { get; }
    string Email { get; }
    IEnumerable<string> Roles { get; }
}
#endregion

#region docs:concepts/service-injection:current-user-implementation
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
#endregion

#region docs:concepts/service-injection:current-user-usage
[Factory]
public class PersonWithUserTracking : IFactorySaveMeta
{
    public string? CreatedBy { get; private set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // Usage in factory
    [Remote]
    [Insert]
    public async Task Insert(
        [Service] ICurrentUser user,
        [Service] IPersonContext context)
    {
        CreatedBy = user.UserId;
        // ...
        await context.SaveChangesAsync();
    }
}
#endregion

#region docs:concepts/service-injection:server-only-services
[Factory]
public class PersonServerOnly
{
    // IPersonContext only registered on server
    [Remote]  // Forces server execution
    [Fetch]
    public async Task<bool> Fetch([Service] IPersonContext context)
    {
        // This always runs on server, context is always available
        var entity = await context.Persons.FindAsync(1);
        return entity != null;
    }

    // Without [Remote], this would fail on client
    [Create]  // No [Remote]
    public void Initialize([Service] IClientService clientService)
    {
        // IClientService must be registered on both client and server
        // if this method can be called from either location
        clientService.Initialize();
    }
}
#endregion

#region docs:concepts/service-injection:without-remote
[Factory]
public class BadPatternExample
{
    [Create]  // No [Remote]
    public void BadPattern([Service] IDbContext context)
    {
        // On client: throws because IDbContext isn't registered
        // On server: works fine
    }
}
#endregion

#region docs:concepts/service-injection:optional-services
public interface IOptionalService
{
    void DoSomething();
}

[Factory]
public class PersonWithOptionalService : IFactorySaveMeta
{
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote]
    [Insert]
    public async Task Insert([Service] IServiceProvider sp, [Service] IPersonContext context)
    {
        // Resolve optionally
        var optional = sp.GetService<IOptionalService>();
        optional?.DoSomething();
        await context.SaveChangesAsync();
    }
}
#endregion

#region docs:concepts/service-injection:error-wrong-location
[Factory]
public class FixWrongLocationExample
{
    [Remote]  // Add this
    [Fetch]
    public async Task<bool> Fetch([Service] IServerOnlyService svc)
    {
        // Now it runs on server where IServerOnlyService is available
        await Task.CompletedTask;
        return true;
    }
}
#endregion

#region docs:concepts/service-injection:best-practice-interfaces
[Factory]
public class BestPracticeInterfacesExample
{
    // Good
    [Fetch]
    public Task<bool> FetchGood([Service] IPersonContext context)
    {
        return Task.FromResult(true);
    }

    // Avoid
    // [Fetch]
    // public Task<bool> FetchBad([Service] PersonContext context)
}
#endregion

#region docs:concepts/service-injection:best-practice-facade
// Instead of this
// public Task Insert(
//     [Service] IContext context,
//     [Service] IEmailService email,
//     [Service] IAuditService audit,
//     [Service] INotificationService notify,
//     [Service] ICacheService cache)

// Consider this
public interface IPersonInsertService
{
    Task Insert(IPersonModel person);
}

[Factory]
public class PersonWithFacade : IPersonModel, IFactorySaveMeta
{
    public int Id { get; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote]
    [Insert]
    public async Task Insert([Service] IPersonInsertService service)
    {
        await service.Insert(this);
    }
}
#endregion

#region docs:concepts/service-injection:document-server-only
/// <summary>
/// Server-only database context. Only use with [Remote] methods.
/// </summary>
public interface IDocumentedPersonContext
{
    DbSet<PersonEntity> Persons { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
#endregion

// Supporting interfaces and classes
public interface IPersonContext
{
    DbSet<PersonEntity> Persons { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class PersonEntity
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}

public interface IOrderContext
{
    DbSet<OrderEntity> Orders { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class OrderEntity
{
    public Guid Id { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderLineItem> LineItems { get; set; } = new();
}

public class OrderLineItem
{
    public int Id { get; set; }
}

public interface ICurrencyService
{
    decimal ConvertToDisplayCurrency(decimal amount);
}

public interface IEmailService
{
    Task SendWelcomeEmail(string? email);
}

public interface IAuditService
{
    Task Log(string message);
}

public interface IClientService
{
    void Initialize();
}

public interface IDbContext { }

public interface IServerOnlyService { }

public interface IHttpContextAccessor
{
    HttpContext? HttpContext { get; }
}

public class HttpContext
{
    public ClaimsPrincipal? User { get; set; }
}

public interface IPersonModel
{
    int Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
}
