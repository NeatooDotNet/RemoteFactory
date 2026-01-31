using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Serialization;

#region serialization-references
// RemoteFactory preserves object identity and handles circular references automatically.
// The NeatooReferenceHandler tracks objects during serialization/deserialization.

/// <summary>
/// Department aggregate with a list of employees (parent side of circular reference).
/// </summary>
[Factory]
public partial class DepartmentWithEmployees
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public List<EmployeeInDepartment> Employees { get; set; } = [];

    [Create]
    public DepartmentWithEmployees()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Employee with reference back to department (child side of circular reference).
/// </summary>
[Factory]
public partial class EmployeeInDepartment
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public DepartmentWithEmployees? Department { get; set; }  // Circular reference

    [Create]
    public EmployeeInDepartment()
    {
        Id = Guid.NewGuid();
    }
}
// NeatooReferenceHandler capabilities:
// - Detects circular references during serialization
// - Preserves object identity (same instance shared, not duplicated)
// - Avoids infinite loops with $ref pointers
// - Reconstructs object graph correctly during deserialization
#endregion

#region serialization-interface
/// <summary>
/// Interface defining the public contract for an employee.
/// </summary>
public interface IEmployeeContract
{
    Guid Id { get; }
    string Name { get; }
    string Department { get; }
}

/// <summary>
/// Concrete [Factory] implementation of IEmployeeContract.
/// RemoteFactory serializes the full concrete type, not just interface members.
/// </summary>
[Factory]
public partial class ContractEmployee : IEmployeeContract
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";

    // Additional properties beyond the interface
    public string Email { get; set; } = "";
    public DateTime HireDate { get; set; }

    [Create]
    public ContractEmployee()
    {
        Id = Guid.NewGuid();
    }
}
// RemoteFactory includes $type discriminator for interface deserialization:
// {"$type":"ContractEmployee","Department":"Engineering",
//  "Email":"john@example.com","HireDate":"2024-01-15T00:00:00Z",
//  "Id":"...","Name":"John Doe"}
#endregion
