// =============================================================================
// DESIGN SOURCE OF TRUTH: Parameterized [AuthorizeFactory<T>] Authorization
// =============================================================================
//
// Tests demonstrating parameterized authorization with [AuthorizeFactory<T>].
// Covers type-matched parameters, target entity parameters, CanXxx suppression,
// and auth failure behaviors.
//
// DESIGN DECISION: Two feature groups
//
// Group 1: Type-matched parameters (CanFetch with Guid)
//   - CanFetch(allowedGuid) passes, CanFetch(deniedGuid) fails
//   - Fetch returns null when auth denies a specific ID
//
// Group 2: Target entity parameter (CanWrite with entity state)
//   - Save succeeds when entity Status is "Active"
//   - Save throws NotAuthorizedException when Status is "Locked"
//   - TrySave catches exception and returns HasAccess=false
//
// =============================================================================

using Design.Domain.Aggregates;
using Design.Tests.TestInfrastructure;
using Neatoo.RemoteFactory;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for parameterized [AuthorizeFactory] authorization on ParamAuthOrder.
/// </summary>
public class ParamAuthorizationTests
{
    // -------------------------------------------------------------------------
    // Type-Matched Parameter Tests (CanFetch with Guid)
    //
    // GENERATOR BEHAVIOR: Generates CanFetch(Guid orderId) on the factory
    //
    // The auth method CanFetchOrder(Guid orderId) has Fetch scope and a Guid
    // parameter. The generator produces CanFetch(Guid) on the factory interface,
    // passing the Guid through to the auth method by type matching.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 1: CanFetch returns HasAccess=true for an allowed Guid.
    /// The auth method CanFetchOrder(Guid) returns true for non-denied IDs.
    /// </summary>
    [Fact]
    public void ParamAuth_CanFetch_ReturnsTrue_WhenGuidAllowed()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        // Act
        var result = factory.CanFetch(Guid.NewGuid());

        // Assert
        Assert.True(result.HasAccess);

        local.Dispose();
    }

    /// <summary>
    /// Scenario 2: CanFetch returns HasAccess=false for the denied Guid.
    /// The auth method CanFetchOrder(Guid) returns false for DenyFetchGuid.
    /// </summary>
    [Fact]
    public void ParamAuth_CanFetch_ReturnsFalse_WhenGuidDenied()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        // Act
        var result = factory.CanFetch(ParamAuthOrderAuth.DenyFetchGuid);

        // Assert
        Assert.False(result.HasAccess);

        local.Dispose();
    }

    /// <summary>
    /// Scenario 3: Fetch returns null when auth denies the specific Guid.
    /// Same null-on-denial behavior as parameterless auth.
    /// </summary>
    [Fact]
    public async Task ParamAuth_Fetch_ReturnsNull_WhenGuidDenied()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        // Act
        var result = await factory.Fetch(ParamAuthOrderAuth.DenyFetchGuid);

        // Assert
        Assert.Null(result);

        local.Dispose();
    }

    /// <summary>
    /// Scenario 4: Fetch returns entity when auth allows the Guid.
    /// </summary>
    [Fact]
    public async Task ParamAuth_Fetch_ReturnsEntity_WhenGuidAllowed()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();
        var orderId = Guid.NewGuid();

        // Act
        var result = await factory.Fetch(orderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(orderId, result.Id);
        Assert.False(result.IsNew);

        local.Dispose();
    }

    // -------------------------------------------------------------------------
    // CanCreate (parameterless Read auth)
    //
    // Create uses the parameterless CanRead() from Read scope.
    // CanFetchOrder(Guid) doesn't apply to Create because Create has no Guid param.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 5: CanCreate returns HasAccess=true when Read is allowed.
    /// </summary>
    [Fact]
    public void ParamAuth_CanCreate_ReturnsTrue_WhenReadAllowed()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        // Act
        var result = factory.CanCreate();

        // Assert
        Assert.True(result.HasAccess);

        local.Dispose();
    }

    /// <summary>
    /// Scenario 6: CanCreate returns HasAccess=false when Read is denied.
    /// </summary>
    [Fact]
    public void ParamAuth_CanCreate_ReturnsFalse_WhenReadDenied()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        ParamAuthOrderAuth.AllowRead = false;
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        // Act
        var result = factory.CanCreate();

        // Assert
        Assert.False(result.HasAccess);

        local.Dispose();
    }

    // -------------------------------------------------------------------------
    // Target Parameter Tests (CanWrite with entity state)
    //
    // GENERATOR BEHAVIOR: CanWrite(IParamAuthOrder target) receives entity
    //
    // On write operations, the generator passes the target entity to
    // CanWrite. Auth decisions can be based on entity state (Status field).
    //
    // GENERATOR BEHAVIOR: CanSave generated with two overloads
    //
    // CanSave() parameterless: runs only non-target Write auth (CanWriteRole)
    // CanSave(target): runs ALL Write auth (CanWriteRole + CanWrite(target))
    //
    // CanInsert/CanUpdate/CanDelete remain suppressed — entity not available
    // before the write operation.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 7: Save succeeds when entity Status is "Active".
    /// CanWrite(target) returns true because target.Status != "Locked".
    /// </summary>
    [Fact]
    public async Task ParamAuth_Save_Succeeds_WhenStatusActive()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        var order = await factory.Create("Active Order");
        Assert.NotNull(order);
        order.Status = "Active";

        // Act
        var result = await factory.Save(order);

        // Assert
        Assert.NotNull(result);
        Assert.False(result!.IsNew); // Reset by FactoryCompleteAsync

        local.Dispose();
    }

    /// <summary>
    /// Scenario 8: Save throws NotAuthorizedException when entity Status is "Locked".
    /// CanWrite(target) returns false because target.Status == "Locked".
    /// </summary>
    [Fact]
    public async Task ParamAuth_Save_ThrowsNotAuthorized_WhenStatusLocked()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        var order = await factory.Create("Locked Order");
        Assert.NotNull(order);
        order.Status = "Locked";

        // Act & Assert
        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.Save(order));

        local.Dispose();
    }

    /// <summary>
    /// Scenario 9: TrySave returns HasAccess=false when Status is "Locked".
    /// TrySave catches NotAuthorizedException from target auth failure.
    /// </summary>
    [Fact]
    public async Task ParamAuth_TrySave_ReturnsFalse_WhenStatusLocked()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        var order = await factory.Create("TrySave Locked");
        Assert.NotNull(order);
        order.Status = "Locked";

        // Act
        var result = await factory.TrySave(order);

        // Assert
        Assert.False(result.HasAccess);
        Assert.Null(result.Result);

        local.Dispose();
    }

    /// <summary>
    /// Scenario 10: TrySave returns HasAccess=true when Status is "Active".
    /// </summary>
    [Fact]
    public async Task ParamAuth_TrySave_ReturnsTrue_WhenStatusActive()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        var order = await factory.Create("TrySave Active");
        Assert.NotNull(order);
        order.Status = "Active";

        // Act
        var result = await factory.TrySave(order);

        // Assert
        Assert.True(result.HasAccess);
        Assert.NotNull(result.Result);
        Assert.False(result.Result!.IsNew);

        local.Dispose();
    }

    // -------------------------------------------------------------------------
    // CanSave Overloads (parameterless + target)
    //
    // GENERATOR BEHAVIOR: Two CanSave overloads when target auth exists
    //
    // CanSave() parameterless: runs only non-target Write auth (CanWriteRole).
    // CanSave(target): runs ALL Write auth (CanWriteRole AND CanWrite(target)).
    // This follows the same pattern as CanFetch calling multiple auth methods.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Plan Scenario 1: CanSave(target) returns true when all auth passes.
    /// Both CanWriteRole() and CanWrite(target) pass.
    /// </summary>
    [Fact]
    public async Task ParamAuth_CanSaveTarget_ReturnsTrue_WhenAllAuthPasses()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags(); // AllowWriteRole = true
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        var order = await factory.Create("CanSave Target Test");
        Assert.NotNull(order);
        order.Status = "Active"; // CanWrite(target) passes

        // Act
        var result = factory.CanSave(order);

        // Assert
        Assert.True(result.HasAccess);

        local.Dispose();
    }

    /// <summary>
    /// Plan Scenario 2: CanSave(target) returns false when target-param auth fails.
    /// CanWriteRole() passes but CanWrite(target) fails (Status = "Locked").
    /// </summary>
    [Fact]
    public async Task ParamAuth_CanSaveTarget_ReturnsFalse_WhenTargetAuthFails()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags(); // AllowWriteRole = true
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        var order = await factory.Create("CanSave Target Locked");
        Assert.NotNull(order);
        order.Status = "Locked"; // CanWrite(target) fails

        // Act
        var result = factory.CanSave(order);

        // Assert
        Assert.False(result.HasAccess);

        local.Dispose();
    }

    /// <summary>
    /// Plan Scenario 3: CanSave(target) returns false when non-target auth fails.
    /// CanWriteRole() fails, even though CanWrite(target) would pass.
    /// </summary>
    [Fact]
    public async Task ParamAuth_CanSaveTarget_ReturnsFalse_WhenNonTargetAuthFails()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        ParamAuthOrderAuth.AllowWriteRole = false; // Non-target auth fails
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        var order = await factory.Create("CanSave NonTarget Fail");
        Assert.NotNull(order);
        order.Status = "Active"; // CanWrite(target) would pass

        // Act
        var result = factory.CanSave(order);

        // Assert
        Assert.False(result.HasAccess);

        local.Dispose();
    }

    /// <summary>
    /// Plan Scenario 4: CanSave() parameterless returns true when non-target auth passes.
    /// Only CanWriteRole() is called — target auth is NOT invoked.
    /// </summary>
    [Fact]
    public void ParamAuth_CanSaveParameterless_ReturnsTrue_WhenNonTargetPasses()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags(); // AllowWriteRole = true
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        // Act
        var result = factory.CanSave();

        // Assert
        Assert.True(result.HasAccess);

        local.Dispose();
    }

    /// <summary>
    /// Plan Scenario 5: CanSave() parameterless returns false when non-target auth fails.
    /// </summary>
    [Fact]
    public void ParamAuth_CanSaveParameterless_ReturnsFalse_WhenNonTargetFails()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        ParamAuthOrderAuth.AllowWriteRole = false;
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IParamAuthOrderFactory>();

        // Act
        var result = factory.CanSave();

        // Assert
        Assert.False(result.HasAccess);

        local.Dispose();
    }

    /// <summary>
    /// Plan Scenario 6: CanSave(target) via factory interface delegates correctly.
    /// Verifies the target overload is accessible through the factory-specific interface.
    /// </summary>
    [Fact]
    public async Task ParamAuth_CanSaveTarget_ViaFactoryInterface_DelegatesCorrectly()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (_, _, local) = DesignClientServerContainers.Scopes();
        IParamAuthOrderFactory factory = local.GetRequiredService<IParamAuthOrderFactory>();

        var order = await factory.Create("Factory Interface Target Test");
        Assert.NotNull(order);
        order.Status = "Locked"; // CanWrite(target) fails

        // Act — call via IParamAuthOrderFactory which has the CanSave(IParamAuthOrder) overload
        var result = factory.CanSave(order);

        // Assert
        Assert.False(result.HasAccess);

        local.Dispose();
    }

    /// <summary>
    /// Plan Scenario 7: CanSave() parameterless via factory interface runs non-target auth.
    /// </summary>
    [Fact]
    public void ParamAuth_CanSaveParameterless_ViaFactoryInterface_RunsNonTargetAuth()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags(); // AllowWriteRole = true
        var (_, _, local) = DesignClientServerContainers.Scopes();
        IParamAuthOrderFactory factory = local.GetRequiredService<IParamAuthOrderFactory>();

        // Act
        var result = factory.CanSave();

        // Assert
        Assert.True(result.HasAccess);

        local.Dispose();
    }

    // -------------------------------------------------------------------------
    // Client-Server Round-Trip
    //
    // Verify parameterized auth works through the serialization boundary.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Scenario 11: Type-matched CanFetch works through client-server serialization.
    /// Fetch with denied Guid returns null even when crossing the wire.
    /// </summary>
    [Fact]
    public async Task ParamAuth_Fetch_ReturnsNull_ThroughClientServer_WhenGuidDenied()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IParamAuthOrderFactory>();

        // Act
        var result = await factory.Fetch(ParamAuthOrderAuth.DenyFetchGuid);

        // Assert
        Assert.Null(result);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Scenario 12: Target auth works through client-server serialization.
    /// Save with "Locked" status throws even when crossing the wire.
    /// </summary>
    [Fact]
    public async Task ParamAuth_Save_ThrowsNotAuthorized_ThroughClientServer_WhenLocked()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IParamAuthOrderFactory>();

        var order = await factory.Create("Client-Server Locked");
        Assert.NotNull(order);
        order.Status = "Locked";

        // Act & Assert
        await Assert.ThrowsAsync<NotAuthorizedException>(() => factory.Save(order));

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Plan Scenario 9: CanSave(target) works through client-server serialization.
    /// Calls CanSave(target) through the client→server boundary.
    /// </summary>
    [Fact]
    public async Task ParamAuth_CanSaveTarget_ThroughClientServer_WhenStatusLocked()
    {
        // Arrange
        ParamAuthOrderAuth.ResetFlags();
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IParamAuthOrderFactory>();

        var order = await factory.Create("Client-Server CanSave");
        Assert.NotNull(order);
        order.Status = "Locked";

        // Act
        var result = factory.CanSave(order);

        // Assert
        Assert.False(result.HasAccess);

        server.Dispose();
        client.Dispose();
    }
}
