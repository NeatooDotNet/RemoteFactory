// =============================================================================
// DESIGN SOURCE OF TRUTH: Interface Factory Authorization Tests
// =============================================================================
//
// Tests demonstrating [AuthorizeFactory<T>] on an INTERFACE FACTORY.
// Covers the Execute/Read scope model, parameter matching, Can{Method}
// generation, and NotAuthorizedException behavior.
//
// Contrast with AuthorizationTests.cs which covers class-factory CRUD auth.
//
// DESIGN DECISION: Execute/Read scopes only (no Create/Fetch/Delete)
//
// Interface factories have no CRUD operation mapping, so class-factory scopes
// (Create, Fetch, Insert, Update, Delete) silently never fire. The only scopes
// that apply to interface factories are Execute and Read — both behave
// uniformly across all interface methods.
//
// DESIGN DECISION: All failures throw NotAuthorizedException (no null-return)
//
// Class factories return null on Create/Fetch auth denial. Interface factories
// always throw — they have no equivalent "read failure is ok" semantic because
// every method is arbitrary.
//
// DESIGN DECISION: Server-side impl registration is explicit
//
// The test configures IAuthorizedRepository → AuthorizedRepository on the
// server container. The client uses the generated proxy (IAuthorizedRepositoryFactory);
// the impl is server-only. Contrast with class factories where the generator
// auto-registers everything.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for [AuthorizeFactory] custom domain authorization on an interface factory.
/// </summary>
public class InterfaceFactoryAuthorizationTests
{
    private static readonly Guid AllowedId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DeniedId = Guid.Parse("00000000-0000-0000-0000-000000000042");

    private static (IServiceScope server, IServiceScope client, IServiceScope local) SetupScopes()
    {
        RepositoryAuth.ResetFlags();
        return DesignClientServerContainers.Scopes(
            configureServer: services =>
            {
                services.AddScoped<IAuthorizedRepository, AuthorizedRepository>();
            });
    }

    // -------------------------------------------------------------------------
    // Auth-Allowed Scenarios
    //
    // GENERATOR BEHAVIOR: All three auth methods pass → impl is invoked
    //
    // LocalGetItem calls HasAccess() → CanAccessItem(id) → CheckReadAccess()
    // in order. All must pass before the impl executes.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 1: GetItem succeeds when all auth checks pass.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_GetItem_Succeeds_WhenAllAuthAllowed()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<IAuthorizedRepositoryFactory>();

        var item = await factory.GetItem(AllowedId);

        Assert.NotNull(item);
        Assert.Equal(AllowedId, item.Id);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Scenario 2: UpdateItem succeeds when all auth checks pass.
    /// Demonstrates that extra non-auth parameters (string name) don't affect auth routing.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_UpdateItem_Succeeds_WhenAllAuthAllowed()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<IAuthorizedRepositoryFactory>();

        var result = await factory.UpdateItem(AllowedId, "Renamed");

        Assert.Equal("Renamed", result.Name);

        server.Dispose();
        client.Dispose();
    }

    // -------------------------------------------------------------------------
    // Auth Denial Scenarios — Parameterless Auth
    //
    // GENERATOR BEHAVIOR: Parameterless auth fires on every method
    //
    // HasAccess() has no parameters, so it runs on every interface method
    // call. When it denies, every method throws NotAuthorizedException
    // regardless of arguments.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 3: HasAccess denied → GetItem throws NotAuthorizedException.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_GetItem_Throws_WhenHasAccessDenied()
    {
        var (server, client, _) = SetupScopes();
        RepositoryAuth.AllowAccess = false;
        var factory = client.ServiceProvider.GetRequiredService<IAuthorizedRepositoryFactory>();

        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.GetItem(AllowedId));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Scenario 4: HasAccess denied → DeleteItem throws NotAuthorizedException.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_DeleteItem_Throws_WhenHasAccessDenied()
    {
        var (server, client, _) = SetupScopes();
        RepositoryAuth.AllowAccess = false;
        var factory = client.ServiceProvider.GetRequiredService<IAuthorizedRepositoryFactory>();

        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.DeleteItem(AllowedId));

        server.Dispose();
        client.Dispose();
    }

    // -------------------------------------------------------------------------
    // Auth Denial Scenarios — Parameterized Auth
    //
    // GENERATOR BEHAVIOR: Parameter matching by type
    //
    // CanAccessItem(Guid id) fires on every interface method (each takes a
    // Guid id). The generator forwards the Guid from the interface call into
    // the auth method. This enables per-entity authorization — deny the
    // specific id without blocking other ids.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 5: CanAccessItem denies specific id → that id throws.
    /// Other ids still succeed.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_GetItem_Throws_WhenItemIdDenied_OthersSucceed()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<IAuthorizedRepositoryFactory>();

        // Denied id throws
        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.GetItem(DeniedId));

        // Allowed id still succeeds
        var item = await factory.GetItem(AllowedId);
        Assert.NotNull(item);

        server.Dispose();
        client.Dispose();
    }

    // -------------------------------------------------------------------------
    // Auth Denial Scenarios — String-Returning Auth
    //
    // GENERATOR BEHAVIOR: Denial message surfaces in exception
    //
    // CheckReadAccess returns null/empty = authorized, non-empty string =
    // denied with that message. NotAuthorizedException.Message contains the
    // denial string, giving callers domain-specific "why."
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 6: String-returning auth denial surfaces the denial message.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_GetItem_ThrowsWithMessage_WhenReadAccessDenied()
    {
        var (server, client, _) = SetupScopes();
        RepositoryAuth.ReadDenialMessage = "read-denied-explanation";
        var factory = client.ServiceProvider.GetRequiredService<IAuthorizedRepositoryFactory>();

        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.GetItem(AllowedId));
        Assert.Contains("read-denied-explanation", ex.Message, StringComparison.Ordinal);

        server.Dispose();
        client.Dispose();
    }

    // -------------------------------------------------------------------------
    // Can{Method} Non-Throwing Helpers
    //
    // GENERATOR BEHAVIOR: Can{Method} on the factory interface
    //
    // For each interface method, the generator emits Can{Method}(matching-params)
    // on IAuthorizedRepositoryFactory that returns Authorized non-throwingly.
    // Callers can check auth before attempting the operation, enabling
    // UI disable-states and conditional rendering.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 7: CanGetItem returns HasAccess=true when auth allows.
    /// </summary>
    [Fact]
    public void InterfaceFactory_CanGetItem_ReturnsTrue_WhenAllowed()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<IAuthorizedRepositoryFactory>();

        var result = factory.CanGetItem(AllowedId);

        Assert.True(result.HasAccess);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Scenario 8: CanGetItem returns HasAccess=false for a denied id.
    /// Reflects the same per-entity auth that the method itself enforces.
    /// </summary>
    [Fact]
    public void InterfaceFactory_CanGetItem_ReturnsFalse_ForDeniedId()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<IAuthorizedRepositoryFactory>();

        var result = factory.CanGetItem(DeniedId);

        Assert.False(result.HasAccess);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Scenario 9: CanUpdateItem parameters mirror only the auth-matching params.
    /// UpdateItem(Guid id, string name) has a string param, but CanUpdateItem
    /// takes only (Guid id) because the string isn't used by any auth method.
    /// </summary>
    [Fact]
    public void InterfaceFactory_CanUpdateItem_OnlyTakesAuthMatchingParams()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<IAuthorizedRepositoryFactory>();

        // CanUpdateItem signature is (Guid id, CancellationToken) — no string.
        var result = factory.CanUpdateItem(AllowedId);

        Assert.True(result.HasAccess);

        server.Dispose();
        client.Dispose();
    }

    // -------------------------------------------------------------------------
    // Client-Server Round-Trip
    //
    // DESIGN DECISION: Verify auth works across serialization
    //
    // The factory resolved from the client container uses a serialized proxy.
    // Auth enforcement happens on the server AFTER deserialization, before
    // the impl is invoked. These tests confirm the full round-trip.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 10: Auth-denied call through client-server proxy throws NotAuthorizedException.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_ServerSideAuthDenial_PropagatesToClient()
    {
        var (server, client, _) = SetupScopes();
        RepositoryAuth.AllowAccess = false;
        var factory = client.ServiceProvider.GetRequiredService<IAuthorizedRepositoryFactory>();

        // The call serializes to the server, auth denies on the server, exception
        // propagates back through the serialization boundary.
        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.GetItem(AllowedId));

        server.Dispose();
        client.Dispose();
    }
}
