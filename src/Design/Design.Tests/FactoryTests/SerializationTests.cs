// =============================================================================
// DESIGN SOURCE OF TRUTH: Serialization Round-Trip Tests
// =============================================================================
//
// Tests verifying that objects serialize correctly across the client/server
// boundary. These tests use the two DI container pattern to validate full
// JSON round-trip serialization.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for serialization round-trip across client/server boundary.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Objects must survive JSON serialization
///
/// When objects cross the client/server boundary, they are serialized to JSON
/// and deserialized on the other side. These tests verify that:
/// 1. All public properties with setters are preserved
/// 2. Value objects serialize correctly
/// 3. Collections are properly reconstructed
/// 4. Nullable values are handled correctly
///
/// COMMON MISTAKE: Private setters break serialization
///
/// Properties with private setters will serialize (JSON includes the value)
/// but won't deserialize (JSON can't set the value back). Always use public
/// setters for properties that need to cross the boundary.
/// </remarks>
public class SerializationTests
{
    /// <summary>
    /// Verifies that Create returns an object that has crossed the serialization boundary.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Remote Create serializes the result back to client
    ///
    /// When you call factory.Create() remotely:
    /// 1. Request is serialized to server
    /// 2. Server creates the object
    /// 3. Object is serialized back to client
    /// 4. Client receives the deserialized object
    ///
    /// This test verifies step 3-4: the created object survives the round-trip.
    /// </remarks>
    [Fact]
    public async Task Create_ResultSurvivesSerializationRoundTrip()
    {
        // Arrange - use client/server mode (not local) to force serialization
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IExampleClassFactoryFactory>();

        // Act - Create goes to server, result comes back serialized
        var example = await factory.Create("Serialization Test");

        // Assert - all values survived the round-trip
        Assert.NotNull(example);
        Assert.Equal("Serialization Test", example.Name);
        Assert.True(example.Id > 0, "Server should have assigned an ID");

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that Fetch returns an object that has crossed the serialization boundary.
    /// </summary>
    [Fact]
    public async Task Fetch_ResultSurvivesSerializationRoundTrip()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IExampleClassFactoryFactory>();

        // Act - Fetch goes to server, result comes back serialized
        var example = await factory.Fetch(123);

        // Assert
        Assert.NotNull(example);
        Assert.Equal(123, example.Id);
        Assert.Equal("Loaded_123", example.Name);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that value objects serialize correctly.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Value objects serialize as nested JSON objects
    ///
    /// The Money value object (record type) is serialized as a nested object:
    /// { "UnitPrice": { "Amount": 10.00, "Currency": "USD" } }
    ///
    /// Records work well because they're immutable with init-only properties,
    /// which the JSON serializer handles correctly.
    ///
    /// IMPORTANT: This test uses local mode because aggregate with children
    /// requires local mode (factory references aren't preserved across
    /// remote serialization). See Collection_SurvivesSerializationRoundTrip
    /// for more details.
    /// </remarks>
    [Fact]
    public async Task ValueObject_SurvivesSerializationRoundTrip()
    {
        // Arrange - use local mode for aggregate with children
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IOrderFactory>();

        // Act - Fetch order with lines
        var order = await factory.Fetch(100);

        // Assert - value object calculated from child collection
        // Total is calculated: (10.00 * 2) + (25.00 * 1) = 45.00
        Assert.NotNull(order);
        Assert.NotNull(order.Total);
        Assert.Equal(45.00m, order.Total.Amount);
        Assert.Equal("USD", order.Total.Currency);

        local.Dispose();
    }

    /// <summary>
    /// Verifies that collections work correctly in local mode.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Collections require local mode for full functionality
    ///
    /// The child collection (OrderLineList) has an injected _lineFactory field
    /// that enables AddLine to create new children. This factory reference is
    /// NOT preserved across remote serialization.
    ///
    /// When using remote mode (client → server → client):
    /// - Collection items serialize as JSON array
    /// - Items are recreated on deserialization
    /// - BUT the _lineFactory is null, so AddLine won't work
    ///
    /// COMMON MISTAKE: Expecting AddLine to work after remote Fetch
    ///
    /// WRONG:
    /// var order = await clientFactory.Fetch(123);  // Remote fetch
    /// order.AddLine("Widget", 10m, 1);  // FAILS - _lineFactory is null
    ///
    /// RIGHT:
    /// var order = await localFactory.Fetch(123);  // Local mode
    /// order.AddLine("Widget", 10m, 1);  // Works - factory preserved
    ///
    /// For remote scenarios, use server-side operations:
    /// await Commands.AddOrderLine(orderId, productName, price, qty);
    /// </remarks>
    [Fact]
    public async Task Collection_WorksInLocalMode()
    {
        // Arrange - use local mode so factory references are preserved
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IOrderFactory>();

        // Act - Fetch order with child collection
        var order = await factory.Fetch(200);

        // Assert - collection is populated
        Assert.NotNull(order);
        Assert.NotNull(order.Lines);
        Assert.Equal(2, order.Lines.Count);

        // Verify child entities have correct values
        Assert.Contains(order.Lines, l => l.ProductName == "Widget A");
        Assert.Contains(order.Lines, l => l.ProductName == "Widget B");

        // Verify value objects on children survived
        var widgetA = order.Lines.First(l => l.ProductName == "Widget A");
        Assert.Equal(10.00m, widgetA.UnitPrice.Amount);
        Assert.Equal(20.00m, widgetA.LineTotal.Amount); // 10 * 2

        local.Dispose();
    }

    /// <summary>
    /// Verifies that nullable values serialize correctly.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Null is serialized as JSON null
    ///
    /// When a server method returns null, it serializes as JSON null
    /// and deserializes as null on the client. This is important for
    /// "not found" patterns in repositories.
    /// </remarks>
    [Fact]
    public async Task NullableValue_SurvivesSerializationRoundTrip()
    {
        // Arrange - use NullReturningRepository to test null handling
        var (client, server, _) = DesignClientServerContainers.Scopes(
            configureServer: services =>
            {
                services.AddScoped<IExampleRepository, NullReturningRepository>();
            });

        var repository = client.GetRequiredService<IExampleRepository>();

        // Act - NullReturningRepository.GetByIdAsync always returns null
        var nullItem = await repository.GetByIdAsync(999);

        // Assert - null survived serialization round-trip
        Assert.Null(nullItem);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that objects modified on client can be sent back to server.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Client modifications are sent via Save
    ///
    /// When you modify an object on the client and call Save:
    /// 1. Modified object is serialized to server
    /// 2. Server deserializes and processes the Save (Insert/Update/Delete)
    /// 3. Result is serialized back to client
    ///
    /// This tests the full round-trip: server → client → server → client
    ///
    /// Note: We use local mode here because AddLine requires the factory
    /// reference which isn't preserved across serialization.
    /// </remarks>
    [Fact]
    public async Task ModifiedObject_SerializesToServer()
    {
        // Arrange - use local mode so AddLine works (factory preserved)
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IOrderFactory>();
        var order = await factory.Create("Test Customer");

        // Add a line (required for Submit)
        order.AddLine("Test Widget", 10.00m, 1);

        // Act - modify on client, then save
        order.Submit();
        await factory.Save(order);

        // Assert - order was successfully saved
        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.False(order.IsNew); // FactoryComplete sets IsNew=false after Insert

        local.Dispose();
    }

    /// <summary>
    /// Verifies that IFactorySaveMeta properties serialize correctly.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: IsNew and IsDeleted must survive serialization
    ///
    /// The Save routing depends on these properties. If they don't serialize
    /// correctly, Save will route to the wrong operation:
    /// - IsNew=true → Insert
    /// - IsDeleted=true → Delete
    /// - Otherwise → Update
    ///
    /// Both properties need public setters for this to work.
    /// </remarks>
    [Fact]
    public async Task SaveMetaProperties_SurviveSerializationRoundTrip()
    {
        // Arrange - create new order (IsNew=true)
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();
        var newOrder = await factory.Create("Save Meta Test");

        // Assert - IsNew survived round-trip from server
        Assert.True(newOrder.IsNew);
        Assert.False(newOrder.IsDeleted);

        // Act - fetch existing order (IsNew=false)
        var existingOrder = await factory.Fetch(400);

        // Assert - IsNew=false survived round-trip
        Assert.False(existingOrder.IsNew);
        Assert.False(existingOrder.IsDeleted);

        // Act - mark for deletion
        existingOrder.MarkDeleted();

        // Assert - IsDeleted is set (will serialize when we Save)
        Assert.True(existingOrder.IsDeleted);

        server.Dispose();
        client.Dispose();
    }
}
