// =============================================================================
// DESIGN SOURCE OF TRUTH: [AuthorizeFactory<T>] Over HTTP, From the Request's User
// =============================================================================
//
// On the server, AuthorizedOrderAuth reads the ICurrentUser that Design.Server
// builds from HttpContext.User. These tests sign each request in as a named
// user (DesignHttpHost's test scheme) and leave the host's switches on, so a
// refusal can only come from the user's roles.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace Design.Tests.Http;

[Collection(AuthorizedOrderSwitches.Name)]
public class AuthorizeFactoryHttpTests(DesignHttpHost host)
{
    /// <summary>
    /// CanFetch needs Sales or Support; a Manager alone is refused.
    /// </summary>
    [Fact]
    public async Task Fetch_UserWithoutAFetchRole_GetsNull()
    {
        using var manager = host.CreateClientScope(new AsUser(OrderRoles.Manager));
        using var support = host.CreateClientScope(new AsUser(OrderRoles.Support));

        Assert.Null(await manager.ServiceProvider.GetRequiredService<IAuthorizedOrderFactory>().Fetch(7));
        Assert.NotNull(await support.ServiceProvider.GetRequiredService<IAuthorizedOrderFactory>().Fetch(7));
    }

    /// <summary>
    /// HasAccess needs a signed-in user, so an anonymous Save is refused.
    /// </summary>
    [Fact]
    public async Task Save_AnonymousCaller_ThrowsNotAuthorized()
    {
        using var support = host.CreateClientScope(new AsUser(OrderRoles.Support));
        using var anonymous = host.CreateClientScope(new AsUser(TestUserScheme.Anonymous));
        var order = (await support.ServiceProvider.GetRequiredService<IAuthorizedOrderFactory>().Fetch(7))!;

        await Assert.ThrowsAsync<NotAuthorizedException>(
            () => anonymous.ServiceProvider.GetRequiredService<IAuthorizedOrderFactory>().Save(order));
    }

    /// <summary>
    /// An interface factory's method has no Authorized&lt;T&gt; to carry a refusal, so its
    /// check throws on the server. UseNeatoo() answers 403 with the exception, and the
    /// client throws it again: the same message and Context as on the server (#103).
    /// </summary>
    /// <remarks>
    /// CustomerDirectoryAuth's switches are static, and InterfaceFactoryAuthorizationTests
    /// flips HasAccess's while this runs, so either bool check may be the one that refuses.
    /// </remarks>
    [Fact]
    public async Task InterfaceFactory_RefusedCall_ThrowsNotAuthorized_WithTheServersContext()
    {
        using var client = host.CreateClientScope();
        var directory = client.ServiceProvider.GetRequiredService<ICustomerDirectoryFactory>();

        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(
            () => directory.GetCustomer(CustomerDirectoryAuth.DeniedCustomerId));

        Assert.StartsWith("operation GetCustomer on ICustomerDirectory (ICustomerDirectoryAuth.", ex.Context, StringComparison.Ordinal);
        Assert.Equal($"Authorization denied for {ex.Context}; no reason was supplied.", ex.Message);
    }
}
