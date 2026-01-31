using EmployeeManagement.Domain.Samples.SaveOperation;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.SaveOperation;

// ============================================================================
// Save Method Usage
// ============================================================================

#region save-usage
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

    /// <summary>
    /// Complete Save lifecycle: Insert, Update, Delete.
    /// </summary>
    public async Task DemonstrateSaveAsync()
    {
        // 1. Create new employee - IsNew = true by default
        var employee = _factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        // Save routes to Insert because IsNew = true
        var inserted = await _factory.Save(employee);

        // After Insert, IsNew = false
        var savedEmployee = (EmployeeCrud)inserted!;

        // 2. Modify and save - routes to Update because IsNew = false
        savedEmployee.FirstName = "Jane";
        await _factory.Save(savedEmployee);

        // 3. Mark for deletion and save - routes to Delete
        savedEmployee.IsDeleted = true;
        await _factory.Save(savedEmployee);
    }
}
#endregion

// ============================================================================
// State After Create
// ============================================================================

#region save-state-new
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
        // Create returns a new entity
        var employee = _factory.Create();

        // New entities have IsNew = true, IsDeleted = false
        var isNewAfterCreate = employee.IsNew;      // true
        var isDeletedAfterCreate = employee.IsDeleted; // false

        // Set required properties
        employee.FirstName = "Alice";
        employee.LastName = "Smith";
        employee.DepartmentId = Guid.NewGuid();

        // Save routes to Insert because IsNew = true
        var saved = await _factory.Save(employee);
        var savedEmployee = (EmployeeCrud)saved!;

        // After Insert, IsNew = false
        var isNewAfterSave = savedEmployee.IsNew; // false

        // Verify state transitions
        if (!isNewAfterCreate)
            throw new InvalidOperationException("IsNew should be true after Create");
        if (isNewAfterSave)
            throw new InvalidOperationException("IsNew should be false after Insert");
    }
}
#endregion

// ============================================================================
// State After Fetch
// ============================================================================

#region save-state-fetch
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
        // Fetch returns an existing entity
        var employee = await _factory.Fetch(existingEmployeeId);

        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        // Fetched entities have IsNew = false
        var isNewAfterFetch = employee.IsNew; // false

        // Modify and save - routes to Update (not Insert)
        employee.FirstName = "Updated Name";
        await _factory.Save(employee);

        // Verify Update was called (IsNew remains false)
        if (isNewAfterFetch)
            throw new InvalidOperationException("IsNew should be false after Fetch");
    }
}
#endregion

// ============================================================================
// Deletion State and Workflow
// ============================================================================

#region save-state-delete
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
        // Start with existing employee
        var employee = await _factory.Fetch(existingEmployeeId);

        if (employee == null)
            throw new InvalidOperationException("Employee not found");

        // Verify initial state: IsNew = false, IsDeleted = false
        var isNewBefore = employee.IsNew;       // false
        var isDeletedBefore = employee.IsDeleted; // false

        // Mark for deletion
        employee.IsDeleted = true;

        // Verify state: IsNew = false, IsDeleted = true
        var isNewAfterMark = employee.IsNew;       // false
        var isDeletedAfterMark = employee.IsDeleted; // true

        // Save routes to Delete because IsDeleted = true
        var result = await _factory.Save(employee);

        // Save returns the deleted entity
        var deletedEmployee = result;

        // Verify state transitions
        if (isNewBefore)
            throw new InvalidOperationException("Fetched entity should not be new");
        if (!isDeletedAfterMark)
            throw new InvalidOperationException("IsDeleted should be true after marking");
    }
}
#endregion
