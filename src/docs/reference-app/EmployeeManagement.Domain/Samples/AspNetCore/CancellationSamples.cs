using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.ValueObjects;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.AspNetCore;

#region aspnetcore-cancellation
[Factory]
public partial class EmployeeWithCancellation
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";

    [Create]
    public EmployeeWithCancellation() => Id = Guid.NewGuid();

    // CancellationToken fires on: client disconnect, server shutdown
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        return true;
    }
}
#endregion
