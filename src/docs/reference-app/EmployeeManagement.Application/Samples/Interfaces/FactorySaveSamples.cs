using EmployeeManagement.Domain.Samples.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Interfaces;

#region interfaces-factorysave
/// <summary>
/// Demonstrates IFactorySave&lt;T&gt; usage from the generated factory.
/// </summary>
public class EmployeeSaveDemo
{
    private readonly IEmployeeWithSaveMetaFactory _factory;

    public EmployeeSaveDemo(IEmployeeWithSaveMetaFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Demonstrates the complete Save lifecycle: Create, Insert, Update, Delete.
    /// </summary>
    public async Task DemonstrateSaveLifecycleAsync()
    {
        // Create new employee
        var employee = _factory.Create();
        employee.Name = "John Smith";

        // First Save (Insert): IsNew=true -> Insert
        var saved = await _factory.Save(employee);

        // Assert saved is not null and IsNew is false after save
        if (saved == null)
            throw new InvalidOperationException("Save returned null");
        if (saved.IsNew)
            throw new InvalidOperationException("IsNew should be false after insert");

        // Second Save (Update): IsNew=false -> Update
        var savedEmployee = (EmployeeWithSaveMeta)saved;
        savedEmployee.Name = "Jane Smith";
        await _factory.Save(savedEmployee);

        // Third Save (Delete): IsDeleted=true -> Delete
        savedEmployee.IsDeleted = true;
        await _factory.Save(savedEmployee);
    }
}
#endregion
