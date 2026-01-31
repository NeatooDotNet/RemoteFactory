namespace EmployeeManagement.Domain.Samples.GettingStarted;

public class EmployeeModelUsage
{
    #region getting-started-usage
    public async Task UseEmployeeFactory(IEmployeeModelFactory factory)
    {
        // Create: Call factory.Create() to instantiate a new employee
        var employee = factory.Create();
        employee.FirstName = "Jane";
        employee.LastName = "Smith";
        employee.Email = "jane.smith@example.com";

        // Insert: Save routes to Insert because IsNew = true
        var saved = await factory.Save(employee);
        if (saved == null) return;
        // saved.IsNew is now false

        // Fetch: Load an existing employee by ID
        var fetched = await factory.Fetch(saved.Id);
        if (fetched == null) return;

        // Update: Modify and save routes to Update because IsNew = false
        fetched.Email = "jane.updated@example.com";
        await factory.Save(fetched);

        // Delete: Set IsDeleted = true and save routes to Delete
        fetched.IsDeleted = true;
        await factory.Save(fetched);
    }
    #endregion
}
