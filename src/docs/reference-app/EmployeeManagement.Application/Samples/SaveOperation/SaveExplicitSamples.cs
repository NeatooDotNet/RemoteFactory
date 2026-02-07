using EmployeeManagement.Domain.Samples.SaveOperation;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.SaveOperation;

// ============================================================================
// Save vs Explicit Method Calls
// ============================================================================

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

    public async Task UsingSaveAsync()
    {
        var employee = _factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        #region save-explicit
        // Save: state-based routing (single "Save" button in UI)
        await _factory.Save(employee);           // Routes to Insert (IsNew=true)
        employee.FirstName = "Jane";
        await _factory.Save(employee);           // Routes to Update (IsNew=false)
        employee.IsDeleted = true;
        await _factory.Save(employee);           // Routes to Delete (IsDeleted=true)
        #endregion
    }
}
