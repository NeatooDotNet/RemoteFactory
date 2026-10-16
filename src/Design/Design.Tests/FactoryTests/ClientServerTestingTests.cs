// =============================================================================
// DESIGN SOURCE OF TRUTH: Testing With the Client/Server Containers
// =============================================================================
//
// DesignClientServerContainers.Scopes() builds three containers: a server, a
// client whose [Remote] calls are serialized and handed to that server, and a
// local one (Logical mode) with nothing serialized. A test resolves factories
// from the CLIENT scope to exercise what a real client sends and receives; the
// local scope runs the same code without the round trip.
//
// The ASP.NET Core page embeds the region below (aspnetcore-testing). HTTP
// itself (status codes, headers, cancellation) is in Design.Tests/Http.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

public class ClientServerTestingTests
{
    [Fact]
    public async Task Order_SavesAndFetches_FromTheClientScope()
    {
        #region aspnetcore-testing
        var (server, client, local) = DesignClientServerContainers.Scopes();
        var orderFactory = client.GetRequiredService<IOrderFactory>();

        IOrder? order = orderFactory.Create("Test_Customer");   // local: no server call
        order = await orderFactory.Save(order);                 // serialized to the server and back
        var fetched = await orderFactory.Fetch(order!.Id);      // serialized again
        #endregion

        Assert.Equal(2, client.ServiceProvider.GetRequiredService<ServerServiceProvider>().RemoteCallCount);
        Assert.NotNull(fetched);
        Assert.Equal("Test_Customer", fetched.CustomerName);
        Assert.NotNull(await server.ServiceProvider.GetRequiredService<IOrderDbContext>().FindAsync(order.Id));

        server.Dispose();
        client.Dispose();
        local.Dispose();
    }
}
