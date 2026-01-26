using EmployeeManagement.Domain.Samples.Save;
using EmployeeManagement.Tests.TestContainers;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Tests.Domain;

public class SaveOperationTests
{
    // Note: Each test uses unique GUIDs, so no need to clear data between tests.
    // Clearing data in constructor causes race conditions when tests run in parallel.

    [Fact]
    public void Create_CallsFactoryComplete_ButNotFactoryStart()
    {
        // Arrange & Act
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeWithSaveFactory>();
        var employee = factory.Create();

        // Assert - Create calls FactoryComplete but not FactoryStart
        Assert.False(employee.OnStartCalled);
        Assert.True(employee.OnCompleteCalled);
        Assert.False(employee.OnCancelledCalled);
        Assert.Equal(FactoryOperation.Create, employee.LastOperation);
    }

    [Fact]
    public async Task FactoryStart_CalledOnInsert()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeWithSaveFactory>();

        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "User";
        employee.Email = "test@example.com";
        employee.DepartmentId = Guid.NewGuid();
        employee.Position = "Tester";
        employee.Salary = 50000;

        // Reset tracking to isolate Save behavior (Create already set OnCompleteCalled)
        // Note: In real code you wouldn't do this, but for testing we need to isolate the Save behavior

        // Act
        employee = await factory.Save(employee);

        // Assert - After Save, OnStartCalled should be true (set during Insert)
        Assert.True(employee.OnStartCalled);
        Assert.Equal(FactoryOperation.Insert, employee.LastOperation);
    }

    [Fact]
    public async Task FactoryComplete_CalledAfterSuccessfulInsert()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeWithSaveFactory>();

        var employee = factory.Create();
        var initialVersion = employee.Version; // Should be 1

        employee.FirstName = "Test";
        employee.LastName = "User";
        employee.Email = "test@example.com";
        employee.DepartmentId = Guid.NewGuid();
        employee.Position = "Tester";
        employee.Salary = 50000;

        // Act
        employee = await factory.Save(employee);

        // Assert - FactoryComplete increments Version for Insert operations
        Assert.True(employee.OnCompleteCalled); // Was true from Create, still true
        Assert.Equal(initialVersion + 1, employee.Version); // Version incremented from 1 to 2
        Assert.Equal(FactoryOperation.Insert, employee.LastOperation);
    }

    [Fact]
    public async Task ValidationFailure_InFactoryStart_ThrowsException()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeWithSaveFactory>();

        var employee = factory.Create();
        // FirstName is empty - should fail validation in FactoryStart
        employee.LastName = "User";
        employee.Email = "test@example.com";
        employee.DepartmentId = Guid.NewGuid();
        employee.Position = "Tester";
        employee.Salary = 50000;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => factory.Save(employee));
        Assert.Equal("FirstName is required", ex.Message);

        // FactoryStart was called and threw the exception
        Assert.True(employee.OnStartCalled);
        Assert.Equal(FactoryOperation.Insert, employee.LastOperation);

        // Note: OnCancelledCalled is NOT set for validation exceptions.
        // FactoryCancelled is only called for OperationCanceledException (token cancellation).
        Assert.False(employee.OnCancelledCalled);
    }

    [Fact]
    public async Task FactoryCancelled_CalledOnCancellation()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeWithSaveFactory>();

        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "User";
        employee.Email = "test@example.com";
        employee.DepartmentId = Guid.NewGuid();
        employee.Position = "Tester";
        employee.Salary = 50000;

        // Create a pre-cancelled token
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => factory.Save(employee, cts.Token));

        // FactoryStart is called first, then the Insert method checks the token and throws
        Assert.True(employee.OnStartCalled);
        Assert.True(employee.OnCancelledCalled);
        Assert.Equal(FactoryOperation.Insert, employee.LastOperation);
    }
}
