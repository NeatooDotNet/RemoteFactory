// =============================================================================
// DESIGN SOURCE OF TRUTH: [AuthorizeFactory<T>] Custom Domain Authorization
// =============================================================================
//
// Tests demonstrating custom domain authorization with [AuthorizeFactory<T>].
// Covers Can* methods, auth failure behaviors (null returns, NotAuthorizedException),
// TrySave, and client-server round-trip with authorization.
//
// DESIGN DECISION: Test all authorization behaviors in isolation
//
// Each test explicitly sets the static auth flags it depends on via
// AuthorizedOrderAuth.ResetFlags() plus individual flag overrides.
// This avoids test pollution from flag state left by prior tests.
//
// DESIGN DECISION: Use local mode for most auth tests
//
// Authorization logic is the same in local and remote modes -- the generated
// code checks auth before executing the operation regardless of mode.
// Client-server tests (scenarios 11, 12) are separate to verify
// serialization round-trip preserves auth behavior.
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Tests.TestInfrastructure;
using Neatoo.RemoteFactory;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for [AuthorizeFactory] custom domain authorization on AuthorizedOrder.
/// </summary>
public class AuthorizationTests
{
    // -------------------------------------------------------------------------
    // Can* Method Tests
    //
    // GENERATOR BEHAVIOR: Can* methods check the auth interface
    //
    // Each Can* method resolves IAuthorizedOrderAuth from DI and calls
    // the auth methods whose operation scope matches. For example:
    // - CanCreate checks HasAccess() (Read|Write scope) and CanCreate() (Create scope)
    // - CanFetch checks HasAccess() (Read|Write scope) and CanFetch() (Fetch scope)
    // - CanSave aggregates auth from Insert, Update, Delete:
    //   distinct set = {HasAccess(), CanDelete()}
    // - CanDelete checks HasAccess() (Read|Write scope) and CanDelete() (Delete scope)
    //
    // DESIGN DECISION: Can* returns Authorized (synchronous, not Task<Authorized>)
    //
    // Even though the source methods are [Remote] internal, the generated Can*
    // methods return Authorized directly (not Task<Authorized>). The generator
    // makes Can* methods synchronous regardless of the source method's async status.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 1: CanCreate returns HasAccess=true when all auth checks pass.
    /// Rule 3: WHEN auth allows Create, THEN factory.CanCreate().HasAccess == true.
    /// </summary>
    [Fact]
    public void AuthorizedOrder_CanCreate_ReturnsTrue_WhenAllowed()
    {
        // Arrange
        AuthorizedOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IAuthorizedOrderFactory>();

        // Act
        var result = factory.CanCreate();

        // Assert
        Assert.True(result.HasAccess);

        local.Dispose();
    }

    /// <summary>
    /// Scenario 2: CanFetch returns HasAccess=false when auth denies Fetch.
    /// Rule 4: WHEN auth denies Fetch, THEN factory.CanFetch().HasAccess == false.
    /// </summary>
    [Fact]
    public void AuthorizedOrder_CanFetch_ReturnsFalse_WhenDenied()
    {
        // Arrange
        AuthorizedOrderAuth.ResetFlags();
        AuthorizedOrderAuth.AllowFetch = false;
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IAuthorizedOrderFactory>();

        // Act
        var result = factory.CanFetch();

        // Assert
        Assert.False(result.HasAccess);

        local.Dispose();
    }

    /// <summary>
    /// Scenario 3: CanSave returns HasAccess=false when any write check fails.
    /// Rule 5: WHEN CanDelete returns false, THEN factory.CanSave().HasAccess == false,
    /// because CanSave aggregates all write auth checks and the Delete check fails.
    /// </summary>
    [Fact]
    public void AuthorizedOrder_CanSave_ReturnsFalse_WhenDeleteDenied()
    {
        // Arrange
        AuthorizedOrderAuth.ResetFlags();
        AuthorizedOrderAuth.AllowDelete = false;
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IAuthorizedOrderFactory>();

        // Act
        var result = factory.CanSave();

        // Assert -- CanSave aggregates {HasAccess(), CanDelete()}; CanDelete fails
        Assert.False(result.HasAccess);

        local.Dispose();
    }

    /// <summary>
    /// Scenario 4: CanSave returns HasAccess=true when all write checks pass.
    /// Rule 6: WHEN all write auth checks pass, THEN factory.CanSave().HasAccess == true.
    /// </summary>
    [Fact]
    public void AuthorizedOrder_CanSave_ReturnsTrue_WhenAllWriteAllowed()
    {
        // Arrange
        AuthorizedOrderAuth.ResetFlags(); // All flags true by default
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IAuthorizedOrderFactory>();

        // Act
        var result = factory.CanSave();

        // Assert
        Assert.True(result.HasAccess);

        local.Dispose();
    }

    // -------------------------------------------------------------------------
    // Auth Failure Behavior -- Create and Fetch
    //
    // GENERATOR BEHAVIOR: Read operations return null on auth failure
    //
    // When authorization denies a Create or Fetch operation, the generated
    // factory method returns null instead of throwing an exception.
    // This allows callers to check CanCreate/CanFetch before calling,
    // or handle the null result gracefully.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 5: Create returns null when auth denies.
    /// Rule 7: WHEN authorization denies Create, THEN factory.Create() returns null.
    /// </summary>
    [Fact]
    public async Task AuthorizedOrder_Create_ReturnsNull_WhenDenied()
    {
        // Arrange
        AuthorizedOrderAuth.ResetFlags();
        AuthorizedOrderAuth.AllowCreate = false;
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IAuthorizedOrderFactory>();

        // Act
        var result = await factory.Create("Should Not Create");

        // Assert
        Assert.Null(result);

        local.Dispose();
    }

    /// <summary>
    /// Scenario 6: Fetch returns null when auth denies.
    /// Rule 8: WHEN authorization denies Fetch, THEN factory.Fetch() returns null.
    /// </summary>
    [Fact]
    public async Task AuthorizedOrder_Fetch_ReturnsNull_WhenDenied()
    {
        // Arrange
        AuthorizedOrderAuth.ResetFlags();
        AuthorizedOrderAuth.AllowFetch = false;
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IAuthorizedOrderFactory>();

        // Act
        var result = await factory.Fetch(123);

        // Assert
        Assert.Null(result);

        local.Dispose();
    }

    /// <summary>
    /// Scenario 7: Create returns non-null when auth allows.
    /// Rule 9: WHEN authorization allows Create, THEN factory.Create() returns non-null IAuthorizedOrder.
    /// </summary>
    [Fact]
    public async Task AuthorizedOrder_Create_ReturnsInstance_WhenAllowed()
    {
        // Arrange
        AuthorizedOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IAuthorizedOrderFactory>();

        // Act
        var result = await factory.Create("Test Customer");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Customer", result.CustomerName);
        Assert.True(result.IsNew);

        local.Dispose();
    }

    // -------------------------------------------------------------------------
    // Auth Failure Behavior -- Save
    //
    // GENERATOR BEHAVIOR: Write operations throw NotAuthorizedException
    //
    // When authorization denies a Save operation, the generated Save() method
    // throws NotAuthorizedException. TrySave() catches this exception and
    // returns Authorized<T> with HasAccess=false.
    //
    // This distinction (null for Read, exception for Write) makes sense because:
    // - Read failures are expected (user might not have access to some data)
    // - Write failures are exceptional (user already has the data, shouldn't lose it)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 8: Save throws NotAuthorizedException when delete is denied.
    /// Rule 10: WHEN authorization denies delete and target.IsDeleted=true,
    /// THEN factory.Save() throws NotAuthorizedException.
    /// </summary>
    [Fact]
    public async Task AuthorizedOrder_Save_ThrowsNotAuthorizedException_WhenDeleteDenied()
    {
        // Arrange
        AuthorizedOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IAuthorizedOrderFactory>();

        // Create an entity, then mark it for deletion
        var order = await factory.Create("Delete Test");
        Assert.NotNull(order);
        // Simulate fetched entity (not new) that is being deleted
        order.IsNew = false;
        order.IsDeleted = true;

        // Now deny delete authorization
        AuthorizedOrderAuth.AllowDelete = false;

        // Act & Assert
        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.Save(order));

        local.Dispose();
    }

    /// <summary>
    /// Scenario 9: TrySave returns HasAccess=false when delete is denied.
    /// Rule 11: WHEN authorization denies delete and target.IsDeleted=true,
    /// THEN factory.TrySave() returns Authorized with HasAccess=false and Result=null.
    /// </summary>
    [Fact]
    public async Task AuthorizedOrder_TrySave_ReturnsFalse_WhenDeleteDenied()
    {
        // Arrange
        AuthorizedOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IAuthorizedOrderFactory>();

        // Create an entity, then mark it for deletion
        var order = await factory.Create("TrySave Delete Test");
        Assert.NotNull(order);
        order.IsNew = false;
        order.IsDeleted = true;

        // Now deny delete authorization
        AuthorizedOrderAuth.AllowDelete = false;

        // Act
        var result = await factory.TrySave(order);

        // Assert
        Assert.False(result.HasAccess);
        Assert.Null(result.Result);

        local.Dispose();
    }

    /// <summary>
    /// Scenario 10: TrySave returns HasAccess=true when insert is allowed.
    /// Rule 12: WHEN all write checks pass and target.IsNew=true,
    /// THEN factory.TrySave() returns Authorized with HasAccess=true, Result != null,
    /// and Result.IsNew == false (reset by FactoryCompleteAsync after Insert).
    /// </summary>
    [Fact]
    public async Task AuthorizedOrder_TrySave_ReturnsTrue_WhenInsertAllowed()
    {
        // Arrange
        AuthorizedOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IAuthorizedOrderFactory>();

        var order = await factory.Create("TrySave Insert Test");
        Assert.NotNull(order);
        Assert.True(order.IsNew);

        // Act
        var result = await factory.TrySave(order);

        // Assert
        Assert.True(result.HasAccess);
        Assert.NotNull(result.Result);
        Assert.False(result.Result!.IsNew); // Reset by FactoryCompleteAsync after Insert

        local.Dispose();
    }

    // -------------------------------------------------------------------------
    // Client-Server Round-Trip Tests
    //
    // DESIGN DECISION: Verify auth works across serialization boundary
    //
    // These tests use the client container to verify that authorization
    // works correctly when requests cross the client-server boundary via
    // JSON serialization. The auth implementation must be registered in
    // both containers since the client checks Can* locally (even though
    // the LocalCan* has the IsServerRuntime guard, which passes because
    // IsServerRuntime defaults to true in test environments).
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 11: Auth-allowed Create works through client-server serialization.
    /// Rules 3, 9, 13: Non-null IAuthorizedOrder returned after serialization round-trip.
    /// </summary>
    [Fact]
    public async Task AuthorizedOrder_Create_WorksThroughClientServer()
    {
        // Arrange
        AuthorizedOrderAuth.ResetFlags();
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IAuthorizedOrderFactory>();

        // Act -- request crosses to server via serialization
        var result = await factory.Create("Client-Server Test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Client-Server Test", result.CustomerName);
        Assert.True(result.IsNew);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Scenario 12: Auth-denied Fetch returns null through client-server.
    /// Rules 4, 8, 13: null returned after serialization round-trip.
    /// </summary>
    [Fact]
    public async Task AuthorizedOrder_Fetch_ReturnsNull_ThroughClientServer()
    {
        // Arrange
        AuthorizedOrderAuth.ResetFlags();
        AuthorizedOrderAuth.AllowFetch = false;
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IAuthorizedOrderFactory>();

        // Act -- request crosses to server via serialization
        var result = await factory.Fetch(456);

        // Assert
        Assert.Null(result);

        server.Dispose();
        client.Dispose();
    }
}
