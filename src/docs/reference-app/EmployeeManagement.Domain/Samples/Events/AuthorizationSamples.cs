using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Events;

#region events-authorization
/// <summary>
/// Authorization interface for employee operations.
/// </summary>
public interface IEmployeeEventAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    Task<bool> CanCreateAsync();
}

/// <summary>
/// Authorization implementation checking user context.
/// </summary>
public class EmployeeEventAuth : IEmployeeEventAuth
{
    private readonly IUserContext _userContext;

    public EmployeeEventAuth(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public Task<bool> CanCreateAsync()
    {
        return Task.FromResult(_userContext.IsAuthenticated);
    }
}

/// <summary>
/// Employee with authorization on operations but events bypass authorization.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeEventAuth>]
public partial class EmployeeWithAuthEvents
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    /// <summary>
    /// Create requires authorization - IEmployeeEventAuth.CanCreateAsync is called.
    /// </summary>
    [Create]
    public void Create(string name)
    {
        Id = Guid.NewGuid();
        Name = name;
    }

    /// <summary>
    /// Event BYPASSES authorization - always executes regardless of user permissions.
    /// Events are internal operations triggered by application code, not user requests.
    /// </summary>
    [Event]
    public async Task NotifySystemAdmin(
        Guid employeeId,
        string message,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        // This event executes without authorization checks
        // It runs in a separate scope with no user context
        await emailService.SendAsync(
            "admin@company.com",
            "System Notification",
            $"Employee {employeeId}: {message}",
            ct);
    }
}
#endregion
