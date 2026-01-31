using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.ValueObjects;
using EmployeeManagement.Tests.TestContainers;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Tests.Samples.AspNetCore;

#region aspnetcore-testing
/// <summary>
/// Two-container test pattern for client/server simulation.
/// </summary>
public class TwoContainerTestingSample
{
    [Fact]
    public void ClientServerRoundTrip_CompareClientVsLocalFactory()
    {
        // Arrange - Get scopes from test container helper
        var (client, server, local) = TestClientServerContainers.CreateScopes();

        // Client container simulates Blazor WASM
        var clientFactory = client.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Local container has no serialization (Logical mode)
        var localFactory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Act - Create entities from both containers
        var clientEmployee = clientFactory.Create();
        var localEmployee = localFactory.Create();

        // Assert - Both should produce valid entities
        Assert.NotEqual(Guid.Empty, clientEmployee.Id);
        Assert.NotEqual(Guid.Empty, localEmployee.Id);
        Assert.True(clientEmployee.IsNew);
        Assert.True(localEmployee.IsNew);
    }

    [Fact]
    public async Task TestFullWorkflow_CreateSaveFetchUpdateDelete()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create
        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "Employee";
        employee.Email = new EmailAddress("test@example.com");
        employee.Position = "Tester";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();
        Assert.True(employee.IsNew);

        // Save (Insert)
        var saved = await factory.Save(employee);
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);

        // Fetch
        var fetched = await factory.Fetch(saved.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Test", fetched.FirstName);

        // Update
        fetched.FirstName = "Updated";
        var updated = await factory.Save(fetched);
        Assert.NotNull(updated);
        Assert.Equal("Updated", updated.FirstName);

        // Delete
        updated.IsDeleted = true;
        await factory.Save(updated);

        // Verify deletion
        var deleted = await factory.Fetch(updated.Id);
        Assert.Null(deleted);
    }
}

/// <summary>
/// Employee entity with full CRUD for testing.
/// </summary>
[Factory]
public partial class EmployeeForTest : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeForTest()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var parts = Name.Split(' ', 2);
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = parts.Length > 0 ? parts[0] : "",
            LastName = parts.Length > 1 ? parts[1] : "",
            Email = "test@example.com",
            Position = "Test",
            SalaryAmount = 50000,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(Id, ct);
        if (entity != null)
        {
            var parts = Name.Split(' ', 2);
            entity.FirstName = parts.Length > 0 ? parts[0] : "";
            entity.LastName = parts.Length > 1 ? parts[1] : "";
            await repository.UpdateAsync(entity, ct);
            await repository.SaveChangesAsync(ct);
        }
    }

    [Remote, Delete]
    public async Task Delete(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
#endregion
