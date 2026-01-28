using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-suppressfactory
/// <summary>
/// Base Employee class with [Factory] attribute.
/// </summary>
[Factory]
public partial class EmployeeBase
{
    public Guid Id { get; protected set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";

    [Create]
    public EmployeeBase()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Manager employee with [SuppressFactory] - no factory generated for this derived class.
/// </summary>
[SuppressFactory]
public partial class ManagerEmployee : EmployeeBase
{
    public int DirectReportCount { get; set; }
    public string Department { get; set; } = "";
    public decimal BonusPercentage { get; set; }
}
#endregion
