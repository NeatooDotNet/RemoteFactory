using EmployeeManagement.Domain.Samples.SaveOperation;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.SaveOperation;

// ============================================================================
// Save Method Usage
// ============================================================================

/// <summary>
/// Demonstrates IFactorySave usage showing Insert, Update, and Delete routing.
/// </summary>
public class SaveUsageDemo
{
    private readonly IEmployeeCrudFactory _factory;

    public SaveUsageDemo(IEmployeeCrudFactory factory)
    {
        _factory = factory;
    }

    public async Task DemonstrateSaveAsync()
    {
        var employee = _factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        #region save-usage
        // Save routes based on state: Insert (IsNew=true), Update (IsNew=false), Delete (IsDeleted=true)
        var saved = await _factory.Save(employee);       // Insert
        saved = await _factory.Save((EmployeeCrud)saved!); // Update
        ((EmployeeCrud)saved!).IsDeleted = true;
        await _factory.Save((EmployeeCrud)saved);        // Delete
        #endregion
    }
}

// ============================================================================
// State After Create
// ============================================================================

/// <summary>
/// Demonstrates state after factory.Create().
/// </summary>
public class CreateStateDemo
{
    private readonly IEmployeeCrudFactory _factory;

    public CreateStateDemo(IEmployeeCrudFactory factory)
    {
        _factory = factory;
    }

    public async Task DemonstrateCreateStateAsync()
    {
        #region save-state-new
        // Create sets IsNew = true; Save(Insert) sets IsNew = false
        var employee = _factory.Create();      // IsNew = true
        employee.FirstName = "Alice";
        var saved = await _factory.Save(employee);
        var result = (EmployeeCrud)saved!;     // IsNew = false
        #endregion

        if (result.IsNew)
            throw new InvalidOperationException("IsNew should be false after Insert");
    }
}

// ============================================================================
// State After Fetch
// ============================================================================

/// <summary>
/// Demonstrates state after factory.Fetch().
/// </summary>
public class FetchStateDemo
{
    private readonly IEmployeeCrudFactory _factory;

    public FetchStateDemo(IEmployeeCrudFactory factory)
    {
        _factory = factory;
    }

    public async Task DemonstrateFetchStateAsync(Guid existingEmployeeId)
    {
        #region save-state-fetch
        // Fetch sets IsNew = false; subsequent Save routes to Update
        var employee = await _factory.Fetch(existingEmployeeId);  // IsNew = false
        employee!.FirstName = "Updated";
        await _factory.Save(employee);  // Routes to Update (not Insert)
        #endregion
    }
}

// ============================================================================
// Deletion State and Workflow
// ============================================================================

/// <summary>
/// Demonstrates deletion workflow via Save.
/// </summary>
public class DeleteStateDemo
{
    private readonly IEmployeeCrudFactory _factory;

    public DeleteStateDemo(IEmployeeCrudFactory factory)
    {
        _factory = factory;
    }

    public async Task DemonstrateDeleteStateAsync(Guid existingEmployeeId)
    {
        var employee = await _factory.Fetch(existingEmployeeId);
        if (employee == null) throw new InvalidOperationException("Employee not found");

        #region save-state-delete
        // Set IsDeleted = true; Save routes to Delete
        employee.IsDeleted = true;
        await _factory.Save(employee);  // Routes to Delete
        #endregion
    }
}
