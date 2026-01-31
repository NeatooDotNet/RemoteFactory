using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.ValueObjects;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.AspNetCore;

#region aspnetcore-cancellation
/// <summary>
/// Employee with cancellation support for long-running operations.
/// </summary>
[Factory]
public partial class EmployeeWithCancellation
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public EmailAddress Email { get; set; } = null!;

    [Create]
    public EmployeeWithCancellation()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an Employee with cancellation support.
    /// Cancellation fires on: 1. Client disconnect, 2. Server shutdown
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken cancellationToken)
    {
        // Check for cancellation before starting work
        cancellationToken.ThrowIfCancellationRequested();

        // Pass token to async operations for cooperative cancellation
        var entity = await repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
            return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = new EmailAddress(entity.Email);
        return true;
    }
}
#endregion
