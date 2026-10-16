// =============================================================================
// DESIGN SOURCE OF TRUTH: Logical Mode
// =============================================================================
//
// NeatooFactory.Logical runs every operation in one process: [Remote]
// operations included, nothing serialized, no HTTP. The services a server
// would supply to [Service] parameters are registered in the same container.
// For tests, console tools and single-tier apps. The factory modes page
// embeds the registration below (modes-logical-config).
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace Design.Tests.FactoryTests;

public class LogicalModeTests
{
    [Fact]
    public async Task Logical_RunsRemoteOperations_InTheSameContainer()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        #region modes-logical-config
        // One process, no HTTP: every operation runs here, [Remote] ones included
        services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(IOrder).Assembly);

        // So the services a server would supply to [Service] parameters are registered here too
        services.AddSingleton<IOrderDbContext, InMemoryOrderDbContext>();
        services.RegisterMatchingName(typeof(IOrder).Assembly);
        #endregion

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var orderFactory = scope.ServiceProvider.GetRequiredService<IOrderFactory>();
        var lineFactory = scope.ServiceProvider.GetRequiredService<IOrderLineFactory>();

        IOrder? order = orderFactory.Create("Logical_Customer");
        order.AddLine(lineFactory.Create("Widget L", 3.00m, 4));
        order = await orderFactory.Save(order);
        var fetched = await orderFactory.Fetch(order!.Id);

        Assert.NotNull(fetched);
        Assert.Equal("Logical_Customer", fetched.CustomerName);
        Assert.Equal(4, Assert.Single(fetched.Lines).Quantity);
        // The [Remote] Fetch and Save ran against this container's store
        Assert.NotNull(await provider.GetRequiredService<IOrderDbContext>().FindAsync(order.Id));
    }
}
