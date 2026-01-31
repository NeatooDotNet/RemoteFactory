using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Serialization;

#region serialization-collections
/// <summary>
/// Demonstrates collection serialization with various collection types.
/// </summary>
[Factory]
public partial class EmployeeWithSkills
{
    public Guid Id { get; private set; }
    public List<string> Skills { get; set; } = [];
    public string[] Certifications { get; set; } = [];
    public Dictionary<string, int> ProjectHours { get; set; } = [];

    [Create]
    public EmployeeWithSkills()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public void FetchSampleData()
    {
        Skills = ["C#", "TypeScript", "SQL"];
        Certifications = ["Azure Developer", "Scrum Master"];
        ProjectHours = new Dictionary<string, int>
        {
            ["Project Alpha"] = 120,
            ["Project Beta"] = 80,
            ["Project Gamma"] = 45
        };
    }
}
#endregion

#region serialization-polymorphism
/// <summary>
/// Abstract base class for employee compensation demonstrating polymorphic serialization.
/// </summary>
public abstract class Compensation
{
    public Guid Id { get; set; }
    public DateTime EffectiveDate { get; set; }
}

/// <summary>
/// Salary-based compensation (annual amount).
/// </summary>
public class SalaryCompensation : Compensation
{
    public decimal AnnualAmount { get; set; }
}

/// <summary>
/// Hourly-based compensation (rate and hours per week).
/// </summary>
public class HourlyCompensation : Compensation
{
    public decimal HourlyRate { get; set; }
    public int HoursPerWeek { get; set; }
}
// The $type discriminator identifies concrete type during deserialization:
// {"$type":"SalaryCompensation","AnnualAmount":85000,"EffectiveDate":"2024-01-01T00:00:00Z","Id":"..."}
// {"$type":"HourlyCompensation","EffectiveDate":"2024-01-01T00:00:00Z","HourlyRate":45.00,"HoursPerWeek":40,"Id":"..."}
#endregion
