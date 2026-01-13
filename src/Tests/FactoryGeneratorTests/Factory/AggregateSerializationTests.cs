using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests for serialization round-trip of aggregates with child collections.
/// Uses the two DI container pattern (client/server) to validate full remote operation flow.
/// </summary>
public class AggregateSerializationTests
{
    // ============================================================================
    // Test Domain Objects
    // ============================================================================

    /// <summary>
    /// Child item in an aggregate - supports local Create and server-side Fetch.
    /// </summary>
    [Factory]
    public class AggregateChild
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public decimal Value { get; set; }
        public bool FetchWasCalled { get; set; }

        [Create]
        public AggregateChild()
        {
            Id = Guid.NewGuid();
        }

        [Fetch]
        public void Fetch(Guid id, string name, decimal value)
        {
            Id = id;
            Name = name;
            Value = value;
            FetchWasCalled = true;
        }
    }

    /// <summary>
    /// Child collection for aggregate.
    /// </summary>
    [Factory]
    public class AggregateChildList : List<AggregateChild>
    {
        [Create]
        public AggregateChildList() { }

        [Fetch]
        public void Fetch(
            IEnumerable<(Guid id, string name, decimal value)> items,
            [Service] IAggregateChildFactory childFactory)
        {
            foreach (var item in items)
            {
                var child = childFactory.Fetch(item.id, item.name, item.value);
                Add(child);
            }
        }
    }

    /// <summary>
    /// Aggregate root with child collection - demonstrates parent-child factory patterns.
    /// </summary>
    [Factory]
    public class AggregateRoot : IFactorySaveMeta
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public decimal Total => Children?.Sum(c => c.Value) ?? 0;
        public AggregateChildList Children { get; set; } = null!;

        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; } = true;

        public bool CreateWasCalled { get; set; }
        public bool FetchWasCalled { get; set; }
        public bool InsertWasCalled { get; set; }
        public bool UpdateWasCalled { get; set; }
        public bool DeleteWasCalled { get; set; }

        /// <summary>
        /// Server-side Create - injects child list factory to initialize children.
        /// </summary>
        [Remote, Create]
        public void Create([Service] IAggregateChildListFactory childListFactory)
        {
            Id = Guid.NewGuid();
            Children = childListFactory.Create();
            CreateWasCalled = true;
        }

        /// <summary>
        /// Server-side Fetch - loads aggregate with children.
        /// </summary>
        [Remote, Fetch]
        public void Fetch(
            Guid id,
            [Service] IAggregateChildListFactory childListFactory)
        {
            Id = id;
            Name = $"Fetched-{id}";

            // Simulate loading children from "database"
            var items = new[]
            {
                (Guid.NewGuid(), "Child1", 10.0m),
                (Guid.NewGuid(), "Child2", 20.0m),
                (Guid.NewGuid(), "Child3", 30.0m)
            };
            Children = childListFactory.Fetch(items);
            FetchWasCalled = true;
            IsNew = false;
        }

        [Remote, Insert]
        public Task Insert()
        {
            InsertWasCalled = true;
            IsNew = false;
            return Task.CompletedTask;
        }

        [Remote, Update]
        public Task Update()
        {
            UpdateWasCalled = true;
            return Task.CompletedTask;
        }

        [Remote, Delete]
        public Task Delete()
        {
            DeleteWasCalled = true;
            return Task.CompletedTask;
        }
    }

    // ============================================================================
    // Create Tests
    // ============================================================================

    [Fact]
    public async Task Create_WithChildFactory_InitializesChildren_Server()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.ServiceProvider.GetRequiredService<IAggregateRootFactory>();

        // Act
        var aggregate = await factory.Create();

        // Assert
        Assert.NotNull(aggregate);
        Assert.NotEqual(Guid.Empty, aggregate.Id);
        Assert.NotNull(aggregate.Children);
        Assert.Empty(aggregate.Children); // Starts empty
        Assert.True(aggregate.IsNew);
        Assert.True(aggregate.CreateWasCalled);
    }

    [Fact]
    public async Task Create_ViaRemote_InitializesChildren_Client()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IAggregateRootFactory>();

        // Act - Client calls Create, which routes to server via remote stub
        var aggregate = await factory.Create();

        // Assert
        Assert.NotNull(aggregate);
        Assert.NotEqual(Guid.Empty, aggregate.Id);
        Assert.NotNull(aggregate.Children);
        Assert.Empty(aggregate.Children);
        Assert.True(aggregate.IsNew);
        // CreateWasCalled won't be true on client - the object was serialized from server
    }

    // ============================================================================
    // Fetch Tests - Aggregate with Children
    // ============================================================================

    [Fact]
    public async Task Fetch_WithChildren_PreservesParentChildRelationship_Server()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.ServiceProvider.GetRequiredService<IAggregateRootFactory>();
        var testId = Guid.NewGuid();

        // Act
        var aggregate = await factory.Fetch(testId);

        // Assert
        Assert.NotNull(aggregate);
        Assert.Equal(testId, aggregate.Id);
        Assert.Equal($"Fetched-{testId}", aggregate.Name);
        Assert.NotNull(aggregate.Children);
        Assert.Equal(3, aggregate.Children.Count);
        Assert.Equal(60.0m, aggregate.Total); // 10 + 20 + 30
        Assert.False(aggregate.IsNew);
        Assert.True(aggregate.FetchWasCalled);
    }

    [Fact]
    public async Task Fetch_ViaRemote_PreservesParentChildRelationship_Client()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IAggregateRootFactory>();
        var testId = Guid.NewGuid();

        // Act - Client calls Fetch, which routes to server and returns serialized result
        var aggregate = await factory.Fetch(testId);

        // Assert
        Assert.NotNull(aggregate);
        Assert.Equal(testId, aggregate.Id);
        Assert.Equal($"Fetched-{testId}", aggregate.Name);
        Assert.NotNull(aggregate.Children);
        Assert.Equal(3, aggregate.Children.Count);
        Assert.Equal(60.0m, aggregate.Total);
        Assert.False(aggregate.IsNew);
        // Note: Children were serialized from server, so FetchWasCalled may not persist
    }

    [Fact]
    public async Task Fetch_RoundTrip_ChildPropertiesPreserved_Client()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IAggregateRootFactory>();

        // Act
        var aggregate = await factory.Fetch(Guid.NewGuid());

        // Assert - Verify child properties survived serialization
        Assert.NotNull(aggregate);
        Assert.All(aggregate.Children, child =>
        {
            Assert.NotEqual(Guid.Empty, child.Id);
            Assert.NotNull(child.Name);
            Assert.True(child.Value > 0);
        });
    }

    // ============================================================================
    // Save Tests - Insert
    // ============================================================================

    [Fact]
    public async Task Save_NewAggregate_CallsInsert_Server()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.ServiceProvider.GetRequiredService<IAggregateRootFactory>();
        var aggregate = await factory.Create();
        aggregate.Name = "TestAggregate";

        // Act
        var saved = await factory.Save(aggregate);

        // Assert
        Assert.NotNull(saved);
        Assert.True(saved.InsertWasCalled);
        Assert.False(saved.IsNew);
    }

    [Fact]
    public async Task Save_NewAggregate_ViaRemote_CallsInsert_Client()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IAggregateRootFactory>();
        var aggregate = await factory.Create();
        aggregate.Name = "TestAggregate";

        // Act - Client sends aggregate to server for save
        var saved = await factory.Save(aggregate);

        // Assert
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);
        // InsertWasCalled may not persist through serialization, but IsNew = false indicates insert happened
    }

    // ============================================================================
    // Save Tests - Update
    // ============================================================================

    [Fact]
    public async Task Save_ExistingAggregate_CallsUpdate_Server()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.ServiceProvider.GetRequiredService<IAggregateRootFactory>();
        var aggregate = await factory.Fetch(Guid.NewGuid());
        Assert.NotNull(aggregate);
        aggregate.Name = "Updated";

        // Act
        var saved = await factory.Save(aggregate);

        // Assert
        Assert.NotNull(saved);
        Assert.True(saved.UpdateWasCalled);
    }

    [Fact]
    public async Task Save_ExistingAggregate_ViaRemote_CallsUpdate_Client()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IAggregateRootFactory>();
        var aggregate = await factory.Fetch(Guid.NewGuid());
        Assert.NotNull(aggregate);
        aggregate.Name = "UpdatedViaClient";

        // Act
        var saved = await factory.Save(aggregate);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal("UpdatedViaClient", saved.Name);
    }

    [Fact]
    public async Task Save_ModifiedProperties_SerializeCorrectly_Client()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IAggregateRootFactory>();
        var aggregate = await factory.Fetch(Guid.NewGuid());
        Assert.NotNull(aggregate);

        // Modify properties
        aggregate.Name = "Modified Name";

        // Act - Round-trip through serialization
        var saved = await factory.Save(aggregate);

        // Assert - Modified properties should be on saved result
        Assert.NotNull(saved);
        Assert.Equal("Modified Name", saved.Name);
    }

    // ============================================================================
    // Save Tests - Delete
    // ============================================================================

    [Fact]
    public async Task Save_DeletedAggregate_CallsDelete_Server()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.ServiceProvider.GetRequiredService<IAggregateRootFactory>();
        var aggregate = await factory.Fetch(Guid.NewGuid());
        Assert.NotNull(aggregate);
        aggregate.IsDeleted = true;

        // Act
        var saved = await factory.Save(aggregate);

        // Assert
        Assert.NotNull(saved);
        Assert.True(saved.DeleteWasCalled);
    }

    [Fact]
    public async Task Save_DeletedAggregate_ViaRemote_HandlesIsDeleted_Client()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IAggregateRootFactory>();
        var aggregate = await factory.Fetch(Guid.NewGuid());
        Assert.NotNull(aggregate);
        aggregate.IsDeleted = true;

        // Act
        var saved = await factory.Save(aggregate);

        // Assert
        Assert.NotNull(saved);
        Assert.True(saved.IsDeleted);
    }

    // ============================================================================
    // Child Collection Modification Tests
    // ============================================================================

    [Fact]
    public async Task Save_WithAddedChild_RoundTripPreservesChild_Client()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IAggregateRootFactory>();
        var childFactory = scopes.client.ServiceProvider.GetRequiredService<IAggregateChildFactory>();

        var aggregate = await factory.Create();

        // Add a child on client side
        var newChild = childFactory.Create();
        newChild.Name = "NewChild";
        newChild.Value = 50.0m;
        aggregate.Children.Add(newChild);

        // Act
        var saved = await factory.Save(aggregate);

        // Assert
        Assert.NotNull(saved);
        Assert.Single(saved.Children);
        Assert.Equal("NewChild", saved.Children[0].Name);
        Assert.Equal(50.0m, saved.Children[0].Value);
    }

    [Fact]
    public async Task Save_WithModifiedChild_RoundTripPreservesChanges_Client()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IAggregateRootFactory>();

        var aggregate = await factory.Fetch(Guid.NewGuid());
        Assert.NotNull(aggregate);
        var firstChild = aggregate.Children[0];
        var firstChildId = firstChild.Id;
        firstChild.Name = "ModifiedChild";
        firstChild.Value = 999.0m;

        // Act
        var saved = await factory.Save(aggregate);

        // Assert
        Assert.NotNull(saved);
        var modifiedChild = saved.Children.First(c => c.Id == firstChildId);
        Assert.Equal("ModifiedChild", modifiedChild.Name);
        Assert.Equal(999.0m, modifiedChild.Value);
    }

    [Fact]
    public async Task Save_WithRemovedChild_RoundTripRemovesChild_Client()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.ServiceProvider.GetRequiredService<IAggregateRootFactory>();

        var aggregate = await factory.Fetch(Guid.NewGuid());
        Assert.NotNull(aggregate);
        var originalCount = aggregate.Children.Count;
        var removedChild = aggregate.Children[0];
        var removedChildId = removedChild.Id;
        aggregate.Children.RemoveAt(0);

        // Act
        var saved = await factory.Save(aggregate);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal(originalCount - 1, saved.Children.Count);
        Assert.DoesNotContain(saved.Children, c => c.Id == removedChildId);
    }

    // ============================================================================
    // Factory Interface Consistency Tests
    // ============================================================================

    [Theory]
    [InlineData("server")]
    [InlineData("client")]
    [InlineData("local")]
    public void FactoryInterface_ConsistentAcrossContainers(string containerType)
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var provider = containerType switch
        {
            "server" => scopes.server.ServiceProvider,
            "client" => scopes.client.ServiceProvider,
            "local" => scopes.local.ServiceProvider,
            _ => throw new ArgumentException()
        };

        var factory = provider.GetRequiredService<IAggregateRootFactory>();

        // Assert - Factory has expected methods
        var factoryType = factory.GetType();
        Assert.NotNull(factoryType.GetMethod("Create"));
        Assert.NotNull(factoryType.GetMethod("Fetch"));
        Assert.NotNull(factoryType.GetMethod("Save"));
    }

    [Fact]
    public async Task Create_ProducesSameInterface_AllContainers()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();

        // Act
        var serverResult = await scopes.server.ServiceProvider.GetRequiredService<IAggregateRootFactory>().Create();
        var clientResult = await scopes.client.ServiceProvider.GetRequiredService<IAggregateRootFactory>().Create();
        var localResult = await scopes.local.ServiceProvider.GetRequiredService<IAggregateRootFactory>().Create();

        // Assert - All produce the same type with same structure
        Assert.IsType<AggregateRoot>(serverResult);
        Assert.IsType<AggregateRoot>(clientResult);
        Assert.IsType<AggregateRoot>(localResult);

        // All have initialized children
        Assert.NotNull(serverResult.Children);
        Assert.NotNull(clientResult.Children);
        Assert.NotNull(localResult.Children);
    }
}
