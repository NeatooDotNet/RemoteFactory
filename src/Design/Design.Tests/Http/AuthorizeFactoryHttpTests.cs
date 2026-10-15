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
}
