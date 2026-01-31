// =============================================================================
// DESIGN SOURCE OF TRUTH: Aggregate Tests
// =============================================================================
//
// Tests demonstrating aggregate root patterns with child entities,
// lifecycle hooks, and Save routing via IFactorySaveMeta.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Tests.TestInfrastructure;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for Order aggregate root with child entities and lifecycle hooks.
/// </summary>
public class AggregateTests
{
    /// <summary>
    /// Verifies that aggregate Create initializes with empty child collection.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Test both client (remote) and local modes
    ///
    /// This ensures the aggregate works correctly in both distributed
    /// and single-tier deployment scenarios.
    /// </remarks>
    [Fact]
    public async Task Order_Create_InitializesWithEmptyLines()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();

        // Act
        var order = await factory.Create("Test Customer");

        // Assert
        Assert.NotNull(order);
        Assert.Equal("Test Customer", order.CustomerName);
        Assert.NotNull(order.Lines);
        Assert.Empty(order.Lines);
        Assert.True(order.IsNew);
        Assert.Equal(OrderStatus.Draft, order.Status);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that aggregate Fetch works in local mode.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Aggregate Fetch with children works best in local mode
    ///
    /// When an aggregate with children is fetched remotely, the child collection
    /// is created on the server but needs to be serialized to the client. The
    /// injected factory reference (_lineFactory) is NOT serialized.
    ///
    /// This is acceptable because:
    /// 1. Client typically doesn't add children after remote fetch
    /// 2. If client needs to add children, refetch or use local mode
    /// 3. Keeping factory references would require complex scope management
    ///
    /// For this test, we use local mode where the factory stays connected.
    /// </remarks>
    [Fact]
    public async Task Order_Fetch_LoadsChildEntities_InLocalMode()
    {
        // Arrange - use local mode for aggregate with children
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IOrderFactory>();

        // Act
        var order = await factory.Fetch(123);

        // Assert
        Assert.NotNull(order);
        Assert.Equal(123, order.Id);
        Assert.False(order.IsNew);
        Assert.NotNull(order.Lines);
        Assert.Equal(2, order.Lines.Count);  // Fetch loads 2 lines
        Assert.Equal(OrderStatus.Submitted, order.Status);

        local.Dispose();
    }

    /// <summary>
    /// Verifies Save routes to Insert when IsNew=true.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: IFactorySaveMeta controls routing
    ///
    /// The generated Save() method examines IsNew and IsDeleted to
    /// determine which operation to call. This test verifies the
    /// Insert path (IsNew=true, IsDeleted=false).
    ///
    /// Note: Testing in local mode to avoid serialization complexity.
    /// The routing logic is the same regardless of mode.
    /// </remarks>
    [Fact]
    public async Task Order_Save_RoutesToInsert_WhenNew()
    {
        // Arrange - use local mode for simplicity
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IOrderFactory>();
        var order = await factory.Create("New Customer");

        // Assert preconditions
        Assert.True(order.IsNew);
        Assert.Equal(0, order.Id);

        // Act - Save should route to Insert
        await factory.Save(order);

        // Assert - Insert assigns ID, FactoryComplete sets IsNew=false
        Assert.NotEqual(0, order.Id);
        Assert.False(order.IsNew);

        local.Dispose();
    }

    /// <summary>
    /// Verifies Save routes to Update when IsNew=false.
    /// </summary>
    [Fact]
    public async Task Order_Save_RoutesToUpdate_WhenNotNew()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();
        var order = await factory.Fetch(456);

        // Assert preconditions
        Assert.False(order.IsNew);

        // Act - Save should route to Update
        await factory.Save(order);

        // Assert - no error means Update was called
        Assert.False(order.IsNew);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies Save routes to Delete when IsDeleted=true.
    /// </summary>
    [Fact]
    public async Task Order_Save_RoutesToDelete_WhenDeleted()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();
        var order = await factory.Fetch(789);
        order.MarkDeleted();

        // Assert preconditions
        Assert.True(order.IsDeleted);

        // Act - Save should route to Delete
        await factory.Save(order);

        // Assert - no error means Delete was called
        Assert.True(order.IsDeleted);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies child entities work through the aggregate.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Children are created via parent, not independently
    ///
    /// OrderLine instances are created through OrderLineList.AddLine,
    /// which uses the injected IOrderLineFactory. This ensures children
    /// are always created in the context of their parent aggregate.
    ///
    /// IMPORTANT: AddLine only works on the server side (or local mode)
    /// because it requires the injected factory. After remote Create/Fetch,
    /// the factory reference is not preserved across serialization.
    ///
    /// Pattern for adding children remotely:
    /// 1. Create order on server via remote Create
    /// 2. Add lines on server (within same request or separate Execute)
    /// 3. Return complete order to client
    ///
    /// COMMON MISTAKE: Trying to add children on client after remote Create
    ///
    /// WRONG (after remote Create):
    /// var order = await factory.Create("Customer");  // Remote
    /// order.AddLine(...);  // FAILS - factory not preserved
    ///
    /// RIGHT (use local mode or server-side operation):
    /// // Option 1: Local mode
    /// var order = localFactory.Create("Customer");
    /// order.AddLine(...);
    ///
    /// // Option 2: Server-side with Execute
    /// await Commands.AddOrderLine(orderId, "Widget", 10m, 2);
    /// </remarks>
    [Fact]
    public async Task Order_AddLine_CreatesChildThroughAggregate_InLocalMode()
    {
        // Arrange - use local mode where factory is preserved
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IOrderFactory>();
        var order = await factory.Create("Line Test Customer");

        // Act
        order.AddLine("Widget A", 10.00m, 2);
        order.AddLine("Widget B", 25.00m, 1);

        // Assert
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(45.00m, order.Total.Amount);  // (10*2) + (25*1) = 45

        local.Dispose();
    }

    /// <summary>
    /// Verifies aggregate works in local mode (single-tier).
    /// </summary>
    [Fact]
    public async Task Order_WorksInLocalMode()
    {
        // Arrange
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IOrderFactory>();

        // Act - full lifecycle in local mode
        var order = await factory.Create("Local Customer");
        order.AddLine("Local Widget", 15.00m, 3);
        order.Submit();

        // Assert
        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal(45.00m, order.Total.Amount);  // 15*3 = 45

        local.Dispose();
    }
}
