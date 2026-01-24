using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

namespace RemoteFactory.IntegrationTests.TypeSerialization;

/// <summary>
/// Tests for serialization round-trip of interface-typed collections.
///
/// These tests validate that RemoteFactory correctly handles:
/// - Single interface properties (baseline)
/// - Concrete collections with interface elements (List&lt;IInterface&gt;)
/// - Interface collections with interface elements (IList&lt;IInterface&gt;)
/// - Nested structures with interface collections
///
/// The key challenge is deserialization: while serialization has access to
/// runtime types, deserialization only sees declared types and must resolve
/// both the collection type and element types.
/// </summary>
public class InterfaceCollectionSerializationTests
{
    // ============================================================================
    // Phase 1: Baseline - Single Interface Property
    // ============================================================================

    /// <summary>
    /// Baseline test: single IInterface property should serialize/deserialize correctly.
    /// This uses the existing NeatooInterfaceJsonTypeConverter.
    /// </summary>
    [Fact]
    public async Task SingleInterface_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchSingleInterface();

        // Assert
        Assert.NotNull(result.SingleChild);
        Assert.IsType<TestChildImpl>(result.SingleChild); // Verify concrete type
        Assert.NotEqual(Guid.Empty, result.SingleChild.Id);
        Assert.Equal("SingleChild", result.SingleChild.Name);
        Assert.Equal(100.0m, result.SingleChild.Value);
    }

    // ============================================================================
    // Phase 2: Concrete Collection with Interface Elements
    // ============================================================================

    /// <summary>
    /// List&lt;IInterface&gt; - concrete collection type, interface elements.
    /// Serializer knows to create List&lt;T&gt;, but must handle interface elements.
    /// </summary>
    [Fact]
    public async Task ListOfInterface_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchListOfInterface();

        // Assert
        Assert.NotNull(result.ListOfInterface);
        Assert.Equal(2, result.ListOfInterface.Count);

        // Verify concrete types survived
        Assert.All(result.ListOfInterface, child =>
        {
            Assert.IsType<TestChildImpl>(child);
            Assert.NotEqual(Guid.Empty, child.Id);
            Assert.NotNull(child.Name);
            Assert.True(child.Value > 0);
        });

        // Verify specific values
        Assert.Equal("ListChild1", result.ListOfInterface[0].Name);
        Assert.Equal("ListChild2", result.ListOfInterface[1].Name);
    }

    /// <summary>
    /// Verify that element property values are preserved, not just types.
    /// </summary>
    [Fact]
    public async Task ListOfInterface_PreservesElementProperties()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchListOfInterface();

        // Assert
        Assert.Equal(10.0m, result.ListOfInterface[0].Value);
        Assert.Equal(20.0m, result.ListOfInterface[1].Value);
    }

    // ============================================================================
    // Phase 3: Interface Collection Types (Critical Tests)
    // ============================================================================

    /// <summary>
    /// IList&lt;IInterface&gt; - this is the critical test case.
    /// Serializer must determine both collection type AND element types.
    /// Currently excluded by !IsGenericType condition.
    /// </summary>
    [Fact]
    public async Task IListOfInterface_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchIListOfInterface();

        // Assert
        Assert.NotNull(result.IListOfInterface);
        Assert.Equal(2, result.IListOfInterface.Count);

        // Verify concrete types survived
        Assert.All(result.IListOfInterface, child =>
        {
            Assert.IsType<TestChildImpl>(child);
            Assert.NotEqual(Guid.Empty, child.Id);
        });

        Assert.Equal("IListChild1", result.IListOfInterface[0].Name);
        Assert.Equal("IListChild2", result.IListOfInterface[1].Name);
    }

    /// <summary>
    /// ICollection&lt;IInterface&gt; - another interface collection type.
    /// </summary>
    [Fact]
    public async Task ICollectionOfInterface_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchICollectionOfInterface();

        // Assert
        Assert.NotNull(result.ICollectionOfInterface);
        Assert.Single(result.ICollectionOfInterface);

        var child = result.ICollectionOfInterface.First();
        Assert.IsType<TestChildImpl>(child);
        Assert.Equal("ICollectionChild1", child.Name);
    }

    // ============================================================================
    // Phase 4: Nested Interface Collections
    // ============================================================================

    /// <summary>
    /// Nested: IParent with IList&lt;IChild&gt; property.
    /// Tests that interface resolution works through object graph.
    /// </summary>
    [Fact]
    public async Task NestedParentWithIListChildren_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchNestedParent();

        // Assert
        Assert.NotNull(result.NestedParent);
        Assert.IsType<TestParentImpl>(result.NestedParent); // Verify concrete parent type
        Assert.NotEqual(Guid.Empty, result.NestedParent.Id);
        Assert.StartsWith("Parent-", result.NestedParent.Name);

        // Verify children survived
        Assert.NotNull(result.NestedParent.Children);
        Assert.Equal(3, result.NestedParent.Children.Count);

        Assert.All(result.NestedParent.Children, child =>
        {
            Assert.IsType<TestChildImpl>(child);
            Assert.NotEqual(Guid.Empty, child.Id);
            Assert.NotNull(child.Name);
        });
    }

    // ============================================================================
    // Phase 5: Comprehensive Round-Trip
    // ============================================================================

    /// <summary>
    /// Tests all collection types in a single object.
    /// Useful for verifying no interference between collection types.
    /// </summary>
    [Fact]
    public async Task AllCollectionTypes_SurviveRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchWithAllCollectionTypes();

        // Assert - Each collection type should have correct values
        Assert.NotNull(result.SingleChild);
        Assert.NotEmpty(result.ListOfInterface);
        Assert.NotEmpty(result.IListOfInterface);
        Assert.NotEmpty(result.ICollectionOfInterface);
        Assert.NotEmpty(result.IReadOnlyListOfInterface);
        Assert.NotNull(result.NestedParent);
    }

    // ============================================================================
    // Phase 6: Save Operation Tests (Client-to-Server Direction)
    // ============================================================================

    /// <summary>
    /// Tests that modifications to List&lt;IInterface&gt; survive save round-trip.
    /// This tests client-to-server serialization direction.
    /// </summary>
    [Fact]
    public async Task ListOfInterface_ModificationsPreservedOnSave()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var containerFactory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        var container = await containerFactory.FetchListOfInterface();

        // Modify on client
        var originalCount = container.ListOfInterface.Count;
        container.ListOfInterface[0].Name = "ModifiedChild";
        container.ListOfInterface[0].Value = 999.0m;

        // Act - Save round-trips through serialization
        var saved = await containerFactory.Save(container);

        // Assert - Modifications should be preserved
        Assert.NotNull(saved);
        var savedList = saved!.ListOfInterface;
        Assert.NotNull(savedList);
        Assert.Equal(originalCount, savedList.Count);
        Assert.Equal("ModifiedChild", savedList[0].Name);
        Assert.Equal(999.0m, savedList[0].Value);
    }

    // ============================================================================
    // Phase 7: Server-Side Direct Tests (Isolation)
    // ============================================================================

    /// <summary>
    /// Server-side test to verify factory works without serialization.
    /// Helps isolate whether failures are in serialization or factory logic.
    /// </summary>
    [Fact]
    public async Task IListOfInterface_WorksDirectlyOnServer()
    {
        // Arrange - Use server container directly (no serialization)
        var (_, server, _) = ClientServerContainers.Scopes();
        var factory = server.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchIListOfInterface();

        // Assert
        Assert.NotNull(result.IListOfInterface);
        Assert.Equal(2, result.IListOfInterface.Count);
        Assert.All(result.IListOfInterface, child => Assert.IsType<TestChildImpl>(child));
    }

    /// <summary>
    /// Local container test - no remoting, single container.
    /// </summary>
    [Fact]
    public async Task IListOfInterface_WorksInLocalMode()
    {
        // Arrange
        var (_, _, local) = ClientServerContainers.Scopes();
        var factory = local.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchIListOfInterface();

        // Assert
        Assert.NotNull(result.IListOfInterface);
        Assert.Equal(2, result.IListOfInterface.Count);
    }

    // ============================================================================
    // Phase 8: JSON Structure Validation
    // ============================================================================

    /// <summary>
    /// Validates that serialized JSON includes $type/$value for interface elements.
    /// This test inspects the actual JSON to understand serialization behavior.
    /// </summary>
    [Fact]
    public async Task ListOfInterface_JsonIncludesTypeInformation()
    {
        // Arrange
        var (_, server, _) = ClientServerContainers.Scopes();
        var serializer = server.ServiceProvider.GetRequiredService<INeatooJsonSerializer>();
        var factory = server.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        var container = await factory.FetchListOfInterface();

        // Act - Serialize to JSON
        var json = serializer.Serialize(container);

        // Assert - JSON should contain type information for interface elements
        Assert.NotNull(json);
        Assert.Contains("$type", json);
        Assert.Contains("TestChildImpl", json);
        // Note: Exact format depends on serialization mode (Named vs Ordinal)
    }

    // ============================================================================
    // Phase 9: Empty Collection Tests
    // ============================================================================

    /// <summary>
    /// Empty IList&lt;IInterface&gt; should serialize/deserialize as empty collection.
    /// </summary>
    [Fact]
    public async Task EmptyIListOfInterface_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.Create();

        // Assert
        Assert.NotNull(result.IListOfInterface);
        Assert.Empty(result.IListOfInterface);
    }

    // ============================================================================
    // Phase 10: Null Collection Tests
    // ============================================================================

    /// <summary>
    /// Tests that null interface collections are handled correctly.
    /// </summary>
    [Fact]
    public async Task NullIListOfInterface_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        var container = await factory.Create();
        container.IListOfInterface = null!; // Intentionally null

        // Act
        var saved = await factory.Save(container);

        // Assert - Should be null or empty, not throw
        // Exact behavior depends on nullability handling
        Assert.NotNull(saved);
        var savedList = saved!.IListOfInterface;
        Assert.True(savedList is null || savedList.Count == 0);
    }
}
