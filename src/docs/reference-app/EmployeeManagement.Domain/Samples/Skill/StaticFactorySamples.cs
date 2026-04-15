using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Skill;

#region skill-static-execute-commands
[Factory]
public static partial class SkillEmployeeCommands
{
    [Remote, Execute]
    private static async Task<bool> _SendNotification(
        string recipient,
        string message,
        [Service] IEmailService service)
    {
        await service.SendAsync(recipient, "Notification", message);
        return true;
    }

    [Remote, Execute]
    private static async Task<SkillEmployeeSummary> _GetEmployeeSummary(
        Guid employeeId,
        [Service] IEmployeeRepository repo)
    {
        var employee = await repo.GetByIdAsync(employeeId);
        if (employee == null)
            return new SkillEmployeeSummary { Id = employeeId, Found = false };

        return new SkillEmployeeSummary
        {
            Id = employeeId,
            FullName = $"{employee.FirstName} {employee.LastName}",
            Position = employee.Position,
            Found = true
        };
    }
}

public class SkillEmployeeSummary
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public bool Found { get; set; }
}
#endregion

