using EmployeeManagement.Domain.Samples.SaveOperation;
using EmployeeManagement.Tests.TestContainers;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Tests.Samples.SaveOperation;

// ============================================================================
// Testing Save Routing
// ============================================================================

/// <summary>
/// Unit tests verifying Save routes to the correct operation.
/// </summary>
public class SaveRoutingTests
{
    #region save-testing
    // Test Save routing: IsNew=true -> Insert, IsNew=false -> Update, IsDeleted=true -> Delete
    [Fact]
    public async Task Save_WhenIsNewTrue_RoutesToInsert()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeCrudFactory>();
        var employee = factory.Create();
        Assert.True(employee.IsNew);
        var saved = (EmployeeCrud)(await factory.Save(employee))!;
        Assert.False(saved.IsNew);  // Insert sets IsNew = false
    }
    #endregion

    [Fact]
    public async Task Save_AfterInsert_RoutesToUpdate()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeCrudFactory>();
        var employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        var inserted = await factory.Save(employee);
        var savedEmployee = (EmployeeCrud)inserted!;

        savedEmployee.FirstName = "Jane";
        var updated = await factory.Save(savedEmployee);
        var updatedEmployee = (EmployeeCrud)updated!;

        Assert.Equal("Jane", updatedEmployee.FirstName);
        Assert.False(updatedEmployee.IsNew);
    }

    [Fact]
    public async Task Save_WhenIsDeletedTrue_RoutesToDelete()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeCrudFactory>();
        var employee = factory.Create();
        employee.FirstName = "John";
        employee.LastName = "Doe";
        employee.DepartmentId = Guid.NewGuid();

        var inserted = await factory.Save(employee);
        var savedEmployee = (EmployeeCrud)inserted!;

        savedEmployee.IsDeleted = true;
        var result = await factory.Save(savedEmployee);

        Assert.NotNull(result);
    }
}
