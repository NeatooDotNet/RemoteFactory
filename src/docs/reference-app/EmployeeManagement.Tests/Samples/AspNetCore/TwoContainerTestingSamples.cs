using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.ValueObjects;
using EmployeeManagement.Tests.TestContainers;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Tests.Samples.AspNetCore;

#region aspnetcore-testing
public class TwoContainerTestingSample
{
    [Fact]
    public async Task ClientServerRoundTrip()
    {
        // Client, server, local scopes simulate client/server without HTTP
        var (client, server, local) = TestClientServerContainers.CreateScopes();
        var factory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.Email = new EmailAddress("test@example.com");
        employee.Position = "Test";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        await factory.Save(employee);

        var fetched = await factory.Fetch(employee.Id);
        Assert.Equal("Test", fetched?.FirstName);
    }
}
#endregion
