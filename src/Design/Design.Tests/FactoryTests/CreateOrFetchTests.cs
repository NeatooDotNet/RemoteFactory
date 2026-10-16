// =============================================================================
// DESIGN SOURCE OF TRUTH: Create-or-fetch Tests
// =============================================================================
//
// Order.OpenDraft: a class-level [Remote, Execute] that fetches the customer's
// Draft order and creates one when there is none (VisionAlignment D11).
//
// Every test runs on the client container, so the call crosses the serializing
// stand-in.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

public class CreateOrFetchTests
{
    /// <summary>
    /// A customer with a Draft order gets that order, with its lines, in one call.
    /// </summary>
    [Fact]
    public async Task OpenDraft_FetchesTheCustomersDraft_WhenOneExists()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();

        var order = await factory.OpenDraft($"Customer_{InMemoryOrderDbContext.DraftOrderId}");

        Assert.Equal(1, client.ServiceProvider.GetRequiredService<ServerServiceProvider>().RemoteCallCount);
        Assert.Equal(InMemoryOrderDbContext.DraftOrderId, order.Id);
        Assert.False(order.IsNew);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(45.00m, order.Total.Amount);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// A customer with no Draft order gets a new, unsaved one in one call, and the
    /// store is untouched: OpenDraft never inserts. Customer_100 has an order, but it
    /// is Submitted, so it is not a draft.
    /// </summary>
    [Theory]
    [InlineData("Customer_100")]
    [InlineData("A New Customer")]
    public async Task OpenDraft_CreatesANewUnsavedOrder_WhenTheCustomerHasNoDraft(string customerName)
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IOrderFactory>();

        var order = await factory.OpenDraft(customerName);

        Assert.Equal(1, client.ServiceProvider.GetRequiredService<ServerServiceProvider>().RemoteCallCount);
        Assert.True(order.IsNew);
        Assert.Equal(0, order.Id);
        Assert.Equal(customerName, order.CustomerName);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Empty(order.Lines);

        var stored = await server.ServiceProvider.GetRequiredService<IOrderDbContext>().FindDraftAsync(customerName);
        Assert.Null(stored);

        server.Dispose();
        client.Dispose();
    }
}
