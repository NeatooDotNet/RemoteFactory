namespace EmployeeManagement.Domain.Samples.GettingStarted;

public class EmployeeModelUsage
{
    #region getting-started-usage
    public async Task UseEmployeeFactory(IEmployeeModelFactory factory)
    {
        // Create new instance via generated factory
        var employee = factory.Create();
        employee.FirstName = "Jane";
        employee.Email = "jane@example.com";

        // Save routes to Insert (IsNew=true), Update (IsNew=false), or Delete (IsDeleted=true)
        employee = await factory.Save(employee);  // Insert: IsNew becomes false

        // Fetch loads existing data from server
        var fetched = await factory.Fetch(employee!.Id);

        // Modify and save routes to Update
        fetched!.FirstName = "Jane Updated";
        await factory.Save(fetched);

        // Mark deleted and save routes to Delete
        fetched.IsDeleted = true;
        await factory.Save(fetched);
    }
    #endregion
}
