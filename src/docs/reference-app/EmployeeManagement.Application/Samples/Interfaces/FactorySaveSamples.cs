using EmployeeManagement.Domain.Samples.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Interfaces;

#region interfaces-factorysave
// IFactorySave<T>: Generated Save() routes to Insert/Update/Delete based on state
public class SaveLifecycleDemo
{
    public async Task Demo(IEmployeeWithSaveMetaFactory factory)
    {
        var employee = factory.Create();           // IsNew=true
        employee.Name = "John Smith";

        await factory.Save(employee);              // IsNew=true -> Insert
        employee.Name = "Jane Smith";
        await factory.Save(employee);              // IsNew=false -> Update

        employee.IsDeleted = true;
        await factory.Save(employee);              // IsDeleted=true -> Delete
    }
}
#endregion
