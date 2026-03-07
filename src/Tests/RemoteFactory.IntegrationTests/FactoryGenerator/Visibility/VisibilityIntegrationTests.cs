using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Visibility;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Visibility;

/// <summary>
/// Integration tests for internal factory method visibility feature.
/// Tests verify behavior across client, server, and local containers.
/// </summary>
public class VisibilityIntegrationTests : IDisposable
{
    public VisibilityIntegrationTests()
    {
        IntegrationVisibilityAuth.ShouldAllow = true;
    }

    public void Dispose()
    {
        IntegrationVisibilityAuth.ShouldAllow = true;
    }

    #region Public Non-[Remote] Methods - Work on Client

    /// <summary>
    /// Public non-[Remote] Create works on the server container.
    /// No IsServerRuntime guard, so it runs directly.
    /// </summary>
    [Fact]
    public void PublicCreate_WorksOnServer()
    {
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.GetRequiredService<IPublicLocalCreateTargetFactory>();

        var result = factory.Create("server-test");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("server-test", result.ReceivedName);
    }

    /// <summary>
    /// Public non-[Remote] Create works on the local container (Logical mode).
    /// No guard, no remote trip -- direct execution.
    /// </summary>
    [Fact]
    public void PublicCreate_WorksOnLocal()
    {
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.local.GetRequiredService<IPublicLocalCreateTargetFactory>();

        var result = factory.Create("local-test");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("local-test", result.ReceivedName);
    }

    /// <summary>
    /// Public non-[Remote] Create works on the client container.
    /// Since there's no [Remote] attribute, the method runs locally on the client
    /// (no server trip). This is the key behavior: public non-[Remote] methods
    /// have no IsServerRuntime guard and no remote delegate, so they execute
    /// in-process on whichever container calls them.
    /// </summary>
    [Fact]
    public void PublicCreate_WorksOnClient_NoServerTrip()
    {
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.GetRequiredService<IPublicLocalCreateTargetFactory>();

        var result = factory.Create("client-test");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("client-test", result.ReceivedName);
    }

    #endregion

    #region Internal Methods - Work on Server

    /// <summary>
    /// Internal Create works on the server container.
    /// The IsServerRuntime guard passes because we're in Server mode.
    /// </summary>
    [Fact]
    public void InternalCreate_WorksOnServer()
    {
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.GetRequiredService<IInternalCreateTargetFactory>();

        var result = factory.Create("server-internal");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("server-internal", result.ReceivedName);
    }

    /// <summary>
    /// Internal Create works in local (Logical) mode.
    /// NeatooRuntime.IsServerRuntime defaults to true, so the guard passes.
    /// </summary>
    [Fact]
    public void InternalCreate_WorksOnLocal()
    {
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.local.GetRequiredService<IInternalCreateTargetFactory>();

        var result = factory.Create("local-internal");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("local-internal", result.ReceivedName);
    }

    #endregion

    #region Can* Methods - Work on Client

    /// <summary>
    /// CanCreate for a public method works on the server container.
    /// </summary>
    [Fact]
    public void CanCreate_PublicMethod_WorksOnServer()
    {
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.GetRequiredService<IPublicCreateWithAuthTargetFactory>();

        IntegrationVisibilityAuth.ShouldAllow = true;
        Assert.True(factory.CanCreate().HasAccess);

        IntegrationVisibilityAuth.ShouldAllow = false;
        Assert.False(factory.CanCreate().HasAccess);
    }

    /// <summary>
    /// CanCreate for a public method works on the client container
    /// without a server trip. This verifies the fix: public non-[Remote]
    /// Can* methods should not have the IsServerRuntime guard.
    /// </summary>
    [Fact]
    public void CanCreate_PublicMethod_WorksOnClient_NoServerTrip()
    {
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.client.GetRequiredService<IPublicCreateWithAuthTargetFactory>();

        IntegrationVisibilityAuth.ShouldAllow = true;
        Assert.True(factory.CanCreate().HasAccess);

        IntegrationVisibilityAuth.ShouldAllow = false;
        Assert.False(factory.CanCreate().HasAccess);
    }

    /// <summary>
    /// CanCreate for a public method works in local (Logical) mode.
    /// </summary>
    [Fact]
    public void CanCreate_PublicMethod_WorksOnLocal()
    {
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.local.GetRequiredService<IPublicCreateWithAuthTargetFactory>();

        IntegrationVisibilityAuth.ShouldAllow = true;
        Assert.True(factory.CanCreate().HasAccess);

        IntegrationVisibilityAuth.ShouldAllow = false;
        Assert.False(factory.CanCreate().HasAccess);
    }

    /// <summary>
    /// Create with authorization returns null when CanCreate returns false.
    /// Tests the full authorization enforcement path on the server.
    /// </summary>
    [Fact]
    public void PublicCreateWithAuth_WhenDenied_ReturnsNull()
    {
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.GetRequiredService<IPublicCreateWithAuthTargetFactory>();

        IntegrationVisibilityAuth.ShouldAllow = false;

        var result = factory.Create();

        Assert.Null(result);
    }

    /// <summary>
    /// Create with authorization returns result when CanCreate returns true.
    /// </summary>
    [Fact]
    public void PublicCreateWithAuth_WhenAllowed_ReturnsResult()
    {
        var scopes = ClientServerContainers.Scopes();
        var factory = scopes.server.GetRequiredService<IPublicCreateWithAuthTargetFactory>();

        IntegrationVisibilityAuth.ShouldAllow = true;

        var result = factory.Create();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    #endregion
}
