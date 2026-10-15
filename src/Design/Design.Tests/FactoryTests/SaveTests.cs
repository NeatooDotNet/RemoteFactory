// =============================================================================
// DESIGN SOURCE OF TRUTH: Save Routing
// =============================================================================
//
// The outcomes the save page states, through the client containers: Save
// routes on IsNew and IsDeleted, and the caller reassigns what it returns.
//
// GENERATOR BEHAVIOR: the generated Save checks IsDeleted first. A new object
// that is also deleted has nothing to write, so Save returns null. A branch
// whose method is missing throws NotImplementedException.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.Entities;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace Design.Tests.FactoryTests;

public class SaveTests
{
    [Fact]
    public async Task Save_RoutesOnIsNewAndIsDeleted()
    {
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var orderFactory = client.GetRequiredService<IOrderFactory>();
        var lineFactory = client.GetRequiredService<IOrderLineFactory>();

        #region save-usage
        var order = orderFactory.Create("Save_Customer");
        order.AddLine(lineFactory.Create("Widget", 5.00m, 2));
        order = (await orderFactory.Save(order))!;      // IsNew: Insert

        order.CustomerName = "Save_Customer_Renamed";
        order = (await orderFactory.Save(order))!;      // stored: Update

        order.MarkDeleted();
        await orderFactory.Save(order);                 // IsDeleted: Delete
        #endregion

        Assert.Null(await orderFactory.Fetch(order.Id));

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }

    /// <summary>
    /// The Insert and the Update each reach the stored row.
    /// </summary>
    #region save-testing
    [Fact]
    public async Task Save_InsertThenUpdate_WritesTheOneRow()
    {
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var orderFactory = client.GetRequiredService<IOrderFactory>();
        var db = server.GetRequiredService<IOrderDbContext>();

        var order = orderFactory.Create("Routing_Customer");
        order = (await orderFactory.Save(order))!;
        Assert.False(order.IsNew);
        Assert.Equal("Routing_Customer", (await db.FindAsync(order.Id))!.CustomerName);

        order.CustomerName = "Routing_Customer_Renamed";
        order = (await orderFactory.Save(order))!;
        Assert.Equal("Routing_Customer_Renamed", (await db.FindAsync(order.Id))!.CustomerName);

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }
    #endregion

    /// <summary>
    /// Created and deleted before it was ever saved: nothing to write, so Save
    /// returns null and no row is made.
    /// </summary>
    [Fact]
    public async Task Save_NewAndDeleted_ReturnsNull()
    {
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var orderFactory = client.GetRequiredService<IOrderFactory>();
        var db = server.GetRequiredService<IOrderDbContext>();
        var order = orderFactory.Create("Never_Saved");
        order.MarkDeleted();

        Assert.Null(await orderFactory.Save(order));
        Assert.Null(await db.FindDraftAsync("Never_Saved"));

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }

    /// <summary>
    /// With authorization, a new-and-deleted Save should match the unauthorized
    /// factory and return null. It throws NotAuthorizedException, although no
    /// authorization check ran.
    /// </summary>
    [Fact(Skip = "Known bug #104: an authorized Save of a new-and-deleted object throws NotAuthorizedException instead of returning null")]
    public async Task AuthorizedSave_NewAndDeleted_ReturnsNull()
    {
        AuthorizedOrderAuth.ResetFlags();
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IAuthorizedOrderFactory>();

        var order = await factory.Create("Never_Saved_Authorized");
        Assert.NotNull(order);
        order.IsDeleted = true;

        Assert.Null(await factory.Save(order));

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }
}
