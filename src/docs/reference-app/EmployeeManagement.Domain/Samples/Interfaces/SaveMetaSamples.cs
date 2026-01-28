using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Interfaces;

#region interfaces-factorysavemeta
/// <summary>
/// Employee aggregate implementing IFactorySaveMeta for Save operation routing.
/// </summary>
[Factory]
public partial class EmployeeWithSaveMeta : IFactorySaveMeta
{
    public Guid EmployeeId { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new employee. Save will call Insert.
    /// </summary>
    [Create]
    public EmployeeWithSaveMeta()
    {
        EmployeeId = Guid.NewGuid();
        IsNew = true;  // Save will call Insert
    }

    /// <summary>
    /// Fetches an existing employee. Save will call Update.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        IsNew = false;  // Save will call Update
        return true;
    }

    /// <summary>
    /// Inserts a new employee into the database.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
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
        IsNew = false;  // After insert, no longer new
    }

    /// <summary>
    /// Updates an existing employee in the database.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(EmployeeId, ct);
        if (entity == null) return;

        var parts = Name.Split(' ', 2);
        entity.FirstName = parts.Length > 0 ? parts[0] : "";
        entity.LastName = parts.Length > 1 ? parts[1] : "";
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Deletes the employee from the database.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(EmployeeId, ct);
        await repository.SaveChangesAsync(ct);
    }
}

// Save routing logic:
// IsNew=true, IsDeleted=false  -> Insert
// IsNew=false, IsDeleted=false -> Update
// IsNew=false, IsDeleted=true  -> Delete
// IsNew=true, IsDeleted=true   -> No operation (new item deleted before save)
#endregion
