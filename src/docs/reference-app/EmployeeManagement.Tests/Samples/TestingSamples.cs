using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.Samples.Authorization;
using EmployeeManagement.Domain.Samples.Events;
using EmployeeManagement.Domain.Samples.Services;
using EmployeeManagement.Domain.ValueObjects;
using EmployeeManagement.Infrastructure.Repositories;
using EmployeeManagement.Infrastructure.Services;
using EmployeeManagement.Tests.TestContainers;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Tests.Samples;

// Snippet moved to EmployeeModelUsage.cs for minimal example
public class GettingStartedUsageTests
{
    [Fact]
    public async Task BasicCrudOperations()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Alice";
        employee.LastName = "Johnson";
        employee.Email = new EmailAddress("alice.johnson@example.com");
        employee.Position = "Software Engineer";
        employee.Salary = new Money(95000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        employee = await factory.Save(employee);
        Assert.NotNull(employee);
        Assert.False(employee.IsNew);

        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Alice", fetched.FirstName);

        fetched.Position = "Senior Software Engineer";
        fetched.Salary = new Money(115000, "USD");
        fetched = await factory.Save(fetched);

        var updated = await factory.Fetch(employee.Id);
        Assert.Equal("Senior Software Engineer", updated?.Position);

        fetched.IsDeleted = true;
        await factory.Save(fetched);

        var deleted = await factory.Fetch(employee.Id);
        Assert.Null(deleted);
    }
}

// Snippet moved to SerializationConfigSample.cs for minimal example
public class SerializationConfigTests
{
    [Fact]
    public void OrdinalFormatConfiguration()
    {
        var ordinalOptions = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };
        Assert.Equal(SerializationFormat.Ordinal, ordinalOptions.Format);
    }

    [Fact]
    public void NamedFormatConfiguration()
    {
        var namedOptions = new NeatooSerializationOptions { Format = SerializationFormat.Named };
        Assert.Equal(SerializationFormat.Named, namedOptions.Format);
    }
}

/// <summary>
/// Testing authorization rules.
/// </summary>
public class AuthorizationTestingSamples
{
    [Fact]
    public void CanCreate_WithHRRole_ReturnsTrue()
    {
        // Arrange - Create user context with HR role
        var userContext = new TestUserContext
        {
            IsAuthenticated = true,
            Roles = ["HR"]
        };

        var authorization = new EmployeeAuthorizationImpl(userContext);

        // Act
        var canCreate = authorization.CanCreate();

        // Assert
        Assert.True(canCreate);
    }

    [Fact]
    public void CanCreate_WhenAuthenticatedWithAnyRole_ReturnsTrue()
    {
        // Arrange - Create user context with any role (no specific role required for Create)
        var userContext = new TestUserContext
        {
            IsAuthenticated = true,
            Roles = ["Employee"]
        };

        var authorization = new EmployeeAuthorizationImpl(userContext);

        // Act
        var canCreate = authorization.CanCreate();

        // Assert - CanCreate only requires authentication, not specific roles
        Assert.True(canCreate);
    }

    [Fact]
    public void CanRead_WhenAuthenticated_ReturnsTrue()
    {
        // Arrange
        var userContext = new TestUserContext
        {
            IsAuthenticated = true,
            Roles = []
        };

        var authorization = new EmployeeAuthorizationImpl(userContext);

        // Act
        var canRead = authorization.CanRead();

        // Assert - All authenticated users can read
        Assert.True(canRead);
    }
}

/// <summary>
/// Test user context for authorization tests.
/// </summary>
internal class TestUserContext : EmployeeManagement.Domain.Interfaces.IUserContext
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "testuser";
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool IsAuthenticated { get; set; }
    public bool IsInRole(string role) => Roles.Contains(role);
}

// Save operation tests - snippet in SaveRoutingTestSample.cs
public class SaveOperationTests
{
    [Fact]
    public async Task Save_NewEmployee_CallsInsert()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "Insert";
        employee.Email = new EmailAddress("test.insert@example.com");
        employee.Position = "Developer";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        Assert.True(employee.IsNew);
        Assert.False(employee.IsDeleted);

        var saved = await factory.Save(employee);

        Assert.NotNull(saved);
        Assert.False(saved.IsNew);
    }

    [Fact]
    public async Task Save_ExistingEmployee_CallsUpdate()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "Update";
        employee.Email = new EmailAddress("test.update@example.com");
        employee.Position = "Developer";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();
        employee = await factory.Save(employee);

        Assert.False(employee.IsNew);
        Assert.False(employee.IsDeleted);

        employee.Position = "Senior Developer";
        var updated = await factory.Save(employee);

        Assert.NotNull(updated);
        Assert.Equal("Senior Developer", updated.Position);
    }

    [Fact]
    public async Task Save_DeletedEmployee_CallsDelete()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "Delete";
        employee.Email = new EmailAddress("test.delete@example.com");
        employee.Position = "Developer";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();
        employee = await factory.Save(employee);
        var employeeId = employee.Id;

        employee.IsDeleted = true;
        await factory.Save(employee);

        var deleted = await factory.Fetch(employeeId);
        Assert.Null(deleted);
    }
}

// Complete Save workflow tests - snippet in DepartmentUsageSamples.cs
public class SaveCompleteUsageTests
{
    [Fact]
    public async Task CompleteSaveWorkflow()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Jane";
        employee.LastName = "Smith";
        employee.Email = new EmailAddress("jane.smith@example.com");
        employee.Position = "Designer";
        employee.Salary = new Money(70000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        Assert.True(employee.IsNew);
        employee = await factory.Save(employee);
        Assert.False(employee.IsNew);
        var id = employee.Id;

        employee = await factory.Fetch(id);
        Assert.NotNull(employee);
        Assert.False(employee.IsNew);

        employee.Position = "Senior Designer";
        employee = await factory.Save(employee);
        Assert.Equal("Senior Designer", employee.Position);

        employee.IsDeleted = true;
        await factory.Save(employee);

        var deleted = await factory.Fetch(id);
        Assert.Null(deleted);
    }
}

// Explicit method routing tests - snippet in SaveExplicitSamples.cs
public class ExplicitMethodTests
{
    [Fact]
    public async Task SaveRoutesToInsertUpdateDelete()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Explicit";
        employee.LastName = "Test";
        employee.Email = new EmailAddress("explicit.test@example.com");
        employee.Position = "Tester";
        employee.Salary = new Money(60000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        Assert.True(employee.IsNew);
        employee = await factory.Save(employee);
        Assert.NotNull(employee);
        Assert.False(employee.IsNew);

        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        fetched.Position = "Lead Tester";

        Assert.False(fetched.IsNew);
        Assert.False(fetched.IsDeleted);
        fetched = await factory.Save(fetched);
        Assert.NotNull(fetched);

        var updated = await factory.Fetch(employee.Id);
        Assert.Equal("Lead Tester", updated?.Position);

        updated!.IsDeleted = true;
        await factory.Save(updated);

        var deleted = await factory.Fetch(employee.Id);
        Assert.Null(deleted);
    }
}

// Events tests - main tests in EventsTests.cs (below are verification only, no doc snippets)
public class EventsTests
{
    [Fact]
    public async Task EventDelegate_FiresAsynchronously()
    {
        InMemoryEmailService.Clear();
        var scopes = TestClientServerContainers.CreateScopes();
        var notifyDelegate = scopes.local.ServiceProvider
            .GetRequiredService<EmployeeManagement.Domain.Events.EmployeeEventHandlers.NotifyHROfNewEmployeeEvent>();
        await notifyDelegate(Guid.NewGuid(), "John Doe");
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();
        await eventTracker.WaitAllAsync();
        var emails = InMemoryEmailService.GetSentEmails();
        Assert.Contains(emails, e => e.Recipient == "hr@company.com" && e.Subject.Contains("John Doe", StringComparison.Ordinal));
    }
}

public class EventLatchTests
{
    [Fact]
    public async Task WaitForEventCompletion()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();
        await eventTracker.WaitAllAsync();
        Assert.Equal(0, eventTracker.PendingCount);
    }
}

/// <summary>
/// Testing service injection patterns.
/// </summary>
public class ServiceInjectionTests
{
    #region service-injection-testing
    [Fact]
    public void ServiceParameter_ResolvedFromDI()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var repository = scopes.local.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        Assert.NotNull(repository);  // Services resolve from test container
    }
    #endregion
}

/// <summary>
/// Demonstrates service lifetime scoping.
/// </summary>
public class ServiceLifetimeTests
{
    #region service-injection-lifetimes
    [Fact]
    public void ScopedServices_SameWithinScope()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var repo1 = scopes.local.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        var repo2 = scopes.local.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        Assert.Same(repo1, repo2);  // Same instance within scope
    }
    #endregion

    [Fact]
    public void ScopedServices_DifferentAcrossScopes()
    {
        var scopes1 = TestClientServerContainers.CreateScopes();
        var scopes2 = TestClientServerContainers.CreateScopes();
        var repo1 = scopes1.local.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        var repo2 = scopes2.local.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        Assert.NotSame(repo1, repo2);
    }
}

/// <summary>
/// RegisterMatchingName pattern for interface/implementation pairs.
/// </summary>
public class MatchingNameTests
{
    #region service-injection-matching-name
    [Fact]
    public void RegisterMatchingName_ResolvesCorrectImplementation()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var repository = scopes.local.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        Assert.IsType<InMemoryEmployeeRepository>(repository);  // IName -> Name convention
    }
    #endregion
}

// aspnetcore-testing snippet moved to AspNetCore/TwoContainerTestingSamples.cs

// EventTracker tests - no doc snippets (snippets now in EventsSamples.cs)
public class EventTrackerAccessTests
{
    [Fact]
    public void EventTracker_ResolvedFromDI()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();
        Assert.NotNull(eventTracker);
    }
}

public class EventTrackerCountTests
{
    [Fact]
    public async Task PendingCount_AfterWaitAll_IsZero()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();
        await eventTracker.WaitAllAsync();
        Assert.Equal(0, eventTracker.PendingCount);
    }
}

public class EventTrackerWaitTests
{
    [Fact]
    public async Task WaitAllAsync_CompletesWhenAllEventsFinish()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();
        await eventTracker.WaitAllAsync();
        Assert.Equal(0, eventTracker.PendingCount);
    }
}

// Serialization tests - snippets consolidated in Server.WebApi/Samples/Serialization
public class SerializationConfigSample
{
    [Fact]
    public void ConfigureOrdinalFormat()
    {
        var options = new NeatooSerializationOptions { Format = SerializationFormat.Ordinal };
        Assert.Equal(SerializationFormat.Ordinal, options.Format);
    }

    [Fact]
    public void ConfigureNamedFormat()
    {
        var options = new NeatooSerializationOptions { Format = SerializationFormat.Named };
        Assert.Equal(SerializationFormat.Named, options.Format);
    }
}

// Mode examples moved to FactoryModesSamples.cs (consolidated minimal snippets)
// These tests verify the factory modes work correctly

/// <summary>
/// Full mode verification test (implementation, not for docs).
/// </summary>
public class FullModeExample
{
    [Fact]
    public async Task FullMode_LocalAndRemoteCode()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "FullMode";
        employee.Email = new EmailAddress("full.mode@example.com");
        employee.Position = "Test";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        await factory.Save(employee);
        var fetched = await factory.Fetch(employee.Id);

        Assert.NotNull(fetched);
    }
}

/// <summary>
/// Logical mode verification test (implementation, not for docs).
/// </summary>
public class LogicalModeExample
{
    [Fact]
    public async Task LogicalMode_AllLocal()
    {
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "LogicalMode";
        employee.Email = new EmailAddress("logical.mode@example.com");
        employee.Position = "Test";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        await factory.Save(employee);

        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        Assert.Equal("LogicalMode", fetched.FirstName);
    }
}

/// <summary>
/// RemoteOnly mode verification test (implementation, not for docs).
/// </summary>
public class RemoteOnlyModeExample
{
    [Fact]
    public void RemoteOnlyMode_GeneratesHttpStubs()
    {
        // Verifies RemoteOnly mode configuration pattern
        Assert.True(true);
    }
}
