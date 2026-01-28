using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-create
/// <summary>
/// Employee aggregate demonstrating multiple [Create] patterns.
/// </summary>
[Factory]
public partial class EmployeeWithCreate
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public DateTime HireDate { get; private set; }

    /// <summary>
    /// [Create] on parameterless constructor - generates new Id.
    /// </summary>
    [Create]
    public EmployeeWithCreate()
    {
        Id = Guid.NewGuid();
        HireDate = DateTime.UtcNow;
    }

    /// <summary>
    /// [Create] on instance method - initializes with provided values.
    /// </summary>
    [Create]
    public void Initialize(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    /// <summary>
    /// [Create] on static factory method - full control over creation.
    /// </summary>
    [Create]
    public static EmployeeWithCreate CreateEmployee(
        string firstName,
        string lastName,
        Guid departmentId)
    {
        return new EmployeeWithCreate
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            DepartmentId = departmentId,
            HireDate = DateTime.UtcNow
        };
    }
}
#endregion
