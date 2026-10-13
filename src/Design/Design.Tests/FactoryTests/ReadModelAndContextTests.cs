// =============================================================================
// DESIGN SOURCE OF TRUTH: Read Model and Context Tests
// =============================================================================
//
// OrderSummary: a [Fetch]-only class the client holds (R-H, R-I).
// OrderCheckout: a context a screen binds to; its verbs act on the held order
// on the client and it saves through the order's root factory (D13).
//
// Every test runs on the client container, so each call that crosses goes
// through the serializing stand-in.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.Contexts;
using Design.Domain.FactoryPatterns;
using Design.Domain.ReadModels;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

public class ReadModelAndContextTests
{
    // -------------------------------------------------------------------------
    // Read model
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies the order summary loads from the client with its line records.
    /// </summary>
    [Fact]
    public async Task OrderSummary_FetchedThroughClient_ArrivesWithLineRecords()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderSummaryFactory>();

        var summary = await factory.Fetch(123);

        Assert.NotNull(summary);
        Assert.Equal(123, summary.OrderId);
        Assert.Equal("Customer_123", summary.CustomerName);
        Assert.Equal(OrderStatus.Submitted, summary.Status);
        Assert.Equal(
            [new OrderSummaryLine("Widget A", 2, 20.00m), new OrderSummaryLine("Widget B", 1, 25.00m)],
            summary.Lines);
        Assert.Equal(45.00m, summary.Total);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies the order summary is null for an order that does not exist.
    /// </summary>
    [Fact]
    public async Task OrderSummary_ReturnsNull_WhenOrderDoesNotExist()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderSummaryFactory>();

        Assert.Null(await factory.Fetch(999_999));

        server.Dispose();
        client.Dispose();
    }

    // -------------------------------------------------------------------------
    // Context
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies the checkout context loads with its order, lines and values in one call.
    /// </summary>
    /// <remarks>
    /// The context has a constructor that takes services, so it crosses in named
    /// form and is built through DI on the client; its IOrder property crosses
    /// through the interface converter with the order's lines inside it.
    /// </remarks>
    [Fact]
    public async Task OrderCheckout_LoadsWithItsOrder_InOneCall()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderCheckoutFactory>();

        var checkout = await factory.Fetch(InMemoryOrderDbContext.DraftOrderId);

        Assert.NotNull(checkout);
        Assert.Equal(1, client.ServiceProvider.GetRequiredService<ServerServiceProvider>().RemoteCallCount);
        Assert.Equal(InMemoryOrderDbContext.DraftOrderId, checkout.Order.Id);
        Assert.Equal(OrderStatus.Draft, checkout.Order.Status);
        Assert.Equal(["Widget A", "Widget B"], checkout.Order.Lines.Select(l => l.ProductName));
        Assert.Equal(2, checkout.Order.Lines.Single(l => l.ProductName == "Widget A").Quantity);
        Assert.Equal(45.00m, checkout.Order.Total.Amount);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies the context is null for an order that does not exist.
    /// </summary>
    [Fact]
    public async Task OrderCheckout_ReturnsNull_WhenOrderDoesNotExist()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderCheckoutFactory>();

        Assert.Null(await factory.Fetch(999_999));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies the context's verbs act on the held order on the client, and
    /// saving persists through the order's root Save.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Verbs are plain instance methods on the context (D13)
    ///
    /// AddProduct and Submit make no server call: the line factory came through
    /// the context's constructor on the client. SaveAsync is the one call that
    /// crosses, and the context then holds the saved order.
    /// </remarks>
    [Fact]
    public async Task OrderCheckout_VerbsRunOnClient_AndSaveGoesThroughTheRoot()
    {
        // Arrange - the screen holds a product it found earlier
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var widgetC = await client.GetRequiredService<IProductCatalog>().FindBySkuAsync("WC-300");
        Assert.NotNull(widgetC);
        var checkout = await client.GetRequiredService<IOrderCheckoutFactory>().Fetch(InMemoryOrderDbContext.DraftOrderId);
        Assert.NotNull(checkout);
        var calls = client.ServiceProvider.GetRequiredService<ServerServiceProvider>();
        var callsBefore = calls.RemoteCallCount;

        // Act - verbs on the client
        checkout.AddProduct(widgetC, 3);
        checkout.Submit();

        // Assert - no server call yet
        Assert.Equal(callsBefore, calls.RemoteCallCount);
        Assert.Equal(OrderStatus.Submitted, checkout.Order.Status);

        // Act - save through the root
        var before = checkout.Order;
        await checkout.SaveAsync();

        // Assert - one call, and the context holds the saved order
        Assert.Equal(callsBefore + 1, calls.RemoteCallCount);
        Assert.NotSame(before, checkout.Order);
        Assert.False(checkout.Order.IsNew);

        var row = await server.ServiceProvider.GetRequiredService<IOrderDbContext>()
            .FindAsync(InMemoryOrderDbContext.DraftOrderId);
        Assert.NotNull(row);
        Assert.Equal(OrderStatus.Submitted, row.Status);
        Assert.Equal(3, row.Lines.Single(l => l.ProductName == "Widget C").Quantity);

        server.Dispose();
        client.Dispose();
    }
}
