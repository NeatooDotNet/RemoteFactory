// =============================================================================
// DESIGN SOURCE OF TRUTH: Operation Forms
// =============================================================================
//
// The operation forms the factory operations page shows beyond the Order
// aggregate's own: a [Create] constructor, an [Insert, Update] upsert with a
// start hook, and commands that change an order through its root.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.Entities;
using Design.Domain.Services;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

public class OperationsTests
{
    [Fact]
    public void CreateConstructor_RunsOnTheClient_WithItsDefaults()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ICustomerPreferencesFactory>();

        var preferences = factory.Create();

        Assert.Equal("Any", preferences.DeliveryWindow);
        Assert.True(preferences.IsNew);
        Assert.Equal(0, client.ServiceProvider.GetRequiredService<ServerServiceProvider>().RemoteCallCount);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// One [Insert, Update] method, saved through the generated SaveUpsert. The first
    /// save inserts (IsNew), the second updates the same row; the start hook stamps
    /// LastSavedAt on the server before each write.
    /// </summary>
    [Fact]
    public async Task SaveUpsert_InsertsThenUpdatesTheOneRow()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ICustomerPreferencesFactory>();
        var store = (InMemoryCustomerPreferencesStore)server.GetRequiredService<ICustomerPreferencesStore>();
        var before = DateTimeOffset.UtcNow;

        var preferences = factory.Create();
        preferences.CustomerName = "Prefs_Customer";
        preferences.NotifyBySms = true;
        preferences = await factory.SaveUpsert(preferences);

        Assert.False(preferences.IsNew);
        Assert.NotNull(preferences.LastSavedAt);
        Assert.True(preferences.LastSavedAt >= before);
        var firstStamp = preferences.LastSavedAt;

        preferences.DeliveryWindow = "Evening";
        preferences = await factory.SaveUpsert(preferences);

        var row = await store.FindAsync("Prefs_Customer");
        Assert.NotNull(row);
        Assert.Equal("Evening", row.DeliveryWindow);
        Assert.True(row.NotifyBySms);
        Assert.True(row.LastSavedAt >= firstStamp);
        Assert.Equal(1, store.Count);

        server.Dispose();
        client.Dispose();
    }

    [Fact]
    public async Task Fetch_UnknownCustomer_ReturnsNull()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ICustomerPreferencesFactory>();

        Assert.Null(await factory.Fetch("Nobody"));

        server.Dispose();
        client.Dispose();
    }

    private static async Task<int> SaveDraftWithALine(IServiceScope client, string customer)
    {
        var orderFactory = client.GetRequiredService<IOrderFactory>();
        var order = orderFactory.Create(customer);
        order.AddLine(client.GetRequiredService<IOrderLineFactory>().Create("Widget S", 5.00m, 1));
        return (await orderFactory.Save(order))!.Id;
    }

    /// <summary>
    /// The command fetches the order through its factory, reads CanSubmit, calls
    /// Submit and saves: one client call. A missing order and an order that is no
    /// longer a draft are results, not exceptions.
    /// </summary>
    [Fact]
    public async Task SubmitOrder_ChangesTheOrderThroughItsRoot_AndReportsWhatItDid()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var submit = client.GetRequiredService<OrderCommands.SubmitOrder>();
        var orderId = await SaveDraftWithALine(client, "Submit_Customer");

        Assert.Equal(SubmitOutcome.Submitted, await submit(orderId));
        Assert.Equal(OrderStatus.Submitted, (await server.GetRequiredService<IOrderDbContext>().FindAsync(orderId))!.Status);
        Assert.Equal(SubmitOutcome.NotSubmittable, await submit(orderId));
        Assert.Equal(SubmitOutcome.NotFound, await submit(999_999));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// A Task-only command has no result to report a missing order in, so it throws.
    /// In these in-process containers the server's exception type reaches the test;
    /// over HTTP the caller gets HttpRequestException (a 500).
    /// </summary>
    [Fact]
    public async Task CancelOrder_CancelsAStoredOrder_AndThrowsForAMissingOne()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var cancel = client.GetRequiredService<OrderCommands.CancelOrder>();
        var orderId = await SaveDraftWithALine(client, "Cancel_Customer");

        await cancel(orderId);

        Assert.Equal(OrderStatus.Cancelled, (await server.GetRequiredService<IOrderDbContext>().FindAsync(orderId))!.Status);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cancel(999_999));

        server.Dispose();
        client.Dispose();
    }
}
