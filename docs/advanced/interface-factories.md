---
layout: default
title: "Interface Factories"
description: "Using [Factory] on interfaces for service abstraction"
parent: Advanced
nav_order: 2
---

# Interface Factories

The `[Factory]` attribute can be applied to interfaces as well as classes. This enables patterns where the interface defines the contract and the implementation exists only on the server.

## When to Use Interface Factories

Interface factories are useful when:

1. **Server-only implementation**: The concrete type should never exist on the client
2. **Service abstraction**: You want to call remote services through a factory pattern
3. **Multiple implementations**: Different implementations for different scenarios
4. **Testing**: Easy to mock the factory interface

## Basic Interface Factory

### Define the Interface

```csharp
[Factory]
public interface IExecuteOperations
{
    [Execute]
    Task<int> GetTotalCount();

    [Execute]
    Task<List<string>> GetActiveUserNames();

    [Execute]
    Task<bool> ProcessBatchJob(string jobId);
}
```

### Implement on the Server

```csharp
public class ExecuteOperations : IExecuteOperations
{
    private readonly IDbContext _context;
    private readonly IJobService _jobService;

    public ExecuteOperations(IDbContext context, IJobService jobService)
    {
        _context = context;
        _jobService = jobService;
    }

    public async Task<int> GetTotalCount()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<List<string>> GetActiveUserNames()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .Select(u => u.Name)
            .ToListAsync();
    }

    public async Task<bool> ProcessBatchJob(string jobId)
    {
        return await _jobService.ExecuteAsync(jobId);
    }
}
```

### Register the Implementation

```csharp
// Server Program.cs
builder.Services.AddScoped<IExecuteOperations, ExecuteOperations>();
```

### Use Through the Factory

```csharp
// Client code
@inject IExecuteOperationsFactory OperationsFactory

async Task LoadData()
{
    var count = await OperationsFactory.GetTotalCount();
    var users = await OperationsFactory.GetActiveUserNames();
}
```

## Generated Code for Interface Factories

When you apply `[Factory]` to an interface with `[Execute]` methods:

### Generated Factory Interface

```csharp
public interface IExecuteOperationsFactory
{
    Task<int> GetTotalCount();
    Task<List<string>> GetActiveUserNames();
    Task<bool> ProcessBatchJob(string jobId);
}
```

### Generated Delegates

```csharp
public delegate Task<int> GetTotalCountDelegate();
public delegate Task<List<string>> GetActiveUserNamesDelegate();
public delegate Task<bool> ProcessBatchJobDelegate(string jobId);
```

### Server-Side Registration

```csharp
// Delegates are registered to call the implementation
services.AddScoped<GetTotalCountDelegate>(sp =>
{
    var impl = sp.GetRequiredService<IExecuteOperations>();
    return () => impl.GetTotalCount();
});
```

## Difference from Class Factories

| Aspect | Class Factory | Interface Factory |
|--------|---------------|-------------------|
| Concrete type | Generated factory creates instances | Implementation registered separately |
| Create method | Generated from `[Create]` constructors | N/A (no constructors on interfaces) |
| Fetch/Save | Operates on instances | N/A (interface methods are Execute) |
| Primary use | Domain models | Remote services |

## Complete Example: Report Service

### Interface Definition

```csharp
[Factory]
public interface IReportService
{
    [Execute]
    Task<byte[]> GeneratePdfReport(ReportParameters parameters);

    [Execute]
    Task<ReportStatus> GetReportStatus(string reportId);

    [Execute]
    Task<List<ReportDefinition>> GetAvailableReports();

    [Execute]
    Task ScheduleReport(string reportId, DateTime runAt);
}

public class ReportParameters
{
    public string ReportType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Dictionary<string, string> Filters { get; set; }
}

public class ReportStatus
{
    public string Id { get; set; }
    public string Status { get; set; }
    public int PercentComplete { get; set; }
    public string DownloadUrl { get; set; }
}

public class ReportDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}
```

### Server Implementation

```csharp
public class ReportService : IReportService
{
    private readonly IReportEngine _engine;
    private readonly IReportRepository _repository;

    public ReportService(IReportEngine engine, IReportRepository repository)
    {
        _engine = engine;
        _repository = repository;
    }

    public async Task<byte[]> GeneratePdfReport(ReportParameters parameters)
    {
        var report = await _engine.GenerateAsync(parameters);
        return await report.ToPdfAsync();
    }

    public async Task<ReportStatus> GetReportStatus(string reportId)
    {
        return await _repository.GetStatusAsync(reportId);
    }

    public async Task<List<ReportDefinition>> GetAvailableReports()
    {
        return await _repository.GetAllDefinitionsAsync();
    }

    public async Task ScheduleReport(string reportId, DateTime runAt)
    {
        await _repository.ScheduleAsync(reportId, runAt);
    }
}
```

### Server Registration

```csharp
builder.Services.AddNeatooAspNetCore(typeof(IReportService).Assembly);
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IReportEngine, PdfReportEngine>();
builder.Services.AddScoped<IReportRepository, SqlReportRepository>();
```

### Client Usage

```csharp
@page "/reports"
@inject IReportServiceFactory ReportFactory

<h3>Available Reports</h3>

@if (_reports != null)
{
    @foreach (var report in _reports)
    {
        <div>
            <h4>@report.Name</h4>
            <p>@report.Description</p>
            <button @onclick="() => GenerateReport(report.Id)">Generate</button>
        </div>
    }
}

@if (_generating)
{
    <p>Generating report: @_status?.PercentComplete%</p>
}

@code {
    private List<ReportDefinition>? _reports;
    private ReportStatus? _status;
    private bool _generating;

    protected override async Task OnInitializedAsync()
    {
        _reports = await ReportFactory.GetAvailableReports();
    }

    private async Task GenerateReport(string reportId)
    {
        _generating = true;
        StateHasChanged();

        var parameters = new ReportParameters
        {
            ReportType = reportId,
            StartDate = DateTime.Today.AddMonths(-1),
            EndDate = DateTime.Today
        };

        var pdf = await ReportFactory.GeneratePdfReport(parameters);
        await DownloadFile(pdf, "report.pdf");

        _generating = false;
    }
}
```

## Combining with Authorization

Interface factories support authorization through `[AspAuthorize]`:

```csharp
[Factory]
public interface IAdminOperations
{
    [Execute]
    [AspAuthorize(Roles = "Admin")]
    Task<SystemStats> GetSystemStats();

    [Execute]
    [AspAuthorize(Policy = "CanManageUsers")]
    Task ResetUserPassword(string userId);

    [Execute]
    [AspAuthorize(Roles = "Admin")]
    Task ClearCache();
}
```

Or using `[AuthorizeFactory<T>]`:

```csharp
public interface IAdminAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool CanExecuteAdminOps();
}

[Factory]
[AuthorizeFactory<IAdminAuth>]
public interface IAdminOperations
{
    [Execute]
    Task<SystemStats> GetSystemStats();
}
```

## Multiple Implementations

You can have different implementations for different scenarios:

```csharp
[Factory]
public interface IEmailService
{
    [Execute]
    Task SendEmail(string to, string subject, string body);

    [Execute]
    Task<List<EmailStatus>> GetSentEmails();
}

// Production implementation
public class SmtpEmailService : IEmailService
{
    public async Task SendEmail(string to, string subject, string body)
    {
        // Actually send email via SMTP
    }
}

// Development implementation
public class LoggingEmailService : IEmailService
{
    private readonly ILogger _logger;

    public async Task SendEmail(string to, string subject, string body)
    {
        _logger.LogInformation("Would send email to {To}: {Subject}", to, subject);
    }
}

// Registration
if (environment.IsDevelopment())
{
    builder.Services.AddScoped<IEmailService, LoggingEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
```

## Testing Interface Factories

Interface factories are easy to mock:

```csharp
public class ReportPageTests
{
    [Fact]
    public async Task LoadsAvailableReports()
    {
        // Arrange
        var mockFactory = new Mock<IReportServiceFactory>();
        mockFactory.Setup(f => f.GetAvailableReports())
            .ReturnsAsync(new List<ReportDefinition>
            {
                new() { Id = "1", Name = "Sales Report" }
            });

        // Act - use the mock in your component test
    }
}
```

## Limitations

- **No Create methods**: Interfaces don't have constructors
- **No Fetch/Save**: These operate on instances; use `[Execute]` instead
- **Server implementation required**: Unlike class factories, you must provide the implementation

## Best Practices

### Use for Service Operations

```csharp
// Good: Service operations
[Factory]
public interface IAnalyticsService
{
    [Execute]
    Task RecordEvent(AnalyticsEvent evt);
}

// Less ideal: This should probably be a class factory
[Factory]
public interface IPersonOperations
{
    [Execute]
    Task<PersonDto> GetPerson(int id);  // Better as PersonModel with [Fetch]
}
```

### Keep Implementation on Server

```csharp
// The interface can be in shared library
// The implementation stays in server project
namespace Shared
{
    [Factory]
    public interface IBackgroundJobs { }
}

namespace Server
{
    internal class BackgroundJobs : IBackgroundJobs { }
}
```

### Group Related Operations

```csharp
// Good: Related operations together
[Factory]
public interface IOrderProcessing
{
    [Execute] Task<OrderResult> ProcessOrder(Order order);
    [Execute] Task<RefundResult> ProcessRefund(string orderId);
    [Execute] Task<ShippingLabel> GenerateLabel(string orderId);
}
```

## Next Steps

- **[Static Execute](static-execute.md)**: Static class Execute operations
- **[Factory Lifecycle](factory-lifecycle.md)**: Lifecycle hooks
- **[Extending FactoryCore](extending-factory-core.md)**: Custom factory behavior
