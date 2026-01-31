using EmployeeManagement.Domain.Samples.SaveOperation;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.SaveOperation;

// ============================================================================
// Complete Department CRUD Workflow
// ============================================================================

#region save-complete-usage
/// <summary>
/// Demonstrates complete CRUD workflow with Department using Save.
/// </summary>
public class DepartmentCrudWorkflow
{
    private readonly IDepartmentCrudFactory _factory;

    public DepartmentCrudWorkflow(IDepartmentCrudFactory factory)
    {
        _factory = factory;
    }

    public async Task<Guid> DemonstrateCrudAsync()
    {
        // CREATE: Create new department
        var department = _factory.Create();
        department.Name = "Engineering";
        department.Code = "ENG";
        department.Budget = 1_000_000m;
        department.IsActive = true;

        // Save routes to Insert (IsNew = true)
        var created = await _factory.Save(department);
        var savedDept = (DepartmentCrud)created!;
        var departmentId = savedDept.Id;

        // READ: Fetch the department
        var fetched = await _factory.Fetch(departmentId);

        if (fetched == null)
            throw new InvalidOperationException("Department not found");

        // UPDATE: Modify budget
        fetched.Budget = 1_500_000m;

        // Save routes to Update (IsNew = false)
        await _factory.Save(fetched);

        // DELETE: Mark for deletion
        fetched.IsDeleted = true;

        // Save routes to Delete (IsDeleted = true)
        await _factory.Save(fetched);

        return departmentId;
    }
}
#endregion
