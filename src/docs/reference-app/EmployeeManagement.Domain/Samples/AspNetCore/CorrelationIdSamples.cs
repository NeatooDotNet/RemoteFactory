using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.ValueObjects;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.AspNetCore;

#region aspnetcore-correlation-id
[Factory]
public partial class EmployeeWithCorrelation
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";

    [Create]
    public EmployeeWithCorrelation() => Id = Guid.NewGuid();

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] ICorrelationContext correlationContext,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        // CorrelationId auto-populated from X-Correlation-Id header
        var correlationId = correlationContext.CorrelationId;

        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        return true;
    }
}
#endregion
