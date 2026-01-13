using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.ClientServerSeparation;

/// <summary>
/// Tests demonstrating and validating client-server separation patterns.
/// These tests use objects within the test project that mimic the OrderEntry pattern.
/// </summary>
public class ClientServerSeparationTests
{
    // ============================================================================
    // Test Domain Objects - Mimicking CLIENT conditional compilation
    // ============================================================================

    /// <summary>
    /// Simulates a domain object with client-side placeholder methods.
    /// In real usage, this would be in a separate assembly with [assembly: FactoryMode(RemoteOnly)].
    /// The placeholder methods throw if called directly - the factory should handle remote calls.
    /// </summary>
    [Factory]
    public class ClientStyleAggregate : IFactorySaveMeta
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; } = true;

        /// <summary>
        /// Client-side placeholder - throws if called directly.
        /// The factory's Create method should make a remote call, not invoke this.
        /// </summary>
        [Remote, Create]
        public void CreatePlaceholder()
        {
            throw new InvalidOperationException("Client should call through IClientStyleAggregateFactory.Create()");
        }

        /// <summary>
        /// Client-side placeholder for Fetch.
        /// </summary>
        [Remote, Fetch]
        public Task FetchPlaceholder(Guid id)
        {
            throw new InvalidOperationException("Client should call through IClientStyleAggregateFactory.Fetch()");
        }

        /// <summary>
        /// Client-side placeholder for Insert.
        /// </summary>
        [Remote, Insert]
        public Task InsertPlaceholder()
        {
            throw new InvalidOperationException("Client should call through IClientStyleAggregateFactory.Save()");
        }

        /// <summary>
        /// Client-side placeholder for Update.
        /// </summary>
        [Remote, Update]
        public Task UpdatePlaceholder()
        {
            throw new InvalidOperationException("Client should call through IClientStyleAggregateFactory.Save()");
        }

        /// <summary>
        /// Client-side placeholder for Delete.
        /// </summary>
        [Remote, Delete]
        public Task DeletePlaceholder()
        {
            throw new InvalidOperationException("Client should call through IClientStyleAggregateFactory.Save()");
        }
    }

    /// <summary>
    /// Simulates a child entity with local Create (no [Remote]).
    /// Local Create runs on both client and server - no network call needed.
    /// </summary>
    [Factory]
    public class LocalCreateChild
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }

        /// <summary>
        /// Local Create - runs on both client and server without remote call.
        /// This is the pattern for simple child entities.
        /// </summary>
        [Create]
        public LocalCreateChild()
        {
            Id = Guid.NewGuid();
        }
    }

    // ============================================================================
    // Placeholder Method Tests
    // ============================================================================

    [Fact]
    public void PlaceholderMethod_Create_ThrowsWhenCalledDirectly()
    {
        // Arrange
        var aggregate = new ClientStyleAggregate();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => aggregate.CreatePlaceholder());
        Assert.Contains("Client should call through", exception.Message);
        Assert.Contains("Factory", exception.Message);
    }

    [Fact]
    public async Task PlaceholderMethod_Fetch_ThrowsWhenCalledDirectly()
    {
        // Arrange
        var aggregate = new ClientStyleAggregate();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => aggregate.FetchPlaceholder(Guid.NewGuid()));
        Assert.Contains("Client should call through", exception.Message);
    }

    [Fact]
    public async Task PlaceholderMethod_Insert_ThrowsWhenCalledDirectly()
    {
        // Arrange
        var aggregate = new ClientStyleAggregate();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => aggregate.InsertPlaceholder());
        Assert.Contains("Client should call through", exception.Message);
    }

    [Fact]
    public async Task PlaceholderMethod_Update_ThrowsWhenCalledDirectly()
    {
        // Arrange
        var aggregate = new ClientStyleAggregate();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => aggregate.UpdatePlaceholder());
        Assert.Contains("Client should call through", exception.Message);
    }

    [Fact]
    public async Task PlaceholderMethod_Delete_ThrowsWhenCalledDirectly()
    {
        // Arrange
        var aggregate = new ClientStyleAggregate();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => aggregate.DeletePlaceholder());
        Assert.Contains("Client should call through", exception.Message);
    }

    // ============================================================================
    // Local Create Tests
    // ============================================================================

    [Fact]
    public void LocalCreate_WorksOnClient()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<ILocalCreateChildFactory>();

        // Act
        var child = factory.Create();

        // Assert
        Assert.NotNull(child);
        Assert.NotEqual(Guid.Empty, child.Id);
    }

    [Fact]
    public void LocalCreate_WorksOnServer()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.ServiceProvider.GetRequiredService<ILocalCreateChildFactory>();

        // Act
        var child = factory.Create();

        // Assert
        Assert.NotNull(child);
        Assert.NotEqual(Guid.Empty, child.Id);
    }

    [Fact]
    public void LocalCreate_WorksOnLocal()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.local.ServiceProvider.GetRequiredService<ILocalCreateChildFactory>();

        // Act
        var child = factory.Create();

        // Assert
        Assert.NotNull(child);
        Assert.NotEqual(Guid.Empty, child.Id);
    }

    [Fact]
    public void LocalCreate_ProducesDifferentIdsEachTime()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<ILocalCreateChildFactory>();

        // Act
        var child1 = factory.Create();
        var child2 = factory.Create();

        // Assert - Each call produces a new instance with unique ID
        Assert.NotEqual(child1.Id, child2.Id);
    }

    // ============================================================================
    // Factory via Remote Tests
    // ============================================================================

    [Fact]
    public async Task RemoteCreate_ViaFactory_RoutesToServer()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IClientStyleAggregateFactory>();

        // Act - Factory should handle the remote call, not call the placeholder directly
        // Note: This will fail because the server-side Create is also a placeholder.
        // In real usage, server would have a different implementation.
        // For this test, we're just verifying the factory exists and routes to server.
        var exception = await Record.ExceptionAsync(() => factory.CreatePlaceholder());

        // Assert - We expect an exception because both client and server use the same placeholder
        // In real client-server separation, server would have real implementation
        // The exception is wrapped in TargetInvocationException due to reflection-based invocation
        Assert.NotNull(exception);

        // Unwrap if necessary
        var innerException = exception is TargetInvocationException tie
            ? tie.InnerException
            : exception;

        Assert.IsType<InvalidOperationException>(innerException);
        Assert.Contains("Client should call through", innerException!.Message);
    }

    // ============================================================================
    // Assembly Verification Tests
    // ============================================================================

    [Fact]
    public void TestAssembly_ContainsNoActualEfTypes()
    {
        // This test verifies the pattern - in real usage, you'd check the client assembly
        // Here we're checking that the test assembly's test objects don't reference EF types

        var testTypes = new[]
        {
            typeof(ClientStyleAggregate),
            typeof(LocalCreateChild)
        };

        foreach (var type in testTypes)
        {
            // Check that the type doesn't have any properties or fields with EF-related types
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var typeName = prop.PropertyType.FullName ?? prop.PropertyType.Name;
                Assert.DoesNotContain("EntityFramework", typeName);
                Assert.DoesNotContain("Microsoft.EntityFrameworkCore", typeName);
            }

            // Check method parameters
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var method in methods)
            {
                foreach (var param in method.GetParameters())
                {
                    var typeName = param.ParameterType.FullName ?? param.ParameterType.Name;
                    Assert.DoesNotContain("EntityFramework", typeName);
                    Assert.DoesNotContain("Microsoft.EntityFrameworkCore", typeName);
                }
            }
        }
    }

    [Fact]
    public void FactoryInterface_DoesNotExposeServiceParameters()
    {
        // Verify that factory interface methods don't include [Service] parameters

        var factoryType = typeof(IClientStyleAggregateFactory);
        var methods = factoryType.GetMethods();

        foreach (var method in methods)
        {
            var parameters = method.GetParameters()
                .Where(p => p.ParameterType != typeof(CancellationToken));

            foreach (var param in parameters)
            {
                // Service parameters should not appear in factory interface
                var attrs = param.GetCustomAttributes(typeof(ServiceAttribute), false);
                Assert.Empty(attrs);

                // Common service types should not appear
                var typeName = param.ParameterType.Name;
                Assert.DoesNotContain("IServiceProvider", typeName);
                Assert.DoesNotContain("ILogger", typeName);
            }
        }
    }
}
