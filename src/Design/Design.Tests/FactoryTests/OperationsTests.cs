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
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ICustomerPreferencesFactory>();

        var preferences = factory.Create();

        Assert.Equal("Any", preferences.DeliveryWindow);
        Assert.True(preferences.IsNew);
        Assert.Equal(0, client.ServiceProvider.GetRequiredService<ServerServiceProvider>().RemoteCallCount);

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }

    /// <summary>
    /// One [Insert, Update] method, saved through the generated SaveUpsert. The first
    /// save inserts (IsNew), the second updates the same row; the start hook stamps
    /// LastSavedAt on the server before each write.
    /// </summary>
    [Fact]
    public async Task SaveUpsert_InsertsThenUpdatesTheOneRow()
    {
        var (server, client, local) = DesignClientServerContainers.Scopes();
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

        // Cleared on the client, so only the hook on the Update can set it again
        preferences.DeliveryWindow = "Evening";
        preferences.LastSavedAt = null;
        preferences = await factory.SaveUpsert(preferences);

        var row = await store.FindAsync("Prefs_Customer");
        Assert.NotNull(row);
        Assert.Equal("Evening", row.DeliveryWindow);
        Assert.True(row.NotifyBySms);
        Assert.NotNull(row.LastSavedAt);
        Assert.True(row.LastSavedAt >= firstStamp);
        Assert.Equal(1, store.Count);

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }

    [Fact]
    public async Task Fetch_UnknownCustomer_ReturnsNull()
    {
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ICustomerPreferencesFactory>();

        Assert.Null(await factory.Fetch("Nobody"));

        server.Dispose();
        client.Dispose();
        local.Dispose();
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
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var submit = client.GetRequiredService<OrderCommands.SubmitOrder>();
        var orderId = await SaveDraftWithALine(client, "Submit_Customer");

        Assert.Equal(SubmitOutcome.Submitted, await submit(orderId));
        Assert.Equal(OrderStatus.Submitted, (await server.GetRequiredService<IOrderDbContext>().FindAsync(orderId))!.Status);
        Assert.Equal(SubmitOutcome.NotSubmittable, await submit(orderId));
        Assert.Equal(SubmitOutcome.NotFound, await submit(999_999));

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }

    /// <summary>
    /// A Task-only command has no result to report a missing order in, so it throws.
    /// In these in-process containers the server's exception type reaches the test;
    /// over HTTP the caller gets HttpRequestException (a 500).
    /// An order already cancelled is left as it is, so a retried cancel succeeds.
    /// </summary>
    [Fact]
    public async Task CancelOrder_CancelsAStoredOrder_AndThrowsForAMissingOne()
    {
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var cancel = client.GetRequiredService<OrderCommands.CancelOrder>();
        var orderId = await SaveDraftWithALine(client, "Cancel_Customer");

        await cancel(orderId);

        Assert.Equal(OrderStatus.Cancelled, (await server.GetRequiredService<IOrderDbContext>().FindAsync(orderId))!.Status);
        await cancel(orderId);   // a retried cancel is not an error
        Assert.Equal(OrderStatus.Cancelled, (await server.GetRequiredService<IOrderDbContext>().FindAsync(orderId))!.Status);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cancel(999_999));

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }

    /// <summary>
    /// A stored row fetches back as an existing object with its saved values; the
    /// start hook runs only on Insert and Update, so the fetch leaves the stamp alone.
    /// </summary>
    [Fact]
    public async Task Fetch_StoredCustomer_ReturnsTheSavedValues()
    {
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<ICustomerPreferencesFactory>();
        var preferences = factory.Create();
        preferences.CustomerName = "Fetch_Customer";
        preferences.DeliveryWindow = "Morning";
        preferences = await factory.SaveUpsert(preferences);

        var fetched = await factory.Fetch("Fetch_Customer");

        Assert.NotNull(fetched);
        Assert.False(fetched.IsNew);
        Assert.Equal("Morning", fetched.DeliveryWindow);
        Assert.Equal(preferences.LastSavedAt, fetched.LastSavedAt);   // a Fetch does not stamp

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }

    /// <summary>
    /// A draft with no lines cannot be submitted: the command reports it from
    /// CanSubmit and leaves the order as it was.
    /// </summary>
    [Fact]
    public async Task SubmitOrder_EmptyDraft_IsNotSubmittable_AndStaysADraft()
    {
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var orderFactory = client.GetRequiredService<IOrderFactory>();
        var submit = client.GetRequiredService<OrderCommands.SubmitOrder>();
        var empty = (await orderFactory.Save(orderFactory.Create("Empty_Customer")))!;

        Assert.False(empty.CanSubmit);
        Assert.Equal(SubmitOutcome.NotSubmittable, await submit(empty.Id));
        Assert.Equal(OrderStatus.Draft, (await server.GetRequiredService<IOrderDbContext>().FindAsync(empty.Id))!.Status);

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }

    /// <summary>
    /// Submit throws only when CanSubmit is false: a caller that ignored the verdict.
    /// </summary>
    [Fact]
    public void Submit_WhenCanSubmitIsFalse_Throws()
    {
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var orderFactory = client.GetRequiredService<IOrderFactory>();
        var lineFactory = client.GetRequiredService<IOrderLineFactory>();

        var empty = orderFactory.Create("Throw_Customer");
        var emptyError = Assert.Throws<InvalidOperationException>(() => empty.Submit());

        var submitted = orderFactory.Create("Throw_Customer_2");
        submitted.AddLine(lineFactory.Create("Widget T", 1.00m, 1));
        submitted.Submit();
        var againError = Assert.Throws<InvalidOperationException>(() => submitted.Submit());

        Assert.Equal("Cannot submit an empty order", emptyError.Message);
        Assert.Equal("Order is not in draft status", againError.Message);

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }
}
