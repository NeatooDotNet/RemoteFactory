using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Operations;

#region operations-class-execute

public interface IConsultation
{
    long PatientId { get; }
    string Status { get; }
}

[Factory]
public partial class Consultation : IConsultation
{
    public long PatientId { get; set; }
    public string Status { get; set; } = string.Empty;

    public Consultation() { }

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

    // Execute on class factory: public static, returns containing type's interface
    [Remote, Execute]
    public static async Task<IConsultation> StartForPatient(
        long patientId,
        [Service] IConsultationFactory factory,
        [Service] IEmployeeRepository repo)
    {
        var existing = await factory.FetchActive(patientId);
        if (existing != null)
            return existing;

        return await factory.CreateAcute(patientId);
    }
}

#endregion
