using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Interfaces;

// Full working implementation - compiled but not extracted as snippet
// (IFactorySaveMeta snippet is in InterfacesSamples.cs)
[Factory]
public partial class EmployeeWithSaveMeta : IFactorySaveMeta
{
    public Guid EmployeeId { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithSaveMeta()
    {
        EmployeeId = Guid.NewGuid();
        IsNew = true;
    }

    [Remote, Fetch]
    internal async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    internal async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var parts = Name.Split(' ', 2);
        var entity = new EmployeeEntity
        {
            Id = EmployeeId,
            FirstName = parts.Length > 0 ? parts[0] : "",
            LastName = parts.Length > 1 ? parts[1] : "",
            Email = $"{Name.Replace(" ", ".", StringComparison.Ordinal).ToUpperInvariant()}@company.com",
            DepartmentId = Guid.Empty,
            Position = "New Hire",
            SalaryAmount = 50000,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    internal async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(EmployeeId, ct);
        if (entity == null) return;
        var parts = Name.Split(' ', 2);
        entity.FirstName = parts.Length > 0 ? parts[0] : "";
        entity.LastName = parts.Length > 1 ? parts[1] : "";
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    internal async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(EmployeeId, ct);
        await repository.SaveChangesAsync(ct);
    }
}
