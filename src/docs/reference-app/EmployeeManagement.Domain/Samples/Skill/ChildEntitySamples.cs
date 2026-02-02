using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Skill;

#region skill-child-entity-no-remote
[Factory]
public partial class Assignment
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public decimal HoursAllocated { get; set; }
    public DateTime StartDate { get; set; }

    [Create]  // No [Remote] - called from server-side Employee operations
    public void Create(string projectName, decimal hoursAllocated, DateTime startDate)
    {
        ProjectName = projectName;
        HoursAllocated = hoursAllocated;
        StartDate = startDate;
    }

    [Fetch]  // No [Remote]
    public void Fetch(int id, string projectName, decimal hoursAllocated, DateTime startDate)
    {
        Id = id;
        ProjectName = projectName;
        HoursAllocated = hoursAllocated;
        StartDate = startDate;
    }
}
#endregion
