// =============================================================================
// DESIGN SOURCE OF TRUTH: [AspAuthorize] Over HTTP
// =============================================================================
//
// [AspAuthorize] evaluates ASP.NET Core policies against HttpContext.User, so
// it is tested only over HTTP: the in-process harness has no HttpContext.
// SecureOrder's policies are registered by Design.Server (region
// authorization-policy-config); DesignHttpHost's test scheme signs each request
// in as the user a test names.
//
// GENERATOR BEHAVIOR: a class factory's [AspAuthorize] denial is a result
//
// A class-factory method calls IAspAuthorize without forbid, so a denial
// comes back with the response: a Fetch returns null, and a Save throws
// NotAuthorizedException on the client. (An interface-factory method forbids
// instead; see authorization.md.)
//
// =============================================================================

using Design.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace Design.Tests.Http;

public class AspAuthorizeHttpTests(DesignHttpHost host)
{
    [Fact]
    public async Task Fetch_SignedInUser_GetsTheOrder()
    {
        using var client = host.CreateClientScope(new AsUser(OrderRoles.Support));
        var factory = client.ServiceProvider.GetRequiredService<ISecureOrderFactory>();

        var order = await factory.Fetch(5);

        Assert.NotNull(order);
        Assert.Equal(5, order.Id);
    }

    /// <summary>
    /// "RequireAuthenticated" refuses a caller nobody signed in.
    /// </summary>
    [Fact]
    public async Task Fetch_AnonymousCaller_IsDenied()
    {
        using var client = host.CreateClientScope(new AsUser(TestUserScheme.Anonymous));
        var factory = client.ServiceProvider.GetRequiredService<ISecureOrderFactory>();

        Assert.Null(await factory.Fetch(5));
    }

    [Fact]
    public async Task Insert_NeedsAnAdminOrManager()
    {
        using var manager = host.CreateClientScope(new AsUser(OrderRoles.Manager));
        using var sales = host.CreateClientScope(new AsUser(OrderRoles.Sales));
        var managerFactory = manager.ServiceProvider.GetRequiredService<ISecureOrderFactory>();
        var salesFactory = sales.ServiceProvider.GetRequiredService<ISecureOrderFactory>();

        var saved = await managerFactory.Save(managerFactory.Create("Secure_Customer"));
        Assert.NotNull(saved);
        Assert.NotEqual(0, saved.Id);

        await Assert.ThrowsAsync<NotAuthorizedException>(() => salesFactory.Save(salesFactory.Create("Secure_Customer")));
    }

    /// <summary>
    /// Update carries two [AspAuthorize] attributes; both must pass.
    /// </summary>
    [Fact]
    public async Task Update_NeedsBothPolicies()
    {
        using var manager = host.CreateClientScope(new AsUser(OrderRoles.Manager));
        using var admin = host.CreateClientScope(new AsUser("Admin"));
        var managerFactory = manager.ServiceProvider.GetRequiredService<ISecureOrderFactory>();
        var adminFactory = admin.ServiceProvider.GetRequiredService<ISecureOrderFactory>();

        var forManager = (await managerFactory.Fetch(7))!;
        forManager.Total = 120m;
        Assert.NotNull(await managerFactory.Save(forManager));

        var forAdmin = (await adminFactory.Fetch(7))!;
        forAdmin.Total = 130m;
        await Assert.ThrowsAsync<NotAuthorizedException>(() => adminFactory.Save(forAdmin));
    }
}
