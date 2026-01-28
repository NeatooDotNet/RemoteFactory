using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-insert
/// <summary>
/// Employee aggregate with [Insert] operation.
/// </summary>
[Factory]
public partial class EmployeeWithInsert : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new Employee, marking it as new.
    /// </summary>
    [Create]
    public EmployeeWithInsert()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    /// <summary>
    /// Persists a new employee to the repository.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);

        IsNew = false;
    }
}
#endregion
