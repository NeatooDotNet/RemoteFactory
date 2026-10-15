// =============================================================================
// DESIGN SOURCE OF TRUTH: Client Usage
// =============================================================================
//
// The code a client screen writes against the Order aggregate. README and
// getting started embed the region below (readme-client-usage), so it uses only
// the public factory interfaces a client assembly can call.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.Entities;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

public class OrderUsageTests
{
    /// <summary>
    /// Open a draft, add a line built on the client, save and keep the result.
    /// Two calls cross: OpenDraft and Save. Creating and adding the line do not.
    /// </summary>
    [Fact]
    public async Task Client_OpensADraft_AddsALine_AndSaves()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var orderFactory = client.GetRequiredService<IOrderFactory>();
        var lineFactory = client.GetRequiredService<IOrderLineFactory>();

        #region readme-client-usage
        // The customer's draft: the stored one, or a new one. One server call.
        IOrder? order = await orderFactory.OpenDraft("Customer_42");

        // Build a line on the client and add it. No server call.
        order.AddLine(lineFactory.Create("Widget C", 12.50m, 2));

        // Save sends the whole order; keep what it returns.
        order = await orderFactory.Save(order);
        #endregion

        Assert.Equal(2, client.ServiceProvider.GetRequiredService<ServerServiceProvider>().RemoteCallCount);
        Assert.NotNull(order);
        Assert.False(order.IsNew);
        Assert.NotEqual(0, order.Id);
        var row = await server.ServiceProvider.GetRequiredService<IOrderDbContext>().FindAsync(order.Id);
        Assert.NotNull(row);
        Assert.Equal("Customer_42", row.CustomerName);
        Assert.Equal(2, Assert.Single(row.Lines).Quantity);

        server.Dispose();
        client.Dispose();
    }
}
