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
// DESIGN DECISION: The impl is registered by naming convention
//
// CustomerDirectory is named for ICustomerDirectory, so RegisterMatchingName
// registers it; these tests add no registration of their own. On the client
// the generated proxy is registered first and wins, so every client call
// crosses to the server, where the auth checks and the impl run.
//
// =============================================================================

using Design.Domain.Services;
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
        CustomerDirectoryAuth.ResetFlags();
        return DesignClientServerContainers.Scopes();
    }

    // -------------------------------------------------------------------------
    // Auth-Allowed Scenarios
    //
    // GENERATOR BEHAVIOR: All three auth methods pass → impl is invoked
    //
    // LocalGetCustomer calls HasAccess() → CanAccessCustomer(id) → CheckReadAccess()
    // in order. All must pass before the impl executes.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 1: GetCustomer succeeds when all auth checks pass.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_GetCustomer_Succeeds_WhenAllAuthAllowed()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<ICustomerDirectoryFactory>();

        var customer = await factory.GetCustomer(AllowedId);

        Assert.NotNull(customer);
        Assert.Equal(AllowedId, customer.Id);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Scenario 2: RenameCustomer succeeds when all auth checks pass.
    /// Demonstrates that extra non-auth parameters (string name) don't affect auth routing.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_RenameCustomer_Succeeds_WhenAllAuthAllowed()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<ICustomerDirectoryFactory>();

        var result = await factory.RenameCustomer(AllowedId, "Renamed");

        Assert.Equal("Renamed", result.Name);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// ListContacts returns a list of records through the proxy, and the same
    /// parameter-matched check guards it: the denied customer's contacts are refused.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_ListContacts_ReturnsContacts_AndDeniesTheDeniedCustomer()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<ICustomerDirectoryFactory>();

        var contacts = await factory.ListContacts(AllowedId);

        Assert.Equal(2, contacts.Count);
        Assert.All(contacts, c => Assert.Equal(AllowedId, c.CustomerId));
        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.ListContacts(DeniedId));

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
    /// Scenario 3: HasAccess denied → GetCustomer throws NotAuthorizedException.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_GetCustomer_Throws_WhenHasAccessDenied()
    {
        var (server, client, _) = SetupScopes();
        CustomerDirectoryAuth.AllowAccess = false;
        var factory = client.ServiceProvider.GetRequiredService<ICustomerDirectoryFactory>();

        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.GetCustomer(AllowedId));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// A Task-returning interface method runs on the server when allowed (#122).
    /// </summary>
    /// <remarks>
    /// Cy (…03) exists only for this test: the store is static, so closing that
    /// account leaves the customers the other tests read untouched.
    /// </remarks>
    [Fact]
    public async Task InterfaceFactory_CloseAccount_RemovesCustomer_WhenAllowed()
    {
        var cyId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var (_, client, _) = SetupScopes();
        var factory = client.GetRequiredService<ICustomerDirectoryFactory>();

        await factory.CloseAccount(cyId);

        Assert.Null(await factory.GetCustomer(cyId));
    }

    /// <summary>
    /// Scenario 4: HasAccess denied → CloseAccount throws NotAuthorizedException.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_CloseAccount_Throws_WhenHasAccessDenied()
    {
        var (server, client, _) = SetupScopes();
        CustomerDirectoryAuth.AllowAccess = false;
        var factory = client.ServiceProvider.GetRequiredService<ICustomerDirectoryFactory>();

        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.CloseAccount(AllowedId));

        server.Dispose();
        client.Dispose();
    }

    // -------------------------------------------------------------------------
    // Auth Denial Scenarios — Parameterized Auth
    //
    // GENERATOR BEHAVIOR: Parameter matching by type
    //
    // CanAccessCustomer(Guid id) fires on every interface method (each takes a
    // Guid id). The generator forwards the Guid from the interface call into
    // the auth method. This enables per-entity authorization — deny the
    // specific id without blocking other ids.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 5: CanAccessCustomer denies specific id → that id throws.
    /// Other ids still succeed.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_GetCustomer_Throws_WhenCustomerIdDenied_OthersSucceed()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<ICustomerDirectoryFactory>();

        // Denied id throws
        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.GetCustomer(DeniedId));

        // Allowed id still succeeds
        var customer = await factory.GetCustomer(AllowedId);
        Assert.NotNull(customer);

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
    public async Task InterfaceFactory_GetCustomer_ThrowsWithMessage_WhenReadAccessDenied()
    {
        var (server, client, _) = SetupScopes();
        CustomerDirectoryAuth.ReadDenialMessage = "read-denied-explanation";
        var factory = client.ServiceProvider.GetRequiredService<ICustomerDirectoryFactory>();

        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.GetCustomer(AllowedId));
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
    // on ICustomerDirectoryFactory that returns Authorized non-throwingly.
    // Callers can check auth before attempting the operation, enabling
    // UI disable-states and conditional rendering.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 7: CanGetCustomer returns HasAccess=true when auth allows.
    /// </summary>
    [Fact]
    public void InterfaceFactory_CanGetCustomer_ReturnsTrue_WhenAllowed()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<ICustomerDirectoryFactory>();

        var result = factory.CanGetCustomer(AllowedId);

        Assert.True(result.HasAccess);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Scenario 8: CanGetCustomer returns HasAccess=false for a denied id.
    /// Reflects the same per-entity auth that the method itself enforces.
    /// </summary>
    [Fact]
    public void InterfaceFactory_CanGetCustomer_ReturnsFalse_ForDeniedId()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<ICustomerDirectoryFactory>();

        var result = factory.CanGetCustomer(DeniedId);

        Assert.False(result.HasAccess);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Scenario 9: CanRenameCustomer parameters mirror only the auth-matching params.
    /// RenameCustomer(Guid id, string name) has a string param, but CanRenameCustomer
    /// takes only (Guid id) because the string isn't used by any auth method.
    /// </summary>
    [Fact]
    public void InterfaceFactory_CanRenameCustomer_OnlyTakesAuthMatchingParams()
    {
        var (server, client, _) = SetupScopes();
        var factory = client.ServiceProvider.GetRequiredService<ICustomerDirectoryFactory>();

        // CanRenameCustomer signature is (Guid id, CancellationToken) — no string.
        var result = factory.CanRenameCustomer(AllowedId);

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
        CustomerDirectoryAuth.AllowAccess = false;
        var factory = client.ServiceProvider.GetRequiredService<ICustomerDirectoryFactory>();

        // The call serializes to the server, auth denies on the server, exception
        // propagates back through the serialization boundary.
        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.GetCustomer(AllowedId));

        server.Dispose();
        client.Dispose();
    }
}
