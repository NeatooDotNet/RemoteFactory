using EmployeeManagement.Domain.Aggregates;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Client.Samples.AspNetCore;

#region aspnetcore-error-handling
public class ErrorHandlingSample
{
    public async Task<string> HandleErrorsDemo(IEmployeeFactory factory, Guid employeeId)
    {
        ArgumentNullException.ThrowIfNull(factory);
        try
        {
            var employee = await factory.Fetch(employeeId);
            return employee != null ? $"Found: {employee.FirstName}" : "Not found";
        }
        catch (NotAuthorizedException) { return "Access denied"; }
        catch (HttpRequestException) { return "Server unavailable"; }
    }
}
#endregion
