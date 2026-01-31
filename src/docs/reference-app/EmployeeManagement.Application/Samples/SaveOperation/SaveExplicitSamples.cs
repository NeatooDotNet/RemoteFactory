using EmployeeManagement.Domain.Samples.SaveOperation;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.SaveOperation;

// ============================================================================
// Save vs Explicit Method Calls
// ============================================================================

#region save-explicit
/// <summary>
/// Demonstrates Save method vs explicit Insert/Update/Delete calls.
/// </summary>
public class SaveVsExplicitDemo
{
    private readonly IEmployeeCrudFactory _factory;

    public SaveVsExplicitDemo(IEmployeeCrudFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Using Save with automatic routing based on state flags.
    /// Save examines IsNew and IsDeleted to determine which operation to call.
    /// </summary>
    public async Task UsingSaveAsync()
    {
        // Create new employee
        var employee = _factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        // Save routes to Insert (IsNew = true)
        var saved = await _factory.Save(employee);
        var savedEmployee = (EmployeeCrud)saved!;

        // Modify and Save routes to Update (IsNew = false)
        savedEmployee.FirstName = "Jane";
        await _factory.Save(savedEmployee);

        // Mark deleted and Save routes to Delete (IsDeleted = true)
        savedEmployee.IsDeleted = true;
        await _factory.Save(savedEmployee);
    }

    // When to use Save:
    // - UI doesn't track state (single "Save" button)
    // - State-based routing simplifies client code
    // - Form-based applications where user edits then saves
    //
    // When to use explicit methods:
    // - Client knows the exact operation needed
    // - Different UI actions map to different operations
    // - You want granular control over each operation
    // - API endpoints that map directly to CRUD operations
}
#endregion
