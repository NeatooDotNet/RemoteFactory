using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-delete
/// <summary>
/// Employee aggregate with [Delete] operation.
/// </summary>
[Factory]
public partial class EmployeeWithDelete : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithDelete()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    /// <summary>
    /// Deletes the employee from the repository.
    /// Called by Save when IsDeleted = true.
    /// </summary>
    [Remote, Delete]
    public async Task Delete(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
#endregion
