---
layout: default
title: "Static Execute"
description: "Using [Execute] with static classes for remote procedure calls"
parent: Advanced
nav_order: 3
---

# Static Execute Operations

RemoteFactory supports `[Execute]` operations on static classes, enabling remote procedure calls without domain model instances. This is useful for query operations, batch processing, and utility functions.

## When to Use Static Execute

Use static Execute operations when:

1. **No domain object needed** - Query operations that return DTOs
2. **Batch operations** - Process multiple records without loading individual models
3. **Utility functions** - Server-side calculations or integrations
4. **Reporting** - Generate reports or aggregated data

## Basic Syntax

### Define a Static Partial Class

```csharp
[Factory]
public static partial class ReportOperations
{
    [Remote]
    [Execute]
    public static async Task<SalesReport> GenerateSalesReport(
        DateTime startDate,
        DateTime endDate,
        [Service] ISalesContext ctx)
    {
        var sales = await ctx.Sales
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.ProductCategory)
            .Select(g => new CategorySales
            {
                Category = g.Key,
                TotalAmount = g.Sum(s => s.Amount),
                Count = g.Count()
            })
            .ToListAsync();

        return new SalesReport
        {
            StartDate = startDate,
            EndDate = endDate,
            Categories = sales,
            GrandTotal = sales.Sum(s => s.TotalAmount)
        };
    }
}
```

### Generated Factory

```csharp
public interface IReportOperationsFactory
{
    Task<SalesReport> GenerateSalesReport(DateTime startDate, DateTime endDate);
}

internal class ReportOperationsFactory : IReportOperationsFactory
{
    public delegate Task<SalesReport> GenerateSalesReportDelegate(
        DateTime startDate, DateTime endDate);

    public GenerateSalesReportDelegate GenerateSalesReportProperty { get; }

    // Constructor for Server mode
    public ReportOperationsFactory(IServiceProvider serviceProvider)
    {
        GenerateSalesReportProperty = (startDate, endDate) =>
            LocalGenerateSalesReport(startDate, endDate);
    }

    // Constructor for Remote mode
    public ReportOperationsFactory(
        IServiceProvider serviceProvider,
        IMakeRemoteDelegateRequest remoteMethodDelegate)
    {
        GenerateSalesReportProperty = async (startDate, endDate) =>
            await RemoteGenerateSalesReport(startDate, endDate);
    }

    public async Task<SalesReport> GenerateSalesReport(DateTime startDate, DateTime endDate)
    {
        return await GenerateSalesReportProperty(startDate, endDate);
    }

    private async Task<SalesReport> LocalGenerateSalesReport(
        DateTime startDate, DateTime endDate)
    {
        var ctx = ServiceProvider.GetRequiredService<ISalesContext>();
        return await ReportOperations.GenerateSalesReport(startDate, endDate, ctx);
    }

    private async Task<SalesReport> RemoteGenerateSalesReport(
        DateTime startDate, DateTime endDate)
    {
        return (await MakeRemoteDelegateRequest!
            .ForDelegate<SalesReport>(
                typeof(GenerateSalesReportDelegate),
                [startDate, endDate]))!;
    }
}
```

### Usage

```csharp
@inject IReportOperationsFactory ReportFactory

@code {
    private SalesReport? _report;

    private async Task GenerateReport()
    {
        _report = await ReportFactory.GenerateSalesReport(
            DateTime.Today.AddMonths(-1),
            DateTime.Today);
    }
}
```

## Multiple Execute Methods

A static class can have multiple Execute methods:

```csharp
[Factory]
public static partial class AdminOperations
{
    [Remote]
    [Execute]
    public static async Task<int> GetUserCount([Service] IUserContext ctx)
    {
        return await ctx.Users.CountAsync();
    }

    [Remote]
    [Execute]
    public static async Task<List<UserSummary>> GetActiveUsers([Service] IUserContext ctx)
    {
        return await ctx.Users
            .Where(u => u.IsActive)
            .Select(u => new UserSummary { Id = u.Id, Name = u.Name })
            .ToListAsync();
    }

    [Remote]
    [Execute]
    public static async Task<bool> DeactivateUser(
        int userId,
        [Service] IUserContext ctx)
    {
        var user = await ctx.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsActive = false;
        await ctx.SaveChangesAsync();
        return true;
    }

    [Remote]
    [Execute]
    public static async Task ClearCache([Service] ICacheService cache)
    {
        await cache.ClearAsync();
    }
}
```

Generated interface:

```csharp
public interface IAdminOperationsFactory
{
    Task<int> GetUserCount();
    Task<List<UserSummary>> GetActiveUsers();
    Task<bool> DeactivateUser(int userId);
    Task ClearCache();
}
```

## With Authorization

Static Execute methods support `[AspAuthorize]`:

```csharp
[Factory]
public static partial class SecureOperations
{
    [Remote]
    [Execute]
    [AspAuthorize(Roles = "Admin")]
    public static async Task<SystemStatus> GetSystemStatus(
        [Service] ISystemContext ctx)
    {
        // Only admins can access
        return new SystemStatus
        {
            ServerTime = DateTime.UtcNow,
            DatabaseConnections = await ctx.GetConnectionCount(),
            MemoryUsage = GC.GetTotalMemory(false)
        };
    }

    [Remote]
    [Execute]
    [AspAuthorize("CanManageUsers")]
    public static async Task ResetUserPassword(
        int userId,
        string newPassword,
        [Service] IUserService userService)
    {
        await userService.ResetPasswordAsync(userId, newPassword);
    }
}
```

## Return Types

Execute methods support various return types:

### Task (void return)

```csharp
[Remote]
[Execute]
public static async Task SendNotification(
    string message,
    [Service] INotificationService notifications)
{
    await notifications.SendToAllAsync(message);
}
```

### Task<T> (with result)

```csharp
[Remote]
[Execute]
public static async Task<List<LogEntry>> GetRecentLogs(
    int count,
    [Service] ILogContext ctx)
{
    return await ctx.Logs
        .OrderByDescending(l => l.Timestamp)
        .Take(count)
        .ToListAsync();
}
```

### Synchronous methods

```csharp
[Execute]  // Note: No [Remote] - local only
public static string FormatCurrency(decimal amount)
{
    return amount.ToString("C");
}
```

## Complex Parameters

Execute methods can accept complex objects:

```csharp
public class SearchCriteria
{
    public string? Keyword { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> Categories { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SearchResults
{
    public List<ProductDto> Products { get; set; } = new();
    public int TotalCount { get; set; }
}

[Factory]
public static partial class ProductSearch
{
    [Remote]
    [Execute]
    public static async Task<SearchResults> Search(
        SearchCriteria criteria,
        [Service] IProductContext ctx)
    {
        var query = ctx.Products.AsQueryable();

        if (!string.IsNullOrEmpty(criteria.Keyword))
        {
            query = query.Where(p => p.Name.Contains(criteria.Keyword));
        }

        if (criteria.Categories.Any())
        {
            query = query.Where(p => criteria.Categories.Contains(p.Category));
        }

        var totalCount = await query.CountAsync();

        var products = await query
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name })
            .ToListAsync();

        return new SearchResults
        {
            Products = products,
            TotalCount = totalCount
        };
    }
}
```

## Combining with Interface Factories

You can use both static classes and interfaces for Execute:

```csharp
// Static class for pure functions
[Factory]
public static partial class Calculations
{
    [Execute]
    public static decimal CalculateTax(decimal amount, string region)
    {
        return region switch
        {
            "US-CA" => amount * 0.0725m,
            "US-TX" => amount * 0.0625m,
            _ => amount * 0.05m
        };
    }
}

// Interface for operations needing DI
[Factory]
public interface IOrderService
{
    [Execute]
    Task<OrderConfirmation> PlaceOrder(OrderRequest request);
}

public class OrderService : IOrderService
{
    private readonly IOrderContext _ctx;
    private readonly IPaymentGateway _payments;
    private readonly IInventoryService _inventory;

    public OrderService(
        IOrderContext ctx,
        IPaymentGateway payments,
        IInventoryService inventory)
    {
        _ctx = ctx;
        _payments = payments;
        _inventory = inventory;
    }

    public async Task<OrderConfirmation> PlaceOrder(OrderRequest request)
    {
        // Complex operation using injected services
        await _inventory.ReserveAsync(request.Items);
        var payment = await _payments.ChargeAsync(request.PaymentMethod, request.Total);

        var order = new OrderEntity { /* ... */ };
        _ctx.Orders.Add(order);
        await _ctx.SaveChangesAsync();

        return new OrderConfirmation
        {
            OrderId = order.Id,
            PaymentId = payment.Id
        };
    }
}
```

## Best Practices

### Keep Methods Focused

```csharp
// Good: Single responsibility
[Execute]
public static async Task<int> GetActiveUserCount([Service] IUserContext ctx)
{
    return await ctx.Users.CountAsync(u => u.IsActive);
}

// Avoid: Too many responsibilities
[Execute]
public static async Task<DashboardData> GetEverything([Service] IContext ctx)
{
    // Don't load unrelated data in one call
}
```

### Use Meaningful Names

```csharp
// Good: Clear intent
[Factory]
public static partial class InventoryOperations { }

// Less clear
[Factory]
public static partial class Ops { }
```

### Group Related Operations

```csharp
// Reporting operations together
[Factory]
public static partial class ReportingOperations
{
    [Execute] public static Task<SalesReport> GetSalesReport(...) { }
    [Execute] public static Task<InventoryReport> GetInventoryReport(...) { }
    [Execute] public static Task<CustomerReport> GetCustomerReport(...) { }
}

// Admin operations together
[Factory]
public static partial class AdminOperations
{
    [Execute] public static Task ClearCache(...) { }
    [Execute] public static Task ResetDatabase(...) { }
    [Execute] public static Task<SystemStatus> GetStatus(...) { }
}
```

### Handle Errors Appropriately

```csharp
[Execute]
public static async Task<OperationResult> ProcessBatch(
    List<int> ids,
    [Service] IContext ctx)
{
    var results = new List<ItemResult>();

    foreach (var id in ids)
    {
        try
        {
            await ProcessItem(id, ctx);
            results.Add(new ItemResult { Id = id, Success = true });
        }
        catch (Exception ex)
        {
            results.Add(new ItemResult
            {
                Id = id,
                Success = false,
                Error = ex.Message
            });
        }
    }

    return new OperationResult
    {
        TotalProcessed = results.Count,
        Succeeded = results.Count(r => r.Success),
        Failed = results.Count(r => !r.Success),
        Details = results
    };
}
```

## Limitations

1. **Must be static partial class** - Regular static classes won't work
2. **No instance state** - Cannot access instance members
3. **Services via parameter** - Use `[Service]` for DI
4. **Requires [Remote] for network** - Without it, executes locally only

## Next Steps

- **[Interface Factories](interface-factories.md)**: Using [Factory] on interfaces
- **[Factory Operations](../concepts/factory-operations.md)**: All operation types
- **[JSON Serialization](json-serialization.md)**: Custom serialization
