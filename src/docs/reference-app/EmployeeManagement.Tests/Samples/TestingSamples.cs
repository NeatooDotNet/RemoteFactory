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

#region getting-started-usage
/// <summary>
/// Getting started usage example with factory operations.
/// </summary>
public class GettingStartedUsageTests
{
    [Fact]
    public async Task BasicCrudOperations()
    {
        // Arrange - Create test container
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create - Factory generates IEmployeeFactory from [Factory] attribute
        var employee = factory.Create();
        employee.FirstName = "Alice";
        employee.LastName = "Johnson";
        employee.Email = new EmailAddress("alice.johnson@example.com");
        employee.Position = "Software Engineer";
        employee.Salary = new Money(95000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // Insert via Save (routes to Insert based on IsNew = true)
        employee = await factory.Save(employee);
        Assert.NotNull(employee);
        Assert.False(employee.IsNew);

        // Fetch - Load existing employee
        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Alice", fetched.FirstName);

        // Update
        fetched.Position = "Senior Software Engineer";
        fetched.Salary = new Money(115000, "USD");
        fetched = await factory.Save(fetched);

        // Verify update
        var updated = await factory.Fetch(employee.Id);
        Assert.Equal("Senior Software Engineer", updated?.Position);

        // Delete
        fetched.IsDeleted = true;
        await factory.Save(fetched);

        // Verify deletion
        var deleted = await factory.Fetch(employee.Id);
        Assert.Null(deleted);
    }
}
#endregion

#region getting-started-serialization-config
/// <summary>
/// Demonstrates serialization format configuration.
/// </summary>
public class SerializationConfigTests
{
    [Fact]
    public void OrdinalFormatConfiguration()
    {
        // Ordinal format (default) - compact array-based serialization
        var ordinalOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };
        Assert.Equal(SerializationFormat.Ordinal, ordinalOptions.Format);
    }

    [Fact]
    public void NamedFormatConfiguration()
    {
        // Named format - human-readable JSON with property names
        var namedOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };
        Assert.Equal(SerializationFormat.Named, namedOptions.Format);
    }
}
#endregion

#region authorization-testing
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
    public void CanCreate_WithoutHROrManagerRole_ReturnsFalse()
    {
        // Arrange - Create user context without required roles
        var userContext = new TestUserContext
        {
            IsAuthenticated = true,
            Roles = ["Employee"]
        };

        var authorization = new EmployeeAuthorizationImpl(userContext);

        // Act
        var canCreate = authorization.CanCreate();

        // Assert
        Assert.False(canCreate);
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
#endregion

#region save-testing
/// <summary>
/// Testing Save operation state transitions.
/// </summary>
public class SaveOperationTests
{
    [Fact]
    public async Task Save_NewEmployee_CallsInsert()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "Insert";
        employee.Email = new EmailAddress("test.insert@example.com");
        employee.Position = "Developer";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // Assert initial state
        Assert.True(employee.IsNew);
        Assert.False(employee.IsDeleted);

        // Act - Save routes to Insert when IsNew = true
        var saved = await factory.Save(employee);

        // Assert - IsNew = false after insert
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);
    }

    [Fact]
    public async Task Save_ExistingEmployee_CallsUpdate()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create and save initial employee
        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "Update";
        employee.Email = new EmailAddress("test.update@example.com");
        employee.Position = "Developer";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();
        employee = await factory.Save(employee);

        // Assert existing state
        Assert.False(employee.IsNew);
        Assert.False(employee.IsDeleted);

        // Act - Modify and save (routes to Update when IsNew = false)
        employee.Position = "Senior Developer";
        var updated = await factory.Save(employee);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("Senior Developer", updated.Position);
    }

    [Fact]
    public async Task Save_DeletedEmployee_CallsDelete()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create and save employee
        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "Delete";
        employee.Email = new EmailAddress("test.delete@example.com");
        employee.Position = "Developer";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();
        employee = await factory.Save(employee);
        var employeeId = employee.Id;

        // Act - Mark for deletion and save (routes to Delete when IsDeleted = true)
        employee.IsDeleted = true;
        await factory.Save(employee);

        // Assert - Employee no longer exists
        var deleted = await factory.Fetch(employeeId);
        Assert.Null(deleted);
    }
}
#endregion

#region save-complete-usage
/// <summary>
/// Complete Save workflow example.
/// </summary>
public class SaveCompleteUsageTests
{
    [Fact]
    public async Task CompleteSaveWorkflow()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Create new employee
        var employee = factory.Create();
        employee.FirstName = "Jane";
        employee.LastName = "Smith";
        employee.Email = new EmailAddress("jane.smith@example.com");
        employee.Position = "Designer";
        employee.Salary = new Money(70000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // Save workflow: Create -> Insert -> Update -> Delete

        // 1. Insert (IsNew = true)
        Assert.True(employee.IsNew);
        employee = await factory.Save(employee);
        Assert.False(employee.IsNew);
        var id = employee.Id;

        // 2. Fetch existing
        employee = await factory.Fetch(id);
        Assert.NotNull(employee);
        Assert.False(employee.IsNew);

        // 3. Update (IsNew = false, IsDeleted = false)
        employee.Position = "Senior Designer";
        employee = await factory.Save(employee);
        Assert.Equal("Senior Designer", employee.Position);

        // 4. Delete (IsDeleted = true)
        employee.IsDeleted = true;
        await factory.Save(employee);

        // Verify deleted
        var deleted = await factory.Fetch(id);
        Assert.Null(deleted);
    }
}
#endregion

#region save-explicit
/// <summary>
/// Save method routing based on IsNew and IsDeleted flags.
/// Note: Insert/Update/Delete are internal methods - use Save for routing.
/// </summary>
public class ExplicitMethodTests
{
    [Fact]
    public async Task SaveRoutesToInsertUpdateDelete()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "Explicit";
        employee.LastName = "Test";
        employee.Email = new EmailAddress("explicit.test@example.com");
        employee.Position = "Tester";
        employee.Salary = new Money(60000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // Save routes to Insert when IsNew = true
        Assert.True(employee.IsNew);
        employee = await factory.Save(employee);
        Assert.NotNull(employee);
        Assert.False(employee.IsNew);

        // Fetch and modify
        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        fetched.Position = "Lead Tester";

        // Save routes to Update when IsNew = false, IsDeleted = false
        Assert.False(fetched.IsNew);
        Assert.False(fetched.IsDeleted);
        fetched = await factory.Save(fetched);
        Assert.NotNull(fetched);

        // Verify update
        var updated = await factory.Fetch(employee.Id);
        Assert.Equal("Lead Tester", updated?.Position);

        // Save routes to Delete when IsDeleted = true
        updated!.IsDeleted = true;
        await factory.Save(updated);

        // Verify deleted
        var deleted = await factory.Fetch(employee.Id);
        Assert.Null(deleted);
    }
}
#endregion

#region events-testing
/// <summary>
/// Testing event handlers via delegate injection.
/// Events are fired via generated delegates, not factory methods.
/// </summary>
public class EventsTests
{
    [Fact]
    public async Task EventDelegate_FiresAsynchronously()
    {
        // Arrange - Clear any previous data
        InMemoryEmailService.Clear();

        var scopes = TestClientServerContainers.CreateScopes();

        // Events are invoked via delegates resolved from DI
        // The delegate is: EmployeeEventHandlers.NotifyHROfNewEmployeeEvent
        var notifyDelegate = scopes.local.ServiceProvider
            .GetRequiredService<EmployeeManagement.Domain.Events.EmployeeEventHandlers.NotifyHROfNewEmployeeEvent>();

        // Act - Invoke event delegate (fire-and-forget)
        await notifyDelegate(
            Guid.NewGuid(),
            "John Doe");

        // Assert - Wait for event to complete
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();
        await eventTracker.WaitAllAsync();

        // Verify email was sent
        var emails = InMemoryEmailService.GetSentEmails();
        Assert.Contains(emails, e =>
            e.Recipient == "hr@company.com" &&
            e.Subject.Contains("John Doe", StringComparison.Ordinal));
    }
}
#endregion

#region events-testing-latch
/// <summary>
/// Testing events with completion latch.
/// </summary>
public class EventLatchTests
{
    [Fact]
    public async Task WaitForEventCompletion()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();

        // Act - Fire event (would be done via delegate injection in real code)
        // Wait for all pending events
        await eventTracker.WaitAllAsync();

        // Assert - All events completed
        Assert.Equal(0, eventTracker.PendingCount);
    }
}
#endregion

#region service-injection-testing
/// <summary>
/// Testing service injection patterns.
/// </summary>
public class ServiceInjectionTests
{
    [Fact]
    public void ServiceParameter_ResolvedFromDI()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();

        // Services are resolved from DI container
        var repository = scopes.local.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();
        var emailService = scopes.local.ServiceProvider
            .GetRequiredService<IEmailService>();
        var auditLog = scopes.local.ServiceProvider
            .GetRequiredService<IAuditLogService>();

        // Assert - Services are not null
        Assert.NotNull(repository);
        Assert.NotNull(emailService);
        Assert.NotNull(auditLog);
    }
}
#endregion

#region service-injection-lifetimes
/// <summary>
/// Demonstrates service lifetime scoping.
/// </summary>
public class ServiceLifetimeTests
{
    [Fact]
    public void ScopedServices_SameWithinScope()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();

        // Act - Get service twice within same scope
        var repo1 = scopes.local.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();
        var repo2 = scopes.local.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();

        // Assert - Same instance within scope
        Assert.Same(repo1, repo2);
    }

    [Fact]
    public void ScopedServices_DifferentAcrossScopes()
    {
        // Arrange - Create two separate scopes
        var scopes1 = TestClientServerContainers.CreateScopes();
        var scopes2 = TestClientServerContainers.CreateScopes();

        // Act
        var repo1 = scopes1.local.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();
        var repo2 = scopes2.local.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();

        // Assert - Different instances across scopes
        Assert.NotSame(repo1, repo2);
    }
}
#endregion

#region service-injection-matching-name
/// <summary>
/// RegisterMatchingName pattern for interface/implementation pairs.
/// </summary>
public class MatchingNameTests
{
    [Fact]
    public void RegisterMatchingName_ResolvesCorrectImplementation()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();

        // Act - IEmployeeRepository -> InMemoryEmployeeRepository
        var repository = scopes.local.ServiceProvider
            .GetRequiredService<IEmployeeRepository>();

        // Assert - Correct implementation resolved
        Assert.IsType<InMemoryEmployeeRepository>(repository);
    }
}
#endregion

#region aspnetcore-testing
/// <summary>
/// Two-container testing pattern for client-server simulation.
/// </summary>
public class TwoContainerTestingSample
{
    [Fact]
    public async Task ClientServerRoundTrip()
    {
        // Arrange - Create isolated client, server, and local scopes
        var (client, server, local) = TestClientServerContainers.CreateScopes();

        // In Logical mode, all containers share the same implementation
        // For full client-server testing, use separate containers with
        // Remote mode on client and Server mode on server

        var factory = local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Act - Create and persist
        var employee = factory.Create();
        employee.FirstName = "Test";
        employee.LastName = "RoundTrip";
        employee.Email = new EmailAddress("test.roundtrip@example.com");
        employee.Position = "Test";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        await factory.Save(employee);

        // Assert - Fetch returns persisted data
        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Test", fetched.FirstName);
    }
}
#endregion

#region events-eventtracker-access
/// <summary>
/// Accessing IEventTracker for pending event monitoring.
/// </summary>
public class EventTrackerAccessTests
{
    [Fact]
    public void EventTracker_ResolvedFromDI()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();

        // Act - IEventTracker is registered by AddNeatooRemoteFactory
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();

        // Assert
        Assert.NotNull(eventTracker);
    }
}
#endregion

#region events-eventtracker-count
/// <summary>
/// Monitoring pending event count.
/// </summary>
public class EventTrackerCountTests
{
    [Fact]
    public async Task PendingCount_AfterWaitAll_IsZero()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();

        // Act - Wait for any pending events
        await eventTracker.WaitAllAsync();

        // Assert - No pending events after wait
        Assert.Equal(0, eventTracker.PendingCount);
    }
}
#endregion

#region events-eventtracker-wait
/// <summary>
/// Waiting for all events to complete.
/// </summary>
public class EventTrackerWaitTests
{
    [Fact]
    public async Task WaitAllAsync_CompletesWhenAllEventsFinish()
    {
        // Arrange
        var scopes = TestClientServerContainers.CreateScopes();
        var eventTracker = scopes.local.ServiceProvider.GetRequiredService<IEventTracker>();

        // Act - Wait for all pending events
        var waitTask = eventTracker.WaitAllAsync();

        // Assert - WaitAllAsync completes
        await waitTask;
        Assert.Equal(0, eventTracker.PendingCount);
    }
}
#endregion

#region serialization-config
/// <summary>
/// Serialization format configuration during registration.
/// </summary>
public class SerializationConfigSample
{
    [Fact]
    public void ConfigureOrdinalFormat()
    {
        // Ordinal format (default) - compact array-based
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal
        };

        // Use in registration:
        // services.AddNeatooRemoteFactory(NeatooFactory.Logical, options, assembly);
        // services.AddNeatooAspNetCore(options, assembly);

        Assert.Equal(SerializationFormat.Ordinal, options.Format);
    }

    [Fact]
    public void ConfigureNamedFormat()
    {
        // Named format - human-readable with property names
        var options = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };

        Assert.Equal(SerializationFormat.Named, options.Format);
    }
}
#endregion

#region serialization-debug-named
/// <summary>
/// Switching to Named format for debugging.
/// </summary>
public class SerializationDebugSample
{
    [Fact]
    public void DebugWithNamedFormat()
    {
        // For debugging, use Named format in development:
        // if (builder.Environment.IsDevelopment())
        // {
        //     services.AddNeatooAspNetCore(
        //         new NeatooSerializationOptions { Format = SerializationFormat.Named },
        //         assembly);
        // }

        // Named format produces human-readable JSON:
        // { "FirstName": "John", "LastName": "Doe", "Age": 30 }

        // Ordinal format produces compact arrays:
        // [30, "John", "Doe"]  // Age, FirstName, LastName (alphabetical)

        var namedOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };

        Assert.Equal(SerializationFormat.Named, namedOptions.Format);
    }
}
#endregion

#region serialization-json-options
/// <summary>
/// NeatooSerializationOptions configuration.
/// </summary>
public class SerializationJsonOptionsSample
{
    [Fact]
    public void NeatooSerializationOptions_FormatProperty()
    {
        // NeatooSerializationOptions is the configuration object
        var options = new NeatooSerializationOptions
        {
            // Format: Choose Ordinal (default, compact) or Named (readable)
            Format = SerializationFormat.Ordinal
        };

        // Note: RemoteFactory manages JsonSerializerOptions internally
        // Use IOrdinalConverterProvider<T> for custom type serialization

        Assert.Equal(SerializationFormat.Ordinal, options.Format);
    }
}
#endregion

#region modes-full-example
/// <summary>
/// Full mode example - generates both local and remote code.
/// </summary>
public class FullModeExample
{
    [Fact]
    public async Task FullMode_LocalAndRemoteCode()
    {
        // Full mode generates:
        // - Local method implementations
        // - Remote HTTP stubs
        // Use in shared domain assemblies

        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        // Local operations (no network)
        var employee = factory.Create();
        employee.FirstName = "FullMode";
        employee.Email = new EmailAddress("full.mode@example.com");
        employee.Position = "Test";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // Remote operations (would use HTTP in Remote mode)
        await factory.Save(employee);
        var fetched = await factory.Fetch(employee.Id);

        Assert.NotNull(fetched);
    }
}
#endregion

#region modes-logical-example
/// <summary>
/// Logical mode example - everything runs locally for testing.
/// </summary>
public class LogicalModeExample
{
    [Fact]
    public async Task LogicalMode_AllLocal()
    {
        // Logical mode:
        // - All methods execute locally
        // - No serialization, no HTTP
        // - Ideal for unit testing domain logic

        var scopes = TestClientServerContainers.CreateScopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<IEmployeeFactory>();

        var employee = factory.Create();
        employee.FirstName = "LogicalMode";
        employee.Email = new EmailAddress("logical.mode@example.com");
        employee.Position = "Test";
        employee.Salary = new Money(50000, "USD");
        employee.DepartmentId = Guid.NewGuid();

        // All operations execute locally
        await factory.Save(employee);

        var fetched = await factory.Fetch(employee.Id);
        Assert.NotNull(fetched);
        Assert.Equal("LogicalMode", fetched.FirstName);
    }
}
#endregion

#region modes-remoteonly-example
/// <summary>
/// RemoteOnly mode example - HTTP stubs for client assemblies.
/// </summary>
public class RemoteOnlyModeExample
{
    [Fact]
    public void RemoteOnlyMode_GeneratesHttpStubs()
    {
        // RemoteOnly mode:
        // - Generates HTTP client stubs only
        // - No local method implementations
        // - Use in Blazor WASM client assemblies

        // Configuration:
        // [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
        // services.AddNeatooRemoteFactory(NeatooFactory.Remote, options, assembly);
        // services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, ...);

        // All [Remote] methods become HTTP calls to /api/neatoo
        Assert.True(true);
    }
}
#endregion
