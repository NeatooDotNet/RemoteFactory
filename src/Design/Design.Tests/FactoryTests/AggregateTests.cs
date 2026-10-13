// =============================================================================
// DESIGN SOURCE OF TRUTH: Aggregate Tests
// =============================================================================
//
// Tests demonstrating the Order aggregate from the client: a local Create, a
// remote Fetch, child lines created on the client (R-C), and the save cascade
// in which each object maps its own row (R-D).
//
// Every test runs on the client container, so each call that crosses goes
// through the serializing stand-in exactly as it would over HTTP.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.Entities;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for Order aggregate root with child entities and Save routing.
/// </summary>
public class AggregateTests
{
    /// <summary>
    /// Verifies that aggregate Create initializes with an empty child collection.
    /// </summary>
    [Fact]
    public void Order_Create_InitializesWithEmptyLines()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();

        // Act
        var order = factory.Create("Test Customer");

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
    /// Verifies that Create, building lines and editing them run on the client.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: A root's Create and a child's Create are local (D14, R-C)
    ///
    /// Nothing about a new draft order or a new line needs the server. The
    /// order first crosses when it is saved.
    /// </remarks>
    [Fact]
    public void Order_CreateAndEditLines_RunOnClientWithoutServerCall()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var orderFactory = client.GetRequiredService<IOrderFactory>();
        var lineFactory = client.GetRequiredService<IOrderLineFactory>();

        // Act
        var order = orderFactory.Create("Client Customer");
        var line = lineFactory.Create("Widget A", 10.00m, 2);
        order.AddLine(line);
        order.AddLine(lineFactory.Create("Widget B", 25.00m, 1));
        order.RemoveLine(line);

        // Assert
        Assert.Single(order.Lines);
        Assert.Equal(0, client.ServiceProvider.GetRequiredService<ServerServiceProvider>().RemoteCallCount);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that Fetch loads the order and its lines through the client.
    /// </summary>
    [Fact]
    public async Task Order_Fetch_LoadsChildEntities()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();

        // Act
        var order = await factory.Fetch(123);

        // Assert
        Assert.NotNull(order);
        Assert.Equal(123, order.Id);
        Assert.False(order.IsNew);
        Assert.NotNull(order.Lines);
        Assert.Equal(2, order.Lines.Count);  // Fetch loads 2 lines
        Assert.Equal(OrderStatus.Submitted, order.Status);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that Fetch returns null for an order that does not exist.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Fetch handles "not there" with false (R-I)
    /// </remarks>
    [Fact]
    public async Task Order_Fetch_ReturnsNull_WhenOrderDoesNotExist()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();

        var order = await factory.Fetch(999_999);

        Assert.Null(order);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies Save routes to Insert when IsNew=true, and the lines are written.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: The generated Save() method examines IsNew and IsDeleted
    /// to determine which operation to call.
    ///
    /// COMMON MISTAKE: Ignoring Save's return value
    ///
    /// Through the client, Save returns a NEW instance deserialized from the
    /// server. The instance passed in is unchanged: it still reports IsNew.
    /// Always reassign: order = await factory.Save(order).
    /// </remarks>
    [Fact]
    public async Task Order_Save_RoutesToInsert_WhenNew()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();
        var lineFactory = client.GetRequiredService<IOrderLineFactory>();
        var order = factory.Create("New Customer");
        order.AddLine(lineFactory.Create("Widget A", 10.00m, 2));
        order.AddLine(lineFactory.Create("Widget B", 25.00m, 1));

        // Assert preconditions
        Assert.True(order.IsNew);
        Assert.Equal(0, order.Id);

        // Act - Save should route to Insert
        var saved = await factory.Save(order);

        // Assert - Insert assigns the ID, FactoryComplete sets IsNew=false
        Assert.NotNull(saved);
        Assert.NotEqual(0, saved.Id);
        Assert.False(saved.IsNew);

        var refetched = await factory.Fetch(saved.Id);
        Assert.NotNull(refetched);
        Assert.Equal("New Customer", refetched.CustomerName);
        Assert.Equal(["Widget A", "Widget B"], refetched.Lines.Select(l => l.ProductName));
        Assert.Equal(45.00m, refetched.Total.Amount);

        server.Dispose();
        client.Dispose();
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
        Assert.NotNull(order);

        // Assert preconditions
        Assert.False(order.IsNew);

        // Act - Save should route to Update
        order.CustomerName = "Renamed";
        var saved = await factory.Save(order);

        // Assert
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);
        var refetched = await factory.Fetch(456);
        Assert.Equal("Renamed", refetched!.CustomerName);

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
        Assert.NotNull(order);
        order.MarkDeleted();

        // Assert preconditions
        Assert.True(order.IsDeleted);

        // Act - Save should route to Delete
        await factory.Save(order);

        // Assert - the row is gone
        Assert.Null(await factory.Fetch(789));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that saving an update for an order whose row was deleted throws.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: A missing row in Update is an exception (R-I)
    /// </remarks>
    [Fact]
    public async Task Order_Update_Throws_WhenRowWasDeleted()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();
        var stale = await factory.Fetch(456);
        var other = await factory.Fetch(456);
        other!.MarkDeleted();
        await factory.Save(other);

        stale!.CustomerName = "Too late";

        await Assert.ThrowsAsync<KeyNotFoundException>(() => factory.Save(stale));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that deleting an order whose row is already gone throws.
    /// </summary>
    [Fact]
    public async Task Order_Delete_Throws_WhenRowWasDeleted()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();
        var stale = await factory.Fetch(789);
        var other = await factory.Fetch(789);
        other!.MarkDeleted();
        await factory.Save(other);

        stale!.MarkDeleted();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => factory.Save(stale));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that lines cannot be removed from a submitted order.
    /// </summary>
    [Fact]
    public async Task Order_RemoveLine_Refused_WhenSubmitted()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();
        var order = await factory.Fetch(123);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Submitted, order.Status);

        Assert.Throws<InvalidOperationException>(() => order.RemoveLine(order.Lines[0]));
        Assert.Equal(2, order.Lines.Count);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies the R-D cascade from the client: change a line, remove a line,
    /// add a line, and one added then removed before saving.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Each object maps its own row (R-D)
    ///
    /// Order.Update finds its row and hands row.Lines to the list factory's Save.
    /// The list finds the changed line's row, removes the removed line's row,
    /// makes a row for the new line, and hands each live line its row. A line
    /// added and removed before the save was never stored, so it is dropped
    /// without a row.
    ///
    /// A removed line crosses to the server inside the list, flagged IsDeleted;
    /// the list's own properties do not cross the wire.
    /// </remarks>
    [Fact]
    public async Task Order_EditLinesFromClient_SavesExactlyThoseChanges()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();
        var lineFactory = client.GetRequiredService<IOrderLineFactory>();
        var order = await factory.Fetch(InMemoryOrderDbContext.DraftOrderId);
        Assert.NotNull(order);

        var widgetA = order.Lines.Single(l => l.ProductName == "Widget A");
        var widgetB = order.Lines.Single(l => l.ProductName == "Widget B");
        var widgetBId = widgetB.Id;

        // Act
        widgetA.UpdateQuantity(5);
        order.RemoveLine(widgetB);
        order.AddLine(lineFactory.Create("Widget C", 7.00m, 1));
        var discarded = lineFactory.Create("Widget D", 1.00m, 1);
        order.AddLine(discarded);
        order.RemoveLine(discarded);

        var saved = await factory.Save(order);

        // Assert - the client sees exactly the changes
        Assert.NotNull(saved);
        var refetched = await factory.Fetch(InMemoryOrderDbContext.DraftOrderId);
        Assert.NotNull(refetched);
        Assert.Equal(["Widget A", "Widget C"], refetched.Lines.Select(l => l.ProductName).Order());
        Assert.Equal(5, refetched.Lines.Single(l => l.ProductName == "Widget A").Quantity);

        // Assert - the store holds exactly those rows
        var row = await server.ServiceProvider.GetRequiredService<IOrderDbContext>()
            .FindAsync(InMemoryOrderDbContext.DraftOrderId);
        Assert.NotNull(row);
        Assert.Equal(["Widget A", "Widget C"], row.Lines.Select(l => l.ProductName).Order());
        Assert.DoesNotContain(row.Lines, l => l.Id == widgetBId);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies the order a save returns can be edited and saved again.
    /// </summary>
    /// <remarks>
    /// A line is not new once inserted, and a removed line leaves the list once
    /// its row is deleted, so the returned aggregate is clean. Saving it again
    /// updates the inserted lines instead of inserting them twice.
    /// </remarks>
    [Fact]
    public async Task Order_SavedResult_CanBeEditedAndSavedAgain()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();
        var lineFactory = client.GetRequiredService<IOrderLineFactory>();
        var order = factory.Create("Twice");
        order.AddLine(lineFactory.Create("Widget A", 10.00m, 1));
        order.AddLine(lineFactory.Create("Widget B", 25.00m, 1));
        var saved = await factory.Save(order);
        Assert.NotNull(saved);

        // Act - edit the returned instance and save it again
        saved.Lines.Single(l => l.ProductName == "Widget A").UpdateQuantity(3);
        saved.RemoveLine(saved.Lines.Single(l => l.ProductName == "Widget B"));
        saved.AddLine(lineFactory.Create("Widget C", 7.00m, 1));
        var savedAgain = await factory.Save(saved);

        // Assert
        Assert.NotNull(savedAgain);
        Assert.Equal(["Widget A", "Widget C"], savedAgain.Lines.Select(l => l.ProductName).Order());
        Assert.All(savedAgain.Lines, l => Assert.False(l.IsNew));

        var row = await server.ServiceProvider.GetRequiredService<IOrderDbContext>().FindAsync(saved.Id);
        Assert.NotNull(row);
        Assert.Equal(2, row.Lines.Count);
        Assert.Equal(3, row.Lines.Single(l => l.ProductName == "Widget A").Quantity);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies that after a save that removed a line, the returned order can be
    /// saved again.
    /// </summary>
    /// <remarks>
    /// The removed line's row is deleted by the first save, and the line leaves
    /// the list the save returns. If it stayed, flagged IsDeleted, the second save
    /// would look for a row that no longer exists.
    /// </remarks>
    [Fact]
    public async Task Order_SavedResultAfterRemoval_CanBeSavedAgain()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();
        var order = await factory.Fetch(InMemoryOrderDbContext.DraftOrderId);
        Assert.NotNull(order);
        order.RemoveLine(order.Lines.Single(l => l.ProductName == "Widget B"));
        var saved = await factory.Save(order);
        Assert.NotNull(saved);

        // Act - edit the returned instance and save it again
        saved.Lines.Single(l => l.ProductName == "Widget A").UpdateQuantity(4);
        var savedAgain = await factory.Save(saved);

        // Assert
        Assert.NotNull(savedAgain);
        var row = await server.ServiceProvider.GetRequiredService<IOrderDbContext>()
            .FindAsync(InMemoryOrderDbContext.DraftOrderId);
        Assert.NotNull(row);
        var widgetA = Assert.Single(row.Lines);
        Assert.Equal("Widget A", widgetA.ProductName);
        Assert.Equal(4, widgetA.Quantity);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies a removed line is hidden from Lines and Total before the save.
    /// </summary>
    [Fact]
    public async Task Order_RemovedLine_HiddenFromLinesAndTotal_BeforeSave()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();
        var order = await factory.Fetch(InMemoryOrderDbContext.DraftOrderId);
        Assert.NotNull(order);

        order.RemoveLine(order.Lines.Single(l => l.ProductName == "Widget B"));

        Assert.Single(order.Lines);
        Assert.Equal("Widget A", order.Lines[0].ProductName);
        Assert.Equal(20.00m, order.Total.Amount);  // 10*2; Widget B (25*1) is gone

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies the aggregate's domain methods on the client.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: The client builds a line with the line factory, then adds it (R-C)
    ///
    /// DID NOT DO THIS: AddLine(name, price, quantity) creating through a factory
    /// stored on the list. A method-injected service stored in a field does not
    /// survive serialization, so after a remote fetch the client could not add.
    /// </remarks>
    [Fact]
    public void Order_DomainMethods_WorkOnClient()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();
        var lineFactory = client.GetRequiredService<IOrderLineFactory>();

        // Act - full lifecycle on the client
        var order = factory.Create("Client Customer");
        order.AddLine(lineFactory.Create("Client Widget", 15.00m, 3));
        order.Submit();

        // Assert
        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.Equal(45.00m, order.Total.Amount);  // 15*3 = 45

        server.Dispose();
        client.Dispose();
    }
}
