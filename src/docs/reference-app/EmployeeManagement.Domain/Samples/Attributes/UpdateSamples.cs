using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-update
/// <summary>
/// Employee aggregate with [Update] operation.
/// </summary>
[Factory]
public partial class EmployeeWithUpdate : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithUpdate()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    /// <summary>
    /// Updates an existing employee in the repository.
    /// Called by Save when IsNew = false.
    /// </summary>
    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            SalaryAmount = Salary
        };

        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }
}
#endregion
