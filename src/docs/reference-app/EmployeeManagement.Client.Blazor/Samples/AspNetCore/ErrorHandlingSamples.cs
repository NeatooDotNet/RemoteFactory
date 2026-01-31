using EmployeeManagement.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Client.Samples.AspNetCore;

#region aspnetcore-error-handling
public class ErrorHandlingSample
{
    private readonly IEmployeeFactory _factory;

    public ErrorHandlingSample(IEmployeeFactory factory)
    {
        _factory = factory;
    }

    public async Task<string> HandleErrorsDemo(Guid employeeId)
    {
        try
        {
            // Fetch operation that may fail with authorization or server errors
            var employee = await _factory.Fetch(employeeId);

            if (employee == null)
                return "Employee not found";

            return $"Found: {employee.FirstName} {employee.LastName}";
        }
        catch (NotAuthorizedException ex)
        {
            // Authorization failed - user doesn't have permission
            // Occurs when [AspAuthorize] policy denies access
            return $"Access denied: {ex.Message}";
        }
        catch (Exception ex) when (ex.Message.Contains("validation", StringComparison.OrdinalIgnoreCase))
        {
            // Server-side validation failed
            // Occurs when domain rules reject the operation
            return $"Validation failed: {ex.Message}";
        }
        catch (HttpRequestException ex)
        {
            // Network or server connectivity issue
            return $"Server unavailable: {ex.Message}";
        }
    }

    public async Task<string> CreateWithErrorHandling(string firstName, string lastName, string email)
    {
        try
        {
            var employee = _factory.Create();
            employee.FirstName = firstName;
            employee.LastName = lastName;
            employee.Email = new Domain.ValueObjects.EmailAddress(email);

            var saved = await _factory.Save(employee);
            return saved != null
                ? $"Created employee: {saved.Id}"
                : "Failed to create employee";
        }
        catch (NotAuthorizedException)
        {
            // User not authorized to create employees
            return "You don't have permission to create employees";
        }
    }
}
#endregion
