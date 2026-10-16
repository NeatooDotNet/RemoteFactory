// =============================================================================
// DESIGN SOURCE OF TRUTH: Entity Duality Tests
// =============================================================================
//
// OrderLine is a child of Order and also a root: a line-detail screen loads one
// line by its id (VisionAlignment R-G). The child operations are unchanged; the
// order cascade's own tests (AggregateTests) keep loading and saving lines.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.Entities;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

public class OrderLineDualityTests
{
    /// <summary>
    /// From the client, a line loads alone by its id, in one call, with the values
    /// its order carries for it.
    /// </summary>
    [Fact]
    public async Task OrderLine_FetchedAsARoot_ByItsId()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var order = await client.GetRequiredService<IOrderFactory>().Fetch(100);
        Assert.NotNull(order);
        var childLine = order.Lines.Single(l => l.ProductName == "Widget B");
        var calls = client.ServiceProvider.GetRequiredService<ServerServiceProvider>();
        var callsBefore = calls.RemoteCallCount;

        var line = await client.GetRequiredService<IOrderLineFactory>().Fetch(childLine.Id);

        Assert.Equal(callsBefore + 1, calls.RemoteCallCount);
        Assert.NotNull(line);
        Assert.Equal(childLine.Id, line.Id);
        Assert.Equal("Widget B", line.ProductName);
        Assert.Equal(25.00m, line.UnitPrice.Amount);
        Assert.Equal(1, line.Quantity);
        Assert.False(line.IsNew);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// A line id no order holds is a false Fetch: null on the client (R-I).
    /// </summary>
    [Fact]
    public async Task OrderLine_FetchedAsARoot_IsNull_WhenNoOrderHoldsIt()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();

        var line = await client.GetRequiredService<IOrderLineFactory>().Fetch(Guid.NewGuid());

        Assert.Null(line);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// The root Fetch leaves the child path alone: the order still loads the line
    /// through its row and saves a change to it through the cascade.
    /// </summary>
    [Fact]
    public async Task OrderLine_StaysAChild_OfItsOrder()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var orderFactory = client.GetRequiredService<IOrderFactory>();
        var order = await orderFactory.Fetch(InMemoryOrderDbContext.DraftOrderId);
        Assert.NotNull(order);
        var lineId = order.Lines.Single(l => l.ProductName == "Widget A").Id;
        Assert.NotNull(await client.GetRequiredService<IOrderLineFactory>().Fetch(lineId));

        order.Lines.Single(l => l.Id == lineId).UpdateQuantity(5);
        order = await orderFactory.Save(order);

        Assert.NotNull(order);
        var row = await server.ServiceProvider.GetRequiredService<IOrderDbContext>().FindLineAsync(lineId);
        Assert.NotNull(row);
        Assert.Equal(5, row.Quantity);

        server.Dispose();
        client.Dispose();
    }
}
