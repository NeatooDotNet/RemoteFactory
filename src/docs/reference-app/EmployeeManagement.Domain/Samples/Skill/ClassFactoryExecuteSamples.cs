using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Skill;

#region skill-class-execute

public interface ISkillConsultation
{
    long PatientId { get; }
    string Status { get; }
}

[Factory]
public partial class SkillConsultation : ISkillConsultation
{
    public long PatientId { get; set; }
    public string Status { get; set; } = string.Empty;

    public SkillConsultation() { }

    [Remote, Create]
    public Task CreateAcute(long patientId, [Service] IEmployeeRepository repo)
    {
        PatientId = patientId;
        Status = "Acute";
        return Task.CompletedTask;
    }

    [Remote, Fetch]
    public Task<bool> FetchActive(long patientId, [Service] IEmployeeRepository repo)
    {
        PatientId = patientId;
        Status = "Active";
        return Task.FromResult(true);
    }

    // Execute on class factory: public static, returns the interface type
    [Remote, Execute]
    public static async Task<ISkillConsultation> StartForPatient(
        long patientId,
        [Service] ISkillConsultationFactory factory,
        [Service] IEmployeeRepository repo)
    {
        var existing = await factory.FetchActive(patientId);
        if (existing != null)
            return existing;

        return await factory.CreateAcute(patientId);
    }
}

#endregion
