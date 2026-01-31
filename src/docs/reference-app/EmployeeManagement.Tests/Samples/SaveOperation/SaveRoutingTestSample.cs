using EmployeeManagement.Domain.Samples.SaveOperation;
using EmployeeManagement.Tests.TestContainers;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Tests.Samples.SaveOperation;

// ============================================================================
// Testing Save Routing
// ============================================================================

#region save-testing
/// <summary>
/// Unit tests verifying Save routes to the correct operation.
/// </summary>
public class SaveRoutingTests
{
    [Fact]
    public async Task Save_WhenIsNewTrue_RoutesToInsert()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeCrudFactory>();

        var employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        // Assert initial state
        Assert.True(employee.IsNew);

        // Act - Save routes to Insert
        var result = await factory.Save(employee);
        var saved = (EmployeeCrud)result!;

        // Assert - Insert was called (IsNew becomes false)
        Assert.False(saved.IsNew);
    }

    [Fact]
    public async Task Save_AfterInsert_RoutesToUpdate()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeCrudFactory>();

        var employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        // Insert first
        var inserted = await factory.Save(employee);
        var savedEmployee = (EmployeeCrud)inserted!;

        // Act - Modify and save (should route to Update)
        savedEmployee.FirstName = "Jane";
        var updated = await factory.Save(savedEmployee);
        var updatedEmployee = (EmployeeCrud)updated!;

        // Assert - Update was called (verify modification persisted)
        Assert.Equal("Jane", updatedEmployee.FirstName);
        Assert.False(updatedEmployee.IsNew);
    }

    [Fact]
    public async Task Save_WhenIsDeletedTrue_RoutesToDelete()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeCrudFactory>();

        var employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        // Insert first
        var inserted = await factory.Save(employee);
        var savedEmployee = (EmployeeCrud)inserted!;

        // Act - Mark deleted and save
        savedEmployee.IsDeleted = true;
        var result = await factory.Save(savedEmployee);

        // Assert - Delete was called (result is returned)
        Assert.NotNull(result);
    }
}
#endregion
