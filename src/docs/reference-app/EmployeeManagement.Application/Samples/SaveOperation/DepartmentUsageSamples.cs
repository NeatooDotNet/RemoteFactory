using EmployeeManagement.Domain.Samples.SaveOperation;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.SaveOperation;

// ============================================================================
// Complete Department CRUD Workflow
// ============================================================================

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
        #region save-complete-usage
        // Complete CRUD: Create -> Insert -> Fetch -> Update -> Delete
        var dept = _factory.Create();
        dept.Name = "Engineering";
        dept = (DepartmentCrud)(await _factory.Save(dept))!;  // Insert
        dept = (await _factory.Fetch(dept.Id))!;              // Fetch
        dept.Name = "Engineering v2";
        await _factory.Save(dept);                            // Update
        dept.IsDeleted = true;
        await _factory.Save(dept);                            // Delete
        #endregion

        return dept.Id;
    }
}
