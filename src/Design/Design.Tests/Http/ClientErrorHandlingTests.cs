// =============================================================================
// DESIGN SOURCE OF TRUTH: Client Error Handling
// =============================================================================
//
// What a client screen sees when a call goes wrong, over real HTTP:
// - a read the server's rules refuse returns null, the same as a missing one;
// - a Save the rules refuse throws NotAuthorizedException;
// - any other exception on the server answers 500, and the client's call
//   throws HttpRequestException (AspNetCoreHttpTests pins the 500). The
//   server's exception type does not cross.
//
// The screen below is the client code the ASP.NET Core page embeds
// (aspnetcore-error-handling). AuthorizedOrder's rules are the real ones,
// over the request's signed-in user, behind switches these tests flip
// (DesignHttpHost).
//
// =============================================================================

using Design.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace Design.Tests.Http;

[Collection(AuthorizedOrderSwitches.Name)]
public class ClientErrorHandlingTests(DesignHttpHost host)
{
    [Fact]
    public async Task DeniedRead_ReturnsNull()
    {
        using var client = host.CreateClientScope();
        var screen = new OrderScreen(client.ServiceProvider.GetRequiredService<IAuthorizedOrderFactory>());

        host.Auth.AllowFetch = false;
        try
        {
            await screen.Load(7);
        }
        finally
        {
            host.Auth.AllowFetch = true;
        }

        Assert.Null(screen.Order);
        Assert.Equal("Order 7 was not found.", screen.Error);
    }

    [Fact]
    public async Task DeniedSave_ThrowsNotAuthorized_AndTheScreenKeepsItsOrder()
    {
        using var client = host.CreateClientScope();
        var screen = new OrderScreen(client.ServiceProvider.GetRequiredService<IAuthorizedOrderFactory>());
        await screen.Load(7);
        var loaded = screen.Order;
        Assert.NotNull(loaded);

        host.Auth.AllowAccess = false;
        try
        {
            await screen.Save();
        }
        finally
        {
            host.Auth.AllowAccess = true;
        }

        Assert.Same(loaded, screen.Order);
        Assert.Equal("You are not allowed to save this order.", screen.Error);
    }

    [Fact]
    public async Task AllowedSave_ReplacesTheScreensOrder()
    {
        using var client = host.CreateClientScope();
        var screen = new OrderScreen(client.ServiceProvider.GetRequiredService<IAuthorizedOrderFactory>());
        await screen.Load(7);
        var loaded = screen.Order;

        await screen.Save();

        Assert.Null(screen.Error);
        Assert.NotNull(screen.Order);
        Assert.NotSame(loaded, screen.Order);
    }

    /// <summary>
    /// A screen's load and save, as a Blazor component's code-behind would write them.
    /// </summary>
    private sealed class OrderScreen(IAuthorizedOrderFactory orderFactory)
    {
        public IAuthorizedOrder? Order { get; private set; }
        public string? Error { get; private set; }

        #region aspnetcore-error-handling
        public async Task Load(int id)
        {
            // Null when there is no such order, and when the server's rules refuse the read
            Order = await orderFactory.Fetch(id);
            Error = Order == null ? $"Order {id} was not found." : null;
        }

        public async Task Save()
        {
            try
            {
                Order = await orderFactory.Save(Order!);
                Error = null;
            }
            catch (NotAuthorizedException)
            {
                // The server's rules refused the write; the order on screen is unsaved
                Error = "You are not allowed to save this order.";
            }
            catch (HttpRequestException)
            {
                // The server threw (it answers 500), or it could not be reached
                Error = "The order could not be saved. Try again.";
            }
        }
        #endregion
    }
}
