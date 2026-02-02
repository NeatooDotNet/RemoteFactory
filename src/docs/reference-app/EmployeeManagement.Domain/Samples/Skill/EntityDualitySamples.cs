using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Skill;

#region skill-entity-duality
[Factory]
public partial class SkillDepartment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    // Aggregate root context - client entry point
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IDepartmentRepository repo, CancellationToken ct)
    {
        var data = await repo.GetByIdAsync(id, ct);
        if (data == null) return false;

        Id = data.Id;
        Name = data.Name;
        Code = data.Code;
        return true;
    }

    // Child context - called from Employee.Fetch on server
    [Fetch]  // No [Remote] - server-side only
    public void FetchAsChild(Guid id, string name, string code)
    {
        Id = id;
        Name = name;
        Code = code;
    }
}
#endregion
