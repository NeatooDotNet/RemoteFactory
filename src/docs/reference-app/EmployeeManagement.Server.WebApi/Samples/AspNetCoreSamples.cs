using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Neatoo.RemoteFactory;

// Sample code: suppressing logging performance warnings for readability
#pragma warning disable CA1848 // Use LoggerMessage delegates for performance
#pragma warning disable CA1873 // Evaluation of argument may be expensive and unnecessary if logging is disabled

namespace EmployeeManagement.Server.WebApi.Samples;

// ASP.NET Core Integration Samples - Factory method patterns
// Configuration samples are in AspNetCore/*.cs files

/// <summary>
/// Demonstrates IHttpContextAccessor injection for HTTP context access.
/// </summary>
[Factory]
public partial class EmployeeHttpContext : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string CreatedBy { get; private set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeHttpContext() { Id = Guid.NewGuid(); }

    #region service-injection-httpcontext-aspnetcore
    // IHttpContextAccessor for accessing HTTP request info (server-only)
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        [Service] Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor,
        CancellationToken ct)
    {
        var httpContext = httpContextAccessor.HttpContext;
        CreatedBy = httpContext?.User?.Identity?.Name ?? "system";
        var correlationId = httpContext?.Request?.Headers["X-Correlation-ID"].FirstOrDefault();
        // ... use httpContext for logging, auditing, etc.
        #endregion

        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToUpperInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}
