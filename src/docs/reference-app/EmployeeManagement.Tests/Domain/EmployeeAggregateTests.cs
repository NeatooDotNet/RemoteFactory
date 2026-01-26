using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.ValueObjects;
using EmployeeManagement.Tests.TestContainers;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Tests.Domain;

public class EmployeeAggregateTests
{
    // Note: Each test uses unique GUIDs, so no need to clear data between tests.
    // Clearing data in constructor causes race conditions when tests run in parallel.

    [Fact]
    public void Create_NewEmployee_HasNewId()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Act
        var employee = factory.Create();

        // Assert
        Assert.NotEqual(Guid.Empty, employee.Id);
        Assert.True(employee.IsNew);
    }

    [Fact]
    public async Task Insert_NewEmployee_PersistsData()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.Email = new EmailAddress("john.doe@example.com");
        employee.Position = "Developer";
        employee.Salary = new Money(75000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // Act - use factory Save method
        var saved = await factory.Save(employee);

        // Assert
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);

        // Verify it was persisted by fetching
        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        Assert.Equal("John", fetched.FirstName);
        Assert.Equal("Doe", fetched.LastName);
    }

    [Fact]
    public async Task Update_ExistingEmployee_UpdatesData()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Jane";
        employee.LastName = "Smith";
        employee.Email = new EmailAddress("jane.smith@example.com");
        employee.Position = "Designer";
        employee.Salary = new Money(65000, "USD");
        employee.DepartmentId = Guid.NewGuid();
        employee = await factory.Save(employee);

        // Act
        employee.Position = "Senior Designer";
        employee.Salary = new Money(85000, "USD");
        employee = await factory.Save(employee);

        // Assert
        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Senior Designer", fetched.Position);
        Assert.Equal(85000, fetched.Salary.Amount);
    }

    [Fact]
    public async Task Delete_ExistingEmployee_RemovesFromRepository()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Delete";
        employee.LastName = "Me";
        employee.Email = new EmailAddress("delete.me@example.com");
        employee.Position = "Temp";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();
        employee = await factory.Save(employee);
        var employeeId = employee.Id;

        // Act - mark for delete and save
        employee.IsDeleted = true;
        await factory.Save(employee);

        // Assert - fetch should return null
        var fetched = await factory.Fetch(employeeId);
        Assert.Null(fetched);
    }
}
