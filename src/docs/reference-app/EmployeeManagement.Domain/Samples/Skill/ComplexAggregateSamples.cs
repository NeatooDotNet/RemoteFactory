using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Skill;

#region skill-complex-aggregate
[Factory]
public partial class SkillEmployeeWithAssignments : IFactorySaveMeta
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public SkillAssignmentList Assignments { get; set; } = null!;
    public decimal TotalHours => Assignments?.CalculateTotalHours() ?? 0;

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote, Create]
    public void Create(
        string firstName,
        string lastName,
        [Service] ISkillAssignmentListFactory assignmentListFactory)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Assignments = assignmentListFactory.Create();
        IsNew = true;
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] ISkillAssignmentListFactory assignmentListFactory,
        [Service] IEmployeeRepository repo,
        CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;

        // Fetch child collection with data
        var assignmentData = new List<(int, string, decimal, DateTime)>
        {
            (1, "Project Alpha", 40, DateTime.Today),
            (2, "Project Beta", 20, DateTime.Today.AddDays(7))
        };
#pragma warning disable CA2016 // Factory method does not accept CancellationToken
        Assignments = assignmentListFactory.Fetch(assignmentData);
#pragma warning restore CA2016
        IsNew = false;
        return true;
    }

    // Domain method - business logic in entity
    public void AddAssignment(string projectName, decimal hoursAllocated, DateTime startDate)
    {
        Assignments.AddAssignment(projectName, hoursAllocated, startDate);
    }
}

[Factory]
public partial class SkillAssignmentList : List<Assignment>
{
    private readonly IAssignmentFactory _assignmentFactory;

    [Create]
    public SkillAssignmentList([Service] IAssignmentFactory assignmentFactory)
    {
        _assignmentFactory = assignmentFactory;
    }

    [Fetch]
    public void Fetch(
        List<(int Id, string ProjectName, decimal Hours, DateTime StartDate)> data,
        [Service] IAssignmentFactory assignmentFactory)
    {
        foreach (var item in data)
        {
            var assignment = assignmentFactory.Fetch(
                item.Id, item.ProjectName, item.Hours, item.StartDate);
            Add(assignment);
        }
    }

    public void AddAssignment(string projectName, decimal hoursAllocated, DateTime startDate)
    {
        var assignment = _assignmentFactory.Create(projectName, hoursAllocated, startDate);
        Add(assignment);
    }

    public decimal CalculateTotalHours()
    {
        return this.Sum(a => a.HoursAllocated);
    }
}
#endregion
